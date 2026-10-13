using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI
{
    public enum UiScreenStateKind { Loading, Ready, Empty, Locked, Unavailable, Error }

    public sealed class UiScreenState : MonoBehaviour
    {
        [SerializeField] private UiScreenStateKind state = UiScreenStateKind.Ready;
        [SerializeField] private Text stateLabel;
        public UiScreenStateKind State => state;

        public void Set(UiScreenStateKind value, string message)
        {
            state = value;
            if (stateLabel != null) stateLabel.text = message ?? string.Empty;
        }
    }
}
