using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace WilliamParadox.Story.Tests
{
    public sealed class RecordingStoryPauseSink : MonoBehaviour, IStoryPauseSink
    {
        public readonly List<bool> requests = new List<bool>();
        public StoryGameplayBridge releaseDuringSuspend;
        public void SetStorySuspended(bool value)
        {
            requests.Add(value);
            if (value && releaseDuringSuspend != null) releaseDuringSuspend.ReleaseStory();
        }
    }

    public sealed class RuntimeFeedbackBridgeTests
    {
        private readonly List<GameObject> owned = new List<GameObject>();
        private GameObject Create(string name)
        {
            var value = new GameObject(name, typeof(RectTransform)); owned.Add(value); return value;
        }
        [TearDown] public void TearDown()
        {
            foreach (var value in owned) if (value != null) Object.DestroyImmediate(value);
            owned.Clear();
        }
        private static void Tick(StoryButtonFeedback feedback, float delta)
        {
            typeof(StoryButtonFeedback).GetMethod("TickFeedback", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(feedback, new object[] { delta });
        }
        private static void Lifecycle(MonoBehaviour component, string method)
        {
            component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
        }
        private StoryButtonFeedback Button(out RectTransform hitbox, out RectTransform visual, out StoryRunner runner)
        {
            var root = Create("StableButton"); root.SetActive(false);
            hitbox = root.GetComponent<RectTransform>(); hitbox.sizeDelta = new Vector2(320, 120);
            root.AddComponent<Image>(); root.AddComponent<Button>(); runner = root.AddComponent<StoryRunner>();
            visual = Create("CosmeticChild").GetComponent<RectTransform>(); visual.SetParent(root.transform, false);
            visual.localScale = new Vector3(0.8f, 0.9f, 1);
            var feedback = root.AddComponent<StoryButtonFeedback>(); feedback.Configure(runner, visual);
            root.SetActive(true);
            // EditMode does not drive ordinary MonoBehaviour callbacks.
            Lifecycle(feedback, "OnEnable");
            return feedback;
        }
        [Test] public void QuickTapAnimatesOnlyTheCosmeticChildAndPreservesSelectableColorsAndHitbox()
        {
            var feedback = Button(out var hitbox, out var visual, out _);
            var rest = visual.localScale; var size = hitbox.sizeDelta; var colors = hitbox.GetComponent<Button>().colors;
            var eventRoot = Create("EventSystem"); var events = eventRoot.AddComponent<EventSystem>();
            var pointer = new PointerEventData(events) { pointerId = 4, button = PointerEventData.InputButton.Left };
            feedback.OnPointerDown(pointer); feedback.OnPointerUp(pointer); Tick(feedback, 0.045f);
            Assert.That(visual.localScale.x, Is.LessThan(rest.x));
            Assert.That(hitbox.localScale, Is.EqualTo(Vector3.one)); Assert.That(hitbox.sizeDelta, Is.EqualTo(size));
            Assert.That(hitbox.GetComponent<Button>().colors, Is.EqualTo(colors));
            Tick(feedback, 0.09f); Assert.That(visual.localScale, Is.EqualTo(rest)); Assert.That(feedback.IsAnimating, Is.False);
        }
        [Test] public void PauseAndDisableRestoreTheExactAuthoredScaleWithoutAQueuedAnimation()
        {
            var feedback = Button(out var hitbox, out var visual, out var runner); var rest = visual.localScale;
            feedback.OnSubmit(null); Tick(feedback, 0.045f); Assert.That(visual.localScale.x, Is.LessThan(rest.x));
            runner.TogglePause(); Tick(feedback, 0.01f);
            Assert.That(visual.localScale, Is.EqualTo(rest)); Assert.That(feedback.IsAnimating, Is.False);
            runner.TogglePause(); Tick(feedback, 0.1f); Assert.That(visual.localScale, Is.EqualTo(rest));
            visual.localScale = rest * 0.97f; feedback.enabled = false; Lifecycle(feedback, "OnDisable");
            Assert.That(visual.localScale, Is.EqualTo(rest)); Assert.That(hitbox.localScale, Is.EqualTo(Vector3.one));
        }
        [Test] public void ReducedMotionNeverScalesAndAnInvalidRootBindingCannotMoveTheHitbox()
        {
            var feedback = Button(out var hitbox, out var visual, out _); var rest = visual.localScale;
            feedback.reducedMotion = true; feedback.OnSubmit(null); Tick(feedback, 1);
            Assert.That(visual.localScale, Is.EqualTo(rest));
            LogAssert.Expect(LogType.Warning, new Regex("cosmetic child"));
            feedback.Configure(null, hitbox); feedback.reducedMotion = false; feedback.OnSubmit(null); Tick(feedback, 1);
            Assert.That(hitbox.localScale, Is.EqualTo(Vector3.one));
        }
        [Test] public void LosingFocusImmediatelyRestoresTheButtonAndNeverReplaysThePress()
        {
            var feedback = Button(out _, out var visual, out _); var rest = visual.localScale;
            feedback.OnSubmit(null); Tick(feedback, 0.045f);
            Assert.That(visual.localScale.x, Is.LessThan(rest.x));
            typeof(StoryButtonFeedback).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(feedback, new object[] { false });
            Assert.That(visual.localScale, Is.EqualTo(rest));
            typeof(StoryButtonFeedback).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(feedback, new object[] { true });
            Tick(feedback, 0.2f);
            Assert.That(visual.localScale, Is.EqualTo(rest)); Assert.That(feedback.IsAnimating, Is.False);
        }
        private StoryGameplayBridge Bridge(out StoryRunner runner, out RecordingStoryPauseSink sink)
        {
            var root = Create("StoryHost"); root.SetActive(false);
            runner = root.AddComponent<StoryRunner>(); sink = root.AddComponent<RecordingStoryPauseSink>();
            var bridge = root.AddComponent<StoryGameplayBridge>(); bridge.runner = runner; bridge.sinks = new MonoBehaviour[] { sink, sink };
            root.SetActive(true); Lifecycle(bridge, "OnEnable"); return bridge;
        }
        [Test] public void CompletionAndDisableReleaseOnlyTheSuspensionOwnedByThisBridge()
        {
            var scale = Time.timeScale; var bridge = Bridge(out var runner, out var sink);
            CollectionAssert.AreEqual(new[] { true }, sink.requests); Assert.That(bridge.ConnectedAdapterCount, Is.EqualTo(1));
            runner.onCompleted.Invoke(); CollectionAssert.AreEqual(new[] { true, false }, sink.requests);
            bridge.BeginStory(); bridge.enabled = false; Lifecycle(bridge, "OnDisable");
            CollectionAssert.AreEqual(new[] { true, false, true, false }, sink.requests);
            Assert.That(Time.timeScale, Is.EqualTo(scale));
        }
        [Test] public void RemovingAnAdapterBalancesItsRequestAndReportsMissingIntegration()
        {
            var bridge = Bridge(out _, out var sink); bridge.sinks = new MonoBehaviour[0];
            LogAssert.Expect(LogType.Warning, new Regex("No IStoryPauseSink adapters")); bridge.RefreshAdapters();
            CollectionAssert.AreEqual(new[] { true, false }, sink.requests);
            Assert.That(bridge.HasAdapters, Is.False); Assert.That(bridge.ConfigurationWarning, Does.Contain("NOT connected"));
        }
        [Test] public void ReentrantHostReleaseCannotLeaveGameplaySuspended()
        {
            var bridge = Bridge(out _, out var sink); bridge.ReleaseStory(); sink.requests.Clear();
            sink.releaseDuringSuspend = bridge; bridge.BeginStory();
            CollectionAssert.AreEqual(new[] { true, false }, sink.requests); Assert.That(bridge.IsSuspended, Is.False);
        }
        [Test] public void RestoredAndMutedNodesAreRememberedInsteadOfPlayingOnResume()
        {
            var root = Create("OptionalSound"); var sound = root.AddComponent<StoryAudioFeedback>();
            sound.PlayNode("restored-node", true); sound.PlayNode("restored-node", false);
            sound.muted = true; sound.PlayNode("muted-node", false); sound.muted = false; sound.PlayNode("muted-node", false);
            Assert.That(sound.SeenNodeCount, Is.EqualTo(2)); Assert.That(sound.HasSeenNode("restored-node"), Is.True);
            sound.ResetSession(); Assert.That(sound.SeenNodeCount, Is.Zero);
        }
    }
}
