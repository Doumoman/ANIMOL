using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Account/Character Upgrade Catalog", fileName = "CharacterUpgradeCatalog")]
    public sealed class CharacterUpgradeCatalog : ScriptableObject
    {
        [SerializeField] private string[] previewAnimalIds = Array.Empty<string>();
        [SerializeField] private CharacterUpgradeDefinition[] definitions = Array.Empty<CharacterUpgradeDefinition>();
        public IReadOnlyList<string> PreviewAnimalIds => previewAnimalIds;
        public IReadOnlyList<CharacterUpgradeDefinition> Definitions => definitions;
        public CharacterUpgradeDefinition Find(string animalId)
        {
            foreach (var definition in definitions) if (definition != null && definition.AnimalId == animalId) return definition;
            return null;
        }
    }
}
