using UnityEngine;

namespace ANIMOL.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private RectTransform target;

        private void Awake() => target = (RectTransform)transform;
        private void OnEnable() => Apply();
        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height) Apply();
        }

        public void Apply()
        {
            if (target == null) target = (RectTransform)transform;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var safe = Screen.safeArea;
            target.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            target.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
            lastSafeArea = safe;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
