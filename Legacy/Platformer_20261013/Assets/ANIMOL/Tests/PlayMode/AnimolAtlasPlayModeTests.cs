using System.Collections;
using Animol;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests.PlayMode
{
    public sealed class AnimolAtlasPlayModeTests
    {
        [UnityTest]
        public IEnumerator KeyPrefabsPlayPhasesAndKeepPartCollidersSynchronized()
        {
            foreach (var sample in new[] { ("C6", "travel", "cart"), ("M2", "active", "left_plate"), ("M6", "turn", "stair_1"), ("R8", "select_left", "cart") })
            {
                var prefab = Resources.Load<GameObject>("__never_used__");
#if UNITY_EDITOR
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ANIMOL/Generated/Prefabs/{sample.Item1}.prefab");
#endif
                Assert.That(prefab, Is.Not.Null, sample.Item1);
                var instance = Object.Instantiate(prefab);
                try
                {
                    var player = instance.GetComponent<AnimolPhasePlayer>();
                    Assert.That(player.PlayPhase(sample.Item2), Is.True, sample.Item1 + "/" + sample.Item2);
                    yield return null;
                    var part = instance.transform.Find(sample.Item3);
                    var follower = part.GetComponent<AnimolPartColliderFollower>();
                    follower.SyncNow();
                    var actual = part.GetComponent<BoxCollider2D>().size;
                    var expected = part.GetComponent<SpriteRenderer>().sprite.bounds.size;
                    Assert.That(actual.x, Is.EqualTo(expected.x).Within(.001f), sample.Item1 + " collider width");
                    Assert.That(actual.y, Is.EqualTo(expected.y).Within(.001f), sample.Item1 + " collider height");
                }
                finally
                {
                    Object.Destroy(instance);
                }
                yield return null;
            }
        }
    }
}
