using System;
using UnityEngine;

namespace William.NotebookUI
{
    [Serializable] public sealed class NotebookTheme
    {
        public string id, name, productId;
        public bool paid;
        public int chapter, startY, height;
    }
    [Serializable] public sealed class NotebookStage
    {
        public string id, themeId, label, sceneName, artworkId;
        public int order, wireVertexIndex;
        public float x, y;
    }
    [Serializable] public sealed class NotebookMapPoint
    {
        public float x, y;
    }
    [Serializable] public sealed class NotebookWireVertex
    {
        public float u, v, x, y;
        public string stageId;
    }
    [Serializable] public sealed class NotebookEdge
    {
        public string from, to, sprite, kind, artworkId;
        public float x, y, width, height;
        public NotebookMapPoint[] points = Array.Empty<NotebookMapPoint>();
    }
    /// <summary>Authoring metadata for a static illustration behind the playable stars.</summary>
    [Serializable] public sealed class NotebookConstellationArtwork
    {
        public string id, themeId, sprite, wireSprite;
        public float x, y, width, height;
        public float sourceWidth, sourceHeight, renderX, renderY, renderWidth, renderHeight;
        public NotebookWireVertex[] wireVertices = Array.Empty<NotebookWireVertex>();
        public int[] wirePath = Array.Empty<int>(), stageRouteIndices = Array.Empty<int>();
        public string[] stageIds = Array.Empty<string>();
    }
    [Serializable] public sealed class NotebookCatalog
    {
        public int referenceWidth = 1080, referenceHeight = 1920, chapterHeight = 1180, mapContentHeight = 4720;
        public int stageEntryCost = 1, telescopeCapacity = 10, recoverySeconds = 180, rewardedAdAmount = 5;
        public bool stageLayoutIsExample, stageBindingsPending, requiredStagePolicyIsExample;
        public string[] battleRequiredStageIds = Array.Empty<string>();
        public NotebookTheme[] themes = Array.Empty<NotebookTheme>();
        public NotebookStage[] stages = Array.Empty<NotebookStage>();
        public NotebookEdge[] edges = Array.Empty<NotebookEdge>();
        public NotebookConstellationArtwork[] artworks = Array.Empty<NotebookConstellationArtwork>();
        public NotebookStage FindStage(string id) => Array.Find(stages, item => item.id == id);
        public NotebookTheme FindTheme(string id) => Array.Find(themes, item => item.id == id);
        public static NotebookCatalog Parse(TextAsset asset)
        {
            if (!asset) throw new ArgumentNullException(nameof(asset));
            NotebookCatalog result = JsonUtility.FromJson<NotebookCatalog>(asset.text);
            if (result == null || result.telescopeCapacity <= 0 || result.recoverySeconds <= 0 || result.stageEntryCost < 0)
                throw new InvalidOperationException("Invalid Notebook UI catalog.");
            return result;
        }
    }
}
