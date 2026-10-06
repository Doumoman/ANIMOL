using System;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.Editor
{
    public enum StageMapAuthoringProxyRole { Cell, Object, PathNode }

    public sealed class StageMapAuthoringProxy : MonoBehaviour
    {
        [SerializeField] private StageMapDefinition map;
        [SerializeField] private StageMapAuthoringProxyRole role;
        [SerializeField] private StageMapLayer layer;
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private int pathNodeIndex = -1;
        [SerializeField] private Vector2Int authoredCell;
        [SerializeField] private Vector2 authoredSize = Vector2.one;

        public StageMapDefinition Map => map;
        public StageMapAuthoringProxyRole Role => role;
        public StageMapLayer Layer => layer;
        public string StableId => stableId;
        public int PathNodeIndex => pathNodeIndex;
        public Vector2Int AuthoredCell => authoredCell;
        public Vector2 AuthoredSize => authoredSize;

        public void EditorConfigure(StageMapDefinition sourceMap, StageMapAuthoringProxyRole proxyRole,
            StageMapLayer sourceLayer, string objectStableId, int nodeIndex, Vector2Int cell)
        {
            map = sourceMap;
            role = proxyRole;
            layer = sourceLayer;
            stableId = objectStableId ?? string.Empty;
            pathNodeIndex = nodeIndex;
            authoredCell = cell;
            authoredSize = Vector2.one;
        }

        public void EditorSetAuthoredSize(Vector2 size) => authoredSize = size;
    }

    // Compatibility facade for old scene assets and callers. No update or selection hooks.
    public static class StageMapAuthoringWorkspace
    {
        public const string ScenePath = "Assets/ANIMOL/Scenes/MapAuthoringWorkspace.unity";
        public static StageMapDefinition CurrentMap => TerrainEditorSceneEditor.Active ? TerrainEditorSceneEditor.Adapter.Map : null;
        public static bool IsOpen => TerrainEditorSceneEditor.Active;
        public static bool IsOpenFor(StageMapDefinition map) => map != null && CurrentMap == map;
        public static void OpenSelectedMap() => TerrainEditorSceneEditor.OpenSelected();
        public static bool Open(StageMapDefinition map, bool askToSaveCurrentScene)
        {
            if (map == null || EditorApplication.isPlayingOrWillChangePlaymode) return false;
            TerrainEditorSceneEditor.Open(map);
            return TerrainEditorSceneEditor.Active;
        }
        public static void ScheduleRefresh(StageMapDefinition map) { }
        public static void RebuildPreview(StageMapDefinition map) => FocusMap(map);
        public static void FocusMap(StageMapDefinition map) { if (map != null) TerrainEditorSceneEditor.Open(map); }
        public static int ExpectedProxyCount(StageMapDefinition map) => map == null ? 0 :
            map.Cells.Count + map.Objects.Count + map.Objects.Sum(item => item.Settings.PathCells.Count);
    }
}
