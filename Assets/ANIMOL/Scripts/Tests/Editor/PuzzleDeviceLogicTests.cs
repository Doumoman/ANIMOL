using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ANIMOL.Puzzle;
using ANIMOL.Presentation;
using ANIMOL.Presentation.Editor;
using ANIMOL.Art.SunsetSanctumV02;
using ANIMOL.Art.StarSwellCoastV01;
using ANIMOL.Art.RanchDevicesV13;
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
        [Test] public void TheaterWinRequiresThreeBubblesAndExitOnly()
        {
            var map=AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzlePrototypeBuilder.Root+"/memory_theater.asset");
            Assert.That(map.targetBubbles,Is.EqualTo(3));
            Assert.That(map.bubbles.Distinct().Count(),Is.EqualTo(3));
            Assert.That(map.scenario.goals,Is.Empty);
            var session=new PuzzleDeviceSession(map);
            Assert.That(session.ExitReady,Is.False);
            foreach(char c in map.scenario.verificationRoute)
            {
                Assert.That(session.Apply(PuzzlePrototypeBuilder.Direction(c)).accepted,Is.True);
                if(session.Collected<3)Assert.That(session.Won,Is.False);
            }
            Assert.That(session.Collected,Is.EqualTo(3));
            Assert.That(session.Cell,Is.EqualTo(map.exit));
            Assert.That(session.Won,Is.True);
        }
        [TestCase("memory_theater")][TestCase("letter_observatory")][TestCase("twilight_sea")]
        public void UpdatedGateRoutesClearInOtherCombinedStages(string name)
        {
            Assert.That(PuzzlePrototypeBuilder.Verify(AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                PuzzlePrototypeBuilder.Root+"/"+name+".asset")),Does.Contain("clear"));
        }
        [Test] public void StarSocketRevealsConfiguredBridgeOnlyAfterMatchingStoneIsSeated()
        {
            var map = Clone(1);
            var socket = map.scenario.devices.Single(d => d.variant == "socket");
            socket.socketReward = StarSocketReward.RevealBridge;
            socket.socketBridgeId = "reward_bridge";
            var bridge = new PrototypeDevice { id = "reward_bridge", role = PrototypeRole.VineBridge,
                position = new Vector2Int(5, 3), direction = Vector2Int.right };
            map.scenario.devices.Add(bridge);
            map.RemoveTerrain(bridge.position);
            Assert.That(map.ValidateMap(), Is.Empty);
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.Supported(bridge.position), Is.False);
            Assert.That(session.Apply(Vector2Int.left).accepted, Is.False);
            Assert.That(session.States[bridge.id].active, Is.False);
            session.Apply(Vector2Int.right);
            Assert.That(session.States[bridge.id].active, Is.False);
            session.Apply(Vector2Int.right);
            Assert.That(session.States[bridge.id].active, Is.False);
            session.Apply(Vector2Int.right);
            Assert.That(session.States[socket.id].active, Is.True);
            Assert.That(session.States[bridge.id].active, Is.True);
            Assert.That(session.Supported(bridge.position), Is.True);
            session.Apply(Vector2Int.right);
            Assert.That(session.States[bridge.id].active, Is.True);
        }
        [Test] public void StarSocketBubbleIsHiddenUntilSeatedAndCollectsOnArrival()
        {
            var map = Clone(1);
            var socket = map.scenario.devices.Single(d => d.variant == "socket");
            var reward = new Vector2Int(6, 3);
            socket.socketReward = StarSocketReward.RevealBubble;
            socket.socketBubbleCell = reward;
            map.targetBubbles = 2;
            Assert.That(map.ValidateMap(), Is.Empty);
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.IsBubbleVisible(reward), Is.False);
            Assert.That(session.Collected, Is.Zero);
            session.Apply(Vector2Int.right);
            Assert.That(session.IsBubbleVisible(reward), Is.False);
            session.Apply(Vector2Int.right);
            Assert.That(session.IsBubbleVisible(reward), Is.False);
            session.Apply(Vector2Int.right);
            Assert.That(session.IsBubbleVisible(reward), Is.True);
            Assert.That(session.Collected, Is.Zero);
            session.Apply(Vector2Int.right);
            session.Apply(Vector2Int.right);
            session.Apply(Vector2Int.right);
            session.Apply(Vector2Int.right);
            session.Apply(Vector2Int.up);
            Assert.That(session.Cell, Is.EqualTo(reward));
            Assert.That(session.Collected, Is.EqualTo(1));
            Assert.That(session.IsCollected(reward), Is.True);
            Assert.That(map.bubbles.Contains(reward), Is.False);
        }
        [Test] public void MismatchedStarSocketRejectsPushWithoutCostOrTurn()
        {
            var map = Clone(1);
            var socket = map.scenario.devices.Single(d => d.variant == "socket");
            socket.number = 2;
            map.scenario.devices.Add(new PrototypeDevice { id = "turn_probe", role = PrototypeRole.Shutter,
                position = new Vector2Int(6, 3), direction = Vector2Int.right });
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            var phase = session.States["turn_probe"].phase;
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            phase = session.States["turn_probe"].phase;
            var rejected = session.Apply(Vector2Int.right);
            Assert.That(rejected.accepted, Is.False);
            Assert.That(rejected.events.Any(e => e.actor == socket.id && e.eventName == "pattern_mismatch_rejected"), Is.True);
            Assert.That(rejected.events.Any(e => e.kind == PrototypeEventKind.Move), Is.False);
            Assert.That(session.States["stone"].cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(session.States[socket.id].active, Is.False);
            Assert.That(session.States["turn_probe"].phase, Is.EqualTo(phase));
            Assert.That(session.Cost, Is.EqualTo(2));
        }
        [Test] public void EmptyStarSocketBlocksPlayerUntilMatchingStoneSeats()
        {
            var map = Clone(1);
            var socket = map.scenario.devices.Single(d => d.variant == "socket");
            map.start = socket.position + Vector2Int.down;
            var session = new PuzzleDeviceSession(map);
            var blocked = session.Apply(Vector2Int.up);
            Assert.That(blocked.accepted, Is.False);
            Assert.That(session.Cost, Is.Zero);
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(session.States[socket.id].active, Is.False);
        }
        [Test] public void SunsetSanctumV02KeepsOneCellFootprintsAndOneMotifForItsActualPortalPair()
        {
            foreach(var map in AssetDatabase.FindAssets("t:PuzzleMap",new[]{PuzzlePrototypeBuilder.Root})
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<PuzzleMap>)
                .Where(m=>m.scenario && m.scenario.themeId=="letter_observatory"))
            {
                var session=new PuzzleDeviceSession(map);
                Assert.That(map.scenario.beamPrefab,Is.Null,map.name);
                foreach(var device in session.Devices.Where(SunsetSanctumV02PuzzleBinding.Covers))
                    Assert.That(session.Footprint(device).Count(),Is.EqualTo(1),map.name+" "+device.id);
                var portals=session.Devices.Where(d=>d.role==PrototypeRole.Portal).ToArray();
                foreach(var portal in portals)
                {
                    var partner=portals.Single(d=>d.position==portal.target);
                    Assert.That(partner.target,Is.EqualTo(portal.position));
                    Assert.That(SunsetSanctumV02PuzzleBinding.IdleClip(portal,session.States[portal.id]),
                        Is.EqualTo(SunsetSanctumV02PuzzleBinding.IdleClip(partner,session.States[partner.id])));
                }
            }
        }
        [Test] public void ThemedStagesExposeEveryDeviceFamilyWithBoundArt()
        {
            var expected = new Dictionary<string, int[]> {
                { "starway", new[] { 1, 2, 7, 9, 18, 19, 20, 24 } },
                { "memory_theater", new[] { 4, 6, 7, 8, 9, 13, 17, 18, 19, 22, 23 } },
                { "letter_observatory", new[] { 3, 7, 9, 10, 14, 16, 18, 19, 21 } },
                { "twilight_sea", new[] { 5, 7, 9, 11, 12, 15, 18, 19 } }
            };
            foreach(var pair in expected)
            {
                var map=AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzlePrototypeBuilder.Root+"/"+pair.Key+".asset");
                Assert.That(map.scenario.themeId,Is.EqualTo(pair.Key));
                Assert.That(map.scenario.costLimit,Is.Zero,pair.Key);
                Assert.That(pair.Value,Is.SubsetOf(map.scenario.devices.Select(d=>(int)d.role).Distinct()),pair.Key);
                Assert.That(map.Height,Is.EqualTo(pair.Value.Length*8-1),pair.Key);
                Assert.That(map.scenario.devices.All(d=>d.visualPrefab && map.Contains(d.position)),Is.True,pair.Key);
                Assert.That(map.ValidateMap(),Is.Empty,pair.Key);
            }
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
                        var definition=session.Definition(e.actor);
                        if (e.eventName == "ChickReturned")
                        {
                            Assert.That(definition.role, Is.EqualTo(PrototypeRole.Chick));
                            Assert.That(map.theme.starwayRanch, Is.Not.Null);
                            var catalog = map.theme.starwayRanch.v13Devices;
                            Assert.That(catalog, Is.Not.Null);
                            Assert.That(catalog.TryClip(e.state, out RanchV13Clip clip), Is.True,
                                map.name + " " + e.actor + " " + e.state);
                            Assert.That(clip.frames, Is.Not.Empty);
                            continue;
                        }
                        var view=definition.visualPrefab.GetComponent<DeviceVisualView>();
                        if (session.Rules.themeId == "twilight_sea" && StarSwellCoastV01PuzzleBinding.Covers(definition))
                        {
                            var key = StarSwellCoastV01PuzzleBinding.EventClip(definition, e, session.States[e.actor]);
                            if (key != null)
                            {
                                StarSwellCoastV01VisualCatalog.Clip clip; string error;
                                Assert.That(map.theme.starSwellCoastV01Catalog.TryGetClip(key,
                                    StarSwellCoastV01PuzzleBinding.ManifestSha256, out clip, out error), Is.True,
                                    map.name + " " + e.actor + " " + key + " " + error);
                            }
                            continue;
                        }
                        if (session.Rules.themeId == "letter_observatory" && SunsetSanctumV02PuzzleBinding.Covers(definition))
                        {
                            var key = SunsetSanctumV02PuzzleBinding.TransitionClip(definition, e, session.States[e.actor]);
                            if (key != null)
                            {
                                SunsetSanctumV02VisualCatalog.Clip clip; string error;
                                Assert.That(map.theme.sunsetSanctumV02Catalog.TryGetClip(key,
                                    SunsetSanctumV02PuzzleBinding.ManifestSha256, out clip, out error), Is.True,
                                    map.name + " " + e.actor + " " + key + " " + error);
                            }
                            continue;
                        }
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
                if (session.Rules.themeId == "twilight_sea" && StarSwellCoastV01PuzzleBinding.Covers(d))
                {
                    var catalog = session.Map.theme.starSwellCoastV01Catalog;
                    Assert.That(catalog, Is.Not.Null, d.id);
                    Assert.That(d.visualPrefab.GetComponent<StarSwellCoastV01AnimatedVisual>(), Is.Not.Null, d.id);
                    var key = StarSwellCoastV01PuzzleBinding.IdleClip(d, session.States[d.id]);
                    StarSwellCoastV01VisualCatalog.Clip clip; string error;
                    Assert.That(catalog.TryGetClip(key, StarSwellCoastV01PuzzleBinding.ManifestSha256,
                        out clip, out error), Is.True, d.id + " " + key + " " + error);
                    Assert.That(d.visualPrefab.GetComponentsInChildren<Collider2D>(true), Is.Empty);
                    continue;
                }
                if (session.Rules.themeId == "letter_observatory" && SunsetSanctumV02PuzzleBinding.Covers(d))
                {
                    var catalog = session.Map.theme.sunsetSanctumV02Catalog;
                    Assert.That(catalog, Is.Not.Null, d.id);
                    if (d.role == PrototypeRole.MetalCrate)
                    {
                        Assert.That(d.visualPrefab.GetComponent<SunsetSanctumV02StaticVisual>(), Is.Not.Null, d.id);
                        SunsetSanctumV02VisualCatalog.Part part; string error;
                        Assert.That(catalog.TryGetPart("devices/metal_box/idle.png",
                            SunsetSanctumV02PuzzleBinding.ManifestSha256, out part, out error), Is.True, error);
                    }
                    else
                    {
                        Assert.That(d.visualPrefab.GetComponent<SunsetSanctumV02AnimatedVisual>(), Is.Not.Null, d.id);
                        var key = SunsetSanctumV02PuzzleBinding.IdleClip(d, session.States[d.id]);
                        SunsetSanctumV02VisualCatalog.Clip clip; string error;
                        Assert.That(catalog.TryGetClip(key, SunsetSanctumV02PuzzleBinding.ManifestSha256,
                            out clip, out error), Is.True, d.id + " " + key + " " + error);
                    }
                    Assert.That(d.visualPrefab.GetComponentsInChildren<Collider2D>(true), Is.Empty);
                    continue;
                }
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
            session=new PuzzleDeviceSession(map);session.Apply(Vector2Int.up);session.Apply(Vector2Int.up);
            Assert.That(session.WindCells.ContainsKey(new Vector2Int(5,4)),Is.False);
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
            if (art) Assert.That(art.states.Any(s=>s.name==cue.state&&s.eventName==cue.eventName),Is.True);
            else
            {
                Assert.That(session.Definition(cue.actor).visualPrefab.GetComponent<SunsetSanctumV02AnimatedVisual>(), Is.Not.Null);
                // V02 has no glass contact clip; the rejected host cue leaves intact glass displayed.
                Assert.That(SunsetSanctumV02PuzzleBinding.TransitionClip(session.Definition(cue.actor), cue,
                    session.States[cue.actor]), Is.Null);
            }
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
            Assert.That(session.WindRouteFor("blower"),Is.Null);
            Assert.That(session.Apply(Vector2Int.up).accepted,Is.True,"approach blower");
            Assert.That(session.Cell,Is.EqualTo(new Vector2Int(1,3)));
            Assert.That(session.Apply(Vector2Int.up).accepted,Is.True,"push blower");
            Assert.That(session.States["blower"].phase,Is.EqualTo(1));
            Assert.That(session.WindCells.ContainsKey(new Vector2Int(2,4)),Is.True,"wind route at 2,4");
            Assert.That(session.States["wood"].cell,Is.EqualTo(new Vector2Int(3,4)));
            Assert.That(session.States["metal"].cell,Is.EqualTo(new Vector2Int(4,4)));
            session.Apply(Vector2Int.up);
            Assert.That(session.States["wood"].cell,Is.EqualTo(new Vector2Int(3,4)));
        }
        [Test] public void StarwayWindCarriesWoodToLastReachableCellAndMetalStopsIt()
        {
            var map = Clone(20);
            map.scenario.devices.First(d => d.id == "metal").position = new Vector2Int(7, 4);
            map.scenario.devices.Add(new PrototypeDevice { id = "wood", role = PrototypeRole.WoodCrate,
                position = new Vector2Int(2, 4) });
            var session = new PuzzleDeviceSession(map);
            session.Apply(Vector2Int.up);
            var first = session.Apply(Vector2Int.up);
            Assert.That(first.accepted, Is.True);
            Assert.That(session.States["wood"].cell, Is.EqualTo(new Vector2Int(4, 4)));
            Assert.That(first.events.Count(e => e.actor == "wood" && e.kind == PrototypeEventKind.Move), Is.EqualTo(2));
            var second = session.Apply(Vector2Int.up);
            Assert.That(second.accepted, Is.True);
            Assert.That(session.States["wood"].cell, Is.EqualTo(new Vector2Int(6, 4)));
            Assert.That(session.States["metal"].cell, Is.EqualTo(new Vector2Int(7, 4)));
            Assert.That(session.Cost, Is.EqualTo(3));
        }
        [Test] public void MatchingStarStoneMovedByWindSeatsAndRevealsFloor()
        {
            var map = Clone(20);
            map.scenario.devices.First(d => d.id == "blower").orientation = 1;
            map.scenario.devices.Add(new PrototypeDevice { id = "wind_stone", role = PrototypeRole.StarSocket,
                position = new Vector2Int(2, 4), number = 3 });
            map.scenario.devices.Add(new PrototypeDevice { id = "wind_socket", role = PrototypeRole.StarSocket,
                variant = "socket", position = new Vector2Int(3, 4), number = 3 });
            var session = new PuzzleDeviceSession(map);
            var action = session.Apply(Vector2Int.right);
            Assert.That(action.accepted, Is.True);
            Assert.That(session.States["wind_stone"].removed, Is.True);
            Assert.That(session.States["wind_socket"].active, Is.True);
            Assert.That(action.events.Any(e => e.actor == "wind_socket" && e.eventName == "matching_star_socket_committed"), Is.True);
            Assert.That(session.Cost, Is.EqualTo(1));
        }
        [Test] public void StarwayWindStopsAtV12BlockingChick()
        {
            var map = Clone(20);
            map.scenario.devices.First(d => d.id == "metal").position = new Vector2Int(7, 4);
            map.scenario.devices.Add(new PrototypeDevice { id = "chick", role = PrototypeRole.Chick,
                position = new Vector2Int(2, 4), target = new Vector2Int(6, 4), number = 1 });
            var session = new PuzzleDeviceSession(map);
            var first = session.Apply(Vector2Int.up);
            Assert.That(first.accepted, Is.True);
            Assert.That(session.WindCells.ContainsKey(new Vector2Int(3, 4)), Is.False);
            Assert.That(session.States["chick"].cell, Is.EqualTo(new Vector2Int(2, 4)));
            Assert.That(first.events.Any(e => e.actor == "chick" && e.kind == PrototypeEventKind.Move), Is.False);
            Assert.That(session.Cost, Is.EqualTo(1));
        }
        [Test] public void BlowerPushCyclesThreeSixNineCellsThenOffWithoutMovingPlayer()
        {
            var map=Clone(20);map.Resize(14,7);
            for(var x=8;x<=12;x++)map.SetTerrain(new Vector2Int(x,4),TerrainMaterial.Grass);
            map.scenario.devices.First(d=>d.id=="metal").position=new Vector2Int(12,4);
            var session=new PuzzleDeviceSession(map);
            session.Apply(Vector2Int.up);
            for(var phase=1;phase<=4;phase++)
            {
                var action=session.Apply(Vector2Int.up);
                Assert.That(action.accepted,Is.True);
                Assert.That(session.Cell,Is.EqualTo(new Vector2Int(1,3)));
                Assert.That(session.States["blower"].cell,Is.EqualTo(new Vector2Int(1,4)));
                Assert.That(session.States["blower"].phase,Is.EqualTo(phase%4));
                Assert.That(session.WindRouteFor("blower")?.cells.Count ?? 0,Is.EqualTo(phase==4?0:phase*3));
                Assert.That(action.events.Any(e=>e.actor=="blower"&&e.eventName=="valid_action_wind_resolved"),Is.True);
            }
            Assert.That(session.WindRouteFor("blower"),Is.Null);
            Assert.That(session.WindCells.Keys.Any(p=>p.y==4),Is.False);
        }
        [TestCase(1, 4)][TestCase(2, 7)][TestCase(3, 10)]
        public void ActiveBlowerCarriesPlayerToLastWindCellInOneAction(int phase, int destinationX)
        {
            var map=Clone(20);map.Resize(14,7);
            for(var x=8;x<=12;x++)map.SetTerrain(new Vector2Int(x,4),TerrainMaterial.Grass);
            map.scenario.devices.First(d=>d.id=="metal").position=new Vector2Int(12,4);
            map.scenario.devices.First(d=>d.id=="blower").orientation=phase;
            map.start=new Vector2Int(2,3);
            var session=new PuzzleDeviceSession(map);
            var action=session.Apply(Vector2Int.up);
            Assert.That(action.accepted,Is.True);
            Assert.That(session.Cell,Is.EqualTo(new Vector2Int(destinationX,4)));
            Assert.That(session.Cost,Is.EqualTo(1));
            Assert.That(action.events.Count(e=>e.actor=="player"&&e.kind==PrototypeEventKind.Move),
                Is.EqualTo(destinationX-1));
        }
        [Test] public void BlowerCarriesPlayerBehindSheepThatWindMovesInSameAction()
        {
            var map = Clone(20); map.Resize(14, 7);
            for (var x = 8; x <= 12; x++) map.SetTerrain(new Vector2Int(x, 4), TerrainMaterial.Grass);
            map.scenario.devices.First(d => d.id == "metal").position = new Vector2Int(12, 4);
            map.scenario.devices.First(d => d.id == "blower").orientation = 3;
            map.scenario.ranchV13Art.Add(new RanchV13ArtPlacement
                { family = "sheep", instanceId = "wind_sheep", position = new Vector2Int(3, 4) });
            map.start = new Vector2Int(2, 3);
            var session = new PuzzleDeviceSession(map);

            var action = session.Apply(Vector2Int.up);

            Assert.That(action.accepted, Is.True);
            Assert.That(session.RanchSheep["wind_sheep"], Is.EqualTo(new Vector2Int(10, 4)));
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(9, 4)));
            Assert.That(session.Cost, Is.EqualTo(1));
        }
        [TestCase(2)][TestCase(3)]
        public void BrakeStopsPlayerOnItsWindCell(int brakeX)
        {
            var map=Clone(20);
            map.scenario.devices.First(d=>d.id=="metal").position=new Vector2Int(7,4);
            map.scenario.devices.First(d=>d.id=="blower").orientation=1;
            map.scenario.devices.Add(new PrototypeDevice{id="wind_brake",role=PrototypeRole.Brake,
                position=new Vector2Int(brakeX,4),direction=Vector2Int.right});
            map.start=new Vector2Int(2,3);
            var session=new PuzzleDeviceSession(map);
            var action=session.Apply(Vector2Int.up);
            Assert.That(action.accepted,Is.True);
            Assert.That(session.Cell,Is.EqualTo(new Vector2Int(brakeX,4)));
            Assert.That(session.Cost,Is.EqualTo(1));
            Assert.That(action.events.Any(e=>e.actor=="wind_brake"&&e.eventName=="forced_motion_stopped"),Is.True);
        }
        [Test] public void OneWayGateCrossesInOneActionAndRejectsABlockedOutlet()
        {
            var map=Clone(9);var session=new PuzzleDeviceSession(map);
            session.Apply(Vector2Int.right);
            var action=session.Apply(Vector2Int.right);
            Assert.That(action.accepted,Is.True);
            Assert.That(session.Cell,Is.EqualTo(new Vector2Int(4,2)));
            Assert.That(session.Cost,Is.EqualTo(2));
            Assert.That(action.events.Count(e=>e.actor=="player"&&e.kind==PrototypeEventKind.Move),Is.EqualTo(2));
            Assert.That(session.Apply(Vector2Int.left).accepted,Is.False);
            map.scenario.devices.Add(new PrototypeDevice{id="outlet_wall",role=PrototypeRole.Wall,position=new Vector2Int(4,2)});
            session=new PuzzleDeviceSession(map);session.Apply(Vector2Int.right);
            Assert.That(session.Apply(Vector2Int.right).accepted,Is.False);
            Assert.That(session.Cell,Is.EqualTo(new Vector2Int(2,2)));
            Assert.That(session.Cost,Is.EqualTo(1));
        }
        [Test] public void ChickEntersNearbyNestAndStopsFollowing()
        {
            var map=Clone(24);var chick=map.scenario.devices.Single(d=>d.id=="chick");
            chick.target=new Vector2Int(2,3);
            var session=new PuzzleDeviceSession(map);
            var action=session.Apply(Vector2Int.right);
            Assert.That(action.accepted,Is.True);
            Assert.That(session.States["chick"].cell,Is.EqualTo(chick.target));
            Assert.That(session.States["chick"].active,Is.True);
            Assert.That(action.events.Any(e=>e.actor=="chick"&&e.kind==PrototypeEventKind.Move&&e.to==chick.target),Is.True);
            var returned = action.events.Single(e => e.eventName == "ChickReturned");
            Assert.That(returned.nestId, Is.EqualTo("nest:2:3"));
            Assert.That(returned.arrivalDirection, Is.EqualTo(Vector2Int.right));
            Assert.That(returned.cells, Is.EqualTo(new[] { new Vector2Int(1, 3), chick.target }));
            var later = session.Apply(Vector2Int.right);
            Assert.That(session.States["chick"].cell,Is.EqualTo(chick.target));
            Assert.That(session.States["chick"].active, Is.True);
            Assert.That(later.events.Any(e => e.eventName == "ChickReturned"), Is.False);
        }
        [Test] public void ChickReturnsToAnotherChicksNest()
        {
            var map = Clone(24);
            var chick = map.scenario.devices.Single(d => d.id == "chick");
            chick.target = new Vector2Int(5, 3);
            map.scenario.goals.Single(g => g.deviceId == "chick").cell = chick.target;
            map.targetBubbles = 0;
            map.scenario.devices.Add(new PrototypeDevice { id = "other_chick", role = PrototypeRole.Chick,
                position = new Vector2Int(5, 4), target = new Vector2Int(2, 3) });
            var session = new PuzzleDeviceSession(map);
            var action = session.Apply(Vector2Int.right);
            Assert.That(action.accepted, Is.True);
            Assert.That(session.States["chick"].cell, Is.EqualTo(new Vector2Int(2, 3)));
            Assert.That(session.States["chick"].active, Is.True);
            Assert.That(session.ExitReady, Is.True);
            Assert.That(action.events.Single(e => e.actor == "chick" && e.eventName == "ChickReturned").nestId,
                Is.EqualTo("nest:2:3"));
        }
        [Test] public void V12ChickFollowsBlocksPlayerAndPressesOrdinaryPlate()
        {
            var map = Clone(24);
            map.scenario.devices.Add(new PrototypeDevice { id = "chick_plate", role = PrototypeRole.VineBridge,
                variant = "plate", position = new Vector2Int(1, 3), group = "chick_plate" });
            var session = new PuzzleDeviceSession(map);
            Assert.That(session.States["chick_plate"].active, Is.True);
            Assert.That(session.Apply(Vector2Int.up).accepted, Is.False, "The initial chick cell blocks entry.");
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(session.Cost, Is.Zero);
            Assert.That(session.Apply(Vector2Int.right).accepted, Is.True);
            var follow = session.Apply(Vector2Int.up);
            Assert.That(follow.accepted, Is.True);
            Assert.That(follow.events.Any(e => e.actor == "chick" && e.kind == PrototypeEventKind.Move &&
                e.to == new Vector2Int(1, 2)), Is.True);
            Assert.That(session.States["chick"].cell, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(session.States["chick_plate"].active, Is.False, "The plate releases after the chick follows.");
            Assert.That(session.Cost, Is.EqualTo(2));
        }
        [Test] public void V12ChickBlocksCratePushWithoutCost()
        {
            var map = Clone(24);
            map.scenario.devices.Single(d => d.id == "chick").position = new Vector2Int(3, 2);
            map.scenario.devices.Add(new PrototypeDevice { id = "crate", role = PrototypeRole.WoodCrate,
                position = new Vector2Int(2, 2) });
            var session = new PuzzleDeviceSession(map);
            var blocked = session.Apply(Vector2Int.right);
            Assert.That(blocked.accepted, Is.False);
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(session.States["crate"].cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(session.States["chick"].cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(session.Cost, Is.Zero);
        }
        [TestCase(PrototypeRole.WoodCrate)]
        [TestCase(PrototypeRole.MetalCrate)]
        [TestCase(PrototypeRole.StarSocket)]
        public void StationaryPushCostsOneAndLeavesPlayerInPlace(PrototypeRole role)
        {
            var map = Clone(24);
            map.scenario.devices.RemoveAll(d => d.role == PrototypeRole.Chick);
            map.scenario.goals.Clear();
            map.scenario.devices.Add(new PrototypeDevice { id = "crate", role = role,
                position = new Vector2Int(2, 2) });
            var session = new PuzzleDeviceSession(map);
            var action = session.Apply(Vector2Int.right);
            Assert.That(action.accepted, Is.True);
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(session.States["crate"].cell, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(session.Cost, Is.EqualTo(1));
            Assert.That(action.events.Any(e => e.actor == "player" && e.kind == PrototypeEventKind.Move), Is.False);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Cell, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(session.States["crate"].cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(session.Cost, Is.Zero);
            session.Apply(Vector2Int.right);
            session.Reset();
            Assert.That(session.States["crate"].cell, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(session.Cost, Is.Zero);
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
