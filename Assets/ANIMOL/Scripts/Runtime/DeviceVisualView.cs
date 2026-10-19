using System;
using System.Collections.Generic;
using ANIMOL.Art.EchoTheaterV03;
using UnityEngine;
namespace ANIMOL.Presentation
{
    [Serializable] public class DeviceVisualState
    {
        public string name, eventName;
        public Sprite sprite, terminal;
        public SpriteFrameSequence sequence;
        // Authored clip references the same frames; only the variable-time sequence writes the renderer.
        public AnimationClip clip;
        public string v03ClipKey;
        public float v03StaticProgress;
    }
    // Presentation only: authoritative gameplay calls Present/Notify after committing its action.
    // No movement, occupancy, collection, cost, turn, collider, audio or gameplay RNG is owned here.
    public sealed class DeviceVisualView : MonoBehaviour
    {
        public string deviceId, initialState;
        public SpriteRenderer visual;
        public DeviceVisualState[] states;
        public bool paused;
        public float initialPhase;
        public string CurrentState {get;private set;}
        public float Elapsed {get;private set;}
        DeviceVisualState active;
        EchoTheaterV03AnimatedVisual v03Player;
        bool v03Active;
        readonly HashSet<string> seenEvents=new HashSet<string>();
        readonly Queue<string> eventOrder=new Queue<string>();
        public bool BlocksInput=>false;
        public bool IsV03Active => v03Active;
        void OnEnable()=>ResetVisual();
        public bool ActivateV03(EchoTheaterV03VisualCatalog catalog, string manifestSha256)
        {
            if (!visual || !catalog) return false;
            v03Player = GetComponent<EchoTheaterV03AnimatedVisual>();
            if (!v03Player) v03Player = gameObject.AddComponent<EchoTheaterV03AnimatedVisual>();
            v03Active = v03Player.Configure(catalog, visual, manifestSha256);
            return v03Active && Present(CurrentState ?? initialState, true);
        }
        public void FailV03(string diagnostic)
        {
            if (v03Active) v03Player.SetClip("INVALID_V03_BINDING_" + diagnostic);
        }
        public bool Present(string state, bool restart=false)
        {
            var next=Array.Find(states??Array.Empty<DeviceVisualState>(),s=>s.name==state);
            if(next==null)
            {
                if (v03Active) FailV03(state);
                return false;
            }
            if(active==next && !restart)return true;
            active=next;CurrentState=state;Elapsed=0;
            if (v03Active)
            {
                if (string.IsNullOrEmpty(next.v03ClipKey))
                    return v03Player.SetClip(next.v03ClipKey);
                return next.sequence
                    ? v03Player.Play(next.v03ClipKey)
                    : v03Player.SnapToState(next.v03ClipKey, next.v03StaticProgress);
            }
            Render();return true;
        }
        public bool Notify(string committedEvent,string state)
        {
            var next=Array.Find(states??Array.Empty<DeviceVisualState>(),s=>s.name==state && s.eventName==committedEvent);
            return next!=null && Present(state,true);
        }
        public bool Notify(string actionId,string committedEvent,string state)
        {
            if(string.IsNullOrEmpty(actionId))return false;
            string key=actionId+"\u001f"+committedEvent+"\u001f"+state;
            if(seenEvents.Contains(key) || !Notify(committedEvent,state))
            {
                if (v03Active && !seenEvents.Contains(key)) FailV03(state);
                return false;
            }
            seenEvents.Add(key);eventOrder.Enqueue(key);
            if(eventOrder.Count>256)seenEvents.Remove(eventOrder.Dequeue());
            return true;
        }
        public void ResetVisual(){paused=false;active=null;seenEvents.Clear();eventOrder.Clear();Present(initialState,true);if(active!=null&&active.sequence&&active.sequence.loop)Advance(initialPhase);}
        public void RebindArt(DeviceVisualState[] replacement,string initial)
        {
            string state=CurrentState;float elapsed=Elapsed;
            states=replacement;initialState=initial;active=null;
            if(!Present(state,true))Present(initial,true);
            if(CurrentState==state)Elapsed=elapsed;
            if (!v03Active) Render(); // Cosmetic theme changes preserve pause and action IDs.
        }
        public void Advance(float seconds)
        {
            if (v03Active) v03Player.SetPaused(paused);
            if(paused||active==null)return;
            Elapsed+=Mathf.Max(0,seconds);
            if (!v03Active) Render();
        }
        void Render()
        {
            if(!visual||active==null)return;
            if(!active.sequence){visual.sprite=active.sprite;visual.enabled=active.sprite;return;}
            bool ended=!active.sequence.loop && Elapsed>=active.sequence.Duration;
            visual.sprite=ended?active.terminal:active.sequence.At(Elapsed);
            visual.enabled=visual.sprite!=null;
        }
        void Update()=>Advance(v03Active ? Time.deltaTime : Time.unscaledDeltaTime);
        public static Vector2Int Direction(string direction)
        {
            switch(direction){case "n":return Vector2Int.up;case "e":return Vector2Int.right;case "s":return Vector2Int.down;case "w":return Vector2Int.left;default:throw new ArgumentException(direction);}
        }
    }
}
