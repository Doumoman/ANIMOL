using UnityEngine;

namespace ANIMOL.StarRainBattle
{
    // Fit the complete authored board/HUD together; gameplay coordinates stay 400x620.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class StarRainBattleSafeArea : MonoBehaviour
    {
        public RectTransform Design;
        public Vector2 ReferenceSize = new Vector2(1080, 1920);

        void LateUpdate()
        {
            Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }

        public void Apply(Rect safeArea, Vector2 screenSize)
        {
            if (!Design || screenSize.x <= 0 || screenSize.y <= 0) return;
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (!parent || ReferenceSize.x <= 0 || ReferenceSize.y <= 0) return;
            if (safeArea.width <= 0 || safeArea.height <= 0) safeArea = new Rect(Vector2.zero, screenSize);
            var min = new Vector2(Mathf.Clamp01(safeArea.xMin / screenSize.x), Mathf.Clamp01(safeArea.yMin / screenSize.y));
            var max = new Vector2(Mathf.Clamp01(safeArea.xMax / screenSize.x), Mathf.Clamp01(safeArea.yMax / screenSize.y));
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var available = Vector2.Scale(parent.rect.size, max - min);
            var scale = Mathf.Min(available.x / ReferenceSize.x, available.y / ReferenceSize.y);
            Design.anchorMin = Design.anchorMax = Design.pivot = new Vector2(.5f, .5f);
            Design.anchoredPosition = Vector2.zero;
            Design.sizeDelta = ReferenceSize;
            Design.localScale = Vector3.one * Mathf.Max(0, scale);
        }
    }
}
