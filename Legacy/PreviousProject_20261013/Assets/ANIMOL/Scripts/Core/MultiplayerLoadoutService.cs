using System;
using System.Linq;

namespace ANIMOL.Core
{
    public enum MatchRequestStatus { Approved, Rejected, Unavailable }

    public readonly struct MatchRequestResult
    {
        public MatchRequestStatus Status { get; }
        public string Message { get; }
        public MatchRequestResult(MatchRequestStatus status, string message) { Status = status; Message = message; }
    }

    public interface IMatchGateway
    {
        bool IsConnected { get; }
        MatchRequestResult RequestReady(string[] selectedAnimalIds);
    }

    public sealed class DisconnectedMatchGateway : IMatchGateway
    {
        public bool IsConnected => false;
        public MatchRequestResult RequestReady(string[] selectedAnimalIds) =>
            new MatchRequestResult(MatchRequestStatus.Unavailable, "매치 서버가 연결되지 않아 Ready를 승인할 수 없습니다.");
    }

    public sealed class MultiplayerLoadoutService
    {
        private readonly ModeLoadoutRuleSet rules;
        private readonly IMatchGateway gateway;
        private readonly string[] selections;
        public bool IsReady { get; private set; }
        public System.Collections.Generic.IReadOnlyList<string> Selections => selections;

        public MultiplayerLoadoutService(ModeLoadoutRuleSet rules, IMatchGateway gateway)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            selections = new string[rules.SlotCount];
        }

        public bool TrySelect(int slotIndex, string animalId)
        {
            if (!rules.HasPlayerSelection || slotIndex < 0 || slotIndex >= selections.Length ||
                string.IsNullOrWhiteSpace(animalId) || !rules.AllowedAnimalIds.Contains(animalId)) return false;
            if (!rules.AllowDuplicates && selections.Where((value, index) => index != slotIndex).Contains(animalId)) return false;
            selections[slotIndex] = animalId;
            IsReady = false;
            return true;
        }

        public MatchRequestResult RequestReady()
        {
            if (selections.Any(string.IsNullOrWhiteSpace)) return new MatchRequestResult(MatchRequestStatus.Rejected, "모든 룰 슬롯을 선택해야 합니다.");
            var result = gateway.RequestReady(selections.ToArray());
            IsReady = result.Status == MatchRequestStatus.Approved;
            return result;
        }
    }
}
