using UnityEngine;

namespace ANIMOL.AnimalUiV2
{
    [CreateAssetMenu(menuName = "ANIMOL/Animal UI v2/Stage Selection Policy", fileName = "StageSelectionPolicyV2")]
    public sealed class StageSelectionPolicyAsset : ScriptableObject
    {
        public StageSelectionRequest Configuration = new StageSelectionRequest();
        // The host supplies live initial loadout and verifies this policy against its existing stage catalogue.
        public StageSelectionRequest CreateRequest(AnimalLoadout confirmedLoadout)
        {
            var request = (Configuration ?? new StageSelectionRequest()).Clone();
            request.InitialLoadout = (confirmedLoadout ?? new AnimalLoadout()).Clone(); return request;
        }
    }
}
