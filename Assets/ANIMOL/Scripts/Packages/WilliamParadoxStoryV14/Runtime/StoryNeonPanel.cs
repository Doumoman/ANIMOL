using UnityEngine;
using UnityEngine.UI;

namespace Animol.Starway
{
    /// <summary>
    /// Flat translucent fill, a thin pale rim and optional very small halo.
    /// Uses the default UGUI material, so CanvasGroup, Mask and RectMask2D work.
    /// No texture, material instance, shader pipeline or external package needed.
    /// </summary>
    [AddComponentMenu("ANIMOL/Story Neon Panel")]
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class StoryNeonPanel : MaskableGraphic
    {
        [SerializeField] private Color fillColor = new Color(13f / 255f, 21f / 255f, 43f / 255f, 0.73f);
        [SerializeField] private Color rimColor = new Color(236f / 255f, 238f / 255f, 231f / 255f, 0.94f);
        [SerializeField] private Color haloColor = new Color(183f / 255f, 213f / 255f, 238f / 255f, 1f);
        [SerializeField, Min(0f)] private float radius = 18f;
        [SerializeField, Min(0.25f)] private float rimWidth = 2f;
        [SerializeField, Range(2, 16)] private int cornerSegments = 8;
        [SerializeField] private bool haloEnabled = true;
        [SerializeField, Range(0f, 8f)] private float haloWidth = 4f;

        public bool HaloEnabled
        {
            get { return haloEnabled; }
            set { if (haloEnabled == value) return; haloEnabled = value; SetVerticesDirty(); }
        }

        protected override void Awake()
        {
            base.Awake();
            // This is a decorative graphic. A separate invisible Image handles
            // button hits in the generated prefab.
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = GetPixelAdjustedRect();
            if (bounds.width <= 0f || bounds.height <= 0f) return;

            float halfMin = Mathf.Min(bounds.width, bounds.height) * 0.5f;
            float glow = haloEnabled ? Mathf.Min(haloWidth, halfMin * 0.2f) : 0f;
            float stroke = Mathf.Min(Mathf.Max(0.25f, rimWidth), halfMin - glow);
            float outerRadius = Mathf.Clamp(radius + glow, 0f, halfMin);

            if (glow > 0f)
            {
                AddRing(mesh, bounds, outerRadius, glow * 0.5f, Tint(haloColor, 0.035f));
                AddRing(mesh, Inset(bounds, glow * 0.5f), Mathf.Max(0f, outerRadius - glow * 0.5f),
                    glow * 0.5f, Tint(haloColor, 0.10f));
            }

            Rect rimBounds = Inset(bounds, glow);
            float rimRadius = Mathf.Max(0f, outerRadius - glow);
            AddRing(mesh, rimBounds, rimRadius, stroke, Tint(rimColor, 1f));
            AddFill(mesh, Inset(rimBounds, stroke), Mathf.Max(0f, rimRadius - stroke), Tint(fillColor, 1f));
        }

        private Color32 Tint(Color source, float opacity)
        {
            // Graphic.color is a tint; CanvasGroup alpha is applied by UGUI.
            return new Color(source.r * color.r, source.g * color.g, source.b * color.b,
                source.a * color.a * opacity);
        }

        private void AddFill(VertexHelper mesh, Rect bounds, float curve, Color32 tint)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            int count = 4 * (Mathf.Clamp(cornerSegments, 2, 16) + 1);
            int start = mesh.currentVertCount;
            mesh.AddVert(bounds.center, tint, Vector2.zero);
            for (int i = 0; i < count; i++) mesh.AddVert(Point(bounds, curve, i), tint, Vector2.zero);
            for (int i = 0; i < count; i++)
                mesh.AddTriangle(start, start + 1 + i, start + 1 + ((i + 1) % count));
        }

        private void AddRing(VertexHelper mesh, Rect bounds, float curve, float thickness, Color32 tint)
        {
            if (bounds.width <= 0f || bounds.height <= 0f || thickness <= 0f) return;
            int count = 4 * (Mathf.Clamp(cornerSegments, 2, 16) + 1);
            int start = mesh.currentVertCount;
            Rect inner = Inset(bounds, thickness);
            for (int i = 0; i < count; i++)
            {
                mesh.AddVert(Point(bounds, curve, i), tint, Vector2.zero);
                mesh.AddVert(Point(inner, Mathf.Max(0f, curve - thickness), i), tint, Vector2.zero);
            }
            for (int i = 0; i < count; i++)
            {
                int a = start + i * 2;
                int b = start + ((i + 1) % count) * 2;
                mesh.AddTriangle(a, b, a + 1);
                mesh.AddTriangle(b, b + 1, a + 1);
            }
        }

        private Vector2 Point(Rect bounds, float curve, int index)
        {
            int segments = Mathf.Clamp(cornerSegments, 2, 16);
            int corner = index / (segments + 1);
            float angle = (corner * 90f + (index % (segments + 1)) * 90f / segments) * Mathf.Deg2Rad;
            curve = Mathf.Clamp(curve, 0f, Mathf.Min(bounds.width, bounds.height) * 0.5f);
            Vector2 center;
            switch (corner)
            {
                case 0: center = new Vector2(bounds.xMax - curve, bounds.yMax - curve); break;
                case 1: center = new Vector2(bounds.xMin + curve, bounds.yMax - curve); break;
                case 2: center = new Vector2(bounds.xMin + curve, bounds.yMin + curve); break;
                default: center = new Vector2(bounds.xMax - curve, bounds.yMin + curve); break;
            }
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * curve;
        }

        private static Rect Inset(Rect rect, float amount)
        {
            amount = Mathf.Clamp(amount, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            return new Rect(rect.x + amount, rect.y + amount,
                Mathf.Max(0f, rect.width - amount * 2f), Mathf.Max(0f, rect.height - amount * 2f));
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
