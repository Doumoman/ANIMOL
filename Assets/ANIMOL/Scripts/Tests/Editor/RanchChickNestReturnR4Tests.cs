using ANIMOL.Art.RanchDevicesV13;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class RanchChickNestReturnR4Tests
    {
        [Test] public void CommittedArrivalDeduplicatesAndRestoreCancelsPresentation()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RanchDevicesV13Catalog>(
                "Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_DEVICES_V13/_GeneratedV13/RanchDevicesV13Catalog.asset");
            Assert.That(catalog, Is.Not.Null);
            var nest = new GameObject("Nest R4 test");
            try
            {
                var child = new GameObject("Dedicated visual");
                child.transform.SetParent(nest.transform, false);
                child.AddComponent<SpriteRenderer>();
                var sprite = child.AddComponent<RanchDeviceVisualV13>();
                var presentation = nest.AddComponent<RanchChickNestReturnVisualR4>();
                Assert.That(presentation.Configure(catalog, sprite, out var error), Is.True, error);
                Assert.That(presentation.SetResolvedState(false, "south", out error), Is.True, error);
                Assert.That(presentation.PlayCommittedArrival("uncommitted", "up", out error), Is.False);
                Assert.That(sprite.CurrentKey, Is.EqualTo("nest/idle"));
                Assert.That(presentation.PlayCommittedArrival("board:1:chick", "north", out error), Is.True, error);
                Assert.That(sprite.CurrentKey, Is.EqualTo("nest_return/north/arrive"));
                Assert.That(presentation.PlayCommittedArrival("board:1:chick", "north", out error), Is.True, error);
                Assert.That(sprite.CurrentKey, Is.EqualTo("nest_return/north/arrive"));
                Assert.That(presentation.PlayCommittedArrival("board:2:chick", "east", out error), Is.False);
                Assert.That(presentation.SetResolvedState(true, "north", out error), Is.True, error);
                Assert.That(presentation.IsPlaying, Is.False);
                Assert.That(sprite.CurrentKey, Is.EqualTo("nest_return/north/occupied"));
                Assert.That(presentation.SetResolvedState(false, "south", out error), Is.True, error);
                Assert.That(sprite.CurrentKey, Is.EqualTo("nest/idle"));
            }
            finally { Object.DestroyImmediate(nest); }
        }
    }
}
