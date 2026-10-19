using System;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using ANIMOL.Art.V12;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public sealed class StarwayRanchV12Tests
    {
        const string Root = "Assets/ANIMOL/Sprites/StarwayRanch/ANIMOL_STARWAY_RANCH_CARTOON_V12";
        const string Art = "Assets/ANIMOL/Data/Art/StarwayRanch/StarwayRanchArt.asset";

        [Test] public void ExactManifestSpritesAndImportValues()
        {
            var art = AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(Art);
            Assert.That(art, Is.Not.Null);
            Assert.That(art.sprites.Length, Is.EqualTo(1937));
            Assert.That(art.sprites.Select(entry => entry.file).Distinct().Count(), Is.EqualTo(1937));
            foreach (var entry in art.sprites)
            {
                Assert.That(entry.sprite, Is.Not.Null, entry.file);
                var path = Root + "/" + entry.file;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
            }
            var chick = art.Find("props/chick/south/idle.png");
            Assert.That(chick.pixelsPerUnit, Is.EqualTo(256));
            Assert.That(chick.pivot.y / chick.rect.height, Is.EqualTo(.4822695f).Within(.00001f));
            Assert.That(art.Find("backgrounds/cloud_bank.png").pixelsPerUnit, Is.EqualTo(166.11111f).Within(.001f));
            Assert.That(art.v12Animations, Is.Not.Null);
            Assert.That(art.v12Animations.ClipCount, Is.EqualTo(54));
            Assert.That(art.v12Animations.SpriteCount, Is.EqualTo(121));
            Assert.That(art.v12Animations.SourceManifestSha256, Is.EqualTo(art.v12ManifestSha256));
            Assert.That(art.v12Animations.ValidateIntegrity(art.v12ManifestSha256, out var error), Is.True, error);
            Assert.That(art.v12Animations.TryGetClip("missing_latest_key", art.v12ManifestSha256,
                out var missing, out error), Is.False);
            Assert.That(missing, Is.Null);
            Assert.That(error, Does.Contain("Missing latest clip key"));
        }

        [Test] public void SavedQaSceneHasIndependentOrderedLayers()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Art_QA.unity");
                var rig = UnityEngine.Object.FindFirstObjectByType<Animol.StarwayRanch.V12.StarwayRanchV12BackgroundRig>();
                Assert.That(rig, Is.Not.Null);
                var renderers = rig.GetComponentsInChildren<SpriteRenderer>();
                Assert.That(renderers.Length, Is.EqualTo(129));
                Assert.That(renderers.All(r => r.sprite && r.sortingLayerName == "Background"), Is.True);
                Assert.That(renderers.Where(r => r.name.Contains("star") || r.name.Contains("dot"))
                    .All(r => r.sortingOrder <= -80), Is.True);
                Assert.That(UnityEngine.Object.FindFirstObjectByType<StarwayRanchMapRenderer>(), Is.Not.Null);
            }
            finally { if (setup.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        [Test] public void RanchPrefabsUseOnlyVerifiedV12Frames()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayRanchV12VisualCatalog>(
                Root + "/_Generated/StarwayRanchV12VisualCatalog.asset");
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/ANIMOL/Prefabs/Themes/starway" });
            Assert.That(prefabs.Length, Is.EqualTo(8));
            foreach (var guid in prefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var view = prefab.GetComponent<ANIMOL.Presentation.DeviceVisualView>();
                Assert.That(view, Is.Not.Null, prefab.name);
                Assert.That(AssetDatabase.GetAssetPath(view.visual.sprite), Does.StartWith(Root), prefab.name);
                foreach (var state in view.states)
                {
                    Assert.That(catalog.TryGetClip(state.v12ClipKey, catalog.SourceManifestSha256,
                        out var clip, out var error), Is.True, prefab.name + "/" + state.name + ": " + error);
                    if (state.sprite) Assert.That(AssetDatabase.GetAssetPath(state.sprite), Does.StartWith(Root));
                    if (state.terminal) Assert.That(AssetDatabase.GetAssetPath(state.terminal), Does.StartWith(Root));
                    Assert.That(state.clip, Is.Not.Null, prefab.name + "/" + state.name);
                }
            }
        }

        [Test] public void ExistingDeviceRolesAndV12ArtStayDistinct()
        {
            Assert.That((int)PrototypeRole.StarSocket, Is.EqualTo(1));
            Assert.That((int)PrototypeRole.VineBridge, Is.EqualTo(2));
            Assert.That((int)PrototypeRole.Conveyor, Is.EqualTo(7));
            Assert.That((int)PrototypeRole.OneWayGate, Is.EqualTo(9));
            Assert.That((int)PrototypeRole.Bounce, Is.EqualTo(18));
            Assert.That((int)PrototypeRole.Brake, Is.EqualTo(19));
            Assert.That((int)PrototypeRole.Wind, Is.EqualTo(20));
            var art = AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(Art);
            Assert.That(art.Find("devices/oneway_gate/closed.png"), Is.Not.Null);
            Assert.That(art.Find("props/ranch_gate.png"), Is.Not.SameAs(art.Find("devices/oneway_gate/closed.png")));
        }
    }
}
