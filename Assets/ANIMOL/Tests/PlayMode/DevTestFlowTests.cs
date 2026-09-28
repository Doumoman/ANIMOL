using System.Collections;
using System.Linq;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class DevTestFlowTests
    {
        [UnityTest]
        public IEnumerator PhysicalFlow_RequiresThreeUniqueBubblesThenExitContact()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return new WaitForFixedUpdate();

            var session = Object.FindFirstObjectByType<DevTestSession>();
            var player = Object.FindFirstObjectByType<DevPlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            var gate = Object.FindFirstObjectByType<DevExitGateView>();
            Assert.That(session, Is.Not.Null);
            Assert.That(player.TrySetAnimal("NOT_IN_FIXED_ROSTER"), Is.False);

            var ui = Object.FindFirstObjectByType<DevGameplayUiPresenter>();
            var pauseButton = ui.transform.Find("SafeArea/ScreenHost/SC15_GameplayHud/PauseButton").GetComponent<UnityEngine.UI.Button>();
            pauseButton.onClick.Invoke();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(ui.transform.Find("SafeArea/ModalHost/PauseModal").gameObject.activeSelf, Is.True);
            ui.transform.Find("SafeArea/ModalHost/PauseModal/ContinueButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            body.position = gate.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Gameplay"));
            Assert.That(session.IsExitOpen, Is.False);

            var pickups = Object.FindObjectsByType<DevBubblePickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .OrderBy(x => x.SlotId).Take(3).ToArray();
            Assert.That(pickups.Length, Is.EqualTo(3));
            foreach (var pickup in pickups)
            {
                body.position = pickup.transform.position;
                body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            Assert.That(session.CollectedCount, Is.EqualTo(3));
            Assert.That(session.IsExitOpen, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Gameplay"), "Opening the gate alone must not resolve the run.");

            body.position = gate.transform.position;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Results"));
            Assert.That(DevRunResultState.HasResult, Is.True);
            Assert.That(DevRunResultState.BubbleCount, Is.EqualTo(3));
        }
    }
}
