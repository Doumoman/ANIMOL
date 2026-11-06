using UnityEngine;
using UnityEngine.UI;

namespace Animol.Starway
{
    /// <summary>
    /// Isolated RawImage for the story effect. Drive it with the existing shot
    /// timeline; this component does not advance dialogue, puzzles or saves.
    /// </summary>
    [AddComponentMenu("ANIMOL/Starway Constellation Overlay")]
    public sealed class RanchConstellationOverlay : RawImage
    {
        private const string ShaderName = "ANIMOL/UI/StarwayConstellationParallax";

        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int DriftId = Shader.PropertyToID("_Drift");
        private static readonly int RefractionId = Shader.PropertyToID("_RefractionPx");
        private static readonly int OpenId = Shader.PropertyToID("_SealOpen");
        private static readonly int VisibilityId = Shader.PropertyToID("_Visibility");
        private static readonly int ReducedMotionId = Shader.PropertyToID("_ReducedMotion");
        private static readonly int LayerWeightsId = Shader.PropertyToID("_LayerWeights");

        [SerializeField] private Shader shaderAsset;
        [SerializeField] private Material materialTemplate;
        [SerializeField, Range(0f, 20f)] private float edgeRefractionAt1920 = 14f;
        [SerializeField] private Vector3 layerWeights = Vector3.one;

        private Material runtimeMaterial;
        private Material stencilVariant;
        private Material previousMaterial;
        private float progress;
        private float sealOpen;
        private float visibility;
        private bool reducedMotion;
        private Vector2 drift = new Vector2(0.05f, -0.08f);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Application.isPlaying)
                return;

            var template = UsesEffectShader(materialTemplate) ? materialTemplate : null;
            var assignedMaterial = material;
            if (template == null && UsesEffectShader(assignedMaterial))
                template = assignedMaterial;

            var selected = template != null ? template.shader : shaderAsset;
            if (selected == null || selected.name != ShaderName)
            {
                template = null;
                selected = Shader.Find(ShaderName);
            }
            if (selected == null)
            {
                Debug.LogWarning("Starway: assign StarwayConstellationParallax.shader on the overlay prefab.", this);
                enabled = false;
                return;
            }

            previousMaterial = material;
            runtimeMaterial = template != null ? new Material(template) : new Material(selected);
            runtimeMaterial.name = "Starway Constellation (Instance)";
            runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
            material = runtimeMaterial;
            raycastTarget = false;
            uvRect = new Rect(0f, 0f, 1f, 1f);
            Push();
        }

        protected override void OnDisable()
        {
            ClearCue();
            base.OnDisable();
            stencilVariant = null;
            if (runtimeMaterial == null)
                return;

            var owned = runtimeMaterial;
            runtimeMaterial = null;
            material = previousMaterial;
            previousMaterial = null;
            Destroy(owned);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Push();
        }

        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            // A stencil Mask may make a material variant. Keep its effect
            // uniforms synchronized without modifying a shared asset.
            var result = base.GetModifiedMaterial(baseMaterial);
            if (runtimeMaterial != null && baseMaterial == runtimeMaterial)
            {
                stencilVariant = result;
                PushTo(result);
            }
            return result;
        }

        // newSealOpen is the legacy argument name; v0.25 uses it as a
        // whole-field return blend, not an aperture or physical seal.
        public void SetCue(float newProgress, float newSealOpen, Vector2 viewDrift,
                           float newVisibility, bool useReducedMotion)
        {
            progress = Mathf.Clamp01(newProgress);
            sealOpen = Mathf.Clamp01(newSealOpen);
            drift = viewDrift;
            visibility = Mathf.Clamp01(newVisibility);
            reducedMotion = useReducedMotion;
            Push();
        }

        public void SetLayerWeights(Vector3 weights)
        {
            layerWeights = weights;
            Push();
        }

        public void ClearCue()
        {
            SetCue(0f, 0f, Vector2.zero, 0f, reducedMotion);
        }

        private void Push()
        {
            if (runtimeMaterial == null)
                return;

            PushTo(runtimeMaterial);
            if (stencilVariant != null && stencilVariant != runtimeMaterial)
                PushTo(stencilVariant);
        }

        private static bool UsesEffectShader(Material candidate)
        {
            return candidate != null && candidate.shader != null && candidate.shader.name == ShaderName;
        }

        private void PushTo(Material target)
        {
            if (target == null)
                return;

            var rect = rectTransform.rect;
            var aspect = rect.height > 0.001f ? Mathf.Max(0.01f, rect.width / rect.height) : 0.5625f;
            target.SetFloat(AspectId, aspect);
            target.SetFloat(ProgressId, progress);
            target.SetVector(DriftId, new Vector4(drift.x, drift.y, 0f, 0f));
            target.SetFloat(RefractionId, edgeRefractionAt1920);
            target.SetFloat(OpenId, sealOpen);
            target.SetFloat(VisibilityId, visibility);
            target.SetFloat(ReducedMotionId, reducedMotion ? 1f : 0f);
            target.SetVector(LayerWeightsId, new Vector4(layerWeights.x, layerWeights.y, layerWeights.z, 0f));
        }
    }
}
