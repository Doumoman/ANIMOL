using System.Collections;
using System.IO;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.DiceHintV01;
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
                    var names = controller.GetComponentsInChildren<Transform>()
                        .Select(t => t.name).ToArray();
                    foreach (var omitted in new[] { "Curtain lintel", "Left curtain", "Right curtain",
                        "Static footlights", "velvet_swag_lower_right", "Left seats", "Right seats", "Organ" })
                        Assert.That(names, Does.Not.Contain(omitted), entry.stageId + " " + omitted);
                    Assert.That(names, Does.Contain("Sky base"), entry.stageId);
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

        [UnityTest] public IEnumerator DiceRollChangesCommittedFacesAndSpriteThenReloadRestoresThem()
        {
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-05_MapData.asset");
            var host = new GameObject("Echo Theater dice distinction test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                Sprite initialSprite = null;
                int[] initialFaces = null;
                for (var load = 0; load < 2; load++)
                {
                    controller.Load(map);
                    Assert.That(controller.Devices.MissingBindings, Is.Zero);
                    var die = controller.Session.Devices.First(d => d.role == PrototypeRole.Dice &&
                        d.variant != "target" && d.variant != "socket");
                    var target = controller.Session.Devices.First(d => d.role == PrototypeRole.Dice &&
                        (d.variant == "target" || d.variant == "socket"));
                    var dieSprite = controller.Devices.Actor(die.id).GetComponentInChildren<SpriteRenderer>().sprite;
                    var targetView = controller.Devices.Actor(target.id).GetComponent<ANIMOL.Presentation.DeviceVisualView>();
                    var targetFace = controller.Devices.Actor(target.id).GetComponent<DiceGoalFaceViewV02>();
                    var targetFloor = controller.Devices.Actor(target.id)
                        .Find("__DiceGoalPlatform_VisualOnly").GetComponent<SpriteRenderer>();
                    Assert.That(dieSprite, Is.Not.Null);
                    Assert.That(targetFace.RequiredFace, Is.EqualTo(target.number));
                    Assert.That(targetView.visual.enabled, Is.True);
                    Assert.That(targetView.visual.sprite, Is.Null);
                    Assert.That(AssetDatabase.GetAssetPath(targetFloor.sprite),
                        Does.EndWith("/Sprites/GoalPlatform/ready.png"));
                    Assert.That(AssetDatabase.GetAssetPath(dieSprite), Does.Contain("/dice_block/orientations/"));
                    var dieState = controller.Session.States[die.id];
                    if (load == 0)
                    {
                        initialSprite = dieSprite;
                        initialFaces = (int[])dieState.dice.Clone();
                        var initialCell = dieState.cell;
                        for (var turn = 0; turn < map.scenario.verificationRoute.Length &&
                            controller.Session.States[die.id].cell == initialCell; turn++)
                        {
                            Assert.That(controller.RequestMove(Heading(map.scenario.verificationRoute[turn])),
                                Is.True, "ET-05 input " + turn);
                            while (controller.IsMoving) yield return null;
                        }
                        dieState = controller.Session.States[die.id];
                        Assert.That(dieState.cell, Is.Not.EqualTo(initialCell), "ET-05 must actually roll the die.");
                        var heading = dieState.cell - initialCell;
                        Assert.That(heading.sqrMagnitude, Is.EqualTo(1));
                        var expected = (int[])initialFaces.Clone();
                        PuzzleDeviceSession.Roll(expected, heading);
                        Assert.That(dieState.dice, Is.EqualTo(expected));
                        Assert.That(dieState.dice[0], Is.Not.EqualTo(initialFaces[0]));
                        yield return new WaitForSecondsRealtime(.2f);
                        var rolledSprite = controller.Devices.Actor(die.id).GetComponentInChildren<SpriteRenderer>().sprite;
                        Assert.That(rolledSprite, Is.Not.SameAs(initialSprite));
                        Assert.That(AssetDatabase.GetAssetPath(rolledSprite), Does.Contain("/dice_block/orientations/"));
                    }
                    else
                    {
                        Assert.That(dieState.dice, Is.EqualTo(initialFaces));
                        Assert.That(dieSprite, Is.SameAs(initialSprite));
                    }
                    yield return null;
                }
            }
            finally { Object.Destroy(host); }
        }

        [UnityTest] public IEnumerator MapEditorPreviewRollsDieOntoItsTarget()
        {
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device22_Dice.asset");
            var host = new GameObject("Map editor preview dice test");
            var preview = host.AddComponent<PuzzleController>();
            preview.stage = map;
            try
            {
                yield return null;
                var controller = host.GetComponent<PuzzlePrototypeController>();
                Assert.That(controller, Is.Not.Null);
                Assert.That(controller.Session, Is.Not.Null);
                yield return null;
                var goal = controller.Devices.Actor("target");
                var face = goal.GetComponent<DiceGoalFaceViewV02>();
                var floor = goal.transform.Find("__DiceGoalPlatform_VisualOnly");
                var hint = controller.Devices.Actor("die").Find("__DiceNextFaceHint_VisualOnly");
                Assert.That(hint, Is.Not.Null);
                Assert.That(hint.gameObject.activeSelf, Is.True);
                Assert.That(AssetDatabase.GetAssetPath(hint.GetComponent<SpriteRenderer>().sprite),
                    Does.EndWith("/Sprites/Faces/face_4.png"));
                Assert.That(face.RequiredFaceVisible, Is.False);
                Assert.That(floor.gameObject.activeSelf, Is.False);
                Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
                var pressing = goal.transform.Find("__DiceGoalPressCopy_VisualOnly");
                var deadline = Time.realtimeSinceStartup + 2f;
                while (controller.IsMoving && !pressing.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(pressing.gameObject.activeSelf, Is.True);
                Assert.That(AssetDatabase.GetAssetPath(pressing.GetComponent<SpriteRenderer>().sprite),
                    Does.EndWith("/devices/dice_block/orientations/o01.png"));
                Assert.That(controller.RequestMove(Vector2Int.right), Is.False);
                while (controller.IsMoving) yield return null;
                Assert.That(controller.Session.States["die"].dice[0], Is.EqualTo(4));
                Assert.That(controller.Session.States["die"].removed, Is.True);
                Assert.That(controller.Session.States["target"].converted, Is.True);
                Assert.That(controller.Session.States["target"].active, Is.True);
                Assert.That(controller.Devices.Actor("die").gameObject.activeSelf, Is.False);
                Assert.That(face.RequiredFaceVisible, Is.False);
                Assert.That(floor.gameObject.activeSelf, Is.True);
                Assert.That(AssetDatabase.GetAssetPath(floor.GetComponent<SpriteRenderer>().sprite),
                    Does.EndWith("/Sprites/GoalPlatform/walkable.png"));
                Assert.That(goal.GetComponent<ANIMOL.Presentation.DeviceVisualView>().visual.enabled, Is.True);
                Assert.That(controller.Session.Cost, Is.EqualTo(1));
                var convertedSnapshot = controller.Session.Copy();
                Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
                while (controller.IsMoving) yield return null;
                Assert.That(controller.Session.Cell, Is.EqualTo(new Vector2Int(3, 2)));
                Assert.That(controller.Session.Cost, Is.EqualTo(2));
                Assert.That(controller.UndoLast(), Is.True);
                Assert.That(controller.Session.States["target"].converted, Is.True);
                Assert.That(controller.UndoLast(), Is.True);
                Assert.That(controller.Session.States["die"].removed, Is.False);
                Assert.That(controller.Session.States["die"].dice[0], Is.EqualTo(1));
                Assert.That(controller.Session.States["target"].converted, Is.False);
                Assert.That(controller.Devices.Actor("die").gameObject.activeSelf, Is.True);
                Assert.That(face.RequiredFaceVisible, Is.True);
                controller.RestoreSnapshot(convertedSnapshot);
                Assert.That(controller.Session.States["target"].converted, Is.True);
                Assert.That(controller.Devices.Actor("die").gameObject.activeSelf, Is.False);
                Assert.That(pressing.gameObject.activeSelf, Is.False);
                Assert.That(AssetDatabase.GetAssetPath(floor.GetComponent<SpriteRenderer>().sprite),
                    Does.EndWith("/Sprites/GoalPlatform/walkable.png"));
                controller.ResetStage();
                Assert.That(controller.Session.States["target"].converted, Is.False);
                Assert.That(controller.Devices.Actor("die").gameObject.activeSelf, Is.True);
                controller.Load(map);
                Assert.That(controller.Session.States["target"].converted, Is.False);
                Assert.That(controller.Devices.Actor("die").gameObject.activeSelf, Is.True);
                Assert.That(controller.Devices.Actor("target").GetComponent<DiceGoalFaceViewV02>()
                    .RequiredFace, Is.EqualTo(4));
                Assert.That(controller.Devices.Actor("target").Find("__DiceGoalPressCopy_VisualOnly")
                    .gameObject.activeSelf, Is.False);
                Assert.That(controller.Devices.MissingBindings, Is.Zero);
            }
            finally { Object.Destroy(host); }
        }

        [UnityTest] public IEnumerator SelectedEchoTheaterMapRollsDieWhenPushed()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-01_MapData.asset");
            var map = Object.Instantiate(source);
            map.start = new Vector2Int(11, 7);
            map.InvalidateIndex();
            var host = new GameObject("ET-01 map editor preview dice test");
            var preview = host.AddComponent<PuzzleController>();
            preview.stage = map;
            try
            {
                yield return null;
                var controller = host.GetComponent<PuzzlePrototypeController>();
                Assert.That(controller, Is.Not.Null);
                Assert.That(controller.Session, Is.Not.Null);
                Assert.That(controller.Devices.MissingBindings, Is.Zero);
                var die = controller.Devices.Actor("dice_1");
                var renderer = die.GetComponent<ANIMOL.Presentation.DeviceVisualView>().visual;
                var before = renderer.sprite;
                Assert.That(controller.RequestMove(Vector2Int.up), Is.True);
                while (controller.IsMoving) yield return null;
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(controller.Session.States["dice_1"].dice[0], Is.EqualTo(2));
                Assert.That(renderer.sprite, Is.Not.SameAs(before));
                Assert.That(AssetDatabase.GetAssetPath(renderer.sprite),
                    Does.EndWith("/devices/dice_block/orientations/o02.png"));
                Assert.That(controller.Devices.MissingBindings, Is.Zero);
            }
            finally { Object.Destroy(host); Object.Destroy(map); }
        }

        [UnityTest] public IEnumerator WrongFaceRemainsSolidAndPushQueryDoesNotCommit()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device22_Dice.asset");
            var map = Object.Instantiate(source);
            map.scenario = Object.Instantiate(source.scenario);
            map.scenario.devices.First(d => d.id == "target").number = 5;
            map.InvalidateIndex();
            var host = new GameObject("Wrong face dice test");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                controller.Load(map);
                var state = controller.Session;
                var initialFaces = (int[])state.States["die"].dice.Clone();
                for (var i = 0; i < 4; i++)
                {
                    Assert.That(state.CanPushDieOneCell("die", Vector2Int.right, out var predicted), Is.True);
                    Assert.That(predicted[0], Is.EqualTo(4));
                }
                Assert.That(state.Cost, Is.Zero);
                Assert.That(state.States["die"].cell, Is.EqualTo(new Vector2Int(2, 2)));
                Assert.That(state.States["die"].dice, Is.EqualTo(initialFaces));
                Assert.That(state.Undo(), Is.False);
                Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
                while (controller.IsMoving) yield return null;
                Assert.That(state.States["die"].removed, Is.False);
                Assert.That(state.States["die"].cell, Is.EqualTo(new Vector2Int(3, 2)));
                Assert.That(state.States["target"].converted, Is.False);
                Assert.That(state.Cost, Is.EqualTo(1));
                Assert.That(controller.RequestMove(Vector2Int.right), Is.True);
                while (controller.IsMoving) yield return null;
                Assert.That(state.States["die"].cell, Is.EqualTo(new Vector2Int(4, 2)));
                Assert.That(state.Cell, Is.EqualTo(new Vector2Int(3, 2)));
                Assert.That(state.Cost, Is.EqualTo(2));
                controller.ResetStage();
                Assert.That(state.Cost, Is.Zero);
                Assert.That(state.States["die"].dice, Is.EqualTo(initialFaces));
                Assert.That(state.States["target"].converted, Is.False);
                Assert.That(controller.Devices.MissingBindings, Is.Zero);
            }
            finally { Object.Destroy(host); Object.Destroy(map.scenario); Object.Destroy(map); }
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
                    var dominoApproach = Vector2Int.zero;
                    for (var turn = 0; turn < limit; turn++)
                    {
                        if (turn == limit - 1)
                        {
                            if (index == 2) dominoApproach = controller.Session.Cell;
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
                        Assert.That(controller.Session.Cell, Is.EqualTo(dominoApproach));
                        Assert.That((Vector2)controller.Player.position, Is.EqualTo((Vector2)dominoApproach));
                        foreach (var id in new[] { "D1", "D2", "D3" })
                        {
                            var actor = controller.Devices.Actor(id);
                            Assert.That(actor.gameObject.activeSelf, Is.True, id + " folded actor");
                            var visual = actor.GetComponent<ANIMOL.Presentation.DeviceVisualView>();
                            for (var frame = 0; frame < 120 && visual.CurrentState != "folded_n"; frame++)
                                yield return null;
                            Assert.That(visual.CurrentState, Is.EqualTo("folded_n"), id);
                            Assert.That(AssetDatabase.GetAssetPath(visual.visual.sprite),
                                Does.EndWith("/devices/domino_wall/north/frame_04.png"), id);
                        }
                        Assert.That(controller.UndoLast(), Is.True);
                        foreach (var id in new[] { "D1", "D2", "D3" })
                        {
                            var actor = controller.Devices.Actor(id);
                            var visual = actor.GetComponent<ANIMOL.Presentation.DeviceVisualView>();
                            Assert.That(actor.gameObject.activeSelf, Is.True, id + " restored actor");
                            Assert.That(visual.CurrentState, Is.EqualTo("standing_n"), id);
                        }
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
