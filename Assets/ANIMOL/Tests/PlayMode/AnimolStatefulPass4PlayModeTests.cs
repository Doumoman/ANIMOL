using System.Collections;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class AnimolStatefulPass4PlayModeTests
    {
        [UnityTearDown] public IEnumerator TearDown()
        {
            var active = SceneManager.GetActiveScene();
            if (active.name == "ThemePlatformLab") { var clean = SceneManager.CreateScene("Pass4-Clean"); SceneManager.SetActiveScene(clean); yield return SceneManager.UnloadSceneAsync(active); }
            foreach (var runtime in Object.FindObjectsByType<StageMapRuntimeObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(runtime.gameObject);
            foreach (var collider in Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (collider.name == "Pass4Blocker") Object.Destroy(collider.gameObject);
            yield return null;
        }

        [UnityTest] public IEnumerator Sheep_WarnsDispersesAndDefersSolidificationUntilClear()
        {
            var sheep = Spawn<CloudSheepStepObject>("CLOUD_SHEEP_STEP", StageMapObjectKind.CloudSheepStep);
            sheep.SetOccupiedForTest(true); yield return new WaitForFixedUpdate(); sheep.SetOccupiedForTest(false);
            yield return new WaitForSeconds(.06f); Assert.That(sheep.State, Is.EqualTo(CloudSheepStepObject.SheepState.Dispersed));
            var blocker = Block(new Vector2(1f, .5f)); yield return new WaitForSeconds(.13f);
            Assert.That(sheep.State, Is.EqualTo(CloudSheepStepObject.SheepState.WaitingForClear)); Assert.That(sheep.GetComponent<BoxCollider2D>().enabled, Is.False);
            Object.Destroy(blocker); yield return null; yield return new WaitForSeconds(.04f);
            Assert.That(sheep.State, Is.EqualTo(CloudSheepStepObject.SheepState.Solid));
            sheep.SetOccupiedForTest(true); sheep.ResetRuntimeState(); Assert.That(sheep.PassengerCount, Is.Zero);
        }

        [UnityTest] public IEnumerator PageAndPopup_DeferClosingWhileOccupied()
        {
            var page = Spawn<PageBridgeObject>("PAGE_BRIDGE", StageMapObjectKind.PageBridge); page.SetApproachForTest(true);
            yield return new WaitForSeconds(.13f); Assert.That(page.State, Is.EqualTo(PageBridgeObject.BridgeState.Open)); Assert.That(page.EnabledPartCount, Is.EqualTo(3));
            page.SetApproachForTest(false); page.SetOccupiedForTest(true); yield return new WaitForSeconds(.12f); Assert.That(page.State, Is.EqualTo(PageBridgeObject.BridgeState.Open));
            page.SetOccupiedForTest(false); yield return new WaitForSeconds(.06f); Assert.That(page.State, Is.EqualTo(PageBridgeObject.BridgeState.Closed));
            var stair = Spawn<LibPopupStairObject>("LIB_POPUP_STAIR", StageMapObjectKind.LibPopupStair); stair.SetApproachForTest(true);
            yield return new WaitForSeconds(.1f); Assert.That(stair.State, Is.EqualTo(LibPopupStairObject.StairState.Open));
            stair.SetApproachForTest(false); stair.SetOccupiedForTest(true); yield return new WaitForSeconds(.1f); Assert.That(stair.State, Is.EqualTo(LibPopupStairObject.StairState.Open));
            stair.SetOccupiedForTest(false); yield return new WaitForSeconds(.05f); Assert.That(stair.State, Is.EqualTo(LibPopupStairObject.StairState.Folded));
        }

        [UnityTest] public IEnumerator Dew_GrowsLeftToRightAndWaitsForClearBeforeEachSolidPart()
        {
            var dew = Spawn<DewSeedStepObject>("DEW_SEED_STEP", StageMapObjectKind.DewSeedStep);
            var blocker = Block(new Vector2(.5f, .45f)); dew.SetPresenceForTest(true); yield return new WaitForSeconds(.06f); Assert.That(dew.EnabledPartCount, Is.Zero);
            Object.Destroy(blocker); yield return null; yield return new WaitForSeconds(.1f);
            Assert.That(dew.State, Is.EqualTo(DewSeedStepObject.SeedState.Full)); Assert.That(dew.EnabledPartCount, Is.EqualTo(3));
            dew.SetPresenceForTest(false); dew.SetOccupiedForTest(true); yield return new WaitForFixedUpdate(); Assert.That(dew.State, Is.EqualTo(DewSeedStepObject.SeedState.Wilting));
            Assert.That(dew.EnabledPartCount, Is.EqualTo(3)); dew.SetOccupiedForTest(false); yield return new WaitForFixedUpdate(); Assert.That(dew.State, Is.EqualTo(DewSeedStepObject.SeedState.Seed));
        }

        [UnityTest] public IEnumerator ThemePlatformLab_SpawnsAndCheckpointResetsAllFour()
        {
            SceneManager.LoadScene("ThemePlatformLab"); yield return null; yield return new WaitForFixedUpdate();
            Assert.That(Object.FindFirstObjectByType<CloudSheepStepObject>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PageBridgeObject>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<LibPopupStairObject>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<DewSeedStepObject>(), Is.Not.Null);
            foreach (var platform in Object.FindObjectsByType<OccupancyPlatformObject>(FindObjectsSortMode.None)) platform.SetOccupiedForTest(true);
            Object.FindFirstObjectByType<StageMapRuntimeLoader>().ResetRuntimeObjects();
            foreach (var platform in Object.FindObjectsByType<OccupancyPlatformObject>(FindObjectsSortMode.None)) Assert.That(platform.PassengerCount, Is.Zero, platform.name);
        }

        private static T Spawn<T>(string id, StageMapObjectKind kind) where T : StageMapRuntimeObject
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ANIMOL/Prefabs/MapObjects/M9B/{id}.prefab");
#endif
            var instance = Object.Instantiate(prefab); var settings = new StageMapObjectSettings();
            settings.EditorConfigure(3, StageMapObjectDirection.Right, new Vector2(3, 1), HalfBlockPlacement.Lower, SideSpringContactPolicy.FacingSideOnly, 0, 0, 2, .02f, .05f, .05f, .5f, .05f, true, System.Array.Empty<Vector2Int>());
            settings.EditorConfigureDesignBehavior(.02f, .05f, .05f, MapObjectPassengerPolicy.DeferStateChangeWhileOccupied, MapObjectTriggerMode.OnOccupancy, 2, 0, 1, true);
            var runtime = instance.GetComponent<T>(); runtime.Configure(new StageMapObjectPlacement(id, kind, 0, 0, id, prefab, settings), 1f); return runtime;
        }

        private static GameObject Block(Vector2 position) { var blocker = new GameObject("Pass4Blocker", typeof(BoxCollider2D)); blocker.transform.position = position; return blocker; }
    }
}
