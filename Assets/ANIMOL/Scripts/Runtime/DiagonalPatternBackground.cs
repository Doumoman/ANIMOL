using UnityEngine;
using UnityEngine.UI;

namespace DiagonalPattern
{
    /// <summary>One uGUI quad with a repeating, diagonally scrolling texture.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("UI/Diagonal Pattern Background")]
    public sealed class DiagonalPatternBackground : MonoBehaviour
    {
        [Header("Standalone pattern PNG (not a Sprite Atlas)")]
        [SerializeField] private Shader scrollingShader;
        [SerializeField] private Texture2D patternTexture;
        [SerializeField] private Vector2 tileSize = new Vector2(256f, 256f);
        [SerializeField] private Color backgroundColor = new Color(0.97f, 0.97f, 0.99f, 1f);
        [SerializeField] private Color patternTint = Color.white;
        [SerializeField, Range(0f, 1f)] private float patternOpacity = 0.20f;

        [Header("Visible motion in Canvas units / second")]
        [Tooltip("+X = right, +Y = up. For down-left use negative X and Y.")]
        [SerializeField] private Vector2 visualVelocity = new Vector2(12f, 12f);
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool isScrolling = true;
        [Tooltip("Starting phase in texture repeats, not pixels.")]
        [SerializeField] private Vector2 initialPhase;

        private static readonly int ScaleId = Shader.PropertyToID("_PatternScale");
        private static readonly int OffsetId = Shader.PropertyToID("_PatternOffset");
        private static readonly int BackgroundId = Shader.PropertyToID("_BackgroundColor");
        private static readonly int TintId = Shader.PropertyToID("_PatternTint");
        private static readonly int OpacityId = Shader.PropertyToID("_PatternOpacity");

        private RawImage graphic;
        private Material instanceMaterial;
        private Material originalMaterial;
        private Texture originalTexture;
        private bool originalRaycastTarget;
        private Texture2D sourceTexture;
        private Texture2D repeatCopy;
        private Texture boundTexture;
        private double phaseX;
        private double phaseY;
        private bool phaseInitialized;

        public Vector2 VisualVelocity { get => visualVelocity; set => visualVelocity = value; }
        public bool IsScrolling { get => isScrolling; set => isScrolling = value; }
        public Vector2 CurrentPhase => new Vector2((float)phaseX, (float)phaseY);

        private void OnEnable()
        {
            // An inactive campaign panel may be opened repeatedly. Keep the phase
            // across those visibility changes so the pattern resumes in place.
            if (!phaseInitialized) ResetPhase();
            Refresh();
        }

        private void LateUpdate()
        {
            if (Application.IsPlaying(gameObject) && isScrolling)
            {
                double dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                AdvancePattern(dt);
            }
            Refresh();
        }

        public void AdvancePattern(double seconds)
        {
            if (!isScrolling || seconds <= 0) return;
            // Bounded doubles avoid long-running _Time precision loss.
            phaseX = Wrap01(phaseX + seconds * visualVelocity.x / Mathf.Max(1f, tileSize.x));
            phaseY = Wrap01(phaseY + seconds * visualVelocity.y / Mathf.Max(1f, tileSize.y));
        }

        private void OnValidate()
        {
            tileSize.x = Mathf.Max(1f, tileSize.x);
            tileSize.y = Mathf.Max(1f, tileSize.y);
            patternOpacity = Mathf.Clamp01(patternOpacity);
            // Resource creation is deferred to LateUpdate, on the main thread.
        }

        private void OnDisable() => ReleaseResources();
        private void OnDestroy() => ReleaseResources();

        public void Configure(Shader shaderAsset, Texture2D textureAsset)
        {
            scrollingShader = shaderAsset; // Serialized reference keeps shader in builds.
            patternTexture = textureAsset;
            ResetPhase();
            if (isActiveAndEnabled) Refresh();
        }

        public void SetAppearance(Texture2D textureAsset, Color background, Color tint, float opacity)
        {
            patternTexture = textureAsset;
            backgroundColor = background;
            patternTint = tint;
            patternOpacity = Mathf.Clamp01(opacity);
            if (isActiveAndEnabled) Refresh();
        }

        [ContextMenu("Reset Pattern Phase")]
        public void ResetPhase()
        {
            phaseX = Wrap01(initialPhase.x);
            phaseY = Wrap01(initialPhase.y);
            phaseInitialized = true;
        }

        private static double Wrap01(double value) => value - System.Math.Floor(value);

        private void Refresh()
        {
            if (!graphic) graphic = GetComponent<RawImage>();
            if (!scrollingShader) return;

            if (instanceMaterial && instanceMaterial.shader != scrollingShader)
                ReleaseResources();

            if (!instanceMaterial)
            {
                originalMaterial = graphic.material;
                originalTexture = graphic.texture;
                originalRaycastTarget = graphic.raycastTarget;
                instanceMaterial = new Material(scrollingShader)
                {
                    name = "Diagonal Pattern (Instance)",
                    hideFlags = HideFlags.HideAndDontSave
                };
                graphic.material = instanceMaterial;
            }

            if (sourceTexture != patternTexture || !boundTexture)
            {
                DestroyOwned(repeatCopy);
                repeatCopy = null;
                sourceTexture = patternTexture;
                boundTexture = patternTexture ? patternTexture : Texture2D.whiteTexture;
                if (patternTexture && patternTexture.wrapMode != TextureWrapMode.Repeat)
                {
                    // Do not change a shared source texture's wrap mode.
                    // Importing a dedicated PNG as Repeat avoids this private copy.
                    repeatCopy = Instantiate(patternTexture);
                    repeatCopy.name = patternTexture.name + " (Repeat Instance)";
                    repeatCopy.hideFlags = HideFlags.HideAndDontSave;
                    repeatCopy.wrapMode = TextureWrapMode.Repeat;
                    boundTexture = repeatCopy;
                }
            }

            graphic.texture = boundTexture;
            graphic.raycastTarget = false;
            ApplyProperties(instanceMaterial);

            // A parent Mask may use a separate cached stencil material.
            Material rendered = graphic.materialForRendering;
            if (rendered && rendered != instanceMaterial) ApplyProperties(rendered);
        }

        private void ApplyProperties(Material target)
        {
            Rect rect = graphic.rectTransform.rect;
            target.SetVector(ScaleId, new Vector4(
                rect.width / Mathf.Max(1f, tileSize.x),
                rect.height / Mathf.Max(1f, tileSize.y), 0f, 0f));
            target.SetVector(OffsetId, new Vector4((float)phaseX, (float)phaseY, 0f, 0f));
            target.SetColor(BackgroundId, backgroundColor);
            target.SetColor(TintId, patternTint);
            target.SetFloat(OpacityId, patternTexture ? patternOpacity : 0f);
        }

        private void ReleaseResources()
        {
            if (graphic && instanceMaterial && graphic.material == instanceMaterial)
            {
                graphic.material = originalMaterial;
                if (graphic.texture == boundTexture) graphic.texture = originalTexture;
                graphic.raycastTarget = originalRaycastTarget;
            }
            DestroyOwned(instanceMaterial);
            DestroyOwned(repeatCopy);
            instanceMaterial = null;
            repeatCopy = null;
            sourceTexture = null;
            boundTexture = null;
        }

        private static void DestroyOwned(Object value)
        {
            if (!value) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
