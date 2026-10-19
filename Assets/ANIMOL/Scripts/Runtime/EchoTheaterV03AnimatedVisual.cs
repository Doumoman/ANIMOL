using System;
using UnityEngine;

namespace ANIMOL.Art.EchoTheaterV03
{
    [DisallowMultipleComponent]
    public sealed class EchoTheaterV03AnimatedVisual : MonoBehaviour
    {
        [SerializeField] private EchoTheaterV03VisualCatalog catalog;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private string expectedManifestSha256;
        [SerializeField] private Sprite diagnosticSprite;
        private EchoTheaterV03VisualCatalog.Clip clip;
        private bool paused, playing, reverse, manual;
        private double elapsed;
        private float durationSeconds = -1f;
        private string lastError;
        private Color normalColor;
        private bool diagnosticShown;
        private bool configurationChecked;
        public string CurrentKey { get { return clip == null ? null : clip.Key; } }
        public bool IsPlaying { get { return playing && !paused && !manual; } }
        public bool IsPaused { get { return paused; } }
        public string LastDiagnostic { get { return lastError; } }

        private void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            // Deliberately no automatic idle, raw-input listener, event callback or legacy sprite fallback.
        }
        private void Start() { if (!configurationChecked) ValidateConfiguration(); }

        public bool Configure(EchoTheaterV03VisualCatalog latest, SpriteRenderer visualRenderer, string pinnedManifestSha256)
        {
            if (diagnosticShown && target != null) target.color = normalColor;
            diagnosticShown = false;
            catalog = latest; target = visualRenderer; expectedManifestSha256 = pinnedManifestSha256;
            clip = null; playing = false; manual = false; elapsed = 0;
            return ValidateConfiguration();
        }

        public bool SetClip(string key, bool restart = true, bool playReverse = false)
        { return Play(key, -1f, restart, playReverse); }

        // Only the project's already-committed simulation event should call this action helper.
        // Invalid/blocked input leaves the current clip, phase and renderer completely unchanged.
        public bool PlayForCommittedAction(string key, bool actionCommitted, float actionDurationSeconds = -1f,
            bool restart = true, bool playReverse = false)
        {
            if (!actionCommitted) return false;
            return Play(key, actionDurationSeconds, restart, playReverse);
        }

        // durationSeconds>0 follows an authoritative step/tween duration; -1 uses frames/fps.
        public bool Play(string key, float authoritativeDurationSeconds = -1f, bool restart = true, bool playReverse = false)
        {
            if (paused) return false;
            if (!EchoTheaterV03VisualCatalog.Finite(authoritativeDurationSeconds)
                || (authoritativeDurationSeconds != -1f && authoritativeDurationSeconds <= 0))
                return Fail("Invalid visual duration for " + key);
            EchoTheaterV03VisualCatalog.Clip next;
            if (!Resolve(key, out next)) return false;
            if (!restart && clip == next && reverse == playReverse) return true;
            clip = next; reverse = playReverse; durationSeconds = authoritativeDurationSeconds;
            elapsed = 0; manual = false; playing = true;
            SetFrame(EchoTheaterV03ClipTiming.FrameIndex(clip.FrameCount, clip.Fps, 0, clip.Loop, reverse, durationSeconds));
            return true;
        }

        // Use this instead of autonomous time when the simulation already supplies tween progress.
        // It does not fire an event, commit an action, move a transform or resume automatic playback.
        public bool ApplyNormalizedProgress(float normalizedProgress)
        {
            if (paused) return false;
            if (!EchoTheaterV03VisualCatalog.Finite(normalizedProgress)) return Fail("Non-finite visual progress.");
            if (clip == null) return Fail("ApplyNormalizedProgress needs an explicitly selected latest clip.");
            manual = true; playing = false;
            SetFrame(EchoTheaterV03ClipTiming.FromNormalized(clip.FrameCount, normalizedProgress, reverse));
            return true;
        }

        public bool ApplyNormalizedProgress(string key, float normalizedProgress, bool playReverse = false)
        {
            if (paused) return false;
            if (!EchoTheaterV03VisualCatalog.Finite(normalizedProgress)) return Fail("Non-finite visual progress.");
            EchoTheaterV03VisualCatalog.Clip next;
            if (!Resolve(key, out next)) return false;
            clip = next; reverse = playReverse; elapsed = 0; manual = true; playing = false;
            SetFrame(EchoTheaterV03ClipTiming.FromNormalized(clip.FrameCount, normalizedProgress, reverse));
            return true;
        }

