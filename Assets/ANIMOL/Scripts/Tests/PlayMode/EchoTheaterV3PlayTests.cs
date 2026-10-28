using System.Collections;
using System.IO;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace ANIMOL.Tests
{
    public sealed class EchoTheaterV3PlayTests
    {
        static Vector2Int Heading(char c) => c == 'U' ? Vector2Int.up : c == 'D' ? Vector2Int.down :
            c == 'L' ? Vector2Int.left : Vector2Int.right;

        [UnityTest] public IEnumerator AllThirtyFiveStagesLoadVisualBindings()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset");
            var host = new GameObject("Echo Theater load sweep");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                foreach (var entry in catalog.maps)
                {
                    controller.Load(entry.map);
                    Assert.That(controller.Session, Is.Not.Null, entry.stageId);
                    Assert.That(controller.Devices.MissingBindings, Is.Zero, entry.stageId);
                    foreach (var die in entry.map.scenario.devices)
                    {
                        if (die.role != PrototypeRole.Dice || die.variant == "socket" || die.variant == "target") continue;
                        var actor = controller.Devices.Actor(die.id);
                        var face = actor.GetComponent<EchoTheaterDiceFaceView>();
                        var sprite = actor.GetComponent<ANIMOL.Presentation.DeviceVisualView>().visual.sprite;
                        Assert.That(face, Is.Not.Null, entry.stageId + " " + die.id);
                        Assert.That(AssetDatabase.GetAssetPath(sprite), Does.Contain(
                            "/ANIMOL_ECHO_THEATER_CARTOON_V03/devices/dice_block/orientations/"),
                            entry.stageId + " " + die.id);
                    }
                    yield return null;
                }
            }
            finally { Object.Destroy(host); }
        }

        [UnityTest] public IEnumerator CampaignSceneLoadsOrderedStagesAndKeepsStableSelectionId()
        {
            const string key = "ANIMOL.EchoTheater.Selected";
            const string clearedKey = "ANIMOL.EchoTheater.Cleared.ET-01";
            var hadKey = PlayerPrefs.HasKey(key);
            var previous = PlayerPrefs.GetInt(key, 0);
            var hadClear = PlayerPrefs.HasKey(clearedKey);
            var previousClear = PlayerPrefs.GetInt(clearedKey, 0);
            try
            {
                var operation = SceneManager.LoadSceneAsync("ANIMOL_Echo_Theater");
                while (!operation.isDone) yield return null;
                yield return null;
                var campaign = Object.FindFirstObjectByType<EchoTheaterCampaignController>();
                Assert.That(campaign, Is.Not.Null);
                Assert.That(campaign.mapSet.maps.Count, Is.EqualTo(35));
                PlayerPrefs.DeleteKey(clearedKey);
                campaign.SelectStage(0);
                foreach (var action in campaign.puzzle.stage.scenario.verificationRoute)
                    Assert.That(campaign.puzzle.Session.Apply(Heading(action)).accepted, Is.True);
                Assert.That(campaign.puzzle.Session.Won, Is.True);
                yield return null;
                Assert.That(EchoTheaterCampaignController.IsCleared("ET-01"), Is.True);
                campaign.SelectStage(34);
                Assert.That(campaign.mapSet.maps[34].stageId, Is.EqualTo("ET-35"));
                Assert.That(campaign.puzzle.stage, Is.EqualTo(campaign.mapSet.maps[34].map));
                Assert.That(campaign.puzzle.Session, Is.Not.Null);
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(key, previous); else PlayerPrefs.DeleteKey(key);
                if (hadClear) PlayerPrefs.SetInt(clearedKey, previousClear); else PlayerPrefs.DeleteKey(clearedKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest] public IEnumerator DominoAndDiceStagesBindVisualsAndOpenPassages()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EchoTheaterMapSet>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset");
            Assert.That(catalog, Is.Not.Null);
            var host = new GameObject("Echo Theater V3 PlayMode test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            Directory.CreateDirectory("Logs");
            try
            {
                foreach (var index in new[] { 2, 4 })
                {
                    var map = catalog.maps[index].map;
                    controller.Load(map);
                    Assert.That(controller.Devices.MissingBindings, Is.Zero, map.name);
                    var limit = index == 2 ? 27 : 20;
                    for (var turn = 0; turn < limit; turn++)
                    {
                        if (turn == limit - 1)
                        {
                            yield return null;
                            ScreenCapture.CaptureScreenshot("Logs/" + map.name + "-before.png");
                            yield return null;
                        }
                        Assert.That(controller.RequestMove(Heading(map.scenario.verificationRoute[turn])), Is.True,
                            map.name + " input " + (turn + 1));
                        while (controller.IsMoving) yield return null;
                    }
                    Assert.That(controller.Devices.MissingBindings, Is.Zero, map.name + " after action");
                    ScreenCapture.CaptureScreenshot("Logs/" + map.name + "-after.png");
                    yield return null;
                    if (index == 2)
                    {
                        Assert.That(controller.Session.States["D1"].active && controller.Session.States["D2"].active &&
                            controller.Session.States["D3"].active, Is.True);
                    }
                    else
                    {
                        Assert.That(controller.Session.States["Q1"].removed, Is.True);
                        Assert.That(controller.Session.States["G1"].active, Is.True);
                        Assert.That(controller.Session.States["Q1"].dice[0], Is.EqualTo(6));
                    }
                }
            }
            finally { Object.Destroy(host); }
        }
    }
}
