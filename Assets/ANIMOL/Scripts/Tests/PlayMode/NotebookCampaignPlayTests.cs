#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using William.NotebookUI;
using Object = UnityEngine.Object;

namespace ANIMOL.Presentation.Tests
{
    public sealed class NotebookCampaignPlayTests
    {
        const string Evidence = "Docs/Validation/TwentyStageCampaign";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject host, hudObject;
        NotebookRoot root;
        bool hadSelection, hadWallet, hadPrefs;
        int selection;
        string wallet, prefs;
        readonly Dictionary<string, (bool had, int value)> clears = new Dictionary<string, (bool, int)>();
        [SetUp] public void PreservePreferences()
        {
            hadSelection = PlayerPrefs.HasKey("ANIMOL.EchoTheater.Selected"); selection = PlayerPrefs.GetInt("ANIMOL.EchoTheater.Selected");
            hadWallet = PlayerPrefs.HasKey("WilliamNotebookUI.Wallet"); wallet = PlayerPrefs.GetString("WilliamNotebookUI.Wallet");
            hadPrefs = PlayerPrefs.HasKey("WilliamNotebookUI.Preferences"); prefs = PlayerPrefs.GetString("WilliamNotebookUI.Preferences");
            clears.Clear();
            foreach (string key in new[] { NotebookProjectGateway.MapClearKey("SW18"), NotebookProjectGateway.MapClearKey("SW19") })
                clears.Add(key, (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key)));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (host) Object.Destroy(host);
            if (hudObject) Object.Destroy(hudObject);
            if (root) Object.Destroy(root.GetComponentInParent<Canvas>().rootCanvas.gameObject);
            yield return null;
            if (hadSelection) PlayerPrefs.SetInt("ANIMOL.EchoTheater.Selected", selection); else PlayerPrefs.DeleteKey("ANIMOL.EchoTheater.Selected");
            if (hadWallet) PlayerPrefs.SetString("WilliamNotebookUI.Wallet", wallet); else PlayerPrefs.DeleteKey("WilliamNotebookUI.Wallet");
            if (hadPrefs) PlayerPrefs.SetString("WilliamNotebookUI.Preferences", prefs); else PlayerPrefs.DeleteKey("WilliamNotebookUI.Preferences");
            foreach (var pair in clears) if (pair.Value.had) PlayerPrefs.SetInt(pair.Key, pair.Value.value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
        }
        static void VerifiedExitFixture(PuzzleDeviceSession state)
        {
            // This fixture exercises clear admission/navigation, not puzzle solving.
            typeof(PuzzleDeviceSession).GetProperty("Cell").SetValue(state, state.Map.exit);
            var collected = (HashSet<Vector2Int>)typeof(PuzzleDeviceSession).GetField("collected", Private).GetValue(state);
            collected.UnionWith(state.Map.bubbles);
            typeof(PuzzleDeviceSession).GetProperty("Won").SetValue(state, true);
        }
        static IEnumerator Capture(Canvas canvas, string name)
        {
            Directory.CreateDirectory(Evidence);
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldPlane = canvas.planeDistance;
            var cameraObject = new GameObject("Campaign UI capture", typeof(Camera)); var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 960; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.03f, .04f, .08f); camera.transform.position = new Vector3(0, 0, -10);
            var target = new RenderTexture(1080, 1920, 24); var oldTarget = RenderTexture.active;
            Texture2D image = null;
            try
            {
                camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                yield return null; Canvas.ForceUpdateCanvases(); yield return null; camera.Render();
                RenderTexture.active = target; image = new Texture2D(1080, 1920, TextureFormat.RGBA32, false); image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); image.Apply();
                File.WriteAllBytes(Evidence + "/" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane;
                RenderTexture.active = oldTarget; camera.targetTexture = null; target.Release(); Object.Destroy(target); Object.Destroy(image); Object.Destroy(cameraObject);
            }
        }
        [UnityTest] public IEnumerator EchoClearOffersNextThenOnlyMapAtTwenty()
        {
            host = new GameObject("Twenty-stage campaign fixture");
            var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera"; camera.transform.SetParent(host.transform);
            var puzzle = host.AddComponent<PuzzlePrototypeController>(); puzzle.secondsPerCell = .01f;
            var campaign = host.AddComponent<EchoTheaterCampaignController>(); campaign.enabled = false;
            campaign.mapSet = AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>("Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset");
            campaign.puzzle = puzzle;
            hudObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab"));
            var hud = hudObject.GetComponentInChildren<NotebookGameplayHud>(true); hud.reducedMotion = true;
            var gateway = host.AddComponent<NotebookEchoTheaterGateway>(); gateway.campaign = campaign;
            hud.BindGateway(gateway);
            campaign.notebookHud = hud; campaign.notebookGateway = gateway;
            campaign.SelectStage(18); yield return null;
            Assert.That(campaign.PlayableCount, Is.EqualTo(20));
            Assert.That(gateway.TryGetNextStage("ET-19", out _), Is.False, "A non-cleared stage cannot approve next entry.");
            VerifiedExitFixture(puzzle.Session);
            Assert.That(gateway.TryGetNextStage("ET-19", out string next), Is.True);
            Assert.That(next, Is.EqualTo("ET-20"));
            Assert.That(hud.ShowVerifiedClear(new NotebookGameplayClearSnapshot { verified = true, exitReached = true,
                stageId = "ET-19", stageLabel = "ET-19", collectedStars = puzzle.Session.Collected,
                requiredStars = puzzle.RuntimeMap.targetBubbles, hasNextStage = true, canEnterNext = true, nextStageId = next }), Is.True);
            yield return null;
            Assert.That(hud.nextButton.gameObject.activeSelf && hud.nextButton.interactable, Is.True);
            Assert.That(hud.nextLabel.text, Is.EqualTo("다음 스테이지로"));
            Assert.That(hud.clearMapButton.gameObject.activeSelf, Is.True);
            yield return Capture(hud.ownerCanvas, "ET19_Clear");
            hud.nextButton.onClick.Invoke(); yield return null;
            Assert.That(campaign.CurrentStageId, Is.EqualTo("ET-20"));
            Assert.That(puzzle.stage, Is.EqualTo(campaign.mapSet.maps[19].map));
            Assert.That(puzzle.Session.Cost, Is.Zero);
            VerifiedExitFixture(puzzle.Session);
            Assert.That(gateway.TryGetNextStage("ET-20", out _), Is.False);
            Assert.That(hud.ShowVerifiedClear(new NotebookGameplayClearSnapshot { verified = true, exitReached = true,
                stageId = "ET-20", stageLabel = "ET-20", collectedStars = puzzle.Session.Collected,
                requiredStars = puzzle.RuntimeMap.targetBubbles, hasNextStage = true, canEnterNext = true, nextStageId = "ET-21" }), Is.True);
            yield return null;
            Assert.That(hud.nextButton.gameObject.activeSelf, Is.False);
            Assert.That(hud.clearMapButton.gameObject.activeSelf && hud.clearMapButton.interactable, Is.True);
            bool approved = true;
            gateway.RequestNavigation(NotebookGameplayDestination.NextStage, "ET-20", "ET-21", "out-of-range", ok => approved = ok);
            Assert.That(approved, Is.False);
            hud.nextButton.onClick.Invoke(); yield return null;
            Assert.That(campaign.CurrentStageId, Is.EqualTo("ET-20"));
            campaign.SelectStage(20); Assert.That(campaign.CurrentStageId, Is.EqualTo("ET-20"));
            yield return Capture(hud.ownerCanvas, "ET20_Clear");
        }
        [UnityTest] public IEnumerator MapPreviewUsesRealNextStageAndStopsAtTwenty()
        {
            host = new GameObject("Twenty-stage map preview fixture");
            var puzzle = host.AddComponent<PuzzleController>();
            var mapSet = AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>("Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset");
            puzzle.stage = mapSet.maps[18].map;
            hudObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab"));
            var hud = hudObject.GetComponentInChildren<NotebookGameplayHud>(true); hud.reducedMotion = true;
            var gateway = host.AddComponent<NotebookMapDataPreviewGateway>();
            var bridge = host.AddComponent<NotebookMapDataPreviewBridge>();
            bridge.sourceMap = puzzle.stage; bridge.puzzle = puzzle; bridge.hud = hud; bridge.gateway = gateway; gateway.bridge = bridge;
            for (int i = 0; i < 30 && !bridge.Ready; i++) yield return null;
            Assert.That(bridge.Ready, Is.True);
            var prototype = host.GetComponent<PuzzlePrototypeController>();
            for (int i = 0; i < 8; i++)
            {
                Assert.That(prototype.RequestMove(Vector2Int.up), Is.True);
                while (prototype.IsMoving) yield return null;
            }
            for (int i = 0; i < 120 && !hud.clearOverlay.activeSelf; i++) yield return null;
            Assert.That(prototype.Session.Won, Is.True);
            Assert.That(hud.RequiredStars, Is.EqualTo(7));
            Assert.That(hud.clearOverlay.activeSelf && hud.nextButton.gameObject.activeSelf && hud.nextButton.interactable, Is.True);
            hud.nextButton.onClick.Invoke(); yield return null;
            Assert.That(bridge.StageId, Is.EqualTo("ET-20"));
            Assert.That(bridge.sourceMap, Is.EqualTo(mapSet.maps[19].map));
            Assert.That(prototype.Session.Cost, Is.Zero);
            VerifiedExitFixture(prototype.Session);
            for (int i = 0; i < 120 && !hud.clearOverlay.activeSelf; i++) yield return null;
            Assert.That(hud.clearOverlay.activeSelf, Is.True);
            Assert.That(hud.nextButton.gameObject.activeSelf, Is.False);
            Assert.That(hud.clearMapButton.gameObject.activeSelf, Is.True);
        }
        [UnityTest] public IEnumerator AllFourMapThemesHaveTwentyStarsAndRepositionedLinks()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Main/NotebookUI.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            root = Object.FindFirstObjectByType<NotebookRoot>(); Assert.That(root, Is.Not.Null);
            root.SetReducedMotion(true); root.OpenStages(); root.HideStageSheet();
            var catalog = NotebookCatalog.Parse(root.catalogJson);
            Assert.That(root.nodes.Length, Is.EqualTo(80)); Assert.That(root.edges.Length, Is.EqualTo(76));
            foreach (var theme in catalog.themes)
            {
                var stages = catalog.stages.Where(s => s.themeId == theme.id).ToArray();
                Assert.That(stages.Length, Is.EqualTo(20));
                Assert.That(stages.Select(s => s.order), Is.EquivalentTo(Enumerable.Range(1, 20)));
                Assert.That(catalog.artworks.Where(a => a.themeId == theme.id).Sum(a => a.stageIds.Length), Is.EqualTo(20));
                var middle = stages[10];
                root.mapScroll.content.anchoredPosition = new Vector2(0, middle.y - 960);
                root.mapScroll.StopMovement(); yield return null;
                yield return Capture(root.GetComponentInParent<Canvas>().rootCanvas, "Map_" + theme.id);
            }
        }
        [UnityTest] public IEnumerator StarwayNextStageKeepsSourceMapIdsAndStopsAtTwenty()
        {
            var project = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab")
                .GetComponentInChildren<NotebookProjectGateway>(true);
            host = new GameObject("Starway next-stage fixture");
            var puzzle = host.AddComponent<PuzzleController>();
            puzzle.stage = project.starwayMaps.Single(m => m && (m.name == "SW18" || m.name == "SW18_MapData"));
            hudObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab"));
            var hud = hudObject.GetComponentInChildren<NotebookGameplayHud>(true); hud.reducedMotion = true;
            var gateway = host.AddComponent<NotebookStarwaySessionGateway>(); gateway.campaignMaps = project.starwayMaps;
            var bridge = host.AddComponent<NotebookMapDataPreviewBridge>();
            bridge.sourceMap = puzzle.stage; bridge.stageIdOverride = "SW19"; bridge.puzzle = puzzle; bridge.hud = hud; bridge.gateway = gateway; gateway.bridge = bridge;
            for (int i = 0; i < 120 && (!bridge.Ready || host.GetComponent<PuzzlePrototypeController>().IsMoving); i++) yield return null;
            Assert.That(bridge.Ready, Is.True);
            var prototype = host.GetComponent<PuzzlePrototypeController>(); VerifiedExitFixture(prototype.Session);
            for (int i = 0; i < 120 && !hud.clearOverlay.activeSelf; i++) yield return null;
            Assert.That(hud.nextButton.gameObject.activeSelf && hud.nextButton.interactable, Is.True);
            hud.nextButton.onClick.Invoke(); yield return null;
            Assert.That(bridge.StageId, Is.EqualTo("SW20"));
            Assert.That(bridge.sourceMap.name, Is.EqualTo("SW19").Or.EqualTo("SW19_MapData"));
            Assert.That(NotebookProjectGateway.IsStarwayCleared("SW19"), Is.True);
            Assert.That(gateway.TryGetNextStage("SW20", out _), Is.False);
        }
    }
}
#endif
