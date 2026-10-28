using ANIMOL.Presentation;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // V3 reflector acts on all four incoming headings; show the actual slash rather than a two-sided corner.
    public sealed class EchoTheaterReflectorView : MonoBehaviour
    {
        static Sprite lineSprite;
        public void Configure(bool slash)
        {
            var view = GetComponent<DeviceVisualView>();
            if (view) { if (view.visual) view.visual.enabled = false; view.enabled = false; }
            var animated = GetComponent<ANIMOL.Art.EchoTheaterV03.EchoTheaterV03AnimatedVisual>();
            if (animated) animated.enabled = false;
            if (!lineSprite) lineSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1),
                new Vector2(.5f, .5f), 1f);
            var child = new GameObject("Reflector " + (slash ? "/" : "\\"));
            child.transform.SetParent(transform, false);
            child.transform.localScale = new Vector3(.9f, .1f, 1f);
            child.transform.localRotation = Quaternion.Euler(0, 0, slash ? 45f : -45f);
            var image = child.AddComponent<SpriteRenderer>(); image.sprite = lineSprite;
            image.color = new Color(1f, .88f, .46f); image.sortingOrder = 31;
        }
    }
}
