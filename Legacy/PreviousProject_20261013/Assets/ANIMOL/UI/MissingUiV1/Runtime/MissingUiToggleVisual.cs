using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.MissingUiV1
{
    [RequireComponent(typeof(Toggle))]
    public sealed class MissingUiToggleVisual : MonoBehaviour
    {
        public Image Track;
        public RectTransform Knob;
        public Sprite OffSprite, OnSprite;
        private Toggle _toggle;
        private void Awake() { _toggle = GetComponent<Toggle>(); _toggle.onValueChanged.AddListener(Apply); }
        private void OnEnable() { if (_toggle == null) _toggle = GetComponent<Toggle>(); Apply(_toggle.isOn); }
        private void Apply(bool on)
        {
            if (Track != null) Track.sprite = on ? OnSprite : OffSprite;
            if (Knob != null) Knob.anchorMin = Knob.anchorMax = new Vector2(on ? .75f : .25f, .5f);
        }
        public void Refresh() { if (_toggle == null) _toggle = GetComponent<Toggle>(); Apply(_toggle.isOn); }
    }
}
