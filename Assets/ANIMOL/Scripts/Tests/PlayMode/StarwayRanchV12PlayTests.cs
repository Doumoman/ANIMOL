using System.Collections;
using System.Linq;
using ANIMOL.Puzzle;
using Animol.StarwayRanch.V12;
using ANIMOL.Art.V12;
using ANIMOL.Art.DevicePatch01;
using ANIMOL.Art.RanchDevicesV13;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class StarwayRanchV12PlayTests
    {
        [SetUp] public void ResetMotionBeforeTest()
        {
            Time.timeScale = 1;
            StarwayRanchMotionSettings.ReducedMotion = false;
        }
        [TearDown] public void ResetMotionAfterTest()
        {
            Time.timeScale = 1;
            StarwayRanchMotionSettings.ReducedMotion = false;
        }

        [Test] public void MissingLatestKeyShowsDiagnosticInsteadOfLegacySprite()
        {
            var art = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(
                "Assets/ANIMOL/Data/Art/StarwayRanch/StarwayRanchArt.asset");
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ANIMOL/Prefabs/Themes/starway/d07_conveyor.prefab");
            var go = Object.Instantiate(prefab);
            try
            {
                var view = go.GetComponent<DeviceVisualView>();
                Assert.That(view.ActivateV12(art.v12Animations, art.v12ManifestSha256), Is.True);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Missing latest clip key"));
                view.FailV12("deliberately_missing_key");
                var player = go.GetComponent<StarwayRanchV12AnimatedVisual>();
                Assert.That(player.LastDiagnostic, Does.Contain("Missing latest clip key"));
                Assert.That(view.visual.sprite.name, Is.EqualTo("V12_MISSING_LATEST_ART"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test] public void CommittedWindUsesSameRouteAndRetainsStaticRangeOnSettle()
        {
            var source = UnityEditor.AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device20_Wind.asset");
            var map = Object.Instantiate(source);
            map.scenario = Object.Instantiate(source.scenario);
            map.scenario.devices.Add(new PrototypeDevice { id = "qa_wood", role = PrototypeRole.WoodCrate,
                position = new Vector2Int(2, 4), direction = Vector2Int.right, artTheme = "starway" });
            var visualRoot = new GameObject("V12 wind test");
            var player = new GameObject("Test player").AddComponent<SpriteRenderer>();
            try
            {
                var session = new PuzzleDeviceSession(map);
                session.Apply(Vector2Int.up);
                var interaction = session.Apply(Vector2Int.up);
                Assert.That(interaction.accepted, Is.True);
                var action = session.Apply(Vector2Int.right);
                Assert.That(action.accepted, Is.True);
                Assert.That(session.Cost, Is.EqualTo(3));
                var cue = interaction.events.First(e => e.eventName == "valid_action_wind_resolved" && e.actor == "blower");
                Assert.That(cue.cells.Count, Is.GreaterThan(0));
                Assert.That(cue.windRoute, Is.Not.Null);
                var view = visualRoot.AddComponent<PuzzleDeviceView>();
                view.Bind(session, player);
                view.Notify(cue, "wind-authority-step");
                var ranges = view.GetComponentsInChildren<AnimolWindRouteRenderer>();
                Assert.That(ranges.Length, Is.EqualTo(1));
                Assert.That(ranges[0].HasVisibleRange, Is.True);
                Assert.That(ranges[0].IsActive, Is.True);
                foreach (var segment in cue.windRoute.cells)
                {
                    Assert.That(cue.windRoute.TryGetForce(segment.cell, out var heading), Is.True);
                    Assert.That(session.WindCells[new Vector2Int(segment.cell.x, segment.cell.y)],
                        Is.EqualTo(heading == WindHeading.North ? Vector2Int.up :
                            heading == WindHeading.East ? Vector2Int.right :
                            heading == WindHeading.South ? Vector2Int.down : Vector2Int.left));
                }
                view.Settle();
                Assert.That(ranges[0].HasVisibleRange, Is.True);
                Assert.That(ranges[0].IsActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(visualRoot);
                Object.DestroyImmediate(player.gameObject);
                Object.DestroyImmediate(map.scenario);
                Object.DestroyImmediate(map);
            }
        }

        [UnityTest] public IEnumerator DirectionalFramesPauseAndResumeWithoutMovingDeviceRoot()
        {
            var art = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayRanchArt>(
                "Assets/ANIMOL/Data/Art/StarwayRanch/StarwayRanchArt.asset");
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ANIMOL/Prefabs/Themes/starway/d07_conveyor.prefab");
            var go = Object.Instantiate(prefab);
            try
            {
                var view = go.GetComponent<DeviceVisualView>();
                Assert.That(view.ActivateV12(art.v12Animations, art.v12ManifestSha256), Is.True);
                var origin = go.transform.position;
                foreach (var direction in new[] { "e", "n", "w", "s" })
                {
                    Assert.That(view.Present("run_" + direction, true), Is.True);
                    var player = go.GetComponent<StarwayRanchV12AnimatedVisual>();
                    Assert.That(player.CurrentKey, Is.EqualTo("conveyor_" +
                        (direction == "e" ? "east" : direction == "n" ? "north" : direction == "w" ? "west" : "south") + "_transport"));
                    var first = view.visual.sprite;
                    yield return new WaitForSeconds(.12f);
                    Assert.That(view.visual.sprite, Is.Not.SameAs(first));
                    Assert.That(go.transform.position, Is.EqualTo(origin));
                }
                view.paused = true;
                yield return null;
                var frozen = view.visual.sprite;
                Time.timeScale = 0;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(view.visual.sprite, Is.SameAs(frozen));
                Time.timeScale = 1;
                view.paused = false;
                yield return new WaitForSeconds(.12f);
                Assert.That(view.visual.sprite, Is.Not.SameAs(frozen));
            }
            finally { Time.timeScale = 1; Object.Destroy(go); }
        }

        [UnityTest] public IEnumerator CampaignWindmillRotatesOnlyTheRotorAndStopsAtPause()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Runtime/ANIMOL_Puzzle.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null; yield return null;
            var motion = Object.FindFirstObjectByType<StarwayRanchV12RotorVisual>();
            Assert.That(motion, Is.Not.Null);
            var body = motion.transform.Find("Fixed body");
            var rotor = motion.transform.Find("Four blade rotor");
            Assert.That(body, Is.Not.Null);
            Assert.That(rotor, Is.Not.Null);
            var bodyRotation = body.localRotation;
            var rotorRotation = rotor.localRotation;
            yield return new WaitForSeconds(.15f);
            Assert.That(Quaternion.Angle(rotor.localRotation, rotorRotation), Is.GreaterThan(1f),
                $"scale={Time.timeScale}, delta={Time.deltaTime}, active={motion.isActiveAndEnabled}");
            Assert.That(body.localRotation, Is.EqualTo(bodyRotation));
            Time.timeScale = 0;
            try
            {
                rotorRotation = rotor.localRotation;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(Quaternion.Angle(rotor.localRotation, rotorRotation), Is.LessThan(.001f));
            }
            finally { Time.timeScale = 1; }
        }

        [UnityTest] public IEnumerator CampaignDeviceCellsResolveToLatestDirectionalV12Renderers()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Runtime/ANIMOL_Puzzle.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null; yield return null;
            var puzzle = Object.FindFirstObjectByType<PuzzleController>();
            Assert.That(puzzle, Is.Not.Null);
            int checkedDevices = 0;
            foreach (var cell in puzzle.RuntimeMap.devices)
            {
                if (cell.kind != DeviceKind.Conveyor && cell.kind != DeviceKind.Wind && cell.kind != DeviceKind.Brake)
                    continue;
                var go = GameObject.Find("V12 " + cell.kind + " " + cell.position);
                Assert.That(go, Is.Not.Null, cell.kind + " " + cell.position);
                var player = go.GetComponent<StarwayRanchV12AnimatedVisual>();
                var renderer = go.GetComponent<SpriteRenderer>();
                Assert.That(player, Is.Not.Null);
                Assert.That(player.CurrentKey, Is.Not.Empty);
                Assert.That(UnityEditor.AssetDatabase.GetAssetPath(renderer.sprite),
                    Does.StartWith("Assets/ANIMOL/Sprites/StarwayRanch/ANIMOL_STARWAY_RANCH_CARTOON_V12/"));
                checkedDevices++;
            }
            Assert.That(checkedDevices, Is.GreaterThanOrEqualTo(10));
            var wind = puzzle.RuntimeMap.devices.First(d => d.kind == DeviceKind.Wind);
            var windGo = GameObject.Find("V12 Wind " + wind.position);
            puzzle.View.StartClassicWind(wind.position);
            Assert.That(windGo.GetComponent<StarwayRanchV12AnimatedVisual>().CurrentKey,
                Does.EndWith("_active"));
            Assert.That(windGo.transform.Find("V12 wind range overlay"), Is.Null);
            puzzle.View.FinishClassicWind(wind.position);
            Assert.That(windGo.GetComponent<StarwayRanchV12AnimatedVisual>().CurrentKey,
                Does.EndWith("_idle"));
        }

        [UnityTest] public IEnumerator PrototypeUsesV13DeviceVisualsWithoutChangingPuzzleAuthority()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Prototype/ANIMOL_starway_Prototype.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null; yield return null;
            var puzzle = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.That(puzzle, Is.Not.Null);
            Assert.That(puzzle.Session, Is.Not.Null);
            var rig = Object.FindFirstObjectByType<StarwayRanchV12BackgroundRig>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(129));
            var camera = Camera.main;
            Assert.That(camera.farClipPlane, Is.GreaterThan(120));
            Assert.That(camera.orthographic, Is.True);
            Assert.That(StarwayRanchV12BackgroundRig.EffectiveParallaxFactor(-10, 0, 70, .03f),
                Is.EqualTo(.00375f).Within(.00001f));
            var art = puzzle.RuntimeMap.theme.starwayRanch;
            Assert.That(art.v13Devices, Is.Not.Null);
            var stone = puzzle.Session.Devices.First(d => d.role == PrototypeRole.StarSocket && d.variant != "socket");
            var stoneView = puzzle.Devices.Actor(stone.id).GetComponent<DeviceVisualView>();
            Assert.That(stoneView.enabled, Is.False);
            var stoneVisual = puzzle.Devices.Actor(stone.id).GetComponentInChildren<RanchDeviceVisualV13>();
            Assert.That(stoneVisual.CurrentKey, Is.EqualTo("star/triangle/stone"));
            Assert.That(stoneVisual.transform, Is.Not.EqualTo(puzzle.Devices.Actor(stone.id)));
            Assert.That(art.v13Devices.TryClip(stoneVisual.CurrentKey, out var stoneClip), Is.True);
            Assert.That(stoneView.visual.sprite, Is.SameAs(stoneClip.frames[0]));
            var socket = puzzle.Session.Devices.First(d => d.role == PrototypeRole.StarSocket && d.variant == "socket");
            var socketView = puzzle.Devices.Actor(socket.id).GetComponent<DeviceVisualView>();
            Assert.That(socketView.enabled, Is.False);
            var socketVisual = puzzle.Devices.Actor(socket.id).GetComponentInChildren<RanchDeviceVisualV13>();
            Assert.That(socketVisual.CurrentKey,
                Is.EqualTo(puzzle.Session.States[socket.id].active ? "star/triangle/filled" : "star/triangle/empty"));
            Assert.That(puzzle.Devices.MissingBindings, Is.Zero);
            var chick = puzzle.Session.Devices.First(d => d.role == PrototypeRole.Chick);
            var chickRoot = puzzle.Devices.Actor(chick.id);
            var chickVisual = chickRoot.GetComponent<DeviceVisualView>().visual.transform;
            Assert.That(chickRoot.GetComponent<DeviceVisualView>().enabled, Is.False);
            Assert.That(chickRoot.GetComponentInChildren<RanchDeviceVisualV13>(), Is.Not.Null);
            Assert.That(chickRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(chickVisual.localScale.x, Is.EqualTo(.62f).Within(.00001f));
            Assert.That(puzzle.Session.Footprint(chick).Count(), Is.EqualTo(1));

            puzzle.FollowCamera.enabled = false;
            var star = rig.transform.Find("far_dot_00");
            var cloud = rig.transform.Find("far_cloud_left");
            Assert.That(star, Is.Not.Null); Assert.That(cloud, Is.Not.Null);
            var starScreen = star.position.x - camera.transform.position.x;
            var cloudScreen = cloud.position.x - camera.transform.position.x;
            camera.transform.position += Vector3.right;
            yield return null;
            var starShift = star.position.x - camera.transform.position.x - starScreen;
            var cloudShift = cloud.position.x - camera.transform.position.x - cloudScreen;
            Assert.That(starShift, Is.EqualTo(-.00375f).Within(.02f));
            Assert.That(cloudShift, Is.EqualTo(-.35714f).Within(.04f),
                $"scale={Time.timeScale}, delta={Time.deltaTime}, rigActive={rig.isActiveAndEnabled}, reduced={rig.ReducedMotion}");
            try
            {
                Time.timeScale = 0;
                yield return null;
                var pausedCloud = cloud.position;
                yield return new WaitForSecondsRealtime(.12f);
                Assert.That(Vector3.Distance(cloud.position, pausedCloud), Is.LessThan(.0001f));
                rig.SetReducedMotion(true);
                var locked = cloud.position.x - camera.transform.position.x;
                camera.transform.position += Vector3.right;
                yield return null;
                Assert.That(cloud.position.x - camera.transform.position.x,
                    Is.EqualTo(locked).Within(.0001f));
            }
            finally { Time.timeScale = 1; }
            Assert.That(puzzle.BlocksInput, Is.False);
        }
    }
}
