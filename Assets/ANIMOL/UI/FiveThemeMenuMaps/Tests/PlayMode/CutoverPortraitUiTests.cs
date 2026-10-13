using System;
using System.Collections;
using System.IO;
using System.Linq;
using ANIMOL.PortraitArtV1;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ANIMOL.FiveThemeMenu.Tests
{
    public sealed class CutoverPortraitUiTests
    {
        [UnityTest]
        public IEnumerator LobbySettingsAndExistingHudKeepTheirSpritesAtPortraitResolution()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Lobby.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var setup = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t => t != null);
            setup.GetMethod("SelectFixedResolution").Invoke(null, new object[] { 1080, 1920, "ANIMOL cutover UI QA" });
            yield return null; yield return null;
            Assert.That(Screen.width, Is.EqualTo(1080));
            Assert.That(Screen.height, Is.EqualTo(1920));
            var entry = Object.FindFirstObjectByType<PortraitEntryController>();
            var navigation = entry.Navigation;
            var canvas = navigation.GetComponent<Canvas>();
            // Gameplay was archived; inspect the surviving authored HUD prefab without a new gameplay scene.
            var host = navigation.transform.Find("SafeArea/ScreenHost");
            if (host.Find("SC15_GameplayHud") == null)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/SC15_GameplayHud.prefab");
                Assert.That(prefab, Is.Not.Null);
                var hud = Object.Instantiate(prefab, host, false);
                hud.name = "SC15_GameplayHud";
                navigation.RebuildIndex();
            }
            foreach (var target in new[] { PortraitEntryController.ModeId, "SC13_Settings", "SC15_GameplayHud" })
            {
                Assert.That(navigation.Navigate(target, false), Is.True, target);
                yield return new WaitForSecondsRealtime(.3f);
                Canvas.ForceUpdateCanvases();
                foreach (var image in canvas.GetComponentsInChildren<Image>(false))
                    if (image.sprite != null) Assert.That(image.sprite.texture, Is.Not.Null, image.name);
                foreach (var raw in canvas.GetComponentsInChildren<RawImage>(false))
                    Assert.That(raw.texture, Is.Not.Null, raw.name);
                CaptureCanvas(canvas, target);
            }
            // HUD is inspected as the surviving UI view; no archived gameplay scene is launched.
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));
            navigation.Navigate(PortraitEntryController.ModeId, false);
#else
            Assert.Ignore("Editor Game View capture is only run in the Editor");
            yield break;
#endif
        }

        private static void CaptureCanvas(Canvas canvas, string name)
        {
            // Explicit Canvas rendering also works in batch tests, where WaitForEndOfFrame stalls.
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var previousDistance = canvas.planeDistance;
            var previousTarget = RenderTexture.active;
            var owner = new GameObject("Cutover UI capture camera");
            var camera = owner.AddComponent<Camera>();
            var target = new RenderTexture(1080, 1920, 24);
            var image = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                image.Apply();
                var pixels = image.GetPixels32();
                // Limited-palette pixel UI can legitimately contain fewer than 16 colors.
                Assert.That(pixels.Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count(), Is.GreaterThan(2), "Blank UI capture: " + name);
                Assert.That(pixels.Count(p => p.r > 250 && p.g < 5 && p.b > 250), Is.LessThan(pixels.Length / 100), "Pink material pixels: " + name);
                string folder = "Screenshots/TopdownCutover_20261013";
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, name.ToLowerInvariant() + "_1080x1920.png"), image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousDistance;
                RenderTexture.active = previousTarget;
                Object.Destroy(owner);
                target.Release(); Object.Destroy(target); Object.Destroy(image);
            }
        }
    }
}
