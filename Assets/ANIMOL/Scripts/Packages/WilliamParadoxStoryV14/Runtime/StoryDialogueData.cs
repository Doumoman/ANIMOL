using System;
using UnityEngine.Events;

namespace Animol.Starway
{
    // JsonUtility uses fields and arrays. Keep these names identical to
    // data/ranch_dialogue.json. Dialogue data contains no persistent game state.
    [Serializable]
    public sealed class StoryDialogueData
    {
        public int schemaVersion = 1;
        public string version;
        public string status;
        public string language;
        public StoryDialogueScene[] scenes;
    }

    [Serializable]
    public sealed class StoryDialogueScene
    {
        public string id;
        public string title;
        public string summary;
        public StoryDialogueLine[] lines;
    }

    [Serializable]
    public sealed class StoryDialogueLine
    {
        public string id;
        public string speaker;
        public string text;
        public string shot;
        public string cue;
        public string emotion;
        public string action;
    }

    [Serializable]
    public sealed class StoryStringEvent : UnityEvent<string> { }
}
