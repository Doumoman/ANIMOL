using System;
using UnityEngine;

namespace ANIMOL.Core
{
    public enum GameModeKind { Campaign, Competitive, Ranked, Coop, PrivateLobby }

    [Serializable]
    public sealed class ModeLoadoutRuleSet
    {
        [SerializeField] private GameModeKind mode;
        [SerializeField] private int slotCount;
        [SerializeField] private bool allowDuplicates;
        [SerializeField] private string[] allowedAnimalIds = Array.Empty<string>();

        public GameModeKind Mode => mode;
        public int SlotCount => slotCount;
        public bool AllowDuplicates => allowDuplicates;
        public System.Collections.Generic.IReadOnlyList<string> AllowedAnimalIds => allowedAnimalIds;
        public bool HasPlayerSelection => mode != GameModeKind.Campaign;

        public ModeLoadoutRuleSet(GameModeKind mode, int slotCount, bool allowDuplicates, string[] allowedAnimalIds)
        {
            this.mode = mode;
            this.slotCount = Math.Max(0, slotCount);
            this.allowDuplicates = allowDuplicates;
            this.allowedAnimalIds = allowedAnimalIds ?? Array.Empty<string>();
        }
    }
}
