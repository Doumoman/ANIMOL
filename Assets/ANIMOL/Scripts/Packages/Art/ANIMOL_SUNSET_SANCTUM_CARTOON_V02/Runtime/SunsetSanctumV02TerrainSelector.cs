using System;
using System.Collections.Generic;

namespace ANIMOL.Art.SunsetSanctumV02
{
    // Pure selector. Query the FULL authored board, not a camera rectangle or isolated chunk.
    // All three playable materials connect geometrically; material changes do not add a rim.
    public static class SunsetSanctumV02TerrainSelector
    {
        public const int N=1,NE=2,E=4,SE=8,S=16,SW=32,W=64,NW=128;
        public enum Material { SanctumFloor, WoodBridge, StoneBridge }
        public static string MaterialName(Material material)
        {
            switch(material) { case Material.SanctumFloor:return "sanctum_floor";
                case Material.WoodBridge:return "wood_bridge";
                case Material.StoneBridge:return "stone_bridge";
                default:throw new ArgumentOutOfRangeException("material"); }
        }
        public static int NeighborMask(Func<int,int,bool> anyPlayable,int x,int y,bool yDown=false)
        {
            if(anyPlayable==null)throw new ArgumentNullException("anyPlayable");
            int up=yDown?-1:1,down=-up;
            return (anyPlayable(x,y+up)?N:0)|(anyPlayable(x+1,y+up)?NE:0)
                |(anyPlayable(x+1,y)?E:0)|(anyPlayable(x+1,y+down)?SE:0)
                |(anyPlayable(x,y+down)?S:0)|(anyPlayable(x-1,y+down)?SW:0)
                |(anyPlayable(x-1,y)?W:0)|(anyPlayable(x-1,y+up)?NW:0);
        }
        public static string File(Material material,int neighborMask)
        {
            if(neighborMask<0 || neighborMask>255)throw new ArgumentOutOfRangeException("neighborMask");
            return "tiles/"+MaterialName(material)+"/full256/n"+neighborMask.ToString("D3")+".png";
        }
        public static uint StableHash(int seed,int worldX,int worldY)
        {
            unchecked { uint h=(uint)seed^((uint)worldX*0x9e3779b9u)^((uint)worldY*0x85ebca6bu);
                h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;return h^(h>>16); }
        }
        // Deterministic continuous lengths; a 2/3/4-cell edge is always ONE fused body.
        // Long edges vary the native span grouping without leaving a one-cell tail.
        // Does not spawn sprites or infer a gameplay footprint from their alpha.
        public static int[] PlanContinuousSpanLengths(int runLength,int seed,int worldX,int worldY)
        {
            if(runLength<1)throw new ArgumentOutOfRangeException("runLength");
            if(runLength<=4)return new[]{runLength};
            uint state=StableHash(seed,worldX,worldY);var parts=new List<int>();int remaining=runLength;
            while(remaining>0) {
                var choices=new List<int>();
                for(int n=2;n<=4;n++)if(n<=remaining && (remaining-n==0 || remaining-n>=2))choices.Add(n);
                unchecked { state+=0x9e3779b9u;state^=state>>16;state*=0x7feb352du; }
                int length=choices[(int)(state%(uint)choices.Count)];parts.Add(length);remaining-=length;
            }
            return parts.ToArray();
        }
    }
}
