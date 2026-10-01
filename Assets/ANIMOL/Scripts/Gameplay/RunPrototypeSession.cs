using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ANIMOL.Core;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Gameplay
{
    public enum RunProbeStrategy { Manual, Walk, Mixed, RunUntilExhausted }

    [Serializable]
    public sealed class RunProbeResult
    {
        public string scenario, strategy;
        public bool completed, landed, landingInsideReservation, exhausted;
        public float elapsed, minimumStamina = 100, finishStamina, landingX, landingY, landingStamina;
        public float walkingSeconds, runningSeconds, idleSeconds, firstExhaustionX;
        public int launches, jumpRequests, respawns, unexpectedAirborneSteps;
        public float peakY;
        public int[] continuousFloorTiles;
    }

    /// <summary>Isolated playable prototype. The probe uses only normal swipe inputs; it never moves the player.</summary>
    public sealed class RunPrototypeSession : MonoBehaviour
    {
        public RunRoutePlan Plan;
        public StageMapDefinition Map;
        public GameObject PlayerPrefab;
        public Text Header, Readout, Instructions;
        public DevPlayerController Player { get; private set; }
        public SideSpringObject Spring { get; private set; }
        public RunSpringLift Lift { get; private set; }
        public bool Landed => Result != null && Result.landed;
        public bool Completed => Result != null && Result.completed;
        public RunProbeResult Result { get; private set; }
        public RunProbeStrategy Strategy { get; private set; }
        public bool ProbeDone { get; private set; }
        private Sprite pixel;
        private float elapsed, nextSample;
        private bool wasStunned, captureRunning, mixedWalking;
        private readonly StringBuilder samples = new StringBuilder();
        private readonly HashSet<string> captures = new HashSet<string>();
        private string captureDirectory;

        private void OnDestroy() { if (pixel != null) Destroy(pixel); }

        private IEnumerator Start()
        {
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            BuildTerrain();
            BuildSpring();
            ResetPlayer();
            yield return null; yield return null;
            ConfigureHud();
        }

        private void BuildTerrain()
        {
            var root = new GameObject("Prototype terrain from 16x16 chunk cells").transform;
            root.SetParent(transform, false);
            foreach (var row in Map.Cells.Where(c => c.Layer == StageMapLayer.Terrain).GroupBy(c => new { c.Y, c.TileId }))
            {
                var xs = row.Select(c => c.X).OrderBy(x => x).ToArray();
                int start = xs[0], end = start;
                for (int i = 1; i <= xs.Length; i++)
                {
                    if (i < xs.Length && xs[i] == end + 1) { end = xs[i]; continue; }
                    bool oneWay = row.Key.TileId != RunRoutePlan.SolidTile;
                    // A small step down starts contact from above, not from a one-way collider's ignored side.
                    float topInset = row.Key.TileId == RunRoutePlan.ApproachTile ? .125f : 0;
                    string id = row.Key.Y == Plan.Legs[0].FloorY ? "RunFloor-A" : row.Key.Y == Plan.Legs[1].FloorY ? "RunFloor-B" : "Optional-OneWay";
                    var floor = Rect(root, id, new Vector2((start + end + 1) * .5f, row.Key.Y + (oneWay ? .875f : .5f) - topInset),
                        new Vector2(end - start + 1, oneWay ? .25f : 1), new Color32(35, 62, 81, 255), 0);
                    var box = floor.AddComponent<BoxCollider2D>();
                    if (oneWay) { box.usedByEffector = true; var effector = floor.AddComponent<PlatformEffector2D>(); effector.useOneWay = true; effector.surfaceArc = 160; }
                    Rect(root, "Readable top surface", new Vector2((start + end + 1) * .5f, row.Key.Y + .96f - topInset),
                        new Vector2(end - start + 1, .08f), oneWay ? new Color32(224, 183, 114, 255) : new Color32(127, 205, 187, 255), 2);
                    if (i < xs.Length) start = end = xs[i];
                }
            }
            foreach (var leg in Plan.Legs)
            {
                for (int x = 0; x <= 80; x += 16)
                {
                    Rect(root, "Chunk seam marker", new Vector2(x, leg.FloorY + .4f), new Vector2(.04f, .65f), new Color32(76, 114, 134, 255), 3);
                    Label(root, $"{(leg.Direction > 0 ? x : 80 - x):00} / 80 {(leg.Direction > 0 ? ">" : "<")}", new Vector2(x + 1, leg.FloorY + 2.3f), .21f);
                }
            }
            Label(root, "TURN  /  NOT PART OF 80", new Vector2(85, Plan.Legs[0].FloorY + 3), .22f);
            Label(root, "LAND + RECOVER  /  16 TILES", new Vector2(84, Plan.Legs[1].FloorY + 2.2f), .20f);
            Label(root, "OPTIONAL JUMP PRACTICE", new Vector2(-9, Plan.Legs[0].FloorY + 6), .20f);
            // Non-colliding architectural guide bars make the depth and the layer change readable.
            for (int x = -4; x <= 96; x += 8)
                Rect(root, "Background post", new Vector2(x, 5), new Vector2(.15f, 20), new Color32(23, 40, 58, 255), -10);
        }

        private void BuildSpring()
        {
            var cell = Plan.Turn.SpringCell;
            var go = new GameObject("Turn spring", typeof(BoxCollider2D), typeof(SideSpringObject), typeof(RunSpringLift));
            go.transform.SetParent(transform, false);
            var settings = new StageMapObjectSettings();
            settings.EditorConfigure(1, StageMapObjectDirection.Left, new Vector2(1, 5), HalfBlockPlacement.Lower,
                SideSpringContactPolicy.FacingSideOnly, -Plan.Turn.SpringImpulse.x, Plan.Turn.SpringImpulse.y,
                3, 0, 0, 5, .55f, 0, true, null);
            Spring = go.GetComponent<SideSpringObject>();
            Spring.Configure(new StageMapObjectPlacement("Run-Turn-Spring", StageMapObjectKind.SideSpring, cell.x, cell.y, "", null, settings), 1);
            // Facing left mirrors the local collider offset. The near face is at x=93.5.
            var box = go.GetComponent<BoxCollider2D>(); box.size = new Vector2(1, 5); box.offset = new Vector2(0, 1.5f);
            Lift = go.GetComponent<RunSpringLift>();
            Rect(transform, "Spring backing", new Vector2(cell.x, cell.y + 1.5f), new Vector2(1, 5), new Color32(101, 64, 51, 255), 3);
            for (int i = 0; i < 6; i++) Rect(transform, "Spring coil", new Vector2(cell.x - .25f, cell.y - .3f + i * .65f), new Vector2(.8f, .18f), new Color32(239, 184, 101, 255), 4);
            Label(transform, Plan.Turn.Ascending ? "< UP" : "< DOWN", new Vector2(93, cell.y + 5), .3f);
        }

        private GameObject Rect(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false);
            go.transform.position = position; go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = pixel; renderer.color = color; renderer.sortingOrder = order;
            return go;
        }
        private void Label(Transform parent, string text, Vector2 position, float size)
        {
            var go = new GameObject(text, typeof(TextMesh)); go.transform.SetParent(parent, false); go.transform.position = position;
            var label = go.GetComponent<TextMesh>(); label.text = text; label.fontSize = 48; label.characterSize = size * .4f;
            label.anchor = TextAnchor.MiddleCenter; label.color = new Color32(167, 193, 201, 255);
        }

        public void ResetPlayer()
        {
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); }
            var go = Instantiate(PlayerPrefab, new Vector3(-1, Plan.Legs[0].FloorY + 1.65f, 0), Quaternion.identity);
            go.name = "Running prototype default player"; Player = go.GetComponent<DevPlayerController>();
            Player.ConfigureCampaignRoster(new[] { "DEV_GROUND" }, "DEV_GROUND", "DEV_GROUND");
            Spring.ResetRuntimeState(); Lift.ResetLift();
            Result = new RunProbeResult { scenario = Plan.Turn.Ascending ? "up" : "down", strategy = "Manual",
                continuousFloorTiles = Plan.Legs.Select(l => Plan.ContinuousFloorLength(Map, l)).ToArray() };
            elapsed = nextSample = 0; wasStunned = mixedWalking = ProbeDone = false; Strategy = RunProbeStrategy.Manual;
            samples.Clear(); samples.AppendLine("seconds,x,y,velocityX,velocityY,pace,stamina,grounded,stunned,launches,landed");
            Camera.main.transform.position = new Vector3(1.2f, Player.transform.position.y + 1.2f, -10);
        }

        public void BeginProbe(RunProbeStrategy strategy)
        {
            ResetPlayer(); Strategy = strategy; Result.strategy = strategy.ToString();
        }

        private void FixedUpdate()
        {
            if (Player == null || Result == null || ProbeDone) return;
            elapsed += Time.fixedDeltaTime;
            if (Strategy != RunProbeStrategy.Manual) DriveProbe();
            Result.minimumStamina = Mathf.Min(Result.minimumStamina, Player.Stamina);
            if (Player.Pace == LocomotionPace.Run) Result.runningSeconds += Time.fixedDeltaTime;
            else if (Player.Pace == LocomotionPace.Walk) Result.walkingSeconds += Time.fixedDeltaTime;
            else Result.idleSeconds += Time.fixedDeltaTime;
            if (Player.IsStunned && !wasStunned) { Result.exhausted = true; Result.firstExhaustionX = Player.Body.position.x; }
            wasStunned = Player.IsStunned;
            Result.launches = Spring.ActivationCount;
            Result.peakY = Mathf.Max(Result.peakY, Player.Body.position.y);
            if (elapsed > .5f && Player.Body.position.x > 2 && Player.Body.position.x < 78 && !Player.IsGrounded)
                Result.unexpectedAirborneSteps++;
            if (!Landed && Spring.ActivationCount > 0 && Player.IsGrounded && Player.LastGroundColliderName == "RunFloor-B" &&
                Player.Body.linearVelocity.y <= .1f && Player.Body.position.y >= Plan.Legs[1].FloorY + 1.45f)
            {
                Result.landed = true; Result.landingX = Player.Body.position.x; Result.landingY = Player.Body.position.y; Result.landingStamina = Player.Stamina;
                Result.landingInsideReservation = Player.Body.position.x > Plan.Turn.Landing.xMin + .5f && Player.Body.position.x < Plan.Turn.Landing.xMax - .5f;
            }
            if (Landed && Player.IsGrounded && Player.Body.position.x < -.5f) { Result.completed = true; FinishProbe(); }
            if (elapsed >= 100 || Player.Body.position.y < -8 || (Strategy == RunProbeStrategy.RunUntilExhausted && Result.exhausted)) FinishProbe();
            if (elapsed >= nextSample) { nextSample += .2f; AppendSample(); }
            Result.elapsed = elapsed; Result.finishStamina = Player.Stamina;
        }

        private void DriveProbe()
        {
            if (elapsed < .3f || Player.IsStunned) return;
            int direction = Spring.ActivationCount > 0 ? -1 : 1;
            bool run = Strategy == RunProbeStrategy.RunUntilExhausted;
            if (Strategy == RunProbeStrategy.Mixed)
            {
                if (Player.Stamina < 30) mixedWalking = true;
                if (Player.Stamina > 85) mixedWalking = false;
                // Turn approach and landing are an explicit, generous recovery zone, not counted in the run legs.
                run = !mixedWalking && Player.Body.position.x < 80;
            }
            SetPace(direction, run);
        }

        private void SetPace(int direction, bool run)
        {
            if (Player.MoveDirection != 0 && Player.MoveDirection != direction) Player.ApplySwipe(-811, direction);
            if (!run && Player.Pace == LocomotionPace.Run) Player.ApplySwipe(-811, -direction);
            if (Player.Pace == LocomotionPace.Idle) Player.ApplySwipe(-811, direction);
            if (run && Player.Pace == LocomotionPace.Walk) Player.ApplySwipe(-811, direction);
        }

        private void FinishProbe()
        {
            ProbeDone = true; Result.elapsed = elapsed; Result.finishStamina = Player.Stamina; AppendSample();
            if (Player.MoveDirection != 0) Player.ApplySwipe(-811, -Player.MoveDirection);
        }
        private void AppendSample()
        {
            var p = Player.Body.position; var v = Player.Body.linearVelocity;
            samples.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F3},{1:F3},{2:F3},{3:F3},{4:F3},{5},{6:F3},{7},{8},{9},{10}",
                elapsed, p.x, p.y, v.x, v.y, Player.Pace, Player.Stamina, Player.IsGrounded, Player.IsStunned, Spring.ActivationCount, Landed));
        }
        public void SaveEvidence(string directory)
        {
            Directory.CreateDirectory(directory);
            string prefix = Result.scenario + "_" + Result.strategy;
            File.WriteAllText(Path.Combine(directory, prefix + ".json"), JsonUtility.ToJson(Result, true));
            File.WriteAllText(Path.Combine(directory, prefix + ".csv"), samples.ToString());
        }

        private void Update()
        {
            if (Player == null) return;
            if (!captureRunning)
            {
                if (Input.GetKeyDown(KeyCode.R)) ResetPlayer();
                if (Input.GetKeyDown(KeyCode.F1)) BeginProbe(RunProbeStrategy.Walk);
                if (Input.GetKeyDown(KeyCode.F2)) BeginProbe(RunProbeStrategy.Mixed);
                if (Input.GetKeyDown(KeyCode.F3)) BeginProbe(RunProbeStrategy.RunUntilExhausted);
            }
            string staminaRate = Player.IsStunned ? "소진 · 회복 대기" : Player.Pace == LocomotionPace.Run ? "−18 / 초" : Player.IsGrounded || Player.Pace == LocomotionPace.Idle ? "+15 / 초" : "공중 회복 없음";
            if (Readout != null) Readout.text = $"{(Landed ? "B  ←" : "A  →")}   {elapsed:0.0}s   {Player.Pace} · {Strategy}\n스태미나 {Player.Stamina:000} / 100   {staminaRate}\n{(Completed ? "완주 확인" : Landed ? "착지 성공 · 복귀 주로" : Spring.ActivationCount > 0 ? "층 전환 중" : "연속 주로 80타일 · 5청크")}";
            if (Player.Body.position.y < -8 && Strategy == RunProbeStrategy.Manual && !captureRunning) ResetPlayer();
        }

        private void ConfigureHud()
        {
            var canvas = Header.GetComponentInParent<Canvas>();
            foreach (var text in canvas.GetComponentsInChildren<Text>(true))
                if (text != Header && text != Readout && text != Instructions && text.GetComponentInParent<DevMobileTouchControl>() == null && text.GetComponentInParent<Button>() == null)
                    text.gameObject.SetActive(false);
            foreach (var control in canvas.GetComponentsInChildren<DevMobileTouchControl>(true))
                if (control.TryGetComponent<Image>(out var image)) image.color = new Color(.08f, .2f, .25f, MobileControlPreferences.Opacity);
            Header.text = Plan.Turn.Ascending ? "RUN LAB / 상승 전환" : "RUN LAB / 하강 전환";
            Instructions.text = "같은 방향 2회: 달리기 · 반대: 정지\nR 재시작   F1 걷기   F2 혼합   F3 소진 검사";
            var pause = canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "PauseButton");
            if (pause != null) pause.gameObject.SetActive(false);
        }

        public void CapturePlay(string directory)
        {
            if (!captureRunning) StartCoroutine(CaptureRoutine(directory));
        }
        private IEnumerator CaptureRoutine(string directory)
        {
            captureRunning = true; captureDirectory = directory; captures.Clear(); Directory.CreateDirectory(directory);
            BeginProbe(RunProbeStrategy.Mixed);
            while (!ProbeDone)
            {
                yield return new WaitForEndOfFrame();
                string phase = Spring.ActivationCount == 0 ? Player.Body.position.x > 85 ? "approach" : elapsed > 2 && Player.Pace == LocomotionPace.Walk ? "recover_walk" : elapsed > 1 ? "run" : "start"
                    : !Landed ? "flight" : Player.Body.position.x > 80 ? "landing" : "return_run";
                if (Spring.ActivationCount > 0 && !Landed)
                {
                    bool clearOfLaunch = Plan.Turn.Ascending ? Player.Body.position.y > Plan.Legs[0].FloorY + 5 : Player.Body.position.y < Plan.Legs[0].FloorY - 1;
                    phase = clearOfLaunch ? "flight" : "launch";
                    if (Plan.Turn.Ascending && clearOfLaunch && Mathf.Abs(Player.Body.linearVelocity.y) < 2) phase = "apex";
                }
                if (captures.Add(phase)) SaveScreen(phase);
            }
            yield return new WaitForEndOfFrame(); SaveScreen("finish"); SaveEvidence(directory);
            File.WriteAllText(Path.Combine(directory, Result.scenario + "_capture.complete"), "Actual Game View with overlay HUD; automatic normal-input replay.");
            captureRunning = false;
        }
        private void SaveScreen(string phase)
        {
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(captureDirectory, $"{Result.scenario}_{phase}_{Screen.width}x{Screen.height}.png"), texture.EncodeToPNG());
            Destroy(texture);
        }
    }
}
