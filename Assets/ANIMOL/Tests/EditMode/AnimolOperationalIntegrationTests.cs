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
    }
}
