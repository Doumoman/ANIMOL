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
        private double stageEpoch;
        private string presentationInput;
        private Dictionary<Vector2Int,GraphicCell> presentedCells;
        public StageCrumbleVfxPool CrumblePool {get;private set;}
        private void Collapse(CommonObstacleObject owner,int sequence,double seconds)
        {
            if(!Application.isPlaying || map==null)return;
            EnsurePool();CrumblePool.TryPlay(owner.GetInstanceID(),owner.ResetGeneration,sequence,map.ThemeId,owner.transform.position,seconds);
        }
        private void ResetOwner(CommonObstacleObject owner)=>CrumblePool?.ClearOwner(owner.GetInstanceID());
        private void EnsurePool()
        {
            if(CrumblePool!=null)return;
            var root=new GameObject("Stage C05 VFX");root.transform.SetParent(transform,false);root.layer=gameObject.layer;CrumblePool=root.AddComponent<StageCrumbleVfxPool>();
        }
        public void ClearPresentation()
        {
            foreach(var item in owners)if(item.owner is CommonObstacleObject d){d.Collapsed-=Collapse;d.RuntimeReset-=ResetOwner;}
            CrumblePool?.ClearStage();presentationInput=null;presentedCells=null;owners.Clear();Restore();lastInput=null;signature=null;failedInput=null;
        }
        public void ResetStagePresentation(){CrumblePool?.ClearStage();stageEpoch=Time.timeAsDouble;lastInput=null;signature=null;Refresh();}
        public void Configure(StageMapDefinition definition, Transform objectRoot, FreeShapeTerrainRenderer renderer)
        {
            ClearPresentation(); map=definition; art=renderer;stageEpoch=Time.timeAsDouble;
            foreach(var owner in objectRoot.GetComponentsInChildren<StageMapRuntimeObject>())
            {
                var p=map.Objects.FirstOrDefault(o=>o.StableId==owner.StableId);
                if(p==null || ObstacleSnapshotAdapter.Kind(p.Kind)==null) continue;
                owners.Add((p,owner));
                if(owner is CommonObstacleObject d){d.Collapsed+=Collapse;d.RuntimeReset+=ResetOwner;}
            }
            Refresh();
        }
        private void LateUpdate() { CrumblePool?.Sample(Time.timeAsDouble);if(map!=null && art!=null) Refresh(); }
        public void Refresh()
        {
            var cells=new Dictionary<Vector2Int,GraphicCell>();
            var stamps=new List<string>();
            double gameSeconds=Time.timeAsDouble;
            var stateInput=map.AuthoringRevision+";"+string.Join(";",owners.Select(i=>i.owner==null?"deleted":i.owner.transform.position+"|"+i.owner.gameObject.activeInHierarchy+"|"+(i.owner is CommonObstacleObject d?d.Pose+"|"+d.ResetGeneration:"idle")+"|"+i.owner.GetComponent<BoxCollider2D>()?.enabled));
            var input=map.AuthoringRevision+";"+string.Join(";",owners.Select(i=>i.owner==null?"deleted":i.owner.transform.position+"|"+i.owner.gameObject.activeInHierarchy+"|"+(i.owner is CommonObstacleObject d?d.Pose+"|"+d.FrameAt(gameSeconds,stageEpoch)+"|"+d.ResetGeneration:"idle")+"|"+i.owner.GetComponent<BoxCollider2D>()?.enabled));
            if(failedInput==input || lastInput==input)return;
            if(presentationInput==stateInput && presentedCells!=null)
            {
                foreach(var item in owners)if(item.owner is CommonObstacleObject device && item.owner.gameObject.activeInHierarchy)
                {
                    var position=Vector2Int.RoundToInt((Vector2)item.owner.transform.position/map.WorldUnitsPerCell);
                    if(presentedCells.TryGetValue(position,out var cell))cell.Frame=device.FrameAt(gameSeconds,stageEpoch);
                }
                art.UpdateAnimationFrames(presentedCells);lastInput=input;return;
            }
            var accepted=new List<StageMapRuntimeObject>();var issues=new List<string>();
            try
            {
                var context=ObstacleSnapshotAdapter.Merge(map.FreeShapeTerrain,null);
                foreach(var item in owners)
                {
                    if(item.owner==null || !item.owner.gameObject.activeInHierarchy) continue;
                    var position=(Vector2)item.owner.transform.position/map.WorldUnitsPerCell;
                    var cell=Vector2Int.RoundToInt(position);
                    if((position-cell).sqrMagnitude>.00001f) throw new InvalidOperationException("Obstacle is not aligned to global cells: "+item.placement.StableId);
                    try
                    {
                        var snap=ObstacleSnapshotAdapter.Read(map,item.placement,item.owner,gameSeconds,stageEpoch);
                        if(cells.ContainsKey(cell))throw new InvalidOperationException("Duplicate device owner at "+cell);
                        if(!context.TryAdd(cell,snap))throw new InvalidOperationException("Terrain/device duplicate owner at "+cell);
                        try
                        {
                            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
                            {
                                var p=cell+new Vector2Int(x,y);if(!context.TryGetValue(p,out var neighbour)||neighbour.Kind=="terrain")continue;
                                var plan=ObstacleVisualResolver.Resolve((a,b)=>context.GetValueOrDefault(new Vector2Int(a,b)),p.x,p.y,map.FreeShapeTerrain.seed);
                                ObstacleArtRegistry.Load().Sprite(plan.OverlayKey);
                                if(plan.Problems.Count>0)throw new InvalidOperationException(string.Join("; ",plan.Problems));
                            }
                        }
                        catch{context.Remove(cell);throw;}
                        cells.Add(cell,snap);accepted.Add(item.owner);
                        stamps.Add(cell+"|"+snap.Kind+"|"+snap.StyleId+"|"+snap.Pose+"|"+snap.SurfaceEnabled+"|"+snap.Frame+"|"+snap.HazardEnabled);
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
                signature=next;failedInput=null;presentationInput=stateInput;presentedCells=cells;
            }
            catch(Exception ex)
            {
                failedInput=input;Restore(); art.Rebuild(map.FreeShapeTerrain,map.WorldUnitsPerCell);
                if(signature!="ERROR:"+ex.Message)Debug.LogError("ANIMOL obstacle graphics: "+ex.Message,this);
                signature="ERROR:"+ex.Message;
            }
        }
        private void Restore(){foreach(var p in originalVisibility)if(p.Key!=null)p.Key.forceRenderingOff=p.Value;originalVisibility.Clear();}
        private void OnDestroy()=>ClearPresentation();
        private void OnDisable()=>CrumblePool?.ClearStage();
    }
}
