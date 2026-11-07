using System.Collections;
using System.Linq;
using System.Reflection;
using Animol.Starway;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WilliamParadox.Story.Tests.PlayMode
{
    public sealed class StoryChroniclePlayTests
    {
        private GameObject root;
        private StoryDialogueView dialogue;
        private StoryChronicleView record;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Open()
        {
#if UNITY_EDITOR
            root = new GameObject("Chronicle test canvas", typeof(Canvas), typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            var background = new GameObject("Existing R17B", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            var rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().sprite = Resources.Load<Sprite>("ANIMOL/Story/Starway/R17B");
            background.GetComponent<Image>().raycastTarget = false;
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Story/StoryDialogueUI.prefab"), root.transform);
            dialogue = instance.GetComponent<StoryDialogueView>(); record = dialogue.Chronicle;
            yield return null;
            typeof(StoryChronicleView).GetField("hasFocus", Private).SetValue(record, true);
            dialogue.SetReducedMotion(false); dialogue.OpenScene("R17"); dialogue.SkipCurrentScene();
            Canvas.ForceUpdateCanvases(); record.Tick(0);
            Assert.That(dialogue.CurrentLine.id, Is.EqualTo("R17_008"));
            Assert.That(dialogue.PendingAction, Is.EqualTo("play_chronicle"));
#else
            Assert.Ignore("Editor prefab required."); yield break;
#endif
        }
        [TearDown] public void Close() { if (root != null) Object.Destroy(root); }
        [UnityTest] public IEnumerator CaptureBothStaticViewpointsAndEmptyBook()
        {
            dialogue.SetReducedMotion(true);
            yield return Capture("asteran");
            for (int i = 0; i < 6; i++) record.AdvanceStaticPage();
            yield return Capture("higher");
            for (int i = 0; i < 6; i++) record.AdvanceStaticPage();
            yield return Capture("await_continue");
        }
        private IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return null;
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.Directory.CreateDirectory("Docs/Validation/StarwayRanchChronicle");
            System.IO.File.WriteAllBytes("Docs/Validation/StarwayRanchChronicle/" + name + ".png", image.EncodeToPNG());
            Object.Destroy(image);
        }
        private PointerEventData Pointer(int id) => new PointerEventData(EventSystem.current) { pointerId = id, button = PointerEventData.InputButton.Left };
        private void Until(StoryChronicleView.Phase phase)
        {
            for (int i = 0; i < 20000 && record.CurrentPhase != phase; i++) record.Tick(0.05f);
            Assert.That(record.CurrentPhase, Is.EqualTo(phase));
        }

        [Test] public void BothWholeRecordsRewindAndOnlyFreshContinueFinishes()
        {
            int reads = 0, skips = 0, continues = 0, rewinds = 0;
            record.ReadThrough.AddListener(() => reads++); record.Skipped.AddListener(() => skips++);
            record.ContinueRequested.AddListener(() => continues++); record.RewindStarted.AddListener(() => rewinds++);
            var column = record.transform.Find("TextViewport/ScrollingColumn");
            var slots = column.Cast<Transform>().Where(t => t.name.StartsWith("Record_C")).ToArray();
            Assert.That(slots.Length, Is.EqualTo(6));
            Assert.That(record.GetComponentInChildren<RectMask2D>(), Is.Not.Null);
            Until(StoryChronicleView.Phase.AsteranRoll);
            float start = record.ScrollPosition; record.Tick(0.05f); float normal = record.ScrollPosition - start;
            record.OnPointerDown(Pointer(1)); start = record.ScrollPosition; record.Tick(0.05f);
            Assert.That(record.ScrollPosition - start, Is.EqualTo(normal * 3).Within(0.01));
            record.OnPointerExit(Pointer(1)); Assert.That(record.IsHolding, Is.False);
            Until(StoryChronicleView.Phase.AsteranPause);
            for (int i = 0; i < 19; i++) record.Tick(0.05f);
            Assert.That(record.CurrentPhase, Is.EqualTo(StoryChronicleView.Phase.AsteranPause));
            Until(StoryChronicleView.Phase.RewindRewrite); Assert.That(rewinds, Is.EqualTo(1));
            start = record.ScrollPosition; record.Tick(0.05f); Assert.That(record.ScrollPosition, Is.LessThan(start));
            Until(StoryChronicleView.Phase.RewritePause);
            CollectionAssert.AreEqual(slots, column.Cast<Transform>().Where(t => t.name.StartsWith("Record_C")).ToArray());
            Assert.That(reads + skips, Is.Zero);
            Until(StoryChronicleView.Phase.HigherRoll);
            record.OnPointerDown(Pointer(2)); Until(StoryChronicleView.Phase.AwaitContinue);
            Assert.That(record.WasReadThrough, Is.True); Assert.That(reads, Is.EqualTo(1)); Assert.That(skips, Is.Zero);
            Assert.That(dialogue.ConfirmAction("play_chronicle"), Is.False);
            record.RequestContinue(); Assert.That(continues, Is.Zero);
            record.OnPointerUp(Pointer(2)); Assert.That(continues, Is.Zero);
            Assert.That(record.IsAwaitingContinue, Is.True);
            record.RequestContinue(); Assert.That(continues, Is.EqualTo(1));
            Assert.That(record.IsOpen, Is.False); Assert.That(dialogue.PendingAction, Is.Empty);
        }

        [Test] public void FocusPauseAndCancellationNeverAdvanceOrLeaveAcceleration()
        {
            Until(StoryChronicleView.Phase.AsteranRoll);
            record.OnPointerDown(Pointer(1)); record.OnPointerDown(Pointer(2));
            record.OnPointerUp(Pointer(1)); Assert.That(record.IsHolding, Is.True);
            record.OnCancel(new BaseEventData(EventSystem.current)); Assert.That(record.IsHolding, Is.False);
            record.OnPointerDown(Pointer(3)); float position = record.ScrollPosition;
            typeof(StoryChronicleView).GetMethod("OnApplicationFocus", Private).Invoke(record, new object[] { false });
            record.Tick(30); Assert.That(record.ScrollPosition, Is.EqualTo(position)); Assert.That(record.IsHolding, Is.False);
            record.RequestContinue(); Assert.That(record.IsOpen, Is.True);
            typeof(StoryChronicleView).GetMethod("OnApplicationFocus", Private).Invoke(record, new object[] { true });
            record.Tick(30); Assert.That(record.ScrollPosition, Is.EqualTo(position));
            record.Tick(30); Assert.That(record.ScrollPosition - position, Is.LessThan(10), "Resume delta is capped.");
            record.SetPaused(true); position = record.ScrollPosition; record.Tick(30); Assert.That(record.ScrollPosition, Is.EqualTo(position));
            record.SetPaused(false); dialogue.Close(); Assert.That(record.IsOpen || record.IsHolding, Is.False);
            Assert.That(dialogue.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            dialogue.OpenScene("R01"); Assert.That(record.IsOpen, Is.False);
        }

        [Test] public void StaticPagesRequireTapsAndSkipNeverBecomesReadThrough()
        {
            dialogue.SetReducedMotion(true); Assert.That(record.StaticPage, Is.Zero);
            record.OnPointerDown(Pointer(1)); record.Tick(30); Assert.That(record.StaticPage, Is.Zero);
            typeof(StoryChronicleView).GetField("pointerStartedAt", Private).SetValue(record, Time.unscaledTime - 1);
            record.OnPointerUp(Pointer(1)); Assert.That(record.StaticPage, Is.Zero);
            record.OnPointerDown(Pointer(2)); record.OnPointerUp(Pointer(2)); Assert.That(record.StaticPage, Is.EqualTo(1));
            record.SkipToEnd(); Assert.That(record.WasSkipped, Is.True); Assert.That(record.WasReadThrough, Is.False);
            record.AdvanceStaticPage(); Assert.That(record.WasReadThrough, Is.False);
            dialogue.Replay(); dialogue.SkipCurrentScene(); Assert.That(record.StaticPage, Is.Zero); Assert.That(record.WasSkipped, Is.False);
            for (int i = 0; i < 12; i++) record.AdvanceStaticPage();
            Assert.That(record.IsAwaitingContinue && record.WasReadThrough, Is.True);
            Assert.That(record.transform.Find("TextViewport/ScrollingColumn").GetComponentsInChildren<TMPro.TMP_Text>().All(t => string.IsNullOrEmpty(t.text)), Is.True);
        }
    }
}
