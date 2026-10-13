using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Services/Availability", fileName = "ExternalServiceConfiguration")]
    public sealed class ExternalServiceConfiguration : ScriptableObject
    {
        [SerializeField] private bool accountServerConnected;
        [SerializeField] private bool matchServerConnected;
        [SerializeField] private bool purchaseSdkConnected;
        [SerializeField] private bool rewardedAdSdkConnected;
        public bool AccountServerConnected => accountServerConnected;
        public bool MatchServerConnected => matchServerConnected;
        public bool PurchaseSdkConnected => purchaseSdkConnected;
        public bool RewardedAdSdkConnected => rewardedAdSdkConnected;
    }
}
