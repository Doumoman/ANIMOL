using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Core
{
    // Serialized separately so Unity can bind this ScriptableObject to its MonoScript.
    [CreateAssetMenu(menuName = "ANIMOL/Economy/M6 Growth Economy Policy", fileName = "GrowthEconomyPolicyCatalog")]
    public sealed class GrowthEconomyPolicyCatalog : ScriptableObject
    {
        [SerializeField] private int version = 1;
        [SerializeField] private AnimalGrowthContract[] animalGrowth = Array.Empty<AnimalGrowthContract>();
        [SerializeField] private GrowthCompletionPackContract growthCompletionPack = new GrowthCompletionPackContract();
        [SerializeField] private EmoteAcquisitionContract[] emoteAcquisitions = Array.Empty<EmoteAcquisitionContract>();
        [SerializeField] private int sharedRewardedAdDailyLimit = 3;
        [SerializeField] private int lobbyGeneralCoinDraft = 150;
        [SerializeField] private string dailyResetAuthority = "ACCOUNT_SERVER";

        public int Version => version;
        public IReadOnlyList<AnimalGrowthContract> AnimalGrowth => animalGrowth;
        public GrowthCompletionPackContract GrowthCompletionPack => growthCompletionPack;
        public IReadOnlyList<EmoteAcquisitionContract> EmoteAcquisitions => emoteAcquisitions;
        public int SharedRewardedAdDailyLimit => sharedRewardedAdDailyLimit;
        public int LobbyGeneralCoinDraft => lobbyGeneralCoinDraft;
        public string DailyResetAuthority => dailyResetAuthority;
        public AnimalGrowthContract FindAnimal(string animalId) => animalGrowth?.FirstOrDefault(x => x != null && x.AnimalId == animalId);
    }
}
