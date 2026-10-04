using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.MissingUiV1
{
    [DisallowMultipleComponent]
    public sealed class MissingUiBinding : MonoBehaviour
    {
        public string SemanticKey;
        public string Kind;
        public string Action;
        public bool DesignEnabled;
        public bool RequiresHost;
        public string LocalChoiceValue;
        public string LocalGroupKey;
        public Sprite InspectSprite;
        public Selectable Control;
    }
}
