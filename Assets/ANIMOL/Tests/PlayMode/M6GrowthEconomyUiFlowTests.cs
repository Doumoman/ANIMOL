using System.Collections;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ANIMOL.Tests
{
    public sealed class M6GrowthEconomyUiFlowTests
    {
        [UnityTest]
        public IEnumerator GuestAccountAndSaveChoice_DoNotFakeLoginOrCloudSync()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;
            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var presenter = Object.FindFirstObjectByType<GrowthEconomyUiPresenter>();
            var root = navigation.transform;

            Click(root, "AccountLinkButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC20_AccountAndSave"));
            Assert.That(Text(root, "AccountIdentityState"), Does.Contain("게스트"));
            Assert.That(Text(root, "AccountGateState"), Does.Contain("온라인/구매"));
            Click(root, "GoogleLinkButton");
            Assert.That(Find(root, "ServiceErrorModal").activeSelf, Is.True);
            Assert.That(Text(root, "ServiceErrorTitle"), Does.Contain("SDK 미연결"));
            Assert.That(presenter.IdentityState, Is.EqualTo(AccountIdentityState.Guest));
            Click(root, "ServiceErrorBackButton");

            Click(root, "SaveConflictDevPreviewButton");
            Assert.That(Text(root, "LocalRecordState"), Does.Contain("진행 4"));
            Assert.That(Text(root, "CloudRecordState"), Does.Contain("서버 검증"));
            Click(root, "SelectLocalSaveButton");
            Assert.That(Text(root, "SaveSelectionState"), Does.Contain("Local"));
            Assert.That(Text(root, "SaveSelectionState"), Does.Contain("실제 동기화 없음"));
        }

        [UnityTest]
        public IEnumerator GrowthStoreAndEmotes_ShowPrototypeAndLockedOperationalState()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;
            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var root = navigation.transform;

            Click(root, "UpgradeButton");
            Click(root, "UpgradeHubAccountButton");
            Assert.That(Text(root, "AccountPrototypeNotice"), Does.Contain("프로토타입"));
            Assert.That(ButtonText(root, "AccountTrack_01"), Does.Contain("+8%"));
            Assert.That(Button(root, "AccountTrack_01").interactable, Is.False);
            Click(root, "AccountUpgradeBackButton");
            Click(root, "UpgradeHubCharacterButton");
            Assert.That(ButtonText(root, "AnimalGrowthCard_01"), Does.Contain("액티브 Lv.0/5"));
            Assert.That(ButtonText(root, "AnimalGrowthCard_01"), Does.Contain("가격 미설정"));
            Assert.That(Button(root, "AnimalGrowthCard_01").interactable, Is.False);

            navigation.Navigate("SC11_Store");
            Assert.That(Text(root, "GrowthPackPolicy"), Does.Contain("계정 공통 3트랙만"));
            Assert.That(Text(root, "CommerceSupplyState"), Does.Contain("실판매 Blocked"));
            navigation.Navigate("SC12_EmoteCollection");
            Assert.That(Text(root, "EmoteAcquisitionPolicy"), Does.Contain("무료 / 코인 교환 / 유료"));
            Assert.That(Button(root, "CoinEmoteOfferButton").interactable, Is.False);
            Assert.That(Button(root, "PaidEmoteOfferButton").interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator DevLedgerUi_VerifiesBalanceLimitDuplicateFailureCancelAndConcurrentTap()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;
            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var presenter = Object.FindFirstObjectByType<GrowthEconomyUiPresenter>();
            var root = navigation.transform;
            Click(root, "AccountLinkButton");
            Click(root, "EconomyDevPreviewButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("SC21_EconomyDevPreview"));
            Assert.That(Text(root, "DevRewardBreakdown"), Does.Contain("총액 2B+T+O 235"));

            Click(root, "DevResultApproveButton");
            Assert.That(presenter.DevVerifiedBalance, Is.EqualTo(100));
            Assert.That(presenter.DevRemainingAdCount, Is.EqualTo(2));
            Click(root, "DevDuplicateButton");
            Assert.That(presenter.LastDevStatus, Is.EqualTo(RewardGrantStatus.Duplicate));
            Assert.That(presenter.DevRemainingAdCount, Is.EqualTo(2));
            Click(root, "DevAdFailButton");
            Click(root, "DevAdCancelButton");
            Assert.That(presenter.DevRemainingAdCount, Is.EqualTo(2));
            Click(root, "DevConcurrentTapButton");
            Assert.That(presenter.LastDevStatus, Is.EqualTo(RewardGrantStatus.Busy));
            Assert.That(presenter.DevRemainingAdCount, Is.EqualTo(2));
            Click(root, "DevGeneralApproveButton");
            Click(root, "DevGeneralApproveButton");
            Assert.That(presenter.DevVerifiedBalance, Is.EqualTo(400));
            Assert.That(presenter.DevRemainingAdCount, Is.EqualTo(0));
            Click(root, "DevGeneralApproveButton");
            Assert.That(presenter.LastDevStatus, Is.EqualTo(RewardGrantStatus.QuotaExceeded));
            Assert.That(Text(root, "DevGrantHistory"), Does.Contain("Failed:0"));
            Assert.That(Text(root, "DevGrantHistory"), Does.Contain("Cancelled:0"));
        }

        [UnityTest]
        public IEnumerator ResultScreen_ShowsFormulaAndServerBlockedQuotaWithoutGrant()
        {
            SceneManager.LoadScene("Results");
            yield return null;
            var root = Object.FindFirstObjectByType<Canvas>().transform;
            Assert.That(Text(root, "ResultRewardBreakdownDetail"), Does.Contain("기본 B"));
            Assert.That(Text(root, "ResultRewardBreakdownDetail"), Does.Contain("총액 2B+T+O"));
            Assert.That(Text(root, "ResultAdQuotaState"), Does.Contain("계정 서버 조회 불가"));
            Assert.That(Button(root, "ResultDoubleBaseAdButton").interactable, Is.False);
        }

        private static string Text(Transform root, string name) => root.GetComponentsInChildren<Text>(true).First(x => x.name == name).text;
        private static string ButtonText(Transform root, string name) => Button(root, name).GetComponentInChildren<Text>(true).text;
        private static Button Button(Transform root, string name) => root.GetComponentsInChildren<Button>(true).First(x => x.name == name);
        private static void Click(Transform root, string name) => Button(root, name).onClick.Invoke();
        private static GameObject Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(x => x.name == name).gameObject;
    }
}
