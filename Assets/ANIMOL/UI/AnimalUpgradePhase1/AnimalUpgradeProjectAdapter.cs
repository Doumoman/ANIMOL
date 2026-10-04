using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;
using ANIMOL.Core;
using ANIMOL.PortraitArtV1;
using ANIMOL.UI;
using UnityEngine;

namespace ANIMOL.AnimalUpgradePhase1
{
    /// <summary>
    /// Reads the project's verified coin boundary and explicit species mappings.
    /// The current project has no animal progression store or transactional growth gateway.
    /// Configuration assets alone must never authorize a purchase or establish ownership.
    /// </summary>
    public sealed class AnimalUpgradeProjectAdapter : AnimalUpgradeBackendAdapterBase
    {
        public AnimalUpgradeIdMap IdMap;
        public GrowthEconomyPolicyCatalog GrowthPolicy;
        public CharacterUpgradeCatalog UpgradeCatalog;

        public override Task<AnimalUiSnapshot> ReadSnapshotAsync(AnimalUiReadRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var navigation = GetComponentInParent<UiNavigationService>();
            var owner = navigation == null ? null : navigation.GetComponentInChildren<PortraitEntryController>(true);
            var snapshot = new AnimalUiSnapshot { CoinBalance = ReadVerifiedCoinBalance(owner?.Ledger) };
            // No synthetic revision, mastery, level, unlock or context permission is created.
            foreach (var entry in IdMap?.Entries ?? Array.Empty<AnimalUpgradeIdMap.Entry>())
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.ArtId) || FindUnique(entry.ArtId) != entry) continue;
                snapshot.Animals.Add(new AnimalProgress
                {
                    AnimalId = entry.ArtId,
                    Implemented = entry.CampaignAnimal != null &&
                        entry.CampaignAnimal.ProductionActiveAbilityConfigured && entry.CampaignAnimal.ProductionPassiveAbilityConfigured,
                    AvailabilityMessage = entry.CampaignAnimal == null ? "ID 설정 대기" : "성장 설정 대기"
                });
            }
            return Task.FromResult(snapshot);
        }

        public static long? ReadVerifiedCoinBalance(IRewardLedgerAdapter ledger) =>
            ledger != null && ledger.IsAvailable && !ledger.IsDevelopmentOnly ? ledger.VerifiedCoinBalance : null;

        public override Task<UpgradeQuote> QuoteUpgradeAsync(string animalId, UpgradeTrack track, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = FindUnique(animalId);
            var quote = new UpgradeQuote { AnimalId = animalId, Track = track, State = UpgradeQuoteState.Unconfigured };
            if (!Enum.IsDefined(typeof(UpgradeTrack), track))
                quote.Message = "강화 트랙 설정 대기";
            else if (entry?.CampaignAnimal == null)
                quote.Message = "실제 동물 ID 매핑 설정 대기 · 초상만 열람할 수 있습니다.";
            else if (string.IsNullOrWhiteSpace(entry.GrowthServiceId))
                quote.Message = entry.CampaignAnimal.AnimalId + " 종 정의 확인 · 성장 서비스 ID 설정 대기. 레벨·숙련도·해금·효과·비용은 아직 확인할 수 없습니다.";
            else if (GrowthPolicy?.FindAnimal(entry.GrowthServiceId)?.IsOperationallyConfigured != true ||
                     UpgradeCatalog?.Find(entry.GrowthServiceId) == null)
                quote.Message = "해당 동물의 실제 성장 카탈로그 설정 대기 · 비용 누락은 무료가 아닙니다.";
            else
            {
                quote.State = UpgradeQuoteState.Unavailable;
                quote.Message = "동물별 성장 조회·견적·원자적 강화 서비스가 연결되지 않았습니다. 카탈로그만으로 구매할 수 없습니다.";
            }
            // Unknown levels remain -1, effects/costs/token absent and IsFree false.
            return Task.FromResult(quote);
        }

        public override Task<AnimalUiCommitResult> TryUpgradeAsync(UpgradeCommitRequest request, CancellationToken cancellationToken)
        {
            // No request is dispatched, so Unavailable is a definitive outcome, not Unknown.
            // IAccountGateway.RequestUpgrade is account stamina only; reward grants are not purchases.
            return Task.FromResult(new AnimalUiCommitResult
            {
                Status = CommitStatus.Unavailable,
                Message = "동물 강화 거래 서비스 미연결 · 재화와 레벨을 변경하지 않았습니다."
            });
        }

        private AnimalUpgradeIdMap.Entry FindUnique(string artId)
        {
            var matches = IdMap?.Entries?.Where(e => e != null && string.Equals(e.ArtId, artId, StringComparison.Ordinal)).Take(2).ToArray();
            return matches?.Length == 1 ? matches[0] : null;
        }
    }
}
