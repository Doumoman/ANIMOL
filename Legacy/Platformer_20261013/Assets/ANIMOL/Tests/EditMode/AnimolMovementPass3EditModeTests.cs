using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests.EditMode
{
    public sealed class AnimolMovementPass3EditModeTests
    {
        [TestCase("CLOUD_SAIL_STEP", typeof(CloudSailStepObject))]
        [TestCase("BOOKMARK_LIFT", typeof(BookmarkLiftObject))]
        [TestCase("GLASS_VINE_LIFT", typeof(GlassVineLiftObject))]
        [TestCase("LIB_SPINE_BRAKE", typeof(LibSpineBrakeObject))]
        public void PromotedPrefabOwnsRuntimePhysicsAndDisablesGeneratedColliders(string id, System.Type runtimeType)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ANIMOL/Prefabs/MapObjects/M9B/{id}.prefab");
            Assert.That(prefab.GetComponent(runtimeType), Is.Not.Null);
            Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<BoxCollider2D>(), Is.Not.Null.And.Property("isTrigger").False);
            Assert.That(prefab.GetComponent<PrototypeMapObject>(), Is.Null);
            var binding = prefab.GetComponent<AnimolOperationalArtBinding>();
            Assert.That(binding.ArtRoot.GetComponentsInChildren<Collider2D>(true).All(x => !x.enabled), Is.True);
            var type = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeDefinition>($"Assets/ANIMOL/Data/Development/M9BThemePlatforms/Types/{id}.asset");
            Assert.That(type.ImplementationLevel, Is.EqualTo(MapObjectImplementationLevel.DevPlayable));
        }

        [Test]
        public void RepresentativeAndGeneratedMapsContainAllFourPromotedTypes()
        {
            foreach (var path in new[]
                     {
                         "Assets/ANIMOL/Data/Development/M9BThemePlatforms/DEV-M9B-REPRESENTATIVE-LAB.asset",
                         "Assets/ANIMOL/Data/Development/M9BThemePlatforms/DEV-M9B-PALETTE-45.asset"
                     })
            {
                var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(path);
                var ids = map.Objects.Select(x => x.DataKey).ToArray();
                CollectionAssert.IsSubsetOf(new[] { "CLOUD_SAIL_STEP", "BOOKMARK_LIFT", "GLASS_VINE_LIFT", "LIB_SPINE_BRAKE" }, ids, path);
            }
        }
    }
}
