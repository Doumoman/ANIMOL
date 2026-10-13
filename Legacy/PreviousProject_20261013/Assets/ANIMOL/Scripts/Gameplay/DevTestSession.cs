using System;
using ANIMOL.Core;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Gameplay
{
    public sealed class DevTestSession : MonoBehaviour
    {
        [SerializeField] private DevTestRunDefinition definition;
        [SerializeField] private Text bubbleLabel;
        [SerializeField] private Text gateLabel;
        [SerializeField] private Text timeLabel;
        [SerializeField] private Text staminaLabel;
        [SerializeField] private Text animalLabel;
        [SerializeField] private Text abilityLabel;
        [SerializeField] private Text checkpointLabel;
        [SerializeField] private DevExitGateView exitGateView;

        private RunSessionController run;
        private BubbleObjectiveService objectives;
        private ExitGateController gate;
        private float elapsed;
        private CampaignRunTimer timer;
        private CheckpointRespawnState checkpoints;
        private Vector3 respawnPosition;
        private bool hasRespawnPosition;
        private bool resultCommitted;

        public int CollectedCount => objectives?.CollectedCount ?? 0;
        public int TargetCount => objectives?.TargetCount ?? 0;
        public bool IsExitOpen => gate?.IsOpen ?? false;
        public RunSessionState State => run?.State ?? RunSessionState.Idle;
        public DevTestRunDefinition Definition => definition;
        public float RemainingSeconds => timer?.RemainingSeconds ?? 0f;
        public string ActiveCheckpointId => checkpoints?.ActiveCheckpointId ?? string.Empty;
        public RetryLayoutIdentity LayoutIdentity => definition == null ? default : definition.LayoutIdentity;

        public void ConfigureDefinition(DevTestRunDefinition value) => definition = value;

        private void Awake()
        {
            if (definition == null) throw new InvalidOperationException("DEV-TEST-01 definition is missing.");
            DevRunResultState.Clear();
            run = new RunSessionController();
            run.BeginLoading(Guid.NewGuid().ToString("N"), definition.StageId, true);
            objectives = new BubbleObjectiveService(definition.TargetBubbleCount);
            gate = new ExitGateController(run, objectives);
            timer = new CampaignRunTimer(definition.TimeLimitSeconds);
            checkpoints = new CheckpointRespawnState(definition.CheckpointIds, definition.CheckpointIds[0]);
            objectives.ProgressChanged += OnProgressChanged;
            objectives.ExitEligible += OnExitEligible;
            run.BeginCountdown();
            run.BeginPlaying();
            OnProgressChanged(0, definition.TargetBubbleCount);
            UpdateGateLabel();
        }

        private void Update()
        {
            if (!hasRespawnPosition && DevPlayerController.Instance != null)
            {
                respawnPosition = DevPlayerController.Instance.transform.position;
                hasRespawnPosition = true;
            }
            if (run != null && (run.State == RunSessionState.Playing || run.State == RunSessionState.ExitEligible))
            {
                elapsed += Time.deltaTime;
                if (timer.Advance(Time.deltaTime)) Complete(CampaignRunOutcome.TimeExpired);
            }
            if (timeLabel != null) timeLabel.text = MobileControlPreferences.Localize(
                $"남은 시간 {RemainingSeconds:0.0}s · 경과 {elapsed:0.0}s",
                $"Time left {RemainingSeconds:0.0}s · elapsed {elapsed:0.0}s");
            var player = DevPlayerController.Instance;
            if (player != null)
            {
                if (staminaLabel != null) staminaLabel.text = MobileControlPreferences.Localize($"공용 스테미나 {player.Stamina:0}/100", $"Shared stamina {player.Stamina:0}/100");
                if (animalLabel != null) animalLabel.text = MobileControlPreferences.Localize(
                    $"현재 {player.CurrentAnimalId} · 인접 {(player.CurrentAnimalId == "DEV_GROUND" ? "DEV_GLIDER" : "DEV_GROUND")}",
                    $"Current {player.CurrentAnimalId} · adjacent {(player.CurrentAnimalId == "DEV_GROUND" ? "DEV_GLIDER" : "DEV_GROUND")}");
            }
        }

        public bool TryCollect(string slotId, string ownerId)
        {
            if (run == null || run.State != RunSessionState.Playing) return false;
            return objectives.TryCollect(run.RunId, definition.MapInstanceId, slotId, ownerId);
        }

        public bool TryEnterExit()
        {
            if (!gate.TryEnter()) return false;
            Complete(CampaignRunOutcome.Cleared);
            return true;
        }

        public bool ActivateCheckpoint(string checkpointId, Vector3 position)
        {
            if (checkpoints == null || !checkpoints.TryActivate(checkpointId)) return false;
            respawnPosition = position;
            hasRespawnPosition = true;
            if (checkpointLabel != null) checkpointLabel.text = MobileControlPreferences.Localize($"체크포인트 ✓ {checkpointId}", $"Checkpoint ✓ {checkpointId}");
            return true;
        }

        public bool RespawnAtLastCheckpoint()
        {
            var player = DevPlayerController.Instance;
            if (player == null || !hasRespawnPosition) return false;
            var body = player.GetComponent<Rigidbody2D>();
            body.position = respawnPosition;
            body.linearVelocity = Vector2.zero;
            return true;
        }

        public void ReportUnavailableAbility(string inputName)
        {
            if (abilityLabel != null) abilityLabel.text = MobileControlPreferences.Localize($"{inputName}: 코어 미연결 · 실행 안 함", $"{inputName}: core unavailable · not executed");
        }

        public void ForceTimeExpiredForTest() => Complete(CampaignRunOutcome.TimeExpired);

        private void Complete(CampaignRunOutcome outcome)
        {
            if (resultCommitted || run == null) return;
            resultCommitted = true;
            if (outcome == CampaignRunOutcome.TimeExpired) run.MarkTimeExpired();
            DevRunResultState.Record(definition.StageId, elapsed, objectives.CollectedCount, outcome, definition.LayoutIdentity);
            run.ShowResult();
            SceneManager.LoadScene("Results");
        }

        private void OnProgressChanged(int collected, int target)
        {
            if (bubbleLabel != null) bubbleLabel.text = MobileControlPreferences.Localize($"방울 {collected}/{target} · 후보 4", $"Bubbles {collected}/{target} · candidates 4");
        }

        private void OnExitEligible()
        {
            UpdateGateLabel();
            if (exitGateView != null) exitGateView.SetOpenVisual(true);
        }

        private void UpdateGateLabel()
        {
            if (gateLabel != null) gateLabel.text = IsExitOpen
                ? MobileControlPreferences.Localize("출구 열림 ✓ · 출구 방향만 안내", "Exit open ✓ · exit direction only")
                : MobileControlPreferences.Localize("출구 잠김 ✕ · 미발견 방울 위치 비공개", "Exit locked ✕ · undiscovered bubbles hidden");
        }
    }
}
