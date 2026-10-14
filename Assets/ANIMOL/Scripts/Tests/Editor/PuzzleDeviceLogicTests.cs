using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ANIMOL.Puzzle;
using ANIMOL.Presentation;
using ANIMOL.Presentation.Editor;
namespace ANIMOL.Tests
{
    public sealed class PuzzleDeviceLogicTests
    {
        readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();
        PuzzleMap Room(int id) => AssetDatabase.FindAssets("t:PuzzleMap",new[]{PuzzlePrototypeBuilder.Root}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("/Device"+id.ToString("00")+"_")).Select(AssetDatabase.LoadAssetAtPath<PuzzleMap>).Single();
        PuzzleMap Clone(int id)
        {
            var map=UnityEngine.Object.Instantiate(Room(id));map.scenario=UnityEngine.Object.Instantiate(map.scenario);map.scenario.costLimit=0;map.InvalidateIndex();temporary.Add(map);temporary.Add(map.scenario);return map;
        }
        [TearDown] public void Cleanup(){foreach(var o in temporary)UnityEngine.Object.DestroyImmediate(o);temporary.Clear();}
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)]
        [TestCase(9)][TestCase(10)][TestCase(11)][TestCase(12)][TestCase(13)][TestCase(14)][TestCase(15)][TestCase(16)]
        [TestCase(17)][TestCase(18)][TestCase(19)][TestCase(20)][TestCase(21)][TestCase(22)][TestCase(23)][TestCase(24)]
        public void DeviceRoomClearsThroughItsAuthoredRoute(int id) => Assert.That(PuzzlePrototypeBuilder.Verify(Room(id)),Does.Contain("clear"));
        [Test] public void FourCombinedStagesClear()
        {
            foreach(var name in new[]{"starway","memory_theater","letter_observatory","twilight_sea"})
                Assert.That(PuzzlePrototypeBuilder.Verify(AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzlePrototypeBuilder.Root+"/"+name+".asset")),Does.Contain("clear"));
        }
        [Test] public void EveryLogicalStateAndCommittedClipHasAnExistingArtBinding()
        {
            foreach(var map in AssetDatabase.FindAssets("t:PuzzleMap",new[]{PuzzlePrototypeBuilder.Root}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<PuzzleMap>))
            {
                var session=new PuzzleDeviceSession(map); AssertBindings(session);
                foreach(char c in map.scenario.verificationRoute)
                {
                    var action=session.Apply(PuzzlePrototypeBuilder.Direction(c));
                    foreach(var e in action.events.Where(e=>e.kind==PrototypeEventKind.Visual))
                    {
                        var definition=session.Definition(e.actor);var view=definition.visualPrefab.GetComponent<DeviceVisualView>();
                        if(!view)view=definition.visualPrefab.GetComponent<FerryVisualView>().clock;
                        Assert.That(view.states.Any(s=>s.name==e.state && s.eventName==e.eventName),Is.True,map.name+" "+e.actor+" "+e.state+" "+e.eventName);
                    }
                    AssertBindings(session);
                }
            }
        }
        static void AssertBindings(PuzzleDeviceSession session)
        {
            foreach(var d in session.Devices.Where(d=>!session.States[d.id].removed))
            {
                Assert.That(d.visualPrefab,Is.Not.Null,d.id);var view=d.visualPrefab.GetComponent<DeviceVisualView>();
                if(!view)view=d.visualPrefab.GetComponent<FerryVisualView>().clock;
                Assert.That(view.states.Any(s=>s.name==PuzzleDeviceView.Idle(d,session.States[d.id])),Is.True,d.id+" "+PuzzleDeviceView.Idle(d,session.States[d.id]));
                Assert.That(d.visualPrefab.GetComponentsInChildren<Collider2D>(true),Is.Empty);
            }
        }
        [Test] public void InvalidInputDoesNotCommitPushCostBombCountdownOrShutterPhase()
        {
            var map=Clone(15);map.SetTerrain(new Vector2Int(3,2),TerrainMaterial.Grass,true);
            var session=new PuzzleDeviceSession(map);Assert.That(session.Apply(Vector2Int.right).accepted,Is.False);
            Assert.That(session.Cell,Is.EqualTo(map.start));Assert.That(session.Cost,Is.Zero);Assert.That(session.States["bomb"].cell,Is.EqualTo(new Vector2Int(2,2)));Assert.That(session.States["bomb"].timer,Is.Zero);
            var shutter=new PuzzleDeviceSession(Room(4));shutter.Apply(Vector2Int.left);Assert.That(shutter.States["shutter"].phase,Is.EqualTo(1));
        }
        [Test] public void BombArmsAtThreeAndExplodesOnlyAfterThreeSubsequentValidActions()
        {
            var session=new PuzzleDeviceSession(Room(15));session.Apply(Vector2Int.right);Assert.That(session.States["bomb"].timer,Is.EqualTo(3));
            session.Apply(Vector2Int.left);Assert.That(session.States["bomb"].timer,Is.EqualTo(2));
            Assert.That(session.Apply(Vector2Int.left).accepted,Is.False);Assert.That(session.States["bomb"].timer,Is.EqualTo(2));
            session.Apply(Vector2Int.down);Assert.That(session.States["bomb"].timer,Is.EqualTo(1));
            var action=session.Apply(Vector2Int.right);Assert.That(session.States["bomb"].removed,Is.True);Assert.That(session.States["cracked"].removed,Is.True);
            Assert.That(action.events.Single(e=>e.kind==PrototypeEventKind.Blast).cells,Is.EquivalentTo(new[]{new Vector2Int(3,2),new Vector2Int(4,2)}));
        }
        [Test] public void PortalRejectsBlockedExitBeforeEnteringAndPreservesSlidingDirection()
        {
            var map=Clone(16);map.exit=new Vector2Int(7,3);map.bubbles.Clear();map.targetBubbles=0;map.SetTerrain(new Vector2Int(7,2),TerrainMaterial.Grass,true);
            var session=new PuzzleDeviceSession(map);session.Apply(Vector2Int.right);
            Assert.That(session.Cell,Is.EqualTo(new Vector2Int(2,2)));Assert.That(session.PortalCuts,Is.Zero);Assert.That(session.Cost,Is.EqualTo(1));
            map=Clone(16);map.scenario.devices.First(d=>d.id=="belt").role=PrototypeRole.Ice;map.exit=new Vector2Int(7,3);
            session=new PuzzleDeviceSession(map);var action=session.Apply(Vector2Int.right);Assert.That(session.Cell,Is.EqualTo(new Vector2Int(7,2)));Assert.That(session.Direction,Is.EqualTo(Vector2Int.right));
            Assert.That(action.events.Count(e=>e.teleport),Is.EqualTo(1));Assert.That(session.Cost,Is.EqualTo(1));
        }
        [Test] public void OccupiedShutterStaysOpenAndInvalidAttemptDoesNotAdvanceIt()
        {
            var session=new PuzzleDeviceSession(Room(4));session.Apply(Vector2Int.right);session.Apply(Vector2Int.right);
            Assert.That(session.Cell,Is.EqualTo(new Vector2Int(3,2)));Assert.That(session.States["shutter"].active,Is.True);
        }
        [Test] public void DepartureSpikesRiseOnLeaveAndRejectReentry()
        {
            var session=new PuzzleDeviceSession(Room(5));session.Apply(Vector2Int.right);Assert.That(session.States["spikes"].phase,Is.EqualTo(1));
            session.Apply(Vector2Int.right);Assert.That(session.States["spikes"].phase,Is.EqualTo(2));Assert.That(session.Apply(Vector2Int.left).accepted,Is.False);Assert.That(session.Cost,Is.EqualTo(2));
        }
        [Test] public void BoatMovesOneWaterCellDisembarksAndRemainsOnWater()
        {
            var session=new PuzzleDeviceSession(Room(11));session.Apply(Vector2Int.right);Assert.That(session.BoatId,Is.EqualTo("boat"));
            session.Apply(Vector2Int.right);Assert.That(session.States["boat"].cell,Is.EqualTo(new Vector2Int(3,2)));
            session.Apply(Vector2Int.right);session.Apply(Vector2Int.right);session.Apply(Vector2Int.right);
            Assert.That(session.Cell,Is.EqualTo(new Vector2Int(6,2)));Assert.That(session.BoatId,Is.Null);Assert.That(session.States["boat"].cell,Is.EqualTo(new Vector2Int(5,2)));
        }
        [Test] public void BarrelRollAndDominoChainEachConsumeOneAction()
        {
            var session=new PuzzleDeviceSession(Room(12));session.Apply(Vector2Int.right);Assert.That(session.States["barrel"].cell,Is.EqualTo(new Vector2Int(5,2)));Assert.That(session.Cost,Is.EqualTo(1));
            session=new PuzzleDeviceSession(Room(13));session.Apply(Vector2Int.right);Assert.That(session.States.Values.Count(s=>s.active),Is.EqualTo(3));Assert.That(session.Cost,Is.EqualTo(1));
        }
        [Test] public void AllNinetySixDiceRollsMatchProvidedOrientationTable()
        {
            var table=AssetDatabase.LoadAssetAtPath<DiceOrientationTable>("Assets/ANIMOL/Data/Themes/memory_theater_dice.asset");int count=0;
            foreach(var o in table.orientations)foreach(var t in o.transitions)
            {
                var f=o.faces;var a=new[]{f.top,f.bottom,f.north,f.south,f.east,f.west};PuzzleDeviceSession.Roll(a,DeviceVisualView.Direction(t.direction));
                Assert.That(table.FindFaces(a[0],a[1],a[2],a[3],a[4],a[5]).id,Is.EqualTo(t.toOrientation));count++;
            }
            Assert.That(count,Is.EqualTo(96));
        }
        [Test] public void GlassBlocksBodyAndWindButPassesLight()
        {
            var session=new PuzzleDeviceSession(Room(10));Assert.That(session.Beams.Any(b=>b.cell==new Vector2Int(2,4)),Is.True);
            session=new PuzzleDeviceSession(Room(21));session.Apply(Vector2Int.right);Assert.That(session.Apply(Vector2Int.right).accepted,Is.False);
            var map=Clone(20);map.scenario.devices.First(d=>d.id=="metal").role=PrototypeRole.Glass;
            session=new PuzzleDeviceSession(map);Assert.That(session.WindCells.ContainsKey(new Vector2Int(5,4)),Is.False);
        }
        [Test] public void RejectedInputShowsExistingDeviceCueWithoutAdvancingPuzzle()
        {
            var session=new PuzzleDeviceSession(Room(21));
            Assert.That(session.Apply(Vector2Int.right).accepted,Is.True);
            var rejected=session.Apply(Vector2Int.right);
            Assert.That(rejected.accepted,Is.False);
            Assert.That(session.Cost,Is.EqualTo(1));
            Assert.That(session.Cell,Is.EqualTo(new Vector2Int(2,2)));
            var cue=rejected.events.Single(e=>e.kind==PrototypeEventKind.Visual);
            Assert.That(cue.actor,Is.EqualTo("glass"));
            Assert.That(cue.state,Is.EqualTo("blocked_square"));
            Assert.That(cue.eventName,Is.EqualTo("existing_body_glass_contact_rejected"));
            var art=session.Definition(cue.actor).visualPrefab.GetComponent<DeviceVisualView>();
            Assert.That(art.states.Any(s=>s.name==cue.state&&s.eventName==cue.eventName),Is.True);
        }
        [Test] public void DieRollsForEveryPhysicalCellButKeepsFacesDuringPortalCut()
        {
            var map=Clone(22);map.scenario.goals.Clear();
            map.scenario.devices.Add(new PrototypeDevice{id="p1",role=PrototypeRole.Portal,position=new Vector2Int(3,2),target=new Vector2Int(5,2)});
            map.scenario.devices.Add(new PrototypeDevice{id="p2",role=PrototypeRole.Portal,position=new Vector2Int(5,2),target=new Vector2Int(3,2)});
            var session=new PuzzleDeviceSession(map);var action=session.Apply(Vector2Int.right);
            Assert.That(action.accepted,Is.True);Assert.That(session.States["die"].cell,Is.EqualTo(new Vector2Int(6,2)));
            Assert.That(session.States["die"].dice[0],Is.EqualTo(6));
            Assert.That(action.events.Count(e=>e.actor=="die"&&e.teleport),Is.EqualTo(1));
            Assert.That(action.events.Count(e=>e.actor=="die"&&e.eventName=="actual_one_cell_die_roll"),Is.EqualTo(2));
            map.SetTerrain(new Vector2Int(6,2),TerrainMaterial.Grass,true);
            session=new PuzzleDeviceSession(map);Assert.That(session.Apply(Vector2Int.right).accepted,Is.False);
            Assert.That(session.States["die"].cell,Is.EqualTo(new Vector2Int(2,2)));
            Assert.That(session.States["die"].dice[0],Is.EqualTo(1));Assert.That(session.Cost,Is.Zero);
        }
        [Test] public void WindMovesAStationaryWoodCrateOnceButMetalResistsAndBlocks()
        {
            var map=Clone(20);
            map.scenario.devices.Add(new PrototypeDevice{id="wood",role=PrototypeRole.WoodCrate,position=new Vector2Int(2,4)});
            var session=new PuzzleDeviceSession(map);
            Assert.That(session.WindCells.ContainsKey(new Vector2Int(2,4)),Is.True);
            session.Apply(Vector2Int.right);
            Assert.That(session.States["wood"].cell,Is.EqualTo(new Vector2Int(3,4)));
            Assert.That(session.States["metal"].cell,Is.EqualTo(new Vector2Int(4,4)));
            session.Apply(Vector2Int.right);
            Assert.That(session.States["wood"].cell,Is.EqualTo(new Vector2Int(3,4)));
        }
        [Test] public void MagnetCannotPullMetalThroughWoodAndDoesNotMoveWood()
        {
            var map=Clone(14);map.scenario.devices.First(d=>d.id=="wood").position=new Vector2Int(4,4);
            var session=new PuzzleDeviceSession(map);session.Apply(Vector2Int.right);Assert.That(session.States["metal"].cell,Is.EqualTo(new Vector2Int(2,4)));Assert.That(session.States["wood"].cell,Is.EqualTo(new Vector2Int(4,4)));
        }
        [Test] public void StatueChargesLockedLineAfterPlayerEvadesAndShattersGlass()
        {
            var session=new PuzzleDeviceSession(Room(17));foreach(char c in "EEEEN")session.Apply(PuzzlePrototypeBuilder.Direction(c));
            Assert.That(session.States["statue"].locked,Is.True);session.Apply(Vector2Int.down);Assert.That(session.States["glass"].removed,Is.True);Assert.That(session.Failed,Is.False);Assert.That(session.States["statue"].cell,Is.EqualTo(new Vector2Int(4,3)));
        }
        [Test] public void BridgeHoldsOccupiedSpanAndRotationRejectsOccupiedFootprint()
        {
            var session=new PuzzleDeviceSession(Room(2));session.Apply(Vector2Int.right);session.Apply(Vector2Int.right);Assert.That(session.States["bridge"].active,Is.True);
            session.Apply(Vector2Int.right);Assert.That(session.States["bridge"].active,Is.False);
            var map=Clone(8);map.scenario.devices.Add(new PrototypeDevice{id="wall",role=PrototypeRole.Wall,position=new Vector2Int(4,2)});
            session=new PuzzleDeviceSession(map);session.Apply(Vector2Int.right);Assert.That(session.States["shelf"].phase,Is.Zero);
        }
        [Test] public void ResizeRetainsScenarioDevicesAndExcludedRecordsCannotTick()
        {
            var map=Clone(15);map.exit=new Vector2Int(1,1);map.bubbles.Clear();map.targetBubbles=0;map.scenario.goals.Clear();map.Resize(2,3);
            Assert.That(map.scenario.devices.Count,Is.EqualTo(2));var session=new PuzzleDeviceSession(map);Assert.That(session.Devices,Is.Empty);
        }
    }
}
