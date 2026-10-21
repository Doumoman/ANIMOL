using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WilliamParadox.Story
{
    [Serializable] public sealed class StoryPresentationPackage
    {
        public int formatVersion = 1;
        public float canvasWidth = 1080, canvasHeight = 1920;
        public StoryLayeredScene[] scenes;
        public StoryPresentationShot[] shots;
    }
    [Serializable] public sealed class StoryLayeredScene
    {
        public string sceneId, backgroundArtId, foregroundArtId;
        public float bounceStrength = 1f;
        public StoryStagePlacement foreground;
        public StoryActorPlacement[] actors;
    }
    [Serializable] public class StoryStagePlacement
    {
        // x/y are normalized, bottom-left origin. Width/height are logical canvas pixels.
        public float x = 0.5f, y = 0.5f, width = 1080, height = 1920;
        public float pivotX = 0.5f, pivotY = 0.5f;
    }
    [Serializable] public sealed class StoryActorPlacement : StoryStagePlacement
    {
        public string actorId, speaker, assetId;
        public int sortOrder;
    }
    [Serializable] public sealed class StoryActorState
    {
        public string actorId, assetId;
    }
    [Serializable] public sealed class StoryPresentationShot
    {
        public string nodeId, speakerActorId;
        public StoryActorState[] actorStates;
        public StoryCameraShot camera;
        public bool bounce = true;
    }
    [Serializable] public sealed class StoryCameraShot
    {
        public float focusX = 0.5f, focusY = 0.5f, scale = 1, duration = 0.25f;
    }

    /// <summary>Small authored sidecar. It never edits story text, flags or route.</summary>
    public sealed class StoryPresentationIndex
    {
        public const int FormatVersion = 1;
        private static readonly HashSet<string> BounceScenes = new HashSet<string>(StringComparer.Ordinal)
            { "R01", "R05", "R06", "R08", "R10", "R13", "R17" };
        public readonly StoryPresentationPackage Package;
        private readonly Dictionary<string, StoryLayeredScene> scenes;
        private readonly Dictionary<string, StoryPresentationShot> shots;
        public StoryPresentationIndex(StoryPresentationPackage package, StoryGraph graph)
        {
            var errors = Validate(package, graph);
            if (errors.Count != 0) throw new InvalidOperationException("Presentation data:\n" + string.Join("\n", errors));
            Package = package;
            scenes = (package.scenes ?? Array.Empty<StoryLayeredScene>()).ToDictionary(x => x.sceneId, StringComparer.Ordinal);
            shots = (package.shots ?? Array.Empty<StoryPresentationShot>()).ToDictionary(x => x.nodeId, StringComparer.Ordinal);
        }
        public static bool AllowsBounce(string sceneId) { return sceneId != null && BounceScenes.Contains(sceneId); }
        public StoryLayeredScene Scene(string sceneId) { return sceneId != null && scenes.TryGetValue(sceneId, out var value) ? value : null; }
        public StoryPresentationShot Shot(string nodeId) { return nodeId != null && shots.TryGetValue(nodeId, out var value) ? value : null; }
        public bool ShouldBounce(StoryNode node)
        {
            return node != null && node.kind == "line" && AllowsBounce(node.sceneId) && (Shot(node.id) == null || Shot(node.id).bounce);
        }
        public string SpeakerActor(StoryNode node)
        {
            var scene = Scene(node.sceneId);
            if (scene == null) return null;
            var explicitId = Shot(node.id)?.speakerActorId;
            if (!string.IsNullOrEmpty(explicitId)) return explicitId;
            if (string.IsNullOrWhiteSpace(node.speaker)) return null;
            return (scene.actors ?? Array.Empty<StoryActorPlacement>()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.speaker) && x.speaker == node.speaker)?.actorId;
        }
        // Whole-CG squash/shake is deliberately disabled. Closeups in this edition use the sidecar camera.
        public static StoryCue[] FilterLegacyCues(StoryCue[] source, bool hasPresentation)
        {
            return (source ?? Array.Empty<StoryCue>()).Where(c => c != null && c.type != "shake" && c.type != "popup" && (!hasPresentation || c.type != "zoom")).ToArray();
        }
        public Dictionary<string, string> ResolveActorStates(StoryGraph graph, StoryNode current)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            string previousScene = null;
            var id = graph.Package.startNodeId;
            // Follow the validated route rather than relying on JSON array order.
            while (!string.IsNullOrEmpty(id))
            {
                var node = graph.Node(id);
                if (node == null) break;
                if (node.sceneId != previousScene)
                {
                    result.Clear();
                    var scene = Scene(node.sceneId);
                    foreach (var actor in scene?.actors ?? Array.Empty<StoryActorPlacement>()) result[actor.actorId] = actor.assetId;
                    previousScene = node.sceneId;
                }
                foreach (var state in Shot(node.id)?.actorStates ?? Array.Empty<StoryActorState>()) result[state.actorId] = state.assetId;
                if (id == current.id) break;
                id = node.nextId;
            }
            return result;
        }
        public StoryCameraShot ResolveCamera(StoryGraph graph, StoryNode current)
        {
            var result = new StoryCameraShot { duration = 0 };
            string previousScene = null;
            string id = graph.Package.startNodeId;
            while (!string.IsNullOrEmpty(id))
            {
                var node = graph.Node(id);
                if (node == null) break;
                if (node.sceneId != previousScene) { result = new StoryCameraShot { duration = 0 }; previousScene = node.sceneId; }
                var authored = Shot(node.id)?.camera;
                if (authored != null) result = authored;
                if (id == current.id) break;
                id = node.nextId;
            }
            return result;
        }

        public static List<string> Validate(StoryPresentationPackage p, StoryGraph graph)
        {
            var errors = new List<string>();
            if (p == null || graph == null) { errors.Add("Presentation and story graph are required."); return errors; }
            if (p.formatVersion != FormatVersion) errors.Add("formatVersion must be1.");
            if (p.scenes == null || p.shots == null) errors.Add("scenes and shots arrays are required.");
            if (!StoryGraph.Finite(p.canvasWidth, p.canvasHeight) || p.canvasWidth != 1080 || p.canvasHeight != 1920) errors.Add("Presentation canvas must be1080x1920.");
            var sceneMap = new Dictionary<string, StoryLayeredScene>(StringComparer.Ordinal);
            var actualScenes = new HashSet<string>(graph.Package.nodes.Select(n => n.sceneId), StringComparer.Ordinal);
            foreach (var scene in p.scenes ?? Array.Empty<StoryLayeredScene>())
            {
                if (scene == null || string.IsNullOrWhiteSpace(scene.sceneId) || !actualScenes.Contains(scene.sceneId) || sceneMap.ContainsKey(scene.sceneId)) { errors.Add("Unknown or duplicate layered scene."); continue; }
                sceneMap.Add(scene.sceneId, scene);
                if (!StoryGraph.Finite(scene.bounceStrength) || scene.bounceStrength < 0 || scene.bounceStrength > 1) errors.Add(scene.sceneId + ": bounceStrength must be0..1.");
                if (!AllowsBounce(scene.sceneId)) errors.Add(scene.sceneId + ": only the seven requested scenes can use separate animated actors.");
                if (graph.Asset(scene.backgroundArtId) == null) errors.Add(scene.sceneId + ": missing backgroundArtId.");
                if (!string.IsNullOrEmpty(scene.foregroundArtId) && graph.Asset(scene.foregroundArtId) == null) errors.Add(scene.sceneId + ": unknown foregroundArtId.");
                if (scene.foreground != null && !ValidPlacement(scene.foreground)) errors.Add(scene.sceneId + ": invalid foreground placement.");
                var actors = new HashSet<string>(StringComparer.Ordinal);
                var speakers = new HashSet<string>(StringComparer.Ordinal);
                foreach (var actor in scene.actors ?? Array.Empty<StoryActorPlacement>())
                {
                    if (actor == null || string.IsNullOrWhiteSpace(actor.actorId) || !actors.Add(actor.actorId)) { errors.Add(scene.sceneId + ": missing/duplicate actorId."); continue; }
                    // Crowd cutouts may intentionally have no speaker and never bounce.
                    if (!string.IsNullOrWhiteSpace(actor.speaker) && !speakers.Add(actor.speaker)) errors.Add(scene.sceneId + ": actor speaker must be unique.");
                    if (graph.Asset(actor.assetId) == null || graph.Asset(actor.assetId).kind != "actor") errors.Add(scene.sceneId + ": actor asset must have kind actor: " + actor.assetId);
                    if (!ValidPlacement(actor)) errors.Add(scene.sceneId + ": invalid actor placement " + actor.actorId);
                }
                if (actors.Count == 0) errors.Add(scene.sceneId + ": layered scene needs actors.");
            }
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var shot in p.shots ?? Array.Empty<StoryPresentationShot>())
            {
                if (shot == null || string.IsNullOrWhiteSpace(shot.nodeId) || !nodeIds.Add(shot.nodeId)) { errors.Add("Missing/duplicate presentation nodeId."); continue; }
                var node = graph.Node(shot.nodeId);
                if (node == null) { errors.Add("Unknown presentation nodeId " + shot.nodeId); continue; }
                sceneMap.TryGetValue(node.sceneId, out var scene);
                var actorIds = new HashSet<string>((scene?.actors ?? Array.Empty<StoryActorPlacement>()).Where(a => a != null).Select(a => a.actorId), StringComparer.Ordinal);
                if (!string.IsNullOrEmpty(shot.speakerActorId) && !actorIds.Contains(shot.speakerActorId)) errors.Add(shot.nodeId + ": unknown speakerActorId.");
                var states = new HashSet<string>(StringComparer.Ordinal);
                foreach (var state in shot.actorStates ?? Array.Empty<StoryActorState>())
                    if (state == null || !actorIds.Contains(state.actorId ?? "") || graph.Asset(state.assetId) == null || graph.Asset(state.assetId).kind != "actor" || !states.Add(state.actorId)) errors.Add(shot.nodeId + ": invalid actor state.");
                var c = shot.camera;
                if (c != null && (!StoryGraph.Finite(c.focusX, c.focusY, c.scale, c.duration) || c.focusX < 0 || c.focusX > 1 || c.focusY < 0 || c.focusY > 1 || c.scale < 1 || c.scale > 2.2f || c.duration < 0 || c.duration > 2)) errors.Add(shot.nodeId + ": invalid camera focus/scale/duration.");
            }
            return errors;
        }
        private static bool ValidPlacement(StoryStagePlacement p)
        {
            return StoryGraph.Finite(p.x, p.y, p.width, p.height, p.pivotX, p.pivotY) && p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1 && p.width > 0 && p.height > 0 && p.width <= 4320 && p.height <= 7680 && p.pivotX >= 0 && p.pivotX <= 1 && p.pivotY >= 0 && p.pivotY <= 1;
        }
    }
}
