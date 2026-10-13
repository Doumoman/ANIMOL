using System;
using System.IO;
using System.Linq;
using Animol;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Tests.EditMode
{
    public sealed class AnimolAtlasImportTests
    {
        private const string AtlasRoot = "Assets/ANIMOL/Atlases";
        private const string GeneratedRoot = "Assets/ANIMOL/Generated";

        [Serializable] private sealed class Manifest { public SpriteEntry[] sprites; public ObjectEntry[] objects; }
        [Serializable] private sealed class SpriteEntry { public string name; }
        [Serializable] private sealed class ObjectEntry { public string id; public PartEntry[] parts; }
        [Serializable] private sealed class PartEntry { public string name; public ClipEntry[] clips; }
        [Serializable] private sealed class ClipEntry { public string name; public string[] sprite_names; }

        [Test]
        public void GeneratedAssetCountsAndTextureSettingsMatchContract()
        {
            var animationSprites = AssetDatabase.LoadAllAssetsAtPath(AtlasRoot + "/ANIMOL_Master_Animations_32.png").OfType<Sprite>().ToArray();
            Assert.That(animationSprites, Has.Length.EqualTo(533));
            Assert.That(AssetDatabase.FindAssets("t:Prefab", new[] { GeneratedRoot + "/Prefabs" }), Has.Length.EqualTo(45));
            Assert.That(AssetDatabase.FindAssets("t:Tile", new[] { GeneratedRoot + "/TerrainTiles" }), Has.Length.EqualTo(80));

            foreach (var file in new[] { "ANIMOL_Master_Animations_32.png", "ANIMOL_Master_Terrain_32.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasRoot + "/" + file);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), file);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), file);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), file);
                Assert.That(importer.mipmapEnabled, Is.False, file);
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(AtlasRoot + "/" + file).OfType<Sprite>())
                {
                    var expected = sprite.rect.size * .5f;
                    Assert.That(sprite.pivot.x, Is.EqualTo(expected.x).Within(.001f), sprite.name + " pivot.x");
                    Assert.That(sprite.pivot.y, Is.EqualTo(expected.y).Within(.001f), sprite.name + " pivot.y");
                }
            }
        }

        [Test]
        public void AnimationCurvesKeepManifestFrameOrder()
        {
            var json = File.ReadAllText(AtlasRoot + "/animation_atlas.json");
            var manifest = JsonUtility.FromJson<Manifest>(json);
            Assert.That(manifest.sprites.Select(item => item.name).Distinct().Count(), Is.EqualTo(533));
            foreach (var obj in manifest.objects)
            foreach (var part in obj.parts)
            foreach (var phase in part.clips)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{GeneratedRoot}/Clips/{obj.id}/{obj.id}_{phase.name}.anim");
                Assert.That(clip, Is.Not.Null, $"{obj.id}/{phase.name}");
                var binding = EditorCurveBinding.PPtrCurve(part.name, typeof(SpriteRenderer), "m_Sprite");
                var names = AnimationUtility.GetObjectReferenceCurve(clip, binding).Select(key => ((Sprite)key.value).name).ToArray();
                Assert.That(names, Is.EqualTo(phase.sprite_names), $"{obj.id}/{part.name}/{phase.name}");
            }
        }

        [TestCase("C6", new[] { "track", "cart", "wheels" }, new[] { "cart" })]
        [TestCase("M2", new[] { "base", "left_plate", "right_plate" }, new[] { "left_plate", "right_plate" })]
        [TestCase("M6", new[] { "moon_dial", "stair_1", "stair_2", "stair_3" }, new[] { "stair_1", "stair_2", "stair_3" })]
        [TestCase("R8", new[] { "branching_track", "branch_switch", "cart" }, new[] { "cart" })]
        public void KeyAssembliesKeepIndependentChildrenAndMovingColliders(string id, string[] children, string[] colliders)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GeneratedRoot}/Prefabs/{id}.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(Enumerable.Range(0, prefab.transform.childCount).Select(i => prefab.transform.GetChild(i).name), Is.EquivalentTo(children));
            foreach (var childName in colliders)
            {
                var child = prefab.transform.Find(childName);
                Assert.That(child.GetComponent<BoxCollider2D>(), Is.Not.Null, id + "/" + childName);
                Assert.That(child.GetComponent<AnimolPartColliderFollower>(), Is.Not.Null, id + "/" + childName);
            }
        }

        [Test]
        public void QaSceneContainsFiveThemesAndSeparateOneWayColliders()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/ANIMOL/Scenes/ANIMOL_Atlas_QA.unity");
            try
            {
                var root = scene.GetRootGameObjects().Single(item => item.name == "ANIMOL Atlas QA");
                Assert.That(Enumerable.Range(0, root.transform.childCount).Count(i => root.transform.GetChild(i).name.StartsWith("Terrain_", StringComparison.Ordinal)), Is.EqualTo(5));
                Assert.That(root.GetComponentsInChildren<PlatformEffector2D>(true).Count(item => item.name == "OneWayCollision"), Is.EqualTo(15));
                Assert.That(root.GetComponentsInChildren<AnimolQaPhaseCycler>(true), Has.Length.EqualTo(4));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void OneWayTileAssetsStayVisualOnly()
        {
            var tiles = AssetDatabase.FindAssets("t:Tile", new[] { GeneratedRoot + "/TerrainTiles" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Tile>).ToArray();
            Assert.That(tiles.Where(tile => tile.name.Contains("platform_", StringComparison.Ordinal)).All(tile => tile.colliderType == Tile.ColliderType.None), Is.True);
        }
    }
}
