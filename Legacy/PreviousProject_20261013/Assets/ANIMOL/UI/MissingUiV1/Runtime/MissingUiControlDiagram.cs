using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ANIMOL.MissingUiV1
{
    // Local layout illustration, not gameplay HUD or stored preferences.
    public sealed class MissingUiControlDiagram : MonoBehaviour
    {
        public RectTransform Controls;
        public CanvasGroup ControlsOpacity;
        public GameObject DownButton;
        public void Apply(Dictionary<string, string> values)
        {
            var size = Read(values, "size", .5f); var position = Read(values, "position", .5f); var opacity = Read(values, "opacity", .5f);
            string hand; values.TryGetValue("hand", out hand);
            if (Controls != null)
            {
                Controls.localScale = Vector3.one * (.75f + size * .5f);
                Controls.anchoredPosition = new Vector2(hand == "left" ? -24 : 24, -100 + position * 80);
            }
            if (ControlsOpacity != null) ControlsOpacity.alpha = opacity;
            string down; if (DownButton != null) DownButton.SetActive(values.TryGetValue("down", out down) && down == "true");
        }
        private static float Read(Dictionary<string, string> values, string key, float fallback)
        {
            string text; float value;
            return values.TryGetValue(key, out text) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? Mathf.Clamp01(value) : fallback;
        }
    }
}
