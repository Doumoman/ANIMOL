using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay.ObstacleGraphics
{
    // LateUpdate observes completed physics state. It never drives device state, timers or collision.
    public sealed class ObstacleRuntimeGraphics : MonoBehaviour
    {
        private StageMapDefinition map;
        private FreeShapeTerrainRenderer art;
        private readonly List<(StageMapObjectPlacement placement,StageMapRuntimeObject owner)> owners=new();
        private readonly Dictionary<SpriteRenderer,bool> originalVisibility=new();
        private string signature, failedInput, lastInput;
        public string LastIssues { get; private set; }
        public void Configure(StageMapDefinition definition, Transform objectRoot, FreeShapeTerrainRenderer renderer)
        {
            Restore(); owners.Clear(); map=definition; art=renderer; signature=null; failedInput=null; lastInput=null;
            foreach(var owner in objectRoot.GetComponentsInChildren<StageMapRuntimeObject>())
            {
                var p=map.Objects.FirstOrDefault(o=>o.StableId==owner.StableId);
                if(p==null || ObstacleSnapshotAdapter.Kind(p.Kind)==null) continue;
                owners.Add((p,owner));
            }
            Refresh();
        }
        private void LateUpdate() { if(map!=null && art!=null) Refresh(); }
        public void Refresh()
        {
            var cells=new Dictionary<Vector2Int,GraphicCell>();
            var stamps=new List<string>();
            var input=map.AuthoringRevision+";"+string.Join(";",owners.Select(i=>i.owner==null?"deleted":i.owner.transform.position+"|"+i.owner.gameObject.activeInHierarchy+"|"+(i.owner is CommonObstacleObject d?d.Pose:"idle")+"|"+i.owner.GetComponent<BoxCollider2D>()?.enabled));
            if(failedInput==input || lastInput==input)return;
            var accepted=new List<StageMapRuntimeObject>();var issues=new List<string>();
            try
            {
                foreach(var item in owners)
                {
                    if(item.owner==null || !item.owner.gameObject.activeInHierarchy) continue;
                    var position=(Vector2)item.owner.transform.position/map.WorldUnitsPerCell;
                    var cell=Vector2Int.RoundToInt(position);
                    if((position-cell).sqrMagnitude>.00001f) throw new InvalidOperationException("Obstacle is not aligned to global cells: "+item.placement.StableId);
                    try
                    {
                        var snap=ObstacleSnapshotAdapter.Read(map,item.placement,item.owner);
                        if(cells.ContainsKey(cell))throw new InvalidOperationException("Duplicate device owner at "+cell);
                        var candidate=new Dictionary<Vector2Int,GraphicCell>(cells){{cell,snap}};
                        ObstacleSnapshotAdapter.Validate(map.FreeShapeTerrain,candidate);
                        cells=candidate;accepted.Add(item.owner);
                        stamps.Add(cell+"|"+snap.Kind+"|"+snap.StyleId+"|"+snap.Pose+"|"+snap.SurfaceEnabled);
                    }
                    catch(InvalidOperationException problem){issues.Add(item.placement.StableId+": "+problem.Message);}
                }
                var issueText=string.Join("; ",issues);
                if(issueText!=LastIssues && issueText.Length>0)Debug.LogError("ANIMOL obstacle graphics: "+issueText,this);
                LastIssues=issueText;lastInput=input;
                var next=map.AuthoringRevision+";"+string.Join(";",stamps);
                if(signature==next)return;
                ObstacleSnapshotAdapter.Validate(map.FreeShapeTerrain,cells);
                art.Rebuild(map.FreeShapeTerrain,map.WorldUnitsPerCell,cells);
                Restore();
                foreach(var owner in accepted) foreach(var r in owner.GetComponentsInChildren<SpriteRenderer>(true))
                {if(!originalVisibility.ContainsKey(r))originalVisibility.Add(r,r.forceRenderingOff);r.forceRenderingOff=true;}
                signature=next;failedInput=null;
            }
            catch(Exception ex)
            {
                failedInput=input;Restore(); art.Rebuild(map.FreeShapeTerrain,map.WorldUnitsPerCell);
                if(signature!="ERROR:"+ex.Message)Debug.LogError("ANIMOL obstacle graphics: "+ex.Message,this);
                signature="ERROR:"+ex.Message;
            }
        }
        private void Restore(){foreach(var p in originalVisibility)if(p.Key!=null)p.Key.forceRenderingOff=p.Value;originalVisibility.Clear();}
        private void OnDestroy()=>Restore();
    }
}
