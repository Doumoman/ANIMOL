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
        // null preserves the seven-scene ranch policy; an authored empty array disables bounce.
        public StorySpeakerBounceScene[] speakerBounceScenes;
        // Portraits are independent UI cards, not a claim that actors were separated from the CG.
        public StorySpeakerPortraitAsset[] speakerPortraits;
        // null permits mapped portraits in every scene; [] hides them everywhere.
        public string[] speakerPortraitScenes;
    }
    [Serializable] public sealed class StorySpeakerBounceScene
    {
        public string sceneId;
        public float strength = 1f;
    }
    [Serializable] public sealed class StorySpeakerPortraitAsset
    {
        public string speaker, assetId;
        public bool independentLayer;
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
        private readonly Dictionary<string, StorySpeakerBounceScene> bounceScenes;
        private readonly Dictionary<string, StorySpeakerPortraitAsset> portraits;
        public StoryPresentationIndex(StoryPresentationPackage package, StoryGraph graph)
        {
            var errors = Validate(package, graph);
            if (errors.Count != 0) throw new InvalidOperationException("Presentation data:\n" + string.Join("\n", errors));
            Package = package;
            scenes = (package.scenes ?? Array.Empty<StoryLayeredScene>()).ToDictionary(x => x.sceneId, StringComparer.Ordinal);
            shots = (package.shots ?? Array.Empty<StoryPresentationShot>()).ToDictionary(x => x.nodeId, StringComparer.Ordinal);
            bounceScenes = package.speakerBounceScenes == null ? null : package.speakerBounceScenes.ToDictionary(x => x.sceneId, StringComparer.Ordinal);
            portraits = (package.speakerPortraits ?? Array.Empty<StorySpeakerPortraitAsset>()).ToDictionary(x => x.speaker, StringComparer.Ordinal);
        }
        public static bool AllowsBounce(string sceneId) { return sceneId != null && BounceScenes.Contains(sceneId); }
        public bool AllowsSpeakerBounce(string sceneId)
        {
            return sceneId != null && (bounceScenes == null ? AllowsBounce(sceneId) : bounceScenes.ContainsKey(sceneId));
        }
        public float BounceStrength(string sceneId)
        {
            if (!AllowsSpeakerBounce(sceneId)) return 0;
            float authored = bounceScenes == null ? 1f : bounceScenes[sceneId].strength;
            return authored * (Scene(sceneId)?.bounceStrength ?? 1f);
        }
        public StorySpeakerPortraitAsset Portrait(string speaker)
        {
            return speaker != null && portraits.TryGetValue(speaker, out var value) ? value : null;
        }
        public bool AllowsPortrait(string sceneId)
        {
            return sceneId != null && (Package.speakerPortraitScenes == null || Array.IndexOf(Package.speakerPortraitScenes, sceneId) >= 0);
        }
        public StoryLayeredScene Scene(string sceneId) { return sceneId != null && scenes.TryGetValue(sceneId, out var value) ? value : null; }
        public StoryPresentationShot Shot(string nodeId) { return nodeId != null && shots.TryGetValue(nodeId, out var value) ? value : null; }
        public bool ShouldBounce(StoryNode node)
        {
            return node != null && node.kind == "line" && BounceStrength(node.sceneId) > 0 && (Shot(node.id) == null || Shot(node.id).bounce);
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
            var authoredBounceScenes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var config in p.speakerBounceScenes ?? Array.Empty<StorySpeakerBounceScene>())
            {
                if (config == null || string.IsNullOrWhiteSpace(config.sceneId) || !actualScenes.Contains(config.sceneId) || !authoredBounceScenes.Add(config.sceneId)) { errors.Add("Unknown or duplicate speaker bounce scene."); continue; }
                if (!StoryGraph.Finite(config.strength) || config.strength < 0 || config.strength > 1) errors.Add(config.sceneId + ": speaker bounce strength must be0..1.");
            }
            var portraitSpeakers = new HashSet<string>(StringComparer.Ordinal);
            var portraitScenes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sceneId in p.speakerPortraitScenes ?? Array.Empty<string>())
                if (string.IsNullOrWhiteSpace(sceneId) || !actualScenes.Contains(sceneId) || !portraitScenes.Add(sceneId)) errors.Add("Unknown or duplicate speaker portrait scene.");
            var actualSpeakers = new HashSet<string>(graph.Package.nodes.Where(n => n.kind == "line" && !string.IsNullOrWhiteSpace(n.speaker)).Select(n => n.speaker), StringComparer.Ordinal);
            foreach (var portrait in p.speakerPortraits ?? Array.Empty<StorySpeakerPortraitAsset>())
            {
                if (portrait == null || string.IsNullOrWhiteSpace(portrait.speaker) || !actualSpeakers.Contains(portrait.speaker) || !portraitSpeakers.Add(portrait.speaker)) { errors.Add("Unknown or duplicate portrait speaker."); continue; }
                var asset = graph.Asset(portrait.assetId);
                if (asset == null || (asset.kind != "actor" && asset.kind != "portrait")) errors.Add(portrait.speaker + ": portrait asset must have kind actor or portrait.");
                if (!portrait.independentLayer) errors.Add(portrait.speaker + ": portrait requires an independent UI layer.");
            }
            foreach (var scene in p.scenes ?? Array.Empty<StoryLayeredScene>())
            {
                if (scene == null || string.IsNullOrWhiteSpace(scene.sceneId) || !actualScenes.Contains(scene.sceneId) || sceneMap.ContainsKey(scene.sceneId)) { errors.Add("Unknown or duplicate layered scene."); continue; }
                sceneMap.Add(scene.sceneId, scene);
                if (!StoryGraph.Finite(scene.bounceStrength) || scene.bounceStrength < 0 || scene.bounceStrength > 1) errors.Add(scene.sceneId + ": bounceStrength must be0..1.");
                if (p.speakerBounceScenes == null ? !AllowsBounce(scene.sceneId) : !authoredBounceScenes.Contains(scene.sceneId)) errors.Add(scene.sceneId + (p.speakerBounceScenes == null ? ": only the seven requested scenes can use separate animated actors." : ": layered scene must be listed in speakerBounceScenes."));
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
