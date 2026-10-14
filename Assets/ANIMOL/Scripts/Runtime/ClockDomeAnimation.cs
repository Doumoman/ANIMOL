using UnityEngine;
using ANIMOL.Presentation;

namespace ANIMOL.Puzzle
{
    // Ambient sprite-only playback; the ceiling anchor never moves.
    public sealed class ClockDomeAnimation : MonoBehaviour
    {
        public SpriteFrameSequence sequence;
        public SpriteRenderer visual;
        public bool motionEnabled = true;
        public float Elapsed { get; private set; }
        void OnEnable() { Elapsed = 0; Render(); }
        public void Advance(float seconds) { Elapsed += Mathf.Max(0, seconds); Render(); }
        void Update() { if (motionEnabled && visual && visual.isVisible) Advance(Time.unscaledDeltaTime); else Render(); }
        void Render() { if (sequence && visual) visual.sprite = sequence.At(motionEnabled ? Elapsed : 0); }
    }
}
