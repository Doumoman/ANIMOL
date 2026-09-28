using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Development/Multiplayer Preview", fileName = "MultiplayerPreviewDefinition")]
    public sealed class MultiplayerPreviewDefinition : ScriptableObject
    {
        [SerializeField] private string previewId = string.Empty;
        [SerializeField] private GameModeKind mode;
        [SerializeField] private int slotCount;
        [SerializeField] private bool allowDuplicates;
        [SerializeField] private string[] allowedAnimalIds = Array.Empty<string>();
        [SerializeField] private bool developmentPreviewOnly = true;
        public string PreviewId => previewId;
        public GameModeKind Mode => mode;
        public int SlotCount => slotCount;
        public bool AllowDuplicates => allowDuplicates;
        public IReadOnlyList<string> AllowedAnimalIds => allowedAnimalIds;
        public bool DevelopmentPreviewOnly => developmentPreviewOnly;
        public ModeLoadoutRuleSet CreateRuleSet() => new ModeLoadoutRuleSet(mode, slotCount, allowDuplicates, allowedAnimalIds);
    }
}
