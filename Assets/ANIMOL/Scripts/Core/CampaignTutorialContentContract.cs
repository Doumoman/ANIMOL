using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Campaign/Tutorial Content Contract", fileName = "CampaignTutorialContentContract")]
    public sealed class CampaignTutorialContentContract : ScriptableObject
    {
        [SerializeField] private int version = 1;
        [SerializeField] private string[] orderedActionKeys = Array.Empty<string>();
        [SerializeField] private bool autoShowMandatoryPopup;
        [SerializeField] private bool requiresPlayableMapContent = true;

        public int Version => version;
        public IReadOnlyList<string> OrderedActionKeys => orderedActionKeys;
        public bool AutoShowMandatoryPopup => autoShowMandatoryPopup;
        public bool RequiresPlayableMapContent => requiresPlayableMapContent;
    }
}
