using System;
using UnityEngine;

namespace William.NotebookUI
{
    // Each Resources asset is a separate load boundary. Never reference these
    // containers from the lobby prefab or the lightweight campaign index.
    public sealed class NotebookDeferredAssets : ScriptableObject
    {
        public const string StarwayResource = "NotebookStarwayAssets";
        public const string TheaterResource = "NotebookTheaterAssets";
        public const string StoryResource = "NotebookStoryAssets";
        public ANIMOL.Puzzle.PuzzleMap[] maps = Array.Empty<ANIMOL.Puzzle.PuzzleMap>();
        public ANIMOL.Puzzle.EchoTheaterMapSet theaterMaps;
        public GameObject storyPrefab;

        public static NotebookDeferredAssets Load(string resource) => Resources.Load<NotebookDeferredAssets>(resource);
    }
}
