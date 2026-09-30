using System.Collections;
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
            Assert.That(cart.SelectBranch(MineCartForkObject.Branch.Left), Is.True);
            yield return new WaitForSeconds(.2f);
            Assert.That(cart.State, Is.EqualTo(MineCartForkObject.CartState.Stopped));
            Assert.That(instance.transform.position.x, Is.LessThan(0f));
            cart.ResetRuntimeState();
            Assert.That(Vector3.Distance(instance.transform.position, Vector3.zero), Is.LessThan(.001f));
            Assert.That(cart.SelectBranch(MineCartForkObject.Branch.Right), Is.True);
            yield return new WaitForSeconds(.2f);
            Assert.That(instance.transform.position.x, Is.GreaterThan(0f));
            Object.Destroy(instance);
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
    }
}
