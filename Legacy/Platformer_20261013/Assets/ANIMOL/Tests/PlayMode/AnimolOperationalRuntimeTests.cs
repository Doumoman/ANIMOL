using System.Collections;
using System.Reflection;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class AnimolOperationalRuntimeTests
    {
        [UnityTest]
        public IEnumerator MineCart_SelectsBothBranches_StopsAndResets()
        {
            var prefab = Resources.Load<GameObject>("__missing");
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/MapObjects/M9B/MINE_CART_FORK.prefab");
#endif
            var instance = Object.Instantiate(prefab);
            var settings = Settings(new[] { new Vector2Int(-1, 1), new Vector2Int(1, 1) }, .05f, 20f);
            var cart = instance.GetComponent<MineCartForkObject>();
            cart.Configure(new StageMapObjectPlacement("r8", StageMapObjectKind.MineCartFork, 0, 0, "", prefab, settings), 1f);
            var phases = instance.GetComponent<AnimolOperationalArtBinding>().PhasePlayer;
            Assert.That(phases.CurrentPhase, Is.EqualTo("neutral"));
            Assert.That(cart.SelectBranch(MineCartForkObject.Branch.Left), Is.True);
            Assert.That(phases.CurrentPhase, Is.EqualTo("select_left"));
            yield return new WaitForSeconds(.2f);
            Assert.That(cart.State, Is.EqualTo(MineCartForkObject.CartState.Stopped));
            Assert.That(instance.transform.position.x, Is.LessThan(0f));
            cart.ResetRuntimeState();
            Assert.That(Vector3.Distance(instance.transform.position, Vector3.zero), Is.LessThan(.001f));
            Assert.That(phases.CurrentPhase, Is.EqualTo("neutral"));
            Assert.That(cart.SelectBranch(MineCartForkObject.Branch.Right), Is.True);
            yield return new WaitForSeconds(.2f);
            Assert.That(instance.transform.position.x, Is.GreaterThan(0f));
            cart.ResetRuntimeState();
            Assert.That(cart.SelectBranch(MineCartForkObject.Branch.Left), Is.True, "repeated boarding must select again after reset");
            Object.Destroy(instance);
        }

        [UnityTest]
        public IEnumerator Balance_RepeatedOccupancyRecoversInLastLoweredDirection()
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_JADE_BALANCE.prefab");
#endif
            var left = Object.Instantiate(prefab).GetComponent<MoonJadeBalanceObject>();
            var right = Object.Instantiate(prefab).GetComponent<MoonJadeBalanceObject>();
            ConfigureBalance(left, prefab, "left", "right", 0);
            ConfigureBalance(right, prefab, "right", "left", 3);
            left.ResolveLink(); right.ResolveLink();
            left.SetOccupiedForTest(true); yield return new WaitForFixedUpdate();
            left.SetOccupiedForTest(false); yield return new WaitForFixedUpdate();
            Assert.That(left.GetComponent<AnimolOperationalArtBinding>().PhasePlayer.CurrentPhase, Is.EqualTo("recover"));
            right.SetOccupiedForTest(true); yield return new WaitForFixedUpdate();
            right.SetOccupiedForTest(false); yield return new WaitForFixedUpdate();
            Assert.That(right.GetComponent<AnimolOperationalArtBinding>().PhasePlayer.CurrentPhase, Is.EqualTo("recover_reverse"));
            Object.Destroy(left.gameObject); Object.Destroy(right.gameObject);
        }

        [UnityTest]
        public IEnumerator MoonStair_DefersWhileOccupied_AndRecoversWhenReturningToA()
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_PHASE_STAIR.prefab");
#endif
            var stair = Object.Instantiate(prefab).GetComponent<MoonPhaseStairObject>();
            stair.Configure(new StageMapObjectPlacement("m6", StageMapObjectKind.MoonPhaseStair, 0, 0, "", prefab, Settings(System.Array.Empty<Vector2Int>(), 0f, 3f)), 1f);
            var phases = stair.GetComponent<AnimolOperationalArtBinding>().PhasePlayer;
            stair.SetOccupiedForTest(true);
            Assert.That(stair.RequestToggle(), Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(stair.IsVertical, Is.False, "occupied transition must remain deferred");
            Assert.That(phases.CurrentPhase, Is.EqualTo("turn"));
            stair.SetOccupiedForTest(false); yield return new WaitForFixedUpdate();
            Assert.That(stair.IsVertical, Is.True);
            Assert.That(phases.CurrentPhase, Is.EqualTo("active"));
            stair.ReleasePatternPlate();
            Assert.That(stair.RequestToggle(), Is.True); yield return new WaitForFixedUpdate();
            Assert.That(stair.IsVertical, Is.False);
            Assert.That(phases.CurrentPhase, Is.EqualTo("recover"));
            Object.Destroy(stair.gameObject);
        }

        [UnityTest]
        public IEnumerator CheckpointResetDispatcher_RestoresNeutralAndAForm()
        {
            GameObject cartPrefab = null, stairPrefab = null;
#if UNITY_EDITOR
            cartPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/MapObjects/M9B/MINE_CART_FORK.prefab");
            stairPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/MapObjects/M9B/MOON_PHASE_STAIR.prefab");
#endif
            var loader = new GameObject("Loader").AddComponent<StageMapRuntimeLoader>();
            var root = new GameObject("M9_RuntimeObjects").transform; root.SetParent(loader.transform);
            typeof(StageMapRuntimeLoader).GetField("runtimeObjectRoot", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(loader, root);
            var cart = Object.Instantiate(cartPrefab, root).GetComponent<MineCartForkObject>();
            cart.Configure(new StageMapObjectPlacement("r8-reset", StageMapObjectKind.MineCartFork, 0, 0, "", cartPrefab, Settings(new[] { new Vector2Int(-1, 1), new Vector2Int(1, 1) }, 0f, 20f)), 1f);
            var stair = Object.Instantiate(stairPrefab, root).GetComponent<MoonPhaseStairObject>();
            stair.Configure(new StageMapObjectPlacement("m6-reset", StageMapObjectKind.MoonPhaseStair, 4, 0, "", stairPrefab, Settings(System.Array.Empty<Vector2Int>(), 0f, 3f)), 1f);
            cart.SelectBranch(MineCartForkObject.Branch.Right); stair.RequestToggle();
            yield return new WaitForFixedUpdate();
            loader.ResetRuntimeObjects();
            Assert.That(cart.State, Is.EqualTo(MineCartForkObject.CartState.Idle));
            Assert.That(cart.GetComponent<AnimolOperationalArtBinding>().PhasePlayer.CurrentPhase, Is.EqualTo("neutral"));
            Assert.That(stair.IsVertical, Is.False);
            Assert.That(stair.GetComponent<AnimolOperationalArtBinding>().PhasePlayer.CurrentPhase, Is.EqualTo("idle"));
            Object.Destroy(loader.gameObject);
        }

        [UnityTest]
        public IEnumerator Rail_WaitsForOccupiedSpawnBeforeSolidifying()
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/MapObjects/OBJ_RAIL_PLATFORM.prefab");
#endif
            var instance = Object.Instantiate(prefab);
            var rail = instance.GetComponent<RailPlatformObject>();
            rail.Configure(new StageMapObjectPlacement("c6", StageMapObjectKind.RailPlatform, 0, 0, "", prefab,
                Settings(new[] { Vector2Int.zero, Vector2Int.right }, 0f, 20f)), 1f);
            var passenger = new GameObject("Passenger").AddComponent<Rigidbody2D>(); passenger.bodyType = RigidbodyType2D.Kinematic;
            rail.RegisterPassenger(passenger);
            yield return new WaitForSeconds(.12f);
            rail.UnregisterPassenger(passenger);
            var blocker = new GameObject("SpawnBlocker"); blocker.transform.position = new Vector3(1f, .25f); blocker.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; blocker.AddComponent<BoxCollider2D>().size = Vector2.one;
            yield return new WaitForSeconds(1.5f);
            Assert.That(rail.Lifecycle, Is.EqualTo(RailPlatformObject.RailLifecycle.WaitingForClear));
            Assert.That(instance.GetComponent<BoxCollider2D>().enabled, Is.False);
            Object.Destroy(blocker);
            yield return new WaitForSeconds(1.5f);
            Assert.That(rail.Lifecycle, Is.EqualTo(RailPlatformObject.RailLifecycle.Idle));
            Assert.That(instance.GetComponent<BoxCollider2D>().enabled, Is.True);
            Object.Destroy(passenger.gameObject); Object.Destroy(instance);
        }

        private static StageMapObjectSettings Settings(Vector2Int[] path, float warning, float speed)
        {
            var value = new StageMapObjectSettings();
            value.EditorConfigure(2, StageMapObjectDirection.Right, new Vector2(3f, 1f), HalfBlockPlacement.Lower,
                SideSpringContactPolicy.FacingSideOnly, 0f, 0f, speed, 0f, 0f, 0f, .5f, 0f, true, path);
            value.EditorConfigureDesignBehavior(warning, .05f, .05f, MapObjectPassengerPolicy.Carry,
                MapObjectTriggerMode.OnOccupancy, 0f, 90f, 1f, true);
            return value;
        }

        private static void ConfigureBalance(MoonJadeBalanceObject balance, GameObject prefab, string id, string linkedId, int x)
        {
            var settings = Settings(new[] { new Vector2Int(x, 0), new Vector2Int(x, -1) }, 0f, 20f);
            settings.EditorConfigureAuthoring(MapObjectImplementationLevel.DevPlayable, new[] { linkedId }, 0,
                MapObjectResetPolicy.RespawnAndRetry, MapObjectRouteRole.Required, 1, string.Empty);
            balance.Configure(new StageMapObjectPlacement(id, StageMapObjectKind.MoonJadeBalance, x, 0, "", prefab, settings), 1f);
        }
    }
}
