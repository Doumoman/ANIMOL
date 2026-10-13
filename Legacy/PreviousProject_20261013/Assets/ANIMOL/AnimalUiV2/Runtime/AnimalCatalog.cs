using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.AnimalUiV2
{
    [Serializable]
    public sealed class AnimalDefinition
    {
        public string Id;
        public string DisplayName;
        public AnimalRole Role;
        public Sprite Portrait;
        [TextArea] public string ArtDescription;
    }

    [CreateAssetMenu(menuName = "ANIMOL/Animal UI v2/Catalog", fileName = "AnimalCatalogV2")]
    public sealed class AnimalCatalog : ScriptableObject
    {
        public List<AnimalDefinition> Animals = new List<AnimalDefinition>();
        public AnimalDefinition Find(string id) => Animals.Find(a => string.Equals(a.Id, id, StringComparison.Ordinal));
        public IEnumerable<AnimalDefinition> ForRole(AnimalRole role) => Animals.FindAll(a => a.Role == role);
        public bool IsValid(out string error)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var animal in Animals)
            {
                if (animal == null || string.IsNullOrEmpty(animal.Id) || !ids.Add(animal.Id))
                { error = "동물 ID가 비어 있거나 중복되어 있습니다."; return false; }
                if (animal.Portrait == null) { error = animal.Id + " 초상화가 연결되지 않았습니다."; return false; }
                if (!Enum.IsDefined(typeof(AnimalRole), animal.Role)) { error = animal.Id + " 역할이 잘못되었습니다."; return false; }
            }
            foreach (AnimalRole role in Enum.GetValues(typeof(AnimalRole)))
                if (!Animals.Exists(animal => animal.Role == role)) { error = AnimalUiRules.RoleName(role) + " 열람 동물이 없습니다."; return false; }
            error = null;
            return true;
        }
    }
}
