using TMPro;
using UnityEngine;

namespace ANIMOL.Typography
{
    public sealed class PixelTypographyProfile : ScriptableObject
    {
        public TMP_FontAsset Font;
        public bool CommonUiApproved;
        public string[] PilotScenes = { "MoonGraphicsQA", "Results" };
        public void EnsurePointSampling()
        {
            SetSampling(Font);
            foreach (var fallback in Font.fallbackFontAssetTable) SetSampling(fallback);
        }
        private static void SetSampling(TMP_FontAsset font)
        {
            if (font == null) return;
            foreach (var texture in font.atlasTextures)
                if (texture != null) { texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp; texture.anisoLevel = 0; }
        }
    }
}
