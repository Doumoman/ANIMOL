using System;
using System.Collections.Generic;

namespace ANIMOL.SanctuaryStory
{
    // JsonUtility serializes fields, so these DTOs intentionally use public fields.
    [Serializable]
    public sealed class SanctuaryChapter
    {
        public int schemaVersion;
        public string chapterId;
        public string initialSceneId;
        public string displayNameBefore;
        public string displayNameAfter;
        public SanctuaryCharacter[] characters;
        public SanctuaryScene[] scenes;
    }

    [Serializable]
    public sealed class SanctuaryCharacter
    {
        public string id;
        public string displayName;
        public string personality;
        public string voiceDirection;
    }

    [Serializable]
    public sealed class SanctuaryScene
    {
        public string id;
        public string title;
        public string nextSceneId;
        public int loopStage;
        public string artReference;
        public SanctuaryStep[] steps;
    }

    [Serializable]
    public sealed class SanctuaryStep
    {
        public string id;
        public string kind;
        public SanctuaryLine line;
        public SanctuaryCue cue;
        public SanctuaryChoiceSet choice;
    }

    [Serializable]
    public sealed class SanctuaryLine
    {
        public string id;
        public string speaker;
        public string text;
        // Production/acting direction only; the player UI must not display it.
        public string delivery;
    }

    [Serializable]
    public sealed class SanctuaryCue
    {
        public string id;
        public float duration;
        public string intensity;
        // Production note only, not a subtitle or a fallback visual effect.
        public string description;
    }

    [Serializable]
    public sealed class SanctuaryChoiceSet
    {
        public string mode;
        public float hesitationSeconds;
        public float revealSeconds;
        public SanctuaryOption[] options;
    }

    [Serializable]
    public sealed class SanctuaryOption
    {
        public string id;
        public string label;
        public string action;
    }

    [Serializable]
    public sealed class SanctuaryCheckpoint
    {
        public int schemaVersion;
        public string chapterId;
        public string sceneId;
        public bool completed;
    }

    /// <summary>Validates the portable data, independently of Unity and project services.</summary>
    public static class SanctuaryChapterValidator
    {
        public static void Validate(SanctuaryChapter chapter)
        {
            Require(chapter != null, "Chapter is null.");
            Require(chapter.schemaVersion == 1, "Unsupported schemaVersion; expected 1.");
            Text(chapter.chapterId, "chapterId");
            Text(chapter.initialSceneId, "initialSceneId");
            Text(chapter.displayNameBefore, "displayNameBefore");
            Text(chapter.displayNameAfter, "displayNameAfter");
            Require(chapter.characters != null && chapter.characters.Length > 0, "characters is empty.");
            Require(chapter.scenes != null && chapter.scenes.Length > 0, "scenes is empty.");

            var characterIds = new HashSet<string>(StringComparer.Ordinal);
            var characterNames = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < chapter.characters.Length; i++)
            {
                var character = chapter.characters[i];
                Require(character != null, "characters[" + i + "] is null.");
                Text(character.id, "characters[" + i + "].id");
                Text(character.displayName, "characters[" + i + "].displayName");
                Require(characterIds.Add(character.id), "Duplicate character id: " + character.id);
                Require(characterNames.Add(character.displayName), "Duplicate character name: " + character.displayName);
            }

            var sceneMap = new Dictionary<string, SanctuaryScene>(StringComparer.Ordinal);
            var stepIds = new HashSet<string>(StringComparer.Ordinal);
            var lineIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < chapter.scenes.Length; i++)
            {
                var scene = chapter.scenes[i];
                Require(scene != null, "scenes[" + i + "] is null.");
                Text(scene.id, "scenes[" + i + "].id");
                Text(scene.title, scene.id + ".title");
                Require(!sceneMap.ContainsKey(scene.id), "Duplicate scene id: " + scene.id);
                sceneMap.Add(scene.id, scene);
                Require(scene.loopStage >= 0 && scene.loopStage <= 3, scene.id + ": loopStage must be 0..3.");
                Require(scene.steps != null && scene.steps.Length > 0, scene.id + ": steps is empty.");
                for (int s = 0; s < scene.steps.Length; s++)
                {
                    var step = scene.steps[s];
                    string path = scene.id + ".steps[" + s + "]";
                    Require(step != null, path + " is null.");
                    Text(step.id, path + ".id");
                    Require(stepIds.Add(step.id), "Duplicate step id: " + step.id);
                    // Unity JsonUtility instantiates missing serializable class fields as empty
                    // objects. Count populated payloads rather than non-null references.
                    int payloads = (step.line != null && !string.IsNullOrEmpty(step.line.id) ? 1 : 0) +
                                   (step.cue != null && !string.IsNullOrEmpty(step.cue.id) ? 1 : 0) +
                                   (step.choice != null && !string.IsNullOrEmpty(step.choice.mode) ? 1 : 0);
                    Require(payloads == 1, path + ": exactly one line/cue/choice payload is required.");
                    switch (step.kind)
                    {
                        case "line":
                            Require(step.line != null, path + ": kind is line but payload is not.");
                            Text(step.line.id, path + ".line.id");
                            Require(lineIds.Add(step.line.id), "Duplicate line id: " + step.line.id);
                            Text(step.line.speaker, path + ".line.speaker");
                            Require(characterIds.Contains(step.line.speaker) || characterNames.Contains(step.line.speaker),
                                path + ": unknown speaker " + step.line.speaker);
                            Text(step.line.text, path + ".line.text");
                            break;
                        case "cue":
                            Require(step.cue != null, path + ": kind is cue but payload is not.");
                            Text(step.cue.id, path + ".cue.id");
                            Time(step.cue.duration, path + ".cue.duration");
                            Require(step.cue.intensity == "normal" || step.cue.intensity == "important" || step.cue.intensity == "climax",
                                path + ": unsupported intensity.");
                            break;
                        case "choice":
                            Require(step.choice != null, path + ": kind is choice but payload is not.");
                            ValidateChoice(step.choice, path);
                            break;
                        default:
                            throw new ArgumentException(path + ": unsupported kind '" + step.kind + "'.");
                    }
                }
            }

