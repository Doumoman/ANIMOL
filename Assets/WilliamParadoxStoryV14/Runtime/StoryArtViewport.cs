using UnityEngine;

namespace WilliamParadox.Story
{
    /// <summary>Keeps the covered scene at its portrait composition on screens taller than 9:16.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class StoryArtViewport : MonoBehaviour
    {
        private RectTransform viewport;
        private RectTransform parent;
        private Vector2 lastParentSize;
        private bool refreshing;

        private void OnEnable() { Refresh(); }
        private void OnRectTransformDimensionsChange() { Refresh(); }

        private void LateUpdate()
        {
            if (parent != null && parent.rect.size != lastParentSize) Refresh();
        }

        public void Refresh()
        {
            if (refreshing) return;
            if (viewport == null) viewport = (RectTransform)transform;
            parent = viewport.parent as RectTransform;
            if (parent == null) return;
            var size = parent.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            var height = Mathf.Min(size.y, size.x * 16f / 9f);
            refreshing = true;
            try
            {
                lastParentSize = size;
                viewport.anchorMin = new Vector2(0, 1);
                viewport.anchorMax = Vector2.one;
                viewport.pivot = new Vector2(0.5f, 1);
                viewport.anchoredPosition = Vector2.zero;
                viewport.sizeDelta = new Vector2(0, height);
            }
            finally { refreshing = false; }
        }
    }
}
