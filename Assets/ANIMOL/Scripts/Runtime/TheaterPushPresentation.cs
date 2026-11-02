using UnityEngine;

namespace Animol.Art.TheaterRetouch
{
    /// <summary>
    /// Changes only a dedicated graphics offset child. The caller owns PUSH state, costs,
    /// grid coordinates, object movement and Animator transitions.
    /// Suggested hierarchy: PlayerLogicRoot [this, colliders] / PushVisualOffset / AnimatedVisuals.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TheaterPushPresentation : MonoBehaviour
    {
        [Header("A collider-free graphics-only child")]
        [SerializeField] private Transform visualOffsetRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TheaterPushSpriteContacts contacts;
        [Tooltip("Optional fallback when the current sprite has no handPixel metadata.")]
        [SerializeField] private Transform handSocket;
        [Tooltip("Play the PUSH sprite frames without moving the character graphics toward the target.")]
        [SerializeField] private bool animationOnly = true;

        [Header("Distances as fractions of one logical tile")]
        [Min(0.001f)] [SerializeField] private float tileWorldSize = 1f;
        [Range(0f, 1f)] [SerializeField] private float maxApproachFraction = 0.22f;
        [Range(0.002f, 0.08f)] [SerializeField] private float palmGapFraction = 0.015f;
        [Range(0f, 0.05f)] [SerializeField] private float anticipationBackFraction = 0.012f;

        [Header("Presentation timings; do not add these to the logical turn cost")]
        [Min(0f)] [SerializeField] private float anticipationSeconds = 0.04f;
        [Min(0.001f)] [SerializeField] private float approachSeconds = 0.08f;
        [Min(0.001f)] [SerializeField] private float recoverSeconds = 0.10f;
        [Min(0.001f)] [SerializeField] private float contactFollowSeconds = 0.025f;

        private enum Phase { Rest, Anticipate, Approach, Contact, Recover }
        private Phase phase;
        private Collider2D target;
        private Transform targetVisualContactPoint;
        private Vector2 direction;
        private Vector3 restLocal;
        private Vector3 offsetLocal;
        private Vector3 phaseStartLocal;
        private float elapsed;
        private bool warnedMissingAnchor;
        private bool initialized;
        private bool hasCachedRestHand;
        private Vector3 cachedRestHandOwnerLocal;
        private float targetFaceOffsetWorld;
        private bool warnedReachLimit;

        public bool PoseActive { get { return phase != Phase.Rest && phase != Phase.Recover; } }
        public bool ContactEstablished { get { return phase == Phase.Contact; } }
        public bool IsSettled { get { return phase == Phase.Rest; } }
        public bool AnimationOnly { get { return animationOnly; } }

        public void SetApproachLimit(float fraction)
        {
            maxApproachFraction = Mathf.Clamp01(fraction);
        }

        private bool HasTheaterContactAsset
        {
            get
            {
                return contacts != null && (contacts.characterId == "seri" || contacts.characterId == "roan" ||
                    contacts.characterId == "mion" || contacts.characterId == "piko");
            }
        }

        private void Awake()
        {
            if (visualOffsetRoot != null) Initialize();
        }

        private void Start()
        {
            if (!initialized) Initialize();
        }

        /// <summary>For a runtime character factory; configure references before BeginPush.</summary>
        public bool Configure(Transform graphicsOffset, SpriteRenderer mainRenderer,
            TheaterPushSpriteContacts contactAsset, float actualTileWorldSize, Transform fallbackSocket = null,
            Vector3? restingLocalPosition = null)
        {
            Vector3 requestedRest = restingLocalPosition ?? (graphicsOffset != null ? graphicsOffset.localPosition : Vector3.zero);
            if (!restingLocalPosition.HasValue && initialized && graphicsOffset == visualOffsetRoot) requestedRest -= offsetLocal;
            ResetPresentation();
            initialized = false;
            visualOffsetRoot = graphicsOffset;
            spriteRenderer = mainRenderer;
            contacts = contactAsset;
            handSocket = fallbackSocket;
            tileWorldSize = actualTileWorldSize;
            bool valid = Initialize(requestedRest);
            if (valid) enabled = true;
            return valid;
        }

        private bool Initialize(Vector3? restOverride = null)
        {
            if (initialized) return true;
            if (visualOffsetRoot == null || visualOffsetRoot == transform ||
                !visualOffsetRoot.IsChildOf(transform) ||
                visualOffsetRoot.GetComponentInChildren<Collider2D>(true) != null ||
                visualOffsetRoot.GetComponentInChildren<Rigidbody2D>(true) != null ||
                visualOffsetRoot.GetComponentInChildren<Collider>(true) != null ||
                visualOffsetRoot.GetComponentInChildren<Rigidbody>(true) != null)
            {
                Debug.LogError("PUSH presentation requires a dedicated child containing graphics only, not the logical root or any physics component.", this);
                enabled = false;
                return false;
            }
            if (spriteRenderer == null)
            {
                SpriteRenderer[] renderers = visualOffsetRoot.GetComponentsInChildren<SpriteRenderer>(true);
                if (renderers.Length == 1) spriteRenderer = renderers[0];
            }
            if (spriteRenderer == null || (spriteRenderer.transform != visualOffsetRoot && !spriteRenderer.transform.IsChildOf(visualOffsetRoot)))
            {
                Debug.LogError("Assign the actual main character SpriteRenderer beneath the graphics-only offset root; multiple renderers must not be guessed.", this);
                enabled = false;
                return false;
            }
            if (!Finite(tileWorldSize) || tileWorldSize <= 0f || !Finite(maxApproachFraction) ||
                !Finite(palmGapFraction) || !Finite(anticipationBackFraction) || !Finite(anticipationSeconds) ||
                !Finite(approachSeconds) || !Finite(recoverSeconds) || !Finite(contactFollowSeconds))
            {
                Debug.LogError("PUSH presentation parameters must be finite and tileWorldSize must be positive.", this);
                enabled = false;
                return false;
            }
            maxApproachFraction = Mathf.Clamp01(maxApproachFraction);
            palmGapFraction = Mathf.Clamp(palmGapFraction, 0.002f, 0.08f);
            anticipationBackFraction = Mathf.Clamp(anticipationBackFraction, 0f, 0.05f);
            anticipationSeconds = Mathf.Max(0f, anticipationSeconds);
            approachSeconds = Mathf.Max(0.001f, approachSeconds);
            recoverSeconds = Mathf.Max(0.001f, recoverSeconds);
            contactFollowSeconds = Mathf.Max(0.001f, contactFollowSeconds);
            Vector3 nextRest = restOverride ?? visualOffsetRoot.localPosition;
            if (!Finite(nextRest.x) || !Finite(nextRest.y) || !Finite(nextRest.z))
            {
                Debug.LogError("The graphics resting position must be finite.", this);
                enabled = false;
                return false;
            }
            restLocal = nextRest;
            visualOffsetRoot.localPosition = restLocal;
            initialized = true;
            return true;
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        /// <summary>Call from the existing push interaction/state. Does not move the object or spend a turn.</summary>
        public void BeginPush(Collider2D pushedObject, Vector2 worldDirection, Transform visualContactPoint = null)
        {
            if (!initialized || !isActiveAndEnabled || !HasTheaterContactAsset ||
                (pushedObject == null && visualContactPoint == null) ||
                !Finite(worldDirection.x) || !Finite(worldDirection.y) || worldDirection.sqrMagnitude < 0.001f) return;
            if (pushedObject != null && (pushedObject.transform == transform ||
                pushedObject.transform.IsChildOf(transform)) ||
                visualContactPoint != null && (visualContactPoint == transform || visualContactPoint.IsChildOf(transform)))
            {
                Debug.LogError("PUSH target/contact point must belong to the pushed object, not this player hierarchy.", this);
                EndPush();
                return;
            }
            // The caller supplies Grid/Tilemap direction transformed into world space.
            Vector2 normalizedDirection = worldDirection.normalized;
            if (target == pushedObject && direction == normalizedDirection && targetVisualContactPoint == visualContactPoint && PoseActive) return;
            if (target == pushedObject && direction == normalizedDirection &&
                targetVisualContactPoint == visualContactPoint && phase == Phase.Recover)
            {
                ChangePhase(Phase.Contact);
                return;
            }
            if (animationOnly)
            {
                ResetPresentation();
                target = pushedObject;
                targetVisualContactPoint = visualContactPoint;
                direction = normalizedDirection;
                phase = Phase.Contact;
                return;
            }
            target = pushedObject;
            targetVisualContactPoint = visualContactPoint;
            direction = normalizedDirection;
            warnedMissingAnchor = false;
            hasCachedRestHand = false;
            warnedReachLimit = false;
            ChangePhase(Phase.Anticipate);
        }

        /// <summary>Call after the existing push animation/interaction completes, is cancelled, or changes state.</summary>
        public void EndPush()
        {
            if (animationOnly) { ResetPresentation(); return; }
            if (phase != Phase.Rest && phase != Phase.Recover) ChangePhase(Phase.Recover);
        }

        public void SetContacts(TheaterPushSpriteContacts value)
        {
            if (contacts == value) return;
            ResetPresentation();
            contacts = value;
        }

        public void ResetPresentation()
        {
            if (initialized && visualOffsetRoot != null && offsetLocal.sqrMagnitude > 0.000000000001f)
                visualOffsetRoot.localPosition = restLocal;
            offsetLocal = Vector3.zero;
            target = null;
            targetVisualContactPoint = null;
            hasCachedRestHand = false;
            phase = Phase.Rest;
        }

        private void ChangePhase(Phase next)
        {
            phaseStartLocal = offsetLocal;
            elapsed = 0f;
            phase = next;
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private Vector3 ToParentLocalVector(Vector3 worldVector)
        {
            return visualOffsetRoot.parent != null
                ? visualOffsetRoot.parent.InverseTransformVector(worldVector) : worldVector;
        }

        private Vector3 ToWorldVector(Vector3 parentLocalVector)
        {
            return visualOffsetRoot.parent != null
                ? visualOffsetRoot.parent.TransformVector(parentLocalVector) : parentLocalVector;
        }

        private bool TryGetRestHand(out Vector2 worldHand)
        {
            Vector3 world;
            TheaterPushSpriteContacts.Entry contact;
            if (spriteRenderer != null && contacts != null &&
                contacts.TryGet(spriteRenderer.sprite, out contact))
            {
                Vector2 handLocal = contact.handLocal;
                if (spriteRenderer.flipX) handLocal.x = -handLocal.x;
                if (spriteRenderer.flipY) handLocal.y = -handLocal.y;
                world = spriteRenderer.transform.TransformPoint(handLocal);
                targetFaceOffsetWorld = Mathf.Max(0f, contact.targetFaceOffsetWorld);
            }
            else if (handSocket != null && handSocket.IsChildOf(visualOffsetRoot))
            {
                world = handSocket.position;
                if (spriteRenderer != null)
                {
                    Vector3 local = spriteRenderer.transform.InverseTransformPoint(world);
                    if (spriteRenderer.flipX) local.x = -local.x;
                    if (spriteRenderer.flipY) local.y = -local.y;
                    world = spriteRenderer.transform.TransformPoint(local);
                }
                targetFaceOffsetWorld = 0f;
            }
            else if (phase == Phase.Recover && hasCachedRestHand)
            {
                worldHand = transform.TransformPoint(cachedRestHandOwnerLocal);
                return true;
            }
            else
            {
                worldHand = Vector2.zero;
                if (!warnedMissingAnchor)
                {
                    Debug.LogWarning("No PUSH hand anchor for this sprite. Assign the imported contact asset or a correctly positioned handSocket; approach stays at rest rather than guessing through the object.", this);
                    warnedMissingAnchor = true;
                }
                return false;
            }
            // Remove our own offset before solving, so repeated frames cannot accumulate drift.
            world -= ToWorldVector(offsetLocal);
            cachedRestHandOwnerLocal = transform.InverseTransformPoint(world);
            hasCachedRestHand = true;
            worldHand = world;
            return true;
        }

        private Vector3 DesiredContactOffset()
        {
            if (targetVisualContactPoint == null &&
                (target == null || !target.enabled || !target.gameObject.activeInHierarchy)) return Vector3.zero;
            if (targetVisualContactPoint != null && !targetVisualContactPoint.gameObject.activeInHierarchy)
                return Vector3.zero;
            Vector2 restHand;
            if (!TryGetRestHand(out restHand)) return Vector3.zero;
            Vector2 face = targetVisualContactPoint != null
                ? (Vector2)targetVisualContactPoint.position : target.ClosestPoint(restHand);
            // ClosestPoint returns the query itself if an over-reaching art anchor is already inside.
            // Use the conservative entry face of the target bounds so the visual can retreat safely.
            if (targetVisualContactPoint == null && (face - restHand).sqrMagnitude < 0.000001f)
            {
                Bounds bounds = target.bounds;
                Vector2 center = new Vector2(bounds.center.x, bounds.center.y);
                float nearPlane = Vector2.Dot(center, direction) -
                    Mathf.Abs(direction.x) * bounds.extents.x - Mathf.Abs(direction.y) * bounds.extents.y;
                face = restHand + direction * (nearPlane - Vector2.Dot(restHand, direction));
            }
            float gap = Vector2.Dot(face - restHand, direction) - tileWorldSize * palmGapFraction - targetFaceOffsetWorld;
            if (phase == Phase.Contact && !warnedReachLimit && Mathf.Abs(gap) > tileWorldSize * maxApproachFraction + tileWorldSize * 0.005f)
            {
                Debug.LogWarning("PUSH visual contact cannot reach within the configured cap. Requested " + gap.ToString("F3") +
                    " world units; cap " + (tileWorldSize * maxApproachFraction).ToString("F3") +
                    ". Check the actual artwork contact plane, handPixel/pivot and direction before adjusting the graphics-only range. Do not move the logical root or collider.", this);
                warnedReachLimit = true;
            }
            float distance = Mathf.Clamp(gap, -tileWorldSize * maxApproachFraction, tileWorldSize * maxApproachFraction);
            if (distance > 0f && targetVisualContactPoint != null)
                distance = Mathf.Min(distance, SafeVisualApproach(targetVisualContactPoint.position));
            return ToParentLocalVector(new Vector3(direction.x * distance, direction.y * distance, 0f));
        }

        private float SafeVisualApproach(Vector3 contactPoint)
        {
            if (!spriteRenderer || !spriteRenderer.sprite) return 0f;
            var sprite = spriteRenderer.sprite;
            var vertices = new System.Collections.Generic.List<Vector2>();
            var leading = float.NegativeInfinity;
            for (var i = 0; i < sprite.GetPhysicsShapeCount(); i++)
            {
                vertices.Clear();
                sprite.GetPhysicsShape(i, vertices);
                foreach (var vertex in vertices)
                {
                    var local = new Vector3(spriteRenderer.flipX ? -vertex.x : vertex.x,
                        spriteRenderer.flipY ? -vertex.y : vertex.y, 0f);
                    var world = spriteRenderer.transform.TransformPoint(local) - ToWorldVector(offsetLocal);
                    leading = Mathf.Max(leading, Vector2.Dot(world, direction));
                }
            }
            if (float.IsNegativeInfinity(leading))
            {
                var bounds = spriteRenderer.bounds;
                var center = (Vector2)(bounds.center - ToWorldVector(offsetLocal));
                leading = Vector2.Dot(center, direction) +
                    Mathf.Abs(direction.x) * bounds.extents.x + Mathf.Abs(direction.y) * bounds.extents.y;
            }
            var surface = Vector2.Dot(contactPoint, direction);
            return Mathf.Max(0f, surface + tileWorldSize * .025f - leading);
        }

        private void LateUpdate()
        {
            if (!initialized || visualOffsetRoot == null) return;
            if (animationOnly)
            {
                if (offsetLocal.sqrMagnitude > 0.000000000001f) ResetPresentation();
                return;
            }
            if (!HasTheaterContactAsset) { ResetPresentation(); return; }
            if (PoseActive && (targetVisualContactPoint != null
                    ? !targetVisualContactPoint.gameObject.activeInHierarchy
                    : target == null || !target.enabled || !target.gameObject.activeInHierarchy)) EndPush();
            elapsed += Time.deltaTime;
            Vector3 desired = DesiredContactOffset();
            if (phase == Phase.Anticipate)
            {
                Vector3 back = ToParentLocalVector(new Vector3(-direction.x, -direction.y, 0f) * tileWorldSize * anticipationBackFraction);
                offsetLocal = Vector3.LerpUnclamped(phaseStartLocal, back, Ease(anticipationSeconds <= 0f ? 1f : elapsed / anticipationSeconds));
                if (elapsed >= anticipationSeconds) ChangePhase(Phase.Approach);
            }
            else if (phase == Phase.Approach)
            {
                offsetLocal = Vector3.LerpUnclamped(phaseStartLocal, desired, Ease(elapsed / approachSeconds));
                if (elapsed >= approachSeconds) ChangePhase(Phase.Contact);
            }
            else if (phase == Phase.Contact)
                offsetLocal = Vector3.Lerp(offsetLocal, desired, 1f - Mathf.Exp(-Time.deltaTime / contactFollowSeconds));
            else if (phase == Phase.Recover)
            {
                offsetLocal = Vector3.LerpUnclamped(phaseStartLocal, Vector3.zero, Ease(elapsed / recoverSeconds));
                if (elapsed >= recoverSeconds) { offsetLocal = Vector3.zero; phase = Phase.Rest; target = null; targetVisualContactPoint = null; }
            }
            else offsetLocal = Vector3.zero;

            // Do not advance beyond the currently capped target while recovering/following.
            // A reach-limit warning means that the artwork/target geometry still requires scene QA.
            if ((target != null || targetVisualContactPoint != null) && phase != Phase.Rest)
            {
                Vector3 currentWorld = ToWorldVector(offsetLocal);
                float advance = Vector2.Dot(new Vector2(currentWorld.x, currentWorld.y), direction);
                Vector3 safeWorld = ToWorldVector(desired);
                float safeAdvance = Vector2.Dot(new Vector2(safeWorld.x, safeWorld.y), direction);
                if (advance > safeAdvance)
                    offsetLocal -= ToParentLocalVector(new Vector3(direction.x, direction.y, 0f) * (advance - safeAdvance));
            }
            visualOffsetRoot.localPosition = restLocal + offsetLocal;
        }

        private void OnDisable()
        {
            ResetPresentation();
        }
    }
}
