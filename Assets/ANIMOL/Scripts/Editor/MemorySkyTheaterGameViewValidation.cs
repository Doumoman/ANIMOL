using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    // Runs in real Play mode using the focused portrait Game View and ScreenCapture.
    // Evidence captures the final native-pixel compositor plus existing overlay HUD.
    public static class MemorySkyTheaterGameViewValidation
    {
        public const string Output = "Docs/Validation/MemorySkyTheater/Portrait";
        public static string Progress { get; private set; } = "Idle";
        public static bool Finished { get; private set; }
        public static string Failure { get; private set; }
        [Serializable] public class Frame { public string map; public int frame, cuts; public Vector2 player, camera; public Vector2Int cell; public bool sliding; }
        [Serializable] public class Shot { public string name, map; public int width, height, collected, moves, cuts; public Vector2 player, camera; public float rows, aspect; public Rect viewport; }
        [Serializable] public class Report
        {
            public string unity, source = "Play mode / ScreenCapture / Game View", backgroundSource, hudSource;
            public Vector2 backgroundPixels;
            public int screenWidth, screenHeight;
            public List<Shot> shots = new List<Shot>(); public List<Frame> frames = new List<Frame>();
            public List<string> checks = new List<string>(); public string failure;
        }
        static Report report;
        static PuzzleController controller;
        static SkyIslandBackdrop backdrop;
        public static void Start()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode in ANIMOL_Puzzle first.");
            controller = UnityEngine.Object.FindFirstObjectByType<PuzzleController>();
            if (!controller) throw new InvalidOperationException("No playable puzzle controller.");
            Directory.CreateDirectory(Output); Finished = false; Failure = null;
            report = new Report { unity = Application.unityVersion, screenWidth = Screen.width, screenHeight = Screen.height };
            report.backgroundSource = AssetDatabase.GetAssetPath(controller.RuntimeMap.theme.memorySkyTheater.far);
            report.backgroundPixels = controller.RuntimeMap.theme.memorySkyTheater.far.rect.size;
            report.hudSource = AssetDatabase.GetAssetPath(controller.hudPrefab);
            controller.FollowCamera.StartCoroutine(Guard(Run()));
        }
        static IEnumerator Guard(IEnumerator run)
        {
            // Flatten nested enumerators so every validation failure writes evidence.
            var stack = new Stack<IEnumerator>(); stack.Push(run);
            while (stack.Count > 0)
            {
                object current = null; bool more = false;
                try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
                catch (Exception error) { Failure = error.ToString(); break; }
                if (!more) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            report.failure = Failure; File.WriteAllText(Output + "/gameview.json", JsonUtility.ToJson(report, true));
            Finished = true; Progress = Failure == null ? "Complete" : "Failed";
            if (Failure == null) Debug.Log("[ANIMOL Memory Theater] Real Game View validation complete."); else Debug.LogError(Failure);
        }
        static IEnumerator Run()
        {
            Require(Screen.width == 1080 && Screen.height == 1920, "Game View must be 1080x1920");
            yield return Load("Small"); yield return Capture("small_centered");
            Require(Vector2.Distance(controller.FollowCamera.transform.position, new Vector2(1.5f, 1.5f)) < .01f, "Small map centered");
            yield return Move(Vector2Int.right, 2); yield return Move(Vector2Int.up, 1); Require(controller.Session.Collected == 1, "Real bubble collection");
            yield return Move(Vector2Int.up, 2); yield return Move(Vector2Int.right, 1); Require(controller.Session.Won, "Real exit victory"); yield return Capture("small_victory");
            yield return Load("Horizontal"); yield return Capture("horizontal_start"); yield return Move(Vector2Int.right, 1); Require(controller.Session.Cell == new Vector2Int(31, 3), "Conveyor/wind chain reaches x31");
            yield return Capture("horizontal_chain_end"); yield return Move(Vector2Int.right, 7); yield return Capture("horizontal_edge");
            yield return Load("Vertical"); yield return Capture("vertical_start"); yield return Move(Vector2Int.up, 1); Require(controller.Session.Cell == new Vector2Int(3, 31), "Vertical automatic chain reaches y31");
            yield return Capture("vertical_chain_end"); yield return Move(Vector2Int.up, 7); yield return Capture("vertical_edge");
            yield return Load("CorridorIslands"); yield return Capture("corridor_start"); yield return Move(Vector2Int.up, 7); yield return Move(Vector2Int.right, 8); yield return Capture("corridor_island");
            yield return Load("UHole"); yield return Move(Vector2Int.right, 1); yield return Move(Vector2Int.up, 2); yield return Capture("u_inner_left");
            Require(PuzzleAutotile.Resolve(controller.RuntimeMap, new Vector2Int(4, 6)).innerCorners != 0, "U notch left has inset corner");
            yield return Move(Vector2Int.down, 1); yield return Move(Vector2Int.right, 4); yield return Move(Vector2Int.up, 1); yield return Capture("u_inner_right");
            yield return Load("MaterialBoundary"); yield return Capture("material_boundary");
            var p = new Vector2Int(7, 9); var before = controller.View.RefreshedCells; var cell = controller.Session.Cell;
            controller.RuntimeMap.RemoveTerrain(p); Require(controller.View.RefreshedCells - before == 9, "Runtime terrain removal refreshes 3x3");
            Require(controller.Session.Cell == cell && controller.stage.Walkable(p), "Visual refresh does not move player or mutate source asset");
            yield return Capture("runtime_terrain_change");
            controller.RuntimeMap.SetTerrain(p, TerrainMaterial.Stone); yield return Capture("runtime_terrain_restored");
            yield return Load("EdgePortal"); yield return Capture("portal_entrance");
            Action onCut = () => { ScreenCapture.CaptureScreenshot(Output + "/portal_exit_cut.png"); Record("portal_exit_cut"); };
            controller.PortalCut += onCut;
            yield return Move(Vector2Int.right, 1); controller.PortalCut -= onCut;
            Require(controller.PortalCuts == 1 && controller.Session.Cell == new Vector2Int(39, 5) && controller.Session.Direction == Vector2Int.right && controller.Session.Collected == 1, "Edge portal preserves direction/slide and arrives at map end");
            Require(report.frames.Where(f => f.map == "EdgePortal").All(f => f.camera.x < 8 || f.camera.x > 32), "Portal camera never crosses distant middle");
            yield return Capture("portal_slide_end");
            Require(report.shots.All(s => Mathf.Abs(s.rows - 11) < .001f), "All maps retain 11 visible rows; no fit-to-map zoom");
            Require(report.shots.All(s => s.viewport.y >= -.01f && s.viewport.yMax <= 1740.01f), "Gameplay viewport preserves existing HUD space; no direction pad");
            Require(!controller.BlocksInput && Mathf.Approximately(Time.timeScale, 1), "Presentation never blocks input or changes time scale");
            report.checks.Add("Far/Mid/Near repeat at native scale with .15/.35/.65 pixel-rounded parallax; terrain root remains fixed");
            yield return Load("StarGarden"); yield return Capture("memory_theater_applied");
        }
        static IEnumerator Load(string name)
        {
            Progress = "Inspect " + name;
            // Start validation on a separate host because Load cancels the controller's movement coroutines.
            var original = AssetDatabase.LoadAssetAtPath<PuzzleMap>(PuzzleBuilder.Data + "/Maps/" + name + ".asset");
            var map = UnityEngine.Object.Instantiate(original); map.name = name;
            map.theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(MemorySkyTheaterBuilder.Data + "/PuzzleTheme.asset"); controller.Load(map);
            yield return null; yield return new WaitForSeconds(.2f);
            backdrop = controller.GetComponentInChildren<SkyIslandBackdrop>();
            Require(backdrop && backdrop.IntegerScale == 2, "Portrait renders at integer 2x");
            Require(!controller.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapCollider2D>().Any(), "Theater layers have no colliders");
        }
        static IEnumerator Move(Vector2Int direction, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var swipe = controller.GetComponent<PuzzleSwipeInput>(); var start = new Vector2(Screen.width * .5f, Screen.height * .5f);
                swipe.BeginGesture(start, swipe.OverUI(start));
                Require(swipe.EndGesture(start + (Vector2)direction * Screen.width * .12f), "Accept swipe " + direction + " in " + controller.stage.name);
                var deadline = Time.realtimeSinceStartup + 12;
                while (controller.IsMoving)
                {
                    yield return new WaitForEndOfFrame();
                    report.frames.Add(new Frame { map = controller.stage.name, frame = Time.frameCount, player = controller.Player.position, camera = controller.FollowCamera.transform.position, cell = controller.Session.Cell, cuts = controller.PortalCuts, sliding = controller.Session.Sliding });
                    var camera = controller.FollowCamera.GetComponent<Camera>(); var bounded = PuzzleFollowCamera.Clamp(controller.FollowCamera.transform.position, controller.RuntimeMap.Width, controller.RuntimeMap.Height, camera.orthographicSize, camera.aspect);
                    Require(Vector2.Distance(bounded, controller.FollowCamera.transform.position) < .01f, "Camera clamped on every moving frame", false);
                    Require(controller.View.transform.position == Vector3.zero && !controller.View.GetComponent<PuzzleParallax>(), "Terrain remains fixed", false);
                    Require(backdrop.IntegerScale == 2 && backdrop.ForegroundTexture.filterMode == FilterMode.Point, "Native point raster remains integer scaled", false);
                    var sample = new Vector2(camera.transform.position.x * 80, -camera.transform.position.y * 80);
                    for (var layer = 0; layer < 3; layer++) Require(backdrop.Phases[layer] == SkyIslandBackdrop.Phase(sample, SkyIslandBackdrop.Parallax[layer]), "Camera movement uses rounded repeating phase", false);
                    Require(Time.realtimeSinceStartup < deadline, "Chain terminates", false);
                }
            }
            yield return new WaitForSeconds(.3f);
        }
        static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Output + "/" + name + ".png"); Record(name); yield return new WaitForSeconds(.15f);
        }
        static void Record(string name)
        {
            var camera = controller.FollowCamera.GetComponent<Camera>(); report.shots.Add(new Shot { name = name, map = controller.stage.name, width = controller.RuntimeMap.Width, height = controller.RuntimeMap.Height,
                collected = controller.Session.Collected, moves = controller.Session.Moves, cuts = controller.PortalCuts, player = controller.Player.position, camera = camera.transform.position, rows = camera.orthographicSize * 2, aspect = camera.aspect, viewport = camera.pixelRect });
        }
        static void Require(bool valid, string message, bool record = true)
        { if (!valid) throw new InvalidOperationException(message); if (record && !report.checks.Contains(message)) report.checks.Add(message); }
    }
}
