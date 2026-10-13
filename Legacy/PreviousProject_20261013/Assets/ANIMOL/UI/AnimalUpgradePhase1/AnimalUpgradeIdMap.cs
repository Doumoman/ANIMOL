using System;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.AnimalUpgradePhase1
{
    /// <summary>Explicit identity evidence only; never grants ownership, implementation or purchase rights.</summary>
    public sealed class AnimalUpgradeIdMap : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string ArtId;
            public CampaignAnimalDefinition CampaignAnimal;
            public string GrowthServiceId;
        }
        public Entry[] Entries = Array.Empty<Entry>();
    }
}
