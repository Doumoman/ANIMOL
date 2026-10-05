using System;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Development
{
    public sealed partial class TerrainEditorScreen
    {
        [SerializeField] private GameObject testRoot;
        [SerializeField] private StageMapDefinition testMap;
        private Camera testCamera;
        private RenderTexture testTarget;
        private RawImage testImage;
        private RectTransform testPanel,testControls;
        private Button testButton;
        private Text testStatus;
        private Vector2Int testScreenSize;
        public StageMapRuntimeLoader TestLoader { get; private set; }
        public DevPlayerController TestPlayer { get; private set; }
        public bool ExitAfterTest { get; set; }
        public void InitializeTestMode()
        {
            testButton=Button("Test",top,"시험 플레이",.72f,.87f,()=>Run(()=>{if(IsTesting)ReturnToEdit();else EnterTest();}));
            testPanel=PanelRect("Test preview",CanvasRect,Ink,new Vector2(0,.035f),new Vector2(1,.945f));
            testImage=new GameObject("Portrait game viewport",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();testImage.transform.SetParent(testPanel,false);testImage.raycastTarget=false;
            var label=Label("Test notice",testPanel,"TEST · ← → / A D 이동 · Space 점프 · Tab 동물\n저장된 맵의 시험 세션",16);Stretch(label.rectTransform,new Vector2(.01f,.88f),new Vector2(.24f,.99f));label.alignment=TextAnchor.UpperLeft;
            testStatus=Label("Test state",testPanel,"",15);Stretch(testStatus.rectTransform,new Vector2(.76f,.7f),new Vector2(.99f,.98f));testStatus.alignment=TextAnchor.UpperLeft;
            testControls=PanelRect("Mobile test controls",testPanel,Color.clear,Vector2.zero,new Vector2(1,.18f));
            testControls.gameObject.AddComponent<DevMobileInputRouter>();
            Touch("Swipe", "좌우 밀기 / 아래 통과", .02f,.45f,MobileTouchAction.SwipeMove);
            Touch("Jump", "점프", .48f,.65f,MobileTouchAction.Jump);
            Touch("Animal", "동물", .68f,.82f,MobileTouchAction.AnimalCycle);
            Button("Reset Test",testControls,"초기화",.84f,.98f,()=>Run(()=>{ReturnToEdit(false);EnterTest();}));
            testPanel.gameObject.SetActive(false);
        }
        private void Touch(string name,string text,float min,float max,MobileTouchAction action)
        {
            var button=Button(name,testControls,text,min,max,()=>{});
            Adapter.ConfigureTouchControl(button.gameObject.AddComponent<DevMobileTouchControl>(),action);
        }
        public void EnterTest()
        {
            if(IsTesting)return;
            CancelPending(false);Adapter.Save();
            var report=StageMapValidator.ValidateStructure(Adapter.Map);
            if(!report.IsValid)throw new InvalidOperationException("시험 시작 불가: "+string.Join(" | ",report.Errors));
            if(Adapter.PlayerPrefab==null)throw new InvalidOperationException("기존 플레이어 프리팹을 찾을 수 없습니다.");
            testMap=Instantiate(Adapter.Map);testMap.name=Adapter.Map.name+" · test snapshot";
            testRoot=new GameObject("V4 Test world");testRoot.transform.SetParent(transform,false);
            try
            {
                testCamera=new GameObject("Test camera",typeof(Camera),typeof(DirectionalCameraFollow)).GetComponent<Camera>();testCamera.transform.SetParent(testRoot.transform,false);testCamera.tag="MainCamera";
                testCamera.scene=testRoot.scene;
                testCamera.cullingMask=~(1<<TerrainEditorArt.PreviewLayer);
                testCamera.clearFlags=CameraClearFlags.SolidColor;testCamera.backgroundColor=Ink;testCamera.orthographic=true;
                testCamera.gameObject.AddComponent<PortraitWorldCameraPolicy>();
                TestLoader=testRoot.AddComponent<StageMapRuntimeLoader>();TestLoader.enabled=false;
                TestLoader.ConfigureStandaloneDevelopmentOwner();TestLoader.ConfigureDefinition(testMap,Adapter.Objects,true);TestLoader.Load(testMap);
                var start=testMap.Objects.FirstOrDefault(o=>o.Kind==StageMapObjectKind.PlayerStart);
                if(start==null)throw new InvalidOperationException("시험 플레이에는 기존 PlayerStart가 필요합니다.");
                var spawn=new Vector3((start.X+.5f)*testMap.WorldUnitsPerCell,(start.Y+.5f)*testMap.WorldUnitsPerCell,0);
                var player=Instantiate(Adapter.PlayerPrefab,testRoot.transform);player.name="V4 Test player";player.transform.position=spawn;
                TestPlayer=player.GetComponent<DevPlayerController>();
                if(testMap.FixedAnimalIds.Count>0)TestPlayer.ConfigureCampaignRoster(testMap.FixedAnimalIds,testMap.InitialAnimalId,testMap.InitialAnimalId);
                testCamera.transform.position=spawn+Vector3.back*20;
                Adapter.ConfigureTestServices(testRoot,spawn);
                editArt.SetActive(false);MapCamera.enabled=false;
                IsTesting=true;Viewport.gameObject.SetActive(false);palette.gameObject.SetActive(false);testPanel.gameObject.SetActive(true);
                foreach(var b in top.GetComponentsInChildren<Button>())if(b.name is "Undo" or "Redo" or "Save")b.interactable=false;
                testButton.GetComponentInChildren<Text>().text="편집 복귀";title.text=Adapter.Map.StageId+" · TEST";
                ResizeTest();SetStatus("시험 플레이 · 정본 편집 잠금 · 편집 복귀로 종료");
            }
            catch{ReturnToEdit();throw;}
        }
        public void ReturnToEdit(bool exitSession=true)
        {
            if(testRoot!=null){TestLoader?.ResetRuntimeObjects();testRoot.SetActive(false);Destroy(testRoot);testRoot=null;}
            if(testMap!=null){Destroy(testMap);testMap=null;}
            if(testTarget!=null){testTarget.Release();Destroy(testTarget);testTarget=null;}
            IsTesting=false;TestLoader=null;TestPlayer=null;testCamera=null;
            if(testPanel!=null)testPanel.gameObject.SetActive(false);
            if(editArt!=null)editArt.SetActive(true);MapCamera.enabled=true;Viewport.gameObject.SetActive(true);palette.gameObject.SetActive(true);
            foreach(var b in top.GetComponentsInChildren<Button>())b.interactable=true;
            testButton.GetComponentInChildren<Text>().text="시험 플레이";title.text=Adapter.Map.StageId+" · EDIT";
            SetStatus("편집 복귀 · 시험 상태 초기화 · revision "+Adapter.Map.AuthoringRevision);RefreshInformation();
            if(ExitAfterTest && exitSession)Adapter.Exit();
        }
        private void UpdateTest()
        {
            if(!IsTesting)return;
            if(testScreenSize.x!=Screen.width || testScreenSize.y!=Screen.height)ResizeTest();
            if(TestPlayer!=null)testStatus.text=$"시험 경과 {testRoot.GetComponent<ObjectLabSession>().ElapsedSeconds:0.0}s\n스태미나 {TestPlayer.Stamina:0}\n접지 {TestPlayer.IsGrounded}\n12셀 가로 시야\n9:16 모바일 미리보기";
        }
        private void ResizeTest()
        {
            Canvas.ForceUpdateCanvases();
            var available=testPanel.rect.size;
            // The same 9:16 portrait viewport and 12-cell camera policy used by the game.
            var rect=SafeAreaLayout.CalculatePortraitViewport(available);
            var screenSafe=SafeAreaLayout.CalculateNormalizedAnchors(Screen.safeArea,new Vector2(Screen.width,Screen.height));
            var min=new Vector2(Mathf.Max(rect.xMin/available.x,screenSafe.x),Mathf.Max(0,screenSafe.y));
            var max=new Vector2(Mathf.Min(rect.xMax/available.x,screenSafe.z),Mathf.Min(1,screenSafe.w));
            Stretch(testImage.rectTransform,min,max);
            Stretch(testControls,new Vector2(min.x,min.y),new Vector2(max.x,Mathf.Min(max.y,min.y+.18f)));
            if(testTarget!=null){testCamera.targetTexture=null;testTarget.Release();Destroy(testTarget);}
            var h=Mathf.Max(64,Mathf.RoundToInt(testPanel.rect.height*CanvasRect.GetComponent<Canvas>().scaleFactor));var w=Mathf.RoundToInt(h*9f/16f);
            testTarget=new RenderTexture(w,h,24){name="V4 Test portrait",filterMode=FilterMode.Point};testTarget.Create();
            testCamera.targetTexture=testTarget;testCamera.aspect=9f/16f;testCamera.GetComponent<PortraitWorldCameraPolicy>().Apply();testImage.texture=testTarget;
            testScreenSize=new Vector2Int(Screen.width,Screen.height);
        }
    }
}
