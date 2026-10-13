using System.Collections;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ANIMOL.Tests
{
    public sealed class RunPrototypePlayModeTests
    {
        [UnityTearDown]
        public IEnumerator CleanScene()
        {
            var previous = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("RunPrototypeTestCleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(previous);
        }

        private IEnumerator Load(bool up)
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/RunPrototype/Run" + (up ? "Up" : "Down") + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null; yield return null; yield return new WaitForSeconds(.1f);
        }
        private IEnumerator Complete(bool up, RunProbeStrategy strategy)
        {
            yield return Load(up);
            var session = Object.FindFirstObjectByType<RunPrototypeSession>(); session.BeginProbe(strategy);
            float deadline = Time.realtimeSinceStartup + 105;
            while (!session.ProbeDone && Time.realtimeSinceStartup < deadline) yield return null;
            session.SaveEvidence("Docs/RunPrototype/Telemetry");
            var result = session.Result;
            Assert.That(result.completed, Is.True, JsonUtility.ToJson(result));
            Assert.That(result.landed && result.landingInsideReservation, Is.True, JsonUtility.ToJson(result));
            Assert.That(result.launches, Is.EqualTo(1)); Assert.That(result.jumpRequests, Is.Zero);
            Assert.That(result.unexpectedAirborneSteps, Is.Zero, "Required RunLeg floors cannot need a jump or hide a seam gap.");
            Assert.That(result.exhausted, Is.False); Assert.That(result.continuousFloorTiles, Is.EqualTo(new[] { 80, 80 }));
            if (strategy == RunProbeStrategy.Mixed) { Assert.That(result.minimumStamina, Is.LessThan(35)); Assert.That(result.walkingSeconds, Is.GreaterThan(3)); Assert.That(result.runningSeconds, Is.GreaterThan(10)); }
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator DescentMixedCompletesWithoutJumpOrStatUpgrade() => Complete(false, RunProbeStrategy.Mixed);
        [UnityTest] public IEnumerator AscentMixedCompletesWithoutJumpOrStatUpgrade() => Complete(true, RunProbeStrategy.Mixed);
        [UnityTest] public IEnumerator DescentWalkingCompletesAndConservesStamina() => Complete(false, RunProbeStrategy.Walk);
        [UnityTest] public IEnumerator AscentWalkingCompletesAndConservesStamina() => Complete(true, RunProbeStrategy.Walk);

        [UnityTest]
        public IEnumerator HoldingRunExhaustsBeforeTheEightyTileLegEnds()
        {
            yield return Load(false); var session = Object.FindFirstObjectByType<RunPrototypeSession>();
            session.BeginProbe(RunProbeStrategy.RunUntilExhausted);
            float deadline = Time.time + 9;
            while (!session.ProbeDone && Time.time < deadline) yield return null;
            session.SaveEvidence("Docs/RunPrototype/Telemetry");
            Assert.That(session.Result.exhausted, Is.True); Assert.That(session.Result.completed, Is.False);
            Assert.That(session.Result.firstExhaustionX, Is.InRange(26f, 33f));
        }

        [UnityTest]
        public IEnumerator SolidLegEntersOneWayApproachFromAboveWithoutFallingThrough()
        {
            yield return Load(false); var session = Object.FindFirstObjectByType<RunPrototypeSession>();
            session.Player.Body.position = new Vector2(78, 9.65f); session.Player.Body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(.3f); session.Player.ApplySwipe(-912, 1);
            yield return new WaitForSeconds(2.5f);
            Assert.That(session.Player.Body.position.x, Is.GreaterThan(85));
            Assert.That(session.Player.Body.position.y, Is.InRange(9.3f, 9.65f));
            Assert.That(session.Player.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator LandingPlatformStillAllowsPlayerScopedDropThrough()
        {
            yield return Load(true); var session = Object.FindFirstObjectByType<RunPrototypeSession>();
            var player = session.Player; player.Body.position = new Vector2(40, 9.65f); player.Body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(.3f);
            Assert.That(player.IsGrounded, Is.True); Assert.That(player.RequestDropThroughOneWayPlatform(), Is.True);
            yield return new WaitForSeconds(.4f); Assert.That(player.Body.position.y, Is.LessThan(9));
        }

        [UnityTest]
        public IEnumerator ExternalSpringLaunchStopsAtAnActualSolidCeiling()
        {
            yield return Load(true); var session = Object.FindFirstObjectByType<RunPrototypeSession>();
            var ceiling = new GameObject("Test solid ceiling", typeof(BoxCollider2D));
            ceiling.transform.position = new Vector2(91, 4); ceiling.GetComponent<BoxCollider2D>().size = new Vector2(8, .5f);
            session.Player.Body.position = new Vector2(91, 1.5f); session.Player.Body.linearVelocity = Vector2.zero;
            session.Player.ApplySwipe(-914, 1);
            float deadline = Time.time + 3, maxY = 0;
            while (Time.time < deadline) { maxY = Mathf.Max(maxY, session.Player.Body.position.y); yield return new WaitForFixedUpdate(); }
            Assert.That(session.Spring.ActivationCount, Is.EqualTo(1)); Assert.That(session.Lift.Active, Is.False);
            Assert.That(maxY, Is.LessThan(3.4f), "External launch must not push through a solid ceiling.");
        }

        [UnityTest]
        public IEnumerator PortraitCameraFramesPlayerAndLandingWithoutInputOrTimeLock()
        {
            yield return Load(true); var session = Object.FindFirstObjectByType<RunPrototypeSession>();
            var camera = Camera.main; var follow = camera.GetComponent<RunPrototypeCamera>();
            foreach (int height in new[] { 1920, 2400 })
            {
                var texture = new RenderTexture(1080, height, 16); camera.targetTexture = texture;
                camera.GetComponent<PortraitWorldCameraPolicy>().Apply();
                // A framing fixture only: full traversal and collision validation are separate tests above.
                session.Player.Body.position = new Vector2(89, 6); session.Player.Body.linearVelocity = Vector2.zero;
                session.Player.Body.constraints = RigidbodyConstraints2D.FreezeAll;
                yield return new WaitForSeconds(.7f);
                Assert.That(follow.FramingLanding, Is.True);
                foreach (var point in new[] { session.Player.Body.position, follow.CurrentLandingFocus })
                {
                    var view = camera.WorldToViewportPoint(point);
                    Assert.That(view.x, Is.InRange(.05f, .95f)); Assert.That(view.y, Is.InRange(.2f, .8f));
                }
                Assert.That(camera.orthographicSize * camera.aspect * 2, Is.EqualTo(12).Within(.01f));
                Assert.That(Time.timeScale, Is.EqualTo(1));
                camera.targetTexture = null; texture.Release(); Object.Destroy(texture);
            }
        }
    }
}
