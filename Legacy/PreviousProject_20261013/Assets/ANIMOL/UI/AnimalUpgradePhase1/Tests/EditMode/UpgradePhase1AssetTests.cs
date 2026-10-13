using System.IO;
using System.Linq;
using ANIMOL.AnimalUiV2;
using ANIMOL.AnimalUpgradePhase1.Editor;
using ANIMOL.Typography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ANIMOL.AnimalUpgradePhase1.Tests
{
    public sealed class UpgradePhase1AssetTests
    {
        [Test] public void ProductionIsIndependentAndDoesNotInstallDemoInfrastructure()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalUpgradePhase1Builder.PrefabPath);
            Assert.NotNull(root);
            var p = root.GetComponent<AnimalUiPresenter>();
            Assert.IsFalse(p.OpenReadonlyPreviewOnStart);
            Assert.That(p.Backend, Is.TypeOf<AnimalUpgradeProjectAdapter>());
            var backend = (AnimalUpgradeProjectAdapter)p.Backend;
            Assert.NotNull(backend.IdMap);
            Assert.NotNull(backend.GrowthPolicy);
            Assert.NotNull(backend.UpgradeCatalog);
            Assert.That(AssetDatabase.GetAssetPath(p.Catalog), Is.EqualTo(AnimalUpgradePhase1Builder.CatalogPath));
            Assert.IsEmpty(root.GetComponentsInChildren<Canvas>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<EventSystem>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<AnimalUiDemoSwitcher>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<AnimalUpgradePhase1DemoHost>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<AnimalUiSafeArea>(true));
            Assert.NotNull(root.GetComponent<AnimalUpgradePhase1Host>());
            Assert.That(root.GetComponentsInChildren<PixelTextBridge>(true).All(b => b.Profile != null), Is.True);
        }

        [Test] public void MissingOnlyBuilderPreservesOperationalOverridesAndMappingBytes()
        {
            var paths = new[] { AnimalUpgradePhase1Builder.PrefabPath, AnimalUpgradePhase1Builder.CatalogPath, AnimalUpgradePhase1Builder.MapPath };
            var before = paths.Select(File.ReadAllBytes).ToArray();
            AnimalUpgradePhase1Builder.CreateMissingProductionAssets();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]), paths[i]);
        }

        [Test] public void AllFifteenFullCanvasPortraitsHaveExplicitNonAuthoritativeMappings()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(AnimalUpgradePhase1Builder.CatalogPath);
            var map = AssetDatabase.LoadAssetAtPath<AnimalUpgradeIdMap>(AnimalUpgradePhase1Builder.MapPath);
            Assert.That(catalog.Animals.Count, Is.EqualTo(15));
            CollectionAssert.AreEquivalent(catalog.Animals.Select(a => a.Id), map.Entries.Select(e => e.ArtId));
            Assert.That(map.Entries.Count(e => e.CampaignAnimal != null), Is.EqualTo(1));
            Assert.That(map.Entries.Single(e => e.CampaignAnimal != null).ArtId, Is.EqualTo("Rabbit"));
            Assert.That(map.Entries.All(e => string.IsNullOrEmpty(e.GrowthServiceId)), Is.True);
            foreach (AnimalRole role in System.Enum.GetValues(typeof(AnimalRole))) Assert.That(catalog.ForRole(role).Count(), Is.EqualTo(5));
            foreach (var animal in catalog.Animals)
            {
                Assert.That(Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(animal.Portrait)), Does.EndWith("_" + animal.Id));
                Assert.That(animal.Portrait.rect.size, Is.EqualTo(new Vector2(128, 160)));
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(animal.Portrait));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                Assert.IsFalse(importer.mipmapEnabled);
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
            }
        }
    }
}
