using UnityEngine;
using ANIMOL.Presentation;

namespace ANIMOL.Puzzle
{
    // A single sprite writer. No gameplay/input/event subscriptions, lights or root motion.
    public sealed class MemorySkyTheaterAnimation : MonoBehaviour
    {
        public SpriteFrameSequence sequence;
        public SpriteRenderer visual;
        public bool optionalLoop = true;
        public float Elapsed { get; private set; }
        public bool Finished => sequence && !sequence.loop && Elapsed >= sequence.Duration;
        void OnEnable() { Elapsed = 0; Render(); }
        public void Advance(float seconds) { Elapsed += Mathf.Max(0, seconds); Render(); }
        void Update() => Advance(Time.unscaledDeltaTime);
        void Render()
        {
            if (sequence && visual) visual.sprite = sequence.At(!optionalLoop && sequence.loop ? 0 : Elapsed);
        }
    }
}
