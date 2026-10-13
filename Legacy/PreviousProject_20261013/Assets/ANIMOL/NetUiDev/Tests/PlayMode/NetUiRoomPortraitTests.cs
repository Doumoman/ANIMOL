#if ANIMOL_NET_UI_DEV
using System;
using System.Collections;
using System.IO;
using System.Linq;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Animol.NetUiDev.Tests
{
    public sealed partial class NetUiAdmissionHttpTests
    {
        [UnityTest] public IEnumerator ExistingRoomPortraitAndSyntheticSafeAreaRender()
        {
            if (Environment.GetEnvironmentVariable("ANIMOL_NET02_TEST_GRAPHICS") != "1") Assert.Ignore("Run HTTP fixture with --graphics for portrait renders.");
            Assert.AreNotEqual(GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType);
            var controller = InstallRoom(); Confirm();
            yield return Until(() => navigation.CurrentScreenId == "SC06_MatchRoom" && !controller.IsBusy);
            for (int i = 2; i <= 4; i++) yield return Wait(RemoteJoin(i, service.CurrentRoom.RoomCode));
            yield return Until(() => service.CurrentRoom.Participants.Length == 4);
            var canvas = navigation.GetComponentInParent<Canvas>(); Assert.NotNull(canvas);
            var heading = Room.Body.content.Find("RoomIdentity").GetComponentsInChildren<ANIMOL.MissingUiV1.Project.MissingUtilityText>(true).First(t => t.name == "HeadingText");
            Assert.AreEqual("Approved room", heading.English);
            var safe = navigation.GetComponentInChildren<SafeAreaLayout>(); var rect = (RectTransform)safe.transform;
            var min = rect.anchorMin; var max = rect.anchorMax; bool safeEnabled = safe.enabled;
            var language = MobileControlPreferences.Language; var large = MobileControlPreferences.LargeText;
            bool hadLanguage = PlayerPrefs.HasKey("ANIMOL.M6.Language"), hadLarge = PlayerPrefs.HasKey("ANIMOL.M6.LargeText");
#if UNITY_EDITOR
            var gameViewType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView", true);
            var gameView = UnityEditor.EditorWindow.GetWindow(gameViewType);
            var selectedSize = gameViewType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var originalSize = selectedSize.GetValue(gameView);
#endif
            try
            {
                foreach (int height in new[] { 1920, 2400 })
                {
#if UNITY_EDITOR
                    var setup = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t => t != null);
                    setup.GetMethod("SelectFixedResolution").Invoke(null, new object[] { 1080, height, "NET02 portrait verification" });
#endif
                    for (int i = 0; i < 12; i++) yield return null;
                    safe.enabled = false;
                    rect.anchorMin = height == 2400 ? new Vector2(48f / 1080, 96f / height) : Vector2.zero;
                    rect.anchorMax = height == 2400 ? new Vector2(1 - 48f / 1080, 1 - 120f / height) : Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    MobileControlPreferences.Language = height == 2400 ? UiLanguage.English : UiLanguage.Korean;
                    MobileControlPreferences.LargeText = height == 2400;
                    foreach (bool bottom in new[] { false, true })
                    {
                        Room.Body.StopMovement(); Room.Body.verticalNormalizedPosition = bottom ? 0 : 1;
                        for (int i = 0; i < 12; i++) yield return null;
                        Canvas.ForceUpdateCanvases();
                        RenderCanvas(canvas, 1080, height, "SC06_" + height + (bottom ? "_bottom" : "_top"));
                        var corners = new Vector3[4]; ((RectTransform)Room.Back.transform).GetWorldCorners(corners);
                        foreach (var point in corners)
                        {
                            var local = rect.InverseTransformPoint(point);
                            Assert.That(local.x, Is.InRange(rect.rect.xMin - 1, rect.rect.xMax + 1));
                            Assert.That(local.y, Is.InRange(rect.rect.yMin - 1, rect.rect.yMax + 1));
                        }
                    }
                }
            }
            finally
            {
                MobileControlPreferences.Language = language; MobileControlPreferences.LargeText = large;
                if (!hadLanguage) PlayerPrefs.DeleteKey("ANIMOL.M6.Language"); if (!hadLarge) PlayerPrefs.DeleteKey("ANIMOL.M6.LargeText"); PlayerPrefs.Save();
                safe.enabled = safeEnabled; rect.anchorMin = min; rect.anchorMax = max; safe.Apply();
#if UNITY_EDITOR
                selectedSize.SetValue(gameView, originalSize); gameView.Repaint();
#endif
            }
        }
        private static void RenderCanvas(Canvas canvas, int width, int height, string name)
        {
            // Explicit render avoids WaitForEndOfFrame, which does not run in batch tests.
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldDistance = canvas.planeDistance;
            var oldActive = RenderTexture.active;
            var cameraObject = new GameObject("NET02 test capture camera"); var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(width, height, 24); var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                string directory = Path.Combine(Application.dataPath, "../Logs/NET02Task06/Captures"); Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                RenderTexture.active = oldActive; Object.Destroy(cameraObject); target.Release(); Object.Destroy(target); Object.Destroy(image);
            }
        }
    }
}
#endif
