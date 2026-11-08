#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.UI.Crayon;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using William.NotebookUI;

namespace ANIMOL.Tests
{
    public sealed class CrayonUIPlayTests
    {
        const string ScenePath = "Assets/ANIMOL/Scenes/Battle/ANIMOL_StarRain_Battle.unity";
        const string Captures = "Docs/Validation/CrayonUI";
        const string Wallet = "WilliamNotebookUI.Wallet", Preferences = "WilliamNotebookUI.Preferences";
        bool hadWallet, hadPreferences, previousMotion;
        string wallet, preferences;
        [SetUp] public void PreserveSettings()
        {
            hadWallet = PlayerPrefs.HasKey(Wallet); wallet = PlayerPrefs.GetString(Wallet);
            hadPreferences = PlayerPrefs.HasKey(Preferences); preferences = PlayerPrefs.GetString(Preferences);
            previousMotion = CrayonUIMotion.GlobalReducedMotion;
        }
        [UnityTearDown] public IEnumerator RestoreSettings()
        {
            var notebook = Object.FindFirstObjectByType<NotebookRoot>();
            if (notebook) Object.Destroy(notebook.GetComponentInParent<Canvas>().rootCanvas.gameObject);
            yield return null;
            if (hadWallet) PlayerPrefs.SetString(Wallet, wallet); else PlayerPrefs.DeleteKey(Wallet);
            if (hadPreferences) PlayerPrefs.SetString(Preferences, preferences); else PlayerPrefs.DeleteKey(Preferences);
            PlayerPrefs.Save(); CrayonUIMotion.SetGlobalReducedMotion(previousMotion);
        }
        static IEnumerator LoadBattle()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            Assert.That(Object.FindFirstObjectByType<BattleCrayonArtPreview>(), Is.Not.Null);
        }

        [UnityTest] public IEnumerator ExistingNotebookNavigationAndReducedMotionWorkWithTheStandalonePreview()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Main/NotebookUI.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var notebook = Object.FindFirstObjectByType<NotebookRoot>();
            Assert.That(notebook, Is.Not.Null);
            notebook.SetReducedMotion(true);
            yield return Capture("Main_Before", 1080, 1920, new Rect(0, 0, 1080, 1920));
            notebook.OpenStages(); yield return null;
            Assert.That(notebook.stageScreen.activeInHierarchy, Is.True);
            yield return Capture("Existing_Constellation_Map", 1080, 1920, new Rect(0, 0, 1080, 1920));
            notebook.OpenMain(); yield return null;
            Assert.That(notebook.mainScreen.activeInHierarchy, Is.True);
            yield return Capture("Main_After", 1080, 1920, new Rect(0, 0, 1080, 1920));
            yield return LoadBattle();
            var preview = Object.FindFirstObjectByType<BattleCrayonArtPreview>();
            Assert.That(preview.motion.ReducedMotion, Is.True);
            foreach (var binding in preview.motion.bindings) Assert.That(binding.image.material, Is.SameAs(binding.still));
            yield return Capture("Battle_Start_Still", 1080, 1920, new Rect(0, 0, 1080, 1920));
            preview.SetReducedMotion(false);
            foreach (var binding in preview.motion.bindings) Assert.That(binding.image.material, Is.SameAs(binding.animated));
            yield return Capture("Battle_Start", 1080, 1920, new Rect(0, 0, 1080, 1920));

