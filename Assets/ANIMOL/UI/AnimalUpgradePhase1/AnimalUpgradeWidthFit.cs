using UnityEngine;

namespace ANIMOL.AnimalUpgradePhase1
{
    /// <summary>Fits the 1080-reference layout inside the existing safe area without applying its insets twice.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class AnimalUpgradeWidthFit : MonoBehaviour
    {
        private void OnEnable() => Apply();
        private void LateUpdate() => Apply();
        private void Apply()
        {
            var rect = (RectTransform)transform;
            if (!(rect.parent is RectTransform parent) || parent.rect.width <= 0) return;
            float scale = parent.rect.width / 1080f;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1080, parent.rect.height / scale);
            rect.localScale = new Vector3(scale, scale, 1);
        }
    }
}
