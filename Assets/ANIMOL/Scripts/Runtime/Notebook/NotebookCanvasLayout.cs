using UnityEngine;
using UnityEngine.UI;

namespace William.NotebookUI
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class NotebookCanvasLayout : MonoBehaviour
    {
        public CanvasScaler scaler;
        public RectTransform design;
        public RectTransform fullScreenSettingsBackdrop, fullScreenModalBackdrop, fullScreenMapBackdrop;
        public RectTransform mapScreen;
        private Rect previousSafe;
        private int previousWidth, previousHeight;
        private void OnEnable() => Reflow();
        private void LateUpdate()
        {
            if (Screen.width != previousWidth || Screen.height != previousHeight || Screen.safeArea != previousSafe) Reflow();
        }
        public void RefreshLayout() => Reflow();
        private void Reflow()
        {
            if (!scaler || !design || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            float scale = Mathf.Max(.01f, Mathf.Min(safe.width / 1080f, safe.height / 1500f));
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = scale;
            design.anchorMin = design.anchorMax = design.pivot = new Vector2(.5f, .5f);
            design.sizeDelta = new Vector2(1080, safe.height / scale);
            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * .5f;
            design.anchoredPosition = (safe.center - screenCenter) / scale;
            if (mapScreen)
            {
                mapScreen.anchorMin = mapScreen.anchorMax = mapScreen.pivot = new Vector2(.5f, .5f);
                mapScreen.sizeDelta = new Vector2(safe.width / scale, safe.height / scale);
                mapScreen.anchoredPosition = Vector2.zero;
            }
            FitBackdrop(fullScreenMapBackdrop, safe, screenCenter, scale);
            FitBackdrop(fullScreenSettingsBackdrop, safe, screenCenter, scale);
            FitBackdrop(fullScreenModalBackdrop, safe, screenCenter, scale);
            previousSafe = safe; previousWidth = Screen.width; previousHeight = Screen.height;
        }
        private static void FitBackdrop(RectTransform backdrop, Rect safe, Vector2 screenCenter, float scale)
        {
            if (!backdrop) return;
            backdrop.anchorMin = backdrop.anchorMax = backdrop.pivot = new Vector2(.5f, .5f);
            backdrop.sizeDelta = new Vector2(Screen.width / scale, Screen.height / scale);
            backdrop.anchoredPosition = (screenCenter - safe.center) / scale;
        }
    }
}