        // Authoritative state changes/reset/undo cancel pending visual playback and snap immediately.
        // Snapping is deliberately allowed while paused; it is a state rebind, not elapsed animation.
        public bool SnapToState(string key, float normalizedProgress = 0f, bool playReverse = false)
        {
            if (!EchoTheaterV03VisualCatalog.Finite(normalizedProgress)) return Fail("Non-finite snap progress.");
            EchoTheaterV03VisualCatalog.Clip next;
            if (!Resolve(key, out next)) return false;
            clip = next; reverse = playReverse; elapsed = 0; manual = true; playing = false;
            SetFrame(EchoTheaterV03ClipTiming.FromNormalized(clip.FrameCount, normalizedProgress, reverse));
            return true;
        }

        public void Cancel() { playing = false; manual = true; elapsed = 0; }
        public void SetPaused(bool value) { paused = value; }

        private void Update()
        {
            if (paused || !playing || manual || clip == null) return;
            float dt = Time.deltaTime; // scaled time only; timeScale=0 freezes playback.
            if (dt <= 0) return;
            elapsed += dt;
            SetFrame(EchoTheaterV03ClipTiming.FrameIndex(clip.FrameCount, clip.Fps, elapsed, clip.Loop, reverse, durationSeconds));
            if (clip == null) return;
            double duration = durationSeconds > 0 ? durationSeconds : clip.FrameCount / (double)clip.Fps;
            if (!clip.Loop && elapsed >= duration) playing = false; // hold last, no gameplay callback.
        }

        private bool ValidateConfiguration()
        {
            configurationChecked = true;
            if (target == null) return Fail("A visual SpriteRenderer is required.");
            if (catalog == null) return Fail("Latest V03 catalog is missing.");
            string error;
            if (!catalog.ValidateIntegrity(expectedManifestSha256, out error)) return Fail(error);
            lastError = null;
            return true;
        }

        private bool Resolve(string key, out EchoTheaterV03VisualCatalog.Clip value)
        {
            value = null;
            if (target == null || catalog == null) return Fail("Latest catalog or visual SpriteRenderer is missing.");
            string error;
            if (!catalog.TryGetClip(key, expectedManifestSha256, out value, out error)) return Fail(error);
            return true;
        }

        private void SetFrame(int index)
        {
            if (target == null || clip == null) return;
            Sprite sprite = clip.GetFrame(index).Sprite;
            if (sprite == null) { Fail("Latest frame unloaded/missing: " + clip.Key + " #" + index); return; }
            if (diagnosticShown) { target.color = normalColor; diagnosticShown = false; }
            target.sprite = sprite;
            lastError = null;
        }

        private bool Fail(string message)
        {
            clip = null; playing = false; manual = true; elapsed = 0;
            if (target != null)
            {
                if (!diagnosticShown) normalColor = target.color;
                diagnosticShown = true; target.color = Color.white;
                target.sprite = diagnosticSprite != null ? diagnosticSprite : EchoTheaterV03VisualDiagnostic.Sprite;
            }
            if (lastError != message) Debug.LogError("[Echo Theater V03] " + message, this);
            lastError = message;
            return false;
        }
    }

    internal static class EchoTheaterV03VisualDiagnostic
    {
        private static Sprite sprite;
        public static Sprite Sprite
        {
            get
            {
                if (sprite != null) return sprite;
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                texture.name = "V03_MISSING_LATEST_ART"; texture.hideFlags = HideFlags.HideAndDontSave;
                texture.filterMode = FilterMode.Point;
                var pixels = new Color32[256];
                for (int y = 0; y < 16; ++y) for (int x = 0; x < 16; ++x)
                    pixels[y * 16 + x] = ((x / 4 + y / 4) & 1) == 0
                        ? new Color32(255, 0, 255, 255) : new Color32(0, 0, 0, 255);
                texture.SetPixels32(pixels); texture.Apply(false, true);
                sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16f);
                sprite.name = "V03_MISSING_LATEST_ART"; sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }
        }
    }
}
