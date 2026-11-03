using System;
using UnityEngine;
using UnityEngine.UI;

namespace William.NotebookUI
{
    [Serializable] public sealed class NotebookMapBackdropWindow
    {
        public RectTransform window;
        public float startY, height;
    }

    /// <summary>
    /// Pins a finished region illustration to the map viewport while its stars scroll.
    /// Only RectTransform geometry changes. Texture pixels, tint and material stay fixed.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(ScrollRect))]
    public sealed class NotebookMapBackdropLayout : MonoBehaviour
    {
        public ScrollRect scroll;
        public NotebookMapBackdropWindow[] windows = Array.Empty<NotebookMapBackdropWindow>();

        private ScrollRect observed;
        private float previousScroll = float.NaN, previousViewportHeight = float.NaN;

        private void OnEnable() { BindScroll(); RefreshLayout(); }
        private void OnDisable()
        {
            if (observed) observed.onValueChanged.RemoveListener(OnScrolled);
            observed = null;
        }
        private void OnRectTransformDimensionsChange() => RefreshLayout();
        private void OnValidate() { previousScroll = previousViewportHeight = float.NaN; }
        private void LateUpdate()
        {
            BindScroll();
            if (!scroll || !scroll.viewport || !scroll.content) return;
            if (scroll.content.anchoredPosition.y != previousScroll || scroll.viewport.rect.height != previousViewportHeight)
                RefreshLayout();
        }
        private void BindScroll()
        {
            if (!scroll) scroll = GetComponent<ScrollRect>();
            if (observed == scroll) return;
            if (observed) observed.onValueChanged.RemoveListener(OnScrolled);
            observed = scroll;
            if (observed) observed.onValueChanged.AddListener(OnScrolled);
        }
        private void OnScrolled(Vector2 unused) => RefreshLayout();

        public void RefreshLayout()
        {
            if (!scroll || !scroll.viewport || !scroll.content || windows == null) return;
            float viewportHeight = scroll.viewport.rect.height;
            if (viewportHeight <= 0) return;
            float scrollY = scroll.content.anchoredPosition.y;
            foreach (NotebookMapBackdropWindow item in windows)
            {
                if (item == null || !item.window) continue;
                float localTop = Mathf.Clamp(scrollY - item.startY, 0, Mathf.Max(0, item.height - viewportHeight));
                item.window.anchorMin = item.window.anchorMax = item.window.pivot = new Vector2(0, 1);
                item.window.anchoredPosition = new Vector2(0, -localTop);
                item.window.sizeDelta = new Vector2(1080, viewportHeight);
            }
            previousScroll = scrollY; previousViewportHeight = viewportHeight;
        }
    }
}
