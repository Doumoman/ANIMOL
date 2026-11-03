using UnityEngine;

namespace ANIMOL.UI.StyleV19
{
    // Optional layout for a direct child of a shared Canvas. Do not run alongside another driver
    // of these same RectTransforms; existing LayoutGroups can use the same constants instead.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class AnimolUiV19ChromeLayout : MonoBehaviour
    {
        public RectTransform topPaper, bottomPaper, topContent, bottomContent;
        public RectTransform profile, sound, settings, modeRow, play;
        public RectTransform[] modes = new RectTransform[3];
        public bool useSafeArea = true;
        public const float Inset = 24, Gap = 12, Control = 132, Primary = 156;
        public const float Header = 180, Footer = 348, MaxContent = 2160;
        private bool applying;
        private Rect lastSafe;
        private Vector2 lastRoot;

        private void OnEnable() { RefreshLayout(); }
        private void OnRectTransformDimensionsChange() { RefreshLayout(); }
        private void Update()
        {
            var root = (RectTransform)transform;
            if (lastSafe != Screen.safeArea || lastRoot != root.rect.size) RefreshLayout();
        }

        [ContextMenu("Refresh V19 layout")]
        public void RefreshLayout()
        {
            if (applying || !isActiveAndEnabled) return;
            applying = true;
            try
            {
                var root = (RectTransform)transform;
                var area = root.rect;
                if (area.width <= 1 || area.height <= 1)
                {
                    lastSafe = Screen.safeArea; lastRoot = area.size;
                    return;
                }
                float left = 0, right = 0, top = 0, bottom = 0;
                var canvas = GetComponentInParent<Canvas>();
                var camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                if (useSafeArea && Screen.width > 0 && Screen.height > 0)
                {
                    var safe = Screen.safeArea;
                    Vector2 lo, hi;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, safe.min, camera, out lo) &&
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, safe.max, camera, out hi))
                    {
                        left = Mathf.Max(0, lo.x - area.xMin);
                        right = Mathf.Max(0, area.xMax - hi.x);
                        bottom = Mathf.Max(0, lo.y - area.yMin);
                        top = Mathf.Max(0, area.yMax - hi.y);
                    }
                }
                float width = Mathf.Min(MaxContent, Mathf.Max(1, area.width - left - right - 2 * Inset));
                float x = (left - right) * .5f;
                Strip(topPaper, true, Header + top);
                Strip(bottomPaper, false, Footer + bottom);
                Center(topContent, true, width, Control, x, -(top + Inset));
                Center(bottomContent, false, width, Control + Gap + Primary, x, bottom + Inset);
                Item(profile, true, 0, 276, Control);
                Item(settings, false, 0, Control, Control);
                Item(sound, false, Control + Gap, Control, Control);
                if (modeRow) { Strip(modeRow, true, Control); }
                if (play) { Strip(play, false, Primary); }
                float modeWidth = Mathf.Max(1, (width - 2 * Gap) / 3);
                if (modes != null)
                    for (int i = 0; i < Mathf.Min(3, modes.Length); i++)
                        Item(modes[i], true, i * (modeWidth + Gap), modeWidth, Control);
                lastSafe = Screen.safeArea;
                lastRoot = area.size;
            }
            finally { applying = false; }
        }

        private static void Strip(RectTransform r, bool top, float height)
        {
            if (!r) return;
            r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
            float y = top ? 1 : 0;
            r.anchorMin = new Vector2(0, y); r.anchorMax = new Vector2(1, y);
            r.pivot = new Vector2(.5f, y); r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(0, height);
        }
        private static void Center(RectTransform r, bool top, float width, float height, float x, float y)
        {
            if (!r) return;
            r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
            float a = top ? 1 : 0;
            r.anchorMin = r.anchorMax = new Vector2(.5f, a); r.pivot = new Vector2(.5f, a);
            r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(width, height);
        }
        private static void Item(RectTransform r, bool left, float inset, float width, float height)
        {
            if (!r) return;
            r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
            float x = left ? 0 : 1;
            r.anchorMin = r.anchorMax = new Vector2(x, .5f); r.pivot = new Vector2(x, .5f);
            r.anchoredPosition = new Vector2(left ? inset : -inset, 0);
            r.sizeDelta = new Vector2(width, height);
        }
    }
}
