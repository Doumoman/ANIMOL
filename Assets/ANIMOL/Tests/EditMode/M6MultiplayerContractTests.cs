using System.Linq;
using ANIMOL.Core;
using NUnit.Framework;
using UnityEditor;

namespace ANIMOL.Tests
{
    public sealed class M6MultiplayerContractTests
    {
        [Test]
        public void ParticipantRanges_ArePublicCompetitiveFourAndCoopTwoToFour()
        {
            Assert.That(MultiplayerModeRules.CompetitiveParticipants.Contains(4), Is.True);
            Assert.That(MultiplayerModeRules.CompetitiveParticipants.Contains(5), Is.False);
            Assert.That(MultiplayerModeRules.CompetitiveParticipants.Contains(8), Is.False);
            Assert.That(MultiplayerModeRules.CompetitiveParticipants.Contains(3), Is.False);
            Assert.That(MultiplayerModeRules.CoopParticipants.Contains(2), Is.True);
            Assert.That(MultiplayerModeRules.CoopParticipants.Contains(4), Is.True);
            Assert.That(MultiplayerModeRules.CoopParticipants.Contains(5), Is.False);
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(8)]
        public void CompetitiveCapacity_RejectsInvalidCountWithoutHidingParticipants(int count)
        {
            var room = new MultiplayerRoomPreviewState(GameModeKind.Competitive, 4, false);
            var participants = room.Participants.ToArray();
            Assert.That(room.SetParticipantCount(count), Is.False);
            CollectionAssert.AreEqual(participants, room.Participants);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new CompetitiveParticipantSelectionService(
                Enumerable.Range(1, count).Select(i => "P" + i),
                System.Array.Empty<CampaignAnimalUnlockRule>(), CampaignAnimalUnlockSnapshot.DevelopmentOnly()));
        }

        [Test]
        public void CompetitivePreviewToggle_RemainsFourAndCoopStillTogglesTwoToFour()
        {
            var competitive = new MultiplayerRoomPreviewState(GameModeKind.Competitive, 4, false);
            var ranked = new MultiplayerRoomPreviewState(GameModeKind.Ranked, 4, true);
            Assert.That(competitive.ToggleMinimumMaximum(), Is.EqualTo(4));
            Assert.That(ranked.ToggleMinimumMaximum(), Is.EqualTo(4));
            Assert.That(competitive.GrowthMode, Is.EqualTo(GrowthPresentationMode.OwnedProgress));
            Assert.That(ranked.GrowthMode, Is.EqualTo(GrowthPresentationMode.RankedMaximumPreset));
            var coop = new MultiplayerRoomPreviewState(GameModeKind.Coop, 2, false);
            Assert.That(coop.ToggleMinimumMaximum(), Is.EqualTo(4));
            Assert.That(coop.ToggleMinimumMaximum(), Is.EqualTo(2));
        }

        [Test]
        public void RoomPrefab_HasExactlyFourNamedParticipantRows()
        {
            var room = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ANIMOL/Prefabs/UI/SC06_MatchRoom.prefab");
            var rows = room.GetComponentsInChildren<UnityEngine.Transform>(true)
                .Where(t => t.name.StartsWith("RoomParticipant_", System.StringComparison.Ordinal)).Select(t => t.name).ToArray();
            CollectionAssert.AreEquivalent(new[] { "RoomParticipant_01", "RoomParticipant_02", "RoomParticipant_03", "RoomParticipant_04" }, rows);
        }

        [Test]
        public void CompetitiveSelection_UsesCampaignUnlocksAndAllowsCrossParticipantDuplicates()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/ANIMOL/Data/Campaign/CampaignCatalog.asset");
            var progression = new CampaignProgressionService(catalog);
            var rules = new[]
            {
                new CampaignAnimalUnlockRule("GROUND", MultiplayerAnimalRole.Ground, string.Empty),
                new CampaignAnimalUnlockRule("AIR", MultiplayerAnimalRole.Air, "T01-S01"),
                new CampaignAnimalUnlockRule("SPECIAL", MultiplayerAnimalRole.Special, "T01-S02")
            };
            var resolver = new CampaignAnimalUnlockResolver(rules);
            var initial = resolver.Resolve(progression.Progress);
            Assert.That(initial.IsUnlocked("GROUND"), Is.True);
            Assert.That(initial.IsUnlocked("AIR"), Is.False);

