using System.Collections;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ANIMOL.Tests.PlayMode
{
    public sealed class PlayerBodySizePlayModeTests
    {
        [UnityTest]
        public IEnumerator MotionFeedbackNeverResizesCollisionBody()
        {
            var root = new GameObject("Player visual feedback fixture", typeof(BoxCollider2D));
            try
            {
                var collider = root.GetComponent<BoxCollider2D>();
                collider.size = CampaignMapMarkerGeometry.PlayerBodySize;
                var visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);
                visual.localScale = new Vector3(.8f, .8f, 1f);
                var feedback = root.AddComponent<PlayerMotionFeedback>();
                System.Action[] events = { feedback.AnticipateJump, feedback.TakeOff, feedback.Land, feedback.FormChanged };
                foreach (var trigger in events)
                {
                    trigger();
                    yield return null;
                    Physics2D.SyncTransforms();
                    Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(collider.bounds.size.x, Is.EqualTo(.8f).Within(.0001f));
                    Assert.That(collider.bounds.size.y, Is.EqualTo(.8f).Within(.0001f));
                    Assert.That(visual.localScale, Is.Not.EqualTo(new Vector3(.8f, .8f, 1f)), "Visual feedback must still animate.");
                }
            }
            finally { Object.Destroy(root); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActualPlayerPassesOneTileCorridorAndRemainsOnFloor()
        {
#if UNITY_EDITOR
            var root = new GameObject("Player size physics fixture");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Gameplay/DevPlayer.prefab");
            try
            {
                var floor = new GameObject("Supporting floor", typeof(BoxCollider2D));
                floor.transform.SetParent(root.transform);
                floor.transform.position = new Vector3(1000, -.5f, 0);
                floor.GetComponent<BoxCollider2D>().size = new Vector2(12, 1);
                var ceiling = new GameObject("One tile headroom", typeof(BoxCollider2D));
                ceiling.transform.SetParent(root.transform);
                ceiling.transform.position = new Vector3(1000, 1.5f, 0);
                ceiling.GetComponent<BoxCollider2D>().size = new Vector2(12, 1);
                var player = Object.Instantiate(prefab, root.transform).GetComponent<DevPlayerController>();
                player.transform.position = CampaignMapMarkerGeometry.BodyAtFeet(player, new Vector3(997, .01f, 0));
                Physics2D.SyncTransforms();
                player.ApplySwipe(1, 1f);
                for (var i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
                var bounds = player.GetComponent<BoxCollider2D>().bounds;
                Assert.That(player.transform.position.x, Is.GreaterThan(999f), "Actual movement must pass below the ceiling.");
                Assert.That(player.IsGrounded, Is.True, player.LastGroundColliderName);
                Assert.That(bounds.min.y, Is.EqualTo(0f).Within(.03f));
                Assert.That(bounds.max.y, Is.LessThan(.85f));
                Assert.That(bounds.size.x, Is.EqualTo(.8f).Within(.0001f));
                Assert.That(bounds.size.y, Is.EqualTo(.8f).Within(.0001f));
            }
            finally { Object.Destroy(root); }
            yield return null;
#else
            Assert.Ignore("Editor prefab fixture required.");
            yield break;
#endif
        }
    }
}
