using System;
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
            Bind("Assets/ANIMOL/Prefabs/MapObjects/OBJ_RAIL_PLATFORM.prefab", "C6", AnimolOperationalArtMode.Whole);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_JADE_BALANCE.prefab", "M2", AnimolOperationalArtMode.BalancePlate);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_PHASE_STAIR.prefab", "M6", AnimolOperationalArtMode.MoonStair);
            Bind("Assets/ANIMOL/Prefabs/MapObjects/M9B/MINE_CART_FORK.prefab", "R8", AnimolOperationalArtMode.Whole, true);
            PromoteMineCartDefinition();
            AssetDatabase.SaveAssets();
            Debug.Log("ANIMOL operational art integration complete: C6, M2, M6, R8.");
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
