using System;
using System.Collections.Generic;
using System.Linq;

namespace WilliamParadox.Story
{
    /// <summary>Pure data validation: does not load Unity objects or mutate a save.</summary>
    public sealed class StoryGraph
    {
        public const int SchemaVersion = 14;
        public readonly StoryPackage Package;
        private readonly Dictionary<string, StoryNode> nodes;
        private readonly Dictionary<string, StoryAsset> assets;
        public StoryNode Node(string id) { return id != null && nodes.TryGetValue(id, out var n) ? n : null; }
        public StoryAsset Asset(string id) { return id != null && assets.TryGetValue(id, out var a) ? a : null; }

        private StoryGraph(StoryPackage package)
        {
            Package = package;
            nodes = package.nodes.ToDictionary(n => n.id, StringComparer.Ordinal);
            assets = package.assets.ToDictionary(a => a.id, StringComparer.Ordinal);
        }

        public static bool TryCreate(StoryPackage p, out StoryGraph graph, out List<string> errors)
        {
            graph = null;
            errors = Validate(p);
            if (errors.Count != 0) return false;
            graph = new StoryGraph(p);
            return true;
        }

        public static List<string> Validate(StoryPackage p)
        {
            var errors = new List<string>();
            if (p == null) { errors.Add("Story JSON is empty or malformed."); return errors; }
            if (p.schemaVersion != SchemaVersion) errors.Add("schemaVersion must be 14.");
            if (string.IsNullOrWhiteSpace(p.packageId)) errors.Add("packageId is required.");
            if (string.IsNullOrWhiteSpace(p.contentVersion)) errors.Add("contentVersion is required.");
            var ns = p.nodes ?? Array.Empty<StoryNode>();
            var ars = p.assets ?? Array.Empty<StoryAsset>();
            if (ns.Length == 0) errors.Add("nodes must not be empty.");
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            var assetIds = new HashSet<string>(StringComparer.Ordinal);
            var knownFlags = new HashSet<string>(StringComparer.Ordinal);
            var assetKinds = new HashSet<string> { "scene", "background", "foreground", "inset", "actor", "prop", "atlas", "ui" };
            foreach (var a in ars)
            {
                if (a == null) { errors.Add("Null asset."); continue; }
                if (string.IsNullOrWhiteSpace(a.id) || !assetIds.Add(a.id)) errors.Add("Missing or duplicate asset ID: " + a.id);
                if (string.IsNullOrWhiteSpace(a.resourcePath) || a.resourcePath.Contains("..") || a.resourcePath.StartsWith("/") || a.resourcePath.Contains("\\") || System.IO.Path.HasExtension(a.resourcePath)) errors.Add(a.id + ": resourcePath must be an extension-free Resources path.");
                if (!assetKinds.Contains(a.kind ?? "")) errors.Add(a.id + ": unknown asset kind " + a.kind);
                if (StorySpriteRect.IsSpecified(a.rect))
                {
                    var r = a.rect;
                    if (!Finite(r.x, r.y, r.width, r.height, r.pivotX, r.pivotY) || r.x < 0 || r.y < 0 || r.width <= 0 || r.height <= 0 || r.pivotX < 0 || r.pivotX > 1 || r.pivotY < 0 || r.pivotY > 1) errors.Add(a.id + ": invalid sprite rect/pivot.");
                }
            }
            foreach (var n in ns)
            {
                if (n == null) { errors.Add("Null node."); continue; }
                if (string.IsNullOrWhiteSpace(n.id) || !nodeIds.Add(n.id)) errors.Add("Missing or duplicate node ID: " + n.id);
                foreach (var f in n.setFlags ?? Array.Empty<string>())
                    if (string.IsNullOrWhiteSpace(f)) errors.Add(n.id + ": empty setFlag."); else knownFlags.Add(f);
            }
            if (string.IsNullOrWhiteSpace(p.startNodeId) || !nodeIds.Contains(p.startNodeId)) errors.Add("Unknown startNodeId.");
            var kinds = new HashSet<string> { "line", "cue", "gate", "credits", "end" };
            var cueKinds = new HashSet<string> { "dim", "zoom", "shake", "popup", "transition", "inset", "hideInset" };
            foreach (var n in ns)
            {
                if (n == null) continue;
                if (!kinds.Contains(n.kind ?? "")) errors.Add(n.id + ": unknown node kind " + n.kind);
                if (string.IsNullOrWhiteSpace(n.sceneId) || string.IsNullOrWhiteSpace(n.beatId)) errors.Add(n.id + ": sceneId and beatId are required.");
                if (n.kind == "line" && string.IsNullOrWhiteSpace(n.text)) errors.Add(n.id + ": line text is empty.");
                if (!string.IsNullOrEmpty(n.artId) && !assetIds.Contains(n.artId)) errors.Add(n.id + ": unknown artId " + n.artId);
                if (n.kind == "end") { if (!string.IsNullOrEmpty(n.nextId)) errors.Add(n.id + ": end cannot have nextId."); }
                else if (string.IsNullOrEmpty(n.nextId) || !nodeIds.Contains(n.nextId)) errors.Add(n.id + ": unknown nextId " + n.nextId);
                foreach (var f in n.requiredFlags ?? Array.Empty<string>())
                    if (string.IsNullOrWhiteSpace(f) || !knownFlags.Contains(f)) errors.Add(n.id + ": unknown requiredFlag " + f);
                if (n.kind == "gate" && (string.IsNullOrWhiteSpace(n.gateId) || string.IsNullOrWhiteSpace(n.gateLabel))) errors.Add(n.id + ": gate ID and label are required.");
                if (n.gateId == "BLOCK_LAMP" && (!Contains(n.requiredFlags, "ISOL_WANTS_RETURN") || n.setFlags == null || n.setFlags.Length != 1 || n.setFlags[0] != "PLAYER_BLOCKS_LAMP")) errors.Add(n.id + ": BLOCK_LAMP must require Isol's wish and set only the player's separate action flag.");
                if (n.kind == "credits" && (n.credits == null || n.credits.Length == 0 || n.credits.Any(string.IsNullOrWhiteSpace))) errors.Add(n.id + ": credits paragraphs are required.");
                foreach (var c in n.cues ?? Array.Empty<StoryCue>())
                {
                    if (c == null) { errors.Add(n.id + ": null cue."); continue; }
                    if (!cueKinds.Contains(c.type ?? "")) errors.Add(n.id + ": unknown cue type " + c.type);
                    if (!Finite(c.duration, c.delay, c.amount, c.x, c.y, c.scale) || c.duration < 0 || c.delay < 0) errors.Add(n.id + ": invalid cue timing/value.");
                    if (!string.IsNullOrEmpty(c.target) && c.target != "artwork" && c.target != "inset") errors.Add(n.id + ": unknown cue target " + c.target);
                    if ((c.type == "dim" || c.type == "transition") && c.target == "inset") errors.Add(n.id + ": dim/transition target must be artwork.");
                    if (c.type == "dim" && (c.amount < 0 || c.amount > 1)) errors.Add(n.id + ": dim amount must be 0..1.");
                    if (c.type == "zoom" && c.amount <= 0) errors.Add(n.id + ": zoom amount must be positive.");
                    if (c.type == "shake" && c.amount < 0) errors.Add(n.id + ": shake amount cannot be negative.");
                    if (c.scale < 0) errors.Add(n.id + ": cue scale cannot be negative.");
                    if (c.type == "inset" && (string.IsNullOrEmpty(c.assetId) || !assetIds.Contains(c.assetId))) errors.Add(n.id + ": inset requires a known assetId.");
                    if (!string.IsNullOrEmpty(c.assetId) && !assetIds.Contains(c.assetId)) errors.Add(n.id + ": unknown cue assetId " + c.assetId);
                }
            }
            // This preview intentionally has a single linear route. A cycle is malformed data,
            // not a real-time time-loop mechanic; repeated story beats have distinct node IDs.
            if (errors.Count == 0)
            {
                var map = ns.ToDictionary(n => n.id, StringComparer.Ordinal);
                var seen = new HashSet<string>();
                var flags = new HashSet<string>();
                string id = p.startNodeId;
                while (!string.IsNullOrEmpty(id))
                {
                    if (!seen.Add(id)) { errors.Add("Graph cycle at " + id); break; }
                    var n = map[id];
                    if ((n.requiredFlags ?? Array.Empty<string>()).Any(f => !flags.Contains(f))) errors.Add(id + ": required flags cannot be reached on the story route.");
                    foreach (var f in n.setFlags ?? Array.Empty<string>()) flags.Add(f);
                    id = n.nextId;
                }
                foreach (var n in ns) if (!seen.Contains(n.id)) errors.Add("Unreachable node: " + n.id);
            }
            return errors;
        }

