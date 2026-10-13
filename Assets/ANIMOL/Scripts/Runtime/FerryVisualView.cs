using System;
using UnityEngine;
namespace ANIMOL.Presentation
{
    [Serializable] public class FerryVisualPair
    {
        public string state;
        public SpriteFrameSequence rear,front,wake;
        public Sprite rearTerminal,frontTerminal;
        public Vector2[] passengerOffsets;
    }
    // One presentation clock, no authority over movement, boarding, occupancy or turns.
    // The full composite has no renderer in this occupied-boat prefab.
    public sealed class FerryVisualView : MonoBehaviour
    {
        public DeviceVisualView clock;
        public SpriteRenderer rear,front,wake,passenger;
        public Sprite wakeClear;
        public FerryVisualPair[] pairs;
        public Vector2 seatOffset=new Vector2(0,-5f/80);
        public Vector2 passengerFootToPivot;
        public int FrameIndex {get;private set;}
        public bool BlocksInput=>false;
        public bool Present(string state,bool restart=false){if(Array.Find(pairs,p=>p.state==state)==null)return false;bool ok=clock.Present(state,restart);Render();return ok;}
        public bool Notify(string actionId,string committedEvent,string state){if(Array.Find(pairs,p=>p.state==state)==null)return false;bool ok=clock.Notify(actionId,committedEvent,state);Render();return ok;}
        public void Advance(float seconds){clock.Advance(seconds);Render();}
        public void ResetVisual(){clock.ResetVisual();Render();}
        void OnEnable(){if(clock)ResetVisual();}
        void LateUpdate()=>Render();
        public void Render()
        {
            if(!clock)return;var pair=Array.Find(pairs??Array.Empty<FerryVisualPair>(),p=>p.state==clock.CurrentState);
            if(pair==null){rear.enabled=front.enabled=wake.enabled=false;return;}
            float t=clock.Elapsed;bool ended=!pair.rear.loop&&t>=pair.rear.Duration;
            rear.sprite=ended?pair.rearTerminal:pair.rear.At(t);front.sprite=ended?pair.frontTerminal:pair.front.At(t);
            rear.enabled=rear.sprite;front.enabled=front.sprite;
            FrameIndex=0;float local=pair.rear.loop?Mathf.Repeat(t,pair.rear.Duration):t;
            if(!ended)while(FrameIndex<pair.rear.frames.Length-1&&local>=pair.rear.durations[FrameIndex]){local-=pair.rear.durations[FrameIndex];FrameIndex++;}
            Vector2 offset=!ended&&pair.passengerOffsets!=null&&FrameIndex<pair.passengerOffsets.Length?pair.passengerOffsets[FrameIndex]:Vector2.zero;
            if(passenger)passenger.transform.localPosition=seatOffset+passengerFootToPivot+new Vector2(offset.x,-offset.y)/80;
            wake.sprite=pair.wake&&!ended?pair.wake.At(t):wakeClear;wake.enabled=pair.wake&&!ended;
        }
    }
}
