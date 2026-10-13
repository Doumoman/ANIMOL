using ANIMOL.Typography;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.MissingUiV1.Project
{
    // Native uGUI, under the existing Canvas/SafeArea. No independent input owner.
    public static class MissingUiLayout
    {
        public static readonly Color Ink = new Color32(26, 28, 44, 255);
        public static readonly Color White = new Color32(244, 244, 244, 255);
        public static readonly Color Muted = new Color32(148, 176, 194, 255);
        public static RectTransform Node(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); return rect;
        }
        public static void Rect(RectTransform r, Vector2 min, Vector2 max, Vector2 lo, Vector2 hi)
        { r.anchorMin = min; r.anchorMax = max; r.offsetMin = lo; r.offsetMax = hi; }
        public static void Fill(RectTransform r, float pad = 0) => Rect(r, Vector2.zero, Vector2.one, Vector2.one * pad, Vector2.one * -pad);
        public static Image Picture(RectTransform r, Sprite sprite, bool sliced = false)
        {
            var image = r.gameObject.AddComponent<Image>(); image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced; image.raycastTarget = sliced;
            return image;
        }
        public static void Height(RectTransform r, float height)
        { var e = r.gameObject.AddComponent<LayoutElement>(); e.minHeight = height; e.preferredHeight = height; }
        public static void Vertical(RectTransform r, int padding, float spacing)
        {
            var layout = r.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding,padding,padding,padding); layout.spacing = spacing;
            layout.childControlHeight = true; layout.childForceExpandHeight = false;
            layout.childControlWidth = true; layout.childForceExpandWidth = true;
        }
        public static Text Label(string name, Transform parent, string value, int size, MissingUiArt art, bool flowing = false)
        {
            var rect = Node(name, parent); var label = rect.gameObject.AddComponent<Text>();
            label.font = art.LegacyFont; label.fontSize = size; label.text = value; label.color = White;
            label.raycastTarget = false; label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow;
            var bridge = rect.gameObject.AddComponent<PixelTextBridge>(); bridge.Profile = art.Typography; bridge.SizeScrollContentToText = flowing;
            if (!flowing) Height(rect, size * 1.8f);
            return label;
        }
        public static Button Button(string name, Transform parent, string caption, MissingUiArt art, bool primary = false)
        {
            var rect = Node(name, parent); Height(rect, 112);
            Picture(rect, primary ? art.Primary : art.Secondary, true);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var text = Label("Caption", rect, caption, 32, art); Fill(text.rectTransform, 18); text.alignment = TextAnchor.MiddleCenter;
            rect.gameObject.AddComponent<UiButtonFeedback>();
            rect.gameObject.AddComponent<MissingUiButtonSkin>().DarkText=!primary;
            return button;
        }
        public static ScrollRect Scroll(Transform parent, MissingUiArt art)
        {
            var root = Node("BodyScroll", parent); var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Node("Viewport", root); Fill(viewport); viewport.offsetMax = new Vector2(-44, 0);
            viewport.gameObject.AddComponent<RectMask2D>(); var raycast = Picture(viewport, null); raycast.color = Color.clear; raycast.raycastTarget = true;
            var content = Node("Content", viewport); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f,1); content.sizeDelta = Vector2.zero; Vertical(content, 8, 24);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            var bar = Node("Scrollbar", root); Rect(bar, Vector2.right, Vector2.one, new Vector2(-36,0), Vector2.zero);
            Picture(bar, art.ScrollTrack, true); var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            var handle = Node("Handle", bar); Fill(handle); var handleImage = Picture(handle, art.ScrollThumb, true);
            scrollbar.direction = Scrollbar.Direction.BottomToTop; scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage;
            scroll.verticalScrollbar = scrollbar; return scroll;
        }
    }
}
