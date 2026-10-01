using System;
using ANIMOL.PortraitArtV1;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.FiveThemeMenu
{
    /// <summary>Decorative menu only. Never writes gameplay time, inputs, rewards or scene assets.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class MenuThemeCycle : MonoBehaviour
    {
        public const double ThemeSeconds=5, FadeSeconds=.5;
        public const int Scale=4, CropLeft=41;
        public const int HorizontalPanDots=30, VerticalPanDots=4;
        private static double epoch=-1;
        private static uint sessionSeed;
        [SerializeField] private MenuThemeCatalog catalog;
        private PortraitEntryController entry;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private Image outgoing, incoming, outgoingLight, incomingLight, rabbit;
        [SerializeField] private bool authoredLayout;
        private bool ownsRuntimeViewport;
        public bool HasAuthoredLayout => authoredLayout && viewport!=null && outgoing!=null && incoming!=null && outgoingLight!=null && incomingLight!=null && rabbit!=null;
        public MenuThemeCatalog Catalog => catalog;
        public Image Rabbit => rabbit;
        public Image Outgoing => outgoing;
        public Image Incoming => incoming;
        public bool IsVisible => viewport!=null && viewport.gameObject.activeSelf;
        public double Elapsed => Math.Max(0,Time.realtimeSinceStartupAsDouble-epoch);
        public double DisplayTime { get; private set; }
        public int ThemeIndex { get; private set; }
        public float Blend { get; private set; }
        public bool Leftward { get; private set; }
        // Explicit QA override: absent in normal operation; does not affect shared session clock.
        public double? InspectionTime { get; set; }
        public bool? InspectionLeftward { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { epoch=-1;sessionSeed=unchecked((uint)Environment.TickCount);SceneManager.sceneLoaded-=Install; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Subscribe() { SceneManager.sceneLoaded-=Install;SceneManager.sceneLoaded+=Install; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallCurrent() => Install(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        private static void Install(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="Bootstrap" && scene.name!="Lobby") return;
            foreach(var root in scene.GetRootGameObjects()) foreach(var nav in root.GetComponentsInChildren<UiNavigationService>(true)) {
                if(nav.GetComponent<Canvas>()==null || nav.transform.Find("SafeArea/ScreenHost")==null || nav.GetComponentInChildren<MenuThemeCycle>(true)!=null) continue;
                var go=new GameObject("FiveThemeMenuCycle");go.transform.SetParent(nav.transform,false);go.AddComponent<MenuThemeCycle>();
            }
        }
        private void Awake()
        {
            if(catalog==null) catalog=Resources.Load<MenuThemeCatalog>("ANIMOLFiveThemeMenu");
            if(catalog==null) { enabled=false;return; }
            if(epoch<0) epoch=Time.realtimeSinceStartupAsDouble;
            if(HasAuthoredLayout) return;
            CreateVisuals();ownsRuntimeViewport=true;
        }
        // Editor authoring calls this once; normal scene playback never builds UI.
        public void AuthorLayout(MenuThemeCatalog source)
        {
            catalog=source;
            if(!HasAuthoredLayout) CreateVisuals();
            authoredLayout=true;RenderAt(2.25);
        }
        private void CreateVisuals()
        {
            // The screen already clips oversized artwork. RectMask2D introduces a
            // one-pixel soft edge in UI/Default, breaking the exact 4x4 edge blocks.
            viewport=new GameObject("FiveThemeBackdrop",typeof(RectTransform)).GetComponent<RectTransform>();
            viewport.SetParent(transform.parent,false);
            viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=viewport.offsetMax=Vector2.zero;
            // Background goes immediately behind the existing SafeArea and all its UI.
            viewport.SetSiblingIndex(transform.parent.Find("SafeArea").GetSiblingIndex());
            outgoing=MakeImage("OutgoingScene",viewport);outgoingLight=MakeImage("OutgoingLight",outgoing.transform);
            incoming=MakeImage("IncomingScene",viewport);incomingLight=MakeImage("IncomingLight",incoming.transform);
            rabbit=MakeImage("DecorativeRabbit",viewport);rabbit.rectTransform.sizeDelta=new Vector2(96,128);
            rabbit.rectTransform.pivot=new Vector2(.5f,.5f);
        }
        private void OnDestroy() { if(ownsRuntimeViewport && viewport!=null) Destroy(viewport.gameObject); }
        private void LateUpdate()
        {
            if(catalog==null) return;
            if(entry==null) {
                entry=transform.parent.GetComponentInChildren<PortraitEntryController>(true);
                if(entry!=null && !authoredLayout) MenuThemeSkin.Apply(entry,catalog);
            }
            if(entry==null) return;
            bool visible=entry.Navigation.CurrentScreenId==PortraitEntryController.StartId || entry.Navigation.CurrentScreenId==PortraitEntryController.ModeId;
            viewport.gameObject.SetActive(visible);
            if(!visible) return;
            RenderAt(InspectionTime ?? Elapsed);
        }
        public static int IndexAt(double seconds) => (int)(Math.Floor(Math.Max(0,seconds)/ThemeSeconds)%5);
        public static float FadeAt(double seconds) => Mathf.Clamp01((float)((Math.Max(0,seconds)%ThemeSeconds-(ThemeSeconds-FadeSeconds))/FadeSeconds));
        public static Vector2Int PanAt(double seconds,long segment)
        {
            // Incoming lives for 0.5 s BEFORE its own 5 s slot. Its absolute phase is
            // retained when it becomes outgoing; no crop/pan reset at any boundary.
            double phase=Math.Max(0,Math.Min(1,(seconds-(segment*ThemeSeconds-FadeSeconds))/(ThemeSeconds+FadeSeconds)));
            int sign=(segment&1)==0?1:-1;
            return new Vector2Int(Mathf.RoundToInt((float)(HorizontalPanDots*(-1+2*phase)))*sign,Mathf.RoundToInt((float)(VerticalPanDots*(-1+2*phase)))*sign);
        }
        private static bool Direction(long segment)
        {
            // Per-session pseudo-random choice, stable when revisiting a QA sample;
            // isolated from UnityEngine.Random and therefore gameplay random state.
            uint x=unchecked(sessionSeed+(uint)segment*0x9E3779B9u);x^=x>>16;x*=0x7FEB352Du;x^=x>>15;
            return (x&1)!=0;
        }
        public void RenderAt(double seconds)
        {
            DisplayTime=Math.Max(0,seconds);long segment=(long)Math.Floor(DisplayTime/ThemeSeconds);
            ThemeIndex=IndexAt(DisplayTime);Blend=FadeAt(DisplayTime);
            DrawLayer(outgoing,outgoingLight,ThemeIndex,PanAt(DisplayTime,segment),1);
            DrawLayer(incoming,incomingLight,(ThemeIndex+1)%5,PanAt(DisplayTime,segment+1),Blend);
            incoming.gameObject.SetActive(Blend>0);
            Leftward=InspectionLeftward ?? Direction(segment);
            float local=(float)(DisplayTime-segment*ThemeSeconds);
            // Leave fully before the fade. Re-enter from outside at the next slot;
            // one rabbit renderer, never ghosted/doubled across differing ground heights.
            float phase=Mathf.Clamp01(local/4.5f);
            float x=Mathf.Lerp(-64,1144,Leftward?1-phase:phase);
            var pan=PanAt(DisplayTime,segment);
            float foot=(catalog.themes[ThemeIndex].footY-catalog.themes[ThemeIndex].cropTop+pan.y)*Scale;
            rabbit.sprite=catalog.rabbitFrames[(int)Math.Floor(DisplayTime*10)%8];
            rabbit.rectTransform.anchoredPosition=new Vector2(Mathf.Round(x/4)*4,-foot+14*Scale);
            rabbit.rectTransform.localScale=new Vector3(Leftward?-1:1,1,1);
        }
        private void DrawLayer(Image image,Image light,int index,Vector2Int pan,float alpha)
        {
            var theme=catalog.themes[index];image.sprite=theme.scene;
            image.rectTransform.sizeDelta=new Vector2(1408,2816);
            image.rectTransform.anchoredPosition=new Vector2((-CropLeft+pan.x)*Scale,(theme.cropTop-pan.y)*Scale);
            image.color=new Color(1,1,1,alpha);
            light.sprite=theme.light;light.rectTransform.sizeDelta=new Vector2(288,1120);
            light.rectTransform.anchoredPosition=new Vector2(208,-80);
            light.color=new Color(1,1,1,.12f*alpha);
        }
        private static Image MakeImage(string name,Transform parent)
        {
            var image=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent,false);image.raycastTarget=false;image.type=Image.Type.Simple;
            var r=image.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            return image;
        }
    }
}
