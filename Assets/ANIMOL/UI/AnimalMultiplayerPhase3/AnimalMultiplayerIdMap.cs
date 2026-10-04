using System;
using System.Linq;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.AnimalMultiplayerPhase3
{
    [CreateAssetMenu(menuName = "ANIMOL/Animal UI v2/Multiplayer ID map")]
    public sealed class AnimalMultiplayerIdMap : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string ArtId;
            public CampaignAnimalDefinition CampaignAnimal;
        }
        public Entry[] Entries = Array.Empty<Entry>();
        // Reject ambiguous mappings in either direction. DEV forms are never species IDs.
        public string ToArtId(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId) || projectId.StartsWith("DEV_", StringComparison.Ordinal)) return null;
            var matches = Entries.Where(e => e?.CampaignAnimal != null && e.CampaignAnimal.AnimalId == projectId).ToArray();
            return matches.Length == 1 && Entries.Count(e => e?.ArtId == matches[0].ArtId) == 1 ? matches[0].ArtId : null;
        }
        public CampaignAnimalDefinition ToProjectAnimal(string artId)
        {
            var matches = Entries.Where(e => e?.ArtId == artId && e.CampaignAnimal != null).ToArray();
            return matches.Length == 1 && ToArtId(matches[0].CampaignAnimal.AnimalId) == artId ? matches[0].CampaignAnimal : null;
        }
    }
}
