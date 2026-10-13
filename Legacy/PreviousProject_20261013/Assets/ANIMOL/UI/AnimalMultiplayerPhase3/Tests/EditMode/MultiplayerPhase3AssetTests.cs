using System.IO;
using System.Linq;
using ANIMOL.AnimalUiV2;
using ANIMOL.AnimalMultiplayerPhase3.Editor;
using ANIMOL.Core;
using ANIMOL.Typography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ANIMOL.AnimalMultiplayerPhase3.Tests
{
    public sealed class MultiplayerPhase3AssetTests
    {
        [Test] public void ProductionIsIndependentAndBuilderPreservesOverrides()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalMultiplayerPhase3Builder.PrefabPath);
            Assert.NotNull(root);
            var p = root.GetComponent<AnimalUiPresenter>();
            Assert.IsFalse(p.OpenReadonlyPreviewOnStart); Assert.That(p.Backend, Is.TypeOf<AnimalMultiplayerProjectAdapter>());
            var adapter = (AnimalMultiplayerProjectAdapter)p.Backend;
            Assert.That(adapter.Catalog, Is.SameAs(p.Catalog));
            Assert.That(AssetDatabase.GetAssetPath(adapter.IdMap), Is.EqualTo(AnimalMultiplayerPhase3Builder.MapPath));
            Assert.That(AssetDatabase.GetAssetPath(p.Catalog), Is.EqualTo(AnimalMultiplayerPhase3Builder.CatalogPath));
            Assert.IsEmpty(root.GetComponentsInChildren<Canvas>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<EventSystem>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<AnimalUiDemoSwitcher>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<AnimalMultiplayerPhase3DemoHost>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<AnimalUiSafeArea>(true));
            Assert.That(root.GetComponentsInChildren<PixelTextBridge>(true).All(b => b.Profile != null), Is.True);
            var paths = new[] { AnimalMultiplayerPhase3Builder.PrefabPath, AnimalMultiplayerPhase3Builder.CatalogPath, AnimalMultiplayerPhase3Builder.MapPath };
            var before = paths.Select(File.ReadAllBytes).ToArray();
            AnimalMultiplayerPhase3Builder.CreateMissingProductionAssets();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]), paths[i]);
        }
        [Test] public void FifteenMappingsAndFullCanvasImports()
        {
            var art = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(AnimalMultiplayerPhase3Builder.CatalogPath);
            var map = AssetDatabase.LoadAssetAtPath<AnimalMultiplayerIdMap>(AnimalMultiplayerPhase3Builder.MapPath);
            Assert.That(art.Animals.Count, Is.EqualTo(15));
            CollectionAssert.AreEquivalent(art.Animals.Select(a => a.Id), map.Entries.Select(e => e.ArtId));
            Assert.That(map.ToArtId("RABBIT"), Is.EqualTo("Rabbit"));
            Assert.That(map.ToProjectAnimal("Rabbit").AnimalId, Is.EqualTo("RABBIT"));
            Assert.That(map.Entries.Count(e => e.CampaignAnimal != null), Is.EqualTo(1));
            Assert.IsNull(map.ToArtId("DEV_GROUND")); Assert.IsNull(map.ToProjectAnimal("Wolf"));
            foreach (AnimalRole role in System.Enum.GetValues(typeof(AnimalRole))) Assert.That(art.ForRole(role).Count(), Is.EqualTo(5));
            foreach (var a in art.Animals)
            {
                var path = AssetDatabase.GetAssetPath(a.Portrait);
                Assert.That(Path.GetFileNameWithoutExtension(path), Does.EndWith("_" + a.Id));
                Assert.That(a.Portrait.rect.size, Is.EqualTo(new Vector2(128, 160)));
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                Assert.IsFalse(importer.mipmapEnabled);
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
            }
        }
        [Test] public void MappingRejectsUnknownAmbiguousAndIncompleteIdentityInBothDirections()
        {
            var original = AssetDatabase.LoadAssetAtPath<AnimalMultiplayerIdMap>(AnimalMultiplayerPhase3Builder.MapPath);
            var map = UnityEngine.Object.Instantiate(original);
            try
            {
                Assert.IsNull(map.ToArtId("rabbit")); Assert.IsNull(map.ToArtId("DEV_GROUND"));
                Assert.IsNull(map.ToProjectAnimal(null)); Assert.IsNull(map.ToProjectAnimal(" "));
                var rabbit = map.ToProjectAnimal("Rabbit");
                map.Entries = new[] { new AnimalMultiplayerIdMap.Entry { ArtId = "Rabbit", CampaignAnimal = rabbit },
                    new AnimalMultiplayerIdMap.Entry { ArtId = "Wolf", CampaignAnimal = rabbit } };
                Assert.IsNull(map.ToArtId("RABBIT")); Assert.IsNull(map.ToProjectAnimal("Rabbit"));
                map.Entries = new[] { new AnimalMultiplayerIdMap.Entry { ArtId = null, CampaignAnimal = rabbit } };
                Assert.IsNull(map.ToArtId("RABBIT")); Assert.IsNull(map.ToProjectAnimal(null));
                map.Entries = null; Assert.IsNull(map.ToArtId("RABBIT")); Assert.IsNull(map.ToProjectAnimal("Rabbit"));
            }
            finally { UnityEngine.Object.DestroyImmediate(map); }
        }
    }
}
