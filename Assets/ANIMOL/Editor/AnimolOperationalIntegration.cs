using System;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Animol.Editor
{
    public static class AnimolOperationalIntegration
    {
        private const string ArtName = "ANIMOL_32px_Art";

        [MenuItem("ANIMOL/Integrate Operational Art")]
        public static void Integrate()
        {
            Bind("Assets/ANIMOL/Prefabs/MapObjects/OBJ_SIDE_SPRING.prefab", "C1", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/OBJ_POUNDER.prefab", "C2", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/OBJ_RICE_SLOW.prefab", "C3", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/TILE_HALF_BLOCK.prefab", "C4", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/TILE_DROP_PLATFORM.prefab", "C5", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/OBJ_RAIL_PLATFORM.prefab", "C6", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_LANTERN_STEP.prefab", "M1", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_JADE_BALANCE.prefab", "M2", AnimolOperationalArtMode.BalancePlate);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_RABBIT_BOWL.prefab", "M3", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_JADE_PENDULUM.prefab", "M4", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_SLIDING_EAVE.prefab", "M5", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_PHASE_STAIR.prefab", "M6", AnimolOperationalArtMode.MoonStair);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/CLOUD_WHALE_FERRY.prefab", "K1", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/CLOUD_BALLOON_TETHER.prefab", "K5", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/INK_BLOT.prefab", "L3", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/LIB_INDEX_DRAWER.prefab", "L4", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/GREEN_SAND_RETRACE.prefab", "H7", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/PRISM_SPRING.prefab", "R2", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MINE_MAGNET_PAIR.prefab", "R4", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MINE_CART_FORK.prefab", "R8", AnimolOperationalArtMode.Whole, true);
            PromoteMineCartDefinition();
            AssetDatabase.SaveAssets();
            Debug.Log("ANIMOL operational art integration complete: C6, M2, M6, R8.");
        }

        [MenuItem("ANIMOL/Integrate Movement Platforms Pass 3")]
        public static void IntegrateMovementPlatformsPass3()
        {
            BindMovement("CLOUD_SAIL_STEP", "K9", typeof(CloudSailStepObject));
            BindMovement("BOOKMARK_LIFT", "L2", typeof(BookmarkLiftObject));
            BindMovement("GLASS_VINE_LIFT", "H2", typeof(GlassVineLiftObject));
            BindMovement("LIB_SPINE_BRAKE", "L7", typeof(LibSpineBrakeObject));
            PromoteDefinition("CLOUD_SAIL_STEP", "CloudSailStep");
            PromoteDefinition("BOOKMARK_LIFT", "BookmarkLift");
            PromoteDefinition("GLASS_VINE_LIFT", "GlassVineLift");
            PromoteDefinition("LIB_SPINE_BRAKE", "LibSpineBrake");
            AddMovementPlatformsToLab();
            AssetDatabase.SaveAssets();
            Debug.Log("ANIMOL pass 3 movement integration complete: K9, L2, H2, L7.");
        }

        private static void BindMovement(string prefabId, string artId, Type runtimeType)
        {
            var path = $"Assets/ANIMOL/Prefabs/MapObjects/M9B/{prefabId}.prefab";
            var generated = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ANIMOL/Generated/Prefabs/{artId}.prefab");
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var prototype = root.GetComponent<PrototypeMapObject>();
                if (prototype != null) UnityEngine.Object.DestroyImmediate(prototype);
                if (root.GetComponent<Rigidbody2D>() == null) root.AddComponent<Rigidbody2D>();
                var collider = root.GetComponent<BoxCollider2D>();
                if (collider == null) collider = root.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(2f, 1f); collider.offset = new Vector2(1f, .5f); collider.isTrigger = false;
                if (root.GetComponent(runtimeType) == null) root.AddComponent(runtimeType);
                var old = root.transform.Find(ArtName); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var art = (GameObject)PrefabUtility.InstantiatePrefab(generated, root.scene);
                art.name = ArtName; art.transform.SetParent(root.transform, false);
                var legacy = root.transform.Find("Visual"); if (legacy != null) legacy.gameObject.SetActive(false);
                var binding = root.GetComponent<AnimolOperationalArtBinding>() ?? root.AddComponent<AnimolOperationalArtBinding>();
                binding.Configure(art, AnimolOperationalArtMode.Whole);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void PromoteDefinition(string id, string behavior)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath($"Assets/ANIMOL/Data/Development/M9BThemePlatforms/Types/{id}.asset");
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("implementationLevel").enumValueIndex = 0;
            var behaviors = serialized.FindProperty("behaviorComponentIds"); behaviors.arraySize = 1;
            behaviors.GetArrayElementAtIndex(0).stringValue = behavior;
            serialized.FindProperty("defaultSettings.implementationLevel").enumValueIndex = 0;
            serialized.FindProperty("defaultSettings.prototypeNotice").stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset);
        }

        private static void AddMovementPlatformsToLab()
        {
            var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>("Assets/ANIMOL/Data/Development/M9BThemePlatforms/DEV-M9B-REPRESENTATIVE-LAB.asset");
            AddLabPlacement(map, "LAB-SAIL", "CLOUD_SAIL_STEP", StageMapObjectKind.CloudSailStep, new Vector2Int(-11, 3), new Vector2Int(-8, 3));
            AddLabPlacement(map, "LAB-BOOKMARK", "BOOKMARK_LIFT", StageMapObjectKind.BookmarkLift, new Vector2Int(-11, -4), new Vector2Int(-11, 0));
            AddLabPlacement(map, "LAB-VINE", "GLASS_VINE_LIFT", StageMapObjectKind.GlassVineLift, new Vector2Int(8, -4), new Vector2Int(8, 0));
            AddLabPlacement(map, "LAB-SPINE", "LIB_SPINE_BRAKE", StageMapObjectKind.LibSpineBrake, new Vector2Int(7, 3), new Vector2Int(11, 3));
            map.EditorMarkCollisionDataSynchronized(); EditorUtility.SetDirty(map);
        }

        private static void AddLabPlacement(StageMapDefinition map, string stableId, string typeId, StageMapObjectKind kind, Vector2Int start, Vector2Int end)
        {
            map.EditorRemoveObject(stableId);
            var type = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeDefinition>($"Assets/ANIMOL/Data/Development/M9BThemePlatforms/Types/{typeId}.asset");
            var settings = type.DefaultSettings.Clone();
            settings.EditorSetPathCells(new[] { start, end });
            settings.EditorConfigureAuthoring(MapObjectImplementationLevel.DevPlayable, Array.Empty<string>(), settings.PhaseSeed,
                MapObjectResetPolicy.RespawnAndRetry, MapObjectRouteRole.Required, 0, string.Empty);
            map.EditorPlaceObject(stableId, kind, start.x, start.y, typeId, type.Prefab, settings);
        }

        private static void PromoteMineCartDefinition()
        {
            var asset = AssetDatabase.LoadMainAssetAtPath("Assets/ANIMOL/Data/Development/M9BThemePlatforms/Types/MINE_CART_FORK.asset");
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("implementationLevel").enumValueIndex = 0;
            var behavior = serialized.FindProperty("behaviorComponentIds");
            behavior.arraySize = 1; behavior.GetArrayElementAtIndex(0).stringValue = "MineCartFork";
            serialized.FindProperty("defaultSettings.implementationLevel").enumValueIndex = 0;
            serialized.FindProperty("defaultSettings.prototypeNotice").stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void Bind(string prefabPath, string artId, AnimolOperationalArtMode mode, bool mineCart = false)
        {
            var generated = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ANIMOL/Generated/Prefabs/{artId}.prefab");
            if (generated == null) throw new InvalidOperationException($"Missing generated art prefab: {artId}");
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var old = root.transform.Find(ArtName);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                if (mineCart)
                {
                    var prototype = root.GetComponent<PrototypeMapObject>();
                    if (prototype != null) UnityEngine.Object.DestroyImmediate(prototype);
                    if (root.GetComponent<Rigidbody2D>() == null) root.AddComponent<Rigidbody2D>();
                    if (root.GetComponent<BoxCollider2D>() == null)
                    {
                        var collider = root.AddComponent<BoxCollider2D>(); collider.size = new Vector2(3f, 1f); collider.offset = new Vector2(1.5f, .5f);
                    }
                    if (root.GetComponent<MineCartForkObject>() == null) root.AddComponent<MineCartForkObject>();
                }
                var art = (GameObject)PrefabUtility.InstantiatePrefab(generated, root.scene);
                art.name = ArtName; art.transform.SetParent(root.transform, false);
                var legacy = root.transform.Find("Visual");
                if (legacy != null) legacy.gameObject.SetActive(false);
                var binding = root.GetComponent<AnimolOperationalArtBinding>() ?? root.AddComponent<AnimolOperationalArtBinding>();
                binding.Configure(art, mode);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
