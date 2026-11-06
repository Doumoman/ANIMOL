using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Animol.Starway;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace WilliamParadox.Story.Tests.PlayMode
{
    public sealed class RanchArtIntegrationPlayTests
    {
        private const string Output = "Docs/Validation/StarwayRanchUi";
        private StoryRunner runner;
        private string priorSave;
        private bool hadSave;
        private static string SaveKey => StorySaveStore.Key("william-paradox.ranch.dialogue-preview");
        [SetUp] public void PreserveSave() { hadSave = PlayerPrefs.HasKey(SaveKey); priorSave = PlayerPrefs.GetString(SaveKey); }
        [TearDown] public void StopSaving()
        {
            if (runner != null) runner.persistProgress = false;
            if (hadSave) PlayerPrefs.SetString(SaveKey, priorSave); else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        [UnityTest] public IEnumerator DistanceStaysAuthoredAcrossPauseSkipDisableReentryAndReducedMotion()
        {
#if UNITY_EDITOR
            var setup = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Presentation.Editor.PortraitGameView")).First(t => t != null);
            setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] {1080,1920,"Ranch art QA"});
            EditorSceneManager.LoadSceneInPlayMode("Assets/WilliamParadoxStoryV14/Scenes/RanchStoryPreview.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            runner = UnityEngine.Object.FindFirstObjectByType<StoryRunner>();
            runner.persistProgress = false; runner.restoreOnStart = false;
            yield return null; yield return null;
            runner.StartStory(false);
            typeof(StoryRunner).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(runner, new object[] {true});
            runner.TogglePause();
            Assert.That(runner.IsPaused, Is.True);
            var stage = runner.view.ranchStage;
            Assert.That(stage, Is.Not.Null); Assert.That(stage.BlocksInput, Is.False);
            Directory.CreateDirectory(Output);
            Settle(); yield return Capture("R01");
            foreach (var scene in Enumerable.Range(1, 15).Select(n => "R" + n.ToString("00")))
            {
                var node = runner.Package.nodes.First(n => n.sceneId == scene);
                Assert.That(runner.JumpTo(node.id), Is.True, scene);
                Assert.That(runner.view.artImage.sprite, Is.Not.Null, scene);
            }
            Assert.That(runner.JumpTo("R08_B1_L001"), Is.True);
            stage.Sample("foreground_cross", 0.15f, false);
            Assert.That(stage.runnerImage.enabled, Is.True);
            Assert.That(stage.runnerImage.color.a, Is.EqualTo(1));
            stage.Sample("foreground_cross", 1, false);
            Assert.That(stage.runnerImage.enabled, Is.False);
            Assert.That(runner.view.artImage.sprite.name, Is.EqualTo("R08_FrontPlate"));
            Assert.That(runner.JumpTo("R12_B1_L001"), Is.True);
            stage.Sample("reactions", 0.5f, false);
            Assert.That(stage.isolReaction.enabled && stage.toriReaction.enabled, Is.True);
            Assert.That(runner.JumpTo("R13_007"), Is.True); // Repair, after all five scolding lines.
            Assert.That(runner.CurrentNode.artId, Is.EqualTo("R13B"));
            Settle(); yield return Capture("R13B");

            runner.SetReducedMotion(false); Assert.That(runner.JumpTo("R15_B1_L001"), Is.True);
            Settle();
            stage.Sample("distance_increases", 0, false);
            var handSize = stage.hand.rectTransform.sizeDelta;
            var handPosition = stage.hand.rectTransform.anchoredPosition;
            Assert.That(stage.friends.rectTransform.localScale.x, Is.EqualTo(0.94f).Within(0.001f));
            yield return Capture("R15_start");
            stage.Sample("distance_increases", 0.5f, false);
            float before = stage.DistanceProgress;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(stage.DistanceProgress, Is.EqualTo(before));
            stage.Sample("distance_increases", 1, false);
            Assert.That(stage.friends.rectTransform.localScale.x, Is.EqualTo(0.56f).Within(0.001f));
            Assert.That(stage.hand.rectTransform.sizeDelta, Is.EqualTo(handSize));
            Assert.That(stage.hand.rectTransform.anchoredPosition, Is.EqualTo(handPosition));
            Assert.That(stage.friends.color, Is.EqualTo(Color.white));
            Assert.That(stage.farStars.material.GetFloat("_Progress"), Is.EqualTo(1));
            Assert.That(stage.nearStars.material.GetFloat("_Progress"), Is.EqualTo(1));
            yield return Capture("R15_far");
            runner.enabled = false;
            Assert.That(stage.composition.gameObject.activeSelf, Is.False);
            runner.enabled = true;
            Assert.That(stage.composition.gameObject.activeSelf, Is.True);
            Assert.That(stage.DistanceProgress, Is.EqualTo(1));
            runner.SetReducedMotion(true);
            Assert.That(stage.farStars.material.GetFloat("_ReducedMotion"), Is.EqualTo(1));
            Assert.That(runner.HasFlag("PLAYER_BLOCKS_LAMP"), Is.False);
            stage.Present(new StoryNode { id = "R16_B1_L001", sceneId = "R16", artId = "R15" }, true, true);
            Assert.That(stage.DistanceProgress, Is.EqualTo(1)); Assert.That(stage.ReturnBlend, Is.Zero);
            stage.Present(new StoryNode { id = "R16_B4_L001", sceneId = "R16", artId = "R16" }, false, false);
            stage.Sample("distance_returns_and_grasp", 0.5f, false);
            Assert.That(stage.ReturnBlend, Is.EqualTo(0.5f));
            stage.Sample("distance_returns_and_grasp", 1, false);
            Assert.That(stage.composition.gameObject.activeSelf, Is.False);
            Assert.That(stage.farStars.material == null || !stage.farStars.material.HasProperty("_Visibility") || stage.farStars.material.GetFloat("_Visibility") == 0, Is.True);
            Assert.That(runner.ValidationErrors, Is.Empty);
            // Optional fragments leave story progress, flags and reduced-motion setting intact.
            var lore = runner.GetComponentInChildren<RanchOptionalLore>(true);
            var flags = runner.Package.packageId + runner.CurrentNodeId;
            lore.Open("LORE_CLOCK"); Assert.That(lore.panel.activeSelf, Is.True);
            lore.nextButton.onClick.Invoke(); lore.Close();
            Assert.That(runner.Package.packageId + runner.CurrentNodeId, Is.EqualTo(flags));
#else
            Assert.Ignore("Editor story scene required."); yield break;
#endif
        }
        private void Settle()
        {
            ((StoryCuePlayer)typeof(StoryRunner).GetField("cuePlayer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(runner)).FinishAll();
        }
        private IEnumerator Capture(string name)
        {
            runner.view.dialogueBridge.dialogue.SetReducedMotion(true);
            Canvas.ForceUpdateCanvases(); yield return null; yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Output + "/" + name + ".png", texture.EncodeToPNG()); UnityEngine.Object.Destroy(texture);
        }

        [UnityTest] public IEnumerator ShaderClipsThroughBothUiMaskTypesAndFadesEntireField()
        {
            foreach (bool stencil in new[] {false, true})
            {
                var root = new GameObject("Overlay Mask Test");
                var cameraObject = new GameObject("Overlay Test Camera");
                var camera = cameraObject.AddComponent<Camera>();
                var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
                target.Create(); camera.targetTexture = target; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear; camera.cullingMask = 1 << 29; camera.transform.position = new Vector3(0, 0, -10);
                var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                var maskRect = new GameObject("Mask", typeof(RectTransform)).GetComponent<RectTransform>(); maskRect.SetParent(root.transform, false);
                maskRect.anchorMin = maskRect.anchorMax = new Vector2(0.5f, 0.5f); maskRect.sizeDelta = new Vector2(128, 128);
                if (stencil) { maskRect.gameObject.AddComponent<Image>(); maskRect.gameObject.AddComponent<Mask>().showMaskGraphic = false; }
                else maskRect.gameObject.AddComponent<RectMask2D>();
                var rect = new GameObject("Stars", typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(maskRect, false);
                rect.sizeDelta = new Vector2(256, 256);
                var overlay = rect.gameObject.AddComponent<RanchConstellationOverlay>();
                foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 29;
                overlay.SetCue(0.5f, 0, Vector2.zero, 1, false);
                yield return null; Canvas.ForceUpdateCanvases(); camera.Render();
                var pixels = Read(target); int visible = 0, outside = 0;
                for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                    if (pixels[y * 256 + x].a > 2) { visible++; if (x < 63 || x > 192 || y < 63 || y > 192) outside++; }
                Assert.That(visible, Is.GreaterThan(10), stencil ? "Mask" : "RectMask2D");
                Assert.That(outside, Is.Zero, stencil ? "Mask" : "RectMask2D");
                overlay.SetCue(1, 1, Vector2.one, 1, false); Canvas.ForceUpdateCanvases(); camera.Render();
                Assert.That(Read(target).Count(c => c.a > 2), Is.Zero, "Return blend must fade the whole star layer.");
                rect.gameObject.SetActive(false);
                var panelRect = new GameObject("Neon Panel", typeof(RectTransform)).GetComponent<RectTransform>();
                panelRect.SetParent(maskRect, false); panelRect.sizeDelta = new Vector2(256, 256); panelRect.gameObject.layer = 29;
                panelRect.gameObject.AddComponent<StoryNeonPanel>();
                var group = panelRect.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0.5f;
                yield return null; Canvas.ForceUpdateCanvases(); camera.Render(); pixels = Read(target); outside = 0;
                for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                    if (pixels[y * 256 + x].a > 2 && (x < 63 || x > 192 || y < 63 || y > 192)) outside++;
                Assert.That(outside, Is.Zero, "Neon panel mask boundary");
                Assert.That(pixels[128 * 256 + 128].a, Is.InRange(60, 140), "Neon panel respects CanvasGroup alpha");
                group.alpha = 0; Canvas.ForceUpdateCanvases(); camera.Render();
                Assert.That(Read(target).Count(c => c.a > 2), Is.Zero);
                UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(cameraObject); target.Release(); UnityEngine.Object.Destroy(target);
                yield return null;
            }
        }
        private static Color32[] Read(RenderTexture target)
        {
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); texture.Apply();
            var result = texture.GetPixels32(); UnityEngine.Object.Destroy(texture); RenderTexture.active = previous; return result;
        }
    }
}
