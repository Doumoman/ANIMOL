using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Gameplay
{
    /// <summary>Opt-in presentation specimen. Never installed into campaign scenes or shared prefabs.</summary>
    public sealed class MoonGraphicsQaScene : MonoBehaviour
    {
        [SerializeField] private StageMapObjectPlacement[] devices;
        [SerializeField] private bool refined = true;
        [SerializeField] private GameObject originalArt;
        [SerializeField] private GameObject refinedArt;
        [SerializeField] private GameObject rabbitArt;
        [SerializeField] private Renderer originalPlayer;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Material cueMaterial;
        [SerializeField] private SpriteInkBounds[] spriteInk;
        [SerializeField] private Material atlasMaterial;
        private readonly Dictionary<SpriteRenderer, Material> originalMaterials = new Dictionary<SpriteRenderer, Material>();
        private readonly Dictionary<SpriteRenderer, bool> originalVisibility = new Dictionary<SpriteRenderer, bool>();
        [Serializable] public sealed class SpriteInkBounds { public Sprite sprite; public Rect bounds; }
        private readonly Dictionary<StageMapRuntimeObject, Vector3[]> originalTransforms = new Dictionary<StageMapRuntimeObject, Vector3[]>();
        private readonly List<StageMapRuntimeObject> runtimeDevices = new List<StageMapRuntimeObject>();
        private readonly Dictionary<StageMapRuntimeObject, LineRenderer> rims = new Dictionary<StageMapRuntimeObject, LineRenderer>();
        private Text status, clockLabel;
        private GameObject pause;
        private float elapsed;
        public bool Refined => refined;
        public IReadOnlyList<StageMapRuntimeObject> Devices => runtimeDevices;
        public static readonly Vector2 Spawn = new Vector2(-2.5f, 1.65f);

        public void Configure(StageMapObjectPlacement[] values, GameObject before, GameObject after,
            GameObject rabbit, Renderer playerRenderer, Canvas ui, Material material, SpriteInkBounds[] ink, Material atlas)
        { devices = values; originalArt = before; refinedArt = after; rabbitArt = rabbit; originalPlayer = playerRenderer; canvas = ui; cueMaterial = material; spriteInk = ink; atlasMaterial = atlas; }

        private IEnumerator Start()
        {
            foreach (var placement in devices)
            {
                var go = Instantiate(placement.Prefab, transform);
                var runtime = StageMapRuntimeFactory.Configure(go, placement, 1f);
                runtimeDevices.Add(runtime);
                var art = runtime.GetComponent<AnimolOperationalArtBinding>()?.ArtRoot;
                if (art != null) originalTransforms[runtime] = new[] { art.transform.localPosition, art.transform.localScale };
                if (art != null) foreach(var renderer in art.GetComponentsInChildren<SpriteRenderer>(true)) originalMaterials[renderer] = renderer.sharedMaterial;
                var line = new GameObject("QA surface cue", typeof(LineRenderer)).GetComponent<LineRenderer>();
                line.transform.SetParent(go.transform, false);
                line.sharedMaterial = cueMaterial; line.useWorldSpace = true; line.positionCount = 2;
                line.startWidth = line.endWidth = 1f / 32f; line.sortingOrder = 25;
                rims.Add(runtime, line);
            }
            foreach (var balance in runtimeDevices.OfType<MoonJadeBalanceObject>()) balance.ResolveLink();
            foreach(var renderer in originalMaterials.Keys) originalVisibility[renderer] = renderer.enabled;
            AlignArt();
            pause = canvas.transform.Find("SafeArea/ModalHost/PauseModal")?.gameObject;
            Bind("PauseButton", TogglePause); Bind("ContinueButton", TogglePause);
            Bind("RestartButton", () => { TogglePause(); ResetSpecimen(); });
            var lobby = canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "LobbyButton");
            if (lobby != null) lobby.gameObject.SetActive(false);
            // Let the existing UI feedback/accessibility installers finish before applying QA-only colors.
            yield return null; yield return null;
            ApplyPresentation(refined);
        }

        public void ApplyPresentation(bool value)
        {
            refined = value; originalArt.SetActive(!value); refinedArt.SetActive(value);
            rabbitArt.SetActive(value); originalPlayer.enabled = !value;
            foreach(var pair in originalMaterials) pair.Key.sharedMaterial = value ? atlasMaterial : pair.Value;
            if (!value) foreach(var pair in originalVisibility) pair.Key.enabled = pair.Value;
            foreach (var pair in rims) pair.Value.enabled = value;
            var specimenHud = canvas.transform.Find("SafeArea/QAHeader");
            if (specimenHud != null) specimenHud.gameObject.SetActive(value);
            foreach (var text in canvas.GetComponentsInChildren<Text>(true))
            {
                if (text.transform.IsChildOf(specimenHud)) continue;
                if (text.GetComponentInParent<DevMobileTouchControl>() != null || text.GetComponentInParent<Button>() != null) continue;
                if (text.transform.IsChildOf(canvas.transform.Find("SafeArea/ScreenHost"))) text.gameObject.SetActive(!value);
            }
            foreach (var control in canvas.GetComponentsInChildren<DevMobileTouchControl>(true))
            {
                var image = control.GetComponent<Image>();
                if (image != null) image.color = value ? new Color(.08f, .19f, .25f, MobileControlPreferences.Opacity) : new Color(.05f, .35f, .48f, MobileControlPreferences.Opacity);
                foreach (var label in control.GetComponentsInChildren<Text>(true)) { label.color = new Color32(222, 238, 221, 255); label.fontSize = 30; }
            }
            status = specimenHud?.Find("State")?.GetComponent<Text>();
            clockLabel = specimenHud?.Find("Clock")?.GetComponent<Text>();
            AlignArt();
        }

        private void AlignArt()
        {
            foreach (var device in runtimeDevices)
            {
                var art = device.GetComponent<AnimolOperationalArtBinding>()?.ArtRoot;
                if (art == null || !originalTransforms.TryGetValue(device, out var original)) continue;
                var renderers = art.GetComponentsInChildren<SpriteRenderer>(true);
                var surface = renderers.FirstOrDefault(r => r.enabled && (r.name == "light_bridge" || r.name == "left_plate" || r.name == "right_plate" || r.name.StartsWith("stair_") || r.name == "platform"));
                if (!refined) { art.transform.localPosition = original[0]; art.transform.localScale = original[1]; continue; }
                var ink = spriteInk.FirstOrDefault(x => x.sprite == surface?.sprite);
                if (ink == null || ink.bounds.width <= 0) continue;
                var box = device.GetComponent<BoxCollider2D>();
                var scale = box.size.x / ink.bounds.width;
                art.transform.localScale = new Vector3(scale, scale, 1);
                art.transform.localPosition = new Vector3(box.offset.x - ink.bounds.center.x * scale,
                    box.offset.y + box.size.y / 2f - ink.bounds.yMax * scale, 0);
                // The atlas base is a full assembly illustration. An independently moving plate must not carry it.
                if (device is MoonJadeBalanceObject)
                    foreach (var renderer in renderers.Where(r => r.name == "base")) renderer.enabled = false;
                if (device is MoonPhaseStairObject stair && stair.PreviewRenderer != null)
                {
                    var preview = stair.PreviewRenderer; preview.sprite = surface.sprite; preview.sharedMaterial = atlasMaterial;
                    preview.transform.localScale = art.transform.localScale;
                    var local = art.transform.localPosition;
                    preview.transform.localPosition = Quaternion.Euler(0,0,stair.IsVertical ? -90:90) * local;
                    preview.color = new Color(1f,.75f,.3f,.35f);
                }
            }
        }

        private void LateUpdate()
        {
            if (!refined) return;
            AlignArt();
            foreach (var lantern in runtimeDevices.OfType<MoonLanternStepObject>())
            {
                var root = lantern.GetComponent<AnimolOperationalArtBinding>()?.ArtRoot; if (root == null) continue;
                var color = lantern.State == LanternStepState.Warning ? new Color(1f,.78f,.4f) : lantern.State == LanternStepState.Recovering ? new Color(.65f,.75f,1f,.5f) : lantern.State == LanternStepState.Hidden ? new Color(1,1,1,.12f) : Color.white;
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>()) renderer.color = color;
            }
        }

        private void Bind(string name, UnityEngine.Events.UnityAction action)
        { var button = canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name); if (button != null) button.onClick.AddListener(action); }
        private void TogglePause() { if (pause == null) return; pause.SetActive(!pause.activeSelf); Time.timeScale = pause.activeSelf ? 0f : 1f; }
        private void OnDestroy() { Time.timeScale = 1f; }
        public void ResetSpecimen()
        {
            var p = DevPlayerController.Instance; if (p == null) return;
            if (p.MoveDirection != 0) p.ApplySwipe(-912, -p.MoveDirection);
            p.ReleaseJump();
            p.ClearTransientMapObjectState(); p.Body.position = Spawn; p.Body.linearVelocity = Vector2.zero;
            foreach (var device in runtimeDevices) device.ResetRuntimeState();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (Input.GetKeyDown(KeyCode.R)) ResetSpecimen();
            if (Input.GetKeyDown(KeyCode.B)) ApplyPresentation(!refined);
            var player = DevPlayerController.Instance;
            if (player != null && player.transform.position.y < -1.5f)
            { ResetSpecimen(); GameplayFeedback.Raise(FeedbackEventKind.Respawn, Spawn, "다시 시작", "Restart"); }
            if (clockLabel != null) clockLabel.text = $"연습  {elapsed:000}s     스태미나  {(player == null ? 100 : player.Stamina):0}";
            var lantern = runtimeDevices.OfType<MoonLanternStepObject>().FirstOrDefault();
            var stair = runtimeDevices.OfType<MoonPhaseStairObject>().FirstOrDefault();
            var balance = runtimeDevices.OfType<MoonJadeBalanceObject>().FirstOrDefault();
            if (status != null && lantern != null && stair != null)
                status.text = $"달빛 {LanternLabel(lantern.State)} · 저울 {(balance != null && balance.IsOccupied ? "하강" : balance != null && Vector2.Distance(balance.transform.position,balance.UpperPosition)>.025f ? "복귀" : "대기")} · 월상 {(stair.PreviewVisible ? "공간 대기" : stair.IsVertical ? "작동" : "수평")}";
            foreach (var pair in rims)
            {
                var collider = pair.Key.GetComponent<BoxCollider2D>(); var bounds = collider.bounds;
                if (!collider.enabled && !(pair.Key is MoonLanternStepObject recovering && recovering.State == LanternStepState.Recovering))
                { pair.Value.enabled = false; continue; }
                if (!collider.enabled) bounds = new Bounds(collider.transform.TransformPoint(collider.offset), collider.size);
                pair.Value.enabled = refined;
                var color = new Color32(183, 238, 213, 255);
                if (pair.Key is MoonLanternStepObject m)
                    color = m.State == LanternStepState.Warning ? new Color32(248, 191, 101, 255) : m.State == LanternStepState.Recovering ? new Color32(154, 173, 240, 255) : color;
                if (pair.Key is MoonPhaseStairObject s && s.PreviewVisible) color = new Color32(248, 191, 101, 255);
                if (pair.Key is MoonJadeBalanceObject b && !b.IsOccupied && Vector2.Distance(b.transform.position,b.UpperPosition)>.025f) color = new Color32(154,173,240,255);
                pair.Value.startColor = pair.Value.endColor = color;
                pair.Value.SetPosition(0, new Vector3(bounds.min.x, bounds.max.y + .015625f, 0));
                pair.Value.SetPosition(1, new Vector3(bounds.max.x, bounds.max.y + .015625f, 0));
            }
        }

        private static string LanternLabel(LanternStepState state) => state switch
        { LanternStepState.Warning => "소멸 예고", LanternStepState.Hidden => "숨김", LanternStepState.Recovering => "복귀 중", _ => "밟기 가능" };

        // Captures the actual Game View at end of frame, including ScreenSpaceOverlay UI.
        // Only invoked explicitly by the QA editor menu; ordinary play never records or drives input.
        public void CaptureEvidence(string directory, bool record) => StartCoroutine(CaptureSequence(directory, record));
        private IEnumerator CaptureSequence(string directory, bool record)
        {
            Directory.CreateDirectory(directory); ResetSpecimen();
            yield return new WaitForSeconds(.5f);
            ApplyPresentation(false); yield return new WaitForSeconds(.2f);
            yield return SaveFrame(Path.Combine(directory, $"before_{Screen.width}x{Screen.height}.png"));
            ApplyPresentation(true); yield return new WaitForSeconds(.2f);
            yield return SaveFrame(Path.Combine(directory, $"after_{Screen.width}x{Screen.height}.png"));
            if (record)
            {
                Directory.CreateDirectory(Path.Combine(directory, "frames"));
                var player = DevPlayerController.Instance;
                // A short real physics/input replay, not a transform animation or mocked gameplay video.
                var replay = StartCoroutine(ReplayInputs());
                var start = Time.realtimeSinceStartup; var index = 0; var next = 0f;
                while (Time.realtimeSinceStartup - start < 10f)
                {
                    var time = Time.realtimeSinceStartup - start;
                    if (time >= next) { yield return SaveFrame(Path.Combine(directory, "frames", $"play_{index++:0000}.png")); next += .1f; }
                    yield return null;
                }
                StopCoroutine(replay);
                File.WriteAllText(Path.Combine(directory, "recording.json"), JsonUtility.ToJson(new RecordingInfo { frames = index, seconds = Time.realtimeSinceStartup - start, width = Screen.width, height = Screen.height }, true));
                yield return CaptureStates(directory);
            }
            ResetSpecimen();
            File.WriteAllText(Path.Combine(directory, $"capture_{Screen.width}x{Screen.height}.complete"), "Actual Game View / ScreenCapture at WaitForEndOfFrame / timeScale unchanged by capture");
        }
        private static IEnumerator ReplayInputs()
        {
            var player = DevPlayerController.Instance;
            yield return new WaitForSeconds(.4f);
            foreach(var target in new[]{-3.5f,-2.5f,-1.5f,-.5f})
            {
                if (Mathf.Approximately(target,-2.5f))
                {
                    var lantern=UnityEngine.Object.FindFirstObjectByType<MoonLanternStepObject>();
                    while(lantern.State!=LanternStepState.Solid) yield return null;
                }
                player.RequestJump(); var time=0f;bool airborne=false;
                while(time<1.6f)
                {
                    var error=target-player.Body.position.x;
                    if(Mathf.Abs(error)>.24f && player.MoveDirection==0) player.ApplySwipe(-910,Mathf.Sign(error));
                    if(Mathf.Abs(error)<=.24f && player.MoveDirection!=0) player.ApplySwipe(-910,-player.MoveDirection);
                    if(!player.IsGrounded) airborne=true;
                    if(airborne && player.IsGrounded && time>.25f) break;
                    time+=Time.deltaTime;yield return null;
                }
                player.ReleaseJump();if(player.MoveDirection!=0)player.ApplySwipe(-910,-player.MoveDirection);
                yield return new WaitForSeconds(.55f);
            }
        }
        private IEnumerator CaptureStates(string directory)
        {
            // Controlled placements exercise actual production state machines; this is separate from the input-only replay.
            var player = DevPlayerController.Instance;
            var lantern = runtimeDevices.OfType<MoonLanternStepObject>().Single();
            ResetSpecimen(); yield return new WaitForSeconds(.2f);
            yield return SaveFrame(Path.Combine(directory,"state_M1_solid.png"));
            lantern.NotifyOccupied(false); yield return new WaitForFixedUpdate();
            yield return SaveFrame(Path.Combine(directory,"state_M1_warning.png"));
            yield return new WaitForSeconds(lantern.Settings.WarningSeconds + .05f);
            yield return SaveFrame(Path.Combine(directory,"state_M1_hidden.png"));
            yield return new WaitForSeconds(lantern.Settings.ActiveSeconds + .05f);
            yield return SaveFrame(Path.Combine(directory,"state_M1_recover.png"));
            var balance=runtimeDevices.OfType<MoonJadeBalanceObject>().First();
            balance.SetOccupiedForTest(true); yield return new WaitForSeconds(.2f);
            yield return SaveFrame(Path.Combine(directory,"state_M2_active.png"));
            balance.SetOccupiedForTest(false);yield return new WaitForSeconds(.08f);
            yield return SaveFrame(Path.Combine(directory,"state_M2_recover.png"));
            var stair=runtimeDevices.OfType<MoonPhaseStairObject>().Single();
            stair.SetOccupiedForTest(true);stair.RequestToggle();yield return new WaitForFixedUpdate();
            yield return SaveFrame(Path.Combine(directory,"state_M6_warning.png"));
            stair.SetOccupiedForTest(false);yield return new WaitForFixedUpdate();
            if (!stair.IsVertical) throw new InvalidOperationException("QA rotation blocked: " + stair.SweptSpaceBlockerName);
            yield return SaveFrame(Path.Combine(directory,"state_M6_active.png"));
            stair.ReleasePatternPlate();stair.RequestToggle();yield return new WaitForFixedUpdate();
            if (stair.IsVertical) throw new InvalidOperationException("QA return blocked: " + stair.SweptSpaceBlockerName);
            yield return SaveFrame(Path.Combine(directory,"state_M6_recover.png"));
            File.WriteAllText(Path.Combine(directory,"state-scenarios.txt"),"Controlled QA occupancy injection through existing state-machine APIs. Not input-only gameplay. M1: Solid/Warning/Hidden/Recovering. M2: occupied movement/empty return. M6: occupied deferral/empty turn/return. Physics, timings and occupancy rules unchanged.");
        }
        [Serializable] private sealed class RecordingInfo { public int frames, width, height; public float seconds; }
        private static IEnumerator SaveFrame(string path)
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, image.EncodeToPNG()); Destroy(image);
        }
    }
}
