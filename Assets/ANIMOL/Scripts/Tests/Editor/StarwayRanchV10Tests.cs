using System;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public sealed class StarwayRanchV10Tests
    {
        const string Root = "Assets/ANIMOL/Sprites/StarwayRanch";
        const string Art = "Assets/ANIMOL/Data/Art/StarwayRanch/StarwayRanchArt.asset";

        [Test] public void ExactManifestSpritesAndImportValues()
        {
            var art = AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(Art);
            Assert.That(art, Is.Not.Null);
            Assert.That(art.sprites.Length, Is.EqualTo(1816));
            Assert.That(art.sprites.Select(entry => entry.file).Distinct().Count(), Is.EqualTo(1816));
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
            var chick = art.Find("props/chick_front.png");
            Assert.That(chick.pixelsPerUnit, Is.EqualTo(256));
            Assert.That(chick.pivot.y / chick.rect.height, Is.EqualTo(.4822695f).Within(.00001f));
            Assert.That(art.Find("backgrounds/cloud_bank.png").pixelsPerUnit, Is.EqualTo(166.11111f).Within(.001f));
        }

        [Test] public void SavedQaSceneHasIndependentOrderedLayers()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Art_QA.unity");
                var rig = UnityEngine.Object.FindFirstObjectByType<Animol.StarwayRanch.V10.StarwayRanchV10BackgroundRig>();
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

        [Test] public void ExistingDeviceRolesAndV10ArtStayDistinct()
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
