using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Gameplay;
using ANIMOL.Development;
using Animol.TerrainStructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Engine=Animol.TerrainStructure.AnimolTerrainPlacementEngine;

namespace ANIMOL.Editor
{
    public static partial class TerrainEditorSceneEditor
    {
        private static AnimolTerrainSavedMap freeSource,freeCandidate;
        private static readonly HashSet<Vector2Int> freeStroke=new HashSet<Vector2Int>();
        private static Vector2Int freeStart,freeLast,freeDelta;
        private static GameObject freePreview;
        private static FreeShapeTerrainRenderer freePreviewRenderer;
        private static bool freeValid;
        private static string freeError;
        private static Vector2 freeScroll;
        private static int freeDrawRevision=-1;
        private static Dictionary<Vector2Int,FreeShapeCell> freeDrawCells;
        public static bool FreeCandidateValid=>freeValid;
        public static double FreeCandidateMilliseconds {get;private set;}

        public static void SetFreeShapeMode(bool enabled)
        {
            if(enabled)FreeShapeArtRegistry.Load(adapter.Map.FreeShapeTerrain);
            Cancel(false);state.freeShape=enabled;state.information=false;mapSettings=false;state.instanceId="";
            if(enabled && state.paletteHeight<.25f)state.paletteHeight=.25f;
            if(string.IsNullOrEmpty(state.freeStyle))state.freeStyle=adapter.Map.ThemeId+"_A";
            status=enabled?"자유형 지형 · B 칠하기 / E 지우기 / R 영역 / [ ] 크기 / Esc 취소":"v3 부품 / 객체";view?.Repaint();
        }
        private static void DrawFreePalette()
        {
            var registry=FreeShapeArtRegistry.Load(adapter.Map.FreeShapeTerrain);EditorGUI.DrawRect(paletteRect,TerrainEditorScreen.Panel);
            GUILayout.BeginArea(new Rect(5,paletteRect.y+4,paletteRect.width-10,52));GUILayout.BeginHorizontal();
            if(GUILayout.Button("v3 부품 / 객체",buttonStyle,GUILayout.Width(110)))SetFreeShapeMode(false);
            var themes=registry.styles.GroupBy(s=>s.themeId).ToArray();string current=state.freeStyle.Split('_')[0];
            int ti=Mathf.Max(0,Array.FindIndex(themes,t=>t.Key==current));int next=EditorGUILayout.Popup(ti,themes.Select(t=>t.Key+" "+t.First().themeName).ToArray(),GUILayout.Width(155));
            if(next!=ti){Cancel(false);state.freeStyle=themes[next].Key+"_A";}
            GUILayout.Label("크기 [ ]",smallStyle,GUILayout.Width(65));state.freeBrush=EditorGUILayout.IntSlider(state.freeBrush,1,8,GUILayout.Width(135));
            if(GUILayout.Button("덩어리 삭제 Del",buttonStyle,GUILayout.Width(120)))Run(DeleteFreeSelection);
            GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
            var fixtures=registry.fixtures;int index=Mathf.Max(0,Array.FindIndex(fixtures,f=>f.id==state.freePreset));
            int chosen=EditorGUILayout.Popup(index,fixtures.Select(f=>FixtureLabel(f.id)).ToArray(),GUILayout.Width(Mathf.Min(210,paletteRect.width*.34f)));state.freePreset=fixtures[chosen].id;
            if(GUILayout.Button("프리셋 그리기",buttonStyle,GUILayout.Width(100))){Cancel(false);state.freeTool="Preset";}
            GUILayout.Label("도구: "+FreeToolLabel()+" · seed "+(adapter.Map.FreeShapeTerrain?.seed??0),smallStyle);
            GUILayout.Label("아트 v"+registry.version,smallStyle,GUILayout.Width(60));
            if(GUILayout.Button("현재 맵 아트 전환",buttonStyle,GUILayout.Width(125)))
            {
                var menu=new GenericMenu();
                foreach(int version in new[]{1,2,3,4})
                {
                    int target=version;var label=new GUIContent("v"+version+(version==4?" · joint_finish_v4":version==3?" · brick_restore_v3":version==2?" · clean_v2":" · 기존 아트"));
                    try{FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,version);menu.AddItem(label,registry.version==version,()=>Run(()=>ChangeFreeShapeArtVersion(target)));}
                    catch(Exception){menu.AddDisabledItem(new GUIContent(label.text+" · 설치 필요"));}
                }
                menu.ShowAsContext();
            }
            GUILayout.EndHorizontal();GUILayout.EndArea();
            var styles=registry.styles.Where(s=>s.themeId==state.freeStyle.Split('_')[0]).ToArray();float cw=Mathf.Max(225,(paletteRect.width-28)/4);float ch=paletteRect.height-62;
            freeScroll=GUI.BeginScrollView(new Rect(4,paletteRect.y+60,paletteRect.width-8,ch),freeScroll,new Rect(0,0,(cw+4)*4,ch-17),false,false);
            for(int i=0;i<styles.Length;i++)
            {
                var s=styles[i];var rect=new Rect(i*(cw+4),0,cw,ch-18);bool selected=state.freeStyle==s.styleId;
                EditorGUI.DrawRect(rect,selected?TerrainEditorScreen.Hex("5d275d"):TerrainEditorScreen.Ink);
                GUI.Label(new Rect(rect.x+4,2,cw-8,20),(selected?"✓ ":"")+s.styleId+" · "+s.displayName,smallStyle);
                var ids=new[]{"rectangle_5x3","thin_horizontal_16x1","open_u_pit","closed_hole"};
                for(int j=0;j<4;j++)
                {
                    var area=new Rect(rect.x+4+j*(cw-8)/4,40,(cw-12)/4,Mathf.Max(12,rect.height-61));
                    DrawFixtureArt(new Rect(area.x,23,area.width,13),s.styleId,registry.fixtures.First(f=>f.id==ids[j]),true);
                    DrawFixtureArt(area,s.styleId,registry.fixtures.First(f=>f.id==ids[j]),false);
                    GUI.Label(new Rect(area.x,rect.yMax-19,area.width,18),new[]{"사각","긴 발판","U구덩이","구멍"}[j],smallStyle);
                }
                if(GUI.Button(rect,new GUIContent("",s.displayName+" · "+FreeToolLabel()),GUIStyle.none)){Cancel(false);state.freeStyle=s.styleId;state.freeSelected=false;}
            }
            GUI.EndScrollView();
        }
        public static void ChangeFreeShapeArtVersion(int version)
        {
            Cancel(false);
            adapter.ChangeFreeShapeArtVersion(version);
            status="현재 맵 아트 v"+version+" 저장 완료 · revision "+adapter.Map.AuthoringRevision;
            view?.Repaint();
        }
        private static string FixtureLabel(string id)=>id switch {"rectangle_5x3"=>"사각형 5×3","open_u_pit"=>"U자 구덩이","closed_hole"=>"닫힌 내부 구멍","asymmetric_stairs"=>"비대칭 계단","large_chunk_seam_34x6"=>"청크 경계 34×6",_=>id};
        private static string FreeToolLabel()=>state.freeTool switch{"Paint"=>"칠하기","Erase"=>"지우기","Rect"=>"사각 칠하기","RectErase"=>"사각 지우기","Select"=>"덩어리 이동","Preset"=>"프리셋",_=>state.freeTool};
        private static void DrawFixtureArt(Rect area,string style,FreeShapeFixture fixture,bool silhouette)
        {
            var cells=FreeShapeTopology.FromRows(fixture.rows,Vector2Int.zero).ToDictionary(p=>p,p=>new FreeShapeCell{x=p.x,y=p.y,styleId=style});
            float size=Mathf.Min(area.width/fixture.rows[0].Length,area.height/fixture.rows.Length);
            var offset=new Vector2(area.x+(area.width-size*fixture.rows[0].Length)*.5f,area.y+(area.height-size*fixture.rows.Length)*.5f);
            var registry=FreeShapeArtRegistry.Load(adapter.Map.FreeShapeTerrain);
            foreach(var p in cells.Keys)
            {
                var rect=new Rect(offset.x+p.x*size,offset.y+(fixture.rows.Length-1-p.y)*size,size,size);
                if(silhouette)EditorGUI.DrawRect(rect,TerrainEditorScreen.Paper);
                else{var sprite=registry.Cell(style,FreeShapeTopology.Raw(cells,p),FreeShapeTopology.Variant(p.x,p.y,0));var sr=sprite.rect;GUI.DrawTextureWithTexCoords(rect,sprite.texture,new Rect(sr.x/sprite.texture.width,sr.y/sprite.texture.height,sr.width/sprite.texture.width,sr.height/sprite.texture.height));}
            }
        }
        private static void HandleFreeInput(Event e,bool inside)
        {
            if(e.type==EventType.KeyDown&&!EditorGUIUtility.editingTextField&&!e.control&&!e.command)
            {
                if(e.keyCode==KeyCode.B||e.keyCode==KeyCode.E||e.keyCode==KeyCode.R){Cancel(false);state.freeTool=e.keyCode==KeyCode.B?"Paint":e.keyCode==KeyCode.E?"Erase":"Rect";e.Use();return;}
                if(e.keyCode==KeyCode.LeftBracket||e.keyCode==KeyCode.RightBracket){state.freeBrush=Mathf.Clamp(state.freeBrush+(e.keyCode==KeyCode.RightBracket?1:-1),1,8);e.Use();return;}
            }
            if(e.type==EventType.MouseDown&&e.button==0&&inside)
            {BeginFreeStroke(CellAt(e.mousePosition));held=true;GUIUtility.hotControl=inputControl;e.Use();}
            else if(e.type==EventType.MouseDrag&&held)
            {if(inside)UpdateFreeStroke(CellAt(e.mousePosition));e.Use();}
            else if(e.type==EventType.MouseUp&&held)
            {held=false;GUIUtility.hotControl=0;if(inside)Run(()=>{UpdateFreeStroke(CellAt(e.mousePosition));if(state.freeTool!="Select"||freeDelta!=Vector2Int.zero)CommitFreeStroke();else CancelFree();});else Cancel(false);e.Use();}
            else if(e.type==EventType.MouseMove&&!held)
            {
                if(inside&&state.freeTool!="Select"){BeginFreeStroke(CellAt(e.mousePosition));}else CancelFree();view?.Repaint();
            }
            if(e.type==EventType.Ignore&&held)Cancel(false);
        }
        public static void BeginFreeStroke(Vector2Int p)
        {
            HideFreePreview();freeSource=adapter.Read();freeStart=freeLast=p;freeDelta=Vector2Int.zero;freeStroke.Clear();freeValid=false;
            if(state.freeTool=="Select"){state.freeSelection=p;state.freeSelected=FreeShapeTopology.Component(freeSource.freeShape,p).Count>0;status=state.freeSelected?"덩어리 드래그 이동 · Del 전체 삭제":"자유형 셀을 선택하세요.";return;}
            UpdateFreeStroke(p);
        }
        public static void UpdateFreeStroke(Vector2Int p)
        {
            if(freeSource==null)return;
            var timer=System.Diagnostics.Stopwatch.StartNew();freeCandidate=null;freeValid=false;freeDelta=p-freeStart;
            try
            {
                if(state.freeTool=="Select")
                {
                    if(!state.freeSelected||freeDelta==Vector2Int.zero)return;
                    freeValid=Engine.TryMoveFreeShape(adapter.Terrain.Catalog,freeSource,state.freeSelection,freeDelta,false,out freeCandidate,out freeError);
                }
                else
                {
                    if(state.freeTool=="Rect"||state.freeTool=="RectErase")
                    {freeStroke.Clear();for(int y=Math.Min(p.y,freeStart.y);y<=Math.Max(p.y,freeStart.y);y++)for(int x=Math.Min(p.x,freeStart.x);x<=Math.Max(p.x,freeStart.x);x++)freeStroke.Add(new Vector2Int(x,y));}
                    else if(state.freeTool=="Preset")
                    {freeStroke.Clear();foreach(var c in FreeShapeTopology.FromRows(FreeShapeArtRegistry.Load(adapter.Map.FreeShapeTerrain).fixtures.First(f=>f.id==state.freePreset).rows,p))freeStroke.Add(c);}
                    else
                    {
                        int distance=Math.Max(Math.Abs(p.x-freeLast.x),Math.Abs(p.y-freeLast.y));
                        for(int step=0;step<=distance;step++){var q=distance==0?p:new Vector2Int(Mathf.RoundToInt(Mathf.Lerp(freeLast.x,p.x,(float)step/distance)),Mathf.RoundToInt(Mathf.Lerp(freeLast.y,p.y,(float)step/distance)));for(int y=0;y<state.freeBrush;y++)for(int x=0;x<state.freeBrush;x++)freeStroke.Add(q+new Vector2Int(x,y));}
                    }
                    freeLast=p;
                    freeValid=Engine.TryEditFreeShape(adapter.Terrain.Catalog,freeSource,freeStroke,state.freeTool=="Erase"||state.freeTool=="RectErase",state.freeStyle,out freeCandidate,out freeError);
                }
                if(freeValid){adapter.Validate(freeCandidate);ShowFreePreview(freeCandidate.freeShape);status=$"{FreeToolLabel()} · {freeCandidate.freeShape.cells.Count}셀 · mouse-up 저장 / Esc 취소";}
                else throw new InvalidOperationException(freeError);
            }
            catch(Exception ex){freeValid=false;freeError=ex.Message;status="자유형 배치 불가: "+ex.Message;HideFreePreview();}
            FreeCandidateMilliseconds=timer.Elapsed.TotalMilliseconds;view?.Repaint();
        }
        public static void CommitFreeStroke()
        {
            if(!freeValid||freeCandidate==null)throw new InvalidOperationException(freeError??"유효한 자유형 후보가 없습니다.");
            var target=state.freeSelection+freeDelta;var edit=freeCandidate;
            adapter.Commit(edit,"Free shape "+state.freeTool);
            if(state.freeTool=="Select")state.freeSelection=target;
            CancelFree();status="자유형 저장 완료 · revision "+adapter.Map.AuthoringRevision;
        }
        public static void DeleteFreeSelection()
        {
            if(!state.freeSelected)throw new InvalidOperationException("덩어리 도구로 자유형 지형을 선택하세요.");
            if(!Engine.TryMoveFreeShape(adapter.Terrain.Catalog,adapter.Read(),state.freeSelection,Vector2Int.zero,true,out var edit,out var message))throw new InvalidOperationException(message);
            adapter.Commit(edit,"Delete free shape component");state.freeSelected=false;
        }
        private static void ShowFreePreview(FreeShapeLayer layer)
        {
            if(freePreview==null){freePreview=new GameObject("FreeShape candidate · not saved");SceneManager.MoveGameObjectToScene(freePreview,scene);freePreview.layer=TerrainEditorArt.PreviewLayer;freePreview.hideFlags=HideFlags.HideAndDontSave;freePreviewRenderer=freePreview.AddComponent<FreeShapeTerrainRenderer>();}
            freePreview.SetActive(true);freePreviewRenderer.Rebuild(layer,adapter.Map.GetEditorPreviewUnitsPerCell());freePreviewRenderer.SetOpacity(.75f);
            if(artwork!=null)foreach(var r in artwork.GetComponentsInChildren<FreeShapeTerrainRenderer>(true))r.gameObject.SetActive(false);
        }
        private static void HideFreePreview()
        {if(freePreview!=null)freePreview.SetActive(false);if(artwork!=null)foreach(var r in artwork.GetComponentsInChildren<FreeShapeTerrainRenderer>(true))r.gameObject.SetActive(true);}
        private static void CancelFree()
        {HideFreePreview();if(freePreview!=null)UnityEngine.Object.DestroyImmediate(freePreview);freePreview=null;freePreviewRenderer=null;freeSource=null;freeCandidate=null;freeStroke.Clear();freeValid=false;freeDelta=Vector2Int.zero;freeDrawRevision=-1;}
        private static void DrawFreeWorld()
        {
            if(freeDrawRevision!=adapter.Map.AuthoringRevision||freeDrawCells==null){freeDrawCells=FreeShapeTopology.Index(adapter.Map.FreeShapeTerrain);freeDrawRevision=adapter.Map.AuthoringRevision;}
            if(state.masks)foreach(var p in freeDrawCells.Keys)DrawBox(new Rect(p,Vector2.one),new Color(.2f,.7f,.4f,.5f),false);
            if(state.freeSelected)foreach(var p in FreeShapeTopology.Component(adapter.Map.FreeShapeTerrain,state.freeSelection))DrawBox(new Rect(p,Vector2.one),TerrainEditorScreen.Gold,false);
            foreach(var p in freeStroke)DrawBox(new Rect(p,Vector2.one),freeValid?TerrainEditorScreen.Green:TerrainEditorScreen.Red,false);
        }
    }
}
