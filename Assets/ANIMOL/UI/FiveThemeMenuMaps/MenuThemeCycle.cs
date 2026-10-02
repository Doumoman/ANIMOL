using System;
using ANIMOL.PortraitArtV1;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.FiveThemeMenu
{
    /// <summary>V7 native pixel compositor (retains existing serialized asset identities). Local clock pauses while hidden; scene entry restarts at T01.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class MenuThemeCycle : MonoBehaviour
    {
        [SerializeField] private FantasyBackgroundCatalog catalog;
        [SerializeField] private RawImage output;
        [SerializeField] private RawImage backdropFill;
        [SerializeField] private uint seed;
        private UiNavigationService navigation;
        private Material material;
        private RenderTexture current, previous, final;
        private Texture2D fillPixel;
        private bool focused=true, paused, wasVisible, needsFallbackSkin;
        private double elapsed;
        public bool HasAuthoredLayout => catalog!=null && output!=null;
        public FantasyBackgroundCatalog Catalog => catalog;
        public RawImage Output => output;
        public RenderTexture NativeFrame => final;
        public bool IsVisible => output!=null && output.gameObject.activeSelf;
        public double Elapsed => elapsed;
        public double DisplayTime { get; private set; }
        public int ThemeIndex => FantasyBackgroundPolicy.ThemeAt(DisplayTime);
        public int FrameIndex => InspectionFrame ?? FantasyBackgroundPolicy.FrameAt(DisplayTime);
        public int? InspectionFrame { get; set; }
        public int? InspectionCameraDirection { get; set; }
        public int? InspectionRabbitDirection { get; set; }
        public int CameraDirection => FantasyBackgroundPolicy.Direction((long)(DisplayTime/5),91,seed);
        public int RabbitDirection => FantasyBackgroundPolicy.Direction((long)(DisplayTime/5),314,seed);
        public double? InspectionTime { get; set; }
        public uint Seed { get => seed; set => seed=value; }
        public int OwnedTextureCount => final==null ? 0 : 3;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => SceneManager.sceneLoaded-=Install;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Subscribe() { SceneManager.sceneLoaded-=Install; SceneManager.sceneLoaded+=Install; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallCurrent() => Install(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        private static void Install(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="Bootstrap" && scene.name!="Lobby") return;
            foreach(var root in scene.GetRootGameObjects()) foreach(var nav in root.GetComponentsInChildren<UiNavigationService>(true)) {
                if(nav.GetComponent<Canvas>()==null || nav.GetComponentInChildren<MenuThemeCycle>(true)!=null) continue;
                var go=new GameObject("MainUiV6Cycle");go.transform.SetParent(nav.transform,false);go.AddComponent<MenuThemeCycle>();
            }
        }
        private void Awake()
        {
            navigation=GetComponentInParent<UiNavigationService>();
            if(catalog==null) catalog=Resources.Load<FantasyBackgroundCatalog>("ANIMOLMainUiV6");
            if(catalog==null) { enabled=false; return; }
            if(output==null) { needsFallbackSkin=true; AuthorLayout(catalog); }
        }
        public void AuthorLayout(FantasyBackgroundCatalog source)
        {
            catalog=source;
            if(backdropFill==null) {
                var oldFill=transform.parent.Find("MainUiBackdropFill");
                if(oldFill!=null) { if(Application.isPlaying) Destroy(oldFill.gameObject); else DestroyImmediate(oldFill.gameObject); }
                backdropFill=new GameObject("MainUiBackdropFill",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage)).GetComponent<RawImage>();
                backdropFill.transform.SetParent(transform.parent,false);
                backdropFill.transform.SetSiblingIndex(transform.parent.Find("SafeArea").GetSiblingIndex());
                backdropFill.rectTransform.anchorMin=Vector2.zero;backdropFill.rectTransform.anchorMax=Vector2.one;
                backdropFill.rectTransform.offsetMin=backdropFill.rectTransform.offsetMax=Vector2.zero;
                backdropFill.color=Color.white;backdropFill.raycastTarget=false;
            }
            if(output!=null) {backdropFill.transform.SetAsFirstSibling();Fit();return;}
            output=new GameObject("MainUiV6Backdrop",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage)).GetComponent<RawImage>();
            output.transform.SetParent(transform.parent,false);
            output.transform.SetSiblingIndex(transform.parent.Find("SafeArea").GetSiblingIndex());
            output.rectTransform.anchorMin=output.rectTransform.anchorMax=output.rectTransform.pivot=new Vector2(.5f,.5f);
            output.raycastTarget=false;output.color=Color.white;
            // Background is a sibling of SafeArea: no screen tween, clipping, tint or input interception.
            Fit();
        }
        private void OnApplicationFocus(bool value) { focused=value; wasVisible=false; }
        private void OnApplicationPause(bool value) { paused=value; wasVisible=false; }
        private void LateUpdate()
        {
            if(!HasAuthoredLayout || navigation==null) return;
            if(needsFallbackSkin) {
                var entry=navigation.GetComponentInChildren<PortraitEntryController>(true);
                var controls=Resources.Load<MenuThemeCatalog>("ANIMOLFiveThemeMenu");
                if(entry!=null && controls!=null) { MenuThemeSkin.Apply(entry,controls); needsFallbackSkin=false; }
            }
            bool visible=navigation.CurrentScreenId==PortraitEntryController.StartId || navigation.CurrentScreenId==PortraitEntryController.ModeId;
            if(backdropFill!=null && backdropFill.gameObject.activeSelf!=visible) backdropFill.gameObject.SetActive(visible);
            if(output.gameObject.activeSelf!=visible) output.gameObject.SetActive(visible);
            if(!visible || !focused || paused) { wasVisible=false; return; }
            Fit();
            if(wasVisible) elapsed+=Time.unscaledDeltaTime;
            wasVisible=true;
            RenderAt(InspectionTime ?? elapsed);
        }
        private void Fit()
        {
            var rect=((RectTransform)output.transform.parent).rect;
            // Width fit preserves the entire rabbit corridor on tall displays; the fill covers any letterbox.
            float scale=rect.width/352;
            output.rectTransform.sizeDelta=new Vector2(352,704)*scale;
            output.rectTransform.anchoredPosition=Vector2.zero;
        }
        private static RenderTexture Allocate(string name)
        {
            var rt=new RenderTexture(352,704,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB) {
                name=name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,
                useMipMap=false,autoGenerateMips=false,antiAliasing=1,hideFlags=HideFlags.DontSave
            };rt.Create();return rt;
        }
        private void EnsureResources()
        {
            if(material!=null) return;
            material=new Material(catalog.compositor){name="Main UI V7 compositor",hideFlags=HideFlags.DontSave};
            current=Allocate("V7 current"); previous=Allocate("V7 previous"); final=Allocate("V7 native output");
            output.texture=final;
            // A texture avoids dark-color quantization in Canvas linear vertex colors.
            fillPixel=new Texture2D(1,1,TextureFormat.RGBA32,false,false){name="V7 opaque base",filterMode=FilterMode.Point,hideFlags=HideFlags.DontSave};
            fillPixel.SetPixel(0,0,new Color32(26,28,44,255));fillPixel.Apply(false,true);
            if(backdropFill!=null) backdropFill.texture=fillPixel;
        }
        public void RenderAt(double seconds)
        {
            if(!HasAuthoredLayout) return;
            EnsureResources(); DisplayTime=Math.Max(0,seconds);
            long slot=(long)Math.Floor(DisplayTime/5);double local=DisplayTime-slot*5;
            bool oldSrgb=GL.sRGBWrite;var oldActive=RenderTexture.active;
            try {
                GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;
                Draw(current,slot,local/5);
                if(slot>0 && local<.36) {
                    Draw(previous,slot-1,.999);
                    material.SetTexture("_Previous",previous);material.SetFloat("_Threshold",(float)(local/.36*1.22-.11));
                    Graphics.Blit(current,final,material,1);
                } else Graphics.CopyTexture(current,final);
            } finally {GL.sRGBWrite=oldSrgb;RenderTexture.active=oldActive;}
        }
        private void Draw(RenderTexture target,long slot,double p)
        {
            var theme=catalog.themes[slot%5];int direction=InspectionCameraDirection ?? FantasyBackgroundPolicy.Direction(slot,91,seed);
            material.SetTexture("_Far",theme.far);material.SetTexture("_Mid",theme.mid);
            material.SetTexture("_Platform",theme.platform);material.SetTexture("_Near",theme.near);
            SetRect("_FarRect",FantasyBackgroundPolicy.Plane(p,direction,1.16f,.30f));
            SetRect("_MidRect",FantasyBackgroundPolicy.Plane(p,direction,1.36f,.62f));
            SetRect("_NearRect",FantasyBackgroundPolicy.Plane(p,direction,1.60f,1.60f));
            int run=InspectionRabbitDirection ?? FantasyBackgroundPolicy.Direction(slot,314,seed);
            material.SetTexture("_Rabbit",run==1?catalog.rabbitRight:catalog.rabbitLeft);
            material.SetFloat("_RabbitX",FantasyBackgroundPolicy.RabbitX(p,run));material.SetFloat("_Frame",FrameIndex);
            Graphics.Blit(null,target,material,0);
        }
        private void SetRect(string name,RectInt r) => material.SetVector(name,new Vector4(r.x,r.y,r.width,r.height));
        private void OnDisable() { wasVisible=false; Release(); if(output!=null) output.gameObject.SetActive(false); if(backdropFill!=null) backdropFill.gameObject.SetActive(false); }
        private void OnDestroy() { Release(); if(output!=null) Destroy(output.gameObject); if(backdropFill!=null) Destroy(backdropFill.gameObject); }
        private void Release()
        {
            if(output!=null) output.texture=null;
            if(backdropFill!=null) backdropFill.texture=null;
            if(fillPixel!=null) Destroy(fillPixel);fillPixel=null;
            ReleaseTexture(ref current);ReleaseTexture(ref previous);ReleaseTexture(ref final);
            if(material!=null) Destroy(material); material=null;
        }
        private static void ReleaseTexture(ref RenderTexture rt) { if(rt==null) return;rt.Release();Destroy(rt);rt=null; }
    }
}
