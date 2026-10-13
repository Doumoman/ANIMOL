using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Engine=Animol.TerrainStructure.AnimolTerrainPlacementEngine;

namespace ANIMOL.Development
{
    public sealed partial class TerrainEditorScreen
    {
        public bool IsTesting { get; private set; }
        public Vector2Int GhostOrigin { get; private set; }
        public TerrainEditorPart GhostPart { get; private set; }
        public bool GhostValid { get; private set; }
        public bool GhostVisible { get; private set; }
        public Rect? SelectedBounds { get; private set; }
        private GameObject ghostArt;
        private readonly List<Material> ghostMaterials=new List<Material>();
        private string ghostArtId, candidateKey, candidateError;
        private AnimolTerrainSavedMap pendingTerrain;
        private TerrainEditorObjectEdit pendingObject;
        private TerrainEditorGrid overlay;
        private bool pointerHeld, panning, moving, wasDragged;
        private Vector2 pointerStart, panStart;
        private Vector2Int cellStart, instanceStart, lastPickCell;
        private int pickIndex;
        private Vector2Int? pairAnchor;
        private readonly List<string> pickIds=new List<string>();
        private RectTransform toolPanel,viewTools,infoPanel;
        private Text infoText,toolLabel;
        private InputField xInput,yInput;

