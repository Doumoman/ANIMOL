using System.Collections;
using ANIMOL.AnimalUiV2;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.AnimalUpgradePhase1.Tests
{
    public sealed class UpgradePhase1TransactionUiTests
    {
        private GameObject holder;
        private AnimalUiPresenter presenter;
        private UpgradeTransactionTestBackend backend;

        [UnitySetUp] public IEnumerator OpenIsolatedUi()
        {
            holder = new GameObject("Phase1 transaction UI test"); holder.SetActive(false);
            var root = Object.Instantiate(Resources.Load<GameObject>(AnimalUpgradePhase1Host.ResourcePath), holder.transform);
            root.GetComponent<AnimalUpgradePhase1Host>().enabled = false;
            presenter = root.GetComponent<AnimalUiPresenter>();
            backend = root.AddComponent<UpgradeTransactionTestBackend>(); presenter.SetBackend(backend);
            holder.SetActive(true); presenter.OpenUpgrade("Rabbit"); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(holder); yield return null; }

        [UnityTest] public IEnumerator CancelDoesNotCallServiceAndPendingDoesNotChangeBalances()
        {
            var view = presenter.View;
            view.ActiveTrack.UpgradeButton.onClick.Invoke(); yield return null;
            Assert.That(view.ModalBody.text, Does.Contain("토끼").And.Contain("액티브").And.Contain("1 → 2")
                .And.Contain("25").And.Contain("5").And.Contain("test next effect"));
            view.ModalCancel.onClick.Invoke(); Assert.That(backend.Requests.Count, Is.Zero);
            string balances = view.Balances.text, level = view.ActiveTrack.Level.text;
            view.ActiveTrack.UpgradeButton.onClick.Invoke(); view.ModalConfirm.onClick.Invoke(); view.ModalConfirm.onClick.Invoke();
            Assert.That(backend.Requests.Count, Is.EqualTo(1));
            Assert.IsTrue(presenter.IsCommitting);
            Assert.IsFalse(view.RoleTabs[1].interactable || view.PassiveTrack.UpgradeButton.interactable || view.ModalCancel.interactable);
            view.RoleTabs[1].onClick.Invoke(); Assert.That(presenter.InspectedAnimalId, Is.EqualTo("Rabbit"));
            presenter.Back(); Assert.IsTrue(presenter.gameObject.activeSelf);
            Assert.That(view.Balances.text, Is.EqualTo(balances)); Assert.That(view.ActiveTrack.Level.text, Is.EqualTo(level));
            int reads = backend.Reads;
            backend.Completion.SetResult(new AnimalUiCommitResult { Status = CommitStatus.Rejected, Message = "test price version changed" });
            yield return null; yield return null;
            Assert.That(backend.Reads, Is.GreaterThan(reads));
            Assert.That(view.Status.text, Is.EqualTo("test price version changed"));
            Assert.IsFalse(presenter.IsCommitting);
            Assert.That(view.Balances.text, Is.EqualTo(balances));
            Assert.That(backend.Requests.Count, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator UnknownAndReentryKeepTheExactRequestUntilResolved()
        {
            var view = presenter.View;
            view.PassiveTrack.UpgradeButton.onClick.Invoke(); view.ModalConfirm.onClick.Invoke();
            backend.Completion.SetException(new System.TimeoutException("test response lost"));
            yield return null; yield return null;
            Assert.IsTrue(presenter.IsCommitting);
            presenter.gameObject.SetActive(false); presenter.gameObject.SetActive(true);
            Assert.IsFalse(presenter.OpenUpgrade("Wolf"));
            view.ModalConfirm.onClick.Invoke();
            Assert.That(backend.Requests.Count, Is.EqualTo(2));
            var first = backend.Requests[0]; var retry = backend.Requests[1];
            Assert.IsNotEmpty(first.ActionId);
            Assert.That(retry.ActionId, Is.EqualTo(first.ActionId));
            Assert.That(retry.AnimalId, Is.EqualTo(first.AnimalId));
            Assert.That(retry.Track, Is.EqualTo(first.Track));
            Assert.That(retry.QuoteToken, Is.EqualTo(first.QuoteToken));
            Assert.That(retry.ExpectedCurrentLevel, Is.EqualTo(first.ExpectedCurrentLevel));
            backend.Completion.SetResult(new AnimalUiCommitResult { Status = CommitStatus.Unavailable, Message = "test definitive no dispatch" });
            yield return null; yield return null;
            Assert.IsFalse(presenter.IsCommitting);
        }
    }
}
