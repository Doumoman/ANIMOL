using UnityEngine;

namespace Animol.TerrainStructure
{
    /// <summary>
    /// Places one unchanged source-art Sprite inside a logical cell footprint.
    /// UniformScale belongs to a child Renderer, never to the logical stamp root or Grid.
    /// Art bounds do not define collision; the stamp's explicit Rows remain authoritative.
    /// </summary>
    [CreateAssetMenu(fileName = "ANIMOL_TerrainArtFrame", menuName = "ANIMOL/Terrain Structure/Source Art Frame")]
    public sealed class AnimolTerrainArtFrameDefinition : ScriptableObject
    {
        public Sprite Sprite;
        public Vector3 VisualOffset;
        public float UniformScale = 1f;
        [Tooltip("Shared source-art material. The supplied source-art pipeline uses its Sweetie-16 palette shader.")]
        public Material Material;

        public bool ValidateForFootprint(int width, int height, out string error)
        {
            const float epsilon = 0.0001f;
            if (width <= 0 || height <= 0)
            {
                error = "Art frame requires a positive logical footprint.";
                return false;
            }
            if (Sprite == null || Sprite.rect.width <= 0f || Sprite.rect.height <= 0f ||
                Mathf.Abs(Sprite.pixelsPerUnit - 32f) > 0.001f || Sprite.pivot.sqrMagnitude > 0.000001f)
            {
                error = "Source art requires a Sprite with PPU 32 and bottom-left pivot (0,0).";
                return false;
            }
            if (Material == null || Material.shader == null)
            {
                error = "Source art requires its shared display material.";
                return false;
            }
            if (!IsFinite(UniformScale) || UniformScale <= 0f ||
                !IsFinite(VisualOffset.x) || !IsFinite(VisualOffset.y) || !IsFinite(VisualOffset.z))
            {
                error = "Art offset must be finite and uniform scale must be finite and positive.";
                return false;
            }
            if (Mathf.Abs(VisualOffset.x) > epsilon || Mathf.Abs(VisualOffset.z) > epsilon || VisualOffset.y < -epsilon)
            {
                error = "Source art is left aligned in the footprint; offset x/z must be zero and y nonnegative.";
                return false;
            }
            float visualWidth = Sprite.rect.width / Sprite.pixelsPerUnit * UniformScale;
            float visualHeight = Sprite.rect.height / Sprite.pixelsPerUnit * UniformScale;
            if (!IsFinite(visualWidth) || !IsFinite(visualHeight) ||
                visualWidth > width + epsilon || VisualOffset.y + visualHeight > height + epsilon)
            {
                error = "Scaled source-art bounds must remain inside the logical footprint.";
                return false;
            }
            error = null;
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
