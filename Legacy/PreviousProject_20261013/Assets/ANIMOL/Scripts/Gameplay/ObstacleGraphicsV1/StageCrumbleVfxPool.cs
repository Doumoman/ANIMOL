using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Gameplay.ObstacleGraphics
{
    // This repository has no reusable stage VFX pool. Extend the existing graphics
    // owner's stage lifetime with a bounded-by-peak pool, never a platform child.
    public sealed class StageCrumbleVfxPool : MonoBehaviour
    {
        private readonly Stack<UnityCrumbleVfx> available=new();
        private readonly List<(UnityCrumbleVfx fx,int owner)> active=new();
        private readonly Dictionary<int,(int generation,int sequence)> latest=new();
        private readonly Dictionary<string,Sprite[]> sprites=new();
        public int ActiveCount=>active.Count;
        public int PooledCount=>available.Count;
        public int EventOwnerCount=>latest.Count;
        public bool TryPlay(int owner,int generation,int sequence,string theme,Vector3 origin,double seconds)
        {
            if(sequence<=0)throw new ArgumentException("Authoritative collapse sequence required.");
            if(latest.TryGetValue(owner,out var last) && (generation<last.generation||generation==last.generation&&sequence<=last.sequence))return false;
            var registry=ObstacleArtRegistry.Load();
            if(!sprites.TryGetValue(theme,out var frames)){frames=registry.Fragments(theme);sprites.Add(theme,frames);}
            var fx=available.Count>0?available.Pop():Create();
            string key=owner+"/"+generation+"/"+sequence;
            if(!fx.TryPlay(key,frames,origin,seconds,FreeShapeArtRegistry.Load().material,registry.crumble,gameObject.layer))return false;
            latest[owner]=(generation,sequence);active.Add((fx,owner));return true;
        }
        private UnityCrumbleVfx Create()
        {var go=new GameObject("C05 native crumble");go.transform.SetParent(transform,false);go.transform.localScale=Vector3.one;return go.AddComponent<UnityCrumbleVfx>();}
        public void Sample(double seconds)
        {
            for(int i=active.Count-1;i>=0;i--)
            {
                var fx=active[i].fx;fx.Sample(seconds);if(fx.IsPlaying)continue;
                active.RemoveAt(i);available.Push(fx);
            }
        }
        public void ClearOwner(int owner)
        {
            for(int i=active.Count-1;i>=0;i--)if(active[i].owner==owner){var fx=active[i].fx;fx.ResetEventCache();active.RemoveAt(i);available.Push(fx);}
            latest.Remove(owner);
        }
        public void ClearStage(){foreach(var p in active){p.fx.ResetEventCache();available.Push(p.fx);}active.Clear();latest.Clear();foreach(var fx in available)fx.ResetEventCache();}
        private void OnDisable()=>ClearStage();
    }
}
