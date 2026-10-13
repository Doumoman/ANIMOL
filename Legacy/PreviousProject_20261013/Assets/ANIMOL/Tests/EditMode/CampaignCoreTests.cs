using System.Linq;
using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEditor;

namespace ANIMOL.Tests
{
    public sealed class CampaignCoreTests
    {
        private const string CatalogPath = "Assets/ANIMOL/Data/Campaign/CampaignCatalog.asset";

        [Test]
        public void Catalog_HasExactlyFiveThemesAndOneHundredStableIds()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            var report = CampaignCatalogValidator.Validate(catalog);
            Assert.That(report.IsValid, Is.True, string.Join("\n", report.Errors));
            Assert.That(report.ThemeCount, Is.EqualTo(5));
            Assert.That(report.StageCount, Is.EqualTo(100));
            Assert.That(report.DuplicateStageIdCount, Is.Zero);
            Assert.That(catalog.EnumerateStages().Select(x => x.StageId).Distinct().Count(), Is.EqualTo(100));
        }

        [Test]
        public void GeneratedOperationalSlots_AreUnassignedNotReady()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            Assert.That(catalog.EnumerateStages().All(x => ContentAvailabilityResolver.Resolve(x) == ContentAvailability.Unassigned), Is.True);
        }

        [Test]
        public void GeneratorSecondRun_PreservesManualNameMapAndFixedAnimals()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            var stage = catalog.FindStage("T01-S01");
            var serialized = new SerializedObject(stage);
            var originalName = stage.DisplayName;
            var originalMap = stage.MapTemplateId;
            var originalInitial = stage.InitialAnimalId;
            var originalAnimals = stage.FixedAllowedAnimalIds.ToArray();
            try
            {
                serialized.FindProperty("displayName").stringValue = "수동 이름 보존 검사";
                serialized.FindProperty("mapTemplateId").stringValue = "MANUAL-MAP";
                serialized.FindProperty("initialAnimalId").stringValue = "MANUAL-GROUND";
                var animals = serialized.FindProperty("fixedAllowedAnimalIds");
                animals.arraySize = 2;
                animals.GetArrayElementAtIndex(0).stringValue = "MANUAL-GROUND";
                animals.GetArrayElementAtIndex(1).stringValue = "MANUAL-GLIDER";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                CampaignCatalogGenerator.BuildOrUpdate();
                Assert.That(stage.DisplayName, Is.EqualTo("수동 이름 보존 검사"));
                Assert.That(stage.MapTemplateId, Is.EqualTo("MANUAL-MAP"));
                Assert.That(stage.InitialAnimalId, Is.EqualTo("MANUAL-GROUND"));
                Assert.That(stage.FixedAllowedAnimalIds, Is.EqualTo(new[] { "MANUAL-GROUND", "MANUAL-GLIDER" }));
            }
            finally
            {
                serialized.Update();
                serialized.FindProperty("displayName").stringValue = originalName;
                serialized.FindProperty("mapTemplateId").stringValue = originalMap;
                serialized.FindProperty("initialAnimalId").stringValue = originalInitial;
                var animals = serialized.FindProperty("fixedAllowedAnimalIds");
                animals.arraySize = originalAnimals.Length;
                for (var i = 0; i < originalAnimals.Length; i++) animals.GetArrayElementAtIndex(i).stringValue = originalAnimals[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
        }

        [Test]
        public void BubbleObjective_DeduplicatesSlotAndOnlyThirdUniqueBubbleOpensExit()
        {
            var objectives = new BubbleObjectiveService(3);
            Assert.That(objectives.TryCollect("run", "map", "A", "player"), Is.True);
            Assert.That(objectives.TryCollect("run", "map", "A", "player"), Is.False);
            Assert.That(objectives.TryCollect("run", "map", "B", "player"), Is.True);
            Assert.That(objectives.IsExitEligible, Is.False);
            Assert.That(objectives.TryCollect("run", "map", "C", "player"), Is.True);
            Assert.That(objectives.IsExitEligible, Is.True);
        }

        [Test]
        public void ExitGate_RequiresThreeBubblesAndPhysicalEntry()
        {
            var session = new RunSessionController();
            session.BeginLoading("run", "DEV-TEST-01", true);
            session.BeginCountdown();
            session.BeginPlaying();
            var objectives = new BubbleObjectiveService(3);
            var gate = new ExitGateController(session, objectives);
            Assert.That(gate.TryEnter(), Is.False);
            objectives.TryCollect("run", "map", "A", "player");
            objectives.TryCollect("run", "map", "B", "player");
            objectives.TryCollect("run", "map", "C", "player");
            Assert.That(session.State, Is.EqualTo(RunSessionState.ExitEligible));
            Assert.That(gate.TryEnter(), Is.True);
            Assert.That(session.State, Is.EqualTo(RunSessionState.MapResolved));
        }

        [Test]
        public void CampaignBoundary_AdvancesThemesAndStopsAfterFinalStage()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            var progression = new CampaignProgressionService(catalog);
            Assert.That(progression.GetNextStageId("T01-S20"), Is.EqualTo("T02-S01"));
            Assert.That(progression.GetNextStageId("T05-S20"), Is.Empty);
        }

        [Test]
        public void DevelopmentResult_NeverMutatesCampaignProgress()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            var progression = new CampaignProgressionService(catalog);
            Assert.That(progression.RecordValidatedCompletion("T01-S01", 12f, true, true), Is.False);
            Assert.That(progression.Progress.Revision, Is.Zero);
        }

        [Test]
        public void FixedCampaignRoster_RejectsAnimalsOutsideStageSnapshot()
        {
            var roster = new CampaignRosterSnapshot(new[] { "GROUND", "GLIDER" }, "GROUND");
            Assert.That(roster.CanTransformTo("GROUND"), Is.True);
            Assert.That(roster.CanTransformTo("GLIDER"), Is.True);
            Assert.That(roster.CanTransformTo("UNASSIGNED_ANIMAL"), Is.False);
        }

        [Test]
        public void AccountUpgradeCatalog_HasThreeVersionedLevelTenTracks()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>("Assets/ANIMOL/Data/Meta/AccountUpgradeCatalog.asset");
            Assert.That(catalog.Tracks.Count, Is.EqualTo(3));
            Assert.That(catalog.Tracks.All(x => x.MaxLevel == 10 && x.PercentByLevel.Count == 11 && x.CostByLevel.Count == 10), Is.True);
            Assert.That(catalog.Tracks.Sum(x => x.CostByLevel.Sum()), Is.EqualTo(13320));
        }

        [Test]
        public void DisconnectedAccountPurchase_NeverApprovesAndDeduplicatesRequestId()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>("Assets/ANIMOL/Data/Meta/AccountUpgradeCatalog.asset");
            var useCase = new UpgradePurchaseUseCase(new DisconnectedAccountGateway());
            Assert.That(useCase.Request("txn-1", catalog.Tracks[0], 0, catalog.Version).Status, Is.EqualTo(AccountRequestStatus.Unavailable));
            Assert.That(useCase.Request("txn-1", catalog.Tracks[0], 0, catalog.Version).Status, Is.EqualTo(AccountRequestStatus.Duplicate));
        }

        [Test]
        public void AccountUpgradeUi_SeparatesAvailableInsufficientMaxAndUnavailable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>("Assets/ANIMOL/Data/Meta/AccountUpgradeCatalog.asset");
            var track = catalog.Tracks[0];
            var cost = track.CostForNextLevel(0);
            Assert.That(AccountUpgradeStateResolver.Resolve(track, 0, cost, true).State, Is.EqualTo(AccountUpgradeUiState.Available));
            Assert.That(AccountUpgradeStateResolver.Resolve(track, 0, cost - 1, true).State, Is.EqualTo(AccountUpgradeUiState.InsufficientCoins));
            Assert.That(AccountUpgradeStateResolver.Resolve(track, 10, 999999, true).State, Is.EqualTo(AccountUpgradeUiState.Max));
            Assert.That(AccountUpgradeStateResolver.Resolve(track, 0, null, false).State, Is.EqualTo(AccountUpgradeUiState.Unavailable));
        }

        [Test]
        public void EmoteLoadout_ProvidesFourSlotsAndRejectsUnownedItems()
        {
            var loadout = new EmoteLoadoutService(new[] { "HELLO", "THANKS", "HELP" });
            Assert.That(loadout.Slots.Count, Is.EqualTo(4));
            Assert.That(loadout.TryEquip(3, "HELLO"), Is.True);
            Assert.That(loadout.TryEquip(4, "HELLO"), Is.False);
            Assert.That(loadout.TryEquip(0, "PAID_NOT_OWNED"), Is.False);
        }

        [Test]
        public void MultiplayerSelection_IsExclusiveToMultiplayerAndDisconnectedReadyNeverSucceeds()
        {
            var campaign = new ModeLoadoutRuleSet(GameModeKind.Campaign, 0, false, new string[0]);
            var competitive = new ModeLoadoutRuleSet(GameModeKind.Competitive, 2, false, new[] { "GROUND", "GLIDER" });
            Assert.That(campaign.HasPlayerSelection, Is.False);
            Assert.That(competitive.HasPlayerSelection, Is.True);
            var loadout = new MultiplayerLoadoutService(competitive, new DisconnectedMatchGateway());
            Assert.That(loadout.TrySelect(0, "GROUND"), Is.True);
            Assert.That(loadout.TrySelect(1, "GROUND"), Is.False, "Duplicate selection must follow the rule asset.");
            Assert.That(loadout.TrySelect(1, "NOT_ALLOWED"), Is.False);
            Assert.That(loadout.TrySelect(1, "GLIDER"), Is.True);
            Assert.That(loadout.RequestReady().Status, Is.EqualTo(MatchRequestStatus.Unavailable));
            Assert.That(loadout.IsReady, Is.False);
        }

        [Test]
        public void ExternalServices_AreExplicitlyDisconnected()
        {
            var services = AssetDatabase.LoadAssetAtPath<ExternalServiceConfiguration>("Assets/ANIMOL/Data/Services/ExternalServiceConfiguration.asset");
            Assert.That(services.AccountServerConnected, Is.False);
            Assert.That(services.MatchServerConnected, Is.False);
            Assert.That(services.PurchaseSdkConnected, Is.False);
            Assert.That(services.RewardedAdSdkConnected, Is.False);
        }

        [Test]
        public void M6_TimerExpiresExactlyAtLimitAndUsesFailureState()
        {
            var timer = new CampaignRunTimer(10f);
            Assert.That(timer.Advance(9.9f), Is.False);
            Assert.That(timer.Advance(.1f), Is.True);
            Assert.That(timer.RemainingSeconds, Is.Zero);

            var run = new RunSessionController();
            run.BeginLoading("run", "T01-S01", false);
            run.BeginCountdown();
            run.BeginPlaying();
            run.MarkTimeExpired();
            Assert.That(run.State, Is.EqualTo(RunSessionState.TimeExpired));
            run.ShowResult();
            Assert.That(run.State, Is.EqualTo(RunSessionState.Result));
        }

        [Test]
        public void M6_TimeExpiredResultNeverUnlocksCampaignOrCreatesBestTime()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            var progression = new CampaignProgressionService(catalog);
            var result = progression.RecordValidatedResult("T01-S01", 45f, CampaignRunOutcome.TimeExpired, false, false);
            Assert.That(result.Accepted, Is.False);
            Assert.That(progression.Progress.CompletedStageIds, Is.Empty);
            Assert.That(progression.Progress.BestTimes, Is.Empty);
        }

        [Test]
        public void M6_CheckpointAndRetryLayoutAreFixedData()
        {
            var checkpoints = new CheckpointRespawnState(new[] { "START", "MID" }, "START");
            Assert.That(checkpoints.TryActivate("RANDOM"), Is.False);
            Assert.That(checkpoints.TryActivate("MID"), Is.True);
            Assert.That(checkpoints.ActiveCheckpointId, Is.EqualTo("MID"));

            var definition = AssetDatabase.LoadAssetAtPath<DevTestRunDefinition>("Assets/ANIMOL/Data/Development/DEV-TEST-01.asset");
            Assert.That(definition.LayoutIdentity, Is.EqualTo(new RetryLayoutIdentity("DEV-TEST-01", 601, 603)));
            Assert.That(definition.CheckpointIds.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void M6_UndiscoveredBubbleNeverExposesWorldPositionOrEdgeArrow()
        {
            Assert.That(BubbleVisibilityGuard.MayRevealWorldPosition(false), Is.False);
            Assert.That(BubbleVisibilityGuard.MayShowEdgeArrow(false), Is.False);
            Assert.That(BubbleVisibilityGuard.MayRevealWorldPosition(true), Is.True);
        }

        [Test]
        public void M6_TouchCaptureTracksIndependentPointerIds()
        {
            var touches = new TouchCaptureRegistry();
            Assert.That(touches.TryCapture(11, MobileTouchAction.SwipeMove), Is.True);
            Assert.That(touches.TryCapture(12, MobileTouchAction.Jump), Is.True);
            Assert.That(touches.TryCapture(13, MobileTouchAction.Special), Is.True);
            Assert.That(touches.TryCapture(14, MobileTouchAction.Ability), Is.True);
            Assert.That(touches.ActiveCount, Is.EqualTo(4));
            Assert.That(touches.TryCapture(11, MobileTouchAction.Jump), Is.False, "A pointer ID cannot be stolen by a second control.");
            Assert.That(touches.Release(12), Is.True);
            Assert.That(touches.ActiveCount, Is.EqualTo(3));
        }

        [Test]
        public void M6_TutorialContractHasNoMandatoryEntryPopup()
        {
            var tutorial = AssetDatabase.LoadAssetAtPath<CampaignTutorialContentContract>("Assets/ANIMOL/Data/Campaign/CampaignTutorialContentContract.asset");
            Assert.That(tutorial, Is.Not.Null);
            Assert.That(tutorial.AutoShowMandatoryPopup, Is.False);
            Assert.That(tutorial.RequiresPlayableMapContent, Is.True);
            Assert.That(tutorial.OrderedActionKeys.Count, Is.EqualTo(6));
        }

        [Test]
        public void M6_WideAspectExpandsWorldInsteadOfCroppingToRankedSixteenByNine()
        {
            var width16x9 = WideWorldCameraPolicy.VisibleWorldWidth(5.4f, 16f / 9f);
            var width20x9 = WideWorldCameraPolicy.VisibleWorldWidth(5.4f, 20f / 9f);
            Assert.That(width20x9, Is.GreaterThan(width16x9));
        }
    }
}
