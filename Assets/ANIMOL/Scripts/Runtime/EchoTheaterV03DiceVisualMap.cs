using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace ANIMOL.Art.EchoTheaterV03
{
    /// <summary>Strict visual lookup by authoritative six-face state, never by an assumed oXX enumeration.</summary>
    public sealed class EchoTheaterV03DiceVisualMap
    {
        [Serializable] private sealed class Document
        { public int schemaVersion; public string packageId; public OrientationRow[] orientations; public RollRow[] rolls; }
        [Serializable] private sealed class OrientationRow
        { public string id, stableClipKey; public int up, down, north, south, east, west; }
        [Serializable] private sealed class RollRow
        { public string fromId, heading, nextId, clipKey; }
        private sealed class Stable
        {
            public string id, key;
            public EchoTheaterV03DiceOrientation.State state;
            public EchoTheaterV03VisualCatalog.Clip clip;
        }
        private sealed class Roll
        { public Stable from, next; public EchoTheaterV03DiceOrientation.Heading heading; public string key; }
        private readonly Dictionary<EchoTheaterV03DiceOrientation.State, Stable> stableByState;
        private readonly Dictionary<string, Roll> rollsByIdHeading;
        public string SourceSha256 { get; private set; }

        private EchoTheaterV03DiceVisualMap(Dictionary<EchoTheaterV03DiceOrientation.State, Stable> stable,
            Dictionary<string, Roll> rolls, string sourceHash)
        { stableByState = stable; rollsByIdHeading = rolls; SourceSha256 = sourceHash; }

        public static bool TryParse(byte[] utf8Json, string expectedMapSha256, EchoTheaterV03VisualCatalog catalog,
            string expectedAnimationManifestSha256, out EchoTheaterV03DiceVisualMap map, out string error)
        {
            map = null; error = null;
            if (utf8Json == null || catalog == null || !EchoTheaterV03VisualCatalog.IsHash(expectedMapSha256, 64)
                || !string.Equals(expectedMapSha256, EchoTheaterV03VisualCatalog.Sha256Of(utf8Json), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(expectedMapSha256, catalog.DiceOrientationMapSha256, StringComparison.OrdinalIgnoreCase))
            { error = "Missing/wrong latest dice map bytes, catalog or pinned SHA256."; return false; }
            if (!catalog.ValidateIntegrity(expectedAnimationManifestSha256, out error)) return false;
            Document doc;
            try { doc = JsonUtility.FromJson<Document>(Encoding.UTF8.GetString(utf8Json)); }
            catch (Exception e) { error = "Invalid dice orientation JSON: " + e.Message; return false; }
            if (doc == null || doc.schemaVersion != 1 || doc.packageId != EchoTheaterV03VisualCatalog.RequiredPackageId
                || doc.orientations == null || doc.orientations.Length != 24 || doc.rolls == null || doc.rolls.Length != 96)
            { error = "Dice map requires latest schema/package, exactly24 orientations and96 rolls."; return false; }

            var byId = new Dictionary<string, Stable>(StringComparer.Ordinal);
            var byState = new Dictionary<EchoTheaterV03DiceOrientation.State, Stable>();
            var stableKeys = new HashSet<string>(StringComparer.Ordinal);
            var stableSprites = new HashSet<Sprite>();
            foreach (OrientationRow row in doc.orientations)
            {
                EchoTheaterV03DiceOrientation.State state;
                if (row == null || !Token(row.id) || !Token(row.stableClipKey) || byId.ContainsKey(row.id)
                    || !stableKeys.Add(row.stableClipKey))
                { error = "Missing/duplicate dice orientation id or stable clip key."; return false; }
                if (!EchoTheaterV03DiceOrientation.TryCreate(row.up, row.down, row.north, row.south, row.east, row.west, out state, out error)) return false;
                if (byState.ContainsKey(state)) { error = "Duplicate six-face dice orientation: " + row.id; return false; }
                EchoTheaterV03VisualCatalog.Clip clip;
                if (!catalog.TryGetClip(row.stableClipKey, expectedAnimationManifestSha256, out clip, out error)) return false;
                if (clip.Family != "dice_block" || clip.FrameCount < 1)
                { error = "Dice stable key must reference a dice_block clip: " + row.stableClipKey; return false; }
                Sprite stableSprite = clip.GetFrame(0).Sprite;
                for (int i = 1; i < clip.FrameCount; ++i)
                    if (clip.GetFrame(i).Sprite != stableSprite) { error = "Stable dice clip must hold its stable endpoint: " + clip.Key; return false; }
                if (!stableSprites.Add(stableSprite)) { error = "Two full orientations share the same baked stable Sprite."; return false; }
                var entry = new Stable { id = row.id, key = row.stableClipKey, state = state, clip = clip };
                byId.Add(row.id, entry); byState.Add(state, entry);
            }

            var rolls = new Dictionary<string, Roll>(StringComparer.Ordinal);
            var rollKeys = new HashSet<string>(StringComparer.Ordinal);
            var shellsByHeading = new Dictionary<EchoTheaterV03DiceOrientation.Heading, Sprite[]>();
            var allShells = new HashSet<Sprite>();
            foreach (RollRow row in doc.rolls)
            {
                Stable from, next; EchoTheaterV03DiceOrientation.Heading heading;
                if (row == null || !Token(row.fromId) || !Token(row.nextId) || !Token(row.clipKey)
                    || !byId.TryGetValue(row.fromId, out from) || !byId.TryGetValue(row.nextId, out next)
                    || !TryHeading(row.heading, out heading) || !rollKeys.Add(row.clipKey))
                { error = "Missing/duplicate dice roll key, ids or cardinal heading."; return false; }
                string bindingKey = RollKey(from.id, heading);
                if (rolls.ContainsKey(bindingKey)) { error = "Duplicate fromId/heading dice roll: " + bindingKey; return false; }
                EchoTheaterV03DiceOrientation.State predicted;
                if (!EchoTheaterV03DiceOrientation.TryPredictVisualRoll(from.state, heading, true, false, out predicted, out error)) return false;
                if (!predicted.Equals(next.state)) { error = "Dice map nextId is not the correct complete90-degree rotation: " + bindingKey; return false; }
                EchoTheaterV03VisualCatalog.Clip clip;
                if (!catalog.TryGetClip(row.clipKey, expectedAnimationManifestSha256, out clip, out error)) return false;
                EchoTheaterV03DiceOrientation.Heading catalogHeading;
                if (!TryCatalogHeading(clip.Heading, out catalogHeading) || catalogHeading != heading)
                { error = "Dice roll catalog heading differs from the map cardinal heading: " + row.clipKey; return false; }
                if (clip.Family != "dice_block" || clip.Loop || clip.FrameCount != 6
                    || Mathf.Abs(clip.Fps - 12f) > .001f
                    || clip.GetFrame(0).Sprite != from.clip.GetFrame(0).Sprite
                    || clip.GetFrame(5).Sprite != next.clip.GetFrame(0).Sprite)
                { error = "Dice roll must be12fps/nonloop/fromStable+4covered+exactNextStable: " + row.clipKey; return false; }
                var shells = new Sprite[4]; var distinct = new HashSet<Sprite>();
                for (int i = 0; i < 4; ++i)
                {
                    shells[i] = clip.GetFrame(i + 1).Sprite;
                    if (stableSprites.Contains(shells[i]) || !distinct.Add(shells[i]))
                    { error = "Dice interior must contain four distinct covered shell frames, not numbered stable frames: " + row.clipKey; return false; }
                }
                Sprite[] registered;
                if (shellsByHeading.TryGetValue(heading, out registered))
                {
                    for (int i = 0; i < 4; ++i) if (shells[i] != registered[i])
                    { error = "All24 rolls of one heading must share the authored covered4-frame sequence."; return false; }
                }
                else
                {
                    shellsByHeading.Add(heading, shells);
                    foreach (Sprite shell in shells) allShells.Add(shell);
                }
                rolls.Add(bindingKey, new Roll { from = from, next = next, heading = heading, key = row.clipKey });
            }
            if (shellsByHeading.Count != 4 || allShells.Count != 16)
            { error = "Dice requires16 distinct covered native shells across four cardinal headings."; return false; }
            foreach (Stable state in byId.Values) for (int h = 1; h <= 4; ++h)
                if (!rolls.ContainsKey(RollKey(state.id, (EchoTheaterV03DiceOrientation.Heading)h)))
                { error = "Missing full-orientation/cardinal roll: " + state.id; return false; }
            map = new EchoTheaterV03DiceVisualMap(byState, rolls, expectedMapSha256.ToLowerInvariant());
            return true;
        }

        public bool TryGetStableClip(EchoTheaterV03DiceOrientation.State authoritativeState, out string clipKey, out string error)
        {
            clipKey = null; error = null;
            if (!EchoTheaterV03DiceOrientation.Validate(authoritativeState, out error)) return false;
            Stable entry;
            if (!stableByState.TryGetValue(authoritativeState, out entry)) { error = "Missing latest full-orientation stable clip."; return false; }
            clipKey = entry.key; return true;
        }

        // before/after come from actual project state. A no-roll returns null key: it NEVER restarts animation.
        // On mismatch, report and separately snap to the actual after stable if valid. Never alter game orientation.
        public bool TryGetCommittedRollClip(EchoTheaterV03DiceOrientation.State authoritativeBefore,
            EchoTheaterV03DiceOrientation.State authoritativeAfter, EchoTheaterV03DiceOrientation.Heading heading,
            bool actualCellRolled, bool teleport, out string clipKey, out string error)
        {
            clipKey = null; error = null;
            if (!EchoTheaterV03DiceOrientation.Validate(authoritativeBefore, out error)
                || !EchoTheaterV03DiceOrientation.Validate(authoritativeAfter, out error)) return false;
            if (!actualCellRolled || teleport || heading == EchoTheaterV03DiceOrientation.Heading.None)
            {
                if (!authoritativeBefore.Equals(authoritativeAfter))
                { error = "Authoritative orientation changed without an actual roll; visual helper will not overwrite it."; return false; }
                return true;
            }
            Stable before; Roll roll;
            if (!stableByState.TryGetValue(authoritativeBefore, out before)
                || !rollsByIdHeading.TryGetValue(RollKey(before.id, heading), out roll))
            { error = "Missing exact latest full-orientation/heading roll clip."; return false; }
            if (!roll.next.state.Equals(authoritativeAfter))
            { error = "Actual project after-state differs from the mapped visual roll; use actual state, never numeric increment."; return false; }
            clipKey = roll.key; return true;
        }

        private static bool Token(string value) { return !string.IsNullOrWhiteSpace(value) && value == value.Trim() && value.IndexOf('\u001f') < 0; }
        private static string RollKey(string id, EchoTheaterV03DiceOrientation.Heading heading)
        { return id + "\u001f" + ((int)heading).ToString(CultureInfo.InvariantCulture); }
        private static bool TryHeading(string heading, out EchoTheaterV03DiceOrientation.Heading value)
        {
            switch (heading)
            {
                case "N": value = EchoTheaterV03DiceOrientation.Heading.North; return true;
                case "E": value = EchoTheaterV03DiceOrientation.Heading.East; return true;
                case "S": value = EchoTheaterV03DiceOrientation.Heading.South; return true;
                case "W": value = EchoTheaterV03DiceOrientation.Heading.West; return true;
                default: value = EchoTheaterV03DiceOrientation.Heading.None; return false;
            }
        }
        // Explicit notation conversion for this map only. Generic catalog lookups remain exact.
        // Map JSON stays N/E/S/W; the assembled clip catalog uses north/east/south/west.
        private static bool TryCatalogHeading(string heading, out EchoTheaterV03DiceOrientation.Heading value)
        {
            switch (heading)
            {
                case "north": case "N": value = EchoTheaterV03DiceOrientation.Heading.North; return true;
                case "east": case "E": value = EchoTheaterV03DiceOrientation.Heading.East; return true;
                case "south": case "S": value = EchoTheaterV03DiceOrientation.Heading.South; return true;
                case "west": case "W": value = EchoTheaterV03DiceOrientation.Heading.West; return true;
                default: value = EchoTheaterV03DiceOrientation.Heading.None; return false;
            }
        }
    }
}
