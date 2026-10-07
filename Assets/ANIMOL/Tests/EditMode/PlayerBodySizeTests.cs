using ANIMOL.Core;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class PlayerBodySizeTests
    {
        [Test]
        public void PrefabCollisionAndVisibleBodyMatchAndSpawnKeepsAuthoredFeet()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Gameplay/DevPlayer.prefab");
            var instance = Object.Instantiate(prefab);
            try
            {
                var player = instance.GetComponent<DevPlayerController>();
                var feet = new Vector3(13.5f, 7f, 0);
                instance.transform.position = CampaignMapMarkerGeometry.BodyAtFeet(player, feet);
                Physics2D.SyncTransforms();
                var bounds = instance.GetComponent<BoxCollider2D>().bounds;
                Assert.That(bounds.size.x, Is.EqualTo(.8f).Within(.0001f));
                Assert.That(bounds.size.y, Is.EqualTo(.8f).Within(.0001f));
                Assert.That(bounds.min.y, Is.EqualTo(feet.y).Within(.0001f));
                Assert.That(bounds.center.x, Is.EqualTo(feet.x).Within(.0001f));
                var visual = instance.transform.Find("Visual").GetComponent<Renderer>().bounds;
                Assert.That(visual.size.x, Is.EqualTo(bounds.size.x).Within(.0001f));
                Assert.That(visual.size.y, Is.EqualTo(bounds.size.y).Within(.0001f));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [TestCase(StageMapObjectKind.PlayerStart)]
        [TestCase(StageMapObjectKind.Checkpoint)]
        public void SafeFeetFitOneTileHeadroomButRejectSolidBodyOverlap(StageMapObjectKind kind)
        {
            var map = ScriptableObject.CreateInstance<StageMapDefinition>();
            try
            {
                map.EditorInitializeIdentity("DEV-PLAYER-SIZE", "T01");
                map.EditorInitializeBoundsFromAuthoredContent(1f);
                map.EditorPlaceObject("safe", kind, 2, 1);
                map.EditorSetCell(2, 0, StageMapLayer.Terrain, "floor");
                map.EditorSetCell(2, 2, StageMapLayer.Terrain, "ceiling");
                var marker = map.Objects[0];
                Assert.That(StageMapMarkerValidation.ValidateSafeFeet(map, marker), Is.Empty,
                    "A square 0.8-tile body fits below the ceiling; the previous 1.2-tile body did not.");
                map.EditorSetCell(2, 1, StageMapLayer.Terrain, "blocked");
                Assert.That(StageMapMarkerValidation.ValidateSafeFeet(map, marker),
                    Has.Some.Contains("overlaps solid terrain"));
            }
            finally { Object.DestroyImmediate(map); }
        }
    }
}
