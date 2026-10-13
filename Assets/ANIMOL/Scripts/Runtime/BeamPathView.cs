using System;
using System.Collections.Generic;
using UnityEngine;
namespace ANIMOL.Presentation
{
    [Serializable] public class BeamVisualCell {public string shape;public Vector2 position;}
    [Serializable] public class BeamVisualSprite {public string shape;public Sprite sprite;}
    public sealed class BeamPathView : MonoBehaviour
    {
        public BeamVisualSprite[] sprites;
        public int sortingOrder=15;
        [SerializeField] List<SpriteRenderer> pool=new List<SpriteRenderer>();
        public int ActiveCount {get;private set;}
        void OnEnable(){ActiveCount=0;foreach(var renderer in pool)if(renderer && renderer.enabled)ActiveCount++;}
        // Caller supplies committed trace cells. This is not a ray tracer or collision/turn authority.
        public void Present(BeamVisualCell[] cells)
        {
            for(int i=0;i<cells.Length;i++)
            {
                var entry=Array.Find(sprites,s=>s.shape==cells[i].shape);if(entry==null)throw new ArgumentException(cells[i].shape);
                if(i>=pool.Count){var child=new GameObject("Beam visual");child.transform.SetParent(transform,false);pool.Add(child.AddComponent<SpriteRenderer>());}
                var renderer=pool[i];renderer.sprite=entry.sprite;renderer.sortingOrder=sortingOrder;renderer.transform.localPosition=cells[i].position;renderer.enabled=true;
            }
            for(int i=cells.Length;i<pool.Count;i++)pool[i].enabled=false;ActiveCount=cells.Length;
        }
        public void Clear()=>Present(Array.Empty<BeamVisualCell>());
        void OnDisable()=>Clear();
    }
}
