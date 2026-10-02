using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ANIMOL.Core;
using ANIMOL.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using W = ANIMOL.ProductionV1.ProductionWidgets;

namespace ANIMOL.ProductionV1
{
    public sealed class ApprovedGrowthSnapshot
    {
        public int Coins;
        public readonly Dictionary<string,int> Levels=new Dictionary<string,int>();
    }

    /// <summary>Presentation extension of the existing seven screen containers and navigation stack.</summary>
    [DefaultExecutionOrder(700)]
    public sealed class ProductionController : MonoBehaviour
    {
        public static readonly string[] Screens={"SC02_ThemeSelect","SC03_StageSelect","SC04_StageDetail","SC09_MapLoading","SC10_UpgradeHub","SC10A_AccountUpgrade","SC10B_CharacterUpgrade"};
        [SerializeField] private ProductionCatalog catalog;
        public ProductionCatalog Catalog => catalog;
        public int SelectedTheme { get; private set; }
        public CampaignStageDefinition SelectedStage { get; private set; }
        public ScrollRect StageScroll { get; private set; }
        public bool Loading { get; private set; }
        public int LoadRequests { get; private set; }
        public PurchasePresentation Purchase { get; }=new PurchasePresentation();
        public UiNavigationService Navigation { get; private set; }
        private UiModalStack modals;
        private CampaignUiPresenter legacyCampaign;
        private CampaignProgressionService progression;
        private readonly Dictionary<string,RectTransform> roots=new Dictionary<string,RectTransform>();
        private readonly List<TextMeshProUGUI> themeLabels=new List<TextMeshProUGUI>();
        private readonly List<TextMeshProUGUI> stageLabels=new List<TextMeshProUGUI>();
        private readonly List<Image> stageIcons=new List<Image>();
        private readonly List<TextMeshProUGUI[]> trackLabels=new List<TextMeshProUGUI[]>();
        private readonly List<Button> trackButtons=new List<Button>();
        private readonly List<Image[]> trackGauges=new List<Image[]>();
        private Image background,detailThumb,loadingThumb;
        private TextMeshProUGUI stageTitle,detailTitle,detailInfo,detailAvailability,loadingText,loadingRoster,accountBalance,hubBalance;
        private Button stageStart,loadingBack;
        private GameObject modal;
        private RectTransform modalBody;
        private IAccountGateway gateway=new DisconnectedAccountGateway();
        private UpgradePurchaseUseCase purchaseUseCase;
        private Func<ApprovedGrowthSnapshot> readApprovedSnapshot;
        private ApprovedGrowthSnapshot approved;
        private bool initialized;