            Click(preview.pauseButton, false);
            Assert.That(preview.CurrentScreen, Is.EqualTo(BattleCrayonArtPreview.Screen.Start), "Disabled pause accepted input.");
            Assert.That(preview.pauseButton.GetComponent<CanvasGroup>().alpha, Is.LessThan(.5f));
            var press = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(preview.startButton.gameObject, press, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(preview.startButton.targetGraphic.canvasRenderer.GetColor().r, Is.LessThan(.9f));
            yield return Capture("Battle_Start_Pressed", 1080, 1920, new Rect(0, 0, 1080, 1920));
            ExecuteEvents.Execute(preview.startButton.gameObject, press, ExecuteEvents.pointerUpHandler);
            Click(preview.startButton); yield return null;
            Assert.That(preview.CurrentScreen, Is.EqualTo(BattleCrayonArtPreview.Screen.Hud));
            yield return Capture("Battle_HUD", 1080, 1920, new Rect(0, 0, 1080, 1920));
            float timeScale = Time.timeScale;
            Click(preview.pauseButton); yield return null;
            Assert.That(preview.CurrentScreen, Is.EqualTo(BattleCrayonArtPreview.Screen.Pause));
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
            yield return Capture("Battle_Pause", 1080, 1920, new Rect(0, 0, 1080, 1920));
            Click(preview.resumeButton); yield return null;
            Click(preview.augmentButton); yield return null;
            Assert.That(preview.CurrentScreen, Is.EqualTo(BattleCrayonArtPreview.Screen.Augment));
            Click(preview.augmentSelectButton); yield return null;
            Assert.That(preview.AugmentSelected && preview.augmentSelection.activeInHierarchy, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(preview.augmentSelectButton.gameObject));
            yield return Capture("Battle_Mint_Augment_Selected", 1080, 1920, new Rect(0, 0, 1080, 1920));
            Click(preview.augmentBackButton); yield return null;
            Click(preview.resultsPreviewButton); yield return null;
            Assert.That(preview.CurrentScreen, Is.EqualTo(BattleCrayonArtPreview.Screen.Results));
            yield return Capture("Battle_Results", 1080, 1920, new Rect(0, 0, 1080, 1920));
            Click(preview.resultRetryButton); yield return null;
            Assert.That(preview.CurrentScreen, Is.EqualTo(BattleCrayonArtPreview.Screen.Start));
            Assert.That(preview.isolHealthFill.fillAmount, Is.EqualTo(1f));
            Assert.That(preview.shieldCount.text, Is.EqualTo("0"));
            Assert.That(preview.BlocksInput || preview.motion.BlocksInput, Is.False);
            Assert.That(Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<ANIMOL.Puzzle.PuzzlePrototypeController>(FindObjectsSortMode.None), Is.Empty);
        }

        [UnityTest] public IEnumerator PortraitCompositionsFitTallNarrowAndShortSafeAreas()
        {
            yield return LoadBattle();
            var preview = Object.FindFirstObjectByType<BattleCrayonArtPreview>();
            preview.SetReducedMotion(true);
            yield return Capture("Battle_Start_Tall_1080x2340", 1080, 2340, new Rect(0, 84, 1080, 2156), true);
            yield return Capture("Battle_Start_Narrow_720x1280", 720, 1280, new Rect(24, 32, 672, 1200), true);
            yield return Capture("Battle_Start_Short_1080x1600", 1080, 1600, new Rect(24, 80, 1032, 1420), true);
        }

        static void Click(Button button, bool requireInteractable = true)
        {
            Assert.That(button.gameObject.activeInHierarchy, Is.True, button.name);
            if (requireInteractable) Assert.That(button.IsInteractable(), Is.True, button.name);
            var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var data = new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(camera, ((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), button.name);
            var target = hits[0].gameObject;
            Assert.That(target.transform == button.transform || target.transform.IsChildOf(button.transform), Is.True,
                button.name + " is covered by " + target.name);
            ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerClickHandler);
        }

