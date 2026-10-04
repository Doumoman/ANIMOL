using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;
using ANIMOL.Core;
using ANIMOL.PortraitArtV1;
using ANIMOL.Typography;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ANIMOL.AnimalUpgradePhase1.Tests
{
    // Runs the operational prefab in the real Lobby. All injected growth values are TEST ONLY.
    // These tests cannot establish server pricing, persistence, atomicity or idempotency.
    public sealed class UpgradePhase1AcceptanceTests
    {
        private UiNavigationService nav;
        private AnimalUiPresenter p;
        private UpgradeTransactionTestBackend b;
        private AnimalUiSnapshot state;
        private AnimalUiScreenView V => p.View;

        [UnitySetUp] public IEnumerator Open()
        {
            SceneManager.LoadScene("Lobby");
            for (int i = 0; i < 4; i++) yield return null;
            nav = Object.FindFirstObjectByType<UiNavigationService>();
            p = nav.GetComponentInChildren<AnimalUpgradePhase1Host>(true).Presenter;
            nav.Navigate("SC10_UpgradeHub"); nav.Navigate(AnimalUpgradePhase1Host.ScreenId);
            b = p.gameObject.AddComponent<UpgradeTransactionTestBackend>();
            state = Snapshot(100);
            b.ReadOverride = () => Task.FromResult(state);
            b.QuoteOverride = (id, track) => Task.FromResult(Quote(state, id, track));
            p.SetBackend(b); yield return null;
        }

        private static AnimalUiSnapshot Snapshot(long coins)
        {
            var s = new AnimalUiSnapshot { CoinBalance = coins, Revision = "fixture-only" };
            s.Animals.Add(new AnimalProgress { AnimalId = "Rabbit", Implemented = true, Unlocked = true,
                ActiveLevel = 1, PassiveLevel = 3, MasteryBalance = 17 });
            s.Animals.Add(new AnimalProgress { AnimalId = "Wolf", Implemented = true, Unlocked = true,
                ActiveLevel = 2, PassiveLevel = 4, MasteryBalance = 83 });
            s.Animals.Add(new AnimalProgress { AnimalId = "FlyingSquirrel", Implemented = true, Unlocked = true,
                ActiveLevel = 2, PassiveLevel = 6, MasteryBalance = long.MaxValue });
            return s;
        }
        private static UpgradeQuote Quote(AnimalUiSnapshot s, string id, UpgradeTrack track)
        {
            var a = s.Find(id);
            if (a == null) return new UpgradeQuote { AnimalId = id, Track = track };
            int level = track == UpgradeTrack.Active ? a.ActiveLevel : a.PassiveLevel;
            var q = new UpgradeQuote { AnimalId = id, Track = track, State = UpgradeQuoteState.Ready,
                CurrentLevel = level, NextLevel = level + 1, CurrentEffect = id + " " + track + " 현재 검증용 효과",
                NextEffect = id + " " + track + " 다음 검증용 효과", QuoteToken = "fixture-" + id + track + level };
            q.Costs.Add(new UpgradeCost { CurrencyId = "fixtureCoin", DisplayName = "검증용 코인", Amount = 20 });
            q.Costs.Add(new UpgradeCost { CurrencyId = "fixtureMastery", DisplayName = "검증용 숙련도", Amount = 7 });
            return q;
        }
        private void Choose(string id) => V.RosterContent.GetComponentsInChildren<AnimalCardView>()
            .Single(c => c.gameObject.activeInHierarchy && c.Name.text == p.Catalog.Find(id).DisplayName).Button.onClick.Invoke();
        private void ForcePurchaseClick()
        {
            V.ActiveTrack.UpgradeButton.onClick.Invoke(); V.ModalConfirm.onClick.Invoke();
            Assert.That(b.Requests.Count, Is.Zero);
        }
        private static string Fingerprint(AnimalUiSnapshot s) => s.CoinBalance + "|" + string.Join(";", s.Animals.Select(a =>
            a.AnimalId + ":" + a.Unlocked + ":" + a.Implemented + ":" + a.ActiveLevel + ":" + a.PassiveLevel + ":" + a.MasteryBalance));

        [UnityTest] public IEnumerator ProjectAdapterReadsLiveLedgerAndBrowsingDoesNotChangeExistingState()
        {
            var project = p.GetComponent<AnimalUpgradeProjectAdapter>();
            var owner = nav.GetComponentInChildren<PortraitEntryController>(true);
            var ledger = new ReadOnlyLedgerFixture(); var originalLedger = owner.Ledger;
            Assert.IsTrue(owner.BindVerifiedLedger(ledger));
            var campaign = nav.GetComponent<CampaignUiPresenter>();
            var progression = (CampaignProgressionService)typeof(CampaignUiPresenter).GetField("progression", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(campaign);
            int revision = progression.Progress.Revision;
            var completed = progression.Progress.CompletedStageIds.ToArray();
            var multiplayer = nav.GetComponent<MultiplayerUiPresenter>();
            var fields = new[] { "coopLoadout", "competitiveSelection", "roomPreview", "currentPreview" }
                .Select(n => typeof(MultiplayerUiPresenter).GetField(n, BindingFlags.Instance | BindingFlags.NonPublic)).ToArray();
            var originals = fields.Select(f => f.GetValue(multiplayer)).ToArray();
            var pendingStage = CampaignLaunchContext.Pending;
            b.ReadOverride = () => project.ReadSnapshotAsync(new AnimalUiReadRequest { Mode = AnimalUiMode.CharacterUpgrade }, CancellationToken.None);
            b.QuoteOverride = (id, track) => project.QuoteUpgradeAsync(id, track, CancellationToken.None);
            var before = b.ReadOverride().Result;
            foreach (AnimalRole role in Enum.GetValues(typeof(AnimalRole)))
            {
                V.RoleTabs[(int)role].onClick.Invoke(); yield return null;
                foreach (var animal in p.Catalog.ForRole(role))
                {
                    Choose(animal.Id); yield return null;
                    Assert.That(V.Balances.text, Does.Contain("코인 123").And.Contain(animal.DisplayName + " 숙련도 --"));
                    Assert.IsFalse(V.ActiveTrack.UpgradeButton.interactable || V.PassiveTrack.UpgradeButton.interactable);
                    ForcePurchaseClick();
                }
            }
            Assert.That(Fingerprint(b.ReadOverride().Result), Is.EqualTo(Fingerprint(before)));
            Assert.That(progression.Progress.Revision, Is.EqualTo(revision)); CollectionAssert.AreEqual(completed, progression.Progress.CompletedStageIds);
            for (int i = 0; i < fields.Length; i++) Assert.That(fields[i].GetValue(multiplayer), Is.SameAs(originals[i]));
            Assert.That(CampaignLaunchContext.Pending, Is.SameAs(pendingStage)); Assert.That(ledger.Grants, Is.Zero);
            ledger.Balance = int.MaxValue; p.Refresh(); yield return null;
            Assert.That(V.Balances.text, Does.Contain(int.MaxValue.ToString("N0")));
            owner.BindVerifiedLedger(originalLedger); p.SetBackend(project); p.Back();
            Assert.That(nav.CurrentScreenId, Is.EqualTo("SC10_UpgradeHub"));
        }
        private sealed class ReadOnlyLedgerFixture : IRewardLedgerAdapter
        {
            public int Balance = 123, Grants;
            public bool IsDevelopmentOnly => false;
            public bool IsAvailable => true;
            public int? VerifiedCoinBalance => Balance;
            public DailyAdQuotaSnapshot Quota => DailyAdQuotaSnapshot.Unavailable(3);
            public RewardGrantStatus ConfirmApprovedGrant(RewardedAdGrantRequest request) { Grants++; return RewardGrantStatus.Unavailable; }
        }

        [UnityTest] public IEnumerator DeniedStatesAndIncompleteQuotesDispatchZeroRequests()
        {
            foreach (var denied in new[] { UpgradeQuoteState.Unconfigured, UpgradeQuoteState.Locked, UpgradeQuoteState.Unavailable,
                UpgradeQuoteState.InsufficientFunds, UpgradeQuoteState.Maximum })
            {
                b.QuoteOverride = (id, track) => { var q = Quote(state, id, track); q.State = denied; q.Message = "fixture reason " + denied; return Task.FromResult(q); };
                p.Refresh(); yield return null;
                Assert.IsFalse(V.ActiveTrack.UpgradeButton.interactable, denied.ToString()); ForcePurchaseClick();
                V.ActiveTrack.DetailsButton.onClick.Invoke(); Assert.That(V.ModalBody.text, Does.Contain("fixture reason " + denied));
                V.ModalCancel.onClick.Invoke();
            }
            b.QuoteOverride = (id, track) => Task.FromResult(Quote(state, id, track));
            foreach (bool implemented in new[] { false, true })
            {
                state.Find("Rabbit").Implemented = implemented; state.Find("Rabbit").Unlocked = false;
                p.Refresh(); yield return null; Assert.IsFalse(V.ActiveTrack.UpgradeButton.interactable); ForcePurchaseClick();
            }
            state.Find("Rabbit").Unlocked = true;
            Action<UpgradeQuote>[] incomplete = {
                q => q.CurrentEffect = null, q => q.NextEffect = " ", q => q.Costs.Clear(),
                q => q.QuoteToken = null, q => q.CurrentLevel = -1, q => q.NextLevel = -1,
                q => q.CurrentLevel = 99, q => q.Costs[0].Amount = -1, q => q.Costs[0].CurrencyId = null };
            foreach (var mutate in incomplete)
            {
                UpgradeQuote active = null;
                b.QuoteOverride = (id, track) => { var q = Quote(state, id, track); mutate(q); if (track == UpgradeTrack.Active) active = q; return Task.FromResult(q); };
                p.Refresh(); yield return null; Assert.IsFalse(V.ActiveTrack.UpgradeButton.interactable); ForcePurchaseClick();
                Assert.That(V.ActiveTrack.Cost.text, Does.Not.Contain("무료"));
                if (active.CurrentLevel < 0) Assert.That(V.ActiveTrack.Level.text, Does.StartWith("Lv. 1"), "Keep the known snapshot level when the quote is incomplete.");
            }
            // Explicit free/zero are fixture data, never production offers.
            foreach (bool free in new[] { false, true })
            {
                b.QuoteOverride = (id, track) => { var q = Quote(state, id, track);
                    q.Costs.Clear(); q.IsFree = free;
                    if (!free) q.Costs.Add(new UpgradeCost { CurrencyId = "fixtureCoin", Amount = 0 });
                    return Task.FromResult(q); };
                p.Refresh(); yield return null; Assert.IsTrue(V.ActiveTrack.UpgradeButton.interactable);
                V.ActiveTrack.UpgradeButton.onClick.Invoke(); V.ModalCancel.onClick.Invoke(); Assert.That(b.Requests.Count, Is.Zero);
            }
            p.SetBackend(null); yield return null; Assert.IsFalse(V.ActiveTrack.UpgradeButton.interactable); ForcePurchaseClick();
        }

        [UnityTest] public IEnumerator LateSnapshotsAndQuotesNeverReplaceCurrentAnimalOrMutateFixture()
        {
            string before = Fingerprint(state), draft = JsonUtility.ToJson(p.Draft);
            var lateRead = new TaskCompletionSource<AnimalUiSnapshot>(); int reads = 0;
            b.ReadOverride = () => ++reads == 1 ? lateRead.Task : Task.FromResult(state);
            p.Refresh(); Choose("Wolf"); yield return null;
            string wolf = V.Balances.text + V.ActiveTrack.Level.text + V.PassiveTrack.Level.text + V.ActiveTrack.Effect.text;
            Assert.That(wolf, Does.Contain("83").And.Contain("Wolf Active").And.Contain("Lv. 2").And.Contain("Lv. 4"));
            lateRead.SetResult(Snapshot(999)); yield return null; yield return null;
            Assert.That(V.Balances.text + V.ActiveTrack.Level.text + V.PassiveTrack.Level.text + V.ActiveTrack.Effect.text, Is.EqualTo(wolf));
            var lateQuote = new TaskCompletionSource<UpgradeQuote>();
            b.ReadOverride = () => Task.FromResult(state);
            b.QuoteOverride = (id, track) => id == "Rabbit" && track == UpgradeTrack.Active ? lateQuote.Task : Task.FromResult(Quote(state, id, track));
            Choose("Rabbit"); Choose("Wolf"); yield return null;
            lateQuote.SetResult(Quote(Snapshot(999), "Rabbit", UpgradeTrack.Active)); yield return null; yield return null;
            Assert.That(V.Balances.text + V.ActiveTrack.Level.text + V.PassiveTrack.Level.text + V.ActiveTrack.Effect.text, Is.EqualTo(wolf));
            Assert.That(V.PassiveTrack.Effect.text, Does.Contain("Wolf Passive"));
            V.RoleTabs[1].onClick.Invoke(); V.RoleTabs[2].onClick.Invoke(); V.RoleTabs[0].onClick.Invoke();
            Assert.That(Fingerprint(state), Is.EqualTo(before)); Assert.That(JsonUtility.ToJson(p.Draft), Is.EqualTo(draft));
            Assert.That(b.Requests.Count, Is.Zero); p.Back(); Assert.That(nav.CurrentScreenId, Is.EqualTo("SC10_UpgradeHub"));
        }

        [UnityTest] public IEnumerator ReadFailureAndDelayedReadNeverAllowPurchase()
        {
            var read = new TaskCompletionSource<AnimalUiSnapshot>(); b.ReadOverride = () => read.Task;
            p.Refresh(); Assert.IsFalse(V.ActiveTrack.UpgradeButton.interactable); ForcePurchaseClick();
            read.SetException(new InvalidOperationException("fixture read failure")); yield return null; yield return null;
            Assert.That(V.Status.text, Does.Contain("fixture read failure")); ForcePurchaseClick();
            b.ReadOverride = () => Task.FromResult(state); V.Refresh.onClick.Invoke(); yield return null;
            Assert.IsTrue(V.ActiveTrack.UpgradeButton.interactable);
        }

        [UnityTest] public IEnumerator DefinitiveRejectionRefreshesChangedStateAndAcceptedUsesFixtureResultOnly()
        {
            int accepted = 0; p.UpgradeAccepted += _ => accepted++;
            V.ActiveTrack.UpgradeButton.onClick.Invoke(); V.ModalConfirm.onClick.Invoke();
            Assert.That(b.Requests[0].ExpectedCurrentLevel, Is.EqualTo(1));
            state = Snapshot(73); state.Find("Rabbit").ActiveLevel = 2; state.Find("Rabbit").MasteryBalance = 10;
            b.Completion.SetResult(new AnimalUiCommitResult { Status = CommitStatus.Rejected, Message = "fixture stale price and level" });
            yield return null; yield return null;
            Assert.That(V.Status.text, Is.EqualTo("fixture stale price and level")); Assert.That(V.ActiveTrack.Level.text, Does.StartWith("Lv. 2"));
            Assert.That(accepted, Is.Zero); Assert.That(b.Requests.Count, Is.EqualTo(1));
            V.ActiveTrack.UpgradeButton.onClick.Invoke(); V.ModalConfirm.onClick.Invoke();
            Assert.That(b.Requests[1].ExpectedCurrentLevel, Is.EqualTo(2)); Assert.That(b.Requests[1].ActionId, Is.Not.EqualTo(b.Requests[0].ActionId));
            state = Snapshot(53); state.Find("Rabbit").ActiveLevel = 3; state.Find("Rabbit").MasteryBalance = 3;
            b.Completion.SetResult(new AnimalUiCommitResult { Status = CommitStatus.Accepted, Snapshot = state, Message = "fixture accepted" });
            yield return null; yield return null;
            Assert.That(accepted, Is.EqualTo(1)); Assert.That(V.Balances.text, Does.Contain("코인 53").And.Contain("숙련도 3"));
            Assert.That(V.ActiveTrack.Level.text, Does.StartWith("Lv. 3")); Assert.That(V.PassiveTrack.Level.text, Does.StartWith("Lv. 3"));
        }

        [UnityTest] public IEnumerator DisabledPendingScreenKeepsRequestAndLateResultDoesNotNavigate()
        {
            int accepted = 0; p.UpgradeAccepted += _ => accepted++;
            V.ActiveTrack.UpgradeButton.onClick.Invoke(); V.ModalConfirm.onClick.Invoke();
            var first = b.Requests[0]; var completion = b.Completion;
            p.gameObject.SetActive(false);
            completion.SetResult(new AnimalUiCommitResult { Status = CommitStatus.Accepted, Snapshot = state });
            yield return null; yield return null;
            Assert.That(accepted, Is.Zero); Assert.That(nav.CurrentScreenId, Is.EqualTo(AnimalUpgradePhase1Host.ScreenId));
            p.gameObject.SetActive(true); yield return null;
            Assert.IsTrue(p.IsCommitting); Assert.IsFalse(p.OpenUpgrade("Wolf"));
            V.ModalCancel.onClick.Invoke(); Assert.IsTrue(p.IsCommitting);
            V.ModalConfirm.onClick.Invoke();
            Assert.That(JsonUtility.ToJson(b.Requests[1]), Is.EqualTo(JsonUtility.ToJson(first)));
            b.Completion.SetResult(new AnimalUiCommitResult { Status = CommitStatus.Rejected, Message = "fixture definitive resolution" });
            yield return null; yield return null; Assert.IsFalse(p.IsCommitting); Assert.That(accepted, Is.Zero);
        }

        [UnityTest] public IEnumerator NumericTypeBoundsAndLongTextRemainReadableInBothSafeGameViews()
        {
#if UNITY_EDITOR
            var setup = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t => t != null);
            state.CoinBalance = int.MaxValue; // Existing ledger's numeric type bound, not an approved business maximum.
            string longEffect = string.Join("\n", Enumerable.Range(1, 24).Select(i => "검증용 장문 " + i + " · 동물 성장 효과와 적용 조건을 생략 없이 상세 화면에서 읽습니다."));
            b.QuoteOverride = (id, track) => { var q = Quote(state, id, track); q.CurrentEffect = longEffect + " CURRENT-END";
                q.NextEffect = longEffect + " NEXT-END"; q.Costs.Clear();
                for (int i = 0; i < 8; i++) q.Costs.Add(new UpgradeCost { CurrencyId = "fixture-" + i, DisplayName = "검증용 비용 전체 항목 " + i, Amount = long.MaxValue });
                return Task.FromResult(q); };
            var output = Path.Combine(Path.GetTempPath(), "ANIMOL-UpgradePhase3-Stress"); Directory.CreateDirectory(output);
            foreach (int height in new[] { 1920, 2400 })
            {
                setup.GetMethod("SelectFixedResolution").Invoke(null, new object[] { 1080, height, "ANIMOL Phase3 QA" });
                yield return null; yield return null;
                var safe = nav.GetComponentInChildren<SafeAreaLayout>(); safe.enabled = false;
                var rect = (RectTransform)safe.transform;
                rect.anchorMin = new Vector2(48f / 1080, 96f / height); rect.anchorMax = new Vector2(1 - 48f / 1080, 1 - 120f / height);
                p.OpenUpgrade("FlyingSquirrel"); yield return null; yield return null;
                Assert.That(V.Balances.text, Does.Contain(int.MaxValue.ToString("N0")).And.Contain(long.MaxValue.ToString("N0")));
                foreach (var bridge in p.GetComponentsInChildren<PixelTextBridge>()) Assert.IsFalse(bridge.Overflow, bridge.name + ": " + bridge.Source.text);
                yield return Capture(Path.Combine(output, height + "_fixture_bounds.png"));
                V.ActiveTrack.DetailsButton.onClick.Invoke(); yield return null; yield return null;
                Assert.That(V.ModalBody.text, Does.Contain(longEffect).And.Contain("CURRENT-END").And.Contain("NEXT-END").And.Contain("항목 7"));
                yield return InspectModal(output, height + "_fixture_details", rect);
                V.ModalCancel.onClick.Invoke(); V.ActiveTrack.UpgradeButton.onClick.Invoke(); yield return null; yield return null;
                Assert.That(V.ModalBody.text, Does.Contain("하늘다람쥐").And.Contain("액티브").And.Contain("2 → 3").And.Contain("항목 7").And.Contain("NEXT-END"));
                yield return InspectModal(output, height + "_fixture_confirm", rect);
                V.ModalCancel.onClick.Invoke(); Assert.That(b.Requests.Count, Is.Zero);
                safe.enabled = true; safe.Apply();
            }
#else
            Assert.Ignore("Requires Editor Game View"); yield break;
#endif
        }
        private IEnumerator InspectModal(string output, string name, RectTransform safe)
        {
            var scroll = V.ModalBody.GetComponentInParent<ScrollRect>();
            Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(.01f), "Every new modal starts with animal and track visible.");
            var bridge = V.ModalBody.GetComponent<PixelTextBridge>(); bridge.Synchronize(); Assert.IsFalse(bridge.Overflow);
            foreach (var button in new[] { V.ModalCancel, V.ModalConfirm })
            {
                var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach (var c in corners) Assert.IsTrue(safe.rect.Contains(safe.InverseTransformPoint(c)));
            }
            scroll.verticalNormalizedPosition = 1; yield return null; yield return Capture(Path.Combine(output, name + "_top.png"));
            scroll.verticalNormalizedPosition = 0; yield return null; yield return Capture(Path.Combine(output, name + "_bottom.png"));
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0).Within(.01f));
        }
        private static IEnumerator Capture(string path)
        {
            yield return new WaitForEndOfFrame(); var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.Destroy(texture);
        }
    }
}
