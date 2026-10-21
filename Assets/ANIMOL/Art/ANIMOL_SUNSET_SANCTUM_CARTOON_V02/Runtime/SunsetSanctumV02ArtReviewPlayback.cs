using System;
using UnityEngine;

namespace ANIMOL.Art.SunsetSanctumV02
{
    /// <summary>
    /// Explicitly opt-in ART REVIEW playback, using strict latest-catalog frames at
    /// their declared native fps. No raw input, movement, turns, collision or events.
    /// Production gameplay should use its own authoritative simulation visual adapter.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SunsetSanctumV02ArtReviewPlayback : MonoBehaviour
    {
        [SerializeField] private bool artReviewOnly;
        [SerializeField] private SunsetSanctumV02VisualCatalog catalog;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private string expectedManifestSha256;
        [SerializeField] private string[] clipKeys = new string[0];
        [SerializeField, Min(0.05f)] private float secondsPerClip = 1f;

        private SunsetSanctumV02VisualCatalog.Clip[] resolved;
        private double[] reviewDurations;
        private double totalReviewDuration;
        private double playlistElapsed;
        private bool paused;
        private bool ready;
        private string lastError;

        public bool IsArtReviewOnly { get { return artReviewOnly; } }

        public void ConfigureForArtReview(SunsetSanctumV02VisualCatalog latest,
            SpriteRenderer renderer, string pinnedCatalogSha256, string[] exactClipKeys,
            float minimumSecondsPerClip, bool explicitlyEnableArtReview)
        {
            if (!explicitlyEnableArtReview)
                throw new ArgumentException("Art-review playback requires an explicit review-only opt-in.");
            if (!Finite(minimumSecondsPerClip) || minimumSecondsPerClip <= 0f)
                throw new ArgumentOutOfRangeException("minimumSecondsPerClip");
            catalog = latest;
            target = renderer;
            expectedManifestSha256 = pinnedCatalogSha256;
            clipKeys = exactClipKeys == null ? new string[0] : (string[])exactClipKeys.Clone();
            secondsPerClip = minimumSecondsPerClip;
            artReviewOnly = true;
            playlistElapsed = 0.0;
            paused = false;
            ready = false;
            if (!ResolveLatest()) throw new InvalidOperationException(lastError);
            RenderAt(0.0);
        }

        private void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (!HasExplicitReviewRoot()) return;
            if (ResolveLatest()) RenderAt(0.0);
        }

        public void SetReviewPaused(bool value) { paused = value; }

        public bool ResetReview()
        {
            playlistElapsed = 0.0;
            if (!HasExplicitReviewRoot() || !ResolveLatest()) return false;
            RenderAt(0.0);
            return true;
        }

        private bool HasExplicitReviewRoot()
        {
            if (!artReviewOnly) return false;
            var marker = GetComponentInParent<SunsetSanctumV02ArtReviewMarker>();
            return marker != null && marker.IsArtReviewOnly
                && string.Equals(marker.PinnedCatalogSha256, expectedManifestSha256,
                    StringComparison.OrdinalIgnoreCase);
        }

        private bool ResolveLatest()
        {
            ready = false;
            if (!HasExplicitReviewRoot()) return Fail("An explicit ART_REVIEW_ONLY root and matching catalog pin are required.");
            if (catalog == null || target == null) return Fail("Art-review catalog or renderer is missing.");
            string error;
            if (!catalog.ValidateIntegrity(expectedManifestSha256, out error)) return Fail(error);
            if (clipKeys == null || clipKeys.Length == 0 || !Finite(secondsPerClip) || secondsPerClip <= 0f)
                return Fail("Art-review playlist or duration is invalid.");

            var next = new SunsetSanctumV02VisualCatalog.Clip[clipKeys.Length];
            var durations = new double[clipKeys.Length];
            double total = 0.0;
            for (int i = 0; i < clipKeys.Length; ++i)
            {
                if (!catalog.TryGetClip(clipKeys[i], expectedManifestSha256, out next[i], out error))
                    return Fail(error);
                var clip = next[i];
                if (clip.FrameCount < 1 || !Finite(clip.Fps) || clip.Fps <= 0f)
                    return Fail("Invalid latest review clip: " + clipKeys[i]);
                // Never speed up/slow down the source frames to fill a review slot.
                // Non-looping motion completes at its real fps, then holds its endpoint.
                durations[i] = Math.Max(secondsPerClip, clip.FrameCount / (double)clip.Fps);
                total += durations[i];
            }
            if (double.IsNaN(total) || double.IsInfinity(total) || total <= 0.0)
                return Fail("Art-review playlist duration is invalid.");
            resolved = next;
            reviewDurations = durations;
            totalReviewDuration = total;
            lastError = null;
            ready = true;
            return true;
        }

        private void Update()
        {
            if (!Application.isPlaying || paused || !ready || !HasExplicitReviewRoot()) return;
            float dt = Time.deltaTime; // Pause/timeScale=0 freezes review playback as well.
            if (!(dt > 0f) || !Finite(dt)) return;
            playlistElapsed = (playlistElapsed + dt) % totalReviewDuration;
            RenderAt(playlistElapsed);
        }

        private void RenderAt(double playlistTime)
        {
            if (!ready || target == null) return;
            double local = Math.Max(0.0, playlistTime);
            int selected = 0;
            while (selected < reviewDurations.Length - 1 && local >= reviewDurations[selected])
            {
                local -= reviewDurations[selected];
                selected++;
            }
            var clip = resolved[selected];
            int frame = SunsetSanctumV02ClipTiming.FrameIndex(clip.FrameCount,
                clip.Fps, local, clip.Loop, false);
            Sprite sprite = clip.GetFrame(frame).Sprite;
            if (sprite == null) { Fail("Latest review frame is missing: " + clip.Key); return; }
            target.sprite = sprite;
        }

        private bool Fail(string message)
        {
            ready = false;
            resolved = null;
            if (target != null) target.sprite = null; // Explicit failure; no stale/legacy art.
            if (lastError != message)
                Debug.LogError("[Sunset Sanctum V02 ART REVIEW ONLY] " + message, this);
            lastError = message;
            return false;
        }

        private static bool Finite(float value)
        { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
