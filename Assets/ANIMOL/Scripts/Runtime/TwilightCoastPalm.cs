using UnityEngine;
using ANIMOL.Presentation;

namespace ANIMOL.Puzzle
{
    // Single ambient sprite writer. No touch/collision subscription exists in the current puzzle.
    public sealed class TwilightCoastPalm : MonoBehaviour
    {
        public SpriteFrameSequence idle;
        public SpriteRenderer visual;
        public bool motionEnabled = true;
        public float Elapsed { get; private set; }
        void OnEnable() { Elapsed = 0; Render(); }
        public void Advance(float seconds) { Elapsed += Mathf.Max(0, seconds); Render(); }
        void Update() { if (motionEnabled && visual && visual.isVisible) Advance(Time.unscaledDeltaTime); else Render(); }
        void Render() { if (idle && visual) visual.sprite = idle.At(motionEnabled ? Elapsed : 0); }
    }
}