        internal HashSet<string> ExpectedFlagsAt(string target)
        {
            var flags = new HashSet<string>(StringComparer.Ordinal);
            var id = Package.startNodeId;
            while (!string.IsNullOrEmpty(id))
            {
                var n = Node(id);
                if (n == null) return null;
                if (id == target)
                {
                    if (n.kind != "gate") foreach (var f in n.setFlags ?? Array.Empty<string>()) flags.Add(f);
                    return flags;
                }
                foreach (var f in n.setFlags ?? Array.Empty<string>()) flags.Add(f);
                id = n.nextId;
            }
            return null;
        }

        internal static bool Contains(string[] values, string value) { return values != null && Array.IndexOf(values, value) >= 0; }
        internal static bool Finite(params float[] values) { return values.All(v => !float.IsNaN(v) && !float.IsInfinity(v)); }
    }

    /// <summary>Pure progress model. Ordinary acknowledge never bypasses a gate or credits.</summary>
    public sealed class StorySession
    {
        public readonly StoryGraph Graph;
        public StoryNode Current { get; private set; }
        private readonly HashSet<string> flags = new HashSet<string>(StringComparer.Ordinal);
        public string[] Flags { get { return flags.OrderBy(f => f, StringComparer.Ordinal).ToArray(); } }
        public bool HasFlag(string flag) { return flags.Contains(flag); }
        public StorySession(StoryGraph graph) { Graph = graph ?? throw new ArgumentNullException(nameof(graph)); Reset(); }
        public void Reset() { flags.Clear(); Enter(Graph.Package.startNodeId); }
        public bool CanEnter(StoryNode n) { return n != null && (n.requiredFlags ?? Array.Empty<string>()).All(flags.Contains); }
        private bool Enter(string id)
        {
            var n = Graph.Node(id);
            if (!CanEnter(n)) return false;
            Current = n;
            if (n.kind != "gate") foreach (var f in n.setFlags ?? Array.Empty<string>()) flags.Add(f);
            return true;
        }
        public bool Acknowledge()
        {
            return Current != null && Current.kind != "gate" && Current.kind != "credits" && Current.kind != "end" && Enter(Current.nextId);
        }
        public bool ActivateGate()
        {
            if (Current == null || Current.kind != "gate" || !CanEnter(Current)) return false;
            var old = new HashSet<string>(flags);
            foreach (var f in Current.setFlags ?? Array.Empty<string>()) flags.Add(f);
            if (Enter(Current.nextId)) return true;
            flags.Clear(); flags.UnionWith(old); return false;
        }
        public bool ContinueCredits() { return Current != null && Current.kind == "credits" && Enter(Current.nextId); }
        public bool DebugJump(string id)
        {
            var n = Graph.Node(id);
            if (!CanEnter(n)) return false;
            var expected = Graph.ExpectedFlagsAt(id);
            var proposed = new HashSet<string>(flags);
            if (n.kind != "gate") foreach (var f in n.setFlags ?? Array.Empty<string>()) proposed.Add(f);
            // Developer preview may move within the current flag epoch, but never invent
            // a rescue, wish or player action merely because a late scene was selected.
            return expected != null && expected.SetEquals(proposed) && Enter(id);
        }
        public bool Restore(StorySaveData save)
        {
            if (save == null || save.schemaVersion != StoryGraph.SchemaVersion || save.packageId != Graph.Package.packageId || save.contentVersion != Graph.Package.contentVersion || Graph.Node(save.nodeId) == null) return false;
            var expected = Graph.ExpectedFlagsAt(save.nodeId);
            var supplied = new HashSet<string>(save.flags ?? Array.Empty<string>(), StringComparer.Ordinal);
            if (expected == null || !expected.SetEquals(supplied)) return false;
            if (!StoryGraph.Finite(save.artScale, save.dimAlpha, save.insetX, save.insetY, save.insetScale, save.creditsPosition, save.creditsHold) || save.artScale <= 0 || save.insetScale <= 0 || save.dimAlpha < 0 || save.dimAlpha > 1 || save.creditsHold < 0 || save.creditsParagraph < 0) return false;
            if (save.insetVisible && Graph.Asset(save.insetAssetId) == null) return false;
            flags.Clear(); flags.UnionWith(supplied);
            Current = Graph.Node(save.nodeId);
            return CanEnter(Current);
        }
    }
}
