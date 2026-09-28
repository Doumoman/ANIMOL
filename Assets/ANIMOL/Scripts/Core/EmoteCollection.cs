using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    [Serializable]
    public sealed class EmoteDefinition
    {
        [SerializeField] private string emoteId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string visualKey = string.Empty;
        [SerializeField] private bool grantedByDefault;
        public string EmoteId => emoteId;
        public string DisplayName => displayName;
        public string VisualKey => visualKey;
        public bool GrantedByDefault => grantedByDefault;
    }

    public sealed class EmoteLoadoutService
    {
        public const int QuickSlotCount = 4;
        private readonly HashSet<string> owned = new HashSet<string>(StringComparer.Ordinal);
        private readonly string[] slots = new string[QuickSlotCount];
        public IReadOnlyList<string> Slots => slots;

        public EmoteLoadoutService(IEnumerable<string> defaultOwned)
        {
            if (defaultOwned == null) return;
            var index = 0;
            foreach (var id in defaultOwned)
            {
                if (string.IsNullOrWhiteSpace(id) || !owned.Add(id)) continue;
                if (index < slots.Length) slots[index++] = id;
            }
        }

        public bool IsOwned(string emoteId) => !string.IsNullOrWhiteSpace(emoteId) && owned.Contains(emoteId);
        public bool TryEquip(int slotIndex, string emoteId)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length || !IsOwned(emoteId)) return false;
            slots[slotIndex] = emoteId;
            return true;
        }
    }
}
