#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace ANIMOL.Art.EchoTheaterV03.Tests
{
    public sealed class EchoTheaterV03DiceVisualMapTests
    {
        private const string ManifestHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        [Serializable] private sealed class Document
        {
            public int schemaVersion = 1;
            public string packageId = EchoTheaterV03VisualCatalog.RequiredPackageId;
            public Orientation[] orientations;
            public Roll[] rolls;
        }
        [Serializable] private sealed class Orientation
        { public string id, stableClipKey; public int up, down, north, south, east, west; }
        [Serializable] private sealed class Roll { public string fromId, heading, nextId, clipKey; }
        private sealed class Fixture
        {
            public Document document;
            public EchoTheaterV03VisualCatalog catalog;
            public Dictionary<EchoTheaterV03DiceOrientation.State, Orientation> byState;
        }
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        [TearDown] public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; --i) if (owned[i] != null) UnityEngine.Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        [Test] public void UppercaseMapAndLowercaseCatalogResolveByActualStateAndArbitraryIds()
        {
            Fixture fixture = MakeFixture(); EchoTheaterV03DiceVisualMap map; string error;
            Assert.IsTrue(Parse(fixture, out map, out error), error);
            var before = EchoTheaterV03DiceOrientation.Initial;
            EchoTheaterV03DiceOrientation.State after;
            EchoTheaterV03DiceOrientation.TryPredictVisualRoll(before, EchoTheaterV03DiceOrientation.Heading.East, true, false, out after, out error);
            string key;
            Assert.IsTrue(map.TryGetStableClip(before, out key, out error), error);
            Assert.AreEqual(fixture.byState[before].stableClipKey, key);
            Assert.IsTrue(map.TryGetCommittedRollClip(before, after, EchoTheaterV03DiceOrientation.Heading.East, true, false, out key, out error), error);
            Assert.AreEqual("dice_block.roll." + fixture.byState[before].id + ".E", key);
            Assert.AreEqual(4, after.Up);
            Assert.IsTrue(map.TryGetCommittedRollClip(before, before, EchoTheaterV03DiceOrientation.Heading.East, false, false, out key, out error), error);
            Assert.IsNull(key, "Blocked input must not request any clip restart.");
            Assert.IsTrue(map.TryGetCommittedRollClip(before, before, EchoTheaterV03DiceOrientation.Heading.None, true, true, out key, out error), error);
            Assert.IsNull(key);
        }

        [Test] public void WrongCatalogHeadingIsRejectedEvenWhenKeyAndFramesMatch()
        {
            Fixture fixture = MakeFixture();
            var frames = new EchoTheaterV03VisualCatalog.Frame[fixture.catalog.SpriteCount];
            for (int i = 0; i < frames.Length; ++i) frames[i] = fixture.catalog.GetSpriteRecord(i);
            var clips = new EchoTheaterV03VisualCatalog.Clip[fixture.catalog.ClipCount];
            string badKey = fixture.document.rolls[0].clipKey;
            for (int i = 0; i < clips.Length; ++i)
            {
                var source = fixture.catalog.GetClipAt(i);
                if (source.Key != badKey) { clips[i] = source; continue; }
                var clipFrames = new EchoTheaterV03VisualCatalog.Frame[source.FrameCount];
                for (int f = 0; f < clipFrames.Length; ++f) clipFrames[f] = source.GetFrame(f);
                string wrongHeading = source.Heading == "north" ? "east" : "north";
                // A unique state avoids a separate duplicate-tuple failure masking the heading check.
                clips[i] = new EchoTheaterV03VisualCatalog.Clip(source.Key, source.Family, wrongHeading,
                    source.State + "_wrongHeadingOnly", source.Fps, source.Loop, clipFrames);
            }
            fixture.catalog.SetEditorData(fixture.catalog.SourceRootPath, fixture.catalog.SourceRootGuid,
                ManifestHash, frames, clips, new EchoTheaterV03VisualCatalog.Part[0]);
            EchoTheaterV03DiceVisualMap map; string error;
            Assert.IsFalse(Parse(fixture, out map, out error));
            StringAssert.Contains("heading", error);
        }

        [Test] public void AuthoritativeMismatchIsReportedRatherThanOverwritingState()
        {
            Fixture fixture = MakeFixture(); EchoTheaterV03DiceVisualMap map; string error;
            Assert.IsTrue(Parse(fixture, out map, out error), error);
            var before = EchoTheaterV03DiceOrientation.Initial;
            EchoTheaterV03DiceOrientation.State actualAfter;
            EchoTheaterV03DiceOrientation.TryPredictVisualRoll(before, EchoTheaterV03DiceOrientation.Heading.North, true, false, out actualAfter, out error);
            var savedActual = actualAfter; string key;
            Assert.IsFalse(map.TryGetCommittedRollClip(before, actualAfter, EchoTheaterV03DiceOrientation.Heading.East, true, false, out key, out error));
            Assert.IsNull(key);
            Assert.AreEqual(savedActual, actualAfter);
            Assert.IsTrue(map.TryGetStableClip(actualAfter, out key, out error), error);
            Assert.AreEqual(fixture.byState[actualAfter].stableClipKey, key);
        }

        [Test] public void WrongPinWrongNextAndIncompleteMapAreRejected()
        {
            Fixture fixture = MakeFixture(); EchoTheaterV03DiceVisualMap map; string error;
            byte[] bytes = Bytes(fixture.document);
            Assert.IsFalse(EchoTheaterV03DiceVisualMap.TryParse(bytes, new string('b', 64), fixture.catalog, ManifestHash, out map, out error));
            fixture.document.rolls[0].nextId = fixture.document.rolls[0].fromId;
            Assert.IsFalse(Parse(fixture, out map, out error));
            StringAssert.Contains("rotation", error);
            Array.Resize(ref fixture.document.rolls, 95);
            Assert.IsFalse(Parse(fixture, out map, out error));
            StringAssert.Contains("96", error);
        }

        private static byte[] Bytes(Document doc) { return Encoding.UTF8.GetBytes(JsonUtility.ToJson(doc)); }
        private static bool Parse(Fixture fixture, out EchoTheaterV03DiceVisualMap map, out string error)
        {
            byte[] bytes = Bytes(fixture.document);
            string hash = EchoTheaterV03VisualCatalog.Sha256Of(bytes);
            fixture.catalog.SetEditorDiceMapSha256(hash);
            return EchoTheaterV03DiceVisualMap.TryParse(bytes, hash, fixture.catalog, ManifestHash, out map, out error);
        }

        private Fixture MakeFixture()
        {
            var texture = new Texture2D(8, 8); owned.Add(texture);
            var allFrames = new List<EchoTheaterV03VisualCatalog.Frame>();
            var clips = new List<EchoTheaterV03VisualCatalog.Clip>();
            var orientations = new List<Orientation>();
            var byState = new Dictionary<EchoTheaterV03DiceOrientation.State, Orientation>();
            var stableFrames = new Dictionary<EchoTheaterV03DiceOrientation.State, EchoTheaterV03VisualCatalog.Frame>();
            // Deliberately reverse BFS and use nonsequential arbitrary ids. No oXX/index assumptions can pass.
            for (int i = 23; i >= 0; --i)
            {
                var state = EchoTheaterV03DiceOrientation.All24[i];
                string id = "custom_" + (101 + i * 7);
                var row = new Orientation { id = id, up = state.Up, down = state.Down, north = state.North,
                    south = state.South, east = state.East, west = state.West, stableClipKey = "dice_block.stable." + id };
                orientations.Add(row); byState.Add(state, row);
                var frame = Frame(texture, "stable_" + id, allFrames.Count); allFrames.Add(frame); stableFrames.Add(state, frame);
                clips.Add(new EchoTheaterV03VisualCatalog.Clip(row.stableClipKey, "dice_block", "None", "stable_" + id, 12, false, new[] { frame }));
            }
            var shells = new Dictionary<int, EchoTheaterV03VisualCatalog.Frame[]>();
            for (int h = 1; h <= 4; ++h)
            {
                var sequence = new EchoTheaterV03VisualCatalog.Frame[4];
                for (int i = 0; i < 4; ++i) { sequence[i] = Frame(texture, "covered_" + h + "_" + i, allFrames.Count); allFrames.Add(sequence[i]); }
                shells.Add(h, sequence);
            }
            var rolls = new List<Roll>();
            foreach (var pair in byState) for (int h = 1; h <= 4; ++h)
            {
                string heading = h == 1 ? "N" : h == 2 ? "E" : h == 3 ? "S" : "W";
                string catalogHeading = h == 1 ? "north" : h == 2 ? "east" : h == 3 ? "south" : "west";
                EchoTheaterV03DiceOrientation.State next; string error;
                EchoTheaterV03DiceOrientation.TryPredictVisualRoll(pair.Key, (EchoTheaterV03DiceOrientation.Heading)h, true, false, out next, out error);
                string key = "dice_block.roll." + pair.Value.id + "." + heading;
                rolls.Add(new Roll { fromId = pair.Value.id, nextId = byState[next].id, heading = heading, clipKey = key });
                var frames = new EchoTheaterV03VisualCatalog.Frame[6];
                frames[0] = stableFrames[pair.Key]; frames[5] = stableFrames[next];
                for (int i = 0; i < 4; ++i) frames[i + 1] = shells[h][i];
                clips.Add(new EchoTheaterV03VisualCatalog.Clip(key, "dice_block", catalogHeading, "roll_" + pair.Value.id, 12, false, frames));
            }
            var catalog = ScriptableObject.CreateInstance<EchoTheaterV03VisualCatalog>(); owned.Add(catalog);
            catalog.SetEditorData("Assets/ANIMOL_ECHO_THEATER_CARTOON_V03", new string('d', 32), ManifestHash,
                allFrames.ToArray(), clips.ToArray(), new EchoTheaterV03VisualCatalog.Part[0]);
            return new Fixture { catalog = catalog, byState = byState,
                document = new Document { orientations = orientations.ToArray(), rolls = rolls.ToArray() } };
        }
        private EchoTheaterV03VisualCatalog.Frame Frame(Texture2D texture, string name, int index)
        {
            var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 256);
            sprite.name = name; owned.Add(sprite);
            return new EchoTheaterV03VisualCatalog.Frame(sprite, "test/" + name + ".png", ManifestHash,
                new string('d', 32), 21300000 + index, 8, 8, 256, new Vector2(.5f, .5f));
        }
    }
}
#endif
