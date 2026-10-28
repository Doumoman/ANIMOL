#if UNITY_EDITOR
using System.Collections;
using ANIMOL.Puzzle;
using ANIMOL.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class Starway20PlayTests
    {
        [UnityTest] public IEnumerator AllTwentyStagesLoadWithHudVisualsAndSevenByElevenCamera()
        {
            var set = AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>(
                "Assets/ANIMOL/Data/Puzzle/Starway20/Starway20MapSet.asset");
            Assert.That(set, Is.Not.Null);
            Assert.That(set.maps.Count, Is.EqualTo(20));
            var cameraObject = new GameObject("Main Camera", typeof(Camera)); cameraObject.tag = "MainCamera";
            var host = new GameObject("Starway20 stage test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            controller.hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/hud_visual.prefab");
            try
            {
                foreach (var entry in set.maps)
                {
                    controller.Load(entry.map); yield return null; yield return null;
                    Assert.That(controller.Session, Is.Not.Null, entry.sourceId);
                    Assert.That(controller.Devices.MissingBindings, Is.Zero, entry.sourceId);
                    Assert.That(controller.Session.Collected, Is.Zero, entry.sourceId);
                    Assert.That(controller.GetComponentInChildren<PuzzleHudView>().bubbles.text, Is.EqualTo("0/3"), entry.sourceId);
                    Assert.That(controller.RuntimeMap.targetBubbles, Is.EqualTo(3), entry.sourceId);
                    Assert.That(controller.FollowCamera.GetComponent<Camera>().orthographicSize, Is.EqualTo(5.5f).Within(.001f), entry.sourceId);
                    Assert.That(controller.Session.Cell, Is.EqualTo(entry.map.start), entry.sourceId);
                }
            }
            finally { Object.Destroy(host); Object.Destroy(cameraObject); }
        }
    }
}
#endif
