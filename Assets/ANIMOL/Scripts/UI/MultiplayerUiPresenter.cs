using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI
{
    public sealed class MultiplayerUiPresenter : MonoBehaviour
    {
        [SerializeField] private MultiplayerPreviewDefinition competitivePreview;
        [SerializeField] private MultiplayerPreviewDefinition coopPreview;
        [SerializeField] private ExternalServiceConfiguration services;

        private static readonly CampaignAnimalUnlockRule[] DevelopmentAnimalRules =
        {
            new CampaignAnimalUnlockRule("DEV_GROUND", MultiplayerAnimalRole.Ground, string.Empty),
            new CampaignAnimalUnlockRule("DEV_GLIDER", MultiplayerAnimalRole.Air, "T01-S01"),
            new CampaignAnimalUnlockRule("DEV_SPECIAL", MultiplayerAnimalRole.Special, "T01-S02")
        };

        private readonly List<(Button button, UnityEngine.Events.UnityAction action)> bindings = new List<(Button, UnityEngine.Events.UnityAction)>();
        private UiNavigationService navigation;
        private UiModalStack modals;
        private MultiplayerPreviewDefinition currentPreview;
        private MultiplayerLoadoutService coopLoadout;
        private CompetitiveParticipantSelectionService competitiveSelection;
        private MultiplayerRoomPreviewState roomPreview;
        private CompetitiveSeriesState competitiveSeries;
        private CompetitiveBubbleAuthorityState bubbleAuthority;
        private CompetitiveFinishWindowState finishWindow;
        private int activeSlot;
        private int connectionPreviewStep;
        private bool rankedPreview;

        public int PreviewParticipantCount => roomPreview?.Participants.Count ?? 0;
        public bool IsRankedPreview => rankedPreview;
        public bool OperationalReadyEnabled => services != null && services.MatchServerConnected;

        private void OnEnable()
        {
            navigation = GetComponent<UiNavigationService>();
            modals = GetComponent<UiModalStack>();
            Bind("CompetitiveButton", () => navigation.Navigate("SC05_CompetitiveHub"));
            Bind("CoopButton", () => navigation.Navigate("SC08_CoopHub"));
            Bind("CompetitiveBackButton", () => navigation.Back());
            Bind("CoopBackButton", () => navigation.Back());
            Bind("MatchRoomBackButton", () => navigation.Back());
            Bind("AnimalSelectBackButton", () => navigation.Back());
            Bind("CompetitiveMatchButton", ShowMatchUnavailable);
            Bind("RankedMatchButton", ShowMatchUnavailable);
            Bind("CompetitivePrivateButton", ShowMatchUnavailable);
            Bind("CoopMatchButton", ShowMatchUnavailable);
            Bind("CoopPrivateButton", ShowMatchUnavailable);
            Bind("CompetitiveDevPreviewButton", () => OpenPreview(competitivePreview, false));
            Bind("RankedDevPreviewButton", () => OpenPreview(competitivePreview, true));
            Bind("CoopDevPreviewButton", () => OpenPreview(coopPreview, false));
            Bind("RoomParticipantCountToggleButton", ToggleParticipantCount);
            Bind("RoomConnectionPreviewButton", CycleConnectionPreview);
            Bind("RoomAnimalSelectButton", OpenAnimalSelect);
            for (var i = 0; i < 4; i++)
            {
                var slot = i;
                Bind($"LoadoutSlot_{i + 1:00}", () => { activeSlot = slot; RefreshLoadout(); });
            }
            Bind("AnimalCard_01", () => SelectAnimalAt(0));
            Bind("AnimalCard_02", () => SelectAnimalAt(1));
            Bind("AnimalCard_03", () => SelectAnimalAt(2));
            Bind("AnimalCard_Locked", () => SelectAnimal("UNAVAILABLE_ANIMAL"));
            Bind("MultiplayerReadyButton", RequestReady);
            Bind("MultiplayerHudPreviewButton", OpenHudPreview);
            Bind("CompetitiveSeriesAdvanceButton", AdvanceCompetitiveSeriesPreview);
            Bind("CompetitiveConnectionCycleButton", CycleConnectionPreview);
            Bind("CoopConnectionCycleButton", CycleConnectionPreview);
            Bind("CoopLocationPingButton", ShowPingUnavailable);
            Bind("CompetitiveHudBackButton", () => navigation.Back());
            Bind("CoopHudBackButton", () => navigation.Back());
            Bind("CompetitiveHudPauseButton", () => modals.Push("MultiplayerPauseModal"));
            Bind("CoopHudPauseButton", () => modals.Push("MultiplayerPauseModal"));
            Bind("MultiplayerPauseContinueButton", () => modals.Pop());
            Bind("MultiplayerPauseLeaveButton", LeavePreview);
            for (var i = 1; i <= 4; i++)
            {
                Bind($"CompetitiveHudEmote_{i:00}", ShowEmoteUnavailable);
                Bind($"CoopHudEmote_{i:00}", ShowEmoteUnavailable);
            }
            RefreshConnectionState();
        }

        private void OnDisable()
        {
            foreach (var binding in bindings) if (binding.button != null) binding.button.onClick.RemoveListener(binding.action);
            bindings.Clear();
        }

        private void RefreshConnectionState()
        {
            SetText("CompetitiveConnectionState", "Unavailable · 매치 서버 미연결 · 실매치/랭킹/보상 확정 불가");
            SetText("CoopConnectionState", "Unavailable · 협동 서버 미연결 · 팀 생성/Ready 불가");
        }

        private void OpenPreview(MultiplayerPreviewDefinition preview, bool ranked)
        {
            if (preview == null || !preview.DevelopmentPreviewOnly) return;
            currentPreview = preview;
            rankedPreview = ranked;
            connectionPreviewStep = 0;
            activeSlot = 0;
            var minimum = preview.Mode == GameModeKind.Coop ? MultiplayerModeRules.CoopParticipants.Minimum : MultiplayerModeRules.CompetitiveParticipants.Minimum;
            roomPreview = new MultiplayerRoomPreviewState(preview.Mode, minimum, ranked);
            if (preview.Mode == GameModeKind.Coop)
            {
                coopLoadout = new MultiplayerLoadoutService(preview.CreateRuleSet(), new DisconnectedMatchGateway());
                competitiveSelection = null;
            }
            else
            {
                coopLoadout = null;
                competitiveSelection = CreateCompetitiveSelection(roomPreview.Participants.Select(x => x.ParticipantId));
            }
            competitiveSeries = new CompetitiveSeriesState();
            bubbleAuthority = new CompetitiveBubbleAuthorityState();
            finishWindow = new CompetitiveFinishWindowState();
            RefreshRoom();
            navigation.Navigate("SC06_MatchRoom");
        }

        private static CompetitiveParticipantSelectionService CreateCompetitiveSelection(IEnumerable<string> participantIds) =>
            new CompetitiveParticipantSelectionService(participantIds, DevelopmentAnimalRules,
                CampaignAnimalUnlockSnapshot.DevelopmentOnly("DEV_GROUND", "DEV_GLIDER", "DEV_SPECIAL"));

        private void ToggleParticipantCount()
        {
            if (roomPreview == null) return;
            roomPreview.ToggleMinimumMaximum();
            if (currentPreview.Mode != GameModeKind.Coop)
                competitiveSelection = CreateCompetitiveSelection(roomPreview.Participants.Select(x => x.ParticipantId));
            connectionPreviewStep = 0;
            RefreshRoom();
        }

        private void CycleConnectionPreview()
        {
            if (roomPreview == null || roomPreview.Participants.Count < 2) return;
            connectionPreviewStep = (connectionPreviewStep + 1) % 4;
            roomPreview.Participants[1].SetConnection((ParticipantConnectionState)connectionPreviewStep);
            RefreshRoom();
            RefreshHud();
        }

        private void RefreshRoom()
        {
            if (roomPreview == null || currentPreview == null) return;
            var mode = currentPreview.Mode == GameModeKind.Coop ? "협동" : rankedPreview ? "랭크 경쟁" : "일반 경쟁";
            var range = currentPreview.Mode == GameModeKind.Coop ? "2~4명" : "4~8명";
            SetText("MatchRoomMode", $"DEV PREVIEW ONLY · {mode} · {roomPreview.Participants.Count}명 / {range}");
            SetText("RoomCapacity", $"가변 참가자 {roomPreview.Participants.Count}명 ✓ · 운영 참가자/방 코드는 서버 미연결");
            SetText("RoomGrowthMode", roomPreview.GrowthMode == GrowthPresentationMode.RankedMaximumPreset
                ? "성장 적용: 전원 최대치 동일 프리셋 ✓"
                : "성장 적용: 보유 성장치 표시 · 계정 서버 미연결");
            SetText("RoomDuplicateRule", currentPreview.Mode == GameModeKind.Coop
                ? $"협동 중복: 모드 데이터 {(currentPreview.AllowDuplicates ? "허용" : "금지")}"
                : "경쟁: 참가자 간 동일 동물 허용 ✓ · 각자 지상/공중/특수 1개");
            for (var i = 0; i < 8; i++)
            {
                var label = FindByName<Text>($"RoomParticipant_{i + 1:00}");
                if (label == null) continue;
                label.gameObject.SetActive(i < roomPreview.Participants.Count);
                if (i >= roomPreview.Participants.Count) continue;
                var participant = roomPreview.Participants[i];
                var readiness = currentPreview.Mode == GameModeKind.Coop ? "선택 대기" : "3역할 선택 대기";
                label.text = $"{participant.DisplayName} · {participant.Connection} · {readiness}";
            }
            var countButton = FindByName<Button>("RoomParticipantCountToggleButton");
            if (countButton != null) SetButtonText(countButton, currentPreview.Mode == GameModeKind.Coop ? "2명 ↔ 4명" : "4명 ↔ 8명");
            SetText("RoomAuthorityTruth", "DEV UI 상태만 변경 · 운영 Ready/순위/보상/서버 참가자 생성 없음");
        }

        private void OpenAnimalSelect()
        {
            if (currentPreview == null) return;
            RefreshLoadout();
            navigation.Navigate("SC07_MultiplayerAnimalSelect");
        }

        private void RefreshLoadout()
        {
            if (currentPreview == null) return;
            var competitive = currentPreview.Mode != GameModeKind.Coop;
            var slotCount = competitive ? 3 : currentPreview.SlotCount;
            SetText("AnimalSelectMode", competitive
                ? "DEV PREVIEW · 캠페인 해금 스냅샷만 유효 · 참가자 간 동일 선택 허용"
                : $"DEV PREVIEW · 협동 모드 데이터 중복 {(currentPreview.AllowDuplicates ? "허용" : "금지")}");
            for (var i = 0; i < 4; i++)
            {
                var button = FindByName<Button>($"LoadoutSlot_{i + 1:00}");
                if (button == null) continue;
                button.gameObject.SetActive(i < slotCount);
                if (i >= slotCount) continue;
                var selected = competitive
                    ? competitiveSelection.GetSelection(roomPreview.Participants[0].ParticipantId, (MultiplayerAnimalRole)i)
                    : coopLoadout.Selections[i];
                var role = competitive ? ((MultiplayerAnimalRole)i).ToString() : $"협동 슬롯 {i + 1}";
                SetButtonText(button, $"{role}\n{(string.IsNullOrWhiteSpace(selected) ? "미선택" : selected)}{(activeSlot == i ? "\n선택 중" : string.Empty)}");
            }
            for (var i = 0; i < 3; i++)
            {
                var button = FindByName<Button>($"AnimalCard_{i + 1:00}");
                if (button == null) continue;
                button.gameObject.SetActive(i < currentPreview.AllowedAnimalIds.Count);
                if (i < currentPreview.AllowedAnimalIds.Count) SetButtonText(button, currentPreview.AllowedAnimalIds[i]);
            }
            var selectionComplete = competitive && competitiveSelection.CanReady(roomPreview.Participants[0].ParticipantId);
            SetText("ReadyReason", selectionComplete
                ? "선택 계약 Ready 가능 ✓ · 운영 Ready는 서버 미연결로 전송 안 함"
                : "선택 완료 전 · 운영 Ready는 서버 승인 필요");
            var ready = FindByName<Button>("MultiplayerReadyButton");
            if (ready != null)
            {
                ready.interactable = OperationalReadyEnabled;
                SetButtonText(ready, OperationalReadyEnabled ? "Ready 요청" : "운영 Ready 불가 · 서버 미연결");
            }
        }

        private void SelectAnimalAt(int index)
        {
            if (currentPreview == null || index < 0 || index >= currentPreview.AllowedAnimalIds.Count) return;
            SelectAnimal(currentPreview.AllowedAnimalIds[index]);
        }

        private void SelectAnimal(string animalId)
        {
            if (currentPreview == null) return;
            var accepted = currentPreview.Mode == GameModeKind.Coop
                ? coopLoadout != null && coopLoadout.TrySelect(activeSlot, animalId)
                : competitiveSelection != null && activeSlot < 3 && competitiveSelection.TrySelect(roomPreview.Participants[0].ParticipantId, (MultiplayerAnimalRole)activeSlot, animalId);
            if (!accepted)
            {
                ShowError("선택 거절", "캠페인 해금·역할 또는 현재 모드의 중복 규칙에 맞지 않습니다.");
                return;
            }
            var slotCount = currentPreview.Mode == GameModeKind.Coop ? currentPreview.SlotCount : 3;
            if (activeSlot + 1 < slotCount) activeSlot++;
            RefreshLoadout();
        }

        private void RequestReady()
        {
            if (!OperationalReadyEnabled)
            {
                ShowError("Ready 승인 실패", "매치 서버가 연결되지 않아 운영 Ready를 승인하지 않았습니다.");
                return;
            }
            if (currentPreview.Mode == GameModeKind.Coop)
            {
                var result = coopLoadout.RequestReady();
                if (result.Status != MatchRequestStatus.Approved) ShowError("Ready 승인 실패", result.Message);
            }
        }

        private void OpenHudPreview()
        {
            if (currentPreview == null) return;
            RefreshHud();
            navigation.Navigate(currentPreview.Mode == GameModeKind.Coop ? "HUD_Coop" : "HUD_Competitive");
        }

        private void RefreshHud()
        {
            if (currentPreview == null || roomPreview == null) return;
            var disconnected = roomPreview.Participants.FirstOrDefault(x => x.Connection != ParticipantConnectionState.Connected);
            if (currentPreview.Mode == GameModeKind.Coop)
            {
                SetText("HudObjective", $"팀 방울 0/3 · 탈출한 팀원 0/{roomPreview.Participants.Count} · 출구 잠김 ✕");
                SetText("CoopRosterState", $"팀 {roomPreview.Participants.Count}명 · 이모티콘/위치 핑 포트 제공 · 서버 전송 없음");
                SetText("CoopReconnectState", disconnected == null ? "팀원 연결: 모두 Connected" : $"팀원 {disconnected.DisplayName}: {disconnected.Connection}");
                return;
            }
            SetText("HudModeTitle", rankedPreview
                ? "DEV 랭크 HUD · 성장 최대치 프리셋 · 서버 미연결"
                : "DEV 일반 경쟁 HUD · 보유 성장치 · 서버 미연결");
            SetText("CompetitiveSeriesState", $"현재 맵 {competitiveSeries.CurrentMapNumber}/3 · 3개 맵 연속 경기");
            SetText("CompetitiveMapResults", string.Join("  |  ", competitiveSeries.Maps.Select(x => $"맵{x.MapNumber} {x.Status} {x.ServerPerformanceScore}점")) + $"\n총합 {competitiveSeries.TotalServerPerformanceScore}점 · 순위 서버 미확정");
            SetText("HudObjective", "개인 방울 0/3 · 출구 자격 ✕ · 동일 슬롯 선점은 서버 확정만 반영");
            SetText("CompetitiveBubbleAuthority", $"방울 슬롯: {bubbleAuthority.Status} · 서버 확정 재생성 대기");
            SetText("CompetitiveFinishWindow", finishWindow.FirstFinishConfirmed ? $"첫 완주 확정 · 추가 {finishWindow.AdditionalFinishSeconds:0}s" : "첫 완주/남은 추가 완주 시간: 서버 대기");
            SetText("CompetitiveHudConnectionState", disconnected == null ? "연결: 모두 Connected" : $"{disconnected.DisplayName}: {disconnected.Connection}");
            SetText("CompetitiveNoCombat", "몸 공격 ✕ · 넉백 ✕ · 수행 점수는 시간 종료 후 서버 확정");
        }

        private void AdvanceCompetitiveSeriesPreview()
        {
            if (competitiveSeries == null) return;
            var current = competitiveSeries.CurrentMapNumber;
            competitiveSeries.ApplyServerMapResult(current, current == 3 ? CompetitiveMapStatus.TimeExpired : CompetitiveMapStatus.Completed,
                current * 100, current == 3 ? "시간 종료 수행 점수" : "완주", true);
            RefreshHud();
        }

        private void LeavePreview()
        {
            modals.Pop();
            navigation.Navigate("SC01_Lobby", false);
        }

        private void ShowMatchUnavailable() => ShowError("온라인 서비스 미연결", "실매치·방 코드·운영 Ready·순위·보상 성공을 생성하지 않습니다. 개발용 UI 미리보기만 사용할 수 있습니다.");
        private void ShowEmoteUnavailable() => ShowError("이모티콘 전송 실패", "UI 포트는 연결됐지만 매치 서버가 없어 전송하지 않았습니다.");
        private void ShowPingUnavailable() => ShowError("위치 핑 전송 실패", "UI 포트는 연결됐지만 협동 서버가 없어 위치 핑을 전송하지 않았습니다.");

        private void ShowError(string title, string message)
        {
            SetText("ServiceErrorTitle", title);
            SetText("ServiceErrorMessage", message);
            modals.Push("ServiceErrorModal");
        }

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

        private static void SetButtonText(Button button, string value)
        {
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = value;
        }
    }
}
