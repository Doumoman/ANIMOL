using System.Collections.Generic;
using System.Linq;
using Animol.TerrainStructure;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Gameplay
{
    // Visual cache only. The existing operational Tilemap owns all physics.
    public sealed class FreeShapeTerrainRenderer : MonoBehaviour
    {
        private Tilemap map;
        private readonly Dictionary<Vector2Int,GameObject> motifs=new Dictionary<Vector2Int,GameObject>();
        private readonly Dictionary<Sprite,Tile> tiles=new Dictionary<Sprite,Tile>();
        private Dictionary<Vector2Int,FreeShapeCell> previous=new Dictionary<Vector2Int,FreeShapeCell>();
        private int seed;
        private FreeShapeArtRegistry currentArt;
        private bool initialized;
        public int LastUpdatedCells {get;private set;}
        public int LastUpdatedMotifs {get;private set;}
        public int TilemapCount=>map==null?0:1;
        public int MotifCount=>motifs.Count;
        public double LastMilliseconds {get;private set;}
        public void Rebuild(FreeShapeLayer layer,float units)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();var cells=FreeShapeTopology.Index(layer);var art=FreeShapeArtRegistry.Load(layer);
            var grid=GetComponent<Grid>();if(grid==null)grid=gameObject.AddComponent<Grid>();grid.cellSize=new Vector3(units,units,1);
            var changed=new HashSet<Vector2Int>(previous.Keys.Where(p=>!cells.TryGetValue(p,out var c)||c.styleId!=previous[p].styleId));
            foreach(var pair in cells)if(!initialized||currentArt!=art||seed!=layer.seed||!previous.TryGetValue(pair.Key,out var old)||old.styleId!=pair.Value.styleId)changed.Add(pair.Key);
            if(currentArt!=art && map!=null)map.GetComponent<TilemapRenderer>().sharedMaterial=art.material;
            var affected=FreeShapeTopology.AffectedCells(changed);LastUpdatedCells=affected.Count;
            foreach(var p in affected)
            {
                if(!cells.TryGetValue(p,out var c)){if(map!=null)map.SetTile(new Vector3Int(p.x,p.y,0),null);continue;}
                if(map==null)
                {
                    var go=new GameObject("FreeShape terrain",typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(transform,false);go.layer=gameObject.layer;go.hideFlags=gameObject.hideFlags;
                    map=go.GetComponent<Tilemap>();map.tileAnchor=new Vector3(.5f,.5f,0);var renderer=go.GetComponent<TilemapRenderer>();renderer.sortingOrder=21;renderer.sharedMaterial=art.material;
                }
                var sprite=art.Cell(c.styleId,FreeShapeTopology.Raw(cells,p),FreeShapeTopology.Variant(p.x,p.y,layer.seed));
                if(!tiles.TryGetValue(sprite,out var tile)){tile=ScriptableObject.CreateInstance<Tile>();tile.hideFlags=HideFlags.HideAndDontSave;tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;tiles.Add(sprite,tile);}
                map.SetTile(new Vector3Int(p.x,p.y,0),tile);
            }
            var candidates=FreeShapeTopology.AffectedMotifs(changed);LastUpdatedMotifs=candidates.Count;
            foreach(var p in candidates)
            {
                if(motifs.TryGetValue(p,out var old)){old.SetActive(false);Release(old);motifs.Remove(p);}
                if(!cells.TryGetValue(p,out var c)||!FreeShapeTopology.Motif(cells,p,c.styleId,layer.seed))continue;
                var go=new GameObject("FreeShape motif "+p,typeof(SpriteRenderer));go.transform.SetParent(transform,false);go.layer=gameObject.layer;go.hideFlags=gameObject.hideFlags;go.transform.localPosition=new Vector3(p.x,p.y,0)*units;go.transform.localScale=Vector3.one*units;
                var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=art.Style(c.styleId).motif;renderer.sharedMaterial=art.material;renderer.sortingOrder=22;motifs.Add(p,go);
            }
            previous=cells;seed=layer.seed;currentArt=art;initialized=true;LastMilliseconds=timer.Elapsed.TotalMilliseconds;
        }
        public void SetOpacity(float opacity)
        {if(map!=null)map.color=new Color(1,1,1,opacity);foreach(var g in motifs.Values)g.GetComponent<SpriteRenderer>().color=new Color(1,1,1,opacity);}
        private static void Release(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy(){foreach(var t in tiles.Values)if(t!=null)Release(t);}
    }
}
