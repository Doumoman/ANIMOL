using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using William.NotebookUI;

namespace ANIMOL.Presentation.Tests
{
    public sealed class NotebookTemporaryPlayTests
    {
        static NotebookCatalog Catalog() => NotebookCatalog.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
            "Assets/ANIMOL/Data/UI/Notebook/catalog.json"));

        [Test] public void UnlimitedEntriesKeepTheRealWalletAndApprovalChecks()
        {
            var state = new NotebookWalletState { balance = 0, lastRecoveryUnix = 1000 };
            var model = new NotebookBmModel(Catalog(), state, 1000, true, true, true);
            Assert.That(model.CanAffordStageEntry, Is.True);
            for (int i = 0; i < 40; i++)
            {
                string stage = (i < 20 ? "SW" : "ET-") + (i % 20 + 1).ToString("00");
                Assert.That(model.AcceptApprovedEntry("free-" + i, stage, true, 1000 + i, out _), Is.True, stage);
            }
            Assert.That(model.AcceptApprovedEntry("rejected", "SW01", false, 1000, out _), Is.False);
            Assert.That(model.AcceptApprovedEntry("locked", "observatory-01", true, 1000, out _), Is.False);
            Assert.That(model.AcceptApprovedEntry("free-0", "SW01", true, 1000, out bool duplicate), Is.True);
            Assert.That(duplicate, Is.True);
            model.Tick(100000);
            Assert.That(state.balance, Is.Zero);
            Assert.That(state.lastRecoveryUnix, Is.EqualTo(1000));
            Assert.That(state.rewardIds, Is.Empty);
            Assert.That(state.ownedThemes, Is.EqualTo(new[] { "starway" }));
            Assert.That(model.RewardAmount(100000), Is.Zero);
        }

        [Test] public void TemporaryClearsNeverBecomeSavedClearsAndCanBeDisabled()
        {
            var state = new NotebookWalletState { balance = 0, lastRecoveryUnix = 1000 };
            var preview = new NotebookBmModel(Catalog(), state, 1000, true, true, true);
            for (int i = 1; i <= 20; i++)
                foreach (string prefix in new[] { "SW", "ET-" })
                {
                    Assert.That(preview.IsCleared(prefix + i.ToString("00")), Is.True);
                    Assert.That(preview.CanEnter(prefix + i.ToString("00")), Is.True);
                }
            Assert.That(preview.IsCleared("observatory-01"), Is.False);
            Assert.That(preview.IsCleared("sea-01"), Is.False);
            Assert.That(state.clearedStageIds, Is.Empty);
            var saved = JsonUtility.FromJson<NotebookWalletState>(JsonUtility.ToJson(state));
            var normal = new NotebookBmModel(Catalog(), saved, 1000, true);
            Assert.That(normal.IsCleared("SW01"), Is.False);
            Assert.That(normal.CanEnter("SW02"), Is.False);
            Assert.That(normal.CanEnter("ET-20"), Is.False);
            Assert.That(normal.CanAffordStageEntry, Is.False);
            Assert.That(normal.AcceptApprovedEntry("paid", "SW01", true, 1000, out _), Is.False);
            Assert.That(new NotebookBmModel(Catalog(), saved, 1000, false, true, true).UnlimitedTelescopes, Is.False);
        }

        [Test] public void ARealClearStillPersistsDuringTemporaryClearMode()
        {
            var state = new NotebookWalletState();
            var preview = new NotebookBmModel(Catalog(), state, 1000, true, true, true);
            Assert.That(preview.AcceptVerifiedClear("SW01", true), Is.True);
            Assert.That(preview.AcceptVerifiedClear("ET-01", false), Is.False);
            Assert.That(state.clearedStageIds, Is.EqualTo(new[] { "SW01" }));
            Assert.That(new NotebookBmModel(Catalog(), state, 1000, true).IsCleared("SW01"), Is.True);
        }
    }
}
