using System;

namespace Animol.Starway
{
    [Serializable]
    public sealed class StoryChronicleData
    {
        public int schemaVersion = 1;
        public string id;
        public float referenceHeight = 1920f;
        public float scrollSpeed = 84f;
        public float holdMultiplier = 3f;
        public float introDuration = 0.6f;
        public float afterAsteranPause = 1f;
        public float rewindDuration = 4.2f;
        public float afterRewritePause = 0.65f;
        public StoryChronicleTitles titles;
        public StoryChronicleRecord[] records;
    }

    [Serializable]
    public sealed class StoryChronicleTitles
    {
        public string asteran;
        public string higher;
    }

    [Serializable]
    public sealed class StoryChronicleRecord
    {
        public string id;
        public string asteran;
        public string higher;
    }
}
