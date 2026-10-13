using System.Collections;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace ANIMOL.Tests
{
    public sealed class AnimolMovementPass3PlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var item in Object.FindObjectsByType<RoutedOccupancyPlatformObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(item.gameObject);
            foreach (var body in Object.FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (body.name == "Passenger") Object.Destroy(body.gameObject);
            foreach (var collider in Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (collider.name.EndsWith("Blocker")) Object.Destroy(collider.gameObject);
            yield return null;
            var active = SceneManager.GetActiveScene();
            if (active.name == "ThemePlatformLab")
            {
                var clean = SceneManager.CreateScene("AnimolMovementPass3-Clean"); SceneManager.SetActiveScene(clean);
                yield return SceneManager.UnloadSceneAsync(active);
            }
        }

        [UnityTest]
        public IEnumerator Sail_CarriesWaitsReturnsAndResetsWhileOccupied()
        {
            var sail = Spawn<CloudSailStepObject>("CLOUD_SAIL_STEP", StageMapObjectKind.CloudSailStep, new Vector2Int(3, 0));
            var passenger = Passenger(); sail.RegisterPassenger(passenger); var offset = passenger.position - (Vector2)sail.transform.position;
            yield return new WaitForSeconds(.35f);
            Assert.That(sail.State, Is.EqualTo(CloudSailStepObject.SailState.EndWait));
            Assert.That(Vector2.Distance(passenger.position - (Vector2)sail.transform.position, offset), Is.LessThan(.02f));
            sail.UnregisterPassenger(passenger); yield return new WaitForSeconds(.4f);
            Assert.That(sail.State, Is.EqualTo(CloudSailStepObject.SailState.Idle));
            sail.RegisterPassenger(passenger); yield return new WaitForFixedUpdate(); sail.ResetRuntimeState();
            Assert.That(sail.transform.position, Is.EqualTo(Vector3.zero)); Assert.That(sail.PassengerCount, Is.Zero);
            Cleanup(sail, passenger);
        }

        [UnityTest]
        public IEnumerator Bookmark_AscendsStopsDescendsAndCarriesPassenger()
        {
            var lift = Spawn<BookmarkLiftObject>("BOOKMARK_LIFT", StageMapObjectKind.BookmarkLift, new Vector2Int(0, 3));
            var passenger = Passenger(); lift.RegisterPassenger(passenger);
            yield return new WaitForSeconds(.18f);
            Assert.That(lift.State, Is.EqualTo(BookmarkLiftObject.LiftState.Top));
            Assert.That(lift.GetComponent<AnimolOperationalArtBinding>().PhasePlayer.CurrentPhase, Is.EqualTo("top"));
            yield return new WaitForSeconds(.3f);
            Assert.That(lift.State, Is.EqualTo(BookmarkLiftObject.LiftState.Idle));
            Assert.That(passenger.position.y, Is.EqualTo(1.2f).Within(.05f));
            Cleanup(lift, passenger);
        }

        [UnityTest]
        public IEnumerator Vine_UsesOccupancyForAscendingAndDescending()
        {
            var lift = Spawn<GlassVineLiftObject>("GLASS_VINE_LIFT", StageMapObjectKind.GlassVineLift, new Vector2Int(0, 3));
            var passenger = Passenger(); lift.RegisterPassenger(passenger);
            yield return new WaitForSeconds(.2f);
            Assert.That(lift.State, Is.EqualTo(GlassVineLiftObject.VineState.Top));
            lift.UnregisterPassenger(passenger); yield return new WaitForSeconds(.2f);
            Assert.That(lift.State, Is.EqualTo(GlassVineLiftObject.VineState.Idle));
            Assert.That(lift.GetComponent<AnimolOperationalArtBinding>().PhasePlayer.CurrentPhase, Is.EqualTo("idle"));
            Cleanup(lift, passenger);
        }

        [UnityTest]
        public IEnumerator Spine_BrakesStopsResumesAndRejectsBlockedPathAndLanding()
        {
            var brake = Spawn<LibSpineBrakeObject>("LIB_SPINE_BRAKE", StageMapObjectKind.LibSpineBrake, new Vector2Int(4, 0));
            var blocker = new GameObject("RouteBlocker"); blocker.transform.position = new Vector3(2f, .5f); blocker.AddComponent<BoxCollider2D>().size = Vector2.one;
            brake.SetOccupiedForTest(true); yield return new WaitForSeconds(.15f);
            Assert.That(brake.IsPathBlocked, Is.True);
            Object.Destroy(blocker); yield return null;
            yield return new WaitForSeconds(.35f);
            Assert.That(brake.State, Is.EqualTo(LibSpineBrakeObject.BrakeState.Stopped));
            var landingBlocker = new GameObject("LandingBlocker"); landingBlocker.transform.position = new Vector3(4f, .5f); landingBlocker.AddComponent<BoxCollider2D>().size = Vector2.one;
            Assert.That(brake.IsLandingSpaceClear(brake.RouteEnd), Is.False);
            Object.Destroy(landingBlocker); brake.SetOccupiedForTest(false); yield return new WaitForSeconds(.4f);
            Assert.That(brake.State, Is.EqualTo(LibSpineBrakeObject.BrakeState.Idle));
            Assert.That(brake.GetComponent<AnimolOperationalArtBinding>().PhasePlayer.CurrentPhase, Is.EqualTo("idle"));
            Object.Destroy(brake.gameObject);
        }

        [UnityTest]
        public IEnumerator ThemePlatformLab_SpawnsBoardsLeavesAndCheckpointResetsAllFour()
        {
            SceneManager.LoadScene("ThemePlatformLab"); yield return null; yield return new WaitForFixedUpdate();
            var sail = Object.FindFirstObjectByType<CloudSailStepObject>();
            var bookmark = Object.FindFirstObjectByType<BookmarkLiftObject>();
            var vine = Object.FindFirstObjectByType<GlassVineLiftObject>();
            var spine = Object.FindFirstObjectByType<LibSpineBrakeObject>();
            Assert.That(sail, Is.Not.Null); Assert.That(bookmark, Is.Not.Null); Assert.That(vine, Is.Not.Null); Assert.That(spine, Is.Not.Null);
            sail.SetOccupiedForTest(true); bookmark.SetOccupiedForTest(true); vine.SetOccupiedForTest(true); spine.SetOccupiedForTest(true);
            yield return new WaitForSeconds(.12f);
            sail.SetOccupiedForTest(false); vine.SetOccupiedForTest(false); spine.SetOccupiedForTest(false);
            Object.FindFirstObjectByType<StageMapRuntimeLoader>().ResetRuntimeObjects();
            foreach (var platform in new RoutedOccupancyPlatformObject[] { sail, bookmark, vine, spine })
            {
                Assert.That(Vector2.Distance(platform.transform.position, platform.RouteStart), Is.LessThan(.01f), platform.name);
                Assert.That(platform.PassengerCount, Is.Zero, platform.name);
            }
        }

        private static T Spawn<T>(string id, StageMapObjectKind kind, Vector2Int end) where T : RoutedOccupancyPlatformObject
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ANIMOL/Prefabs/MapObjects/M9B/{id}.prefab");
#endif
            var instance = Object.Instantiate(prefab);
            var settings = new StageMapObjectSettings();
            settings.EditorConfigure(3, StageMapObjectDirection.Right, new Vector2(2f, 1f), HalfBlockPlacement.Lower,
                SideSpringContactPolicy.FacingSideOnly, 0f, 0f, 20f, 0f, 0f, 0f, .5f, .05f, true,
                new[] { Vector2Int.zero, end });
            settings.EditorConfigureDesignBehavior(.02f, .05f, .05f, MapObjectPassengerPolicy.Carry,
                MapObjectTriggerMode.OnOccupancy, 0f, 0f, 1f, true);
            var runtime = instance.GetComponent<T>();
            runtime.Configure(new StageMapObjectPlacement(id, kind, 0, 0, id, prefab, settings), 1f);
            return runtime;
        }

        private static Rigidbody2D Passenger()
        {
            var passenger = new GameObject("Passenger").AddComponent<Rigidbody2D>();
            passenger.bodyType = RigidbodyType2D.Kinematic; passenger.position = new Vector2(1f, 1.2f); return passenger;
        }

        private static void Cleanup(Component runtime, Rigidbody2D passenger)
        { Object.Destroy(runtime.gameObject); Object.Destroy(passenger.gameObject); }
    }
}
