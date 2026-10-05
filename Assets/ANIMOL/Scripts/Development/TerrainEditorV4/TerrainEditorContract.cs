using System;
using System.Collections.Generic;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using UnityEngine;

namespace ANIMOL.Development
{
    // All AssetDatabase, native Undo and disk operations live behind this Editor-owned adapter.
    public interface ITerrainEditorAdapter : IDisposable
    {
        StageMapDefinition Map { get; }
        StageTerrainStructureRegistry Terrain { get; }
        StageMapObjectTypeRegistry Objects { get; }
        Font Font { get; }
        Sprite ButtonSprite { get; }
        event Action Changed;
        AnimolTerrainSavedMap Read();
        void Validate(AnimolTerrainSavedMap candidate);
        void Commit(AnimolTerrainSavedMap candidate, string label);
        void Save();
        void Undo();
        void Redo();
        void Exit();
        TerrainEditorThumbnail Thumbnail(TerrainEditorPart part);
        StageMapObjectPlacement ObjectCandidate(TerrainEditorObjectEdit edit);
        void ValidateObject(TerrainEditorObjectEdit edit);
        void CommitObject(TerrainEditorObjectEdit edit);
        GameObject PlayerPrefab { get; }
        void ConfigureTestServices(GameObject root, Vector3 spawn);
        void ConfigureTouchControl(DevMobileTouchControl control, MobileTouchAction action);
        IReadOnlyList<TerrainEditorPart> Markers { get; }
        void EditMapConfiguration(string operation);
    }

    public sealed class TerrainEditorThumbnail
    {
        public Texture2D color, silhouette;
        public string diagnostic, sourceHash;
    }
    public sealed class TerrainEditorPart
    {
        public string id, name, theme, style, category;
        public Vector2 size;
        public AnimolTerrainCatalogEntry terrain;
        public StageMapObjectTypeDefinition obj;
        public StageMapObjectKind? marker;
        public bool IsOverlay => terrain?.kind == "InteriorOverlay";
    }
    public sealed class TerrainEditorObjectEdit
    {
        public string operation, definitionId, instanceId;
        public Vector2Int origin, second;
        public StageMapObjectSettings settings;
    }

    [Serializable]
    public sealed class TerrainEditorViewState
    {
        public Vector2 center = new Vector2(8, 5);
        public float zoom = 10;
        public string partId = "", instanceId = "", query = "", theme = "CURRENT", style = "ALL", category = "ALL";
        public string tool = "Place";
        public bool freeShape, freeSelected;
        public string freeStyle="", freeTool="Paint", freePreset="rectangle_5x3";
        public int freeBrush=1;
        public Vector2Int freeSelection;
        public bool grid = true, masks = true, information = false;
        public float paletteHeight = .25f;
    }

    // Kept on the live session across domain reload, so an entry from an existing Play session
    // can restore precisely the roots it suspended (including the previous EventSystem).
    public sealed class TerrainEditorSuspendedRoots : MonoBehaviour
    {
        public List<GameObject> roots = new List<GameObject>();
        public void Restore()
        {
            foreach (var root in roots) if (root != null) root.SetActive(true);
            roots.Clear();
        }
    }
}
