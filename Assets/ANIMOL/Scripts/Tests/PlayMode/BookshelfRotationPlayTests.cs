#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ANIMOL.Tests
{
    public sealed class BookshelfRotationPlayTests
    {
        [UnityTest] public IEnumerator Et12RuntimeControllerRotatesAfterTheBlockingCartMovesAway()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                "Assets/ANIMOL/Scenes/Prototype/ANIMOL_memory_theater_Prototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null; yield return null; yield return null;
            var host = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-12_MapData.asset");
            host.secondsPerCell = .01f; host.Load(source);
            const string route = "RUUUUUUDLLDURRDL";
            foreach (var input in route)
            {
                Assert.That(host.RequestMove(input == 'R' ? Vector2Int.right : input == 'L' ? Vector2Int.left :
                    input == 'U' ? Vector2Int.up : Vector2Int.down), Is.True);
                var deadline = Time.realtimeSinceStartup + 5;
                while (host.IsMoving && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(host.IsMoving, Is.False, "ET-12 input did not settle: " + input);
            }
            Assert.That(host.Session.States["statue_1"].cell, Is.EqualTo(new Vector2Int(12, 9)));
            Assert.That(host.Session.States["movingstage_1"].cell, Is.EqualTo(new Vector2Int(10, 8)));
            Assert.That(host.Session.States["bookshelf_1"].phase, Is.EqualTo(1));
            Assert.That(host.Session.Cost, Is.EqualTo(route.Length));
            var view = host.Devices.Actor("bookshelf_1").GetComponent<DeviceVisualView>();
            var visualDeadline = Time.realtimeSinceStartup + 2;
            while (view.CurrentState != "vertical" && Time.realtimeSinceStartup < visualDeadline) yield return null;
            Assert.That(view.CurrentState, Is.EqualTo("vertical"));
            Assert.That(view.BlocksInput, Is.False); Assert.That(host.Devices.MissingBindings, Is.Zero);
        }

        [UnityTest] public IEnumerator CommittedArtSweepsTheSameDiagonalAsCollisionInEveryPoseAndDirection()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/Prototypes/Device08_Bookshelf.asset");
            var prefab = source.scenario.devices.Single(d => d.role == PrototypeRole.Bookshelf && d.variant != "plate").visualPrefab;
            var map = ScriptableObject.CreateInstance<PuzzleMap>();
            var rules = ScriptableObject.CreateInstance<PuzzleScenario>();
            map.Resize(7, 7); map.theme = source.theme; map.scenario = rules;
            map.start = new Vector2Int(1, 0); map.exit = new Vector2Int(0, 6); map.targetBubbles = 0;
            foreach (var cell in new[] { map.start, map.exit, new Vector2Int(1, 1), new Vector2Int(4, 3) })
                map.SetTerrain(cell, TerrainMaterial.TheaterStageFloor);
            rules.themeId = "memory_theater";
            rules.devices.Add(new PrototypeDevice { id = "plate", role = PrototypeRole.Bookshelf, variant = "plate",
                group = "books", position = new Vector2Int(1, 1), visualPrefab = prefab });
            var shelf = new PrototypeDevice { id = "shelf", role = PrototypeRole.Bookshelf,
                group = "books", position = new Vector2Int(4, 3), visualPrefab = prefab };
            rules.devices.Add(shelf);
            GameObject host = null;
            try
            {
                for (var phase = 0; phase < 4; phase++) foreach (var ccw in new[] { false, true })
                {
                    rules.echoTheaterV3Rules = phase < 2;
                    shelf.orientation = phase;
                    shelf.bookshelfRotation = ccw ? BookshelfRotationDirection.CounterClockwise : BookshelfRotationDirection.Clockwise;
                    host = new GameObject("Bookshelf sweep test");
                    var player = host.AddComponent<SpriteRenderer>();
                    var presenter = host.AddComponent<PuzzleDeviceView>();
                    var session = new PuzzleDeviceSession(map); presenter.Bind(session, player);
                    var cue = session.Apply(Vector2Int.up).events.Single(e => e.eventName == "bookshelf_rotation_committed");
                    presenter.Notify(cue, "sweep");
                    var view = host.GetComponentsInChildren<DeviceVisualView>().Single(v => v.gameObject.name == shelf.id);
                    var art = view.GetComponent<ANIMOL.Art.EchoTheaterV03.EchoTheaterV03AnimatedVisual>();
                    Assert.That(art.ApplyNormalizedProgress(.5f), Is.True); view.Advance(0);
                    var expectedSign = ((phase & 1) == 0) != ccw ? -1 : 1;
                    Assert.That(DiagonalSign(view.visual), Is.EqualTo(expectedSign), "phase=" + phase + ", ccw=" + ccw);
                    Assert.That(BookshelfRotationRules.SweptCells(shelf.position, phase, shelf.bookshelfRotation),
                        Does.Contain(shelf.position + new Vector2Int(1, expectedSign)));
                    Assert.That(presenter.MissingBindings, Is.Zero); Assert.That(view.BlocksInput, Is.False);
                    view.paused = true; var flipped = view.visual.flipX; var elapsed = view.Elapsed;
                    yield return null;
                    Assert.That(view.visual.flipX, Is.EqualTo(flipped)); Assert.That(view.Elapsed, Is.EqualTo(elapsed));
                    view.paused = false; view.Advance(0);
                    Assert.That(art.ApplyNormalizedProgress(1), Is.True); view.Advance(0);
                    Assert.That(view.visual.flipX, Is.False, "Terminal pose must match the authored idle.");
                    Assert.That(session.Undo(), Is.True); presenter.Settle(true);
                    Assert.That(view.visual.flipX, Is.False);
                    Assert.That(session.States[shelf.id].phase, Is.EqualTo(phase));
                    presenter.Notify(session.Apply(Vector2Int.up).events.Single(e => e.eventName == "bookshelf_rotation_committed"), "undo-mid-sweep");
                    Assert.That(art.ApplyNormalizedProgress(.5f), Is.True); view.Advance(0);
                    Assert.That(session.Undo(), Is.True); presenter.Settle(true);
                    Assert.That(view.visual.flipX, Is.False, "Undo must clear an intermediate mirror immediately.");
                    Assert.That(view.CurrentState, Is.EqualTo(new[] { "horizontal", "vertical", "horizontal_reversed", "vertical_reversed" }[phase]));
                    Object.Destroy(host); host = null; yield return null;
                }
            }
            finally { if (host) Object.Destroy(host); Object.Destroy(map); Object.Destroy(rules); }
        }

        // Measure the actual painted diagonal, including renderer mirroring,
        // rather than accepting a clip name or a direction flag as evidence.
        static int DiagonalSign(SpriteRenderer renderer)
        {
            var sprite = renderer.sprite;
            var target = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 0);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(sprite.texture, target); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                var data = pixels.GetPixels32(); double covariance = 0;
                var rect = sprite.rect;
                for (var y = (int)rect.y; y < rect.yMax; y += 8)
                    for (var x = (int)rect.x; x < rect.xMax; x += 8)
                        covariance += data[y * target.width + x].a * (x + .5 - rect.x - sprite.pivot.x) * (y + .5 - rect.y - sprite.pivot.y);
                return System.Math.Sign(covariance) * (renderer.flipX ? -1 : 1) * (renderer.flipY ? -1 : 1);
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.Destroy(pixels); }
        }

        [UnityTest] public IEnumerator CounterclockwiseArtRunsBackwardsFreezesOnPauseAndHoldsItsRealTerminal()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/Prototypes/Device08_Bookshelf.asset");
            var shelf = source.scenario.devices.Single(d => d.role == PrototypeRole.Bookshelf && d.variant != "plate");
            var instance = Object.Instantiate(shelf.visualPrefab);
            try
            {
                var view = instance.GetComponent<DeviceVisualView>(); var art = source.theme.memorySkyTheater.v02;
                Assert.That(view.ActivateV03(art.v03Animations, art.v03ManifestSha256), Is.True);
                var binding = view.states.Single(s => s.name == "rotate_90_cw");
                Assert.That(art.v03Animations.TryGetClip(binding.v03ClipKey, art.v03ManifestSha256, out var clip, out var error), Is.True, error);
                Assert.That(view.Notify("ccw", "bookshelf_rotation_committed", binding.name, true), Is.True);
                Assert.That(view.visual.sprite, Is.SameAs(clip.GetFrame(clip.FrameCount - 1).Sprite));
                Assert.That(view.Notify("ccw", "bookshelf_rotation_committed", binding.name, true), Is.False);
                yield return new WaitForSeconds(.03f);
                view.paused = true; view.Advance(0);
                var pausedSprite = view.visual.sprite; var pausedTime = view.Elapsed;
                yield return new WaitForSeconds(.12f);
                Assert.That(view.visual.sprite, Is.SameAs(pausedSprite)); Assert.That(view.Elapsed, Is.EqualTo(pausedTime));
                view.paused = false; view.Advance(0);
                yield return new WaitForSeconds(clip.FrameCount / (float)clip.Fps + .1f);
                Assert.That(view.visual.sprite, Is.SameAs(clip.GetFrame(0).Sprite));
                Assert.That(view.BlocksInput, Is.False);
            }
            finally { Object.Destroy(instance); }
        }
    }
}
#endif
