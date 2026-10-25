using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.SanctuaryStory
{
    public enum SanctuaryWorldShape { Fold, Path, Boundary }

    /// <summary>
    /// A sharp paper-like fold and two world-space floor masks. These are geometry,
    /// not character substitutes. They never receive UI pointer events.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class SanctuaryFoldGraphic : MaskableGraphic
    {
        [SerializeField] private SanctuaryWorldShape shape;
        [SerializeField, Range(0f, 1f)] private float progress;

        public void SetShape(SanctuaryWorldShape value) { shape = value; raycastTarget = false; SetVerticesDirty(); }
        public void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(progress, value)) return;
            progress = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (progress <= 0f) return;
            Rect r = rectTransform.rect;
            if (shape == SanctuaryWorldShape.Fold)
            {
                // Exactly one moving crease; no flashing, zoom, noise or repeated pulses.
                if (progress >= 1f) return;
                float span = r.width * .27f;
                bool closing = progress < .5f;
                float edge = closing
                    ? Mathf.Lerp(r.xMin, r.xMax + span, progress * 2f)
                    : Mathf.Lerp(r.xMin - span, r.xMax, (progress - .5f) * 2f);
                float top = Mathf.Clamp(edge, r.xMin, r.xMax);
                float bottom = Mathf.Clamp(edge - span, r.xMin, r.xMax);
                if (closing)
                    Quad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax),
                        new Vector2(top, r.yMax), new Vector2(bottom, r.yMin), color);
                else
                    Quad(vh, new Vector2(bottom, r.yMin), new Vector2(top, r.yMax),
                        new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), color);
                Color crease = color; crease.r *= .7f; crease.g *= .7f; crease.b *= .7f;
                float thickness = r.width * .025f;
                Quad(vh, new Vector2(bottom, r.yMin), new Vector2(top, r.yMax),
                    new Vector2(Mathf.Min(r.xMax, top + thickness), r.yMax),
                    new Vector2(Mathf.Min(r.xMax, bottom + thickness), r.yMin), crease);
            }
            else if (shape == SanctuaryWorldShape.Path)
            {
                // Reveal a near-foot patch before extending the bend. The far world stays dark.
                float right = Mathf.Lerp(r.xMin, r.xMax, progress);
                float bend = Mathf.Min(right, r.xMin + r.width * .54f);
                Quad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + r.width * .13f, r.yMin + r.height * .48f),
                    new Vector2(bend, r.yMin + r.height * .77f), new Vector2(bend, r.yMin + r.height * .2f), color);
                if (progress > .54f)
                    Quad(vh, new Vector2(bend, r.yMin + r.height * .2f), new Vector2(bend, r.yMin + r.height * .77f),
                        new Vector2(right, r.yMax), new Vector2(right, r.yMin + r.height * .44f), color);
            }
            else
            {
                Color shade = color; shade.a *= progress;
                Quad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + r.width * .3f, r.yMax),
                    new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), shade);
            }
        }

        private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, tint, Vector2.zero); vh.AddVert(b, tint, Vector2.up);
            vh.AddVert(c, tint, Vector2.one); vh.AddVert(d, tint, Vector2.right);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