            Require(sceneMap.ContainsKey(chapter.initialSceneId), "initialSceneId does not exist: " + chapter.initialSceneId);
            foreach (var scene in chapter.scenes)
                Require(string.IsNullOrEmpty(scene.nextSceneId) || sceneMap.ContainsKey(scene.nextSceneId),
                    scene.id + ": nextSceneId does not exist: " + scene.nextSceneId);

            // A fixed story route has one acyclic next-scene chain. Hesitation is local to a choice.
            var visited = new HashSet<string>(StringComparer.Ordinal);
            string cursor = chapter.initialSceneId;
            while (!string.IsNullOrEmpty(cursor))
            {
                Require(visited.Add(cursor), "Scene graph contains a cycle at " + cursor + ".");
                cursor = sceneMap[cursor].nextSceneId;
            }
            Require(visited.Count == chapter.scenes.Length, "One or more scenes are unreachable from initialSceneId.");
        }

        public static void ValidateCheckpoint(SanctuaryChapter chapter, SanctuaryCheckpoint checkpoint)
        {
            Validate(chapter);
            Require(checkpoint != null, "Checkpoint is null.");
            Require(checkpoint.schemaVersion == chapter.schemaVersion, "Checkpoint schemaVersion does not match chapter.");
            Require(string.Equals(checkpoint.chapterId, chapter.chapterId, StringComparison.Ordinal), "Checkpoint belongs to another chapter.");
            SanctuaryScene found = null;
            foreach (var scene in chapter.scenes)
                if (string.Equals(scene.id, checkpoint.sceneId, StringComparison.Ordinal)) found = scene;
            Require(found != null, "Checkpoint scene does not exist: " + checkpoint.sceneId);
            Require(!checkpoint.completed || string.IsNullOrEmpty(found.nextSceneId), "A completed checkpoint must refer to the final scene.");
        }

        private static void ValidateChoice(SanctuaryChoiceSet choice, string path)
        {
            Require(choice.mode == "normal" || choice.mode == "forced", path + ": unsupported choice mode.");
            Time(choice.hesitationSeconds, path + ".hesitationSeconds");
            Time(choice.revealSeconds, path + ".revealSeconds");
            Require(choice.options != null && choice.options.Length > 0, path + ": options is empty.");
            var optionIds = new HashSet<string>(StringComparer.Ordinal);
            int climb = 0, hesitate = 0;
            foreach (var option in choice.options)
            {
                Require(option != null, path + ": null option.");
                Text(option.id, path + ".option.id");
                Text(option.label, path + ".option.label");
                Require(optionIds.Add(option.id), path + ": duplicate option id " + option.id);
                if (option.action == "climb") climb++;
                else if (option.action == "hesitate") hesitate++;
                else throw new ArgumentException(path + ": unsupported option action '" + option.action + "'.");
            }
            Require(climb > 0, path + ": a climb option is required.");
            if (choice.mode == "forced") Require(hesitate == 0, path + ": forced choices may only climb.");
            else
            {
                Require(hesitate > 0, path + ": normal choices require a hesitation option.");
                Require(choice.revealSeconds <= choice.hesitationSeconds, path + ": reveal exceeds total hesitation time.");
            }
        }

        private static void Text(string value, string path) { Require(!string.IsNullOrWhiteSpace(value), path + " is empty."); }
        private static void Time(float value, string path) { Require(!float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f, path + " must be finite and nonnegative."); }
        private static void Require(bool condition, string message) { if (!condition) throw new ArgumentException(message); }
    }
}