            progression.RecordValidatedResult("T01-S01", 30f, CampaignRunOutcome.Cleared, true, false);
            progression.RecordValidatedResult("T01-S02", 31f, CampaignRunOutcome.Cleared, true, false);
            var unlocked = resolver.Resolve(progression.Progress);
            var participants = new[] { "P1", "P2", "P3", "P4" };
            var selection = new CompetitiveParticipantSelectionService(participants, rules, unlocked);
            foreach (var participant in participants.Take(2))
            {
                Assert.That(selection.TrySelect(participant, MultiplayerAnimalRole.Ground, "GROUND"), Is.True);
                Assert.That(selection.TrySelect(participant, MultiplayerAnimalRole.Air, "AIR"), Is.True);
                Assert.That(selection.TrySelect(participant, MultiplayerAnimalRole.Special, "SPECIAL"), Is.True);
                Assert.That(selection.CanReady(participant), Is.True);
            }
            Assert.That(selection.GetSelection("P1", MultiplayerAnimalRole.Special), Is.EqualTo("SPECIAL"));
            Assert.That(selection.GetSelection("P2", MultiplayerAnimalRole.Special), Is.EqualTo("SPECIAL"));
            Assert.That(selection.AllowsSameAnimalAcrossParticipants, Is.True);
        }

        [Test]
        public void CompetitiveSelection_RejectsLockedOrWrongRoleAnimals()
        {
            var rules = new[]
            {
                new CampaignAnimalUnlockRule("GROUND", MultiplayerAnimalRole.Ground, string.Empty),
                new CampaignAnimalUnlockRule("SPECIAL", MultiplayerAnimalRole.Special, "T01-S20")
            };
            var selection = new CompetitiveParticipantSelectionService(
                new[] { "P1", "P2", "P3", "P4" }, rules, CampaignAnimalUnlockSnapshot.DevelopmentOnly("GROUND"));
            Assert.That(selection.TrySelect("P1", MultiplayerAnimalRole.Ground, "SPECIAL"), Is.False);
            Assert.That(selection.TrySelect("P1", MultiplayerAnimalRole.Special, "SPECIAL"), Is.False);
            Assert.That(selection.CanReady("P1"), Is.False);
        }

        [Test]
        public void CoopPreview_PreservesExistingTwoSlotNoDuplicateRule()
        {
            var preview = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>("Assets/ANIMOL/Data/Development/DEV-MULTI-COOP.asset");
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.SlotCount, Is.EqualTo(2));
            Assert.That(preview.AllowDuplicates, Is.False);
            var loadout = new MultiplayerLoadoutService(preview.CreateRuleSet(), new DisconnectedMatchGateway());
            Assert.That(loadout.TrySelect(0, preview.AllowedAnimalIds[0]), Is.True);
            Assert.That(loadout.TrySelect(1, preview.AllowedAnimalIds[0]), Is.False);
        }

        [Test]
        public void CompetitiveSeries_AcceptsOnlyServerConfirmedThreeMapResults()
        {
            var series = new CompetitiveSeriesState();
            Assert.That(series.Maps.Count, Is.EqualTo(3));
            Assert.That(series.ApplyServerMapResult(1, CompetitiveMapStatus.Completed, 100, "완주", false), Is.False);
            Assert.That(series.TotalServerPerformanceScore, Is.Zero);
            Assert.That(series.ApplyServerMapResult(1, CompetitiveMapStatus.Completed, 100, "완주", true), Is.True);
            Assert.That(series.ApplyServerMapResult(2, CompetitiveMapStatus.Completed, 200, "완주", true), Is.True);
            Assert.That(series.ApplyServerMapResult(3, CompetitiveMapStatus.TimeExpired, 300, "시간 종료", true), Is.True);
            Assert.That(series.TotalServerPerformanceScore, Is.EqualTo(600));
            Assert.That(series.HasAuthoritativeRanking, Is.False);
        }

        [Test]
        public void BubbleClaim_RequiresServerConfirmationAndThreePersonalAwardsForExit()
        {
            var bubbles = new CompetitiveBubbleAuthorityState();
            foreach (var slot in new[] { "A", "B", "C" })
            {
                Assert.That(bubbles.RequestClaim(slot), Is.True);
                Assert.That(bubbles.Status, Is.EqualTo(BubbleSlotAuthorityStatus.AwaitingServerConfirmation));
                Assert.That(bubbles.ApplyServerClaim(slot, "P1", true), Is.True);
                Assert.That(bubbles.Status, Is.EqualTo(BubbleSlotAuthorityStatus.RespawnPending));
            }
            Assert.That(bubbles.GetPersonalCount("P1"), Is.EqualTo(3));
            Assert.That(bubbles.HasExitEligibility("P1"), Is.True);
            Assert.That(bubbles.RequestClaim("A"), Is.False, "An already claimed slot cannot be awarded again before authoritative respawn.");
        }

        [Test]
        public void CoopProgressAndCompetitiveCombatRemainServerAuthoritative()
        {
            foreach (var participantCount in new[] { 2, 4 })
            {
                var objective = new CoopTeamObjectiveState(participantCount);
                Assert.That(objective.ApplyServerProgress(3, participantCount, false), Is.False);
                Assert.That(objective.TeamBubbleCount, Is.Zero);
                Assert.That(objective.IsComplete, Is.False);

                Assert.That(objective.ApplyServerProgress(2, participantCount, true), Is.True);
                Assert.That(objective.IsComplete, Is.False, "All members at the exit are insufficient before the team has three bubbles.");
                Assert.That(objective.ApplyServerProgress(3, participantCount - 1, true), Is.True);
                Assert.That(objective.IsComplete, Is.False, "Three team bubbles are insufficient before every member reaches the exit.");
                Assert.That(objective.ApplyServerProgress(3, participantCount, true), Is.True);
                Assert.That(objective.TeamBubbleCount, Is.EqualTo(3));
                Assert.That(objective.EscapedMemberCount, Is.EqualTo(participantCount));
                Assert.That(objective.IsComplete, Is.True);
            }
            Assert.That(MultiplayerModeRules.AllowCompetitiveBodyAttack, Is.False);
            Assert.That(MultiplayerModeRules.AllowCompetitiveKnockback, Is.False);
        }

        [Test]
        public void RankedUsesMaximumPresetWhileCasualAndCoopUseOwnedGrowth()
        {
            var ranked = new MultiplayerRoomPreviewState(GameModeKind.Competitive, 4, true);
            var casual = new MultiplayerRoomPreviewState(GameModeKind.Competitive, 4, false);
            var coop = new MultiplayerRoomPreviewState(GameModeKind.Coop, 2, false);
            Assert.That(ranked.GrowthMode, Is.EqualTo(GrowthPresentationMode.RankedMaximumPreset));
            Assert.That(casual.GrowthMode, Is.EqualTo(GrowthPresentationMode.OwnedProgress));
            Assert.That(coop.GrowthMode, Is.EqualTo(GrowthPresentationMode.OwnedProgress));
            Assert.That(ranked.DevelopmentPreviewOnly, Is.True);
        }
    }
}
