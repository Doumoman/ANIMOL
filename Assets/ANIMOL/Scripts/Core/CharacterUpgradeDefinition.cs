using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Account/Character Upgrade", fileName = "CharacterUpgradeDefinition")]
    public sealed class CharacterUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string animalId = string.Empty;
        [SerializeField] private string upgradeId = string.Empty;
        [SerializeField] private int maxLevel;
        [SerializeField] private int[] costByLevel = Array.Empty<int>();
        [SerializeField] private string effectKey = string.Empty;
        [SerializeField] private string modeApplicability = string.Empty;
        [SerializeField] private int version = 1;
        public string AnimalId => animalId;
        public string UpgradeId => upgradeId;
        public int MaxLevel => maxLevel;
        public IReadOnlyList<int> CostByLevel => costByLevel;
        public string EffectKey => effectKey;
        public string ModeApplicability => modeApplicability;
        public int Version => version;
    }
}
