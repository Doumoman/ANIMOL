using System.Collections.Generic;
using System.Linq;
using Animol.TerrainStructure;
using ANIMOL.Gameplay.ObstacleGraphics;
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
        private Dictionary<Vector2Int,string> graphicSignatures=new();
        private readonly Dictionary<Vector2Int,GameObject> details=new();
        private readonly Dictionary<(Sprite,bool),Sprite> strips=new();
        private int seed;
        private float currentUnits;
        private FreeShapeArtRegistry currentArt;
        private bool initialized;
        public int LastUpdatedCells {get;private set;}
        public int LastUpdatedMotifs {get;private set;}
        public int TilemapCount=>map==null?0:1;
        public int MotifCount=>motifs.Count;
        public double LastMilliseconds {get;private set;}
        public void Rebuild(FreeShapeLayer layer,float units,IReadOnlyDictionary<Vector2Int,GraphicCell> devices=null,IReadOnlyDictionary<Vector2Int,GraphicCell> previewContext=null)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();var cells=FreeShapeTopology.Index(layer);var art=FreeShapeArtRegistry.Load(layer);
            var graphics=ObstacleSnapshotAdapter.Merge(layer,devices);
            var context=previewContext??graphics;
            var signatures=graphics.ToDictionary(p=>p.Key,p=>string.Join("|",p.Value.Kind,p.Value.StyleId,p.Value.ThemeId,p.Value.ArtVersion,p.Value.JoinGroup,p.Value.Facing,p.Value.Pose,p.Value.SurfaceEnabled));
            var plans=new Dictionary<Vector2Int,GraphicPlan>();
            var grid=GetComponent<Grid>();if(grid==null)grid=gameObject.AddComponent<Grid>();grid.cellSize=new Vector3(units,units,1);
            var changed=new HashSet<Vector2Int>(previous.Keys.Where(p=>!cells.TryGetValue(p,out var c)||c.styleId!=previous[p].styleId));
            foreach(var pair in cells)if(!initialized||currentArt!=art||seed!=layer.seed||!previous.TryGetValue(pair.Key,out var old)||old.styleId!=pair.Value.styleId)changed.Add(pair.Key);
            foreach(var p in graphicSignatures.Keys)if(!signatures.TryGetValue(p,out var value)||value!=graphicSignatures[p])changed.Add(p);
            foreach(var p in signatures.Keys)if(!graphicSignatures.TryGetValue(p,out var value)||value!=signatures[p])changed.Add(p);
            if(!initialized||currentArt!=art||seed!=layer.seed||currentUnits!=units||previewContext!=null)changed.UnionWith(graphics.Keys);
            if(currentArt!=art && map!=null)map.GetComponent<TilemapRenderer>().sharedMaterial=art.material;
            var affected=FreeShapeTopology.AffectedCells(changed);LastUpdatedCells=affected.Count;
            foreach(var p in affected)
            {
                if(!graphics.ContainsKey(p))continue;
                var plan=ObstacleVisualResolver.Resolve((x,y)=>context.GetValueOrDefault(new Vector2Int(x,y)),p.x,p.y,layer.seed);
                if(plan.OverlayKey!=null)ObstacleArtRegistry.Load().Sprite(plan.OverlayKey);
                if(plan.Problems.Count>0)throw new System.InvalidOperationException(p+": "+string.Join("; ",plan.Problems));
                plans.Add(p,plan);
            }
            foreach(var p in affected)
            {
                if(details.TryGetValue(p,out var detail)){detail.SetActive(false);Release(detail);details.Remove(p);}
                if(!graphics.TryGetValue(p,out var c)){if(map!=null)map.SetTile(new Vector3Int(p.x,p.y,0),null);continue;}
                var plan=plans[p];
                if(map==null)
                {
                    var go=new GameObject("FreeShape terrain",typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(transform,false);go.layer=gameObject.layer;go.hideFlags=gameObject.hideFlags;
                    map=go.GetComponent<Tilemap>();map.tileAnchor=new Vector3(.5f,.5f,0);var renderer=go.GetComponent<TilemapRenderer>();renderer.sortingOrder=21;renderer.sharedMaterial=art.material;
                }
                if(plan.DrawBody)
                {
                    var sprite=art.Cell(c.StyleId,plan.BodyRaw,plan.Variant);
                    if(plan.CapPatch)sprite=Crop(sprite,false);
                    if(!tiles.TryGetValue(sprite,out var tile)){tile=ScriptableObject.CreateInstance<Tile>();tile.hideFlags=HideFlags.HideAndDontSave;tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;tiles.Add(sprite,tile);}
                    map.SetTile(new Vector3Int(p.x,p.y,0),tile);
                }
                else map.SetTile(new Vector3Int(p.x,p.y,0),null);
                if(plan.CapOnly||plan.CapPatch||plan.OverlayKey!=null)
                {
                    var root=new GameObject("Obstacle join "+p);root.transform.SetParent(transform,false);root.layer=gameObject.layer;root.hideFlags=gameObject.hideFlags;details.Add(p,root);
                    if(plan.CapOnly||plan.CapPatch)DrawDetail(root,Crop(art.Cell(c.StyleId,plan.CapRaw,plan.Variant),true),new Vector3(p.x+.5f,p.y+.875f,0)*units,units,23,art.material);
                    if(plan.OverlayKey!=null)DrawDetail(root,ObstacleArtRegistry.Load().Sprite(plan.OverlayKey),new Vector3(p.x+.5f,p.y+.5f,0)*units,units,24,art.material);
                }
            }
            var candidates=FreeShapeTopology.AffectedMotifs(changed);LastUpdatedMotifs=candidates.Count;
            foreach(var p in candidates)
            {
                if(motifs.TryGetValue(p,out var old)){old.SetActive(false);Release(old);motifs.Remove(p);}
                if(!cells.TryGetValue(p,out var c)||!FreeShapeTopology.Motif(cells,p,c.styleId,layer.seed))continue;
                var go=new GameObject("FreeShape motif "+p,typeof(SpriteRenderer));go.transform.SetParent(transform,false);go.layer=gameObject.layer;go.hideFlags=gameObject.hideFlags;go.transform.localPosition=new Vector3(p.x,p.y,0)*units;go.transform.localScale=Vector3.one*units;
                var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=art.Style(c.styleId).motif;renderer.sharedMaterial=art.material;renderer.sortingOrder=22;motifs.Add(p,go);
            }
            currentUnits=units;graphicSignatures=signatures;previous=cells;seed=layer.seed;currentArt=art;initialized=true;LastMilliseconds=timer.Elapsed.TotalMilliseconds;
        }
        private Sprite Crop(Sprite source,bool cap)
        {
            if(strips.TryGetValue((source,cap),out var value))return value;
            var r=source.rect;var rect=cap?new Rect(r.x,r.y+24,32,8):new Rect(r.x,r.y,32,24);
            value=Sprite.Create(source.texture,rect,cap?new Vector2(.5f,.5f):new Vector2(.5f,16f/24f),32,0,SpriteMeshType.FullRect,Vector4.zero,false);
            value.hideFlags=HideFlags.HideAndDontSave;strips.Add((source,cap),value);return value;
        }
        private static void DrawDetail(GameObject parent,Sprite sprite,Vector3 position,float units,int order,Material material)
        {
            var go=new GameObject("Source pixels",typeof(SpriteRenderer));go.transform.SetParent(parent.transform,false);go.layer=parent.layer;go.hideFlags=parent.hideFlags;
            go.transform.localPosition=position;go.transform.localScale=Vector3.one*units;
            var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.sortingOrder=order;
        }
        public void SetOpacity(float opacity)
        {if(map!=null)map.color=new Color(1,1,1,opacity);foreach(var g in motifs.Values)g.GetComponent<SpriteRenderer>().color=new Color(1,1,1,opacity);foreach(var g in details.Values)foreach(var r in g.GetComponentsInChildren<SpriteRenderer>())r.color=new Color(1,1,1,opacity);}
        private static void Release(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy(){foreach(var t in tiles.Values)if(t!=null)Release(t);foreach(var s in strips.Values)if(s!=null)Release(s);}
    }
}
