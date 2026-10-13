using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.MissingUiV1
{
    public sealed class MissingUiChoiceVisual : MonoBehaviour
    {
        public Image Radio;
        public Sprite Unselected, Selected;
        public void Apply(bool selected) { if (Radio != null) Radio.sprite = selected ? Selected : Unselected; }
    }
}
