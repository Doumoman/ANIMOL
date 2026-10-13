using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ANIMOL.Presentation
{
    // Only renders cells supplied by an authoritative engine; never infers range or terrain.
    public sealed class ResolvedCellPreview : MonoBehaviour
    {
        public BeamPathView boundary,targets,endpoint;
        public Sprite[] boundarySprites;
        public Sprite targetSprite;
        public Sprite[] stopSprites;
        static readonly Vector2Int[] Directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        public static int ExteriorMask(Vector2Int cell,ISet<Vector2Int> resolvedCells)
        {int mask=0;for(int i=0;i<4;i++)if(!resolvedCells.Contains(cell+Directions[i]))mask|=1<<i;return mask;}
        public static int ShoreMask(Vector2Int cell,ISet<Vector2Int> waterOrOutside)
        {int mask=0;for(int i=0;i<4;i++)if(waterOrOutside.Contains(cell+Directions[i]))mask|=1<<i;return mask;}
        public void PresentBlast(IEnumerable<Vector2Int> resolved,IEnumerable<Vector2Int> crackedWalls)
        {
            var cells=new HashSet<Vector2Int>(resolved);
            boundary.sprites=boundarySprites.Select((s,i)=>new BeamVisualSprite{shape=i.ToString(),sprite=s}).ToArray();
            boundary.Present(cells.Select(c=>new BeamVisualCell{shape=ExteriorMask(c,cells).ToString(),position=c}).ToArray());
            targets.sprites=new[]{new BeamVisualSprite{shape="target",sprite=targetSprite}};
            targets.Present(crackedWalls.Distinct().Where(c=>cells.Contains(c)).Select(c=>new BeamVisualCell{shape="target",position=c}).ToArray());
        }
        public void PresentStop(Vector2Int? actualEnd,string direction)
        {
            if(!actualEnd.HasValue){endpoint.Clear();return;}
            int i=direction=="e"?0:direction=="s"?1:direction=="w"?2:3;
            endpoint.sprites=new[]{new BeamVisualSprite{shape="stop",sprite=stopSprites[i]}};
            endpoint.Present(new[]{new BeamVisualCell{shape="stop",position=actualEnd.Value}});
        }
        public void Clear(){if(boundary)boundary.Clear();if(targets)targets.Clear();if(endpoint)endpoint.Clear();}
        void OnDisable()=>Clear();
    }
}
