using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    public static partial class UiBuildPipeline
    {
        private const string EconomyPolicyPath = "Assets/ANIMOL/Data/Meta/GrowthEconomyPolicyCatalog.asset";

        private static void BuildM6GrowthEconomyFollowup()
        {
            ConfigureAccountPrototypeValues();
            ConfigureGrowthEconomyPolicy();
            BuildM6GrowthEconomyScreens();
            UpdateM6GrowthEconomyPrefabs();
        }

        private static void ConfigureAccountPrototypeValues()
        {
            var account = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>(AccountCatalogPath);
            if (account == null) throw new InvalidOperationException("Account upgrade catalog is missing.");
            var serialized = new SerializedObject(account);
            serialized.FindProperty("prototypeValues").boolValue = true;
            serialized.FindProperty("prototypeNotice").stringValue = "플레이 검증 전 튜닝 초안";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureGrowthEconomyPolicy()
        {
            AssetDatabase.ImportAsset("Assets/ANIMOL/Scripts/Core/GrowthEconomyPolicyCatalog.cs", ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var policy = AssetDatabase.LoadAssetAtPath<GrowthEconomyPolicyCatalog>(EconomyPolicyPath);
            if (policy == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(EconomyPolicyPath) != null) AssetDatabase.DeleteAsset(EconomyPolicyPath);
                policy = ScriptableObject.CreateInstance<GrowthEconomyPolicyCatalog>();
                AssetDatabase.CreateAsset(policy, EconomyPolicyPath);
            }

            var serialized = new SerializedObject(policy);
            serialized.FindProperty("version").intValue = 1;
            serialized.FindProperty("sharedRewardedAdDailyLimit").intValue = 3;
            serialized.FindProperty("lobbyGeneralCoinDraft").intValue = 150;
            serialized.FindProperty("dailyResetAuthority").stringValue = "ACCOUNT_SERVER";

            var animals = serialized.FindProperty("animalGrowth");
            animals.arraySize = 3;
            var animalIds = new[] { "DEV_GROUND", "DEV_GLIDER", "DEV_SPECIAL" };
            var requiredStages = new[] { string.Empty, "T01-S01", "T01-S02" };
            for (var i = 0; i < animals.arraySize; i++)
            {
                var animal = animals.GetArrayElementAtIndex(i);
                animal.FindPropertyRelative("animalId").stringValue = animalIds[i];
                animal.FindPropertyRelative("requiredCompletedStageId").stringValue = requiredStages[i];
                animal.FindPropertyRelative("masteryGainPolicyKey").stringValue = string.Empty;
                var tracks = animal.FindPropertyRelative("tracks");
                tracks.arraySize = 2;
                for (var trackIndex = 0; trackIndex < 2; trackIndex++)
                {
                    var track = tracks.GetArrayElementAtIndex(trackIndex);
                    track.FindPropertyRelative("kind").enumValueIndex = trackIndex;
                    track.FindPropertyRelative("maxLevel").intValue = 5;
                    track.FindPropertyRelative("effectKey").stringValue = string.Empty;
                    track.FindPropertyRelative("sharedCoinCostByLevel").arraySize = 0;
                    track.FindPropertyRelative("masteryCostByLevel").arraySize = 0;
                }
            }

            var pack = serialized.FindProperty("growthCompletionPack");
            pack.FindPropertyRelative("offerKey").stringValue = "ACCOUNT_GROWTH_COMPLETE_PACK";
            var included = pack.FindPropertyRelative("includedAccountTrackIds");
            included.arraySize = 3;
            var accountTrackIds = new[] { "ACCOUNT_MAX_STAMINA", "ACCOUNT_STAMINA_REGEN", "ACCOUNT_STAMINA_EFFICIENCY" };
            for (var i = 0; i < accountTrackIds.Length; i++) included.GetArrayElementAtIndex(i).stringValue = accountTrackIds[i];
            pack.FindPropertyRelative("includesAnimalGrowth").boolValue = false;
            pack.FindPropertyRelative("includesAnimalUnlocks").boolValue = false;
            pack.FindPropertyRelative("cosmeticCompensationFormulaConfigured").boolValue = false;
            pack.FindPropertyRelative("authenticatedSaleEnabled").boolValue = false;

            var emotes = serialized.FindProperty("emoteAcquisitions");
            emotes.arraySize = 5;
            var emoteIds = new[] { "EMOTE_HELLO", "EMOTE_THANKS", "EMOTE_HELP", "EMOTE_COIN_PREVIEW", "EMOTE_PAID_PREVIEW" };
            var emoteKinds = new[] { EmoteAcquisitionKind.Free, EmoteAcquisitionKind.Free, EmoteAcquisitionKind.Free, EmoteAcquisitionKind.CoinExchange, EmoteAcquisitionKind.Paid };
            for (var i = 0; i < emoteIds.Length; i++)
            {
                var emote = emotes.GetArrayElementAtIndex(i);
                emote.FindPropertyRelative("emoteId").stringValue = emoteIds[i];
                emote.FindPropertyRelative("acquisitionKind").enumValueIndex = (int)emoteKinds[i];
                emote.FindPropertyRelative("authenticatedOfferKey").stringValue = i < 3 ? string.Empty : "OFFER_" + emoteIds[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(policy);
        }

        private static void BuildM6GrowthEconomyScreens()
        {
            SaveScreen("SC20_AccountAndSave", root =>
            {
                CreateLabel("AccountSaveTitle", root.transform, "계정 연결 · 저장 선택", 50, new Vector2(.5f, .92f), new Vector2(.5f, .92f), new Vector2(1200, 72), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("AccountIdentityState", root.transform, "현재: 게스트", 28, new Vector2(.5f, .84f), new Vector2(.5f, .84f), new Vector2(1300, 44), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("AccountGateState", root.transform, "게스트 캠페인 가능 · 온라인/구매 잠김", 25, new Vector2(.5f, .78f), new Vector2(.5f, .78f), new Vector2(1450, 42), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("CoinSeparationState", root.transform, "로컬 기록 / 검증된 온라인 코인 분리", 24, new Vector2(.5f, .72f), new Vector2(.5f, .72f), new Vector2(1450, 42), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("GuestCampaignButton", root.transform, "게스트로 캠페인 시작", new Vector2(.35f, .62f), new Vector2(420, 78), Cyan);
                CreateButton("GoogleLinkButton", root.transform, "Google 계정 연결\nSDK 미연결", new Vector2(.65f, .62f), new Vector2(420, 78), Amber);
                CreateLabel("LocalRecordState", root.transform, "로컬 기록", 25, new Vector2(.3f, .49f), new Vector2(.3f, .49f), new Vector2(650, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("CloudRecordState", root.transform, "클라우드 기록", 25, new Vector2(.7f, .49f), new Vector2(.7f, .49f), new Vector2(650, 80), Vector2.zero, TextAnchor.MiddleCenter);
                var local = CreateButton("SelectLocalSaveButton", root.transform, "로컬 기록 선택", new Vector2(.3f, .39f), new Vector2(320, 68), Panel); local.interactable = false;
                var cloud = CreateButton("SelectCloudSaveButton", root.transform, "클라우드 기록 선택", new Vector2(.7f, .39f), new Vector2(320, 68), Panel); cloud.interactable = false;
                CreateLabel("SaveSelectionState", root.transform, "저장 선택: 없음", 24, new Vector2(.5f, .3f), new Vector2(.5f, .3f), new Vector2(1250, 44), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("SaveConflictDevPreviewButton", root.transform, "DEV ONLY · 로컬/클라우드 충돌 미리보기", new Vector2(.5f, .22f), new Vector2(600, 68), Amber);
                CreateButton("EconomyDevPreviewButton", root.transform, "DEV ONLY · 경제 원장 검증", new Vector2(.68f, .1f), new Vector2(390, 66), Amber);
                CreateButton("AccountSaveBackButton", root.transform, "로비", new Vector2(.12f, .1f), new Vector2(240, 66), Panel);
            });

            SaveScreen("SC21_EconomyDevPreview", root =>
            {
                CreateLabel("EconomyDevTitle", root.transform, "DEV ONLY · 광고/코인 원장 검증", 48, new Vector2(.5f, .91f), new Vector2(.5f, .91f), new Vector2(1350, 72), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("EconomyDevWarning", root.transform, "운영 지급 아님 · 로컬 시계 사용 안 함 · 서버 응답 형태의 테스트 어댑터", 25, new Vector2(.5f, .83f), new Vector2(.5f, .83f), new Vector2(1450, 46), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("DevRewardBreakdown", root.transform, "DEV 보상 산식", 26, new Vector2(.5f, .73f), new Vector2(.5f, .73f), new Vector2(1500, 70), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("DevLedgerState", root.transform, "DEV 어댑터 대기", 27, new Vector2(.5f, .64f), new Vector2(.5f, .64f), new Vector2(1500, 55), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("DevGrantHistory", root.transform, "내역 없음", 22, new Vector2(.5f, .55f), new Vector2(.5f, .55f), new Vector2(1500, 78), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("DevResultApproveButton", root.transform, "결과 B 추가 승인", new Vector2(.22f, .4f), new Vector2(300, 72), Cyan);
                CreateButton("DevDuplicateButton", root.transform, "같은 결과 중복", new Vector2(.4f, .4f), new Vector2(300, 72), Panel);
                CreateButton("DevConcurrentTapButton", root.transform, "동시 탭", new Vector2(.58f, .4f), new Vector2(300, 72), Panel);
                CreateButton("DevGeneralApproveButton", root.transform, "일반 +150 승인", new Vector2(.76f, .4f), new Vector2(300, 72), Cyan);
                CreateButton("DevAdFailButton", root.transform, "광고 실패", new Vector2(.39f, .27f), new Vector2(300, 70), Panel);
                CreateButton("DevAdCancelButton", root.transform, "광고 취소", new Vector2(.61f, .27f), new Vector2(300, 70), Panel);
                CreateLabel("OperationalEconomyBlocked", root.transform, "운영: 계정 서버/광고 SDK 미연결 · 지급/카운터/초기화 시각 Blocked", 25, new Vector2(.5f, .17f), new Vector2(.5f, .17f), new Vector2(1450, 45), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("EconomyDevBackButton", root.transform, "계정/저장", new Vector2(.12f, .08f), new Vector2(270, 64), Panel);
            });
        }

        private static void UpdateM6GrowthEconomyPrefabs()
        {
            MutatePrefab("SC01_Lobby", root =>
            {
                EnsureButton(root, "AccountLinkButton", "계정 · 저장", new Vector2(.82f, .09f), new Vector2(250, 72), Panel);
                SetRect(root.transform.Find("ExitButton") as RectTransform, new Vector2(.68f, .09f), new Vector2(220, 72));
            });
            MutatePrefab("SC10A_AccountUpgrade", root =>
            {
                EnsureLabel(root, "AccountPrototypeNotice", "프로토타입 값 · 적용/구매 잠김", 25, new Vector2(.5f, .32f), new Vector2(1250, 52));
            });
            MutatePrefab("SC10B_CharacterUpgrade", root =>
            {
                for (var i = 1; i <= 2; i++)
                {
                    var old = root.transform.Find($"CharacterCard_{i:00}");
                    if (old != null) old.name = $"AnimalGrowthCard_{i:00}";
                }
                EnsureButton(root, "AnimalGrowthCard_03", "DEV_SPECIAL\n액티브/패시브 Lv.0/5", new Vector2(.75f, .56f), new Vector2(430, 270), Panel);
                SetRect(root.transform.Find("AnimalGrowthCard_01") as RectTransform, new Vector2(.25f, .56f), new Vector2(430, 270));
                SetRect(root.transform.Find("AnimalGrowthCard_02") as RectTransform, new Vector2(.5f, .56f), new Vector2(430, 270));
                SetRect(root.transform.Find("CharacterUpgradeState") as RectTransform, new Vector2(.5f, .31f), new Vector2(1500, 70));
            });
            MutatePrefab("SC11_Store", root =>
            {
                SetRect(root.transform.Find("StoreGrowthDetailsButton") as RectTransform, new Vector2(.35f, .55f), new Vector2(470, 260));
                SetRect(root.transform.Find("StoreEmoteDetailsButton") as RectTransform, new Vector2(.65f, .55f), new Vector2(470, 260));
                EnsureLabel(root, "GrowthPackPolicy", "성장 완료 팩: 계정 공통 3트랙만 포함", 24, new Vector2(.5f, .35f), new Vector2(1400, 48));
                EnsureLabel(root, "CommerceSupplyState", "보전 산식/인증 공급 데이터 미설정 · 실판매 Blocked", 24, new Vector2(.5f, .29f), new Vector2(1450, 52));
            });
            MutatePrefab("SC12_EmoteCollection", root =>
            {
                for (var i = 1; i <= 3; i++) SetRect(root.transform.Find($"FreeEmote_{i:00}") as RectTransform, new Vector2(.23f + (i - 1) * .2f, .7f), new Vector2(300, 150));
                var coin = root.transform.Find("CoinEmoteOfferButton")?.GetComponent<Button>();
                if (coin == null) coin = CreateButton("CoinEmoteOfferButton", root.transform, "코인 교환\n인증 가격 없음 · 잠김", new Vector2(.75f, .7f), new Vector2(300, 150), Panel);
                coin.interactable = false;
                var paid = root.transform.Find("PaidEmoteOfferButton")?.GetComponent<Button>();
                if (paid == null) paid = CreateButton("PaidEmoteOfferButton", root.transform, "유료\n인증 가격 없음 · 잠김", new Vector2(.87f, .7f), new Vector2(260, 150), Panel);
                paid.interactable = false;
                EnsureLabel(root, "EmoteAcquisitionPolicy", "무료 / 코인 교환 / 유료 분리 · 인증 공급 데이터만 표시", 23, new Vector2(.5f, .55f), new Vector2(1450, 42));
            });
            MutatePrefab("SC16_Result", root =>
            {
                SetRect(root.transform.Find("ResultSummary") as RectTransform, new Vector2(.5f, .64f), new Vector2(1450, 150));
                EnsureLabel(root, "ResultRewardBreakdownDetail", "기본 B: 서버 미확정 | 시간 T: 서버 미확정 | 부가 O: 서버 미확정\n광고 추가 B: 지급 없음 | 총액 2B+T+O: 서버 승인 대기", 25, new Vector2(.5f, .45f), new Vector2(1450, 86));
                EnsureLabel(root, "ResultAdQuotaState", "오늘 남은 횟수: 계정 서버 조회 불가 · 초기화 시각 미확정", 23, new Vector2(.5f, .35f), new Vector2(1300, 44));
                var button = root.transform.Find("ResultDoubleBaseAdButton")?.GetComponent<Button>();
                if (button == null) button = CreateButton("ResultDoubleBaseAdButton", root.transform, "기본 B 1회 추가 · 지급 불가", new Vector2(.5f, .27f), new Vector2(430, 70), Panel);
                button.interactable = false;
                SetRect(root.transform.Find("RetryButton") as RectTransform, new Vector2(.39f, .14f), new Vector2(340, 80));
                SetRect(root.transform.Find("LobbyButton") as RectTransform, new Vector2(.61f, .14f), new Vector2(340, 80));
            });
            MutatePrefab("RewardedAdModal", root =>
            {
                EnsureLabel(root, "RewardedAdPolicy", "공용 일일 3회", 22, new Vector2(.5f, .66f), new Vector2(780, 40));
                EnsureLabel(root, "RewardedAdServerState", "잔여/초기화 계정 서버 조회 불가", 21, new Vector2(.5f, .59f), new Vector2(800, 40));
                var watch = root.GetComponentsInChildren<Button>(true).FirstOrDefault(x => x.name == "RewardedAdWatchButton");
                if (watch != null) watch.interactable = false;
            });
        }

        private static void AttachM6GrowthEconomyToLobby(GameObject ui, Scene scene, Transform host)
        {
            foreach (var screenName in new[] { "SC20_AccountAndSave", "SC21_EconomyDevPreview" })
            {
                if (host.Find(screenName) != null) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/{screenName}.prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(host, false);
            }
            var presenter = ui.GetComponent<GrowthEconomyUiPresenter>() ?? ui.AddComponent<GrowthEconomyUiPresenter>();
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("accountUpgradeCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>(AccountCatalogPath);
            serialized.FindProperty("policyCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GrowthEconomyPolicyCatalog>(EconomyPolicyPath);
            serialized.FindProperty("services").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ExternalServiceConfiguration>(ServiceConfigPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<string> ValidateM6GrowthEconomyFollowup(List<string> errors)
        {
            var account = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>(AccountCatalogPath);
            var policy = AssetDatabase.LoadAssetAtPath<GrowthEconomyPolicyCatalog>(EconomyPolicyPath);
            if (account == null || !account.PrototypeValues || account.Tracks.Count != 3) errors.Add("Account common prototype tracks are missing.");
            var expectedDeltas = new[] { 8f, 8f, -6f };
            if (account != null && account.Tracks.Select((x, i) => Math.Abs(x.TotalPercentDelta - expectedDeltas[i]) < .01f).Any(x => !x))
                errors.Add("Account prototype deltas must be +8/+8/-6 percent.");
            if (policy == null) errors.Add("M6 growth/economy policy asset is missing.");
            if (policy != null)
            {
                if (policy.AnimalGrowth.Count != 3 || policy.AnimalGrowth.Any(x => !x.HasTwoLevelFiveTracks || x.IsOperationallyConfigured))
                    errors.Add("Animal growth must expose two Lv.5 tracks while remaining locked until effects/mastery/costs exist.");
                var pack = policy.GrowthCompletionPack;
                if (pack.IncludedAccountTrackIds.Count != 3 || pack.IncludesAnimalGrowth || pack.IncludesAnimalUnlocks || pack.CosmeticCompensationFormulaConfigured || pack.CanSell)
                    errors.Add("Growth completion pack scope/sale gate mismatch.");
                if (policy.EmoteAcquisitions.Count(x => x.AcquisitionKind == EmoteAcquisitionKind.Free) != 3 ||
                    policy.EmoteAcquisitions.Count(x => x.AcquisitionKind == EmoteAcquisitionKind.CoinExchange) != 1 ||
                    policy.EmoteAcquisitions.Count(x => x.AcquisitionKind == EmoteAcquisitionKind.Paid) != 1)
                    errors.Add("Emote acquisition categories mismatch.");
                if (policy.SharedRewardedAdDailyLimit != 3 || policy.LobbyGeneralCoinDraft != 150 || policy.DailyResetAuthority != "ACCOUNT_SERVER")
                    errors.Add("Shared rewarded-ad policy mismatch.");
            }

            var guest = AccountAccessSnapshot.GuestDisconnected;
            if (!guest.CanStartGuestCampaign || guest.CanUseOnline || guest.CanPurchase) errors.Add("Guest account gates are invalid.");
            var save = new SaveConflictSelectionService(
                new CampaignRecordSummary(SaveRecordSource.Local, true, false, 1, 1, 1),
                new CampaignRecordSummary(SaveRecordSource.Cloud, false, false, 0, 0, 0), false);
            if (!save.TrySelect(SaveRecordSource.Local, true) || save.TrySelect(SaveRecordSource.Cloud, true)) errors.Add("Save source selection gate mismatch.");
            var reward = new CampaignCoinRewardBreakdown(100, 25, 10, true);
            if (reward.TotalWithoutAd != 135 || reward.AdAdditionalBaseB != 100 || reward.TotalWithApprovedAd != 235) errors.Add("Campaign reward breakdown formula mismatch.");
            var disconnected = new RewardedAdGrantCoordinator(new DisconnectedRewardLedgerAdapter(3));
            if (disconnected.Begin(new RewardedAdGrantRequest("A", "R", RewardedAdRewardType.CampaignBaseCoinDouble, "P", "Q", 100)) != RewardGrantStatus.Unavailable || disconnected.Quota.RemainingCount.HasValue)
                errors.Add("Disconnected reward ledger fabricated availability or quota.");

            var dev = new RewardedAdGrantCoordinator(new DevRewardLedgerAdapter(0, 3, "2030-01-02T00:00:00Z"));
            var prelogin = new RewardedAdGrantRequest(string.Empty, "R0", RewardedAdRewardType.LobbyGeneralCoinDraft, "P0", "Q0", 150);
            if (dev.Begin(prelogin) != RewardGrantStatus.LoginRequired) errors.Add("Pre-login reward request was not gated.");
            var first = new RewardedAdGrantRequest("DEV", "R1", RewardedAdRewardType.CampaignBaseCoinDouble, "P1", "Q1", 100);
            if (dev.Begin(first) != RewardGrantStatus.Pending || dev.Complete("Q1", RewardGrantStatus.Approved) != RewardGrantStatus.Approved) errors.Add("DEV result reward approval failed.");
            var duplicate = new RewardedAdGrantRequest("DEV", "R1", RewardedAdRewardType.CampaignBaseCoinDouble, "P2", "Q2", 100);
            if (dev.Begin(duplicate) != RewardGrantStatus.Duplicate) errors.Add("Same result duplicate was not rejected.");
            var concurrentA = new RewardedAdGrantRequest("DEV", "R2", RewardedAdRewardType.LobbyGeneralCoinDraft, "P3", "Q3", 150);
            var concurrentB = new RewardedAdGrantRequest("DEV", "R2", RewardedAdRewardType.LobbyGeneralCoinDraft, "P4", "Q4", 150);
            if (dev.Begin(concurrentA) != RewardGrantStatus.Pending || dev.Begin(concurrentB) != RewardGrantStatus.Busy) errors.Add("Concurrent tap was not blocked.");
            dev.Complete("Q3", RewardGrantStatus.Cancelled);
            var failed = new RewardedAdGrantRequest("DEV", "R3", RewardedAdRewardType.LobbyGeneralCoinDraft, "P5", "Q5", 150);
            dev.Begin(failed); dev.Complete("Q5", RewardGrantStatus.Failed);
            foreach (var tuple in new[] { ("R4", "P6", "Q6"), ("R5", "P7", "Q7") })
            {
                var request = new RewardedAdGrantRequest("DEV", tuple.Item1, RewardedAdRewardType.LobbyGeneralCoinDraft, tuple.Item2, tuple.Item3, 150);
                dev.Begin(request); dev.Complete(tuple.Item3, RewardGrantStatus.Approved);
            }
            var fourth = new RewardedAdGrantRequest("DEV", "R6", RewardedAdRewardType.LobbyGeneralCoinDraft, "P8", "Q8", 150);
            if (dev.Quota.UsedCount != 3 || dev.Quota.RemainingCount != 0 || dev.VerifiedCoinBalance != 400 || dev.Begin(fourth) != RewardGrantStatus.QuotaExceeded)
                errors.Add("DEV shared limit/balance/failure cancellation behavior mismatch.");

            var animalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC10B_CharacterUpgrade.prefab");
            var storePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC11_Store.prefab");
            var emotePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC12_EmoteCollection.prefab");
            var resultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC16_Result.prefab");
            var gameplayHud = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC15_GameplayHud.prefab");
            if (animalPrefab == null || animalPrefab.GetComponentsInChildren<Button>(true).Count(x => x.name.StartsWith("AnimalGrowthCard_", StringComparison.Ordinal)) != 3) errors.Add("Animal growth UI schema missing.");
            if (storePrefab == null || storePrefab.GetComponentsInChildren<Text>(true).All(x => x.name != "CommerceSupplyState")) errors.Add("Store sale gate label missing.");
            if (emotePrefab == null || emotePrefab.GetComponentsInChildren<Button>(true).Any(x => (x.name == "CoinEmoteOfferButton" || x.name == "PaidEmoteOfferButton") && x.interactable)) errors.Add("Unauthenticated emote offers must be locked.");
            if (resultPrefab == null || resultPrefab.GetComponentsInChildren<Button>(true).FirstOrDefault(x => x.name == "ResultDoubleBaseAdButton")?.interactable != false) errors.Add("Result ad grant must be locked operationally.");
            if (gameplayHud != null && gameplayHud.GetComponentsInChildren<Transform>(true).Any(x => x.name.Contains("RewardedAd", StringComparison.OrdinalIgnoreCase) || x.name.Contains("DoubleBaseAd", StringComparison.OrdinalIgnoreCase)))
                errors.Add("Rewarded ad entry leaked into an active run HUD.");

            var lines = new List<string>
            {
                "accountCommonTracks=3", "accountPrototypeDeltas=+8,+8,-6", "accountPrototypeLabel=true", "accountUpgradeOperational=false",
                "animalGrowthTracks=active+passive", "animalGrowthMaxLevel=5", "animalEffectMasteryPriceConfigured=false", "animalGrowthPurchaseApply=false",
                "sharedCoinAndAnimalMasterySchema=true", "animalUnlockSource=CAMPAIGN_PROGRESS",
                "growthPackAccountTracksOnly=true", "growthPackAnimalGrowth=false", "growthPackAnimalUnlock=false", "cosmeticCompensationFormulaConfigured=false", "growthPackSale=false",
                "guestCampaign=true", "googleSdkConnected=false", "fakeLoginSuccess=false", "onlinePurchaseBeforeLink=false",
                "offlineLocalAndVerifiedOnlineCoinSeparated=true", "localCloudRecordChoice=true",
                "emoteRoutes=free:3,coin:1,paid:1", "authenticatedOfferSupply=false", "operationalPriceOwnershipShown=false",
                "rewardedAdSharedDailyLimit=3", "lobbyGeneralCoinDraft=150", "lobbyAdDuringRun=false", "campaignAdAddsBaseBOnly=true", "rewardFormula=2B+T+O",
                "rewardIdempotency=accountId+resultId+rewardType", "rewardProofAndGrantRequestId=true", "concurrentTapBlocked=true",
                "quotaConsumedOnlyOnApprovedGrant=true", "failedCancelledConsumeQuota=false", "dailyResetAuthority=ACCOUNT_SERVER", "localClockAuthority=false",
                "operationalRewardGrant=BLOCKED", "devOnlyLedgerBalance=400", "devOnlyLedgerUsed=3", "duplicateResult=REJECTED", "preLoginReward=REJECTED"
            };
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
            File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "animol-m6-economy-validation.txt"), lines.Concat(new[] { "errors=" + string.Join(" | ", errors) }));
            return lines;
        }

        public static string OpenM6AccountSaveForCapture(bool showConflict)
        {
            InvokeRuntimeButton("AccountLinkButton");
            if (showConflict) InvokeRuntimeButton("SaveConflictDevPreviewButton");
            PrepareUiCameraCapture();
            return $"accountSave conflictPreview={showConflict} googleSdk=false";
        }

        public static string OpenM6AnimalGrowthForCapture()
        {
            InvokeRuntimeButton("UpgradeButton");
            InvokeRuntimeButton("UpgradeHubCharacterButton");
            PrepareUiCameraCapture();
            return "animalGrowth activePassive=Lv5 operational=false";
        }

        public static string OpenM6EconomyDevForCapture()
        {
            InvokeRuntimeButton("AccountLinkButton");
            InvokeRuntimeButton("EconomyDevPreviewButton");
            InvokeRuntimeButton("DevResultApproveButton");
            InvokeRuntimeButton("DevDuplicateButton");
            PrepareUiCameraCapture();
            return "economyDev approved=1 duplicate=blocked operational=false";
        }

        private static void PrepareUiCameraCapture()
        {
            var camera = Camera.main;
            if (camera == null) return;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
        }

        public static string InspectGrowthEconomyPolicyScript()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/ANIMOL/Scripts/Core/GrowthEconomyPolicyCatalog.cs");
            return script == null ? "script-missing" : script.GetClass() == null ? "class-missing" : script.GetClass().AssemblyQualifiedName;
        }
    }
}
