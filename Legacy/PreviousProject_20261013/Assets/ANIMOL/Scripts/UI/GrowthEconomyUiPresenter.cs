using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI
{
    /// <summary>
    /// Presents the M6 growth/economy contracts without pretending that account,
    /// commerce, rewarded-ad, or cloud-save services are connected.
    /// </summary>
    public sealed class GrowthEconomyUiPresenter : MonoBehaviour
    {
        [SerializeField] private AccountUpgradeCatalog accountUpgradeCatalog;
        [SerializeField] private GrowthEconomyPolicyCatalog policyCatalog;
        [SerializeField] private ExternalServiceConfiguration services;

        private readonly List<(Button button, UnityEngine.Events.UnityAction action)> bindings = new List<(Button, UnityEngine.Events.UnityAction)>();
        private UiNavigationService navigation;
        private UiModalStack modals;
        private AccountAccessSnapshot access = AccountAccessSnapshot.GuestDisconnected;
        private SaveConflictSelectionService saveSelection;
        private DevRewardLedgerAdapter devLedger;
        private RewardedAdGrantCoordinator devCoordinator;
        private RewardGrantStatus lastDevStatus = RewardGrantStatus.Unavailable;
        private int requestSequence;

        public AccountIdentityState IdentityState => access.IdentityState;
        public int? DevVerifiedBalance => devCoordinator?.VerifiedCoinBalance;
        public int? DevRemainingAdCount => devCoordinator?.Quota.RemainingCount;
        public RewardGrantStatus LastDevStatus => lastDevStatus;
        public int DevHistoryCount => devCoordinator?.History.Count ?? 0;

        private void OnEnable()
        {
            navigation = GetComponent<UiNavigationService>();
            modals = GetComponent<UiModalStack>();
            access = AccountAccessSnapshot.GuestDisconnected;
            if (navigation != null) navigation.ScreenChanged += OnScreenChanged;

            Bind("AccountLinkButton", () => { RefreshAccountAndSave(); navigation.Navigate("SC20_AccountAndSave"); });
            Bind("GuestCampaignButton", () => navigation.Navigate("SC02_ThemeSelect"));
            Bind("GoogleLinkButton", ShowGoogleSdkUnavailable);
            Bind("AccountSaveBackButton", () => navigation.Back());
            Bind("SaveConflictDevPreviewButton", BeginSaveConflictPreview);
            Bind("SelectLocalSaveButton", () => SelectSave(SaveRecordSource.Local));
            Bind("SelectCloudSaveButton", () => SelectSave(SaveRecordSource.Cloud));
            Bind("EconomyDevPreviewButton", OpenEconomyDevPreview);
            Bind("EconomyDevBackButton", () => navigation.Back());
            Bind("DevResultApproveButton", ApproveResultReward);
            Bind("DevDuplicateButton", TryDuplicateResultReward);
            Bind("DevAdFailButton", () => CompleteProviderOutcome(RewardGrantStatus.Failed));
            Bind("DevAdCancelButton", () => CompleteProviderOutcome(RewardGrantStatus.Cancelled));
            Bind("DevConcurrentTapButton", TryConcurrentTap);
            Bind("DevGeneralApproveButton", ApproveGeneralReward);

            RefreshAllContractLabels();
            RefreshAccountAndSave();
            RefreshDevEconomy();
        }

        private void OnDisable()
        {
            if (navigation != null) navigation.ScreenChanged -= OnScreenChanged;
            foreach (var binding in bindings)
                if (binding.button != null) binding.button.onClick.RemoveListener(binding.action);
            bindings.Clear();
        }

        private void OnScreenChanged(string screenId)
        {
            if (screenId == "SC10A_AccountUpgrade" || screenId == "SC10B_CharacterUpgrade") RefreshAllContractLabels();
            else if (screenId == "SC11_Store" || screenId == "SC12_EmoteCollection") RefreshStoreAndEmotes();
            else if (screenId == "SC20_AccountAndSave") RefreshAccountAndSave();
            else if (screenId == "SC21_EconomyDevPreview") RefreshDevEconomy();
        }

        private void RefreshAllContractLabels()
        {
            SetText("LobbyAccountStatus", "게스트 · 오프라인 캠페인 가능 · 정식 온라인 코인/구매는 Google 연동 및 서버 필요");
            SetText("AccountPrototypeNotice", accountUpgradeCatalog == null
                ? "계정 공통 성장 데이터 없음"
                : $"프로토타입 값 · {accountUpgradeCatalog.PrototypeNotice} · 적용/구매 잠김");

            if (accountUpgradeCatalog != null)
            {
                for (var i = 0; i < 3; i++)
                {
                    var button = FindByName<Button>($"AccountTrack_{i + 1:00}");
                    if (button == null) continue;
                    if (i < accountUpgradeCatalog.Tracks.Count)
                    {
                        var track = accountUpgradeCatalog.Tracks[i];
                        var delta = track.TotalPercentDelta;
                        SetButtonText(button, $"{track.DisplayName}\nLv.0/{track.MaxLevel} · {(delta >= 0f ? "+" : string.Empty)}{delta:0.#}% (프로토타입)\n서버 미연결 · 적용 잠김");
                    }
                    button.interactable = false;
                }
            }

            RefreshAnimalGrowth();
            RefreshStoreAndEmotes();
            SetText("RewardedAdPolicy", "두 선택형 광고가 계정 공용 일일 3회 사용 · 성공 지급 확정 때만 1회 소진");
            SetText("RewardedAdServerState", "계정/광고 SDK 미연결 · 잔여 횟수와 초기화 시각 조회 불가 · 실제 지급 Blocked");
            var watch = FindByName<Button>("RewardedAdWatchButton");
            if (watch != null) watch.interactable = false;
        }

        private void RefreshAnimalGrowth()
        {
            var entries = policyCatalog == null ? Array.Empty<AnimalGrowthContract>() : policyCatalog.AnimalGrowth.ToArray();
            for (var i = 0; i < 3; i++)
            {
                var button = FindByName<Button>($"AnimalGrowthCard_{i + 1:00}") ?? FindByName<Button>($"CharacterCard_{i + 1:00}");
                if (button == null) continue;
                if (i >= entries.Length)
                {
                    SetButtonText(button, "동물 성장 데이터 없음");
                    button.interactable = false;
                    continue;
                }
                var entry = entries[i];
                var unlock = string.IsNullOrWhiteSpace(entry.RequiredCompletedStageId) ? "초기 해금" : $"캠페인 {entry.RequiredCompletedStageId} 완료 시 해금";
                SetButtonText(button, $"{entry.AnimalId}\n액티브 Lv.0/5 · 패시브 Lv.0/5\n{unlock}\n효과/숙련도 획득량/가격 미설정 · 잠김");
                button.interactable = false;
            }
            SetText("CharacterUpgradeState", "공유 코인 + 동물별 숙련도 계약 · 실제 효과/획득량/가격 미설정 · 강화 적용 및 구매 잠김");
            var purchase = FindByName<Button>("CharacterPurchaseButton");
            if (purchase != null) purchase.interactable = false;
        }

        private void RefreshStoreAndEmotes()
        {
            SetText("GrowthPackPolicy", "성장 완료 팩: 계정 공통 3트랙만 포함 · 동물 성장/해금 제외");
            SetText("CommerceSupplyState", "사용 코인 코스메틱 교환 재화 보전 산식 미정 · 인증 공급 가격/보유 상태 없음 · 실판매 Blocked");
            SetText("EmoteAcquisitionPolicy", "무료 / 코인 교환 / 유료 이모티콘 분리 · 운영 가격/보유 상태는 인증된 공급 데이터만 사용");
            foreach (var name in new[] { "CoinEmoteOfferButton", "PaidEmoteOfferButton", "ProductPurchaseButton" })
            {
                var button = FindByName<Button>(name);
                if (button != null) button.interactable = false;
            }
        }

        private void RefreshAccountAndSave()
        {
            SetText("AccountIdentityState", "현재: 게스트 · 로그인 성공 상태를 연출하지 않음");
            SetText("AccountGateState", "게스트 캠페인 시작 가능 · 온라인/구매는 Google 연동 + 계정 서버 + 결제 SDK 전까지 잠김");
            SetText("CoinSeparationState", "오프라인 로컬 기록과 검증된 정식 온라인 코인은 분리 · 로컬 코인을 실코인으로 표시하지 않음");
            SetText("LocalRecordState", saveSelection == null
                ? "로컬 기록: 기기 저장 · 캠페인 진행 0 · 최고 시간 0"
                : FormatRecord(saveSelection.Local));
            SetText("CloudRecordState", saveSelection == null
                ? "클라우드 기록: 계정 서버 미연결 · 조회 불가"
                : FormatRecord(saveSelection.Cloud));
            SetText("SaveSelectionState", saveSelection?.SelectedSource == null
                ? "저장 선택: 없음 · 실제 동기화/덮어쓰기 없음"
                : $"저장 선택: {saveSelection.SelectedSource} · DEV ONLY 미리보기, 실제 동기화 없음");
            var cloud = FindByName<Button>("SelectCloudSaveButton");
            if (cloud != null) cloud.interactable = saveSelection?.Cloud.IsAvailable == true;
            var local = FindByName<Button>("SelectLocalSaveButton");
            if (local != null) local.interactable = saveSelection?.Local.IsAvailable == true;
        }

        private void BeginSaveConflictPreview()
        {
            saveSelection = new SaveConflictSelectionService(
                new CampaignRecordSummary(SaveRecordSource.Local, true, false, 4, 3, 7),
                new CampaignRecordSummary(SaveRecordSource.Cloud, true, true, 6, 5, 9), true);
            RefreshAccountAndSave();
        }

        private void SelectSave(SaveRecordSource source)
        {
            if (saveSelection != null) saveSelection.TrySelect(source, true);
            RefreshAccountAndSave();
        }

        private static string FormatRecord(CampaignRecordSummary record) =>
            $"{(record.Source == SaveRecordSource.Local ? "로컬" : "클라우드")} 기록: 진행 {record.CompletedStages} · 최고 시간 {record.BestTimeCount} · rev.{record.Revision}" +
            (record.IsAuthoritative ? " · 서버 검증" : " · 로컬 전용");

        private void ShowGoogleSdkUnavailable()
        {
            SetText("ServiceErrorTitle", "Google 계정 SDK 미연결");
            SetText("ServiceErrorMessage", "로그인 성공 상태를 만들지 않았습니다. 온라인·구매·클라우드·정식 코인은 계속 잠깁니다.");
            modals?.Push("ServiceErrorModal");
        }

        private void OpenEconomyDevPreview()
        {
            devLedger = new DevRewardLedgerAdapter(0, policyCatalog == null ? 3 : policyCatalog.SharedRewardedAdDailyLimit, "2030-01-02T00:00:00Z");
            devCoordinator = new RewardedAdGrantCoordinator(devLedger);
            lastDevStatus = RewardGrantStatus.Unavailable;
            requestSequence = 0;
            RefreshDevEconomy();
            navigation.Navigate("SC21_EconomyDevPreview");
        }

        private RewardedAdGrantRequest Request(string resultId, RewardedAdRewardType type, int amount, string suffix) =>
            new RewardedAdGrantRequest("DEV-ACCOUNT", resultId, type, "DEV-AD-PROOF-" + suffix, "DEV-GRANT-" + suffix + "-" + (++requestSequence), amount);

        private void ApproveResultReward()
        {
            var request = Request("DEV-RESULT-001", RewardedAdRewardType.CampaignBaseCoinDouble, 100, "RESULT");
            lastDevStatus = devCoordinator.Begin(request);
            if (lastDevStatus == RewardGrantStatus.Pending) lastDevStatus = devCoordinator.Complete(request.GrantRequestId, RewardGrantStatus.Approved);
            RefreshDevEconomy();
        }

        private void TryDuplicateResultReward()
        {
            lastDevStatus = devCoordinator.Begin(Request("DEV-RESULT-001", RewardedAdRewardType.CampaignBaseCoinDouble, 100, "DUP"));
            RefreshDevEconomy();
        }

        private void CompleteProviderOutcome(RewardGrantStatus outcome)
        {
            var request = Request("DEV-GENERAL-" + requestSequence, RewardedAdRewardType.LobbyGeneralCoinDraft, policyCatalog == null ? 150 : policyCatalog.LobbyGeneralCoinDraft, outcome.ToString());
            lastDevStatus = devCoordinator.Begin(request);
            if (lastDevStatus == RewardGrantStatus.Pending) lastDevStatus = devCoordinator.Complete(request.GrantRequestId, outcome);
            RefreshDevEconomy();
        }

        private void TryConcurrentTap()
        {
            var first = Request("DEV-CONCURRENT", RewardedAdRewardType.LobbyGeneralCoinDraft, 150, "CONCURRENT-A");
            var second = Request("DEV-CONCURRENT", RewardedAdRewardType.LobbyGeneralCoinDraft, 150, "CONCURRENT-B");
            var firstStatus = devCoordinator.Begin(first);
            lastDevStatus = firstStatus == RewardGrantStatus.Pending ? devCoordinator.Begin(second) : firstStatus;
            if (firstStatus == RewardGrantStatus.Pending) devCoordinator.Complete(first.GrantRequestId, RewardGrantStatus.Cancelled);
            RefreshDevEconomy();
        }

        private void ApproveGeneralReward()
        {
            var amount = policyCatalog == null ? 150 : policyCatalog.LobbyGeneralCoinDraft;
            var request = Request("DEV-GENERAL-APPROVE-" + requestSequence, RewardedAdRewardType.LobbyGeneralCoinDraft, amount, "GENERAL");
            lastDevStatus = devCoordinator.Begin(request);
            if (lastDevStatus == RewardGrantStatus.Pending) lastDevStatus = devCoordinator.Complete(request.GrantRequestId, RewardGrantStatus.Approved);
            RefreshDevEconomy();
        }

        private void RefreshDevEconomy()
        {
            var breakdown = new CampaignCoinRewardBreakdown(100, 25, 10, true);
            SetText("DevRewardBreakdown", $"DEV ONLY · 기본 B {breakdown.BaseCoinB} / 시간 T {breakdown.TimeBonusT} / 부가 O {breakdown.OptionalBonusO} / 광고 추가 B {breakdown.AdAdditionalBaseB} / 총액 2B+T+O {breakdown.TotalWithApprovedAd}");
            if (devCoordinator == null)
            {
                SetText("DevLedgerState", "DEV 어댑터 대기 · 운영 지급과 분리");
                SetText("DevGrantHistory", "내역 없음");
                return;
            }
            var quota = devCoordinator.Quota;
            SetText("DevLedgerState", $"DEV ONLY 검증 잔액 {devCoordinator.VerifiedCoinBalance.GetValueOrDefault()} · 오늘 남은 {quota.RemainingCount.GetValueOrDefault()}/{quota.Limit} · 초기화 {quota.ResetAtServerIso8601} · 최근 {lastDevStatus}");
            SetText("DevGrantHistory", devCoordinator.History.Count == 0
                ? "내역 없음"
                : "내역: " + string.Join(" | ", devCoordinator.History.Select(x => $"{x.Status}:{x.GrantedCoins}")));
        }

        private void Bind(string name, UnityEngine.Events.UnityAction action)
        {
            var button = FindByName<Button>(name);
            if (button == null) return;
            button.onClick.AddListener(action);
            bindings.Add((button, action));
        }

        private T FindByName<T>(string name) where T : Component =>
            GetComponentsInChildren<T>(true).FirstOrDefault(x => x.name == name);

        private void SetText(string name, string value)
        {
            var label = FindByName<Text>(name);
            if (label != null) label.text = value;
        }

        private static void SetButtonText(Button button, string value)
        {
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = value;
        }
    }
}
