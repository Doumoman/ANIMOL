using System;
using System.Collections.Generic;

namespace ANIMOL.Art.SunsetSanctumV02
{
    public static class SunsetSanctumV02UndersideSelector
    {
        public struct SouthRun
        {
            public int x,y,length;public string support;public bool enclosedHole;
        }
        public struct Placement
        {
            public float logicalCenterX,logicalCenterY;
            public int spanCells;public bool enclosedHole;public string file;
        }
        private static bool Solid(char c) { return c=='G'||c=='W'||c=='S'; }
        private static string Support(char c) { return c=='W'?"wood":Solid(c)?"masonry":null; }
        // min/max enclose the WHOLE authored board. Camera/chunk bounds create false end caps.
        // Host coordinates here are +Y North; adapt a row-down map before calling.
        public static List<SouthRun> SelectRuns(Func<int,int,char> materialAt,int minX,int minY,int maxX,int maxY)
        {
            if(materialAt==null)throw new ArgumentNullException("materialAt");
            if(minX>maxX || minY>maxY)throw new ArgumentException("Use the full authored board bounds.");
            Func<int,int,char> at=(x,y)=>x<minX||x>maxX||y<minY||y>maxY?'0':materialAt(x,y);
            int width=checked(maxX-minX+3),height=checked(maxY-minY+3);
            var exterior=new bool[width,height];var queue=new Queue<int>();
            exterior[0,0]=true;queue.Enqueue(0);
            int[] dx={0,1,0,-1},dy={1,0,-1,0};
            while(queue.Count>0)
            {
                int p=queue.Dequeue(),x=p%width,y=p/width;
                for(int d=0;d<4;d++) {
                    int a=x+dx[d],b=y+dy[d];
                    if(a<0||b<0||a>=width||b>=height||exterior[a,b]||Solid(at(a+minX-1,b+minY-1)))continue;
                    exterior[a,b]=true;queue.Enqueue(b*width+a);
                }
            }
            var result=new List<SouthRun>();
            for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;)
            {
                if(!Solid(at(x,y)) || Solid(at(x,y-1))) { x++;continue; }
                int start=x;string support=Support(at(x,y));bool hole=!exterior[x-minX+1,y-minY];
                while(x<=maxX && Solid(at(x,y)) && !Solid(at(x,y-1)) && Support(at(x,y))==support
                    && (!exterior[x-minX+1,y-minY])==hole)x++;
                result.Add(new SouthRun{x=start,y=y,length=x-start,support=support,enclosedHole=hole});
            }
            return result;
        }
        public static List<Placement> Plan(SouthRun run,int seed)
        {
            if(run.length<1 || (run.support!="wood" && run.support!="masonry"))throw new ArgumentException("Invalid actual exposed run.");
            int[] lengths=SunsetSanctumV02TerrainSelector.PlanContinuousSpanLengths(run.length,seed,run.x,run.y);
            var result=new List<Placement>();int offset=0;
            foreach(int n in lengths) {
                result.Add(new Placement{logicalCenterX=run.x+offset+n*.5f,logicalCenterY=run.y+.5f,
                    spanCells=n,enclosedHole=run.enclosedHole,file="undersides/"+run.support+"/"+(run.enclosedHole?"cap_":"span_")+n.ToString("D2")+".png"});
                offset+=n;
            }
            return result;
        }
    }
}
