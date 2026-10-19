using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animol.StarwayRanch.V12
{
    /// <summary>
    /// Orthographic, depth-driven parallax for independent background sprites.
    /// Authored world Z describes depth beyond the gameplay plane. Sorting Layer /
    /// Order remain explicit because SpriteRenderer sorting does not rely on Z alone.
    /// This component never changes the camera, gameplay transforms, or colliders.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class StarwayRanchV12BackgroundRig : MonoBehaviour
    {
        [Serializable]
        public sealed class Layer
        {
            public SpriteRenderer renderer;
            public int sortingOrder = -500;
            [Tooltip("Multiply depth-derived screen motion: stars about .03, clouds/islands 1. Zero locks this layer to the camera.")]
            [Min(0f)] public float parallaxMultiplier = 1f;
            [Tooltip("World-unit amplitude. From the authored starting point, excursion is at most twice this value.")]
            public Vector2 driftAmplitude = Vector2.zero;
            [Tooltip("One full smooth oscillation in seconds, independently for X and Y.")]
            public Vector2 driftPeriodSeconds = new Vector2(100f, 137f);
            public float phaseDegrees;
            [Range(0f, 0.15f)] public float twinkleAmount;
            [Min(1f)] public float twinklePeriodSeconds = 7f;
            [Tooltip("Enable for repeating stars/clouds; keep unique distant islands disabled.")]
            public bool wrapToView;
            [Tooltip("Extra invisible guard in world units. Use at least renderer extents + twice drift amplitude.")]
            public Vector2 guardWorld = Vector2.one;
        }

        [SerializeField] private Camera targetCamera;
        [SerializeField] private float gameplayPlaneZ;
        [SerializeField] private string backgroundSortingLayer = "Background";
        [SerializeField] private bool reducedMotion;
        [SerializeField] private Layer[] layers = new Layer[0];

        private sealed class State
        {
            public Layer layer;
            public Transform transform;
            public Vector2 anchor;
            public Color originalColor;
            public double phase;
            public double startWaveX;
            public double startWaveY;
        }

        private const double Tau = Math.PI * 2.0;
        private State[] states = new State[0];
        private Vector2 cameraOrigin;
        private double elapsed;
        private bool initialized;

        public bool ReducedMotion { get { return reducedMotion; } }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            RestoreColors();
            initialized = false;
        }

        /// <summary>Call after creating scene sprites, or assign the same fields in the Inspector.</summary>
        public void Configure(Camera sceneCamera, float worldGameplayPlaneZ,
            string sortingLayerName, Layer[] backgroundLayers)
        {
            RestoreColors();
            targetCamera = sceneCamera;
            gameplayPlaneZ = worldGameplayPlaneZ;
            backgroundSortingLayer = sortingLayerName;
            layers = backgroundLayers ?? new Layer[0];
            Initialize();
        }

        /// <summary>Freeze drift/twinkle and camera-lock the background; toggling does not jump.</summary>
        public void SetReducedMotion(bool value)
        {
            if (reducedMotion == value) return;
            reducedMotion = value;
            RestoreColors();
            Initialize();
        }

        /// <summary>
        /// Call after a scene teleport or an intentional repositioning of background objects.
        /// Existing scene positions become the new anchors without moving any sprite immediately.
        /// </summary>
        public void Rebase()
        {
            RestoreColors();
            Initialize();
        }

        private void Initialize()
        {
            initialized = false;
            elapsed = 0.0;
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null || !targetCamera.orthographic)
            {
                Debug.LogWarning("V12 background needs an existing orthographic camera; no camera was changed.", this);
                return;
            }
            if (Mathf.Abs(targetCamera.transform.forward.z) < 0.9999f ||
                Mathf.Abs(targetCamera.transform.up.y) < 0.9999f)
            {
                Debug.LogWarning("V12 background expects an axis-aligned XY camera without tilt or roll.", this);
                return;
            }

            cameraOrigin = targetCamera.transform.position;
            int sortingLayerId = 0;
            bool sortingLayerExists = false;
            SortingLayer[] availableSortingLayers = SortingLayer.layers;
            for (int i = 0; i < availableSortingLayers.Length; i++)
            {
                if (availableSortingLayers[i].name != backgroundSortingLayer) continue;
                sortingLayerId = availableSortingLayers[i].id;
                sortingLayerExists = true;
                break;
            }
            if (!sortingLayerExists)
                Debug.LogWarning("Create the Background Sorting Layer below all gameplay layers. Existing renderer layers were retained.", this);

            var captured = new List<State>(layers == null ? 0 : layers.Length);
            var seen = new HashSet<SpriteRenderer>();
            if (layers != null)
            {
                for (int i = 0; i < layers.Length; i++)
                {
                    Layer layer = layers[i];
                    if (layer == null || layer.renderer == null || !seen.Add(layer.renderer)) continue;
                    Transform spriteTransform = layer.renderer.transform;
                    var state = new State
                    {
                        layer = layer,
                        transform = spriteTransform,
                        anchor = spriteTransform.position,
                        originalColor = layer.renderer.color,
                        phase = layer.phaseDegrees * Math.PI / 180.0
                    };
                    state.startWaveX = Math.Sin(state.phase);
                    state.startWaveY = Math.Sin(state.phase + Math.PI * 0.5);
                    if (sortingLayerExists) layer.renderer.sortingLayerID = sortingLayerId;
                    layer.renderer.sortingOrder = Mathf.Clamp(layer.sortingOrder, -32768, 32767);
                    captured.Add(state);

                    float clipDistance = Vector3.Dot(spriteTransform.position - targetCamera.transform.position,
                        targetCamera.transform.forward);
                    if (clipDistance <= targetCamera.nearClipPlane || clipDistance >= targetCamera.farClipPlane)
                        Debug.LogWarning("V12 background sprite is outside the camera clipping planes: " + layer.renderer.name, layer.renderer);
                }
            }
            states = captured.ToArray();
            initialized = true;
        }

        private void LateUpdate()
        {
            if (!initialized || targetCamera == null) return;
            // Scaled time intentionally respects Time.timeScale = 0 pauses.
            if (!reducedMotion) elapsed += Time.deltaTime;
            Vector2 cameraDelta = (Vector2)targetCamera.transform.position - cameraOrigin;
            float cameraZ = targetCamera.transform.position.z;
            Vector2 cameraXY = targetCamera.transform.position;
            Vector2 halfView = new Vector2(targetCamera.orthographicSize * targetCamera.aspect,
                targetCamera.orthographicSize);

            for (int i = 0; i < states.Length; i++)
            {
                State state = states[i];
                if (state.transform == null || state.layer.renderer == null) continue;
                Vector3 position = state.transform.position;
                float factor = reducedMotion ? 0f : EffectiveParallaxFactor(cameraZ, gameplayPlaneZ,
                    position.z, state.layer.parallaxMultiplier);
                Vector2 drift = Vector2.zero;
                if (!reducedMotion)
                {
                    Layer layer = state.layer;
                    double waveX = Math.Sin(elapsed * Tau / Math.Max(1.0, layer.driftPeriodSeconds.x) + state.phase);
                    double waveY = Math.Sin(elapsed * Tau / Math.Max(1.0, layer.driftPeriodSeconds.y) + state.phase + Math.PI * 0.5);
                    drift.x = layer.driftAmplitude.x * (float)(waveX - state.startWaveX);
                    drift.y = layer.driftAmplitude.y * (float)(waveY - state.startWaveY);
                }
                Vector2 xy = state.anchor + cameraDelta * (1f - factor) + drift;
                position.x = xy.x;
                position.y = xy.y;
                // World Z remains the authored scene depth. No gameplay Transform is touched.
                state.transform.position = position;

                if (state.layer.wrapToView)
                {
                    // Include full sprite extents, not just its pivot. Each move occurs only
                    // after ALL of the sprite has passed the guard beyond the camera view.
                    Bounds bounds = state.layer.renderer.bounds;
                    Vector2 limits = halfView + new Vector2(Mathf.Max(0f, state.layer.guardWorld.x),
                        Mathf.Max(0f, state.layer.guardWorld.y)) + (Vector2)bounds.extents;
                    Vector2 relativeCentre = (Vector2)bounds.center - cameraXY;
                    Vector2 shift = new Vector2(WrapShift(relativeCentre.x, limits.x),
                        WrapShift(relativeCentre.y, limits.y));
                    state.anchor += shift;
                    position.x += shift.x;
                    position.y += shift.y;
                    state.transform.position = position;
                }

                Color color = state.originalColor;
                if (!reducedMotion && state.layer.twinkleAmount > 0f)
                {
                    float amount = Mathf.Clamp(state.layer.twinkleAmount, 0f, 0.15f);
                    double wave = Math.Sin(elapsed * Tau / Math.Max(1.0, state.layer.twinklePeriodSeconds) + state.phase);
                    color.a *= 1f - amount * 0.5f + amount * 0.5f * (float)wave;
                }
                state.layer.renderer.color = color;
            }
        }

        /// <summary>
        /// Example: camera Z=-10, gameplay Z=0, layer Z=40 => 10/(10+40)=0.2.
        /// A 1-unit camera move then displaces the layer only 0.2 units on screen.
        /// Works for an XY camera on either side of the gameplay plane.
        /// </summary>
        public static float DepthParallaxFactor(float cameraZ, float gameplayZ, float spriteZ)
        {
            float distance = Mathf.Max(0.001f, Mathf.Abs(gameplayZ - cameraZ));
            float direction = gameplayZ >= cameraZ ? 1f : -1f;
            float depth = Mathf.Max(0f, (spriteZ - gameplayZ) * direction);
            return distance / (distance + depth);
        }

        /// <summary>
        /// Authoring control layered over physical Z depth. Stars use a small multiplier
        /// to stay almost fixed; cloud/island multipliers of one retain full depth response.
        /// </summary>
        public static float EffectiveParallaxFactor(float cameraZ, float gameplayZ, float spriteZ,
            float multiplier)
        {
            return Mathf.Clamp01(DepthParallaxFactor(cameraZ, gameplayZ, spriteZ) * Mathf.Max(0f, multiplier));
        }

        /// <summary>
        /// Returns zero while any part may be visible in the guarded view. Otherwise
        /// recycles in a single modulo operation, including very long camera teleports.
        /// Small crossings enter at the opposite invisible edge. A large teleport can
        /// repopulate the new camera view immediately; the source was already offscreen.
        /// </summary>
        public static float WrapShift(float relativeCentre, float limit)
        {
            if (limit <= 0f || (relativeCentre >= -limit && relativeCentre <= limit)) return 0f;
            double span = 2.0 * limit;
            double wrapped = ((relativeCentre + (double)limit) % span + span) % span - limit;
            return (float)(wrapped - relativeCentre);
        }

        private void RestoreColors()
        {
            for (int i = 0; i < states.Length; i++)
            {
                State state = states[i];
                if (state.layer != null && state.layer.renderer != null)
                    state.layer.renderer.color = state.originalColor;
            }
        }
    }
}
