using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Development
{
    // uGUI geometry over the actual RawImage viewport. It never creates physics objects.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TerrainEditorGrid : MaskableGraphic
    {
        public TerrainEditorScreen screen;
        private Vector2 Local(Vector2 logical)
        {
            var point=screen.MapCamera.WorldToViewportPoint(logical*screen.Adapter.Map.GetEditorPreviewUnitsPerCell());
            var rect=rectTransform.rect;return new Vector2(rect.xMin+point.x*rect.width,rect.yMin+point.y*rect.height);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(screen==null || screen.Adapter==null || screen.IsTesting)return;
            if(screen.State.grid)
            {
                var units=screen.Adapter.Map.GetEditorPreviewUnitsPerCell();
                var min=(Vector2)screen.MapCamera.ViewportToWorldPoint(new Vector3(0,0,50))/units;
                var max=(Vector2)screen.MapCamera.ViewportToWorldPoint(new Vector3(1,1,50))/units;
                for(int x=Mathf.FloorToInt(min.x);x<=Mathf.CeilToInt(max.x);x++)
                    Line(vh,Local(new Vector2(x,min.y)),Local(new Vector2(x,max.y)),TerrainEditorScreen.Panel,.5f);
                for(int y=Mathf.FloorToInt(min.y);y<=Mathf.CeilToInt(max.y);y++)
                    Line(vh,Local(new Vector2(min.x,y)),Local(new Vector2(max.x,y)),TerrainEditorScreen.Panel,.5f);
            }
            if(screen.SelectedBounds.HasValue)Box(vh,screen.SelectedBounds.Value,TerrainEditorScreen.Gold,2,false);
            if(!screen.GhostVisible)return;
            var entry=screen.GhostPart?.terrain;var origin=screen.GhostOrigin;
            if(entry!=null && screen.State.masks)
            {
                for(int row=0;row<entry.height;row++)for(int x=0;x<entry.width;x++)
                {
                    bool solid=entry.solidRows[row][x]=='#',support=entry.supportRows[row][x]=='#';
                    var c=!screen.GhostValid?TerrainEditorScreen.Red:support?TerrainEditorScreen.Blue:solid?TerrainEditorScreen.Green:TerrainEditorScreen.Paper;
                    Box(vh,new Rect(origin.x+x,origin.y+entry.height-1-row,1,1),c,1,!solid&&!support);
                }
            }
            else Box(vh,new Rect(origin,screen.GhostPart?.size??Vector2.one),screen.GhostValid?TerrainEditorScreen.Green:TerrainEditorScreen.Red,2,false);
            var p=Local(origin);Line(vh,p-new Vector2(6,0),p+new Vector2(6,0),TerrainEditorScreen.Gold,3);Line(vh,p-new Vector2(0,6),p+new Vector2(0,6),TerrainEditorScreen.Gold,3);
        }
        private void Box(VertexHelper vh,Rect r,Color c,float width,bool dashed)
        {
            var a=Local(r.min);var b=Local(new Vector2(r.xMax,r.yMin));var d=Local(r.max);var e=Local(new Vector2(r.xMin,r.yMax));
            Edge(vh,a,b,c,width,dashed);Edge(vh,b,d,c,width,dashed);Edge(vh,d,e,c,width,dashed);Edge(vh,e,a,c,width,dashed);
        }
        private static void Edge(VertexHelper vh,Vector2 a,Vector2 b,Color c,float width,bool dashed)
        {
            if(!dashed){Line(vh,a,b,c,width);return;}
            var length=Vector2.Distance(a,b);for(float t=0;t<length;t+=8)Line(vh,Vector2.Lerp(a,b,t/length),Vector2.Lerp(a,b,Mathf.Min(length,t+4)/length),c,width);
        }
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,Color c,float width)
        {
            var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int start=vh.currentVertCount;
            vh.AddVert(a-normal,c,Vector2.zero);vh.AddVert(a+normal,c,Vector2.zero);vh.AddVert(b+normal,c,Vector2.zero);vh.AddVert(b-normal,c,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