        public void InitializeTools()
        {
            var input=Viewport.gameObject.AddComponent<TerrainEditorMapInput>();input.screen=this;
            var gridGo=new GameObject("Logical cell overlay",typeof(RectTransform),typeof(TerrainEditorGrid));gridGo.transform.SetParent(Viewport.transform,false);
            Stretch(gridGo.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
            overlay=gridGo.GetComponent<TerrainEditorGrid>();overlay.screen=this;overlay.raycastTarget=false;
            toolPanel=PanelRect("Map tools",Viewport.transform,Panel,new Vector2(.008f,.56f),new Vector2(.12f,.97f));
            var tools=new[]{("Place","배치"),("Select","선택 / 이동"),("Delete","전체 삭제"),("Brush","1셀 칠하기"),("Erase","1셀 지우기")};
            for(int i=0;i<tools.Length;i++)
            {
                var tool=tools[i];var button=Button(tool.Item1,toolPanel,tool.Item2,.04f,.96f,()=>SetTool(tool.Item1));
                Stretch(button.GetComponent<RectTransform>(),new Vector2(.04f,1-(i+1)*.16f),new Vector2(.96f,.98f-i*.16f));
            }
            toolLabel=Label("Current tool",toolPanel,"",13);Stretch(toolLabel.rectTransform,new Vector2(.04f,0),new Vector2(.96f,.17f));
            viewTools=PanelRect("View tools",Viewport.transform,Panel,new Vector2(.65f,.9f),new Vector2(.995f,.99f));
            Button("Zoom in",viewTools,"+",0,.1f,()=>Zoom(.8f));Button("Zoom out",viewTools,"−",.11f,.21f,()=>Zoom(1.25f));
            Button("Grid",viewTools,"격자",.22f,.42f,()=>{State.grid=!State.grid;overlay.SetVerticesDirty();});
            Button("Masks",viewTools,"마스크",.43f,.64f,()=>{State.masks=!State.masks;overlay.SetVerticesDirty();});
            Button("Info",viewTools,"정보",.65f,.83f,()=>{State.information=!State.information;RefreshInformation();});
            Button("Pan",viewTools,"이동",.84f,1,()=>SetTool("Pan"));
            infoPanel=PanelRect("Selection information",Viewport.transform,Panel,new Vector2(.7f,.03f),new Vector2(.99f,.87f));
            infoText=Label("Selection details",infoPanel,"",16);Stretch(infoText.rectTransform,new Vector2(.04f,.68f),new Vector2(.96f,.98f));infoText.alignment=TextAnchor.UpperLeft;
            xInput=Input("X",infoPanel,"X",new Vector2(.04f,.57f),new Vector2(.32f,.66f));
            yInput=Input("Y",infoPanel,"Y",new Vector2(.35f,.57f),new Vector2(.63f,.66f));
            var apply=Button("Apply coordinates",infoPanel,"적용",.67f,.96f,ApplyCoordinates);Stretch(apply.GetComponent<RectTransform>(),new Vector2(.67f,.57f),new Vector2(.96f,.66f));
            var remove=Button("Delete instance",infoPanel,"선택 전체 삭제",.04f,.96f,()=>Run(DeleteSelected));Stretch(remove.GetComponent<RectTransform>(),new Vector2(.04f,.46f),new Vector2(.96f,.55f));
            InitializeSettings();
            RefreshInformation();
        }
        public void SetTool(string tool){CancelPending(false);State.tool=tool;RefreshInformation();}
        partial void PartSelected(){CancelPending(false);RefreshInformation();}
        partial void Refreshed()
        {
            candidateKey=null;pendingTerrain=null;pendingObject=null;
            if(!string.IsNullOrEmpty(State.instanceId) && !Adapter.Read().placements.Any(p=>p.instanceId==State.instanceId) && !Adapter.Map.Objects.Any(o=>o.StableId==State.instanceId))State.instanceId="";
            RefreshInformation();if(overlay!=null)overlay.SetVerticesDirty();
            SetStatus("저장된 정본 반영 · revision "+Adapter.Map.AuthoringRevision);
        }
        partial void UpdateTools()
        {
            if(overlay!=null)overlay.SetVerticesDirty();
            if(IsTesting || TextEditing())return;
            if(UnityEngine.Input.GetKeyDown(KeyCode.Escape))CancelPending();
            if(UnityEngine.Input.GetKeyDown(KeyCode.Delete))Run(DeleteSelected);
        }
        public bool TextEditing()=>EventSystem.current?.currentSelectedGameObject?.GetComponentInParent<InputField>()?.isFocused==true;
        private void OnApplicationFocus(bool focus){if(!focus)CancelPending(false);}
        public void CancelPending(bool selectTool=true)
        {
            pointerHeld=false;moving=false;panning=false;wasDragged=false;pendingTerrain=null;pendingObject=null;candidateKey=null;
            pairAnchor=null;
            GhostVisible=false;if(ghostArt!=null)ghostArt.SetActive(false);
            if(selectTool)State.tool="Select";
            if(overlay!=null)overlay.SetVerticesDirty();SetStatus("후보 취소 · 정본 변경 없음");
        }
        public bool IsMapPoint(Vector2 point)
        {
            if(!RectTransformUtility.RectangleContainsScreenPoint(Viewport.rectTransform,point,null))return false;
            var results=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},results);
            return results.Count>0 && results[0].gameObject==Viewport.gameObject;
        }
        public void PointerDown(PointerEventData data)
        {
            if(IsTesting || TextEditing())return;
            if(data.button==PointerEventData.InputButton.Right){CancelPending();return;}
            if(!IsMapPoint(data.position))return;
            pointerHeld=true;pointerStart=data.position;cellStart=ScreenToCell(data.position);wasDragged=false;
            panning=data.button==PointerEventData.InputButton.Middle || UnityEngine.Input.GetKey(KeyCode.Space) || State.tool=="Pan";
            panStart=State.center;if(panning){GhostVisible=false;return;}
            if(State.tool=="Select" || State.tool=="Delete")
            {
                Pick(cellStart);moving=State.tool=="Select" && !string.IsNullOrEmpty(State.instanceId);
                instanceStart=SelectionOrigin();return;
            }
            UpdateCandidate(cellStart,false);
        }
        public void PointerMove(PointerEventData data)
        {
            if(IsTesting || TextEditing())return;
            if(pointerHeld && panning)
            {
                var pixels=Viewport.rectTransform.rect.size*CanvasRect.GetComponent<Canvas>().scaleFactor;
                var delta=data.position-pointerStart;
                State.center=panStart-new Vector2(delta.x/pixels.x*2*State.zoom*MapCamera.aspect,delta.y/pixels.y*2*State.zoom);
                candidateKey=null;return;
            }
            if(!IsMapPoint(data.position)){if(!pointerHeld){GhostVisible=false;if(ghostArt!=null)ghostArt.SetActive(false);}return;}
            var cell=ScreenToCell(data.position);
            if(pointerHeld && moving){wasDragged|=(data.position-pointerStart).sqrMagnitude>16;UpdateCandidate(instanceStart+cell-cellStart,true);}
            else if(State.tool=="Place" || State.tool=="Brush" || State.tool=="Erase")UpdateCandidate(cell,false);
        }
        public void PointerUp(PointerEventData data)
        {
            if(!pointerHeld)return;
            var pan=panning;pointerHeld=false;panning=false;
            if(IsTesting || pan || !IsMapPoint(data.position)){CancelPending(false);return;}
            if(State.tool=="Delete"){Run(DeleteSelected);return;}
            if(State.tool=="Select" && !wasDragged){moving=false;return;}
            if(moving)UpdateCandidate(instanceStart+ScreenToCell(data.position)-cellStart,true);
            else UpdateCandidate(ScreenToCell(data.position),false);
            moving=false;
            if(!GhostValid){var reason=candidateError;CancelPending(false);SetStatus("배치 취소 · "+reason);return;}
            Run(CommitPending);
        }
        public void Zoom(float factor)
        {if(IsTesting)return;State.zoom=Mathf.Clamp(State.zoom*factor,2,80);candidateKey=null;}
        public void UpdateCandidate(Vector2Int origin,bool move)
        {
            if(IsTesting)return;
            TerrainEditorPart part=move?SelectionPart():SelectedPart;
            var key=Adapter.Map.AuthoringRevision+":"+origin+":"+State.tool+":"+part?.id+":"+State.instanceId+":"+move;
            if(candidateKey==key)return;candidateKey=key;
            pendingTerrain=null;pendingObject=null;candidateError=null;GhostOrigin=origin;GhostPart=part;GhostVisible=true;GhostValid=false;
            try
            {
                var dto=Adapter.Read();var catalog=Adapter.Terrain.Catalog;
                if(State.tool=="Brush" || State.tool=="Erase")
                {
                    GhostPart=null;
                    var style=part?.terrain?.styleId??Adapter.Map.ThemeId+"_A";
                    if(!Engine.TrySetBaseCell(catalog,dto,origin,State.tool=="Brush",style,out pendingTerrain,out candidateError))throw new InvalidOperationException(candidateError);
                }
                else if(part?.terrain!=null)
                {
                    var ok=move?Engine.TryMove(catalog,dto,State.instanceId,origin,out pendingTerrain,out candidateError):Engine.TryPlace(catalog,dto,part.id,origin,out pendingTerrain,out candidateError);
                    if(!ok)throw new InvalidOperationException(candidateError);
                }
                else if(part?.obj!=null || part?.marker!=null)
                {
                    pendingObject=new TerrainEditorObjectEdit{operation=move?"Move":"Place",definitionId=part.id,instanceId=move?State.instanceId:part.id+"-"+Guid.NewGuid().ToString("N"),origin=origin};
                    if(!move && part.obj!=null && part.obj.RequiresLinkedPair)
                    {pendingObject.operation=pairAnchor.HasValue?"Pair":"PairAnchor";pendingObject.second=origin;if(pairAnchor.HasValue)pendingObject.origin=pairAnchor.Value;}
                    Adapter.ValidateObject(pendingObject);
                }
                else throw new InvalidOperationException("하단 팔레트에서 부품을 선택하세요.");
                if(pendingTerrain!=null)Adapter.Validate(pendingTerrain);
                GhostValid=true;
                SetStatus($"({origin.x}, {origin.y}) · 1셀 스냅 · "+(move?"놓아서 전체 이동":"클릭하여 배치")+" · "+Describe(part));
            }
            catch(Exception ex){candidateError=ex.Message;SetStatus($"({origin.x}, {origin.y}) · "+candidateError);}
            ShowGhost();
        }
        public void CommitPending()
        {
            if(IsTesting)throw new InvalidOperationException("Test에서는 편집할 수 없습니다.");
            if(!GhostValid)throw new InvalidOperationException(candidateError??"유효한 후보가 없습니다.");
            if(pendingTerrain!=null)
            {
                var added=pendingTerrain.placements.LastOrDefault(p=>!Adapter.Read().placements.Any(old=>old.instanceId==p.instanceId));
                var candidate=pendingTerrain;Adapter.Validate(candidate);Adapter.Commit(candidate,"ANIMOL Terrain "+State.tool);
                if(added!=null)State.instanceId=added.instanceId;
            }
            else if(pendingObject!=null)
            {
                var edit=pendingObject;
                if(edit.operation=="PairAnchor"){pairAnchor=edit.origin;candidateKey=null;SetStatus("첫 위치 고정 · 두 번째 연결 객체 위치를 클릭하세요 · Esc 취소");return;}
                Adapter.CommitObject(edit);State.instanceId=edit.instanceId;pairAnchor=null;
            }
            else throw new InvalidOperationException("변경 후보가 없습니다.");
            pendingTerrain=null;pendingObject=null;candidateKey=null;GhostVisible=false;if(ghostArt!=null)ghostArt.SetActive(false);
            RefreshInformation();SetStatus("저장 완료 · revision "+Adapter.Map.AuthoringRevision);
        }
        public void DeleteSelected()
        {
            if(IsTesting || TextEditing())return;
            if(string.IsNullOrEmpty(State.instanceId))throw new InvalidOperationException("부품 전체를 선택해 삭제하세요.");
            var dto=Adapter.Read();
            if(dto.placements.Any(p=>p.instanceId==State.instanceId))
            {if(!Engine.TryDelete(Adapter.Terrain.Catalog,dto,State.instanceId,out var candidate,out var error))throw new InvalidOperationException(error);Adapter.Commit(candidate,"Delete complete terrain instance");}
            else Adapter.CommitObject(new TerrainEditorObjectEdit{operation="Delete",instanceId=State.instanceId});
            State.instanceId="";CancelPending(false);RefreshInformation();SetStatus("전체 삭제 저장 완료");
        }
        public void Pick(Vector2Int cell)
        {
            var placements=Adapter.Read().placements;
            var ids=placements.Where(p=>{var e=Parts.First(a=>a.id==p.catalogId).terrain;return new RectInt(p.x,p.y,e.width,e.height).Contains(cell);})
                .OrderBy(p=>Parts.First(a=>a.id==p.catalogId).IsOverlay?0:1).Select(p=>p.instanceId)
                .Concat(Adapter.Map.Objects.Where(o=>StageMapDefinition.EnumeratePlacementCells(o).Contains(cell)).Select(o=>o.StableId)).ToList();
            pickIndex=cell==lastPickCell && pickIds.SequenceEqual(ids)?(pickIndex+1)%Mathf.Max(1,ids.Count):0;
            lastPickCell=cell;pickIds.Clear();pickIds.AddRange(ids);
            State.instanceId=ids.Count==0?"":ids[pickIndex];State.information=ids.Count>0;
            RefreshInformation();SetStatus(ids.Count==0?"선택 가능한 부품 없음":$"선택 {pickIndex+1}/{ids.Count} · 같은 셀 클릭으로 순환 · "+State.instanceId);
        }
        public Vector2Int SelectionOrigin()
        {
            var p=Adapter.Read().placements.FirstOrDefault(p=>p.instanceId==State.instanceId);if(p!=null)return new Vector2Int(p.x,p.y);
            var o=Adapter.Map.Objects.FirstOrDefault(o=>o.StableId==State.instanceId);return o==null?Vector2Int.zero:new Vector2Int(o.X,o.Y);
        }
        public TerrainEditorPart SelectionPart()
        {
            var p=Adapter.Read().placements.FirstOrDefault(p=>p.instanceId==State.instanceId);
            if(p!=null)return Parts.FirstOrDefault(a=>a.id==p.catalogId);
            var o=Adapter.Map.Objects.FirstOrDefault(o=>o.StableId==State.instanceId);
            if(o==null)return null;
            var type=Adapter.Objects.Find(o);
            return type!=null?Parts.FirstOrDefault(a=>a.obj==type):Parts.FirstOrDefault(a=>a.marker==o.Kind);
        }
        private void ApplyCoordinates()
        {
            Run(()=>{if(!int.TryParse(xInput.text,out var x)||!int.TryParse(yInput.text,out var y))throw new InvalidOperationException("정수 X/Y 좌표를 입력하세요.");
                UpdateCandidate(new Vector2Int(x,y),!string.IsNullOrEmpty(State.instanceId));CommitPending();});
        }
        private void ShowGhost()
        {
            var part=GhostPart;
            if(ghostArtId!=part?.id || ghostArt==null)
            {
                if(ghostArt!=null){ghostArt.SetActive(false);Destroy(ghostArt);}foreach(var m in ghostMaterials)Destroy(m);ghostMaterials.Clear();ghostArt=null;ghostArtId=part?.id;
                if(part?.terrain!=null)ghostArt=StageTerrainStructureRuntime.CreateArt(artRoot,new AnimolTerrainPlacement{instanceId="Placement ghost",catalogId=part.id},part.terrain,Adapter.Terrain.Frame(part.terrain.frameId),Adapter.Map.GetEditorPreviewUnitsPerCell(),100);
                else if(part?.obj!=null)ghostArt=TerrainEditorArt.CopySprites(part.obj.Prefab,artRoot);
                if(ghostArt!=null)TerrainEditorArt.SetLayer(ghostArt);
                if(ghostArt!=null)foreach(var r in ghostArt.GetComponentsInChildren<SpriteRenderer>())
                {
                    r.sortingOrder=100;
                    if(r.sharedMaterial!=null && r.sharedMaterial.HasProperty("_PreviewOpacity"))
                    {var material=new Material(r.sharedMaterial);material.SetFloat("_PreviewOpacity",.55f);r.sharedMaterial=material;ghostMaterials.Add(material);}
                    else r.color=new Color(1,1,1,.55f);
                }
            }
            if(ghostArt==null)return;
            ghostArt.SetActive(GhostVisible);ghostArt.transform.localPosition=new Vector3(GhostOrigin.x,GhostOrigin.y,0)*Adapter.Map.GetEditorPreviewUnitsPerCell();
        }
        partial void DestroyTools(){foreach(var m in ghostMaterials)if(m!=null)Destroy(m);}
        private void RefreshInformation()
        {
            if(infoPanel==null)return;
            var selected=SelectionPart();var part=selected??SelectedPart;var origin=SelectionOrigin();
            SelectedBounds=selected==null?(Rect?)null:new Rect(origin,selected.size);
            infoPanel.gameObject.SetActive(State.information && !IsTesting);
            infoText.text=Describe(part)+"\n"+(selected==null?"배치 원점 · 정수 X/Y":State.instanceId+"\n전체 instance · X/Y 이동");
            if(!xInput.isFocused)xInput.SetTextWithoutNotify(origin.x.ToString());if(!yInput.isFocused)yInput.SetTextWithoutNotify(origin.y.ToString());
            toolLabel.text="도구: "+State.tool+"\n1셀 고정";
            RefreshSettings();
        }
    }
    public sealed class TerrainEditorMapInput : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerMoveHandler,IDragHandler,IBeginDragHandler,IEndDragHandler,IScrollHandler,IPointerExitHandler
    {
        public TerrainEditorScreen screen;
        public void OnPointerDown(PointerEventData e)=>screen.PointerDown(e);
        public void OnPointerUp(PointerEventData e)=>screen.PointerUp(e);
        public void OnPointerMove(PointerEventData e)=>screen.PointerMove(e);
        public void OnDrag(PointerEventData e)=>screen.PointerMove(e);
        public void OnBeginDrag(PointerEventData e){}
        public void OnEndDrag(PointerEventData e){}
        public void OnScroll(PointerEventData e){screen.Zoom(Mathf.Pow(1.15f,-e.scrollDelta.y));}
        public void OnPointerExit(PointerEventData e)=>screen.PointerMove(e);
    }
}
