using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ANIMOL.AnimalMultiplayerPhase3.Tests
{
    public sealed class MultiplayerPhase3TransactionTests
    {
        private AnimalMultiplayerPhase3Host host;
        private AnimalUiPresenter p;
        private UiNavigationService nav;
        private MultiplayerTransactionFixture backend;
        private MultiplayerSelectionRequest context;
        private int accepted;
        private int originalParticipants;
        private int waitingRoomNavigations;
        private string originalHostData;
        private static string Output => Path.Combine(Path.GetTempPath(), "ANIMOL-MultiplayerPhase3-Task3-Transactions");
        [Serializable] private sealed class Evidence
        {
            public string Test, Scope = "Unity fixture only; no production service", Context;
            public int ProtectedEntryCalls, PresenterAcceptedEvents, WaitingRoomNavigations;
            public string[] Requests;
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            SceneManager.LoadScene("Lobby"); yield return null; yield return null; yield return null; yield return null;
            nav = Object.FindFirstObjectByType<UiNavigationService>();
            host = nav.GetComponent<AnimalMultiplayerPhase3Entry>().Host; p = host.Presenter;
            originalParticipants = nav.GetComponent<MultiplayerUiPresenter>().PreviewParticipantCount;
            originalHostData = JsonUtility.ToJson(nav.GetComponent<MultiplayerUiPresenter>());
            waitingRoomNavigations = 0; nav.ScreenChanged += ObserveNavigation;
            nav.Navigate("SC05_CompetitiveHub");
            context = new MultiplayerSelectionRequest
            {
                ModeId = "TEST_MODE_A", DisplayName = "TEST ONLY", EntryIntent = "TEST_INTENT",
                InitialLoadout = new AnimalLoadout { Ground = "Rabbit", Special = "DreamFox", Air = "Swallow" },
                Requirements = new SelectionRequirements { RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air },
                    RepresentativeRole = AnimalRole.Special, PolicyRevision = "TEST_POLICY" }
            };
            backend = host.gameObject.AddComponent<MultiplayerTransactionFixture>(); backend.Catalog = p.Catalog;
            backend.OnRead = c => Task.FromResult(Snapshot(c));
            backend.OnSubmit = r => Task.FromResult(Receipt(r));
            p.SetBackend(backend); accepted = 0; p.MultiplayerAccepted += OnAccepted;
            Assert.IsTrue(host.Open(context)); yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, TestContext.CurrentContext.Test.Name + ".json"), JsonUtility.ToJson(new Evidence
            {
                Test = TestContext.CurrentContext.Test.Name, Context = JsonUtility.ToJson(context),
                ProtectedEntryCalls = backend.Requests.Count, PresenterAcceptedEvents = accepted,
                WaitingRoomNavigations = waitingRoomNavigations, Requests = backend.Requests.Select(r => JsonUtility.ToJson(r)).ToArray()
            }, true));
            nav.ScreenChanged -= ObserveNavigation;
            p.MultiplayerAccepted -= OnAccepted;
            Assert.That(waitingRoomNavigations, Is.Zero);
            Assert.That(JsonUtility.ToJson(nav.GetComponent<MultiplayerUiPresenter>()), Is.EqualTo(originalHostData));
            Assert.That(nav.GetComponent<MultiplayerUiPresenter>().PreviewParticipantCount, Is.EqualTo(originalParticipants));
            // No operational receipt consumer or waiting-room navigation exists in this project.
            Assert.That(nav.CurrentScreenId, Is.EqualTo(AnimalMultiplayerPhase3Host.ScreenId));
            yield return null;
        }
        private void OnAccepted(AnimalUiCommitResult result) => accepted++;
        private void ObserveNavigation(string id) { if (id == "SC06_MatchRoom") waitingRoomNavigations++; }
        private static IEnumerator Capture(string name)
        {
            Directory.CreateDirectory(Output); yield return null; yield return null; yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG()); Object.Destroy(image);
        }
        private AnimalUiSnapshot Snapshot(MultiplayerSelectionRequest c)
        {
            var snapshot = new AnimalUiSnapshot { Revision = "TEST_READ", ContextId = c.ModeId, PolicyRevision = c.Requirements.PolicyRevision, EntryIntent = c.EntryIntent };
            foreach (var a in p.Catalog.Animals)
                snapshot.Animals.Add(new AnimalProgress { AnimalId = a.Id, Implemented = a.Id != "Otter", HasContextPermission = true,
                    CanUseInContext = true, Unlocked = false, ActiveLevel = 2, PassiveLevel = 4, ActiveDescription = "TEST ACTIVE", PassiveDescription = "TEST PASSIVE" });
            return snapshot;
        }
        private static AnimalUiCommitResult Receipt(SelectionCommitRequest r) => new AnimalUiCommitResult
        { Status = CommitStatus.Accepted, AcceptanceToken = "TEST_ONLY_RECEIPT", AcceptedContextId = r.ContextId,
            AcceptedPolicyRevision = r.PolicyRevision, AcceptedEntryIntent = r.EntryIntent, AcceptedLoadout = r.Loadout.Clone() };
        private void Confirm() { p.View.Primary.onClick.Invoke(); p.View.ModalConfirm.onClick.Invoke(); }

        [UnityTest] public IEnumerator CancelZeroConfirmOnceModalAndPendingBlockMutation()
        {
            p.View.Primary.onClick.Invoke(); Assert.IsTrue(p.View.Modal.activeSelf);
            int reads = backend.Reads; p.Refresh();
            Assert.Throws<InvalidOperationException>(() => p.SetBackend(backend));
            var other = context.Clone(); other.EntryIntent = "OTHER";
            Assert.IsFalse(host.Open(other)); Assert.IsFalse(p.OpenMultiplayer(other));
            Assert.That(host.Context.EntryIntent, Is.EqualTo(context.EntryIntent)); Assert.That(backend.Reads, Is.EqualTo(reads));
            p.View.ModalCancel.onClick.Invoke(); Assert.That(backend.Requests.Count, Is.Zero);
            yield return Capture("fixture_cancel_zero");
            var pending = new TaskCompletionSource<AnimalUiCommitResult>(); backend.OnSubmit = _ => pending.Task;
            Confirm(); Assert.IsTrue(p.IsCommitting);
            string draft = p.Draft.Fingerprint();
            for (int i = 0; i < 5; i++) { p.View.ModalConfirm.onClick.Invoke(); p.Back(); p.View.RoleTabs[2].onClick.Invoke(); p.Refresh(); }
            Assert.IsFalse(host.Open(other)); Assert.That(p.Draft.Fingerprint(), Is.EqualTo(draft));
            Assert.That(backend.Requests.Count, Is.EqualTo(1)); Assert.That(accepted, Is.Zero);
            var request = backend.Requests.Single();
            Assert.IsFalse(string.IsNullOrWhiteSpace(request.ActionId)); Assert.That(request.ContextId, Is.EqualTo(context.ModeId));
            Assert.That(request.SnapshotRevision, Is.EqualTo("TEST_READ")); Assert.That(request.PolicyRevision, Is.EqualTo("TEST_POLICY"));
            Assert.That(request.EntryIntent, Is.EqualTo(context.EntryIntent)); Assert.That(request.Loadout.Fingerprint(), Is.EqualTo(draft));
            Assert.That(context.InitialLoadout.Fingerprint(), Is.EqualTo(draft));
            yield return Capture("fixture_pending_one_request");
            pending.SetResult(Receipt(request)); yield return null;
            Assert.That(accepted, Is.EqualTo(1)); Assert.IsFalse(p.IsCommitting);
            p.View.ModalConfirm.onClick.Invoke(); p.View.Primary.onClick.Invoke();
            host.gameObject.SetActive(false); yield return null; host.gameObject.SetActive(true); yield return null;
            p.View.Primary.onClick.Invoke(); p.View.ModalConfirm.onClick.Invoke();
            Assert.That(accepted, Is.EqualTo(1)); Assert.That(backend.Requests.Count, Is.EqualTo(1));
            yield return Capture("fixture_accepted_event_only");
        }

        [UnityTest] public IEnumerator RefusalReasonSurvivesUntilManualReadAndNextActionIsNew()
        {
            foreach (var status in new[] { CommitStatus.Rejected, CommitStatus.Unavailable })
            {
                backend.OnSubmit = _ => Task.FromResult(new AnimalUiCommitResult { Status = status, Message = "TEST_POLICY_CHANGED" });
                int reads = backend.Reads; Confirm(); yield return null;
                var previous = backend.Requests.Last().ActionId; int calls = backend.Requests.Count;
                Assert.IsFalse(p.View.Primary.interactable); Assert.That(backend.Reads, Is.EqualTo(reads));
                Assert.That(p.View.Status.text, Does.Contain("TEST_POLICY_CHANGED"));
                p.View.Primary.onClick.Invoke(); p.View.ModalConfirm.onClick.Invoke(); Assert.That(backend.Requests.Count, Is.EqualTo(calls));
                p.View.Refresh.onClick.Invoke(); yield return null;
                Assert.That(backend.Reads, Is.EqualTo(reads + 1)); Assert.IsTrue(p.View.Primary.interactable);
                // A new confirmation has a new ID, even if the selection is unchanged.
                backend.OnSubmit = _ => Task.FromResult(new AnimalUiCommitResult { Status = CommitStatus.Rejected });
                Confirm(); yield return null;
                Assert.That(backend.Requests.Last().ActionId, Is.Not.EqualTo(previous));
                p.View.Refresh.onClick.Invoke(); yield return null;
            }
            Assert.That(accepted, Is.Zero);
        }

        [UnityTest] public IEnumerator UnknownAndMalformedReceiptsKeepIdenticalRequestUntilExactReceipt()
        {
            string fault = "timeout";
            backend.OnSubmit = r =>
            {
                if (fault == "timeout") return Task.FromException<AnimalUiCommitResult>(new TimeoutException("TEST_TIMEOUT"));
                var receipt = Receipt(r);
                if (fault == "token") receipt.AcceptanceToken = " ";
                if (fault == "context") receipt.AcceptedContextId = "OTHER";
                if (fault == "policy") receipt.AcceptedPolicyRevision = "OTHER";
                if (fault == "intent") receipt.AcceptedEntryIntent = "OTHER";
                if (fault == "loadout") receipt.AcceptedLoadout.Air = "Bat";
                return Task.FromResult(receipt);
            };
            Confirm(); yield return null;
            yield return Capture("fixture_timeout_unknown");
            foreach (var next in new[] { "token", "context", "policy", "intent", "loadout", "valid" })
            {
                Assert.IsTrue(p.IsCommitting); Assert.That(accepted, Is.Zero);
                Assert.IsFalse(host.Open(context)); p.Back(); p.Refresh();
                Assert.Throws<InvalidOperationException>(() => p.SetBackend(backend));
                fault = next; p.View.ModalConfirm.onClick.Invoke(); yield return null;
            }
            Assert.That(accepted, Is.EqualTo(1)); Assert.That(backend.Requests.Count, Is.EqualTo(7));
            Assert.That(backend.Requests.Select(JsonUtility.ToJson).Distinct().Count(), Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator InactivePendingThenLateReceiptUsesCachedResultWithoutSecondDispatch()
        {
            var pending = new TaskCompletionSource<AnimalUiCommitResult>(); backend.OnSubmit = _ => pending.Task;
            Confirm(); host.gameObject.SetActive(false); yield return null;
            host.gameObject.SetActive(true); yield return null;
            Assert.IsTrue(p.IsCommitting); Assert.IsFalse(host.Open(context));
            p.View.ModalConfirm.onClick.Invoke(); yield return null;
            Assert.That(backend.Requests.Count, Is.EqualTo(1)); Assert.That(accepted, Is.Zero);
            pending.SetResult(Receipt(backend.Requests.Single())); yield return null;
            // The old UI epoch must not emit an acceptance into a reactivated screen.
            Assert.That(accepted, Is.Zero); Assert.IsTrue(p.IsCommitting);
            p.View.ModalConfirm.onClick.Invoke(); yield return null;
            Assert.That(accepted, Is.EqualTo(1)); Assert.That(backend.Requests.Count, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator LateReadCannotReplaceNewModeAndFailedReadClearsOldPermissionDisplay()
        {
            var delayed = new TaskCompletionSource<AnimalUiSnapshot>();
            backend.OnRead = c => c.ModeId == context.ModeId ? delayed.Task : Task.FromResult(Snapshot(c));
            p.Refresh();
            var other = context.Clone(); other.ModeId = "TEST_MODE_B"; other.EntryIntent = "TEST_INTENT_B";
            Assert.IsTrue(host.Open(other)); yield return null;
            delayed.SetResult(Snapshot(context)); yield return null;
            Assert.That(host.Context.ModeId, Is.EqualTo(other.ModeId)); Assert.IsTrue(p.View.Primary.interactable);
            backend.OnRead = _ => Task.FromException<AnimalUiSnapshot>(new InvalidOperationException("TEST_READ_FAILED"));
            p.Refresh(); yield return null;
            Assert.IsFalse(p.View.Primary.interactable); Assert.That(p.View.Status.text, Does.Contain("TEST_READ_FAILED"));
            p.View.Primary.onClick.Invoke(); Assert.That(backend.Requests.Count, Is.Zero);
        }

        [UnityTest] public IEnumerator UnsupportedResultLookupKeepsUnknownAndExplainsNoResubmission()
        {
            backend.ReconciliationAvailable = false;
            backend.OnSubmit = _ => Task.FromResult(new AnimalUiCommitResult { Status = CommitStatus.Unknown, Message = "TEST_ONLY_RESPONSE_LOST" });
            Confirm(); yield return null;
            p.View.ModalConfirm.onClick.Invoke(); yield return null;
            yield return Capture("fixture_reconciliation_unsupported");
            Assert.That(backend.Requests.Count, Is.EqualTo(1)); Assert.IsTrue(p.IsCommitting);
            Assert.That(p.View.ModalBody.text, Does.Contain("멱등"));
            Assert.IsFalse(host.Open(context)); p.Back(); p.Refresh(); Assert.That(accepted, Is.Zero);
            host.gameObject.SetActive(false); yield return null; host.gameObject.SetActive(true); yield return null;
            Assert.That(p.View.ModalBody.text, Does.Contain("멱등"));
        }

        [UnityTest] public IEnumerator EachPermissionAndPoolBlocksOnlyInvalidSlotsWithoutSaving()
        {
            foreach (var fault in new[] { "implemented", "permission", "canUse", "ownershipOnly", "emptyPool", "restrictedPool" })
            {
                var c = context.Clone(); var s = Snapshot(c); var wolf = s.Find("Wolf");
                wolf.AvailabilityMessage = "TEST_BLOCKED_" + fault;
                if (fault == "implemented") wolf.Implemented = false;
                if (fault == "permission") wolf.HasContextPermission = false;
                if (fault == "canUse") wolf.CanUseInContext = false;
                if (fault == "ownershipOnly") { wolf.Unlocked = true; wolf.HasContextPermission = false; wolf.CanUseInContext = false; }
                if (fault == "emptyPool") c.AllowedAnimalIds = Array.Empty<string>();
                if (fault == "restrictedPool") c.AllowedAnimalIds = new[] { "Rabbit", "DreamFox", "Swallow" };
                backend.OnRead = _ => Task.FromResult(s); Assert.IsTrue(host.Open(c)); yield return null;
                p.View.RosterContent.GetComponentsInChildren<AnimalCardView>().Single(a => a.Name.text == p.Catalog.Find("Wolf").DisplayName).Button.onClick.Invoke();
                Assert.That(p.InspectedAnimalId, Is.EqualTo("Wolf")); Assert.That(p.Draft.Ground, Is.EqualTo("Rabbit"));
                Assert.That(p.View.Primary.interactable, Is.EqualTo(fault != "emptyPool"));
                // Selecting another invalid animal cannot invalidate the valid selected three.
                Assert.That(backend.Requests.Count, Is.Zero);
                if (fault != "emptyPool") { p.View.Primary.onClick.Invoke(); p.View.ModalCancel.onClick.Invoke(); }
                var invalid = c.Clone(); invalid.InitialLoadout.Ground = "Wolf";
                Assert.IsTrue(host.Open(invalid)); yield return null; p.View.Primary.onClick.Invoke(); p.View.ModalConfirm.onClick.Invoke();
                Assert.IsFalse(p.View.Primary.interactable); Assert.That(backend.Requests.Count, Is.Zero);
            }
            backend.OnRead = c => Task.FromResult(Snapshot(c)); Assert.IsTrue(host.Open(context)); yield return null;
            foreach (var choice in new[] { (AnimalRole.Ground, "Wolf"), (AnimalRole.Special, "StarCat"), (AnimalRole.Air, "Bat") })
            {
                var before = p.Draft; p.View.RoleTabs[(int)choice.Item1].onClick.Invoke();
                p.View.RosterContent.GetComponentsInChildren<AnimalCardView>().Single(a => a.Name.text == p.Catalog.Find(choice.Item2).DisplayName).Button.onClick.Invoke();
                Assert.That(p.Draft.Get(choice.Item1), Is.EqualTo(choice.Item2));
                foreach (AnimalRole role in Enum.GetValues(typeof(AnimalRole))) if (role != choice.Item1) Assert.That(p.Draft.Get(role), Is.EqualTo(before.Get(role)));
            }
            Assert.IsTrue(p.View.Primary.interactable); Assert.That(context.InitialLoadout.Fingerprint(), Is.EqualTo("Rabbit|DreamFox|Swallow"));
            Assert.That(backend.Requests.Count, Is.Zero);
        }

        [UnityTest] public IEnumerator MalformedContextAndLoadoutNeverDispatches()
        {
            foreach (var fault in new[] { "mode", "name", "intent", "policy", "roles", "duplicateRoles", "representative", "wrongRole", "missingSlot", "unknownId", "readRevision", "preview" })
            {
                var c = context.Clone();
                if (fault == "mode") c.ModeId = " ";
                if (fault == "name") c.DisplayName = null;
                if (fault == "intent") c.EntryIntent = null;
                if (fault == "policy") c.Requirements.PolicyRevision = null;
                if (fault == "roles") c.Requirements.RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Air };
                if (fault == "duplicateRoles") c.Requirements.RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Ground, AnimalRole.Air };
                if (fault == "representative") c.Requirements.RepresentativeRole = (AnimalRole)99;
                if (fault == "wrongRole") c.InitialLoadout.Air = "Wolf";
                if (fault == "missingSlot") c.InitialLoadout.Special = null;
                if (fault == "unknownId") c.InitialLoadout.Ground = "UNKNOWN";
                if (fault == "preview") c.ModeId = "PREVIEW_ONLY";
                backend.OnRead = r => { var s = Snapshot(r); if (fault == "readRevision") s.Revision = null; return Task.FromResult(s); };
                // Direct presenter path also needs to reject PREVIEW_ONLY even without host filtering.
                Assert.IsTrue(p.OpenMultiplayer(c)); yield return null;
                p.View.Primary.onClick.Invoke(); p.View.ModalConfirm.onClick.Invoke();
                Assert.IsFalse(p.View.Primary.interactable, fault); Assert.IsFalse(p.View.Modal.activeSelf, fault);
                Assert.That(backend.Requests.Count, Is.Zero, fault);
            }
        }
    }
}
