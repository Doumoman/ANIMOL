using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests.EditMode
{
    public sealed class AnimolStatefulPass4EditModeTests
    {
        [TestCase("CLOUD_SHEEP_STEP", typeof(CloudSheepStepObject), 1)]
        [TestCase("PAGE_BRIDGE", typeof(PageBridgeObject), 3)]
        [TestCase("LIB_POPUP_STAIR", typeof(LibPopupStairObject), 3)]
        [TestCase("DEW_SEED_STEP", typeof(DewSeedStepObject), 3)]
        public void PromotedPrefabOwnsOperationalPhysicsAndGeneratedArtOwnsNone(string id, System.Type runtimeType, int colliderCount)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ANIMOL/Prefabs/MapObjects/M9B/{id}.prefab");
            Assert.That(prefab.GetComponent(runtimeType), Is.Not.Null);
            Assert.That(prefab.GetComponent<PrototypeMapObject>(), Is.Null);
            var binding = prefab.GetComponent<AnimolOperationalArtBinding>();
            Assert.That(binding, Is.Not.Null);
            Assert.That(binding.ArtRoot.GetComponentsInChildren<Collider2D>(true).All(x => !x.enabled), Is.True);
            var operational = prefab.GetComponentsInChildren<BoxCollider2D>(true).Where(x => !x.transform.IsChildOf(binding.ArtRoot.transform)).ToArray();
            Assert.That(operational.Length, Is.EqualTo(colliderCount));
            if (colliderCount == 3) Assert.That(operational.Select(x => x.transform.parent).Distinct().Single().name, Is.EqualTo("OperationalPhysicsParts"));
            var type = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeDefinition>($"Assets/ANIMOL/Data/Development/M9BThemePlatforms/Types/{id}.asset");
            Assert.That(type.ImplementationLevel, Is.EqualTo(MapObjectImplementationLevel.DevPlayable));
        }

        [Test]
        public void GeneratedMapNeverUsesPrototypeAsRequiredRouteOrRewardAccess()
        {
            var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>("Assets/ANIMOL/Data/Development/M9BThemePlatforms/DEV-M9B-PALETTE-45.asset");
            Assert.That(map.Objects.Count(x => x.Settings.ImplementationLevel == MapObjectImplementationLevel.PlaceablePrototype), Is.EqualTo(17));
            Assert.That(map.Objects.Any(x => x.Settings.ImplementationLevel == MapObjectImplementationLevel.PlaceablePrototype && x.Settings.RouteRole == MapObjectRouteRole.Required), Is.False);
            Assert.That(StageMapValidator.ValidateStructure(map).Errors, Has.None.Contains("PlaceablePrototype objects cannot"));
        }

        [Test]
        public void LabAndGeneratedMapContainAllFourPassedTypes()
        {
            foreach (var path in new[] { "Assets/ANIMOL/Data/Development/M9BThemePlatforms/DEV-M9B-REPRESENTATIVE-LAB.asset", "Assets/ANIMOL/Data/Development/M9BThemePlatforms/DEV-M9B-PALETTE-45.asset" })
            {
                var ids = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(path).Objects.Select(x => x.DataKey).ToArray();
                CollectionAssert.IsSubsetOf(new[] { "CLOUD_SHEEP_STEP", "PAGE_BRIDGE", "LIB_POPUP_STAIR", "DEW_SEED_STEP" }, ids);
            }
        }
    }
}
