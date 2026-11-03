using UnityEngine;

namespace WilliamParadox.Story
{
    /// <summary>Cover-fits one logical1080x1920 stage. Actor coordinates stay stable across devices.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class StoryLayerStage : MonoBehaviour
    {
        public RectTransform viewport;
        public Vector2 logicalSize = new Vector2(1080, 1920);
        public Vector2 DisplayedSize { get; private set; }
        private void OnEnable() { Refresh(); }
        private void LateUpdate() { Refresh(); }
        public void Refresh()
        {
            var root = transform as RectTransform;
            if (root == null || viewport == null || logicalSize.x <= 0 || logicalSize.y <= 0) return;
            var size = viewport.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            float cover = Mathf.Max(size.x / logicalSize.x, size.y / logicalSize.y);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = logicalSize;
            root.localScale = Vector3.one * cover;
            DisplayedSize = logicalSize * cover;
        }
    }
}
