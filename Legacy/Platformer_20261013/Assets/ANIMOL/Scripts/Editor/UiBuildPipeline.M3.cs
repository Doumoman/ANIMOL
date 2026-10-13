using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    public static partial class UiBuildPipeline
    {
        private const string MetaDataFolder = "Assets/ANIMOL/Data/Meta";
        private const string ServiceDataFolder = "Assets/ANIMOL/Data/Services";
        private const string AccountCatalogPath = MetaDataFolder + "/AccountUpgradeCatalog.asset";
        private const string CharacterCatalogPath = MetaDataFolder + "/CharacterUpgradeCatalog.asset";
        private const string EmoteCatalogPath = MetaDataFolder + "/EmoteCatalog.asset";
        private const string ServiceConfigPath = ServiceDataFolder + "/ExternalServiceConfiguration.asset";

        public static void BuildM3Menu()
        {
            BuildM0(); BuildM1(); BuildM2(); BuildM3(); ValidateM3();
        }

        static partial void BuildM3()
        {
            Directory.CreateDirectory(MetaDataFolder);
            Directory.CreateDirectory(ServiceDataFolder);
            BuildAccountCatalog();
            BuildCharacterCatalog();
            BuildEmoteCatalog();
            BuildServiceConfiguration();
            BuildM3Screens();
            BuildM3Modals();
            BuildLobbyScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ANIMOL][M3] Growth, emote, store and records UI built with disconnected service state.");
        }

        static partial void ValidateM3()
        {
            var errors = new List<string>();
            var account = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>(AccountCatalogPath);
            var characters = AssetDatabase.LoadAssetAtPath<CharacterUpgradeCatalog>(CharacterCatalogPath);
            var emotes = AssetDatabase.LoadAssetAtPath<EmoteCatalog>(EmoteCatalogPath);
            var services = AssetDatabase.LoadAssetAtPath<ExternalServiceConfiguration>(ServiceConfigPath);
            if (account == null || account.Tracks.Count != 3) errors.Add("Account upgrade tracks must equal 3.");
            if (account != null && account.Tracks.Any(x => x.MaxLevel != 10 || x.PercentByLevel.Count != 11 || x.CostByLevel.Count != 10)) errors.Add("Account upgrade level/cost data mismatch.");
            if (characters == null || characters.Definitions.Count != 0) errors.Add("Character upgrade definitions must remain unconfigured.");
            if (emotes == null || emotes.Emotes.Count(x => x.GrantedByDefault) != 3) errors.Add("Exactly 3 default-free emotes required.");
            if (services == null || services.AccountServerConnected || services.MatchServerConnected || services.PurchaseSdkConnected || services.RewardedAdSdkConnected)
                errors.Add("External services must remain explicitly disconnected.");

            var required = new[]
            {
                "SC10_UpgradeHub", "SC10A_AccountUpgrade", "SC10B_CharacterUpgrade", "SC11_Store", "SC12_EmoteCollection", "SC17_ProfileRecords",
                "RewardedAdModal", "ProductDetailModal", "ServiceErrorModal"
            };
            foreach (var name in required) if (AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/{name}.prefab") == null) errors.Add("Missing " + name);
            var emotePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC12_EmoteCollection.prefab");
            var quickSlots = emotePrefab == null ? 0 : emotePrefab.GetComponentsInChildren<Button>(true).Count(x => x.name.StartsWith("QuickSlot_", StringComparison.Ordinal));
            var freeEmotes = emotePrefab == null ? 0 : emotePrefab.GetComponentsInChildren<Button>(true).Count(x => x.name.StartsWith("FreeEmote_", StringComparison.Ordinal));
            if (quickSlots != 4 || freeEmotes != 3) errors.Add($"Emote UI free={freeEmotes}, slots={quickSlots}");
            var store = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/ProductDetailModal.prefab");
            var purchase = store == null ? null : store.GetComponentsInChildren<Button>(true).FirstOrDefault(x => x.name == "ProductPurchaseButton");
            if (purchase == null || purchase.interactable) errors.Add("Product purchase must be disabled without SDK/product price.");

            var lines = new[]
            {
                "ANIMOL M3 validation", $"valid={errors.Count == 0}", "accountTracks=3", "accountMaxLevel=10",
                "characterUpgradeDefinitions=0", "characterUpgradeState=Unconfigured", "defaultFreeEmotes=3", $"quickSlots={quickSlots}",
                "accountServerConnected=false", "purchaseSdkConnected=false", "rewardedAdSdkConnected=false", "productPurchaseEnabled=false",
                "errors=" + string.Join(" | ", errors)
            };
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
            File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "animol-m3-validation.txt"), lines);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static void BuildAccountCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>(AccountCatalogPath);
            if (catalog != null) return;
            catalog = ScriptableObject.CreateInstance<AccountUpgradeCatalog>();
            AssetDatabase.CreateAsset(catalog, AccountCatalogPath);
            var serialized = new SerializedObject(catalog);
            serialized.FindProperty("version").intValue = 1;
            var tracks = serialized.FindProperty("tracks"); tracks.arraySize = 3;
            var ids = new[] { "ACCOUNT_MAX_STAMINA", "ACCOUNT_STAMINA_REGEN", "ACCOUNT_STAMINA_EFFICIENCY" };
            var names = new[] { "최대 스테미나", "재생 속도", "소비량 감소" };
            var costs = new[] { 180, 220, 260, 310, 370, 440, 520, 610, 710, 820 };
            for (var t = 0; t < 3; t++)
            {
                var item = tracks.GetArrayElementAtIndex(t);
                item.FindPropertyRelative("trackId").stringValue = ids[t];
                item.FindPropertyRelative("displayName").stringValue = names[t];
                item.FindPropertyRelative("maxLevel").intValue = 10;
                var values = item.FindPropertyRelative("percentByLevel"); values.arraySize = 11;
                for (var level = 0; level <= 10; level++) values.GetArrayElementAtIndex(level).floatValue = t == 2 ? 100f - .6f * level : 100f + .8f * level;
                var price = item.FindPropertyRelative("costByLevel"); price.arraySize = costs.Length;
                for (var level = 0; level < costs.Length; level++) price.GetArrayElementAtIndex(level).intValue = costs[level];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildCharacterCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterUpgradeCatalog>(CharacterCatalogPath);
            if (catalog != null) return;
            catalog = ScriptableObject.CreateInstance<CharacterUpgradeCatalog>();
            AssetDatabase.CreateAsset(catalog, CharacterCatalogPath);
            var serialized = new SerializedObject(catalog);
            var animals = serialized.FindProperty("previewAnimalIds"); animals.arraySize = 2;
            animals.GetArrayElementAtIndex(0).stringValue = "DEV_GROUND";
            animals.GetArrayElementAtIndex(1).stringValue = "DEV_GLIDER";
            serialized.FindProperty("definitions").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildEmoteCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EmoteCatalog>(EmoteCatalogPath);
            if (catalog != null) return;
            catalog = ScriptableObject.CreateInstance<EmoteCatalog>();
            AssetDatabase.CreateAsset(catalog, EmoteCatalogPath);
            var serialized = new SerializedObject(catalog);
            var emotes = serialized.FindProperty("emotes"); emotes.arraySize = 3;
            var ids = new[] { "EMOTE_HELLO", "EMOTE_THANKS", "EMOTE_HELP" };
            var names = new[] { "인사", "감사", "도움 요청" };
            for (var i = 0; i < 3; i++)
            {
                var item = emotes.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("emoteId").stringValue = ids[i];
                item.FindPropertyRelative("displayName").stringValue = names[i];
                item.FindPropertyRelative("visualKey").stringValue = "placeholder/" + ids[i].ToLowerInvariant();
                item.FindPropertyRelative("grantedByDefault").boolValue = true;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildServiceConfiguration()
        {
            if (AssetDatabase.LoadAssetAtPath<ExternalServiceConfiguration>(ServiceConfigPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ExternalServiceConfiguration>(), ServiceConfigPath);
        }

        private static void BuildM3Screens()
        {
            SaveScreen("SC10_UpgradeHub", root =>
            {
                CreateLabel("UpgradeTitle", root.transform, "업그레이드", 58, new Vector2(.5f, .88f), new Vector2(.5f, .88f), new Vector2(900, 90), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("UpgradeConnectionState", root.transform, "Unavailable · 계정 서버 미연결", 26, new Vector2(.5f, .77f), new Vector2(.5f, .77f), new Vector2(1200, 55), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("UpgradeHubAccountButton", root.transform, "계정 공통 성장\n3트랙 · Lv.0/10 데이터", new Vector2(.35f, .5f), new Vector2(480, 270), Cyan);
                CreateButton("UpgradeHubCharacterButton", root.transform, "동물별 성장\n정의 없음 · 기획 중", new Vector2(.65f, .5f), new Vector2(480, 270), new Color(.22f, .3f, .4f, 1));
                CreateButton("UpgradeHubBackButton", root.transform, "← 로비", new Vector2(.11f, .1f), new Vector2(250, 76), Panel);
            });
            SaveScreen("SC10A_AccountUpgrade", root =>
            {
                CreateLabel("AccountUpgradeTitle", root.transform, "계정 공통 성장 · 코인 미연결", 50, new Vector2(.5f, .9f), new Vector2(.5f, .9f), new Vector2(1200, 80), Vector2.zero, TextAnchor.MiddleCenter);
                for (var i = 0; i < 3; i++) CreateButton($"AccountTrack_{i + 1:00}", root.transform, "트랙", new Vector2(.22f + i * .28f, .52f), new Vector2(430, 350), new Color(.08f, .28f, .4f, 1)).interactable = false;
                CreateLabel("AccountRankNote", root.transform, "랭크: 서버가 잠근 동일 프리셋 적용 · 로컬 구매로 변경하지 않음", 26, new Vector2(.5f, .26f), new Vector2(.5f, .26f), new Vector2(1300, 55), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("AccountExplainButton", root.transform, "구매 불가 사유", new Vector2(.5f, .16f), new Vector2(330, 76), Amber);
                CreateButton("AccountUpgradeBackButton", root.transform, "← 업그레이드", new Vector2(.11f, .08f), new Vector2(290, 70), Panel);
            });
            SaveScreen("SC10B_CharacterUpgrade", root =>
            {
                CreateLabel("CharacterUpgradeTitle", root.transform, "동물별 업그레이드 · 확장 자리", 50, new Vector2(.5f, .9f), new Vector2(.5f, .9f), new Vector2(1200, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("CharacterCard_01", root.transform, "DEV_GROUND\n기획 중", new Vector2(.32f, .58f), new Vector2(430, 250), Panel);
                CreateButton("CharacterCard_02", root.transform, "DEV_GLIDER\n기획 중", new Vector2(.68f, .58f), new Vector2(430, 250), Panel);
                CreateLabel("CharacterUpgradeState", root.transform, "효과·가격·MAX 미정 · 캠페인 고정 동물을 바꾸지 않음", 28, new Vector2(.5f, .35f), new Vector2(.5f, .35f), new Vector2(1300, 70), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("CharacterPurchaseButton", root.transform, "구매 불가 · 정의 없음", new Vector2(.5f, .23f), new Vector2(420, 80), new Color(.25f, .28f, .32f, 1)).interactable = false;
                CreateButton("CharacterExplainButton", root.transform, "잠금 사유", new Vector2(.67f, .23f), new Vector2(260, 80), Amber);
                CreateButton("CharacterUpgradeBackButton", root.transform, "← 업그레이드", new Vector2(.11f, .08f), new Vector2(290, 70), Panel);
            });
            SaveScreen("SC11_Store", root =>
            {
                CreateLabel("StoreTitle", root.transform, "상점", 58, new Vector2(.5f, .9f), new Vector2(.5f, .9f), new Vector2(800, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("StoreConnectionState", root.transform, "Unavailable · 결제 SDK/상품/가격 미연결", 28, new Vector2(.5f, .8f), new Vector2(.5f, .8f), new Vector2(1200, 60), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("StoreGrowthDetailsButton", root.transform, "성장 완료 팩\n계정 공통 3트랙만\n판매 준비 중", new Vector2(.35f, .5f), new Vector2(470, 330), Panel);
                CreateButton("StoreEmoteDetailsButton", root.transform, "이모티콘 번들\n상품 미연결\n판매 준비 중", new Vector2(.65f, .5f), new Vector2(470, 330), Panel);
                CreateButton("StoreRestoreButton", root.transform, "구매 복원 · SDK 미연결", new Vector2(.5f, .23f), new Vector2(430, 80), Amber);
                CreateButton("StoreBackButton", root.transform, "← 로비", new Vector2(.11f, .08f), new Vector2(250, 70), Panel);
            });
            SaveScreen("SC12_EmoteCollection", root =>
            {
                CreateLabel("EmoteTitle", root.transform, "이모티콘 · 기본 무료 3개", 52, new Vector2(.5f, .91f), new Vector2(.5f, .91f), new Vector2(1100, 80), Vector2.zero, TextAnchor.MiddleCenter);
                for (var i = 0; i < 3; i++) CreateButton($"FreeEmote_{i + 1:00}", root.transform, "무료 이모티콘", new Vector2(.25f + i * .25f, .68f), new Vector2(360, 190), Cyan);
                CreateLabel("QuickSlotTitle", root.transform, "퀵 슬롯 · 최대 4개 · 보유 항목만 장착", 30, new Vector2(.5f, .48f), new Vector2(.5f, .48f), new Vector2(1100, 55), Vector2.zero, TextAnchor.MiddleCenter);
                for (var i = 0; i < 4; i++) CreateButton($"QuickSlot_{i + 1:00}", root.transform, $"슬롯 {i + 1}", new Vector2(.23f + i * .18f, .34f), new Vector2(280, 130), Panel);
                CreateButton("EmoteStoreButton", root.transform, "미보유 상품 보기", new Vector2(.67f, .14f), new Vector2(330, 72), Amber);
                CreateButton("EmoteBackButton", root.transform, "← 로비", new Vector2(.11f, .08f), new Vector2(250, 70), Panel);
            });
            SaveScreen("SC17_ProfileRecords", root =>
            {
                CreateLabel("ProfileTitle", root.transform, "프로필 · 기록", 58, new Vector2(.5f, .88f), new Vector2(.5f, .88f), new Vector2(900, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("CampaignRecordState", root.transform, "캠페인 0/100 · 준비된 운영 스테이지 0\n실제 검증 완료 기록만 표시", 34, new Vector2(.5f, .6f), new Vector2(.5f, .6f), new Vector2(1100, 150), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("ProfileOnlineState", root.transform, "경쟁 기록: 서비스 미연결", 30, new Vector2(.5f, .4f), new Vector2(.5f, .4f), new Vector2(1000, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("ProfileBackButton", root.transform, "← 로비", new Vector2(.11f, .08f), new Vector2(250, 70), Panel);
            });
        }

        private static void BuildM3Modals()
        {
            SaveModal("RewardedAdModal", "선택 광고", "광고 SDK 미연결 · 시청/보상 승인 불가\n런타임 HUD에는 진입점이 없습니다.", "RewardedAdWatchButton", "시청 불가", false, "RewardedAdCancelButton", "취소");
            SaveModal("ProductDetailModal", "상품 상세", "플랫폼 가격: 판매 준비 중", "ProductPurchaseButton", "구매 불가", false, "ProductCancelButton", "닫기");
            SaveModal("ServiceErrorModal", "서비스 미연결", "외부 서비스가 연결되지 않았습니다.", "ServiceErrorRetryButton", "재시도", true, "ServiceErrorBackButton", "뒤로");
        }

        private static void SaveModal(string name, string title, string message, string primaryName, string primaryText, bool primaryEnabled, string secondaryName, string secondaryText)
        {
            var root = CreatePanelRoot(name, new Color(0, 0, 0, .8f));
            try
            {
                var card = CreateImage("Card", root.transform, new Vector2(.25f, .2f), new Vector2(.75f, .8f), Vector2.zero, Vector2.zero, Panel);
                CreateLabel(name == "ProductDetailModal" ? "ProductTitle" : name == "ServiceErrorModal" ? "ServiceErrorTitle" : "ModalTitle", card.transform, title, 48, new Vector2(.5f, .78f), new Vector2(.5f, .78f), new Vector2(800, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel(name == "ProductDetailModal" ? "ProductDescription" : name == "ServiceErrorModal" ? "ServiceErrorMessage" : "ModalMessage", card.transform, message, 28, new Vector2(.5f, .52f), new Vector2(.5f, .52f), new Vector2(820, 190), Vector2.zero, TextAnchor.MiddleCenter);
                var primary = CreateButton(primaryName, card.transform, primaryText, new Vector2(.35f, .2f), new Vector2(280, 76), primaryEnabled ? Cyan : new Color(.25f, .28f, .32f, 1));
                primary.interactable = primaryEnabled;
                CreateButton(secondaryName, card.transform, secondaryText, new Vector2(.65f, .2f), new Vector2(280, 76), Panel);
                PrefabUtility.SaveAsPrefabAsset(root, $"{UiPrefabFolder}/{name}.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
