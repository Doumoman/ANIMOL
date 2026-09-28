using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI
{
    public sealed class MetaUiPresenter : MonoBehaviour
    {
        [SerializeField] private AccountUpgradeCatalog accountUpgradeCatalog;
        [SerializeField] private CharacterUpgradeCatalog characterUpgradeCatalog;
        [SerializeField] private EmoteCatalog emoteCatalog;
        [SerializeField] private ExternalServiceConfiguration services;

        private readonly List<(Button button, UnityEngine.Events.UnityAction action)> bindings = new List<(Button, UnityEngine.Events.UnityAction)>();
        private UiNavigationService navigation;
        private UiModalStack modals;
        private EmoteLoadoutService emoteLoadout;
        private int nextQuickSlot;

        private void OnEnable()
        {
            navigation = GetComponent<UiNavigationService>();
            modals = GetComponent<UiModalStack>();
            var defaults = emoteCatalog == null ? Array.Empty<string>() : emoteCatalog.Emotes.Where(x => x.GrantedByDefault).Select(x => x.EmoteId).ToArray();
            emoteLoadout = new EmoteLoadoutService(defaults);

            Bind("UpgradeButton", () => navigation.Navigate("SC10_UpgradeHub"));
            Bind("EmoteButton", () => { RefreshEmotes(); navigation.Navigate("SC12_EmoteCollection"); });
            Bind("StoreButton", () => navigation.Navigate("SC11_Store"));
            Bind("ProfileButton", () => navigation.Navigate("SC17_ProfileRecords"));
            Bind("RewardedAdButton", () => modals.Push("RewardedAdModal"));
            Bind("UpgradeHubAccountButton", () => { RefreshAccountTracks(); navigation.Navigate("SC10A_AccountUpgrade"); });
            Bind("UpgradeHubCharacterButton", () => navigation.Navigate("SC10B_CharacterUpgrade"));
            Bind("UpgradeHubBackButton", () => navigation.Back());
            Bind("AccountUpgradeBackButton", () => navigation.Back());
            Bind("CharacterUpgradeBackButton", () => navigation.Back());
            Bind("StoreBackButton", () => navigation.Back());
            Bind("EmoteBackButton", () => navigation.Back());
            Bind("ProfileBackButton", () => navigation.Back());
            Bind("AccountExplainButton", () => ShowServiceError("계정 서버 미연결", "코인 잔액과 성장 구매는 서버 승인이 필요합니다. 로컬에서 성공이나 차감을 만들지 않습니다."));
            Bind("CharacterExplainButton", () => ShowServiceError("동물별 성장 미설정", "동물별 효과·가격·레벨 정의가 아직 없어 구매할 수 없습니다."));
            Bind("StoreRestoreButton", () => ShowServiceError("결제 SDK 미연결", "구매 복원 요청을 보낼 플랫폼 SDK가 없습니다."));
            Bind("StoreGrowthDetailsButton", () => ShowProduct("성장 완료 팩", "계정 공통 3트랙만 대상 · 부분 성장 환급 정책 미정", false));
            Bind("StoreEmoteDetailsButton", () => ShowProduct("이모티콘 번들", "상품 ID·플랫폼 가격·소유권 서버 미연결", false));
            Bind("ProductCancelButton", () => modals.Pop());
            Bind("RewardedAdCancelButton", () => modals.Pop());
            Bind("ServiceErrorBackButton", () => modals.Pop());
            Bind("ServiceErrorRetryButton", RetryUnavailableService);
            Bind("EmoteStoreButton", () => navigation.Navigate("SC11_Store"));
            for (var i = 1; i <= 3; i++)
            {
                var index = i - 1;
                Bind($"FreeEmote_{i:00}", () => EquipFreeEmote(index));
            }
            RefreshServiceLabels();
            RefreshAccountTracks();
            RefreshCharacterUpgrades();
            RefreshEmotes();
        }

        private void OnDisable()
        {
            foreach (var binding in bindings) if (binding.button != null) binding.button.onClick.RemoveListener(binding.action);
            bindings.Clear();
        }

        private void RefreshServiceLabels()
        {
            SetText("LobbyAccountStatus", "계정·코인: 미연결 · 로컬 캠페인만 사용 가능");
            SetText("UpgradeConnectionState", "Unavailable · 계정 서버 미연결 · 코인 선차감 없음");
            SetText("StoreConnectionState", "Unavailable · 결제 SDK/상품/플랫폼 가격 미연결");
            SetText("ProfileOnlineState", "경쟁 기록: 서비스 미연결 · 가짜 전적 표시 안 함");
        }

        private void RefreshAccountTracks()
        {
            if (accountUpgradeCatalog == null) return;
            for (var i = 0; i < 3; i++)
            {
                var button = FindByName<Button>($"AccountTrack_{i + 1:00}");
                if (button == null) continue;
                if (i >= accountUpgradeCatalog.Tracks.Count)
                {
                    button.interactable = false;
                    SetButtonText(button, "미설정 트랙");
                    continue;
                }
                var track = accountUpgradeCatalog.Tracks[i];
                SetButtonText(button, $"{track.DisplayName}\nLv.0/{track.MaxLevel} · 다음 {track.PercentByLevel[1]:0.#}% · {track.CostForNextLevel(0)} 코인\n구매 불가 · 서버 미연결");
                button.interactable = false;
            }
        }

        private void RefreshCharacterUpgrades()
        {
            var ids = characterUpgradeCatalog == null ? Array.Empty<string>() : characterUpgradeCatalog.PreviewAnimalIds.ToArray();
            for (var i = 0; i < 2; i++)
            {
                var button = FindByName<Button>($"CharacterCard_{i + 1:00}");
                if (button == null) continue;
                var id = i < ids.Length ? ids[i] : "미할당";
                SetButtonText(button, $"{id}\n개발 fixture · 전용 성장 기획 중");
            }
            var purchase = FindByName<Button>("CharacterPurchaseButton");
            if (purchase != null) purchase.interactable = false;
        }

        private void RefreshEmotes()
        {
            if (emoteCatalog != null)
            {
                for (var i = 0; i < 3; i++)
                {
                    var button = FindByName<Button>($"FreeEmote_{i + 1:00}");
                    if (button == null || i >= emoteCatalog.Emotes.Count) continue;
                    var emote = emoteCatalog.Emotes[i];
                    SetButtonText(button, $"{emote.DisplayName}\n기본 무료 · 보유");
                }
            }
            for (var i = 0; i < EmoteLoadoutService.QuickSlotCount; i++)
            {
                var button = FindByName<Button>($"QuickSlot_{i + 1:00}");
                if (button != null) SetButtonText(button, $"슬롯 {i + 1}\n{(string.IsNullOrWhiteSpace(emoteLoadout.Slots[i]) ? "비어 있음" : emoteLoadout.Slots[i])}");
            }
        }

        private void EquipFreeEmote(int index)
        {
            if (emoteCatalog == null || index < 0 || index >= emoteCatalog.Emotes.Count) return;
            var emote = emoteCatalog.Emotes[index];
            if (emoteLoadout.TryEquip(nextQuickSlot, emote.EmoteId)) nextQuickSlot = (nextQuickSlot + 1) % EmoteLoadoutService.QuickSlotCount;
            RefreshEmotes();
        }

        private void ShowProduct(string title, string description, bool purchasable)
        {
            SetText("ProductTitle", title);
            SetText("ProductDescription", description + "\n플랫폼 가격: 판매 준비 중");
            var purchase = FindByName<Button>("ProductPurchaseButton");
            if (purchase != null) purchase.interactable = purchasable;
            modals.Push("ProductDetailModal");
        }

        private void ShowServiceError(string title, string message)
        {
            SetText("ServiceErrorTitle", title);
            SetText("ServiceErrorMessage", message);
            modals.Push("ServiceErrorModal");
        }

        private void RetryUnavailableService()
        {
            SetText("ServiceErrorMessage", "재시도 결과: 여전히 미연결입니다. 성공·보상·잔액 변경은 발생하지 않았습니다.");
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
