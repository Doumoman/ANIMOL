using System;
using System.Collections.Generic;

namespace ANIMOL.SanctuaryStory
{
    [Serializable]
    public sealed class SanctuaryPresentationData
    {
        public int schemaVersion;
        public string chapterId;
        public SanctuaryScenePresentation[] scenes;
        public SanctuaryLinePresentation[] lines;

        public SanctuaryScenePresentation FindScene(string id)
        {
            if (scenes != null) foreach (SanctuaryScenePresentation scene in scenes)
                if (scene != null && scene.sceneId == id) return scene;
            return null;
        }

        public SanctuaryLinePresentation FindLine(string id)
        {
            if (lines != null) foreach (SanctuaryLinePresentation line in lines)
                if (line != null && line.lineId == id) return line;
            return null;
        }
    }

    [Serializable]
    public sealed class SanctuaryScenePresentation
    {
        public string sceneId, backgroundId;
        public float backgroundScale = 1f, backgroundX, backgroundY;
        public SanctuaryActorPresentation[] actors;
    }

    [Serializable]
    public sealed class SanctuaryActorPresentation
    {
        public string actorId, variantId;
        public float x, feetY = .4f, scale = 1f;
        public bool visible, flipped;
    }

    [Serializable]
    public sealed class SanctuaryLinePresentation
    {
        public string lineId, shot, emotion;
        public SanctuaryActorState[] actors;
    }

    [Serializable]
    public sealed class SanctuaryActorState
    {
        public string actorId, variantId;
        public bool visible;
    }

    /// <summary>Validates authored presentation independently of flow and save ownership.</summary>
    public static class SanctuaryPresentationValidator
    {
        public static void Validate(SanctuaryPresentationData data, SanctuaryChapter chapter, SanctuaryVisualCatalog catalog)
        {
            Require(data != null && data.schemaVersion == 1, "Presentation schemaVersion must be 1.");
            Require(chapter != null && data.chapterId == chapter.chapterId, "Presentation chapterId differs from the chapter.");
            Require(data.scenes != null && data.scenes.Length == chapter.scenes.Length, "Presentation must cover every scene exactly once.");
            Require(data.lines != null, "Presentation lines are missing.");
            var actorIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (SanctuaryCharacter actor in chapter.characters) actorIds.Add(actor.id);
            var sceneIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (SanctuaryScenePresentation scene in data.scenes)
            {
                Require(scene != null && !string.IsNullOrEmpty(scene.sceneId) && sceneIds.Add(scene.sceneId), "Presentation scene ID is empty or duplicated.");
                bool found = false;
                foreach (SanctuaryScene authored in chapter.scenes) if (authored.id == scene.sceneId) { found = true; break; }
                Require(found, "Unknown presentation scene " + scene.sceneId);
                Require(!string.IsNullOrEmpty(scene.backgroundId), scene.sceneId + ": backgroundId is missing.");
                Require(Finite(scene.backgroundScale) && scene.backgroundScale >= 1f && scene.backgroundScale <= 1.14f,
                    scene.sceneId + ": backgroundScale must be 1..1.14.");
                Require(Finite(scene.backgroundX) && Finite(scene.backgroundY) && Math.Abs(scene.backgroundX) <= 75f && Math.Abs(scene.backgroundY) <= 110f,
                    scene.sceneId + ": background offset is outside the bounded composition.");
                Require(scene.actors != null && scene.actors.Length == actorIds.Count, scene.sceneId + ": include all five scene actors.");
                var included = new HashSet<string>(StringComparer.Ordinal);
                foreach (SanctuaryActorPresentation actor in scene.actors)
                {
                    Require(actor != null && actorIds.Contains(actor.actorId) && included.Add(actor.actorId), scene.sceneId + ": invalid or repeated actor.");
                    Require(Finite(actor.x) && actor.x >= -.43f && actor.x <= .43f && Finite(actor.feetY) && actor.feetY >= .35f && actor.feetY <= .52f,
                        scene.sceneId + ": actor placement would leave the portrait composition.");
                    Require(Finite(actor.scale) && actor.scale >= .75f && actor.scale <= 1.12f, scene.sceneId + ": actor scale must be .75..1.12.");
                    ValidateVariant(catalog, actor.actorId, actor.variantId, scene.sceneId);
                }
                if (catalog != null) Require(catalog.FindBackground(scene.backgroundId) != null, "Unbound background " + scene.backgroundId);
            }
            var authoredLines = new Dictionary<string, SanctuaryLine>(StringComparer.Ordinal);
            foreach (SanctuaryScene scene in chapter.scenes) foreach (SanctuaryStep step in scene.steps)
                if (step.kind == "line") authoredLines.Add(step.line.id, step.line);
            Require(data.lines.Length == authoredLines.Count, "Presentation must cover every utterance exactly once.");
            var lineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (SanctuaryLinePresentation line in data.lines)
            {
                Require(line != null && !string.IsNullOrEmpty(line.lineId) && lineIds.Add(line.lineId) && authoredLines.ContainsKey(line.lineId),
                    "Presentation line ID is unknown or duplicated.");
                Require(line.actors != null && line.actors.Length > 0, line.lineId + ": actor states are missing.");
                var included = new HashSet<string>(StringComparer.Ordinal);
                string speaker = authoredLines[line.lineId].speaker;
                foreach (SanctuaryCharacter actor in chapter.characters)
                    if (speaker == actor.displayName) { speaker = actor.id; break; }
                foreach (SanctuaryActorState state in line.actors)
                {
                    Require(state != null && actorIds.Contains(state.actorId) && included.Add(state.actorId), line.lineId + ": invalid or repeated actor state.");
                    ValidateVariant(catalog, state.actorId, state.variantId, line.lineId);
                }
                Require(included.Contains(speaker), line.lineId + ": include the speaker's explicit expression.");
            }
        }

        private static void ValidateVariant(SanctuaryVisualCatalog catalog, string actorId, string variantId, string context)
        {
            Require(!string.IsNullOrEmpty(variantId), context + ": expression key is empty.");
            if (catalog == null) return;
            SanctuaryCharacterVisual actor = catalog.FindCharacter(actorId);
            Require(actor != null && actor.ResolveExpression(variantId) != null, context + ": unbound expression " + actorId + "/" + variantId);
            SanctuaryExpressionVisual metadata = actor.FindExpression(variantId);
            if (metadata != null && metadata.hasRegistrationX)
                Require(Finite(metadata.registrationX) && metadata.registrationX >= 0f && metadata.registrationX <= 1f,
                    context + ": invalid foot-center registration for " + actorId + "/" + variantId);
            if (metadata != null && metadata.hasContentBounds)
                Require(Finite(metadata.contentMinX) && Finite(metadata.contentMinY) && Finite(metadata.contentMaxX) && Finite(metadata.contentMaxY) &&
                    metadata.contentMinX >= 0f && metadata.contentMinY >= 0f && metadata.contentMaxX <= 1f && metadata.contentMaxY <= 1f &&
                    metadata.contentMaxX > metadata.contentMinX && metadata.contentMaxY > metadata.contentMinY,
                    context + ": invalid alpha-content registration for " + actorId + "/" + variantId);
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static void Require(bool condition, string message) { if (!condition) throw new ArgumentException(message); }
    }
}
