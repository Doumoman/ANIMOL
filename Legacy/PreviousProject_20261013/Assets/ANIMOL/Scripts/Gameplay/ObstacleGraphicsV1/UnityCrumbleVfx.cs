using System;
using UnityEngine;

namespace ANIMOL.Gameplay.ObstacleGraphics
{
    // Adapted from the packaged native component. The stage pool supplies registered
    // sprites/effect data and the same scaled game clock as the physical device.
    public sealed class UnityCrumbleVfx : MonoBehaviour
    {
        private readonly SpriteRenderer[] pieces=new SpriteRenderer[4];
        private CrumbleEffectSettings effect;
        private Vector3 origin;
        private double start;
        private string eventKey;
        public bool IsPlaying {get;private set;}
        public bool TryPlay(string key,Sprite[] sprites,Vector3 cellOrigin,double seconds,Material material,CrumbleEffectSettings settings,int layer,int order=25)
        {
            if(string.IsNullOrEmpty(key)||sprites?.Length!=4||double.IsNaN(seconds)||double.IsInfinity(seconds))throw new ArgumentException("Valid C05 event required.");
            settings.Validate();if(IsPlaying||eventKey==key)return false;
            for(int i=0;i<4;i++)if(sprites[i]==null)throw new ArgumentException("Missing C05 fragment.");
            effect=settings;eventKey=key;origin=cellOrigin;start=seconds;IsPlaying=true;
            for(int i=0;i<4;i++)
            {
                if(pieces[i]==null){var go=new GameObject("C05 fragment "+i,typeof(SpriteRenderer));go.transform.SetParent(transform,false);pieces[i]=go.GetComponent<SpriteRenderer>();}
                var r=pieces[i];r.gameObject.layer=layer;r.sprite=sprites[i];r.sharedMaterial=material;r.sortingOrder=order;r.color=Color.white;r.enabled=true;
                r.transform.rotation=Quaternion.identity;r.transform.localScale=Vector3.one;
            }
            Sample(seconds);return true;
        }
        public void Sample(double seconds)
        {
            if(!IsPlaying)return;
            if(double.IsNaN(seconds)||double.IsInfinity(seconds))throw new ArgumentException("Valid game clock required.");
            float t=Mathf.Max(0,(float)(seconds-start));
            if(t>=effect.durationMs/1000f){Stop();return;}
            float alpha=t<=effect.fadeStartMs/1000f?1:Mathf.Clamp01(1-(t-effect.fadeStartMs/1000f)/((effect.durationMs-effect.fadeStartMs)/1000f));
            for(int i=0;i<4;i++)
            {
                var f=effect.fragments[i];float ppu=effect.pixelsPerUnit;
                float x=origin.x+(f.rect.x+f.rect.width*.5f)/ppu+Mathf.Round(f.velocityX*t*ppu)/ppu;
                float y=origin.y+1-(f.rect.y+f.rect.height*.5f)/ppu+Mathf.Round((f.velocityY*t-.5f*effect.gravity*t*t)*ppu)/ppu;
                pieces[i].transform.position=new Vector3(x,y,origin.z);pieces[i].color=new Color(1,1,1,alpha);
            }
        }
        public void Stop(){IsPlaying=false;foreach(var r in pieces)if(r!=null)r.enabled=false;}
        public void ResetEventCache(){Stop();eventKey=null;}
        private void OnDisable()=>Stop();
    }
}
