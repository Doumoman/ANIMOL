using UnityEngine;
namespace ANIMOL.StarwayV04
{
    public sealed class StarwayFrames : ScriptableObject
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
