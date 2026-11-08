using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Animol.MapHudCrayonUI
{
    /// <summary>
    /// Adapts only the painted pixels of an existing Image. The source stays the Button's
    /// targetGraphic and keeps its state, hit rectangle, transforms and event connections.
    /// </summary>
    [DefaultExecutionOrder(10000), DisallowMultipleComponent]
    public sealed class CrayonImageBinding : MonoBehaviour
    {
        public Image sourceImage, decoration;
        public CrayonSpriteSet[] states = System.Array.Empty<CrayonSpriteSet>();
        public CrayonSkinScope scope;
        // Optional for an existing transparent hit-surface Image that has no state sprite.
        public CrayonSpriteSet constantState;
        public bool preserveSourceOpacity = true;
        public bool matchSourceType = true;
        public Image.Type decorationType = Image.Type.Simple;

        [SerializeField] private bool sourceCaptured;
        [SerializeField] private Color originalColor = Color.white;
        private readonly Dictionary<Sprite, CrayonSpriteSet> lookup = new Dictionary<Sprite, CrayonSpriteSet>();
        private CrayonSpriteSet current;
        private CrayonSpriteSet fallback;
        [SerializeField] private Color lastHiddenColor;
        [SerializeField] private bool sourceIsHidden;

        public void Configure(Image source, Image child, CrayonSpriteSet[] sets, CrayonSkinScope owner)
        {
            if (sourceImage != source)
            {
                RestoreOriginal(); sourceCaptured = false; sourceIsHidden = false;
            }
            sourceImage = source; decoration = child; states = sets ?? System.Array.Empty<CrayonSpriteSet>(); scope = owner;
            CaptureSource(); RebuildLookup(); RefreshNow();
        }

        private void Awake() { CaptureSource(); RebuildLookup(); }
        private void OnEnable() { CaptureSource(); RebuildLookup(); RefreshNow(); }
        private void LateUpdate() => RefreshNow();

        private void CaptureSource()
        {
            if (!sourceImage || sourceCaptured) return;
            originalColor = sourceImage.color; sourceCaptured = true; sourceIsHidden = false;
        }

        private void RebuildLookup()
        {
            lookup.Clear();
            fallback = constantState;
            foreach (CrayonSpriteSet set in states ?? System.Array.Empty<CrayonSpriteSet>())
            {
                if (set == null || !set.frameA) continue;
                if (set.source) lookup[set.source] = set;
                else if (fallback == null) fallback = set;
            }
            current = null;
        }

        public void RefreshNow()
        {
            if (!sourceImage || !decoration || decoration == sourceImage) return;
            if (!scope) { RestoreOriginal(); return; }
            if (Application.isPlaying && !scope.isActiveAndEnabled) { RestoreOriginal(); return; }
            if (!scope.CanDecorate(sourceImage)) { RestoreOriginal(); return; }
            CaptureSource();
            Sprite source = sourceImage.overrideSprite ? sourceImage.overrideSprite : sourceImage.sprite;
            CrayonSpriteSet next = null;
            if (source) lookup.TryGetValue(source, out next);
            if (next == null) next = fallback;
            if (next == null || !next.frameA)
            {
                RestoreOriginal(); return;
            }

            // Preserve authored opacity changes made by gameplay while this visual adapter is active.
            Color sourceColor = sourceImage.color;
            if (!sourceIsHidden || sourceColor != lastHiddenColor) originalColor = sourceColor;
            lastHiddenColor = originalColor; lastHiddenColor.a = 0;
            if (sourceImage.color != lastHiddenColor) sourceImage.color = lastHiddenColor;
            sourceIsHidden = true;

            bool still = scope.EffectiveReducedMotion;
            Material material = still ? next.stillMaterial : next.animatedMaterial;
            if (!material) material = next.stillMaterial;
            if (material && (!material.shader || !material.shader.isSupported)) material = null;
            if (current != next || decoration.sprite != next.frameA) decoration.sprite = next.frameA;
            if (decoration.material != material) decoration.material = material;
            current = next;

            decoration.raycastTarget = false;
            decoration.type = matchSourceType ? sourceImage.type : decorationType;
            decoration.preserveAspect = sourceImage.preserveAspect;
            decoration.fillCenter = sourceImage.fillCenter;
            decoration.fillMethod = sourceImage.fillMethod;
            decoration.fillOrigin = sourceImage.fillOrigin;
            decoration.fillClockwise = sourceImage.fillClockwise;
            decoration.fillAmount = sourceImage.fillAmount;
            decoration.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
            Color visible = Color.white; visible.a = preserveSourceOpacity ? originalColor.a : 1;
            decoration.color = visible;
            // Button ColorTint transitions use CanvasRenderer color independently of Image.color.
            decoration.canvasRenderer.SetColor(sourceImage.canvasRenderer.GetColor());
            decoration.enabled = sourceImage.enabled;
        }

        /// <summary>Used by disable, removal and the editor's reversible application flow.</summary>
        public void RestoreOriginal()
        {
            if (sourceImage && sourceCaptured && sourceIsHidden)
            {
                if (sourceImage.color != lastHiddenColor) originalColor = sourceImage.color;
                sourceImage.color = originalColor;
            }
            sourceIsHidden = false; current = null;
            if (decoration) decoration.enabled = false;
        }

        private void OnDisable() => RestoreOriginal();
        private void OnDestroy() => RestoreOriginal();
    }
}
