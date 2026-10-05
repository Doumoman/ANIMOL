using System;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using Animol.TerrainStructure.Editor;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public static class StageTerrainStructureIntegration
    {
        public const string Root = "Assets/ANIMOL/TerrainStructure";
        public const string RegistryPath = "Assets/ANIMOL/Resources/ANIMOL_TerrainStructureRegistry.asset";

        [MenuItem("ANIMOL/Terrain Structure/Register In Campaign Editor")]
        public static void Initialize()
        {
            AnimolTerrainSourceArtBuilder.Initialize();
            var registry = AssetDatabase.LoadAssetAtPath<StageTerrainStructureRegistry>(RegistryPath);
            if (registry == null)
            { registry = ScriptableObject.CreateInstance<StageTerrainStructureRegistry>(); AssetDatabase.CreateAsset(registry, RegistryPath); }
            registry.catalogJson = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/editor_tile_catalog.json");
            registry.frames = registry.Catalog.entries.Select(e => AssetDatabase.LoadAssetAtPath<AnimolTerrainArtFrameDefinition>(Root + "/Generated/SourceFrames/" + e.frameId + ".asset")).ToArray();
            registry.bindings.Clear();
            var legacy = Resources.Load<StageTerrainTileCatalog>("ANIMOL_TerrainTileCatalog");
            if (legacy == null) throw new InvalidOperationException("Existing terrain tile registry is missing.");
            foreach (var entry in legacy.Entries)
            {
                AddBinding(registry, entry.ThemeId, entry.TileId, "", entry.ThemeId + "_A");
                AddBinding(registry, entry.ThemeId, entry.TileId, entry.TileId, entry.ThemeId + "_A");
                foreach (TerrainTileTopology topology in Enum.GetValues(typeof(TerrainTileTopology)))
                {
                    AddBinding(registry, entry.ThemeId, entry.TileId, StageMapScenePalette.ComposeVariantId("ANIMOL", topology), entry.ThemeId + "_A");
                    AddBinding(registry, entry.ThemeId, entry.TileId, StageMapScenePalette.ComposeVariantId("BASE", topology), entry.ThemeId + "_A");
                }
            }
            foreach (var theme in new[] { "T01", "T02", "T03", "T04", "T05" })
            {
                // Explicit compatibility bindings. Existing art/variants are retained verbatim.
                foreach (var id in new[] { "DEV_TERRAIN_PLACEHOLDER", "M9_MOON_SOLID_16PX_PLACEHOLDER", "M9_MOON_ONE_WAY_16PX_PLACEHOLDER" })
                {
                    if (id.StartsWith("M9_") && theme != "T01") continue;
                    AddBinding(registry, theme, id, "", theme + "_A");
                    foreach (TerrainTileTopology topology in Enum.GetValues(typeof(TerrainTileTopology)))
                        AddBinding(registry, theme, id, StageMapScenePalette.ComposeVariantId("BASE", topology), theme + "_A");
                }
                foreach (var style in new[] { "A", "B", "C", "D" })
                    AddBinding(registry, theme, StageMapScenePalette.ThemeTerrainFolder(theme) + "_C", theme + "_" + style + "@CENTER", theme + "_" + style, true);
            }
            EditorUtility.SetDirty(registry); AssetDatabase.SaveAssetIfDirty(registry);
            Debug.Log("ANIMOL terrain v3 registered: 60 frames, 40 structures, 20 overlays; explicit legacy tile/variant bindings.");
        }

        private static void AddBinding(StageTerrainStructureRegistry registry, string theme, string tile, string variant, string style, bool preferred = false)
        {
            registry.bindings.Add(new StageTerrainStyleBinding { themeId = theme, tileId = tile, variantId = variant, styleId = style, preferred = preferred });
        }

        public static void Validate(StageMapDefinition map, AnimolTerrainSavedMap candidate)
        {
            var registry = StageTerrainStructureRegistry.Load();
            map.ValidateTerrainCandidate(registry, candidate);
            // Exercise existing host footprint/path/overlap policy on a disposable candidate map.
            var scratch = UnityEngine.Object.Instantiate(map);
            try
            {
                scratch.EditorApplyTerrainCandidate(registry, candidate);
                scratch.ResolveTerrain(registry);
                foreach (var item in scratch.Objects)
                {
                    var before = StageMapObjectAuthoringOperations.ValidatePlacement(map, map.Objects.First(o => o.StableId == item.StableId), item.StableId);
                    var after = StageMapObjectAuthoringOperations.ValidatePlacement(scratch, item, item.StableId);
                    var newErrors = after.Errors.Except(before.Errors).ToArray();
                    if (newErrors.Length > 0) throw new InvalidOperationException(string.Join(" | ", newErrors));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(scratch); }
        }

        public static void Write(StageMapDefinition map, AnimolTerrainSavedMap candidate)
        {
            Validate(map, candidate);
            var registry = StageTerrainStructureRegistry.Load();
            var snapshot = EditorJsonUtility.ToJson(map);
            // Import the before-change backup and saved map in one pass. Keep this
            // synchronous scope inside the transaction; never pause imports across frames.
            AssetDatabase.StartAssetEditing();
            try
            {
                if (AssetDatabase.Contains(map)) StageMapBackupService.CreateBackup(map, "autosave-before-terrain-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
                Undo.RecordObject(map, "Edit ANIMOL Terrain Structure");
                try
                {
                    map.EditorApplyTerrainCandidate(registry, candidate);
                    EditorUtility.SetDirty(map);
                    AssetDatabase.SaveAssetIfDirty(map);
                }
                catch
                {
                    EditorJsonUtility.FromJsonOverwrite(snapshot, map);
                    EditorUtility.SetDirty(map); AssetDatabase.SaveAssetIfDirty(map);
                    Refresh(map); throw;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
        }

        public static void Refresh(StageMapDefinition map)
        {
            StageMapAuthoringWorkspace.ScheduleRefresh(map);
            SceneView.RepaintAll();
        }
    }
}
