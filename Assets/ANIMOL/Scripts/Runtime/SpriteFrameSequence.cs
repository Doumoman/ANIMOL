using UnityEngine;
namespace ANIMOL.Presentation
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ANIMOL.StarwayV04", "ANIMOL.StarwayV04", "StarwayFrames")]
    public sealed class SpriteFrameSequence : ScriptableObject
    {
        public Sprite[] frames;
        public float[] durations;
        public bool loop;
        public float Duration { get { float total=0; foreach(var d in durations) total+=d; return total; } }
        public Sprite At(float seconds)
        {
            if(loop) seconds=Mathf.Repeat(seconds,Duration);
            for(int i=0;i<frames.Length;i++) { if(seconds<durations[i]) return frames[i]; seconds-=durations[i]; }
            return frames[frames.Length-1];
        }
    }
}
