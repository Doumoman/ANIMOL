using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Animol.TerrainStructure
{
    [Serializable] public sealed class FreeShapeCell
    {
        public int x, y;
        public string styleId;
        public Vector2Int Position => new Vector2Int(x,y);
    }
    // Independent saved contract. No reuse of v3 tile IDs, variants or placement schema.
    [Serializable] public sealed class FreeShapeLayer
    {
        public const string Contract = "ANIMOL_FREE_SHAPE_BLOB47_V1";
        public static bool SupportsArtVersion(int version)=>version==1 || version==2 || version==3 || version==4;
        public int schemaVersion = 1, artVersion = 1, seed = 0;
        public string artContract = Contract;
        public List<FreeShapeCell> cells = new List<FreeShapeCell>();
        public bool HasData => cells == null || cells.Count != 0 || schemaVersion != 1 ||
            artVersion != 1 || artContract != Contract || seed != 0;
        public FreeShapeLayer Copy() => new FreeShapeLayer {schemaVersion=schemaVersion,artVersion=artVersion,
            seed=seed,artContract=artContract,cells=cells?.Select(c=>c==null?null:new FreeShapeCell{x=c.x,y=c.y,styleId=c.styleId}).ToList()};
    }

    public static class FreeShapeTopology
    {
        public static readonly Vector2Int[] Neighbors = {new Vector2Int(0,1),new Vector2Int(1,1),new Vector2Int(1,0),new Vector2Int(1,-1),new Vector2Int(0,-1),new Vector2Int(-1,-1),new Vector2Int(-1,0),new Vector2Int(-1,1)};
        public static int Canonical(int raw)
        {
            if(raw<0 || raw>255)throw new ArgumentOutOfRangeException(nameof(raw));
            int mask=raw;
            if((raw&5)!=5)mask&=~2;
            if((raw&20)!=20)mask&=~8;
            if((raw&80)!=80)mask&=~32;
            if((raw&65)!=65)mask&=~128;
            return mask;
        }
        public static int FloorMod(long value,int n)=>(int)((value%n+n)%n);
        public static int Variant(int x,int y,int seed)=>FloorMod((long)x+(seed&1),2)+2*FloorMod(-(long)y+((seed>>1)&1),2);
        public static Vector2Int Chunk(Vector2Int p)=>new Vector2Int(AnimolTerrainPlacementEngine.FloorDiv(p.x,16),AnimolTerrainPlacementEngine.FloorDiv(p.y,16));
        public static uint Hash(string style,int x,int y,int seed)
        {
            string text=style+"|"+x.ToString(CultureInfo.InvariantCulture)+"|"+y.ToString(CultureInfo.InvariantCulture)+"|"+seed.ToString(CultureInfo.InvariantCulture);
            uint hash=2166136261;
            unchecked {foreach(char c in text){hash^=c;hash*=16777619;}}
            return hash;
        }
        public static Dictionary<Vector2Int,FreeShapeCell> Index(FreeShapeLayer layer)
        {
            var result=new Dictionary<Vector2Int,FreeShapeCell>();
            if(layer==null)return result;
            if(layer.schemaVersion!=1 || !FreeShapeLayer.SupportsArtVersion(layer.artVersion) || layer.artContract!=FreeShapeLayer.Contract || layer.cells==null)
                throw new InvalidOperationException("Unsupported free-shape schema/art contract; explicit migration required.");
            foreach(var c in layer.cells)
            {
                if(c==null || string.IsNullOrEmpty(c.styleId) || !result.TryAdd(c.Position,c))
                    throw new InvalidOperationException("Null/duplicate free-shape cell.");
            }
            return result;
        }
        public static int Raw(IReadOnlyDictionary<Vector2Int,FreeShapeCell> cells,Vector2Int p)
        {
            if(!cells.TryGetValue(p,out var cell))return -1;
            int raw=0;
            for(int i=0;i<8;i++)if(cells.TryGetValue(p+Neighbors[i],out var n)&&n.styleId==cell.styleId)raw|=1<<i;
            return raw;
        }
        public static bool Motif(IReadOnlyDictionary<Vector2Int,FreeShapeCell> cells,Vector2Int p,string style,int seed)
        {
            if(FloorMod(p.x,8)!=2 || FloorMod(p.y,8)!=2 || Hash(style,p.x,p.y,seed)%4==0)return false;
            for(int y=-1;y<=4;y++)for(int x=-1;x<=4;x++)
                if(!cells.TryGetValue(p+new Vector2Int(x,y),out var c)||c.styleId!=style)return false;
            return true;
        }
        public static HashSet<Vector2Int> Component(FreeShapeLayer layer,Vector2Int start)
        {
            var cells=Index(layer);var found=new HashSet<Vector2Int>();
            if(!cells.TryGetValue(start,out var root))return found;
            var queue=new Queue<Vector2Int>();queue.Enqueue(start);found.Add(start);
            while(queue.Count>0){var p=queue.Dequeue();for(int i=0;i<8;i+=2){var n=p+Neighbors[i];if(cells.TryGetValue(n,out var c)&&c.styleId==root.styleId&&found.Add(n))queue.Enqueue(n);}}
            return found;
        }
        public static HashSet<Vector2Int> AffectedCells(IEnumerable<Vector2Int> changed)
        {var result=new HashSet<Vector2Int>();foreach(var p in changed)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)result.Add(p+new Vector2Int(x,y));return result;}
        public static HashSet<Vector2Int> AffectedMotifs(IEnumerable<Vector2Int> changed)
        {var result=new HashSet<Vector2Int>();foreach(var p in changed)for(int y=-4;y<=1;y++)for(int x=-4;x<=1;x++){var a=p+new Vector2Int(x,y);if(FloorMod(a.x,8)==2&&FloorMod(a.y,8)==2)result.Add(a);}return result;}
        public static IEnumerable<Vector2Int> FromRows(string[] rows,Vector2Int origin)
        {
            if(rows==null||rows.Length==0)throw new ArgumentException("Empty fixture rows.");
            int width=rows[0].Length;
            for(int row=0;row<rows.Length;row++) {if(rows[row].Length!=width)throw new ArgumentException("Unequal fixture rows.");for(int x=0;x<width;x++)
                {char c=rows[row][x];if(c=='#')yield return origin+new Vector2Int(x,rows.Length-1-row);else if(c!='.')throw new ArgumentException("Invalid fixture cell.");}}
        }
    }
    public static partial class AnimolTerrainPlacementEngine
    {
        public static bool TryEditFreeShape(AnimolTerrainCatalogData catalog,AnimolTerrainSavedMap source,
            IEnumerable<Vector2Int> positions,bool erase,string style,out AnimolTerrainSavedMap candidate,out string error)
        {
            candidate=null;
            if(!Resolve(catalog,source,out var resolved,out error))return false;
            var edited=CloneMap(source);var cells=FreeShapeTopology.Index(edited.freeShape);bool changed=false;
            foreach(var p in positions.Distinct())
            {
                if(resolved.solids.TryGetValue(p,out var occupied) && !cells.ContainsKey(p))
                    return Fail("기존 지형/v3 소유 셀 "+p+": 해당 모드 또는 완성 instance로 편집하세요.",out error);
                if(erase){changed|=cells.Remove(p);continue;}
                if(cells.TryGetValue(p,out var old)&&old.styleId==style)continue;
                cells[p]=new FreeShapeCell{x=p.x,y=p.y,styleId=style};changed=true;
            }
            if(!changed)return Fail("변경할 자유형 셀이 없습니다.",out error);
            edited.freeShape.cells=cells.Values.OrderBy(c=>c.y).ThenBy(c=>c.x).ToList();
            return CompleteTransaction(catalog,source,edited,null,out candidate,out error);
        }
        public static bool TryMoveFreeShape(AnimolTerrainCatalogData catalog,AnimolTerrainSavedMap source,
            Vector2Int selected,Vector2Int delta,bool delete,out AnimolTerrainSavedMap candidate,out string error)
        {
            candidate=null;if(!Resolve(catalog,source,out _,out error))return false;
            var component=FreeShapeTopology.Component(source.freeShape,selected);
            if(component.Count==0 || (!delete && delta==Vector2Int.zero))return Fail("자유형 덩어리를 선택하고 이동하세요.",out error);
            var edited=CloneMap(source);var moving=edited.freeShape.cells.Where(c=>component.Contains(c.Position)).ToArray();
            edited.freeShape.cells.RemoveAll(c=>component.Contains(c.Position));
            if(!delete)foreach(var c in moving){long x=(long)c.x+delta.x,y=(long)c.y+delta.y;if(x<int.MinValue||x>int.MaxValue||y<int.MinValue||y>int.MaxValue)return Fail("Coordinate overflow.",out error);c.x=(int)x;c.y=(int)y;edited.freeShape.cells.Add(c);}
            edited.freeShape.cells=edited.freeShape.cells.OrderBy(c=>c.y).ThenBy(c=>c.x).ToList();
            return CompleteTransaction(catalog,source,edited,null,out candidate,out error);
        }
    }
}
