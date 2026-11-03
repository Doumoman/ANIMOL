using System;
using UnityEngine;
using UnityEngine.UI;

namespace William.NotebookUI
{
    /// <summary>Displays supplied digit PNGs only. The gameplay model owns the numeric value.</summary>
    [DisallowMultipleComponent]
    public sealed class NotebookSpriteNumber : MonoBehaviour
    {
        public RectTransform area;
        public Image[] places = Array.Empty<Image>();
        public Sprite[] digits = Array.Empty<Sprite>();
        public float maximumDigitHeight = 204f, gap = 6f;
        private int value;
        public int Value => value;
        public void SetNumber(int number)
        {
            value = Mathf.Max(0, number);
            Render();
        }
        private void OnEnable() => Render();
        private void OnRectTransformDimensionsChange() => Render();
        private void Render()
        {
            if (!area || places == null || places.Length == 0 || digits == null || digits.Length < 10) return;
            string text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            int count = Mathf.Min(text.Length, places.Length);
            float ratio = digits[0] ? digits[0].rect.width / digits[0].rect.height : 220f / 280f;
            float height = Mathf.Min(maximumDigitHeight, area.rect.height);
            float width = height * ratio;
            if (count * width + Mathf.Max(0, count - 1) * gap > area.rect.width)
            { width = Mathf.Max(1, (area.rect.width - Mathf.Max(0, count - 1) * gap) / count); height = width / Mathf.Max(.01f, ratio); }
            float left = (area.rect.width - count * width - Mathf.Max(0, count - 1) * gap) * .5f;
            for (int i = 0; i < places.Length; i++)
            {
                Image image = places[i]; if (!image) continue;
                bool visible = i < count; image.gameObject.SetActive(visible); if (!visible) continue;
                image.sprite = digits[text[i] - '0']; image.color = Color.white; image.preserveAspect = true; image.raycastTarget = false;
                RectTransform r = image.rectTransform; r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
                r.anchoredPosition = new Vector2(left + i * (width + gap), 0); r.sizeDelta = new Vector2(width, height);
            }
        }
    }
}
