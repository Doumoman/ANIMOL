using UnityEngine;

namespace ANIMOL.AnimalUiV2
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class AnimalUiSafeArea : MonoBehaviour
    {
        private Rect _last;
        private Vector2Int _size;
        private void OnEnable() => Apply();
        private void Update()
        {
            if (_last != Screen.safeArea || _size.x != Screen.width || _size.y != Screen.height) Apply();
        }
        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            _last = Screen.safeArea; _size = new Vector2Int(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(_last.xMin / Screen.width, _last.yMin / Screen.height);
            rect.anchorMax = new Vector2(_last.xMax / Screen.width, _last.yMax / Screen.height);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
    }
}
