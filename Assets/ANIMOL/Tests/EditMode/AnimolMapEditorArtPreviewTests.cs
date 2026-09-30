using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Tests.EditMode
{
    public sealed class AnimolMapEditorArtPreviewTests
    {
        [TestCase("CLOUD_SHEEP_STEP")]
        [TestCase("PAGE_BRIDGE")]
        [TestCase("LIB_POPUP_STAIR")]
        [TestCase("DEW_SEED_STEP")]
        public void OperationalTypePreviewUsesSpriteFromBoundAnimolArt(string id)
        {
            var type = StageMapScenePalette.FindType(id);
            var sprite = StageMapScenePalette.ResolvePreviewSprite(type);
            Assert.That(type, Is.Not.Null);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(sprite), Does.StartWith("Assets/ANIMOL/Atlases/ANIMOL_Master_Animations_32.png"));
            Assert.That(sprite, Is.Not.EqualTo(type.GhostSprite));
        }

        [Test]
        public void EveryDevPlayableTypeHasAnEditorPreviewSprite()
        {
            var registry = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>("Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset");
            foreach (var type in registry.Types)
                if (type.ImplementationLevel == MapObjectImplementationLevel.DevPlayable)
                    Assert.That(StageMapScenePalette.ResolvePreviewSprite(type), Is.Not.Null, type.StableTypeId);
        }

        [Test]
        public void EveryDevPlayableTypeHasCompactFocusedCardDescription()
        {
            var registry = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>("Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset");
            foreach (var type in registry.Types)
            {
                if (type.ImplementationLevel != MapObjectImplementationLevel.DevPlayable) continue;
                var description = StageMapScenePalette.BriefDescription(type, 100);
                Assert.That(description, Is.Not.Empty, type.StableTypeId);
                Assert.That(description.Length, Is.LessThanOrEqualTo(100), type.StableTypeId);
            }
        }

        [Test]
        public void SelectingPaletteTypeClearsPlacedSelectionForReadyToPlaceCard()
        {
            var window = ScriptableObject.CreateInstance<CampaignMapEditorWindow>();
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(CampaignMapEditorWindow).GetField("selectedObjectStableId", flags)?.SetValue(window, "OLD-PLACEMENT");
                typeof(CampaignMapEditorWindow).GetField("selectedMarkerStableId", flags)?.SetValue(window, "OLD-MARKER");
                typeof(CampaignMapEditorWindow).GetMethod("SelectLogicObjectType", flags)?.Invoke(window, new object[] { 3 });
                Assert.That(typeof(CampaignMapEditorWindow).GetField("selectedObjectStableId", flags)?.GetValue(window), Is.EqualTo(string.Empty));
                Assert.That(typeof(CampaignMapEditorWindow).GetField("selectedMarkerStableId", flags)?.GetValue(window), Is.EqualTo(string.Empty));
                Assert.That(typeof(CampaignMapEditorWindow).GetField("objectTypeIndex", flags)?.GetValue(window), Is.EqualTo(3));
            }
            finally { Object.DestroyImmediate(window); }
        }

        [Test]
        public void JadeBalancePairCanBePlacedWithMutualStableLinks()
        {
            const string path = "Assets/ANIMOL/Data/Development/M9BThemePlatforms/__JADE_PAIR_TEST.asset";
            AssetDatabase.DeleteAsset(path);
            var map = ScriptableObject.CreateInstance<StageMapDefinition>();
            map.EditorInitializeIdentity("JADE-PAIR-TEST", "T01");
            map.EditorTrySetChunkBounds(new RectInt(0, 0, 1, 1), true, out _);
            AssetDatabase.CreateAsset(map, path);
            try
            {
                var type = StageMapScenePalette.FindType("MOON_JADE_BALANCE");
                Assert.That(StageMapObjectAuthoringOperations.PlaceLinkedPair(map, type, new Vector2Int(2, 4), new Vector2Int(5, 4), out var message), Is.True, message);
                Assert.That(map.Objects.Count, Is.EqualTo(2));
                Assert.That(map.Objects[0].Settings.LinkedInstanceIds[0], Is.EqualTo(map.Objects[1].StableId));
                Assert.That(map.Objects[1].Settings.LinkedInstanceIds[0], Is.EqualTo(map.Objects[0].StableId));
            }
            finally { AssetDatabase.DeleteAsset(path); AssetDatabase.DeleteAsset("Assets/ANIMOL/MapBackups/JADE-PAIR-TEST"); }
        }

        [TestCase("MOON_JADE_BALANCE")]
        [TestCase("PAGE_BRIDGE")]
        [TestCase("DEW_SEED_STEP")]
        public void MapPreviewScalePreservesPixelAspectAndFitsFootprint(string id)
        {
            var type = StageMapScenePalette.FindType(id);
            var sprite = StageMapScenePalette.ResolvePreviewSprite(type);
            var scale = StageMapScenePalette.CalculateAspectFitScale(sprite, type.FootprintCells, 1f);
            Assert.That(scale.x, Is.EqualTo(scale.y).Within(.0001f));
            Assert.That(sprite.bounds.size.x * scale.x, Is.LessThanOrEqualTo(type.FootprintCells.x + .001f));
            Assert.That(sprite.bounds.size.y * scale.y, Is.LessThanOrEqualTo(type.FootprintCells.y + .001f));
        }

        [Test]
        public void GeneratedTerrainCatalogContainsSixteenTilesPerTheme()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StageTerrainTileCatalog>("Assets/ANIMOL/Resources/ANIMOL_TerrainTileCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Entries.Count, Is.EqualTo(80));
            foreach (var themeId in new[] { "T01", "T02", "T03", "T04", "T05" })
            {
                Assert.That(StageMapScenePalette.GetThemeTerrainTiles(themeId).Length, Is.EqualTo(16), themeId);
                Assert.That(catalog.FindThemeDefault(themeId), Is.Not.Null, themeId);
            }
        }

        [Test]
        public void LegacyTerrainCellUsesCurrentThemeDefaultTileAtRuntime()
        {
            var root = new GameObject("ThemeTileRuntimeTest", typeof(Grid), typeof(StageMapRuntimeLoader));
            var terrainObject = new GameObject("Terrain", typeof(Tilemap), typeof(TilemapRenderer));
            terrainObject.transform.SetParent(root.transform, false);
            var map = ScriptableObject.CreateInstance<StageMapDefinition>();
            try
            {
                map.EditorInitializeIdentity("THEME-TILE-TEST", "T04");
                map.EditorTrySetChunkBounds(new RectInt(0, 0, 1, 1), true, out _);
                map.EditorSetCell(0, 0, StageMapLayer.Terrain, "LEGACY_PLACEHOLDER_TILE");
                map.EditorInitializeVariableChunksFromAuthoredContent(1f);
                var loader = root.GetComponent<StageMapRuntimeLoader>();
                typeof(StageMapRuntimeLoader).GetField("terrain", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(loader, terrainObject.GetComponent<Tilemap>());
                loader.Load(map);
                var catalog = Resources.Load<StageTerrainTileCatalog>("ANIMOL_TerrainTileCatalog");
                Assert.That(terrainObject.GetComponent<Tilemap>().GetTile(Vector3Int.zero), Is.EqualTo(catalog.FindThemeDefault("T04")));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(map);
            }
        }

    }
}
