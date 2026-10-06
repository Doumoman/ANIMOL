using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ANIMOL.Core;
using ANIMOL.Development;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Engine=Animol.TerrainStructure.AnimolTerrainPlacementEngine;

namespace ANIMOL.Editor
{
    // The production authoring entry is an Edit Mode Scene View. A private preview scene
    // isolates its render-only objects without opening, saving or hiding any user scene.
    [InitializeOnLoad]
    public static partial class TerrainEditorSceneEditor
    {
        private const string Key="ANIMOL.SceneTerrainV4.";
        private const string Title="ANIMOL · Map Editor";
        private static RectInt boundsDraft;
        private static StageMapDefinition boundsDraftMap;
        private static int boundsDraftRevision=-1;
        private static readonly PropertyInfo CustomScene=typeof(SceneView).GetProperty("customScene",BindingFlags.Instance|BindingFlags.NonPublic);
        private static readonly PropertyInfo CullingMask=typeof(SceneView).GetProperty("overrideSceneCullingMask",BindingFlags.Instance|BindingFlags.NonPublic);
        private static Scene scene;
        private static SceneView view;
        private static GameObject artwork,ghost;
        private static string staticArtSignature;
        private static readonly List<Material> ghostMaterials=new List<Material>();
        private static TerrainEditorAdapter adapter;
        private static TerrainEditorViewState state=new TerrainEditorViewState();
        private static readonly List<TerrainEditorPart> parts=new List<TerrainEditorPart>();
        private static Vector2 paletteScroll,infoScroll;
        private static string status="",ghostId,key,error;
        private static bool valid,hover,held,dragging,panning,mapSettings;
        private static Vector2Int origin,dragStart,oldOrigin;
        private static bool spaceHeld;
        private static Vector3 panWorld;
        private static Vector2Int? pairAnchor;
        private static AnimolTerrainSavedMap candidate;
        private static TerrainEditorObjectEdit objectCandidate;
        private static TerrainEditorPart ghostPart;
        private static Rect paletteRect,infoRect,canvasRect,toolbarRect;
        private static TerrainEditorSettingsDraft settings;
        private static string settingsKey;
        private static int pickIndex;
        private static int inputControl;
        private static Vector2Int pickCell;
        private static string pickSignature;
        private static GUIStyle labelStyle,buttonStyle,smallStyle;
        public static bool Active=>adapter!=null && view!=null;
        public static TerrainEditorAdapter Adapter=>adapter;
        public static SceneView View=>view;
        public static TerrainEditorViewState State=>state;
        public static string Status=>status;
        public static Vector2Int GhostOrigin=>origin;
        public static bool GhostValid=>valid;
        public static int PartCount=>parts.Count;
        public static Vector2? ProbeWorld;
        public static Vector2 ProbeGui {get;private set;}

