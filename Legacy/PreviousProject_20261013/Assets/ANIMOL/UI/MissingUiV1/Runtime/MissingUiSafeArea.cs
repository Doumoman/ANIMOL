using UnityEngine;

namespace ANIMOL.MissingUiV1
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class MissingUiSafeArea : MonoBehaviour
    {
        private Rect _last;
        private int _width, _height;
        private void OnEnable() { Apply(); }
        private void Update()
        {
            if (_last != Screen.safeArea || _width != Screen.width || _height != Screen.height) Apply();
        }
        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rect = (RectTransform)transform;
            _last = Screen.safeArea; _width = Screen.width; _height = Screen.height;
            rect.anchorMin = new Vector2(_last.xMin / _width, _last.yMin / _height);
            rect.anchorMax = new Vector2(_last.xMax / _width, _last.yMax / _height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