        // Export the actual uGUI composition through a camera. Synthetic safe rects inspect layout
        // bounds without changing the existing NotebookCanvasLayout or claiming device notch tests.
        static IEnumerator Capture(string name, int width, int height, Rect safe, bool inspectBounds = false)
        {
            Directory.CreateDirectory(Captures);
            var layout = Object.FindFirstObjectByType<NotebookCanvasLayout>();
            var canvas = layout.GetComponent<Canvas>().rootCanvas;
            var camera = Camera.main;
            if (!camera) camera = new GameObject("Crayon capture camera", typeof(Camera)).GetComponent<Camera>();
            var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera;
            var previousTarget = camera.targetTexture; var previousSize = camera.orthographicSize;
            bool previousOrtho = camera.orthographic, previousLayout = layout.enabled;
            var scaler = canvas.GetComponent<CanvasScaler>(); float previousScale = scaler.scaleFactor;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            try
            {
                layout.enabled = false;
                camera.orthographic = true; camera.orthographicSize = height * .5f; camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
                float scale = Mathf.Min(safe.width / 1080f, safe.height / 1500f);
                scaler.scaleFactor = scale; canvas.scaleFactor = scale;
                layout.design.anchorMin = layout.design.anchorMax = layout.design.pivot = new Vector2(.5f, .5f);
                layout.design.sizeDelta = new Vector2(1080, safe.height / scale);
                layout.design.anchoredPosition = (safe.center - new Vector2(width, height) * .5f) / scale;
                yield return null; Canvas.ForceUpdateCanvases(); yield return null;
                camera.Render();
                Save(target, Captures + "/" + name + ".png");
                if (inspectBounds)
                {
                    var corners = new Vector3[4];
                    foreach (var button in canvas.GetComponentsInChildren<Button>().Where(b => b.gameObject.activeInHierarchy))
                    {
                        ((RectTransform)button.transform).GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            var point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                            Assert.That(point.x, Is.InRange(safe.xMin - 1f, safe.xMax + 1f), name + " " + button.name);
                            Assert.That(point.y, Is.InRange(safe.yMin - 1f, safe.yMax + 1f), name + " " + button.name);
                        }
                    }
                }
            }
            finally
            {
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera;
                camera.targetTexture = previousTarget; camera.orthographic = previousOrtho; camera.orthographicSize = previousSize;
                scaler.scaleFactor = previousScale; layout.enabled = previousLayout; layout.RefreshLayout();
                Object.Destroy(target);
            }
        }
        static void Save(RenderTexture target, string path)
        {
            var previous = RenderTexture.active;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.Destroy(pixels); }
        }

        [UnityTest] public IEnumerator BothStencilMaskAndRectMaskClipThePrepaintedFrames()
        {
            yield return LoadBattle();
            var artRoot = Object.FindFirstObjectByType<BattleCrayonArtPreview>(); artRoot.gameObject.SetActive(false);
            foreach (bool stencil in new[] { true, false })
            {
                var host = new GameObject("Mask validation", typeof(RectTransform), typeof(Canvas)); host.layer = 31;
                var cameraObject = new GameObject("Mask camera", typeof(Camera));
                var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 256;
                camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear; camera.cullingMask = 1 << 31;
                var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
                var canvas = host.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                var mask = new GameObject("Clip", typeof(RectTransform)); mask.layer = 31; mask.transform.SetParent(host.transform, false);
                ((RectTransform)mask.transform).sizeDelta = new Vector2(100, 100);
                if (stencil) { mask.AddComponent<Image>(); mask.AddComponent<Mask>().showMaskGraphic = false; }
                else mask.AddComponent<RectMask2D>();
                var frame = new GameObject("Masked frame", typeof(RectTransform), typeof(Image)); frame.layer = 31; frame.transform.SetParent(mask.transform, false);
                var rect = (RectTransform)frame.transform; rect.sizeDelta = new Vector2(128, 128); rect.anchoredPosition = new Vector2(40, 0);
                var image = frame.GetComponent<Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/BattleCrayonUI/Sprites/Frame_ButtonSquare_A.png");
                image.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/ANIMOL/BattleCrayonUI/Materials/M_Frame_ButtonSquare_Still.mat");
                image.type = Image.Type.Sliced; image.raycastTarget = false;
                try
                {
                    yield return null; Canvas.ForceUpdateCanvases(); yield return null; camera.Render();
                    Directory.CreateDirectory(Captures); Save(target, Captures + (stencil ? "/Mask_Stencil.png" : "/Mask_RectMask2D.png"));
                    var previous = RenderTexture.active; RenderTexture.active = target;
                    var pixels = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                    try
                    {
                        pixels.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); pixels.Apply();
                        var colors = pixels.GetPixels32(); int inside = 0, outside = 0;
                        for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++)
                            if (colors[y * 512 + x].a > 32)
                            { if (x < 204 || x > 308 || y < 204 || y > 308) outside++; else inside++; }
                        Assert.That(inside, Is.GreaterThan(20), stencil ? "Stencil empty" : "RectMask empty");
                        Assert.That(outside, Is.Zero, stencil ? "Stencil leaked" : "RectMask leaked");
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(pixels); }
                }
                finally { Object.Destroy(host); Object.Destroy(cameraObject); Object.Destroy(target); }
                yield return null;
            }
        }
    }
}
#endif
