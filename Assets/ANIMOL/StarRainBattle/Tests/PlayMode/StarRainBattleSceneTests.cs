#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.StarRainBattle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ANIMOL.StarRainBattle.Tests
{
    public sealed class StarRainBattleSceneTests
    {
        const string ScenePath = "Assets/ANIMOL/StarRainBattle/Scenes/StarRainBossBattle.unity";
        const string Output = "Docs/Validation/StarRainBossBattle";
        StarRainBattleController battle;

        [UnitySetUp] public IEnumerator LoadBattle()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            battle = Object.FindFirstObjectByType<StarRainBattleController>();
            Assert.That(battle, Is.Not.Null);
            Canvas.ForceUpdateCanvases();
        }

        static Vector2 ScreenPoint(RectTransform rect, Vector2 local, Camera camera = null)
            => RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(local));

        PointerEventData Pointer(Vector2 screen, GameObject expected, bool raycast = true)
        {
            var data = new PointerEventData(EventSystem.current) { position = screen, pointerId = 17 };
            var raycaster = battle.GetComponentInParent<GraphicRaycaster>();
            if (raycast)
            {
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
                Assert.That(hits, Is.Not.Empty);
                Assert.That(hits[0].gameObject, Is.SameAs(expected));
                data.pointerPressRaycast = hits[0];
            }
            else data.pointerPressRaycast = new RaycastResult { gameObject = expected, module = raycaster };
            return data;
        }
        void Click(Button button)
        {
            var data = Pointer(ScreenPoint(button.GetComponent<RectTransform>(), Vector2.zero), button.gameObject);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
        Vector2 NodeScreen(int index, Camera camera = null)
        {
            var p = battle.Model.Nodes[index].Position;
            var local = new Vector2(battle.Board.rect.xMin + p.x / 400f * battle.Board.rect.width,
                battle.Board.rect.yMax - p.y / 620f * battle.Board.rect.height);
            return ScreenPoint(battle.Board, local, camera);
        }
        void Tap(int index, Camera camera = null, bool raycast = true)
        {
            var data = Pointer(NodeScreen(index, camera), battle.InputSurface.gameObject, raycast);
            battle.OnPointerDown(data); battle.OnPointerUp(data);
        }
        void Drag(int from, int to)
        {
            var data = Pointer(NodeScreen(from), battle.InputSurface.gameObject);
            battle.OnPointerDown(data); data.position = NodeScreen(to);
            battle.OnDrag(data); battle.OnPointerUp(data);
        }
        void Quiet()
        {
            battle.Model.TestSetSpawnTimes(float.PositiveInfinity, float.PositiveInfinity);
            battle.Model.TestSetNode(0, new Vector2(80, 310));
            battle.Model.TestSetNode(1, new Vector2(320, 310));
            battle.Model.TestSetNode(2, new Vector2(60, 170));
            battle.Model.TestSetNode(3, new Vector2(340, 170));
            battle.Model.TestSetNode(4, new Vector2(60, 450));
        }
        static void Capture(string name)
        {
            Directory.CreateDirectory(Output);
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
            Object.Destroy(image);
        }
        void AssertVisibleArtIsStill()
        {
            var images = battle.GetComponentInParent<Canvas>().GetComponentsInChildren<Image>()
                .Where(image => image.isActiveAndEnabled && image.material && image.material.HasProperty("_Motion"));
            Assert.That(images, Is.Not.Empty);
            foreach (var image in images)
                Assert.That(image.material.GetFloat("_Motion"), Is.Zero, image.name);
        }

        [UnityTest] public IEnumerator PointerFlowPauseRetryAndRealCoreOutcomes()
        {
            Assert.That(battle.Model.Phase, Is.EqualTo(BattlePhase.Ready));
            Assert.That(battle.Nodes, Has.Length.EqualTo(5));
            Assert.That(battle.ShotViews, Has.Length.EqualTo(24));
            Assert.That(battle.FxViews, Has.Length.EqualTo(24));
            Assert.That(battle.BlocksInput, Is.False);
            yield return new WaitForEndOfFrame(); Capture("Ready");
            Click(battle.StartButton); Quiet(); yield return null;
            Assert.That(battle.Model.Phase, Is.EqualTo(BattlePhase.Running));
            Tap(0); Tap(1);
            Assert.That(battle.Model.HasLink, Is.True);
            Drag(2, 3); Assert.That(battle.Model.LinkA, Is.EqualTo(2));
            Assert.That(battle.Model.LinkB, Is.EqualTo(3));
            var seed = battle.Model.Seed;
            var time = battle.Model.Time; var expires = battle.Model.LinkExpires;
            var position = battle.Model.Nodes[0].Position;
            var scale = Time.timeScale;
            Click(battle.PauseButton);
            yield return new WaitForSeconds(.15f);
            Assert.That(battle.Model.Phase, Is.EqualTo(BattlePhase.Paused));
            Assert.That(battle.Model.Time, Is.EqualTo(time));
            Assert.That(battle.Model.LinkExpires, Is.EqualTo(expires));
            Assert.That(battle.Model.Nodes[0].Position, Is.EqualTo(position));
            Assert.That(Time.timeScale, Is.EqualTo(scale));
            yield return new WaitForEndOfFrame(); Capture("Paused");
            Click(battle.StartButton); yield return new WaitForSeconds(.6f);
            Assert.That(battle.Model.HasLink, Is.False);
            battle.Replay(); Quiet(); yield return null;
            Assert.That(battle.Model.Seed, Is.EqualTo(seed));
            for (var i = 0; i < StarRainBattleModel.CurseMaxHP; i++)
                battle.Model.TestAddMeteor(MeteorTeam.Isol, new Vector2(200, 120), new Vector2(200, 76), new Vector2(200, 30), .1f);
            yield return new WaitForSeconds(.2f);
            Assert.That(battle.Model.Phase, Is.EqualTo(BattlePhase.Won));
            Assert.That(battle.CurseHpText.text, Is.EqualTo("0"));
            yield return new WaitForEndOfFrame(); Capture("Victory_ControlledCoreHits");
            Click(battle.ReplayButton); Quiet(); yield return null;
            Assert.That(battle.Model.Seed, Is.EqualTo(seed));
            Assert.That(battle.Model.IsolHP, Is.EqualTo(7)); Assert.That(battle.Model.CurseHP, Is.EqualTo(11));
            for (var i = 0; i < StarRainBattleModel.IsolMaxHP; i++)
                battle.Model.TestAddMeteor(MeteorTeam.Curse, new Vector2(200, 500), new Vector2(200, 544), new Vector2(200, 580), .1f);
            yield return new WaitForSeconds(.2f);
            Assert.That(battle.Model.Phase, Is.EqualTo(BattlePhase.Lost));
            Assert.That(battle.IsolHpText.text, Is.EqualTo("0"));
            yield return new WaitForEndOfFrame(); Capture("Defeat_ControlledCoreHits");
            Click(battle.FreshButton); Quiet();
            Assert.That(battle.Model.Seed, Is.Not.EqualTo(seed));
            Assert.That(battle.Model.Phase, Is.EqualTo(BattlePhase.Running));
        }

        [UnityTest] public IEnumerator PoolingBasicAbsorptionPrismAndReducedMotion()
        {
            Click(battle.StartButton); Quiet(); yield return null;
            var objects = battle.GetComponentsInChildren<Transform>(true).Length;
            Tap(0); Tap(1);
            battle.Model.TestAddMeteor(MeteorTeam.Curse, new Vector2(200, 260), new Vector2(200, 310), new Vector2(200, 360), .2f);
            battle.Model.TestAddMeteor(MeteorTeam.Isol, new Vector2(220, 360), new Vector2(220, 310), new Vector2(220, 260), .2f);
            yield return new WaitForSeconds(.25f);
            Assert.That(battle.Model.BlockedEnemy, Is.EqualTo(1)); Assert.That(battle.Model.BlockedFriend, Is.EqualTo(1));
            Click(battle.AugmentButton); Quiet(); yield return null;
            Assert.That(battle.Model.Augmented, Is.True);
            Tap(0); Tap(1);
            battle.Model.TestAddMeteor(MeteorTeam.Curse, new Vector2(200, 260), new Vector2(200, 310), new Vector2(200, 360), .2f);
            battle.Model.TestAddMeteor(MeteorTeam.Isol, new Vector2(220, 360), new Vector2(220, 310), new Vector2(220, 260), .2f);
            yield return null;
            Assert.That(battle.ShotViews.Count(v => v.Head.enabled), Is.GreaterThan(0));
            battle.SetReducedMotion(true);
            AssertVisibleArtIsStill();
            var time = battle.Model.Time;
            yield return new WaitForSeconds(.25f);
            Assert.That(battle.Model.Time, Is.GreaterThan(time));
            Assert.That(battle.Model.BlockedEnemy, Is.Zero); Assert.That(battle.Model.BlockedFriend, Is.Zero);
            Assert.That(battle.Model.Meteors.Any(m => m.Team == MeteorTeam.Reflected), Is.True);
            AssertVisibleArtIsStill();
            yield return new WaitForEndOfFrame(); Capture("Prism_StillArt");
            yield return new WaitForSeconds(1.25f);
            Assert.That(battle.Model.CurseHP, Is.EqualTo(10));
            battle.Model.TestAddMeteor(MeteorTeam.Curse, new Vector2(200, 100), new Vector2(200, 200), new Vector2(200, 250), 1);
            yield return null;
            AssertVisibleArtIsStill();
            Assert.That(battle.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objects));
            Assert.That(battle.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(battle.GetComponentsInChildren<Collider>(true), Is.Empty);
        }

        [UnityTest] public IEnumerator SafeAreaAndPointerCoordinatesAtFourRenderSizes()
        {
            battle.StartOrResume(); Quiet(); yield return null;
            var canvas = battle.GetComponentInParent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler.enabled, Is.True);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080, 1920)));
            var safe = canvas.GetComponentInChildren<StarRainBattleSafeArea>(); safe.enabled = false;
            var cameraObject = new GameObject("Battle layout capture camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0, 0, -10);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var cases = new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(720, 1280), new Vector2Int(1920, 1080) };
            try
            {
                foreach (var size in cases)
                {
                    var texture = new RenderTexture(size.x, size.y, 24); camera.targetTexture = texture;
                    var bounds = size.y > size.x ? new Rect(0, 60, size.x, size.y - 140) : new Rect(0, 0, size.x, size.y);
                    try
                    {
                        Canvas.ForceUpdateCanvases(); yield return null;
                        safe.Apply(bounds, size); Canvas.ForceUpdateCanvases();
                        foreach (var rect in new[] { battle.Board, battle.PauseButton.GetComponent<RectTransform>(),
                            battle.AugmentButton.GetComponent<RectTransform>(), battle.IsolHpText.rectTransform, battle.CurseHpText.rectTransform,
                            battle.Overlay.GetComponent<RectTransform>(), battle.OverlayTitle.rectTransform,
                            battle.StartButton.GetComponent<RectTransform>(), battle.ReplayButton.GetComponent<RectTransform>(),
                            battle.FreshButton.GetComponent<RectTransform>() })
                        {
                            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                            foreach (var corner in corners)
                            {
                                var screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
                                Assert.That(screen.x, Is.InRange(bounds.xMin - 1f, bounds.xMax + 1f));
                                Assert.That(screen.y, Is.InRange(bounds.yMin - 1f, bounds.yMax + 1f));
                            }
                        }
                        battle.Replay(); Quiet(); yield return null;
                        Tap(0, camera, false); Tap(1, camera, false);
                        Assert.That(battle.Model.LinkA, Is.EqualTo(0)); Assert.That(battle.Model.LinkB, Is.EqualTo(1));
                        Assert.That(battle.Link.type, Is.EqualTo(Image.Type.Sliced));
                        Assert.That(battle.Link.sprite.border, Is.EqualTo(Vector4.one * 24));
                        camera.Render(); var previous = RenderTexture.active; RenderTexture.active = texture;
                        var capture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
                        try
                        {
                            capture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); capture.Apply();
                            Directory.CreateDirectory(Output); File.WriteAllBytes(Output + "/Layout_" + size.x + "x" + size.y + ".png", capture.EncodeToPNG());
                        }
                        finally { RenderTexture.active = previous; Object.Destroy(capture); }
                    }
                    finally { camera.targetTexture = null; Object.Destroy(texture); }
                }
            }
            finally { Object.Destroy(cameraObject); }
        }
    }
}
#endif
