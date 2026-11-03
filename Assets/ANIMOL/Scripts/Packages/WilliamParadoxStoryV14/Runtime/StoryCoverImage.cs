using UnityEngine;
using UnityEngine.UI;

namespace WilliamParadox.Story
{
    /// <summary>Uniform cover-fit inside a fixed masked viewport. Never stretches source art.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class StoryCoverImage : MonoBehaviour
    {
        public RectTransform viewport;
        public Image image;
        private Vector2 lastSize;
        private Sprite lastSprite;
        private void OnEnable() { Refresh(); }
        private void LateUpdate()
        {
            if (viewport != null && image != null && (lastSize != viewport.rect.size || lastSprite != image.sprite)) Refresh();
        }
        public void Refresh()
        {
            if (image == null) image = GetComponent<Image>();
            if (image == null || viewport == null || image.sprite == null) return;
            var size = viewport.rect.size;
            var source = image.sprite.rect.size;
            if (size.x <= 0 || size.y <= 0 || source.x <= 0 || source.y <= 0) return;
            var factor = Mathf.Max(size.x / source.x, size.y / source.y);
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = source * factor;
            rect.localScale = Vector3.one;
            image.preserveAspect = true;
            lastSize = size; lastSprite = image.sprite;
        }
    }
}
