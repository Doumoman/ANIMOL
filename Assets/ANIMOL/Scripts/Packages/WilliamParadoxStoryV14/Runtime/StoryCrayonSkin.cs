using UnityEngine;
using UnityEngine.UI;

namespace Animol.Starway
{
    /// <summary>One UI clock. Only the shader's prepainted A/B/C choice changes.</summary>
    [DisallowMultipleComponent, AddComponentMenu("ANIMOL/Story Crayon Skin")]
    public sealed class StoryCrayonSkin : MonoBehaviour
    {
        [SerializeField] private bool reducedMotion;
        [SerializeField] private StoryCrayonFrame[] externalFrames = System.Array.Empty<StoryCrayonFrame>();
        private static readonly int Clock = Shader.PropertyToID("_ANIMOLStoryCrayonUnscaledTime");
        public bool ReducedMotion { get { return reducedMotion; } }
        private void OnEnable() { Tick(); RefreshFrames(); }
        private void Update() { Tick(); }
        private void Tick() { Shader.SetGlobalFloat(Clock, Time.unscaledTime); }
        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            RefreshFrames();
        }
        public void SetExternalFrames(StoryCrayonFrame[] frames)
        {
            externalFrames = frames ?? System.Array.Empty<StoryCrayonFrame>();
            RefreshFrames();
        }
        /// <summary>Connects a host-created control to already painted sprite/material states.</summary>
        public void DecorateExternalControl(Selectable control, StoryCrayonFrame template)
        {
            if (control == null || template == null) return;
            var fill = Decoration(control.transform, "CrayonQuietFill");
            fill.color = new Color(5f / 255f, 6f / 255f, 9f / 255f, .73f);
            fill.rectTransform.offsetMin = new Vector2(9, 9); fill.rectTransform.offsetMax = new Vector2(-9, -9);
            var border = Decoration(control.transform, "CrayonOutline");
            border.transform.SetAsFirstSibling(); fill.transform.SetAsFirstSibling();
            var frame = control.GetComponent<StoryCrayonFrame>() ?? control.gameObject.AddComponent<StoryCrayonFrame>();
            frame.ConfigureFrom(template, border, control, this);
            if (control.targetGraphic is Image background)
            { var color = background.color; color.a = 0; background.color = color; }
            if (System.Array.IndexOf(externalFrames, frame) < 0)
            { int length = externalFrames.Length; System.Array.Resize(ref externalFrames, length + 1); externalFrames[length] = frame; }
        }
        private static Image Decoration(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child.GetComponent<Image>();
            var image = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
            image.gameObject.layer = parent.gameObject.layer; image.transform.SetParent(parent, false);
            var rect = image.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; image.raycastTarget = false;
            image.GetComponent<LayoutElement>().ignoreLayout = true;
            return image;
        }
        private void RefreshFrames()
        {
            foreach (var frame in GetComponentsInChildren<StoryCrayonFrame>(true)) frame.RefreshNow();
            foreach (var frame in externalFrames) if (frame != null) frame.RefreshNow();
        }
    }
}
