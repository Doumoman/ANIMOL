using System.Collections;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ANIMOL.Tests
{
    public sealed class M6CampaignMobileFlowTests
    {
        [UnityTest]
        public IEnumerator MobileHudHasSixActionsAndSupportsIndependentPointerIds()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;

            var controls = Object.FindObjectsByType<DevMobileTouchControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var router = Object.FindFirstObjectByType<DevMobileInputRouter>(FindObjectsInactive.Include);
            Assert.That(controls.Select(x => x.Action).Distinct().Count(), Is.EqualTo(6));
            Assert.That(controls.Length, Is.EqualTo(6));

            Assert.That(router.Begin(101, MobileTouchAction.SwipeMove), Is.True);
            Assert.That(router.Begin(102, MobileTouchAction.Jump), Is.True);
            Assert.That(router.Begin(103, MobileTouchAction.Special), Is.True);
            Assert.That(router.Begin(104, MobileTouchAction.Ability), Is.True);
            Assert.That(router.ActiveTouchCount, Is.EqualTo(4));
            Assert.That(router.Begin(101, MobileTouchAction.AnimalGround), Is.False);

            var ability = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.name == "AbilityStateLabel");
            Assert.That(ability.text, Does.Contain("미연결"));
            router.End(101); router.End(102); router.End(103); router.End(104);
            Assert.That(router.ActiveTouchCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PhysicalCheckpointBecomesRespawnPoint()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return new WaitForFixedUpdate();

            var session = Object.FindFirstObjectByType<DevTestSession>();
            var player = Object.FindFirstObjectByType<DevPlayerController>();
            var checkpoint = Object.FindFirstObjectByType<DevCheckpoint>();
            var body = player.GetComponent<Rigidbody2D>();
            body.position = checkpoint.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(session.ActiveCheckpointId, Is.EqualTo("CHECKPOINT_MID"));

            body.position = new Vector2(-7f, 4f);
            Assert.That(session.RespawnAtLastCheckpoint(), Is.True);
            Assert.That(Vector2.Distance(body.position, checkpoint.transform.position), Is.LessThan(.1f));
        }

        [UnityTest]
        public IEnumerator ForcedTimeExpiryProducesFailureWithoutFakeClear()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var session = Object.FindFirstObjectByType<DevTestSession>();
            var layout = session.LayoutIdentity;
            session.ForceTimeExpiredForTest();
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Results"));
            Assert.That(DevRunResultState.Outcome, Is.EqualTo(CampaignRunOutcome.TimeExpired));
            Assert.That(DevRunResultState.BubbleCount, Is.LessThan(3));
            Assert.That(DevRunResultState.LayoutIdentity, Is.EqualTo(layout));
        }

        [UnityTest]
        public IEnumerator ControlAccessibilitySettingsNavigateAndPersistTruthfully()
        {
            MobileControlPreferences.Reset();
            SceneManager.LoadScene("Lobby");
            yield return null;

            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var root = navigation.transform;
            FindButton(root, "SettingsButton").onClick.Invoke();
            FindButton(root, "ControlSettingsButton").onClick.Invoke();
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC19_ControlSettings"));

            FindButton(root, "ControlSizeUpButton").onClick.Invoke();
            FindButton(root, "ControlOpacityButton").onClick.Invoke();
            FindButton(root, "LargeTextToggleButton").onClick.Invoke();
            FindButton(root, "VibrationToggleButton").onClick.Invoke();
            FindButton(root, "LanguageToggleButton").onClick.Invoke();
            Assert.That(MobileControlPreferences.SizeScale, Is.EqualTo(1.1f).Within(.001f));
            Assert.That(MobileControlPreferences.Opacity, Is.EqualTo(.62f).Within(.001f));
            Assert.That(MobileControlPreferences.LargeText, Is.True);
            Assert.That(MobileControlPreferences.Vibration, Is.False);
            Assert.That(MobileControlPreferences.Language, Is.EqualTo(UiLanguage.English));
            Assert.That(root.GetComponentsInChildren<Text>(true).First(x => x.name == "ControlSettingsSummary").text, Does.Contain("Control size"));

            FindButton(root, "ControlResetButton").onClick.Invoke();
            MobileControlPreferences.Reset();
        }

        private static Button FindButton(Transform root, string name) => root.GetComponentsInChildren<Button>(true).First(x => x.name == name);
    }
}
