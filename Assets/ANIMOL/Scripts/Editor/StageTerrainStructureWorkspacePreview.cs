using System;
using ANIMOL.Gameplay;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    // Follows the existing workspace rebuild without changing its canonical map or physics.
    [InitializeOnLoad]
    public static class StageTerrainStructureWorkspacePreview
    {
        private static GameObject preview;
        private static string signature;
        static StageTerrainStructureWorkspacePreview() { EditorApplication.update += Refresh; }
        private static void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !StageMapAuthoringWorkspace.IsOpen) return;
            var map = StageMapAuthoringWorkspace.CurrentMap;
            var root = GameObject.Find("[MAP PREVIEW - NOT SAVED]");
            if (map == null || root == null) return;
            var next = map.GetInstanceID() + ":" + map.AuthoringRevision;
            if (preview != null && preview.transform.parent == root.transform && signature == next) return;
            if (preview != null) UnityEngine.Object.DestroyImmediate(preview);
            preview = new GameObject("Terrain v3 complete instances"); preview.hideFlags = HideFlags.DontSave;
            preview.transform.SetParent(root.transform, false); signature = next;
            if (!map.HasTerrainStructures) return;
            try
            {
                var registry = StageTerrainStructureRegistry.Load(); var resolved = map.ResolveTerrain(registry);
                foreach (var p in map.TerrainPlacements.placements)
                {
                    var e = resolved.entriesById[p.catalogId];
                    var child = StageTerrainStructureRuntime.CreateArt(preview.transform, p, e, registry.Frame(e.frameId),
                        map.GetEditorPreviewUnitsPerCell(), e.kind == "Structure" ? 5 : 15);
                    child.hideFlags = HideFlags.DontSave;
                    foreach (Transform t in child.transform) t.gameObject.hideFlags = HideFlags.DontSave;
                }
            }
            catch (Exception ex) { Debug.LogError("Terrain workspace: " + ex.Message); }
        }
    }
}
