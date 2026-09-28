using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.UI
{
    public sealed class CampaignUiPresenter : MonoBehaviour
    {
        [SerializeField] private CampaignCatalog catalog;
        private UiNavigationService navigation;
        private UiModalStack modals;
        private ToastAndErrorPresenter toast;
        private readonly List<(Button button, UnityEngine.Events.UnityAction action)> bindings = new List<(Button, UnityEngine.Events.UnityAction)>();
        private ThemeDefinition selectedTheme;
        private CampaignStageDefinition selectedStage;
        private CampaignProgressionService progression;

        private void OnEnable()
        {
            navigation = GetComponent<UiNavigationService>();
            modals = GetComponent<UiModalStack>();
            toast = GetComponentInChildren<ToastAndErrorPresenter>(true);
            if (catalog != null) progression = new CampaignProgressionService(catalog);
            Bind("CampaignButton", () => navigation.Navigate("SC02_ThemeSelect"));
            Bind("SettingsButton", () => navigation.Navigate("SC13_Settings"));
            Bind("HelpButton", () => navigation.Navigate("SC14_Help"));
            Bind("ExitButton", () => modals.Push("ConfirmExitModal"));
            Bind("ExitCancelButton", () => modals.Pop());
            Bind("ExitConfirmButton", () => ShowToast("모바일 빌드에서 앱 종료 확인으로 연결됩니다."));
            Bind("ThemeBackButton", () => navigation.Back());
            Bind("StageBackButton", () => navigation.Back());
            Bind("DetailBackButton", () => navigation.Back());
            Bind("SettingsBackButton", () => navigation.Back());
            Bind("SettingsResetButton", () => ShowToast("설정 기본값을 복원했습니다."));
            Bind("HelpBackButton", () => navigation.Back());
            Bind("LoadingBackButton", () => navigation.Navigate("SC01_Lobby", false));
            Bind("MilestoneLobbyButton", () => navigation.Navigate("SC01_Lobby", false));
            Bind("DevTestButton", LaunchDevelopmentRun);
            BindThemeCards();
            BindStageCards();
            RefreshThemeCards();
        }

        private void OnDisable()
        {
            foreach (var binding in bindings) if (binding.button != null) binding.button.onClick.RemoveListener(binding.action);
            bindings.Clear();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (modals != null && modals.Pop()) return;
            if (navigation != null && navigation.Back()) return;
            modals?.Push("ConfirmExitModal");
        }

        private void BindThemeCards()
        {
            for (var i = 1; i <= 5; i++)
            {
                var index = i - 1;
                Bind($"ThemeCard_{i:00}", () => SelectTheme(index));
            }
        }

        private void BindStageCards()
        {
            for (var i = 1; i <= 20; i++)
            {
                var index = i - 1;
                Bind($"StageCard_{i:00}", () => SelectStage(index));
            }
        }

        private void SelectTheme(int index)
        {
            if (catalog == null || index < 0 || index >= catalog.Themes.Count) return;
            selectedTheme = catalog.Themes[index];
            RefreshStageCards();
            navigation.Navigate("SC03_StageSelect");
        }

        private void SelectStage(int index)
        {
            if (selectedTheme == null || index < 0 || index >= selectedTheme.Stages.Count) return;
            selectedStage = selectedTheme.Stages[index];
            RefreshStageDetail();
            navigation.Navigate("SC04_StageDetail");
        }

        private void RefreshThemeCards()
        {
            if (catalog == null) return;
            for (var i = 0; i < catalog.Themes.Count; i++)
            {
                var button = FindByName<Button>($"ThemeCard_{i + 1:00}");
                var label = button == null ? null : button.GetComponentInChildren<Text>(true);
                var firstStage = catalog.Themes[i].Stages.Count > 0 ? catalog.Themes[i].Stages[0] : null;
                var unlocked = progression != null && progression.IsUnlocked(firstStage);
                if (label != null) label.text = $"{catalog.Themes[i].DisplayName}\n0/20 · {(unlocked ? "열림 ○ / 제작 중" : "잠김 ✕ / 이전 테마 필요")}";
            }
        }

        private void RefreshStageCards()
        {
            if (selectedTheme == null) return;
            SetText("StageThemeTitle", $"{selectedTheme.DisplayName} · 20개 스테이지");
            for (var i = 0; i < selectedTheme.Stages.Count; i++)
            {
                var stage = selectedTheme.Stages[i];
                var button = FindByName<Button>($"StageCard_{i + 1:00}");
                var label = button == null ? null : button.GetComponentInChildren<Text>(true);
                var unlocked = progression != null && progression.IsUnlocked(stage);
                var completed = progression != null && progression.Progress.CompletedStageIds.Contains(stage.StageId);
                var state = completed ? "클리어 ✓" : unlocked ? "열림 ○ · 제작 중" : "잠김 ✕";
                if (label != null) label.text = $"{i + 1:00}\n{state}";
            }
        }

        private void RefreshStageDetail()
        {
            if (selectedStage == null) return;
            var availability = ContentAvailabilityResolver.Resolve(selectedStage);
            SetText("StageDetailTitle", selectedStage.StageId);
            SetText("StageObjectiveText", "목표: 방울 3개 획득 → 출구 개방 → 실제 출구 도착");
            SetText("FixedAnimalsText", selectedStage.FixedAllowedAnimalIds.Count == 0
                ? "고정 사용 동물: 미설정 (읽기 전용)"
                : $"고정 사용 동물: {string.Join(", ", selectedStage.FixedAllowedAnimalIds)}\n시작 동물: {selectedStage.InitialAnimalId}");
            SetText("AvailabilityText", availability == ContentAvailability.Ready ? "준비 완료" : "제작 중: 맵/고정 동물/출구 검증 필요");
            var policy = selectedStage.RuntimePolicy;
            SetText("RuntimePolicyText", policy == null
                ? "런 규칙: 제한시간·체크포인트·동일 배치 데이터 미설정"
                : $"제한시간 {policy.TimeLimitSeconds:0.#}s · 체크포인트 {policy.CheckpointIds.Count} · 재도전 동일 배치");
            SetText("OptionalObjectiveText", policy == null || policy.OptionalObjectives.Count == 0
                ? "선택 목표: 이 맵에는 설정 없음"
                : $"선택 목표 {policy.OptionalObjectives.Count}개 · 보상은 데이터/서버 승인값");
            SetText("TutorialPolicyText", "초반 맵 플레이로 학습 · 입장 필수 팝업 없음");
            var best = 0f;
            var hasBest = progression != null && progression.Progress.BestTimes.TryGetValue(selectedStage.StageId, out best);
            SetText("BestTimeText", hasBest ? $"클리어 ✓ · 최고 시간 {best:0.00}s" : "클리어/최고 시간: 기록 없음");
            var start = FindByName<Button>("StageStartButton");
            if (start != null) start.interactable = availability == ContentAvailability.Ready && progression != null && progression.IsUnlocked(selectedStage);
        }

        private void LaunchDevelopmentRun() => SceneManager.LoadScene("Gameplay");

        private void Bind(string name, UnityEngine.Events.UnityAction action)
        {
            var button = FindByName<Button>(name);
            if (button == null) return;
            button.onClick.AddListener(action);
            bindings.Add((button, action));
        }

        private T FindByName<T>(string name) where T : Component
        {
            foreach (var component in GetComponentsInChildren<T>(true)) if (component.name == name) return component;
            return null;
        }

        private void SetText(string name, string value)
        {
            var label = FindByName<Text>(name);
            if (label != null) label.text = value;
        }

        private void ShowToast(string message)
        {
            if (toast != null) toast.Show(message);
        }
    }
}
