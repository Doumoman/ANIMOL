using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Gameplay.ObstacleGraphics
{
    [Serializable] public sealed class MechanismSprite { public string key; public Sprite sprite; }
    [Serializable] public sealed class FragmentSprite {public string themeId;public int piece;public Sprite sprite;}
    [Serializable] public sealed class CrumbleRect {public int x,y,width,height;}
    [Serializable] public sealed class CrumbleFragment {public int id;public CrumbleRect rect;public float velocityX,velocityY;}
    [Serializable] public sealed class CrumbleEffectSettings
    {
        public int visualVersion,durationMs,fadeStartMs,pixelsPerUnit,rotationDegrees;
        public float gravity;
        public bool pixelSnap,colliders;
        public CrumbleFragment[] fragments;
        public void Validate()
        {
            if(visualVersion!=3 || durationMs!=400 || fadeStartMs!=80 || pixelsPerUnit!=32 || gravity!=5 || !pixelSnap || colliders || rotationDegrees!=0 || fragments?.Length!=4)
                throw new InvalidOperationException("Invalid native C05 effect contract.");
            var starts=new[]{0,8,18,26};var widths=new[]{8,10,8,6};var vx=new[]{-.10f,-.03f,.04f,.10f};var vy=new[]{-1.2f,-1.6f,-1.35f,-1.8f};
            for(int i=0;i<4;i++)if(fragments[i]?.rect==null||fragments[i].id!=i||fragments[i].rect.x!=starts[i]||fragments[i].rect.y!=0||fragments[i].rect.width!=widths[i]||fragments[i].rect.height!=8||fragments[i].velocityX!=vx[i]||fragments[i].velocityY!=vy[i])
                throw new InvalidOperationException("Invalid C05 fragment registration.");
        }
    }
    public sealed class ObstacleArtRegistry : ScriptableObject
    {
        public int visualVersion = 3, terrainArtVersion = 6;
        public string sourceHash;
        public MechanismSprite[] markers;
        public Sprite Marker(string key) => markers.First(e=>e.key==key).sprite;
        public MechanismSprite[] entries;
        public FragmentSprite[] fragments;
        public CrumbleEffectSettings crumble;
        public ANIMOL.UI.FeedbackProfile CrumbleFeedback => new ANIMOL.UI.FeedbackProfile(ANIMOL.UI.FeedbackStrength.Subtle,crumble.durationMs/1000f,0,0,false,false,false);
        private Dictionary<string, Sprite> lookup;
        public static ObstacleArtRegistry Load()
        {
            var value = Resources.Load<ObstacleArtRegistry>("ANIMOL_ObstacleGraphicsV1");
            if (value == null) throw new InvalidOperationException("Obstacle graphics visualVersion=3 for current terrain is not installed.");
            if (value.lookup == null) value.ValidateComplete();
            return value;
        }
        public void ValidateComplete()
        {
            lookup = null;
            if (visualVersion != 3 || terrainArtVersion != 6 || entries?.Length != 393 ||
                entries.Any(e => e == null || e.sprite == null || string.IsNullOrEmpty(e.key)) || entries.Select(e => e.key).Distinct().Count() != 393)
                throw new InvalidOperationException("Incomplete obstacle graphics registry.");
            if(entries.Any(e=>e.key.Contains("/C09/")) || fragments?.Length!=20 || fragments.Any(f=>f==null||f.sprite==null||f.piece<0||f.piece>3) || fragments.Select(f=>f.themeId+"/"+f.piece).Distinct().Count()!=20)
                throw new InvalidOperationException("Incomplete common-nine/fragment registry.");
            for(int t=1;t<=5;t++)for(int p=0;p<4;p++)if(!fragments.Any(f=>f.themeId=="T"+t.ToString("D2")&&f.piece==p))throw new InvalidOperationException("Missing C05 fragment.");
            crumble?.Validate();if(crumble==null)throw new InvalidOperationException("C05 effect contract missing.");
            lookup = entries.ToDictionary(e => e.key, e => e.sprite, StringComparer.Ordinal);
        }
        public Sprite Sprite(string key)
        {
            if (lookup == null) ValidateComplete();
            return lookup.TryGetValue(key, out var sprite) ? sprite : throw new InvalidOperationException("Unregistered mechanism Sprite: " + key);
        }
        public Sprite[] Fragments(string theme) => Enumerable.Range(0,4).Select(i=>fragments.First(f=>f.themeId==theme&&f.piece==i).sprite).ToArray();
        private void OnEnable() => lookup = null;
        private void OnValidate() => lookup = null;
    }
}