        static TerrainEditorSceneEditor()
        {
            SceneView.duringSceneGui+=Draw;
            AssemblyReloadEvents.beforeAssemblyReload+=()=>{Remember();Release(false);};
            EditorApplication.playModeStateChanged+=PlayChanged;
            EditorApplication.update+=()=>
            {
                if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
                if(adapter==null)return;
                if(view==null)Release(true);
                else if(held && EditorWindow.focusedWindow!=view)Cancel(false);
            };
            EditorApplication.delayCall+=Recover;
        }
        public static void OpenSelected()
        {
            var map=Selection.activeObject as StageMapDefinition ?? (Selection.activeObject as CampaignStageDefinition)?.MapDefinition;
            if(map==null){CampaignMapEditorWindow.Open();return;}Open(map);
        }
        public static void Open(StageMapDefinition map,string selectedPart="")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Scene 편집은 Unity Play Mode 종료 후 사용할 수 있습니다.");
            if(map==null || !EditorUtility.IsPersistent(map))throw new InvalidOperationException("정본 StageMapDefinition을 선택하세요.");
            if(Active && adapter.Map==map){if(selectedPart!="")SelectPart(selectedPart);view.Focus();return;}
            Release(false);
            SessionState.SetString(Key+"guid",AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(map)));
            state=new TerrainEditorViewState{freeShape=true,freeStyle=map.ThemeId+"_A",partId=selectedPart,center=map.EditorPreviewCellBounds.center*map.GetEditorPreviewUnitsPerCell(),zoom=Mathf.Min(18,Mathf.Max(8,map.EditorPreviewCellBounds.height*.5f))};
            SessionState.SetBool(Key+"active",true);Build();
        }
        private static void Recover()
        {
            if(Active || !SessionState.GetBool(Key+"active",false) || EditorApplication.isPlayingOrWillChangePlaymode)return;
            state=JsonUtility.FromJson<TerrainEditorViewState>(SessionState.GetString(Key+"state","{}"));Build();
        }
        private static void Build()
        {
            try
            {
                adapter=new TerrainEditorAdapter(SessionState.GetString(Key+"guid",""));adapter.Changed+=Refresh;
                view=EditorUtility.InstanceIDToObject(SessionState.GetInt(Key+"window",0)) as SceneView;
                if(view==null)view=Resources.FindObjectsOfTypeAll<SceneView>().FirstOrDefault(v=>v.titleContent.text==Title);
                if(view==null){view=ScriptableObject.CreateInstance<SceneView>();view.titleContent=new GUIContent(Title);view.Show();}
                view.titleContent=new GUIContent(Title);SessionState.SetInt(Key+"window",view.GetInstanceID());
                if(CustomScene==null || CullingMask==null)throw new InvalidOperationException("Unity Scene View preview API가 변경되었습니다.");
                scene=EditorSceneManager.NewPreviewScene();CustomScene.SetValue(view,scene);
                CullingMask.SetValue(view,EditorSceneManager.GetSceneCullingMask(scene));
                view.in2DMode=true;view.orthographic=true;view.sceneLighting=false;view.drawGizmos=false;
                view.LookAtDirect(state.center,Quaternion.identity,state.zoom);
                parts.Clear();
                parts.AddRange(adapter.Markers.Where(p=>p.marker is StageMapObjectKind.PlayerStart or StageMapObjectKind.Exit or StageMapObjectKind.Checkpoint or StageMapObjectKind.BubbleCandidate));
                parts.AddRange(adapter.Objects.Types.Where(d=>d!=null).Select(d=>new TerrainEditorPart{id=d.StableTypeId,name=d.DisplayName,theme=string.IsNullOrEmpty(d.ThemeId)?"COMMON":d.ThemeId,category="Obstacles",size=d.FootprintCells,obj=d}));
                state.freeShape=true;state.freeStyle=adapter.Map.ThemeId+"_A";Refresh();view.Focus();
                status=$"Scene 편집 · {adapter.Map.StageId} · 자유형 V6 + 장애물 + 캠페인 마커 · 저장 revision {adapter.Map.AuthoringRevision}";
            }
            catch(Exception ex){Release(true);status=ex.Message;Debug.LogError("ANIMOL Scene editor: "+ex.Message);}
        }
        private static string RoleLabel(string role)=>role switch{"Platform"=>"발판","Bridge"=>"다리","Mass"=>"몸체","FillPatch"=>"질감","WallCorner"=>"L벽",_=>role};
        private static void Refresh()
        {
            if(adapter==null || !scene.IsValid())return;
            Cancel(false);
            var map=adapter.Map;
            var signature=map.GetInstanceID()+"|"+map.GetEditorPreviewUnitsPerCell()+"|"+JsonUtility.ToJson(map.TerrainPlacements)+"|"+
                string.Join(";",map.Cells.Select(c=>JsonUtility.ToJson(c)))+"|"+string.Join(";",map.Objects.Select(o=>JsonUtility.ToJson(o)));
            if(artwork!=null && signature==staticArtSignature)
            {
                var free=artwork.GetComponentInChildren<FreeShapeTerrainRenderer>(true);
                if(free==null && map.HasFreeShape){var go=new GameObject("FreeShape artwork");go.transform.SetParent(artwork.transform,false);go.layer=TerrainEditorArt.PreviewLayer;go.hideFlags=HideFlags.HideAndDontSave;free=go.AddComponent<FreeShapeTerrainRenderer>();}
                if(free!=null){free.gameObject.SetActive(true);free.Rebuild(map.FreeShapeTerrain??new FreeShapeLayer(),map.GetEditorPreviewUnitsPerCell(),TerrainEditorArt.ObstaclePreview(map));}
                status="저장 완료 · revision "+map.AuthoringRevision;view?.Repaint();return;
            }
            var root=new GameObject("ANIMOL Scene artwork · not saved");SceneManager.MoveGameObjectToScene(root,scene);
            try{TerrainEditorArt.BuildMap(adapter,root.transform);}
            catch{UnityEngine.Object.DestroyImmediate(root);throw;}
            if(artwork!=null)UnityEngine.Object.DestroyImmediate(artwork);artwork=root;
            staticArtSignature=signature;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.hideFlags=HideFlags.HideAndDontSave;
            key=null;candidate=null;objectCandidate=null;settingsKey=null;
            if(!adapter.Read().placements.Any(p=>p.instanceId==state.instanceId) && !adapter.Map.Objects.Any(o=>o.StableId==state.instanceId))state.instanceId="";
            if(state.instanceId!="")origin=SelectionOrigin();
            status="저장 완료 · revision "+adapter.Map.AuthoringRevision;view?.Repaint();
        }
        private static void Remember()
        {
            if(view!=null){state.center=view.pivot;state.zoom=view.size;}
            SessionState.SetString(Key+"state",JsonUtility.ToJson(state));
        }
        public static void Close(){Remember();Release(true);if(view!=null)view.Close();view=null;}
        private static void Release(bool end)
        {
            Cancel(false);DestroyGhost();fixtureCells.Clear();
            if(settings!=null)UnityEngine.Object.DestroyImmediate(settings);settings=null;
            if(adapter!=null){adapter.Changed-=Refresh;adapter.Dispose();adapter=null;}
            if(view!=null && CustomScene!=null){CustomScene.SetValue(view,default(Scene));CullingMask?.SetValue(view,0UL);}
            if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);scene=default;artwork=null;
            if(end)SessionState.SetBool(Key+"active",false);
        }
        private static void PlayChanged(PlayModeStateChange change)
        {
            if(change==PlayModeStateChange.ExitingEditMode){Remember();Release(false);}
            if(change==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Recover;
        }
        private static void Styles()
        {
            if(labelStyle!=null)return;
            labelStyle=new GUIStyle(EditorStyles.label){font=adapter.Font,fontSize=14,wordWrap=true,normal={textColor=TerrainEditorScreen.Paper}};
            smallStyle=new GUIStyle(labelStyle){fontSize=11};
            buttonStyle=new GUIStyle(GUI.skin.button){font=adapter.Font,fontSize=13};
        }
        private static void Draw(SceneView current)
        {
            if(current!=view || adapter==null || EditorApplication.isPlayingOrWillChangePlaymode)return;
            Styles();var e=Event.current;
            inputControl=GUIUtility.GetControlID("ANIMOLSceneDrag".GetHashCode(),FocusType.Passive);
            float width=view.position.width,height=view.position.height-24;
            float paletteHeight=state.paletteHeight<.1f?42:Mathf.Clamp(height*state.paletteHeight,150,350);
            toolbarRect=new Rect(0,0,width,width<720?66:40);
            paletteRect=new Rect(0,height-paletteHeight-25,width,paletteHeight);
            canvasRect=new Rect(0,toolbarRect.yMax,width,paletteRect.yMin-toolbarRect.yMax);
            infoRect=state.information||mapSettings?new Rect(width-Mathf.Min(310,width*.43f)-8,toolbarRect.yMax+8,Mathf.Min(310,width*.43f),Mathf.Max(70,canvasRect.height-16)):Rect.zero;
            if(ProbeWorld.HasValue)ProbeGui=HandleUtility.WorldToGUIPoint(ProbeWorld.Value*adapter.Map.GetEditorPreviewUnitsPerCell());
            // Own an in-progress canvas gesture before dynamically appearing IMGUI fields
            // can consume its drag/up event. Rect checks keep all palette/panel input out.
            HandleInput(e);
            DrawWorld();
            Handles.BeginGUI();
            try{DrawToolbar(width);DrawPalette();if(infoRect.width>0)DrawInformation();
                EditorGUI.DrawRect(new Rect(0,paletteRect.yMax,width,25),TerrainEditorScreen.Ink);GUI.Label(new Rect(8,paletteRect.yMax+2,width-16,22),status,smallStyle);}
            finally{Handles.EndGUI();}
        }
        private static void DrawToolbar(float width)
        {
            EditorGUI.DrawRect(toolbarRect,TerrainEditorScreen.Panel);
            GUILayout.BeginArea(new Rect(5,3,width-10,toolbarRect.height-4));GUILayout.BeginHorizontal();
            if(GUILayout.Button(adapter.Map.StageId+" · "+adapter.Map.ThemeId,buttonStyle,GUILayout.Width(width<720?135:195))){mapSettings=!mapSettings;state.information=false;}
            if(GUILayout.Button("저장",buttonStyle))Run(()=>{adapter.Save();status="저장 완료 · revision "+adapter.Map.AuthoringRevision;});
            if(GUILayout.Button("Undo",buttonStyle))Run(adapter.Undo);
            if(GUILayout.Button("Redo",buttonStyle))Run(adapter.Redo);
            if(GUILayout.Button("시험 플레이",buttonStyle))Run(()=>{adapter.Save();var report=StageMapValidator.ValidateStructure(adapter.Map);if(!report.IsValid)throw new InvalidOperationException(string.Join(" | ",report.Errors));TerrainEditorSession.OpenForSceneTest(adapter.Map);});
            if(GUILayout.Button("캠페인",buttonStyle,GUILayout.Width(65)))CampaignMapEditorWindow.Open();
            if(GUILayout.Button("닫기",buttonStyle,GUILayout.Width(45)))EditorApplication.delayCall+=Close;
            if(width<720){GUILayout.EndHorizontal();GUILayout.BeginHorizontal();}
            foreach(var tool in (state.freeShape?new[]{("Paint","칠하기 B"),("Erase","지우기 E"),("Rect","영역 R"),("RectErase","영역−"),("Select","덩어리")} : new[]{("Place","배치"),("Select","선택"),("Delete","삭제")}))
            {var old=GUI.backgroundColor;if((state.freeShape?state.freeTool:state.tool)==tool.Item1)GUI.backgroundColor=TerrainEditorScreen.Gold;if(GUILayout.Button(tool.Item2,buttonStyle)){Cancel(false);if(state.freeShape)state.freeTool=tool.Item1;else state.tool=tool.Item1;}GUI.backgroundColor=old;}
            state.grid=GUILayout.Toggle(state.grid,"격자",buttonStyle);state.masks=GUILayout.Toggle(state.masks,"마스크",buttonStyle);
            GUILayout.EndHorizontal();GUILayout.EndArea();
        }
        private static void DrawPalette()
        {
            if(state.freeShape){DrawFreePalette();return;}
            if(state.category=="Obstacles"){DrawObstaclePalette();return;}
            EditorGUI.DrawRect(paletteRect,TerrainEditorScreen.Panel);
            GUILayout.BeginArea(new Rect(8,paletteRect.y+5,paletteRect.width-16,30));
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("자유형 지형 V6",buttonStyle,GUILayout.Width(135)))Run(()=>SetFreeShapeMode(true));
            if(GUILayout.Button("장애물",buttonStyle,GUILayout.Width(90)))SetObstacleMode();
            GUILayout.Label("캠페인 마커 · 선택 후 맵을 클릭하세요",labelStyle);
            GUILayout.EndHorizontal();GUILayout.EndArea();
            float width=Mathf.Min(150,(paletteRect.width-40)/4);
            var markerParts=parts.Where(p=>p.marker.HasValue).ToArray();
            for(int i=0;i<markerParts.Length;i++)
            {
                var part=markerParts[i];var rect=new Rect(8+i*(width+6),paletteRect.y+42,width,Mathf.Max(30,paletteRect.height-52));
                var old=GUI.backgroundColor;
                if(part.id==state.partId)GUI.backgroundColor=TerrainEditorScreen.Gold;
                if(GUI.Button(rect,part.name,buttonStyle))SelectPart(part.id);
                GUI.backgroundColor=old;
            }
        }
        public static void SelectPart(string id){Cancel(false);state.freeShape=false;state.partId=id;state.instanceId="";state.tool="Place";state.information=true;mapSettings=false;status=Details(SelectedPart);view?.Repaint();}
        private static TerrainEditorPart SelectedPart=>parts.FirstOrDefault(p=>p.id==state.partId);
        private static string Details(TerrainEditorPart p)=>p==null?"부품 선택":p.id+" · "+p.name+" · "+p.size.x+"×"+p.size.y+(p.terrain==null?"":p.IsOverlay?$" · 추가 충돌 0 · 받침 {p.terrain.supportCount}":$" · 고체 {p.terrain.solidCount} · 빈 {p.terrain.voidCount}");
        private static void DrawInformation()
        {
            EditorGUI.DrawRect(infoRect,TerrainEditorScreen.Panel);GUILayout.BeginArea(new Rect(infoRect.x+8,infoRect.y+8,infoRect.width-16,infoRect.height-16));
            infoScroll=GUILayout.BeginScrollView(infoScroll);
            if(mapSettings)
            {
                GUILayout.Label(adapter.Map.StageId+"\n맵 범위(셀) "+adapter.Map.CellBounds+"\n셀 크기 "+adapter.Map.WorldUnitsPerCell+" unit",labelStyle);
                if(adapter.Map.WorldUnitsPerCell<=0)EditorGUILayout.HelpBox("현재 미설정: 편집 미리보기만 1unit을 사용합니다.",MessageType.Warning);
                if(GUILayout.Button("1셀 = 1unit 설정",buttonStyle))Run(()=>adapter.EditMapConfiguration("Units"));
                if(boundsDraftMap!=adapter.Map || boundsDraftRevision!=adapter.Map.AuthoringRevision)
                {boundsDraftMap=adapter.Map;boundsDraftRevision=adapter.Map.AuthoringRevision;boundsDraft=adapter.Map.EditorPreviewCellBounds;}
                var min=EditorGUILayout.Vector2IntField("시작 X / Y",boundsDraft.position);
                var size=EditorGUILayout.Vector2IntField("너비 / 높이 (셀)",boundsDraft.size);
                boundsDraft=new RectInt(min,size);
                if(GUILayout.Button("맵 범위 적용",buttonStyle))Run(()=>adapter.ResizeMap(boundsDraft));
                GUILayout.Label("1셀 단위 · 기존 지형/객체를 자르는 축소는 거부합니다.",smallStyle);
            }
            else
            {
                var part=SelectionPart()??SelectedPart;GUILayout.Label(Details(part),labelStyle);
                GUILayout.Label(state.instanceId,smallStyle);
                var target=origin;
                var requested=EditorGUILayout.Vector2IntField("원점 X / Y",target);
                if(requested!=target)origin=requested;
                if(GUILayout.Button(state.instanceId==""?"좌표에 배치":"좌표로 전체 이동",buttonStyle))Run(()=>{UpdateCandidate(origin,state.instanceId!="");Commit();});
                if(state.instanceId!="" && GUILayout.Button("선택 instance 전체 삭제",buttonStyle))Run(DeleteSelected);
                var obj=adapter.Map.Objects.FirstOrDefault(o=>o.StableId==state.instanceId);
                if(obj!=null)
                {
                    if(ANIMOL.Gameplay.ObstacleGraphics.ObstacleSnapshotAdapter.Kind(obj.Kind)!=null)
                    {
                        try{ANIMOL.Gameplay.ObstacleGraphics.ObstacleSnapshotAdapter.Read(adapter.Map,obj);}
                        catch(InvalidOperationException problem){EditorGUILayout.HelpBox(problem.Message,MessageType.Error);}
                        if(GUILayout.Button("선택 재료 적용: "+state.freeStyle,buttonStyle))Run(()=>{var changed=obj.Settings.Clone();changed.EditorSetObstacleStyle(state.freeStyle);adapter.CommitObject(new TerrainEditorObjectEdit{operation="Settings",instanceId=obj.StableId,settings=changed});});
                    }
                    var signature=obj.StableId+":"+adapter.Map.AuthoringRevision;
                    if(signature!=settingsKey)
                    {if(settings!=null)UnityEngine.Object.DestroyImmediate(settings);settings=ScriptableObject.CreateInstance<TerrainEditorSettingsDraft>();settings.hideFlags=HideFlags.HideAndDontSave;settings.value=obj.Settings.Clone();settingsKey=signature;}
                    var serialized=new SerializedObject(settings);serialized.Update();
                    using(new EditorGUI.DisabledScope(obj.Settings.Version>3)){EditorGUILayout.PropertyField(serialized.FindProperty("value"),new GUIContent("객체 설정"),true);serialized.ApplyModifiedPropertiesWithoutUndo();
                        if(GUILayout.Button("설정 적용",buttonStyle))Run(()=>adapter.CommitObject(new TerrainEditorObjectEdit{operation="Settings",instanceId=obj.StableId,settings=settings.value.Clone()}));}
                }
            }
            GUILayout.EndScrollView();if(GUILayout.Button("접기",buttonStyle)){state.information=false;mapSettings=false;}GUILayout.EndArea();
        }
        private static void HandleInput(Event e)
        {
            if(e.type==EventType.KeyUp && e.keyCode==KeyCode.Space){spaceHeld=false;e.Use();return;}
            if(e.type==EventType.KeyDown)
            {
                if(EditorGUIUtility.editingTextField)return;
                if(e.keyCode==KeyCode.Space){spaceHeld=true;e.Use();return;}
                if(e.keyCode==KeyCode.Escape){Cancel();e.Use();return;}
                if(e.keyCode==KeyCode.Delete){Run(DeleteSelected);e.Use();return;}
                if(e.control||e.command)
                {if(e.keyCode==KeyCode.Z){Run(e.shift?adapter.Redo:adapter.Undo);e.Use();return;}if(e.keyCode==KeyCode.Y){Run(adapter.Redo);e.Use();return;}if(e.keyCode==KeyCode.S){Run(()=>{adapter.Save();status="저장 완료";});e.Use();return;}}
            }
            bool inside=canvasRect.Contains(e.mousePosition)&&!infoRect.Contains(e.mousePosition);
            if(e.type==EventType.Layout)HandleUtility.AddDefaultControl(inputControl);
            if(e.type==EventType.MouseDown && e.button==0 && inside && spaceHeld)
            {panning=true;panWorld=WorldAt(e.mousePosition);e.Use();return;}
            if(panning && e.type==EventType.MouseDrag){view.pivot+=panWorld-WorldAt(e.mousePosition);view.Repaint();e.Use();return;}
            if(panning && e.type==EventType.MouseUp){panning=false;e.Use();return;}
            if(e.type==EventType.MouseDown && e.button==1){Cancel();e.Use();return;}
            if(e.alt || e.button==2)return; // native Scene View pan/zoom
            if(state.freeShape){HandleFreeInput(e,inside);return;}
            if(e.type==EventType.MouseDown && e.button==0 && inside)
            {
                held=true;dragging=false;dragStart=CellAt(e.mousePosition);oldOrigin=SelectionOrigin();
                if(state.tool=="Select"||state.tool=="Delete"){Pick(dragStart);oldOrigin=SelectionOrigin();}
                else UpdateCandidate(dragStart,false);
                GUIUtility.hotControl=inputControl;e.Use();
            }
            else if(e.type==EventType.MouseDrag && held)
            {dragging=true;if(inside)UpdateCandidate(state.tool=="Select"?oldOrigin+CellAt(e.mousePosition)-dragStart:CellAt(e.mousePosition),state.tool=="Select");e.Use();}
            else if(e.type==EventType.MouseUp && held)
            {
                held=false;GUIUtility.hotControl=0;
                if(!inside){Cancel(false);e.Use();return;}
                if(state.tool=="Delete")Run(DeleteSelected);
                else if(state.tool!="Select"||dragging)Run(()=>{UpdateCandidate(state.tool=="Select"?oldOrigin+CellAt(e.mousePosition)-dragStart:CellAt(e.mousePosition),state.tool=="Select");Commit();});
                e.Use();
            }
            else if(e.type==EventType.MouseMove && !held)
            {if(inside && state.tool is "Place" or "Brush" or "Erase")UpdateCandidate(CellAt(e.mousePosition),false);else{hover=false;if(ghost!=null)ghost.SetActive(false);}view.Repaint();}
            if(e.type==EventType.Ignore && held)Cancel(false);
        }
        public static Vector2Int CellAt(Vector2 guiPoint)
        {var p=WorldAt(guiPoint)/adapter.Map.GetEditorPreviewUnitsPerCell();return new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y));}
        private static Vector3 WorldAt(Vector2 guiPoint)
        {var ray=HandleUtility.GUIPointToWorldRay(guiPoint);var plane=new Plane(Vector3.forward,Vector3.zero);return plane.Raycast(ray,out var distance)?ray.GetPoint(distance):Vector3.zero;}
        public static void UpdateCandidate(Vector2Int point,bool move)
        {
            var part=move?SelectionPart():SelectedPart;var signature=adapter.Map.AuthoringRevision+":"+point+":"+part?.id+":"+state.tool+":"+move+":"+state.instanceId;
            if(signature==key)return;key=signature;origin=point;ghostPart=part;hover=true;valid=false;candidate=null;objectCandidate=null;
            try
            {
                var dto=adapter.Read();
                if(state.tool is "Brush" or "Erase")
                {ghostPart=null;if(!Engine.TrySetBaseCell(adapter.Terrain.Catalog,dto,point,state.tool=="Brush",part?.terrain?.styleId??adapter.Map.ThemeId+"_A",out candidate,out error))throw new InvalidOperationException(error);}
                else if(part?.terrain!=null)
                {bool ok=move?Engine.TryMove(adapter.Terrain.Catalog,dto,state.instanceId,point,out candidate,out error):Engine.TryPlace(adapter.Terrain.Catalog,dto,part.id,point,out candidate,out error);if(!ok)throw new InvalidOperationException(error);}
                else if(part?.obj!=null || part?.marker!=null)
                {
                    objectCandidate=new TerrainEditorObjectEdit{operation=move?"Move":"Place",definitionId=part.id,instanceId=move?state.instanceId:part.id+"-"+Guid.NewGuid().ToString("N"),origin=point};
                    if(!move && part.obj?.RequiresLinkedPair==true){objectCandidate.operation=pairAnchor.HasValue?"Pair":"PairAnchor";objectCandidate.second=point;if(pairAnchor.HasValue)objectCandidate.origin=pairAnchor.Value;}
                    if(!move && part.obj!=null && ANIMOL.Gameplay.ObstacleGraphics.ObstacleSnapshotAdapter.Kind(part.obj.Kind)!=null)
                    {objectCandidate.settings=StageMapObjectAuthoringOperations.PlacementSettings(part.obj,point);objectCandidate.settings.EditorSetObstacleStyle(state.freeStyle);}
                    adapter.ValidateObject(objectCandidate);
                }
                else throw new InvalidOperationException("하단 팔레트에서 부품을 선택하세요.");
                if(candidate!=null)adapter.Validate(candidate);valid=true;status=$"({point.x}, {point.y}) · 1셀 스냅 · "+Details(part);
            }
            catch(Exception ex){error=ex.Message;status=$"({point.x}, {point.y}) · 배치 불가: "+error;}
            ShowGhost();view?.Repaint();
        }
        public static void Commit()
        {
            if(!valid){var message=error;Cancel(false);throw new InvalidOperationException(message);}
            if(candidate!=null)
            {var id=candidate.placements.FirstOrDefault(p=>!adapter.Read().placements.Any(o=>o.instanceId==p.instanceId))?.instanceId;adapter.Commit(candidate,"ANIMOL Scene "+state.tool);if(id!=null)state.instanceId=id;}
            else if(objectCandidate!=null)
            {var edit=objectCandidate;if(edit.operation=="PairAnchor"){pairAnchor=edit.origin;key=null;status="첫 위치 선택 · 두 번째 위치 클릭 · Esc 취소";return;}adapter.CommitObject(edit);state.instanceId=edit.instanceId;pairAnchor=null;}
            candidate=null;objectCandidate=null;key=null;hover=false;if(ghost!=null)ghost.SetActive(false);status="저장 완료 · revision "+adapter.Map.AuthoringRevision;
        }
        public static void Cancel(bool select=true)
        {CancelFree();if(inputControl!=0 && GUIUtility.hotControl==inputControl)GUIUtility.hotControl=0;held=false;dragging=false;panning=false;spaceHeld=false;candidate=null;objectCandidate=null;key=null;pairAnchor=null;hover=false;valid=false;if(ghost!=null)ghost.SetActive(false);if(select)state.tool="Select";status="후보 취소 · 정본 변경 없음";}
        private static void Pick(Vector2Int point)
        {
            var ids=adapter.Read().placements.Where(p=>{var a=parts.FirstOrDefault(t=>t.id==p.catalogId);return a!=null && new Rect(p.x,p.y,a.size.x,a.size.y).Contains(point);})
                .OrderBy(p=>parts.FirstOrDefault(t=>t.id==p.catalogId).IsOverlay?0:1).Select(p=>p.instanceId)
                .Concat(adapter.Map.Objects.Where(o=>StageMapDefinition.EnumeratePlacementCells(o).Contains(point)).Select(o=>o.StableId)).ToArray();
            string next=string.Join("|",ids);pickIndex=point==pickCell&&next==pickSignature?(pickIndex+1)%Mathf.Max(1,ids.Length):0;pickCell=point;pickSignature=next;
            state.instanceId=ids.Length==0?"":ids[pickIndex];state.information=ids.Length>0;origin=SelectionOrigin();mapSettings=false;
            status=$"선택 {pickIndex+1}/{ids.Length} · 같은 셀 클릭으로 선택 순환";
        }
        private static TerrainEditorPart SelectionPart()
        {
            if(string.IsNullOrEmpty(state.instanceId))return null;
            var p=adapter?.Read().placements.FirstOrDefault(p=>p.instanceId==state.instanceId);if(p!=null)return parts.FirstOrDefault(t=>t.id==p.catalogId);
            var o=adapter?.Map.Objects.FirstOrDefault(o=>o.StableId==state.instanceId);if(o==null)return null;
            return parts.FirstOrDefault(t=>t.marker==o.Kind);
        }
        private static Vector2Int SelectionOrigin()
        {
            var p=adapter?.Read().placements.FirstOrDefault(p=>p.instanceId==state.instanceId);if(p!=null)return new Vector2Int(p.x,p.y);
            var o=adapter?.Map.Objects.FirstOrDefault(o=>o.StableId==state.instanceId);return o==null?Vector2Int.zero:new Vector2Int(o.X,o.Y);
        }
        public static void DeleteSelected()
        {
            if(state.freeShape){DeleteFreeSelection();return;}
            if(string.IsNullOrEmpty(state.instanceId))throw new InvalidOperationException("완성 instance를 선택하세요.");
            var dto=adapter.Read();if(dto.placements.Any(p=>p.instanceId==state.instanceId))
            {if(!Engine.TryDelete(adapter.Terrain.Catalog,dto,state.instanceId,out var removed,out var message))throw new InvalidOperationException(message);adapter.Commit(removed,"Delete Scene instance");}
            else adapter.CommitObject(new TerrainEditorObjectEdit{operation="Delete",instanceId=state.instanceId});state.instanceId="";Cancel(false);status="전체 삭제 저장 완료";
        }
        private static void ShowGhost()
        {
            var nextGhostId=ghostPart?.id;
            if(ghostPart?.obj!=null && ANIMOL.Gameplay.ObstacleGraphics.ObstacleSnapshotAdapter.Kind(ghostPart.obj.Kind)!=null)
                nextGhostId+="|"+state.freeStyle+"|"+origin+"|"+adapter.Map.AuthoringRevision;
            if(ghostId!=nextGhostId || ghost==null)
            {
                DestroyGhost();ghostId=nextGhostId;
                var parent=new GameObject("Scene placement ghost");SceneManager.MoveGameObjectToScene(parent,scene);ghost=parent;
                if(ghostPart?.terrain!=null)StageTerrainStructureRuntime.CreateArt(parent.transform,new AnimolTerrainPlacement{instanceId="ghost",catalogId=ghostPart.id},ghostPart.terrain,adapter.Terrain.Frame(ghostPart.terrain.frameId),adapter.Map.GetEditorPreviewUnitsPerCell(),100);
                else if(ghostPart?.obj!=null)
                {
                    if(ANIMOL.Gameplay.ObstacleGraphics.ObstacleSnapshotAdapter.Kind(ghostPart.obj.Kind)!=null && objectCandidate!=null && valid)
                        TerrainEditorArt.BuildObstacle(adapter.Map,adapter.ObjectCandidate(objectCandidate),parent.transform);
                    else TerrainEditorArt.CopySprites(ghostPart.obj.Prefab,parent.transform);
                }
                foreach(var r in ghost.GetComponentsInChildren<SpriteRenderer>())
                {r.sortingOrder=100;if(r.sharedMaterial.HasProperty("_PreviewOpacity")){var m=new Material(r.sharedMaterial);m.SetFloat("_PreviewOpacity",.5f);r.sharedMaterial=m;ghostMaterials.Add(m);}else r.color=new Color(1,1,1,.5f);}
                foreach(var t in ghost.GetComponentsInChildren<Transform>())t.gameObject.hideFlags=HideFlags.HideAndDontSave;
            }
            ghost.transform.position=new Vector3(origin.x,origin.y,0)*adapter.Map.GetEditorPreviewUnitsPerCell();ghost.SetActive(hover);
        }
        private static void DestroyGhost(){if(ghost!=null)UnityEngine.Object.DestroyImmediate(ghost);ghost=null;ghostId=null;foreach(var m in ghostMaterials)if(m!=null)UnityEngine.Object.DestroyImmediate(m);ghostMaterials.Clear();}
        private static void DrawWorld()
        {
            float units=adapter.Map.GetEditorPreviewUnitsPerCell();var bounds=adapter.Map.EditorPreviewCellBounds;
            if(state.grid)
            {
                int stepX=Mathf.Max(1,Mathf.CeilToInt(bounds.width/256f)),stepY=Mathf.Max(1,Mathf.CeilToInt(bounds.height/256f));
                for(int x=bounds.xMin;x<=bounds.xMax;x+=stepX){Handles.color=new Color(.3f,.4f,.5f,.3f);Handles.DrawLine(new Vector3(x,bounds.yMin,0)*units,new Vector3(x,bounds.yMax,0)*units);}
                for(int y=bounds.yMin;y<=bounds.yMax;y+=stepY){Handles.color=new Color(.3f,.4f,.5f,.3f);Handles.DrawLine(new Vector3(bounds.xMin,y,0)*units,new Vector3(bounds.xMax,y,0)*units);}
            }
            if(state.freeShape){DrawFreeWorld();return;}
            var selected=SelectionPart();if(selected!=null)DrawBox(new Rect(SelectionOrigin(),selected.size),TerrainEditorScreen.Gold,false);
            if(!hover)return;var entry=ghostPart?.terrain;
            if(entry!=null&&state.masks)
            {for(int row=0;row<entry.height;row++)for(int x=0;x<entry.width;x++)
                {bool solid=entry.solidRows[row][x]=='#',support=entry.supportRows[row][x]=='#';DrawBox(new Rect(origin.x+x,origin.y+entry.height-1-row,1,1),!valid?TerrainEditorScreen.Red:support?TerrainEditorScreen.Blue:solid?TerrainEditorScreen.Green:TerrainEditorScreen.Paper,!solid&&!support);}}
            else DrawBox(new Rect(origin,ghostPart?.size??Vector2.one),valid?TerrainEditorScreen.Green:TerrainEditorScreen.Red,false);
            Handles.color=TerrainEditorScreen.Gold;Handles.DrawSolidDisc((Vector2)origin*units,Vector3.forward,units*.1f);
        }
        private static void DrawBox(Rect rect,Color color,bool dotted)
        {
            float units=adapter.Map.GetEditorPreviewUnitsPerCell();var points=new[]{new Vector3(rect.xMin,rect.yMin,0)*units,new Vector3(rect.xMax,rect.yMin,0)*units,new Vector3(rect.xMax,rect.yMax,0)*units,new Vector3(rect.xMin,rect.yMax,0)*units};
            Handles.color=color;for(int i=0;i<4;i++){if(dotted)Handles.DrawDottedLine(points[i],points[(i+1)%4],4);else Handles.DrawAAPolyLine(2,points[i],points[(i+1)%4]);}
        }
        private static void Run(Action action){try{action();}catch(Exception ex){status=ex.Message;}view?.Repaint();}
    }
    public sealed class TerrainEditorSettingsDraft : ScriptableObject { public StageMapObjectSettings value; }
}
