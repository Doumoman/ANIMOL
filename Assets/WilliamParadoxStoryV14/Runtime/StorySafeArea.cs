using UnityEngine;

namespace WilliamParadox.Story
{
    /// <summary>Attach once to the screen-space overlay Canvas safe-area child.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class StorySafeArea : MonoBehaviour
    {
        public RectTransform target;
        private Rect previous;
        private Vector2Int screen;
        private void OnEnable() { Refresh(); }
        private void Update()
        {
            if (previous != Screen.safeArea || screen.x != Screen.width || screen.y != Screen.height) Refresh();
        }
        public void Refresh()
        {
            if (target == null) target = transform as RectTransform;
            if (target == null || Screen.width <= 0 || Screen.height <= 0) return;
            var safe = Screen.safeArea;
            target.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            target.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            target.offsetMin = target.offsetMax = Vector2.zero;
            previous = safe; screen = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
