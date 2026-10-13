using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests.EditMode
{
    public sealed class AnimolOperationalIntegrationTests
    {
        [TestCase("OBJ_RAIL_PLATFORM", typeof(RailPlatformObject), "cart", "wheels")]
        [TestCase("M9B/MOON_JADE_BALANCE", typeof(MoonJadeBalanceObject), "left_plate", "right_plate")]
        [TestCase("M9B/MOON_PHASE_STAIR", typeof(MoonPhaseStairObject), "stair_1", "stair_3")]
        [TestCase("M9B/MINE_CART_FORK", typeof(MineCartForkObject), "branching_track", "cart")]
        public void OperationalPrefab_PreservesPhysicsAndIndependentArt(string path, System.Type runtimeType, string first, string second)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ANIMOL/Prefabs/MapObjects/{path}.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent(runtimeType), Is.Not.Null);
            Assert.That(prefab.GetComponent<BoxCollider2D>(), Is.Not.Null);
            var binding = prefab.GetComponent<AnimolOperationalArtBinding>();
            Assert.That(binding, Is.Not.Null);
            Assert.That(binding.ArtRoot.transform.Cast<Transform>().Any(x => x.name == first), Is.True, first);
            Assert.That(binding.ArtRoot.transform.Cast<Transform>().Any(x => x.name == second), Is.True, second);
            Assert.That(binding.ArtRoot.GetComponentsInChildren<Collider2D>(true).All(x => !x.enabled), Is.True);
        }

        [Test]
        public void MineCartType_IsDevPlayable()
        {
            var type = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeDefinition>("Assets/ANIMOL/Data/Development/M9BThemePlatforms/Types/MINE_CART_FORK.asset");
            Assert.That(type.ImplementationLevel, Is.EqualTo(MapObjectImplementationLevel.DevPlayable));
            Assert.That(type.BehaviorComponentIds, Does.Contain("MineCartFork"));
        }

        [Test]
        public void ExistingRuntimeObstacles_HaveOperationalArtBindings()
        {
            var paths = new[]
            {
                "OBJ_SIDE_SPRING", "OBJ_POUNDER", "OBJ_RICE_SLOW", "TILE_HALF_BLOCK", "TILE_DROP_PLATFORM",
                "M9B/MOON_LANTERN_STEP", "M9B/MOON_RABBIT_BOWL", "M9B/MOON_JADE_PENDULUM", "M9B/MOON_SLIDING_EAVE",
                "M9B/CLOUD_WHALE_FERRY", "M9B/CLOUD_BALLOON_TETHER", "M9B/INK_BLOT", "M9B/LIB_INDEX_DRAWER",
                "M9B/GREEN_SAND_RETRACE", "M9B/PRISM_SPRING", "M9B/MINE_MAGNET_PAIR"
            };
            foreach (var path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ANIMOL/Prefabs/MapObjects/{path}.prefab");
                Assert.That(prefab, Is.Not.Null, path);
                var binding = prefab.GetComponent<AnimolOperationalArtBinding>();
                Assert.That(binding, Is.Not.Null, path);
                Assert.That(binding.PhasePlayer, Is.Not.Null, path);
                Assert.That(binding.ArtRoot.GetComponentsInChildren<Collider2D>(true).All(x => !x.enabled), Is.True, path);
            }
        }
    }
}
