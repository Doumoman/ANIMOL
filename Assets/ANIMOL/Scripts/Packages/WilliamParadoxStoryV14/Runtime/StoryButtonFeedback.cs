using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WilliamParadox.Story
{
    /// <summary>Scale feedback for a cosmetic child only. Selectable retains ownership of ColorTint and clicks.</summary>
    [DisallowMultipleComponent]
    public sealed class StoryButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler
    {
        public StoryRunner runner;
        [Tooltip("Cosmetic descendant of this button. Never assign the button/hitbox root.")]
        public RectTransform visual;
        public bool reducedMotion;

        private const float PressScale = 0.97f, PressDuration = 0.045f, ReleaseDuration = 0.09f;
        private enum Phase { Idle, Press, Held, Release }
        private Phase phase;
        private RectTransform boundVisual, rejectedVisual;
        private Selectable selectable;
        private Vector3 rest = Vector3.one, from, target;
        private float elapsed, duration;
        private bool releaseRequested, hasFocus = true, applicationPaused;
        private int? pointerId;
        private int lastPressFrame = int.MinValue;
        public bool IsAnimating { get { return phase == Phase.Press || phase == Phase.Release; } }

        public void Configure(StoryRunner storyRunner, RectTransform visualRoot)
        {
            ResetFeedback(); runner = storyRunner; visual = visualRoot; BindVisual();
        }
        private void Awake() { selectable = GetComponent<Selectable>(); BindVisual(); }
        private void OnEnable() { selectable = GetComponent<Selectable>(); BindVisual(); ResetFeedback(); }
        private bool BindVisual()
        {
            if (boundVisual == visual && boundVisual != null) return true;
            ResetFeedback(); boundVisual = null;
            if (visual == null) return false;
            if (visual == rejectedVisual) return false;
            if (visual == transform || !visual.IsChildOf(transform))
            {
                rejectedVisual = visual;
                Debug.LogWarning("StoryButtonFeedback requires a cosmetic child; the button/hitbox root is never animated.", this);
                return false;
            }
            rejectedVisual = null; boundVisual = visual; rest = visual.localScale; return true;
        }
        private bool CanUse()
        {
            return isActiveAndEnabled && hasFocus && !applicationPaused && (runner == null || !runner.IsPaused)
                && selectable != null && selectable.IsActive() && selectable.IsInteractable();
        }
        private bool MotionOff { get { return reducedMotion || (runner != null && runner.reducedMotion); } }
        private void Update()
        {
            if (boundVisual != visual) BindVisual();
            TickFeedback(Mathf.Max(0, Time.unscaledDeltaTime));
        }
        private void LateUpdate()
        {
            // Pause/disable can happen in EventSystem after this component's Update.
            if (!CanUse() || MotionOff) ResetFeedback();
        }
        private void TickFeedback(float delta)
        {
            if (!CanUse() || MotionOff || boundVisual == null) { ResetFeedback(); return; }
            if (phase != Phase.Press && phase != Phase.Release) return;
            elapsed += Mathf.Max(0, delta);
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            boundVisual.localScale = Vector3.LerpUnclamped(from, target, eased);
            if (t < 1) return;
            if (phase == Phase.Press)
            {
                if (releaseRequested) BeginRelease(); else phase = Phase.Held;
            }
            else { phase = Phase.Idle; boundVisual.localScale = rest; }
        }
        private void BeginPress(bool autoRelease)
        {
            if (!CanUse() || MotionOff || !BindVisual() || lastPressFrame == Time.frameCount) return;
            lastPressFrame = Time.frameCount; releaseRequested = autoRelease;
            from = boundVisual.localScale; target = rest * PressScale;
            elapsed = 0; duration = PressDuration; phase = Phase.Press;
        }
        private void BeginRelease()
        {
            if (boundVisual == null) return;
            from = boundVisual.localScale; target = rest;
            elapsed = 0; duration = ReleaseDuration; phase = Phase.Release;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left || pointerId.HasValue || !CanUse() || MotionOff) return;
            if (!BindVisual()) return;
            pointerId = eventData.pointerId; BeginPress(false);
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || !pointerId.HasValue || pointerId.Value != eventData.pointerId) return;
            pointerId = null;
            if (!CanUse() || MotionOff) { ResetFeedback(); return; }
            // Even a quick tap completes its 45 ms press before the 90 ms return.
            if (phase == Phase.Press) releaseRequested = true;
            else if (phase == Phase.Held) BeginRelease();
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData == null || !pointerId.HasValue || pointerId.Value != eventData.pointerId) return;
            pointerId = null;
            if (!CanUse() || MotionOff) ResetFeedback(); else BeginRelease();
        }
        public void OnSubmit(BaseEventData eventData)
        {
            if (!pointerId.HasValue) BeginPress(true);
        }
        public void ResetFeedback()
        {
            phase = Phase.Idle; elapsed = 0; releaseRequested = false; pointerId = null;
            if (boundVisual != null) boundVisual.localScale = rest;
        }
        private void OnDisable() { ResetFeedback(); }
        private void OnDestroy() { ResetFeedback(); }
        private void OnApplicationFocus(bool value) { hasFocus = value; if (!value) ResetFeedback(); }
        private void OnApplicationPause(bool value) { applicationPaused = value; if (value) ResetFeedback(); }
    }
}
