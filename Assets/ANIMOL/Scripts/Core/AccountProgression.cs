using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    [Serializable]
    public sealed class AccountUpgradeTrackDefinition
    {
        [SerializeField] private string trackId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private int maxLevel = 10;
        [SerializeField] private float[] percentByLevel = Array.Empty<float>();
        [SerializeField] private int[] costByLevel = Array.Empty<int>();

        public string TrackId => trackId;
        public string DisplayName => displayName;
        public int MaxLevel => maxLevel;
        public IReadOnlyList<float> PercentByLevel => percentByLevel;
        public IReadOnlyList<int> CostByLevel => costByLevel;
        public float TotalPercentDelta => percentByLevel == null || percentByLevel.Length == 0 ? 0f : percentByLevel[percentByLevel.Length - 1] - percentByLevel[0];
        public int CostForNextLevel(int currentLevel) => currentLevel >= 0 && currentLevel < costByLevel.Length ? costByLevel[currentLevel] : 0;
    }

    public enum AccountRequestStatus { Approved, Rejected, Unavailable, Duplicate }

    public enum AccountUpgradeUiState { Available, Max, InsufficientCoins, Unavailable }

    public readonly struct AccountUpgradeUiSnapshot
    {
        public AccountUpgradeUiState State { get; }
        public int CurrentLevel { get; }
        public int NextCost { get; }

        public AccountUpgradeUiSnapshot(AccountUpgradeUiState state, int currentLevel, int nextCost)
        {
            State = state;
            CurrentLevel = currentLevel;
            NextCost = nextCost;
        }
    }

    public static class AccountUpgradeStateResolver
    {
        public static AccountUpgradeUiSnapshot Resolve(AccountUpgradeTrackDefinition track, int currentLevel, int? approvedCoinBalance, bool accountServerConnected)
        {
            if (track == null || !accountServerConnected || !approvedCoinBalance.HasValue)
                return new AccountUpgradeUiSnapshot(AccountUpgradeUiState.Unavailable, Math.Max(0, currentLevel), 0);

            var clampedLevel = Math.Max(0, Math.Min(currentLevel, track.MaxLevel));
            if (clampedLevel >= track.MaxLevel)
                return new AccountUpgradeUiSnapshot(AccountUpgradeUiState.Max, clampedLevel, 0);

            var nextCost = track.CostForNextLevel(clampedLevel);
            return new AccountUpgradeUiSnapshot(
                approvedCoinBalance.Value < nextCost ? AccountUpgradeUiState.InsufficientCoins : AccountUpgradeUiState.Available,
                clampedLevel,
                nextCost);
        }
    }

    public readonly struct AccountRequestResult
    {
        public AccountRequestStatus Status { get; }
        public string Message { get; }
        public AccountRequestResult(AccountRequestStatus status, string message) { Status = status; Message = message; }
    }

    public interface IAccountGateway
    {
        bool IsConnected { get; }
        AccountRequestResult RequestUpgrade(string transactionId, string trackId, int expectedLevel, int catalogVersion);
    }

    public sealed class DisconnectedAccountGateway : IAccountGateway
    {
        public bool IsConnected => false;
        public AccountRequestResult RequestUpgrade(string transactionId, string trackId, int expectedLevel, int catalogVersion) =>
            new AccountRequestResult(AccountRequestStatus.Unavailable, "계정 서버가 연결되지 않아 구매를 승인할 수 없습니다.");
    }

    public sealed class UpgradePurchaseUseCase
    {
        private readonly IAccountGateway gateway;
        private readonly HashSet<string> requestIds = new HashSet<string>(StringComparer.Ordinal);
        public UpgradePurchaseUseCase(IAccountGateway gateway) => this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        public AccountRequestResult Request(string transactionId, AccountUpgradeTrackDefinition track, int expectedLevel, int catalogVersion)
        {
            if (string.IsNullOrWhiteSpace(transactionId) || track == null)
                return new AccountRequestResult(AccountRequestStatus.Rejected, "유효하지 않은 성장 요청입니다.");
            if (!requestIds.Add(transactionId)) return new AccountRequestResult(AccountRequestStatus.Duplicate, "중복 요청은 처리하지 않습니다.");
            return gateway.RequestUpgrade(transactionId, track.TrackId, expectedLevel, catalogVersion);
        }
    }
}