        private IEnumerator Start()
        {
            // Let existing PortraitEntryController install its authored lobby first.
            yield return null;
            Initialize();
            yield return null;
            RestoreAuthoredButtonColors();
        }
        private void RestoreAuthoredButtonColors()
        {
            foreach(var b in GetComponentsInChildren<Button>(true).Where(x=>x.name.StartsWith("PV1_",StringComparison.Ordinal)))
            {
                if(b.image!=null)b.image.color=b.name.StartsWith("PV1_Theme_",StringComparison.Ordinal)?Color.clear:Color.white;
                var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1,.97f,.85f);c.selectedColor=c.highlightedColor;c.pressedColor=new Color(.85f,.85f,.85f);c.disabledColor=new Color(.65f,.65f,.65f);b.colors=c;
            }
        }
        public void Initialize()
        {
            if(initialized||Catalog==null)return;
            Navigation=GetComponent<UiNavigationService>();modals=GetComponent<UiModalStack>();
            legacyCampaign=GetComponent<CampaignUiPresenter>();
            // Reuse the campaign presenter's session progression, including any loaded progress.
            progression=legacyCampaign==null?null:typeof(CampaignUiPresenter).GetField("progression",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(legacyCampaign) as CampaignProgressionService;
            if(progression==null&&Catalog.Campaign!=null)progression=new CampaignProgressionService(Catalog.Campaign);
            purchaseUseCase=new UpgradePurchaseUseCase(gateway);
            BindAuthoredHierarchy();
            initialized=true;Navigation.ScreenChanged+=OnScreen;OnScreen(Navigation.CurrentScreenId);
        }

#if UNITY_EDITOR
        public void Author(ProductionCatalog source)
        {
            if(Application.isPlaying)throw new InvalidOperationException("Author in Edit Mode only.");
            catalog=source;Navigation=GetComponent<UiNavigationService>();modals=GetComponent<UiModalStack>();
            progression=Catalog.Campaign==null?null:new CampaignProgressionService(Catalog.Campaign);
            var host=transform.Find("SafeArea/ScreenHost");
            foreach(var id in Screens)
            {
                var screen=host.Find(id);
                if(screen==null)continue;
                // Retain authored assets and presenter components; replace only the displayed children.
                foreach(Transform child in screen)child.gameObject.SetActive(false);
                foreach(var g in screen.GetComponents<Graphic>())g.enabled=false;
                var r=W.Rect(screen,"ProductionV1",0,0,1080,1920);W.Stretch(r);roots[id]=r;
            }
            var bgRoot=W.Rect(transform,"ProductionV1Background",0,0,1080,1920);W.Stretch(bgRoot);bgRoot.SetAsFirstSibling();
            background=W.Art(bgRoot,Catalog,"BG_UI_Campaign_Common",0,0,1080,2160);
            background.rectTransform.anchorMin=background.rectTransform.anchorMax=background.rectTransform.pivot=new Vector2(.5f,.5f);
            background.rectTransform.anchoredPosition=Vector2.zero;
            bgRoot.gameObject.AddComponent<ProductionBackgroundFit>().Surface=background.rectTransform;
            BuildThemes();BuildStages();BuildDetail();BuildLoading();BuildHub();BuildAccount();BuildAnimal();BuildModal();
            var lobby=host.Find("SC01_Lobby");
            if(lobby!=null&&gameObject.scene.name=="Lobby")
            {
                var b=W.Button(lobby,Catalog,"LobbyUpgrade","강화",54,360,264,120,()=>Navigation.Navigate(Screens[4]));
                W.Art(b.transform,Catalog,"UI_Icon_AccountGrowth",20,24,64,64);
                var label=b.GetComponentInChildren<TextMeshProUGUI>();label.rectTransform.anchoredPosition=new Vector2(84,-8);label.rectTransform.sizeDelta=new Vector2(160,104);
            }
            background.transform.parent.gameObject.SetActive(false);
            foreach(var r in roots.Values)r.gameObject.SetActive(true);
        }
#endif
        private void OnDestroy(){if(Navigation!=null)Navigation.ScreenChanged-=OnScreen;}
        private static T Find<T>(Transform root,string name) where T:Component => root.GetComponentsInChildren<T>(true).FirstOrDefault(x=>x.name==name);
        private void Wire(string name,Action action)
        {
            var button=Find<Button>(transform,"PV1_"+name);
            if(button==null)return;
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>action());
        }
        private void BindAuthoredHierarchy()
        {
            var host=transform.Find("SafeArea/ScreenHost");
            foreach(var id in Screens)
            {
                var r=host.Find(id+"/ProductionV1") as RectTransform;
                if(r!=null){roots[id]=r;Wire(id+"Back",Back);}
            }
            background=Find<Image>(transform.Find("ProductionV1Background"),"BG_UI_Campaign_Common");
            modal=transform.Find("SafeArea/ModalHost/ProductionV1Modal").gameObject;
            modalBody=modal.transform.Find("Body") as RectTransform;
            if(!roots.ContainsKey(Screens[0]))return;
            for(int i=0;i<5;i++){int n=i;themeLabels.Add(Find<TextMeshProUGUI>(roots[Screens[0]],"ThemeData_"+i));Wire("Theme_"+i,()=>SelectTheme(n));}
            StageScroll=Find<ScrollRect>(roots[Screens[1]],"StageScroll");stageTitle=Find<TextMeshProUGUI>(roots[Screens[1]],"ThemeName");
            for(int i=0;i<20;i++){int n=i;stageLabels.Add(Find<TextMeshProUGUI>(roots[Screens[1]],"StageState_"+i));Wire("Stage_"+i,()=>SelectStage(n));}
            stageIcons.AddRange(roots[Screens[1]].GetComponentsInChildren<Image>(true).Where(x=>x.name.StartsWith("StageIcon_")));
            detailTitle=Find<TextMeshProUGUI>(roots[Screens[2]],"StageName");detailInfo=Find<TextMeshProUGUI>(roots[Screens[2]],"StageInformation");
            detailAvailability=Find<TextMeshProUGUI>(roots[Screens[2]],"EntryReason");detailThumb=Find<Image>(roots[Screens[2]],"Thumb_Theme_T01");
            stageStart=Find<Button>(roots[Screens[2]],"PV1_StageStart");Wire("StageStart",BeginLoading);Wire("DetailUpgrade",()=>Navigation.Navigate(Screens[4]));
            loadingText=Find<TextMeshProUGUI>(roots[Screens[3]],"LoadStatus");loadingRoster=Find<TextMeshProUGUI>(roots[Screens[3]],"LoadRoster");loadingThumb=Find<Image>(roots[Screens[3]],"Thumb_Theme_T01");
            loadingBack=Find<Button>(roots[Screens[3]],"PV1_LoadingReturn");Wire("LoadingReturn",()=>Navigation.Navigate(Screens[2],false));
            Wire("LobbyUpgrade",()=>Navigation.Navigate(Screens[4]));Wire("Account",()=>Navigation.Navigate(Screens[5]));Wire("Character",()=>Navigation.Navigate(Screens[6]));
            Wire("Help",()=>ShowState(ProductionState.Unconfigured,"계정 공통 강화와 동물별 액티브·패시브 성장입니다. 운영 정책과 서비스 연결 후 구매할 수 있습니다."));
            hubBalance=Find<TextMeshProUGUI>(roots[Screens[4]],"Balance");accountBalance=Find<TextMeshProUGUI>(roots[Screens[5]],"Balance");
            var texts=roots[Screens[5]].GetComponentsInChildren<TextMeshProUGUI>(true);
            var levels=texts.Where(t=>t.name=="Level").ToArray();var effects=texts.Where(t=>t.name=="Effect").ToArray();var costs=texts.Where(t=>t.name=="Cost").ToArray();
            var gauges=roots[Screens[5]].GetComponentsInChildren<Image>(true).Where(x=>x.name=="Gauge").ToArray();int start=0;
            for(int i=0;i<levels.Length;i++)
            {
                int n=i;trackLabels.Add(new[]{levels[i],effects[i],costs[i]});trackButtons.Add(Find<Button>(roots[Screens[5]],"PV1_Buy_"+i));Wire("Buy_"+i,()=>ConfirmAccount(n));
                int count=Catalog.Account?.Tracks.ElementAtOrDefault(i)?.MaxLevel??0;trackGauges.Add(gauges.Skip(start).Take(count).ToArray());start+=count;
            }
        }
        private void Update()
        {
            if(!initialized||!roots.ContainsKey(Navigation.CurrentScreenId))return;
            if(Input.GetKeyDown(KeyCode.Escape))Back();
        }
        public void Back()
        {
            if(modals!=null&&modals.Pop())return;
            if(Loading)return; // Unity scene activation cannot be cancelled safely.
            Navigation.Back();
        }
        private void OnScreen(string id)
        {
            bool own=roots.ContainsKey(id);
            if(legacyCampaign!=null)legacyCampaign.enabled=!own;
            background.transform.parent.gameObject.SetActive(own);
            if(!own)return;
            string bg=id==Screens[0]?"BG_UI_Campaign_Common":id==Screens[4]||id==Screens[5]||id==Screens[6]?"BG_UI_Growth_Common":"BG_UI_Campaign_T"+(SelectedTheme+1).ToString("00");
            background.sprite=Catalog.Find(bg);
            if(id==Screens[0])RefreshThemes();
            if(id==Screens[4]||id==Screens[5])RefreshAccount();
        }
        private RectTransform Header(string id,string title,string subtitle)
        {
            if(!roots.TryGetValue(id,out var r))return null;
            W.Panel(r,Catalog,54,20,972,228);
            W.Button(r,Catalog,id+"Back","",72,56,156,156,Back);
            W.Art(r,Catalog,"UI_Icon_Back",114,98,72,72);
            W.Text(r,Catalog,"Title",title,258,62,704,80,60);
            W.Text(r,Catalog,"Subtitle",subtitle,260,146,702,54,28);
            return r;
        }
        private void BuildThemes()
        {
            var r=Header(Screens[0],"캠페인","테마를 선택해 스테이지를 확인하세요");if(r==null)return;
            var scroll=W.Scroll(r,"ThemeScroll",266,32,1700);
            for(int i=0;i<5;i++)
            {
                int index=i;float y=12+i*330;
                var paper=W.Rect(scroll.content,"ThemePaper",90,y+60,900,178).gameObject.AddComponent<Image>();paper.color=new Color32(244,244,244,255);paper.raycastTarget=false;
                W.Art(scroll.content,Catalog,"UI_Campaign_ThemeCard_Frame",54,y,972,310,true);
                W.Art(scroll.content,Catalog,"Thumb_Theme_T"+(i+1).ToString("00"),90,y+54,350,175);
                themeLabels.Add(W.Text(scroll.content,Catalog,"ThemeData_"+i,"",470,y+60,508,170,32));
                var hit=W.Rect(scroll.content,"PV1_Theme_"+i,54,y,972,310).gameObject.AddComponent<Image>();hit.color=Color.clear;
                hit.gameObject.AddComponent<Button>().onClick.AddListener(()=>SelectTheme(index));
            }
            RefreshThemes();
        }
        private void RefreshThemes()
        {
            for(int i=0;i<themeLabels.Count;i++)
            {
                var t=Catalog.Campaign?.Themes.ElementAtOrDefault(i);
                if(t==null){themeLabels[i].text="테마 데이터 없음";continue;}
                int count=t.Stages.Count(s=>s!=null&&progression.Progress.IsCompleted(s.StageId));
                bool unlocked=progression.IsUnlocked(t.Stages.FirstOrDefault());
                themeLabels[i].text=$"{t.DisplayName}\n{count}/{t.Stages.Count} 완료\n"+(unlocked?"열림 · 상세 확인":"잠김 · 선행 단계 필요");
            }
        }
        public void SelectTheme(int index)
        {
            if(Catalog.Campaign==null||index<0||index>=Catalog.Campaign.Themes.Count)return;
            bool changed=SelectedTheme!=index;SelectedTheme=index;RefreshStages();
            if(changed&&StageScroll!=null)StageScroll.verticalNormalizedPosition=1;
            Navigation.Navigate(Screens[1]);
        }
        private void BuildStages()
        {
            var r=Header(Screens[1],"스테이지 선택","잠금과 콘텐츠 준비 상태를 확인하세요");if(r==null)return;
            stageTitle=W.Text(r,Catalog,"ThemeName","",80,265,920,90,42);stageTitle.color=Color.white;
            StageScroll=W.Scroll(r,"StageScroll",374,44,1510);
            W.Panel(StageScroll.content,Catalog,54,0,972,1460);
            for(int i=0;i<20;i++)
            {
                int index=i;float x=78+(i%4)*234,y=50+(i/4)*274;
                var b=W.Button(StageScroll.content,Catalog,"Stage_"+i,(i+1).ToString("00"),x,y,220,132,()=>SelectStage(index));
                b.image.sprite=Catalog.Find("UI_Campaign_StageNode_Frame");
                stageLabels.Add(W.Text(StageScroll.content,Catalog,"StageState_"+i,"",x,y+140,220,94,30,true));
                var icon=W.Art(StageScroll.content,Catalog,"UI_Icon_Lock",x+80,y+228,52,52);icon.name="StageIcon_"+i;stageIcons.Add(icon);
            }
            RefreshStages();
        }
        private void RefreshStages()
        {
            var t=Catalog.Campaign?.Themes.ElementAtOrDefault(SelectedTheme);if(t==null)return;
            if(stageTitle!=null)stageTitle.text=t.DisplayName+" · "+t.Stages.Count+"개 스테이지";
            for(int i=0;i<stageLabels.Count;i++)
            {
                var s=t.Stages.ElementAtOrDefault(i);bool unlocked=progression.IsUnlocked(s);
                bool complete=s!=null&&progression.Progress.IsCompleted(s.StageId);
                bool ready=s!=null&&ContentAvailabilityResolver.Resolve(s)==ContentAvailability.Ready;
                string state=s==null?"데이터 없음":complete?"클리어":!unlocked?"잠김":ready?"입장 가능":"제작 중";
                stageLabels[i].text=state;
                stageIcons[i].sprite=Catalog.Find(complete?"UI_Icon_Check":!unlocked?"UI_Icon_Lock":ready?"UI_Icon_Ready":"UI_Icon_Construction");
            }
        }
        public void SelectStage(int index)
        {
            SelectedStage=Catalog.Campaign?.Themes.ElementAtOrDefault(SelectedTheme)?.Stages.ElementAtOrDefault(index);
            if(SelectedStage==null)return;RefreshDetail();Navigation.Navigate(Screens[2]);
        }
        private void BuildDetail()
        {
            var r=Header(Screens[2],"스테이지 상세","사용 동물은 스테이지 고정 목록입니다");if(r==null)return;
            var body=W.Scroll(r,"DetailScroll",270,196,1450).content;
            W.Panel(body,Catalog,54,0,972,1430);
            detailTitle=W.Text(body,Catalog,"StageName","스테이지를 선택하세요",92,26,896,110,44);
            detailThumb=W.Art(body,Catalog,"Thumb_Theme_T01",120,158,840,420);
            detailInfo=W.Text(body,Catalog,"StageInformation","--",96,608,888,492,34);
            detailAvailability=W.Text(body,Catalog,"EntryReason","",96,1120,888,250,30);
            var footer=W.Rect(r,"Actions",0,0,1080,170);footer.anchorMin=footer.anchorMax=new Vector2(0,0);footer.anchoredPosition=new Vector2(0,176);
            W.Button(footer,Catalog,"DetailUpgrade","강화 보기",54,8,330,140,()=>Navigation.Navigate(Screens[4]));
            stageStart=W.Button(footer,Catalog,"StageStart","게임 시작",414,8,612,140,BeginLoading,true);stageStart.interactable=false;
        }
        public static string RosterText(CampaignStageDefinition s)
        {
            if(s==null)return "사용 동물 --";
            var names=s.FixedAnimalDefinitions.Where(x=>x!=null&&!x.AnimalId.StartsWith("DEV_",StringComparison.Ordinal)).Select(x=>x.KoreanDisplayName).ToArray();
            string initial=s.InitialAnimalDefinition!=null&&!s.InitialAnimalDefinition.AnimalId.StartsWith("DEV_",StringComparison.Ordinal)?s.InitialAnimalDefinition.KoreanDisplayName:"미설정";
            return "고정 사용 동물: "+(names.Length>0?string.Join(", ",names):"미설정")+"\n시작 동물: "+initial+" · 읽기 전용";
        }
        private void RefreshDetail()
        {
            var s=SelectedStage;if(s==null||detailInfo==null)return;
            detailTitle.text=s.StageId+" · "+(string.IsNullOrWhiteSpace(s.DisplayName)?"스테이지":s.DisplayName);
            detailThumb.sprite=Catalog.Find("Thumb_Theme_T"+(SelectedTheme+1).ToString("00"));
            string rules=s.MapDefinition==null?"제한시간 미설정":$"제한시간 {s.MapDefinition.MainTimeLimitSeconds:0.#}초 · 출구 후 {s.MapDefinition.EscapeTimeLimitSeconds:0.#}초";
            string record=progression.Progress.TryGetBestTime(s.StageId,out var best)?$"최고 기록 {best:0.00}초":"최고 기록: 기록 없음";
            detailInfo.text=$"목표: 방울 {s.TargetBubbleCount}개 수집 → 출구 개방\n실제 출구에 도착하면 완료\n\n{RosterText(s)}\n\n{rules}\n체크포인트: {(s.RuntimePolicy==null?"미설정":s.RuntimePolicy.CheckpointIds.Count.ToString())}\n선택 목표: {(s.RuntimePolicy==null||s.RuntimePolicy.OptionalObjectives.Count==0?"미설정":s.RuntimePolicy.OptionalObjectives.Count+"개")}\n{record}\n보상: 승인된 보상 정책 확인 필요";
            bool unlocked=progression.IsUnlocked(s),ready=ContentAvailabilityResolver.Resolve(s)==ContentAvailability.Ready;
            string reason=ContentAvailabilityResolver.GetBlockingReason(s);
            if(reason.Contains("approved")||reason.Contains("review"))reason="보상 정책 승인과 맵 검수가 완료되지 않았습니다.";
            detailAvailability.text=!unlocked?"잠김 · 선행 스테이지 "+s.PrerequisiteStageId+" 완료 필요":ready?"입장 준비 완료":"제작 중 · "+reason;
            stageStart.interactable=unlocked&&ready&&!Loading;
        }
        private void BuildLoading()
        {
            var r=Header(Screens[3],"스테이지 준비","실제 씬 준비 상태를 표시합니다");if(r==null)return;
            W.Panel(r,Catalog,54,302,972,1110);
            loadingThumb=W.Art(r,Catalog,"Thumb_Theme_T01",120,364,840,420);
            var im=W.Art(r,Catalog,"UI_Icon_Loading",452,830,176,176);im.gameObject.AddComponent<ProductionLoadingAnimation>().Frames=Catalog.LoadingFrames;
            loadingText=W.Text(r,Catalog,"LoadStatus","준비 중",100,1030,880,130,44,true);
            loadingRoster=W.Text(r,Catalog,"LoadRoster","사용 동물 --",110,1190,860,162,34,true);
            loadingBack=W.Button(r,Catalog,"LoadingReturn","상세로 돌아가기",150,1450,780,132,()=>Navigation.Navigate(Screens[2],false));
        }
        public void BeginLoading()
        {
            if(Loading||SelectedStage==null)return;
            if(!progression.IsUnlocked(SelectedStage)){ShowState(ProductionState.Unconfigured,"선행 스테이지를 완료해야 합니다.");return;}
            Loading=true;LoadRequests++;stageStart.interactable=false;Navigation.Navigate(Screens[3]);
            loadingText.text="준비 중";loadingBack.interactable=false;loadingRoster.text=RosterText(SelectedStage);loadingThumb.sprite=detailThumb.sprite;
            StartCoroutine(LoadSelected());
        }
        private IEnumerator LoadSelected()
        {
            yield return null;yield return new WaitForEndOfFrame();
            AsyncOperation operation=null;string error=null;
            try
            {
                var snapshot=new StageLaunchUseCase().Launch(SelectedStage);
                if(!Application.CanStreamedLevelBeLoaded(snapshot.Setup.ThemeRuntimeSceneName))throw new InvalidOperationException("게임 씬이 빌드 목록에 없습니다.");
                CampaignLaunchContext.Set(snapshot);
                operation=SceneManager.LoadSceneAsync(snapshot.Setup.ThemeRuntimeSceneName);
                if(operation==null)throw new InvalidOperationException("씬 로딩 요청을 생성하지 못했습니다.");
            }
            catch(Exception e){error=e.Message;CampaignLaunchContext.Clear();}
            if(error!=null){LoadFailed(error);yield break;}
            while(!operation.isDone){loadingText.text=$"{SelectedStage.StageId}\n씬 준비 {operation.progress*100:0}%";yield return null;}
        }
        private void LoadFailed(string reason)
        {
            Loading=false;loadingText.text="진입 준비 실패";loadingBack.interactable=true;RefreshDetail();
            if(reason.StartsWith("Stage content is not ready",StringComparison.Ordinal))reason="스테이지 콘텐츠의 승인 및 검수가 완료되지 않았습니다. 상세 화면에서 준비 상태를 확인해 주세요.";
            ShowState(ProductionState.LoadingFailure,reason,BeginLoading);
        }
        private void BuildHub()
        {
            var r=Header(Screens[4],"강화","공통 성장과 동물별 성장을 확인하세요");if(r==null)return;
            var b=W.Scroll(r,"HubScroll",270,34,1500).content;
            W.Panel(b,Catalog,54,0,972,154);W.Art(b,Catalog,"UI_Icon_Coin",92,40,72,72);
            hubBalance=W.Text(b,Catalog,"Balance","보유 코인 -- · 조회 불가",187,36,770,82,38);
            W.Panel(b,Catalog,54,190,972,450);W.Art(b,Catalog,"UI_Icon_AccountGrowth",95,235,128,128);
            W.Text(b,Catalog,"AccountInfo","계정 공통 강화\n최대 스테미나 · 재생 속도 · 소비량 감소\n현재 레벨 -- / 서버 조회 필요",250,224,700,236,36);
            W.Button(b,Catalog,"Account","계정 공통 강화",108,480,864,132,()=>Navigation.Navigate(Screens[5]),true);
            W.Panel(b,Catalog,54,684,972,450);W.Art(b,Catalog,"UI_Icon_Animal",95,735,128,128);
            W.Text(b,Catalog,"AnimalInfo","동물별 강화\n액티브 · 패시브 성장\n효과와 가격이 설정된 동물만 강화 가능",250,724,700,236,36);
            W.Button(b,Catalog,"Character","동물별 강화",108,976,864,132,()=>Navigation.Navigate(Screens[6]),true);
            W.Panel(b,Catalog,54,1180,972,220);
            W.Text(b,Catalog,"Service","계정 서비스 미연결\n정보를 열람할 수 있으며 구매는 연결 후 가능합니다.",94,1220,890,132,34);
            W.Button(b,Catalog,"Help","성장 안내",180,1410,720,132,()=>ShowState(ProductionState.Unconfigured,"공통 강화는 계정 성장, 동물별 강화는 각 동물의 액티브·패시브 성장입니다.\n모드별 적용 범위는 확정된 운영 정책을 따릅니다."));
        }
        private void BuildAccount()
        {
            var r=Header(Screens[5],"계정 공통 강화","현재와 다음 효과 · 비용을 구분합니다");if(r==null)return;
            var b=W.Scroll(r,"AccountScroll",270,32,1580).content;
            W.Panel(b,Catalog,54,0,972,154);W.Art(b,Catalog,"UI_Icon_Coin",92,40,72,72);
            accountBalance=W.Text(b,Catalog,"Balance","보유 -- · 조회 불가",187,36,770,82,38);
            var icons=new[]{"UI_Growth_Icon_MaxStamina","UI_Growth_Icon_StaminaRegen","UI_Growth_Icon_StaminaEfficiency"};
            for(int i=0;i<3;i++)
            {
                int index=i;float y=182+i*424;var t=Catalog.Account?.Tracks.ElementAtOrDefault(i);
                W.Panel(b,Catalog,54,y,972,400);W.Art(b,Catalog,icons[i],90,y+36,96,96);
                W.Text(b,Catalog,"TrackTitle",t?.DisplayName??"트랙 미설정",208,y+32,730,72,46);
                var level=W.Text(b,Catalog,"Level","",96,y+112,868,56,32);
                var effect=W.Text(b,Catalog,"Effect","",96,y+173,864,78,32);
                var cost=W.Text(b,Catalog,"Cost","",96,y+286,542,78,30);
                trackLabels.Add(new[]{level,effect,cost});
                int steps=t==null?0:Mathf.Clamp(t.MaxLevel,0,100);var gauges=new Image[steps];
                for(int j=0;j<steps;j++){gauges[j]=W.Rect(b,"Gauge",96+j*528f/Mathf.Max(1,steps),y+258,528f/Mathf.Max(1,steps)-5,16).gameObject.AddComponent<Image>();gauges[j].color=new Color32(148,176,194,255);gauges[j].raycastTarget=false;}trackGauges.Add(gauges);
                trackButtons.Add(W.Button(b,Catalog,"Buy_"+i,"조회 필요",674,y+258,302,132,()=>ConfirmAccount(index)));
            }
            W.Text(b,Catalog,"CatalogNotice",Catalog.Account?.PrototypeValues==true?"현재 카탈로그는 밸런스 초안입니다.\n운영 승인과 계정 연결 후 구매할 수 있습니다.":"서버가 확정한 레벨과 잔액만 반영합니다.",84,1466,912,105,32,true).color=Color.white;
        }
        public void BindAccountGateway(IAccountGateway value,Func<ApprovedGrowthSnapshot> reader)
        {
            gateway=value??new DisconnectedAccountGateway();readApprovedSnapshot=reader;
            purchaseUseCase=new UpgradePurchaseUseCase(gateway);RefreshAccount();
        }
        public void RefreshAccount()
        {
            try{approved=gateway.IsConnected?readApprovedSnapshot?.Invoke():null;}catch{approved=null;}
            string balance=approved==null?"보유 코인 -- · 조회 불가":"보유 코인 "+approved.Coins.ToString("N0");
            if(accountBalance!=null)accountBalance.text=balance;if(hubBalance!=null)hubBalance.text=balance;
            for(int i=0;i<trackLabels.Count;i++)
            {
                var t=Catalog.Account?.Tracks.ElementAtOrDefault(i);int level=0;
                bool known=t!=null&&approved!=null&&approved.Levels.TryGetValue(t.TrackId,out level);
                bool configured=t!=null&&t.CostByLevel.Count>=t.MaxLevel&&t.PercentByLevel.Count>t.MaxLevel;
                bool max=known&&level>=t.MaxLevel;
                trackLabels[i][0].text=known?$"현재 Lv.{level} → "+(max?"MAX":$"다음 Lv.{level+1}")+$" / 상한 {t.MaxLevel}":"현재 Lv.-- → 다음 Lv.-- / 상한 "+(t==null?"미설정":t.MaxLevel.ToString())+" (초안)";
                trackLabels[i][1].text=known&&configured?$"현재 효과 {t.PercentByLevel[Mathf.Clamp(level,0,t.MaxLevel)]:0.##}% → "+(max?"MAX":$"다음 {t.PercentByLevel[level+1]:0.##}%"):"현재 효과 -- → 다음 효과 --";
                trackLabels[i][2].text=!configured?"효과·가격 미설정":!known?"필요 코인 --\n보유 -- · 조회 필요":max?"최대 레벨":"필요 "+t.CostForNextLevel(level).ToString("N0")+" / 보유 "+approved.Coins.ToString("N0");
                string label=Purchase.Pending?"처리 중":Purchase.ResultUnknown?"결과 미확정":max?"MAX":!configured?"미설정":Catalog.Account.PrototypeValues?"운영 미설정":!known?"조회 필요":approved.Coins<t.CostForNextLevel(level)?"코인 부족":"강화";
                trackButtons[i].GetComponentInChildren<TextMeshProUGUI>().text=label;
                trackButtons[i].interactable=known&&configured&&!max&&!Catalog.Account.PrototypeValues&&!Purchase.Pending&&!Purchase.ResultUnknown;
                for(int j=0;j<trackGauges[i].Length;j++)trackGauges[i][j].color=known&&j<level?new Color32(56,183,100,255):new Color32(148,176,194,255);
            }
        }
        private void ConfirmAccount(int index)
        {
            RefreshAccount();var t=Catalog.Account.Tracks[index];
            if(Purchase.Pending||Purchase.ResultUnknown||approved==null||!approved.Levels.TryGetValue(t.TrackId,out var level)||Catalog.Account.PrototypeValues)return;
            if(level>=t.MaxLevel){ShowState(ProductionState.Max,t.DisplayName);return;}
            if(approved.Coins<t.CostForNextLevel(level)){ShowState(ProductionState.Insufficient,$"필요 {t.CostForNextLevel(level):N0} / 보유 {approved.Coins:N0}\n부족 {t.CostForNextLevel(level)-approved.Coins:N0}");return;}
            ShowState(ProductionState.Confirm,$"{t.DisplayName}\nLv.{level} → Lv.{level+1}\n효과 {t.PercentByLevel[level]:0.##}% → {t.PercentByLevel[level+1]:0.##}%\n필요 코인 {t.CostForNextLevel(level):N0}\n보유 코인 {approved.Coins:N0}",()=>StartCoroutine(RequestAccount(t,level)));
        }
        private IEnumerator RequestAccount(AccountUpgradeTrackDefinition track,int expected)
        {
            if(!Purchase.Begin())yield break;RefreshAccount();ShowState(ProductionState.Processing,"서버의 확정 응답을 기다리고 있습니다.");yield return null;
            AccountRequestResult result;
            try{result=purchaseUseCase.Request(Purchase.TransactionId,track,expected,Catalog.Account.Version);}
            catch{Purchase.MarkUnknown();RefreshAccount();ShowState(ProductionState.UnknownResult,"응답이 끊겼습니다. 구매 결과를 다시 조회하기 전에는 재요청하지 않습니다.");yield break;}
            Purchase.Complete(result.Status);RefreshAccount();
            bool confirmed=result.Status==AccountRequestStatus.Approved&&approved!=null&&approved.Levels.TryGetValue(track.TrackId,out var level)&&level>expected;
            if(confirmed)ShowState(ProductionState.Success,$"{track.DisplayName} Lv.{approved.Levels[track.TrackId]}\n보유 코인 {approved.Coins:N0}");
            else if(result.Status==AccountRequestStatus.Approved){Purchase.MarkUnknown();ShowState(ProductionState.UnknownResult,"요청 승인 후 갱신된 계정 정보를 조회하지 못했습니다.");}
            else ShowState(Purchase.ResultUnknown?ProductionState.UnknownResult:ProductionState.ConnectionFailure,result.Message);
        }
        private void BuildAnimal()
        {
            var r=Header(Screens[6],"동물별 강화","액티브 · 패시브 / 캠페인 편성 변경 없음");if(r==null)return;
            var b=W.Scroll(r,"AnimalScroll",270,30,1560).content;
            var rabbit=Catalog.Campaign?.EnumerateStages().SelectMany(s=>s.FixedAnimalDefinitions).FirstOrDefault(a=>a!=null&&a.AnimalId=="RABBIT");
            var contract=rabbit==null?null:Catalog.Growth?.AnimalGrowth.FirstOrDefault(a=>a.AnimalId==rabbit.AnimalId);
            W.Panel(b,Catalog,54,0,972,363);W.Art(b,Catalog,"UI_Character_Rabbit_Portrait",94,20,256,320);
            W.Text(b,Catalog,"AnimalName",rabbit==null?"토끼 · 아트 미리보기":rabbit.KoreanDisplayName,366,40,598,80,52);
            W.Text(b,Catalog,"AnimalDescription","동물 상세\n"+(contract==null?"성장 데이터 미설정":"성장 계약 확인 필요")+"\n캠페인 사용 동물은 스테이지 고정",366,136,598,175,32);
            for(int i=0;i<2;i++)
            {
                float y=390+i*416;var kind=i==0?AnimalGrowthTrackKind.Active:AnimalGrowthTrackKind.Passive;
                var track=contract?.Tracks.FirstOrDefault(t=>t.Kind==kind);
                W.Panel(b,Catalog,54,y,972,392);W.Art(b,Catalog,i==0?"UI_Icon_Active":"UI_Icon_Passive",94,y+40,96,96);
                W.Text(b,Catalog,"TrackName",i==0?"액티브 성장":"패시브 성장",208,y+42,744,74,48);
                W.Text(b,Catalog,"Level","현재 Lv.-- → 다음 Lv.--\n상한 "+(track==null?"미설정":track.MaxLevel.ToString()),94,y+128,874,94,34);
                W.Text(b,Catalog,"AnimalCost","현재 / 다음 효과 --\n필요 코인 -- · 숙련도 --",94,y+239,544,114,30);
                var button=W.Button(b,Catalog,"AnimalPurchase_"+i,track?.IsConfigured==true?"조회 필요":"미설정",686,y+238,294,132,null);button.interactable=false;
            }
            W.Panel(b,Catalog,54,1240,972,252);
            W.Art(b,Catalog,"UI_Icon_Coin",92,1274,64,64);W.Text(b,Catalog,"AnimalCoins","보유 코인 --",172,1274,380,64,34);
            W.Art(b,Catalog,"UI_Growth_Icon_Mastery",562,1274,64,64);W.Text(b,Catalog,"Mastery","보유 숙련도 --",642,1274,335,64,32);
            W.Text(b,Catalog,"AnimalReason","동물별 효과·가격·숙련도 정책과 서비스 연결이 필요합니다.",94,1365,890,88,30);
        }
        private void BuildModal()
        {
            var host=transform.Find("SafeArea/ModalHost");if(host==null)return;
            var r=W.Rect(host,"ProductionV1Modal",0,0,1080,1920);W.Stretch(r);modal=r.gameObject;
            var dim=modal.AddComponent<Image>();dim.color=new Color(26/255f,28/255f,44/255f,.8f);dim.raycastTarget=true;
            modalBody=W.Rect(r,"Body",54,0,972,950);modalBody.anchorMin=modalBody.anchorMax=new Vector2(.5f,.5f);modalBody.pivot=new Vector2(.5f,.5f);modalBody.anchoredPosition=Vector2.zero;
            W.Panel(modalBody,Catalog,0,0,972,950);
            W.Text(modalBody,Catalog,"StateTitle","",64,66,844,96,56,true);
            var icon=W.Art(modalBody,Catalog,"UI_Icon_Info",820,70,72,72);icon.name="StateIcon";icon.gameObject.AddComponent<ProductionLoadingAnimation>().Frames=Catalog.LoadingFrames;
            W.Text(modalBody,Catalog,"StateMessage","",72,196,828,490,36);
            W.Button(modalBody,Catalog,"ModalConfirm","",504,730,396,140,null,true);
            W.Button(modalBody,Catalog,"ModalClose","",72,730,396,140,null);
            modal.SetActive(false);
        }
        public void ShowState(ProductionState state,string message,Action confirm=null)
        {
            if(modal==null)return;
            if(modal.activeSelf)modals.Pop();

            string title=state==ProductionState.Confirm?"강화 확인":state==ProductionState.Processing?"처리 중":state==ProductionState.Success?"강화 완료":state==ProductionState.Insufficient?"재화 부족":state==ProductionState.Max?"MAX":state==ProductionState.Unconfigured?"콘텐츠 미설정":state==ProductionState.UnknownResult?"구매 결과 미확정":state==ProductionState.LoadingFailure?"진입 준비 실패":"연결 실패";
            Find<TextMeshProUGUI>(modalBody,"StateTitle").text=title;
            Find<TextMeshProUGUI>(modalBody,"StateMessage").text=message;
            var icon=Find<Image>(modalBody,"StateIcon");icon.GetComponent<ProductionLoadingAnimation>().enabled=state==ProductionState.Processing;
            icon.sprite=Catalog.Find(state==ProductionState.Success||state==ProductionState.Max?"UI_Icon_Check":state==ProductionState.ConnectionFailure||state==ProductionState.LoadingFailure||state==ProductionState.UnknownResult?"UI_Icon_Error":"UI_Icon_Info");
            var accept=Find<Button>(modalBody,"PV1_ModalConfirm");
            accept.gameObject.SetActive(confirm!=null);accept.onClick.RemoveAllListeners();
            accept.GetComponentInChildren<TextMeshProUGUI>().text=state==ProductionState.LoadingFailure?"재시도":"확인";
            if(confirm!=null)accept.onClick.AddListener(()=>{modals.Pop();confirm();});
            var close=Find<Button>(modalBody,"PV1_ModalClose");close.onClick.RemoveAllListeners();
            ((RectTransform)close.transform).anchoredPosition=new Vector2(confirm==null?288:72,-730);
            close.GetComponentInChildren<TextMeshProUGUI>().text=state==ProductionState.Confirm?"취소":"닫기";
            close.onClick.AddListener(()=>modals.Pop());
            modals.Push(modal.name);
        }
    }
}
