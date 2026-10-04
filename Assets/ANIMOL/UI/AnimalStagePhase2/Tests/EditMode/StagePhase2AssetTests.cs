using System.IO;
using System.Linq;
using ANIMOL.AnimalUiV2;
using ANIMOL.AnimalStagePhase2.Editor;
using ANIMOL.Core;
using ANIMOL.Typography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ANIMOL.AnimalStagePhase2.Tests
{
    public sealed class StagePhase2AssetTests
    {
        [Test] public void ProductionIsIndependentAndBuilderPreservesOverrides()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalStagePhase2Builder.PrefabPath);
            Assert.NotNull(root);
            var p = root.GetComponent<AnimalUiPresenter>();
            Assert.IsFalse(p.OpenReadonlyPreviewOnStart); Assert.IsNull(p.Backend);
            Assert.IsNull(p.View.CampaignBackground, "Host resolves actual selected stage theme.");
            Assert.That(AssetDatabase.GetAssetPath(p.Catalog), Is.EqualTo(AnimalStagePhase2Builder.CatalogPath));
            Assert.IsEmpty(root.GetComponentsInChildren<Canvas>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<EventSystem>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<AnimalUiDemoSwitcher>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<AnimalStagePhase2DemoHost>(true));
            Assert.IsEmpty(root.GetComponentsInChildren<AnimalUiSafeArea>(true));
            Assert.That(root.GetComponentsInChildren<PixelTextBridge>(true).All(b => b.Profile != null), Is.True);
            var paths = new[] { AnimalStagePhase2Builder.PrefabPath, AnimalStagePhase2Builder.CatalogPath, AnimalStagePhase2Builder.MapPath };
            var before = paths.Select(File.ReadAllBytes).ToArray();
            AnimalStagePhase2Builder.CreateMissingProductionAssets();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]), paths[i]);
        }
        [Test] public void RealContextDoesNotFillMissingSpeciesOrMutateStage()
        {
            var map = AssetDatabase.LoadAssetAtPath<AnimalStageIdMap>(AnimalStagePhase2Builder.MapPath);
            var art = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(AnimalStagePhase2Builder.CatalogPath);
            var stage = AssetDatabase.LoadAssetAtPath<CampaignStageDefinition>("Assets/ANIMOL/Data/Campaign/Stages/T01-S01.asset");
            string before = JsonUtility.ToJson(stage);
            var context = AnimalStageContext.Read(stage, map, art);
            Assert.That(context.StageId, Is.EqualTo(stage.StageId));
            Assert.That(context.FixedLoadout.Fingerprint(), Is.EqualTo("Rabbit||"));
            Assert.That(context.Requirements.RepresentativeRole, Is.EqualTo(AnimalRole.Ground));
            Assert.That(context.Requirements.PolicyRevision, Is.EqualTo("content:" + stage.ContentVersion + ";runtime:" + stage.RuntimePolicy.Version));
            Assert.IsFalse(AnimalUiRules.ValidateFixedStageContext(art, context, out _));
            context.FixedLoadout.Ground = "Wolf";
            Assert.That(JsonUtility.ToJson(stage), Is.EqualTo(before));
            Assert.That(context.InitialLoadout.Ground, Is.EqualTo("Rabbit"));
            Assert.IsNull(map.ToArtId("DEV_GROUND")); Assert.IsNull(map.ToArtId("WOLF"));
            Assert.That(map.ToArtId("RABBIT"), Is.EqualTo("Rabbit"));
            Assert.That(map.ToProjectAnimal("Rabbit").AnimalId, Is.EqualTo("RABBIT"));
            Assert.That(map.Entries.Count(e => e.CampaignAnimal != null), Is.EqualTo(1));
        }
        [Test] public void FifteenMappingsAndFullCanvasImports()
        {
            var art = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(AnimalStagePhase2Builder.CatalogPath);
            var map = AssetDatabase.LoadAssetAtPath<AnimalStageIdMap>(AnimalStagePhase2Builder.MapPath);
            Assert.That(art.Animals.Count, Is.EqualTo(15));
            CollectionAssert.AreEquivalent(art.Animals.Select(a => a.Id), map.Entries.Select(e => e.ArtId));
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
    }
}
