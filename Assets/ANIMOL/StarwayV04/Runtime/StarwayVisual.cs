using UnityEngine;
namespace ANIMOL.StarwayV04
{
    // Presentation only: caller retains collection, cost, turn and collision authority.
    public sealed class StarwayVisual : MonoBehaviour
    {
        public SpriteRenderer visual;
        public StarwayFrames idle, touch;
        public bool paused, returnToPool;
        public float idlePhase;
        public bool IsTouching { get; private set; }
        float elapsed;
        void OnEnable() { ResetVisual(); }
        public void ResetVisual() { elapsed=0; IsTouching=false; if(visual) { visual.enabled=true; if(idle) visual.sprite=idle.At(idlePhase); } }
        public void PlayTouch() { if(!touch)return; elapsed=0; IsTouching=true; if(visual)visual.sprite=touch.frames[0]; }
        public void Advance(float seconds)
        {
            if(paused || !visual) return;
            elapsed+=Mathf.Max(0,seconds);
            if(IsTouching && elapsed>=touch.Duration)
            {
                IsTouching=false; elapsed=0; idlePhase=0;
                if(returnToPool) { visual.enabled=false; gameObject.SetActive(false); return; }
            }
            var sequence=IsTouching?touch:idle;
            if(sequence)visual.sprite=sequence.At(elapsed+(IsTouching?0:idlePhase));
        }
        void Update() { Advance(Time.unscaledDeltaTime); }
    }
}
