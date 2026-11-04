using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        [Test] public void StarwayStageOrderPreservesSourceMapProgress()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var gateway = prefab.GetComponentInChildren<NotebookProjectGateway>(true);
            Assert.That(gateway.starwayMaps.Length, Is.EqualTo(20));
            for (int i = 0; i < 20; i++)
            {
                string mapId = "SW" + i.ToString("00");
                Assert.That(gateway.starwayMaps[i].name,
                    Is.EqualTo(mapId).Or.EqualTo(mapId + "_MapData"));
                Assert.That(gateway.CanOpenStage("SW" + (i + 1).ToString("00")), Is.True);
            }

            const string oldKey = "ANIMOL.Starway.Cleared.SW01";
            string firstKey = NotebookProjectGateway.MapClearKey("SW00");
            string secondKey = NotebookProjectGateway.MapClearKey("SW01");
            bool hadOld = PlayerPrefs.HasKey(oldKey), hadFirst = PlayerPrefs.HasKey(firstKey),
                hadSecond = PlayerPrefs.HasKey(secondKey);
            int oldValue = PlayerPrefs.GetInt(oldKey), firstValue = PlayerPrefs.GetInt(firstKey),
                secondValue = PlayerPrefs.GetInt(secondKey);
            try
            {
                PlayerPrefs.SetInt(oldKey, 1);
                PlayerPrefs.DeleteKey(firstKey);
                PlayerPrefs.DeleteKey(secondKey);
                Assert.That(NotebookProjectGateway.IsStarwayCleared("SW01"), Is.False);
                Assert.That(NotebookProjectGateway.IsStarwayCleared("SW02"), Is.True);
            }
            finally
            {
                if (hadOld) PlayerPrefs.SetInt(oldKey, oldValue); else PlayerPrefs.DeleteKey(oldKey);
                if (hadFirst) PlayerPrefs.SetInt(firstKey, firstValue); else PlayerPrefs.DeleteKey(firstKey);
                if (hadSecond) PlayerPrefs.SetInt(secondKey, secondValue); else PlayerPrefs.DeleteKey(secondKey);
                PlayerPrefs.Save();
            }
        }

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
            Assert.That(root.stageScreen.name, Is.EqualTo("Constellation Neon Map"));
            var images = root.stageScreen.GetComponentsInChildren<Image>(true);
            Assert.That(images.Count(i => i.name.StartsWith("Constellation Figure ")), Is.EqualTo(32));
            Assert.That(images.Count(i => i.name.StartsWith("Static Constellation Wire ")), Is.EqualTo(32));
            Assert.That(images.Where(i => i.name.StartsWith("Constellation Figure "))
                .All(i => i.preserveAspect && i.color == Color.white), Is.True);
            Assert.That(images.Where(i => i.name.StartsWith("Static Constellation Wire "))
                .All(i => !i.preserveAspect && i.color == Color.white), Is.True);
            var skies = images.Where(i => i.name == "Static Night Sky").ToArray();
            Assert.That(skies.Length, Is.EqualTo(1));
            Assert.That(skies.All(i => AssetDatabase.GetAssetPath(i.sprite) == MapTextures + "Map/NightSky.png"), Is.True);
            foreach (var art in catalog.artworks)
            {
                Assert.That(art.stageIds.Length, Is.GreaterThan(0), art.id);
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(MapTextures + art.sprite + ".png"), Is.Not.Null, art.id);
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(MapTextures + art.wireSprite + ".png"), Is.Not.Null, art.id);
                Assert.That(art.stageIds.All(id => catalog.FindStage(id) != null), Is.True, art.id);
                Assert.That(art.stageRouteIndices.Length, Is.EqualTo(art.stageIds.Length), art.id);
                var figure = images.Single(i => i.name == "Constellation Figure " + art.id);
                var wire = images.Single(i => i.name == "Static Constellation Wire " + art.id);
                Assert.That(wire.rectTransform.anchoredPosition, Is.EqualTo(figure.rectTransform.anchoredPosition), art.id);
                Assert.That(wire.rectTransform.sizeDelta, Is.EqualTo(figure.rectTransform.sizeDelta), art.id);
                for (int i = 0; i < art.stageIds.Length; i++)
                {
                    var stage = catalog.FindStage(art.stageIds[i]);
                    int vertexIndex = art.wirePath[art.stageRouteIndices[i]];
                    var vertex = art.wireVertices[vertexIndex];
                    var node = root.nodes.Single(n => n.stageId == stage.id);
                    var box = (RectTransform)node.star.rectTransform.parent;
                    Assert.That(stage.artworkId, Is.EqualTo(art.id));
                    Assert.That(stage.wireVertexIndex, Is.EqualTo(vertexIndex));
                    Assert.That(vertex.stageId, Is.EqualTo(stage.id));
                    Assert.That(stage.x, Is.EqualTo(vertex.x).Within(.02f), stage.id);
                    Assert.That(stage.y, Is.EqualTo(vertex.y).Within(.02f), stage.id);
                    Assert.That(stage.x, Is.EqualTo(art.renderX + vertex.u * art.renderWidth).Within(.02f), stage.id);
                    Assert.That(stage.y, Is.EqualTo(art.renderY + vertex.v * art.renderHeight).Within(.02f), stage.id);
                    Assert.That(box.anchoredPosition.x + node.star.rectTransform.rect.width * .5f,
                        Is.EqualTo(stage.x).Within(.02f), stage.id);
                    Assert.That(-box.anchoredPosition.y + node.star.rectTransform.rect.height * .5f,
                        Is.EqualTo(stage.y).Within(.02f), stage.id);
                }
            }
            Assert.That(catalog.artworks.Sum(a => a.stageIds.Length), Is.EqualTo(110));
            Assert.That(catalog.edges.Count(e => e.kind == "constellation"), Is.EqualTo(78));
            Assert.That(catalog.edges.Count(e => e.kind == "transition"), Is.EqualTo(28));
            Assert.That(root.mapStarFrames.Length, Is.EqualTo(48));
            Assert.That(root.stageSheet.gameObject.activeSelf, Is.False);
            Assert.That(root.storyReplayButton.gameObject.activeSelf, Is.False);
            Assert.That(root.chapters, Is.Empty);
            Assert.That(images.Any(i => i.name == "Map Header" || i.name == "Selected Stage Footer"), Is.False);
            foreach (var edge in catalog.edges)
            {
                var from = catalog.FindStage(edge.from); var to = catalog.FindStage(edge.to);
                Assert.That(edge.points.Length, Is.GreaterThanOrEqualTo(2), edge.sprite);
                Assert.That(edge.points[0].x, Is.EqualTo(from.x).Within(.02f), edge.sprite);
                Assert.That(edge.points[0].y, Is.EqualTo(from.y).Within(.02f), edge.sprite);
                Assert.That(edge.points[edge.points.Length - 1].x, Is.EqualTo(to.x).Within(.02f), edge.sprite);
                Assert.That(edge.points[edge.points.Length - 1].y, Is.EqualTo(to.y).Within(.02f), edge.sprite);
            }
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
            foreach (var button in new[] { root.campaignButton, root.settingsButton, root.modalPrimary })
            {
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap), button.name);
                Assert.That(button.image.color, Is.EqualTo(Color.white), button.name);
                Assert.That(button.spriteState.highlightedSprite, Is.Not.Null, button.name);
                Assert.That(button.spriteState.pressedSprite, Is.Not.Null, button.name);
                Assert.That(button.spriteState.disabledSprite, Is.Not.Null, button.name);
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(button.image.sprite)) as TextureImporter;
                Assert.That(importer.spriteBorder, Is.EqualTo(Vector4.one * 36), button.name);
            }
            foreach (var button in new[] { root.mapBack, root.telescopeButton, root.storyReplayButton, root.launchButton })
            {
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap), button.name);
                Assert.That(button.image.color, Is.EqualTo(Color.white), button.name);
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(button.image.sprite)) as TextureImporter;
                Assert.That(importer.spriteBorder, Is.EqualTo(Vector4.one * 32), button.name);
            }
            Assert.That(((TextureImporter)AssetImporter.GetAtPath(MapTextures + "MapNeon/PanelFrame.png")).spriteBorder,
                Is.EqualTo(Vector4.one * 36));
            string[] states = { "Available", "Current", "Done", "Locked" };
            for (int state = 0; state < states.Length; state++)
                for (int variant = 0; variant < 3; variant++)
                    for (int frame = 0; frame < 4; frame++)
                    {
                        int index = (state * 3 + variant) * 4 + frame;
                        string path = MapTextures + "Map/Stars/Star_" + states[state] + "_V" + variant + "_F" + frame + ".png";
                        Assert.That(AssetDatabase.GetAssetPath(root.mapStarFrames[index]), Is.EqualTo(path));
                        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100), path);
                        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                        Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                        Assert.That(importer.mipmapEnabled, Is.False, path);
                        Assert.That(importer.spriteBorder, Is.EqualTo(Vector4.zero), path);
                        Assert.That(root.mapStarFrames[index].rect.width, Is.EqualTo(160), path);
                        Assert.That(root.mapStarFrames[index].rect.height, Is.EqualTo(160), path);
                    }
            foreach (var node in root.nodes)
            {
                int variant = NotebookRoot.MapStarVariant(node.stageId);
                Assert.That(variant, Is.InRange(0, 2), node.stageId);
                Assert.That(NotebookRoot.MapStarVariant(node.stageId), Is.EqualTo(variant), node.stageId);
                Assert.That(node.star.transform.localRotation, Is.EqualTo(Quaternion.identity), node.stageId);
            }
        }

        [Test] public void NeonMapMenuRestoresSelectedSceneObjectWhenArtworkIsMissing()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var scene = EditorSceneManager.NewPreviewScene();
            TextAsset brokenCatalog = null;
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var root = instance.GetComponentInChildren<NotebookRoot>(true);
                var oldMap = root.stageScreen;
                oldMap.name = "Temporary Map For Failure Recovery";
                var oldMain = root.mainScreen;
                var oldSettings = root.settingsScreen;
                var oldModal = root.modal;
                var oldGateway = root.gateway;
                var oldLayout = root.GetComponentInParent<NotebookCanvasLayout>();
                var oldMapScreen = oldLayout.mapScreen;
                int childCount = oldMap.transform.parent.childCount;
                var catalog = NotebookCatalog.Parse(root.catalogJson);
                brokenCatalog = new TextAsset(root.catalogJson.text.Replace(catalog.artworks[0].sprite,
                    "Constellations/Missing_Neon_Map_Artwork"));
                root.catalogJson = brokenCatalog;
                Selection.activeGameObject = root.gameObject;
                Assert.Throws<InvalidOperationException>(() =>
                    William.NotebookUI.Editor.NotebookInstaller.ApplyNeonMapToSelectedNotebook());
                Assert.That(root.stageScreen, Is.EqualTo(oldMap));
                Assert.That(oldMap.transform.parent.childCount, Is.EqualTo(childCount));
                Assert.That(root.mainScreen, Is.EqualTo(oldMain));
                Assert.That(root.settingsScreen, Is.EqualTo(oldSettings));
                Assert.That(root.modal, Is.EqualTo(oldModal));
                Assert.That(root.gateway, Is.EqualTo(oldGateway));
                Assert.That(oldLayout.mapScreen, Is.EqualTo(oldMapScreen));
            }
            finally
            {
                Selection.activeGameObject = null;
                EditorSceneManager.ClosePreviewScene(scene);
                if (brokenCatalog) UnityEngine.Object.DestroyImmediate(brokenCatalog);
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
