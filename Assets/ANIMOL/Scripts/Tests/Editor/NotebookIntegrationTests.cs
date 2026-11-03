using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using William.NotebookUI;

namespace ANIMOL.Presentation.Tests
{
    public sealed class NotebookIntegrationTests
    {
        const string CatalogPath = "Assets/ANIMOL/Data/UI/Notebook/catalog.json";
        const string PrefabPath = "Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab";
        const string ScenePath = "Assets/ANIMOL/Scenes/Main/NotebookUI.unity";
        const string MapTextures = "Assets/ANIMOL/Art/UI/ConstellationMap/Textures/";

        [Test] public void ActiveSceneUsesFreshNightMapWith32FiguresAnd110RealIds()
        {
            var catalog = NotebookCatalog.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath));
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(ScenePath));
            var dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            Assert.That(dependencies, Does.Contain(PrefabPath));
            Assert.That(dependencies.Any(path => path.Contains("/Map/Region_")), Is.False);
            Assert.That(catalog.stages.Length, Is.EqualTo(110));
            Assert.That(catalog.artworks.Length, Is.EqualTo(32));
            Assert.That(catalog.edges.Length, Is.EqualTo(106));
            Assert.That(catalog.themes.Select(t => catalog.stages.Count(s => s.themeId == t.id)),
                Is.EqualTo(new[] { 20, 30, 30, 30 }));
            Assert.That(catalog.FindStage("SW01"), Is.Not.Null);
            Assert.That(catalog.FindStage("ET-30").sceneName, Is.EqualTo("ANIMOL_Echo_Theater"));
            Assert.That(catalog.FindStage("observatory-01").sceneName, Is.Empty);
            Assert.That(catalog.FindStage("sea-01").sceneName, Is.Empty);
            Assert.That(catalog.battleRequiredStageIds, Is.Empty);
            Assert.That(catalog.themes.Where(t => t.paid).All(t => string.IsNullOrEmpty(t.productId)), Is.True);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var root = prefab.GetComponentInChildren<NotebookRoot>(true);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.gateway, Is.TypeOf<NotebookProjectGateway>());
            Assert.That(root.nodes.Length, Is.EqualTo(110));
            Assert.That(root.edges.Length, Is.EqualTo(106));
            var images = root.GetComponentsInChildren<Image>(true);
            Assert.That(images.Count(i => i.name.StartsWith("Constellation Figure ")), Is.EqualTo(32));
            Assert.That(images.Count(i => i.name.StartsWith("Static Constellation Wire ")), Is.EqualTo(32));
            Assert.That(images.Where(i => i.name.StartsWith("Constellation Figure ") ||
                i.name.StartsWith("Static Constellation Wire ")).All(i => i.preserveAspect && i.color == Color.white), Is.True);
            var skies = images.Where(i => i.name.StartsWith("Static Night Sky ")).ToArray();
            Assert.That(skies.Length, Is.EqualTo(4));
            Assert.That(skies.All(i => AssetDatabase.GetAssetPath(i.sprite) == MapTextures + "Map/NightSky.png"), Is.True);
            foreach (var art in catalog.artworks)
            {
                Assert.That(art.stageIds.Length, Is.GreaterThan(0), art.id);
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(MapTextures + art.sprite + ".png"), Is.Not.Null, art.id);
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(MapTextures + art.wireSprite + ".png"), Is.Not.Null, art.id);
                Assert.That(art.stageIds.All(id => catalog.FindStage(id) != null), Is.True, art.id);
            }
            Assert.That(catalog.artworks.Sum(a => a.stageIds.Length), Is.EqualTo(110));
            Assert.That(images.Where(i => i.sprite && AssetDatabase.GetAssetPath(i.sprite).StartsWith("Assets/ANIMOL/Art/UI/"))
                .All(i => i.color == Color.white), Is.True);
        }

        [Test] public void NewPngImportsAndButtonSpritesUseThePackageSettings()
        {
            var catalog = NotebookCatalog.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath));
            var pngs = new HashSet<string> { "Map/NightSky" };
            foreach (var art in catalog.artworks) { pngs.Add(art.sprite); pngs.Add(art.wireSprite); }
            foreach (var edge in catalog.edges) { pngs.Add(edge.sprite + "_Future"); pngs.Add(edge.sprite + "_Cleared"); }
            Assert.That(pngs.Count, Is.EqualTo(277));
            foreach (string relative in pngs)
            {
                string path = MapTextures + relative + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.GetPlatformTextureSettings("Android").overridden, Is.False, path);
                Assert.That(importer.GetPlatformTextureSettings("iPhone").overridden, Is.False, path);
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
            }
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponentInChildren<NotebookRoot>(true);
            foreach (var button in new[] { root.campaignButton, root.settingsButton, root.launchButton, root.modalPrimary })
            {
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap), button.name);
                Assert.That(button.image.color, Is.EqualTo(Color.white), button.name);
                Assert.That(button.spriteState.highlightedSprite, Is.Not.Null, button.name);
                Assert.That(button.spriteState.pressedSprite, Is.Not.Null, button.name);
                Assert.That(button.spriteState.disabledSprite, Is.Not.Null, button.name);
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(button.image.sprite)) as TextureImporter;
                Assert.That(importer.spriteBorder, Is.EqualTo(Vector4.one * 36), button.name);
            }
        }

        [Test] public void WalletApprovalsAndBattleGateRequireUniqueVerifiedEvents()
        {
            var catalog = new NotebookCatalog {
                themes = new[] { new NotebookTheme { id = "starway" }, new NotebookTheme { id = "theater", paid = true } },
                stages = new[] { new NotebookStage { id = "SW01", themeId = "starway", order = 1 },
                    new NotebookStage { id = "ET-01", themeId = "theater", order = 1 } },
                requiredStagePolicyIsExample = false, battleRequiredStageIds = new[] { "SW01", "ET-01" }
            };
            const long now = 10000;
            var state = new NotebookWalletState { balance = 0, lastRecoveryUnix = now };
            var model = new NotebookBmModel(catalog, state, now);
            Assert.That(model.AcceptVerifiedReward("cancel", false, now, out _), Is.False);
            Assert.That(state.balance, Is.Zero);
            Assert.That(model.AcceptVerifiedReward("grant-1", true, now, out int first), Is.True);
            Assert.That(first, Is.EqualTo(5));
            Assert.That(model.AcceptVerifiedReward("grant-1", true, now, out _), Is.False);
            Assert.That(state.balance, Is.EqualTo(5));
            state.balance = 8;
            Assert.That(model.AcceptVerifiedReward("grant-2", true, now, out int second), Is.True);
            Assert.That(second, Is.EqualTo(2));
            Assert.That(model.AcceptVerifiedReward("grant-3", true, now, out int third), Is.True);
            Assert.That(third, Is.Zero);
            Assert.That(state.balance, Is.EqualTo(10));
            Assert.That(model.AcceptApprovedEntry("entry-rejected", "SW01", false, now, out _), Is.False);
            Assert.That(state.balance, Is.EqualTo(10));
            Assert.That(model.AcceptApprovedEntry("entry-1", "SW01", true, now, out bool duplicate), Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(state.balance, Is.EqualTo(9));
            Assert.That(model.AcceptApprovedEntry("entry-1", "SW01", true, now, out duplicate), Is.True);
            Assert.That(duplicate, Is.True);
            Assert.That(state.balance, Is.EqualTo(9));
            Assert.That(model.BattleUnlocked(), Is.False);
            Assert.That(model.AcceptVerifiedPurchase("theater", true), Is.True);
            Assert.That(model.BattleUnlocked(), Is.False);
            Assert.That(model.AcceptVerifiedClear("SW01", true), Is.True);
            Assert.That(model.BattleUnlocked(), Is.False);
            Assert.That(model.AcceptVerifiedClear("ET-01", true), Is.True);
            Assert.That(model.BattleUnlocked(), Is.True);
            state.balance = 0; state.lastRecoveryUnix = now;
            model.Tick(now + 180);
            Assert.That(state.balance, Is.EqualTo(1));
        }
    }
}
