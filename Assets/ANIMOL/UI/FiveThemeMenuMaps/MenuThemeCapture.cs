#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.PortraitArtV1;
using ANIMOL.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ANIMOL.FiveThemeMenu
{
    /// <summary>Explicit Play-mode evidence tool; never installed automatically.</summary>
    public sealed class MenuThemeCapture : MonoBehaviour
    {
        private const string Output="Docs/FiveThemeMenu/Captures/";
        private readonly List<string> checks=new List<string>();
        private readonly List<string> errors=new List<string>();
        private bool previousBackground;
        private void Awake() {previousBackground=Application.runInBackground;Application.runInBackground=true;Application.logMessageReceived+=Log;}
        private void OnDestroy() {Application.runInBackground=previousBackground;Application.logMessageReceived-=Log;var c=FindFirstObjectByType<MenuThemeCycle>();if(c!=null) {c.InspectionTime=null;c.InspectionLeftward=null;}}
        private void Log(string message,string stack,LogType type) {if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(message);}
        public static void Begin()
        {
            if(!Application.isPlaying || FindFirstObjectByType<MenuThemeCapture>()!=null) throw new InvalidOperationException("Begin once in Play mode.");
            var go=new GameObject("Five theme evidence capture");DontDestroyOnLoad(go);var capture=go.AddComponent<MenuThemeCapture>();capture.StartCoroutine(capture.Run());
        }
        public static void BeginLiveOnly()
        {
            if(!Application.isPlaying || FindFirstObjectByType<MenuThemeCapture>()!=null) throw new InvalidOperationException("Begin once in Play mode.");
            var go=new GameObject("Five theme live clock evidence");var capture=go.AddComponent<MenuThemeCapture>();capture.StartCoroutine(capture.LiveOnly());
        }
        private IEnumerator LiveOnly() {yield return RecordLive();File.WriteAllText(Output+"LiveClock.complete","PASS");Destroy(gameObject);}
        private IEnumerator RecordLive()
        {
            Directory.CreateDirectory(Output);var cycle=FindFirstObjectByType<MenuThemeCycle>();
            cycle.InspectionTime=null;cycle.InspectionLeftward=null;
            // Let LateUpdate render the live clock before recording, not the last QA sample.
            yield return null;yield return null;
            var lines=new List<string>{"elapsed,theme,blend,frame,leftward,foot_top_px,scene_x,scene_y"};
            double end=cycle.Elapsed+26;
            while(cycle.Elapsed<end) {
                var r=cycle.Rabbit.rectTransform;
                lines.Add(FormattableString.Invariant($"{cycle.DisplayTime:F4},{cycle.ThemeIndex},{cycle.Blend:F4},{cycle.Rabbit.sprite.name},{cycle.Leftward},{-r.anchoredPosition.y+56},{cycle.Outgoing.rectTransform.anchoredPosition.x},{cycle.Outgoing.rectTransform.anchoredPosition.y}"));
                yield return new WaitForSecondsRealtime(.05f);
            }
            File.WriteAllLines(Output+"LiveClock_26s.csv",lines);Check(lines.Count>100,"26 seconds of live unscaled animation recorded");
        }
        private IEnumerator Run()
        {
            Directory.CreateDirectory(Output);yield return new WaitForSecondsRealtime(.5f);
            var entry=FindFirstObjectByType<PortraitEntryController>();var cycle=FindFirstObjectByType<MenuThemeCycle>();
            Check(entry.Navigation.CurrentScreenId==PortraitEntryController.StartId,"Begin at Start");
            for(int theme=0;theme<5;theme++) yield return Frame("Start_"+cycle.Catalog.themes[theme].id,theme*5+2.25,theme%2!=0);
            cycle.InspectionTime=null;cycle.InspectionLeftward=null;yield return null;
            double elapsed=cycle.Elapsed,real=Time.realtimeSinceStartupAsDouble;
            Click(entry.StartScreen.Play);yield return new WaitForSecondsRealtime(.8f);
            entry=FindFirstObjectByType<PortraitEntryController>();cycle=FindFirstObjectByType<MenuThemeCycle>();
            Check(entry.Navigation.CurrentScreenId==PortraitEntryController.ModeId,"Start Play -> ModeSelect through existing handler");
            Check(Math.Abs((cycle.Elapsed-elapsed)-(Time.realtimeSinceStartupAsDouble-real))<.05,"Shared epoch survives Bootstrap -> Lobby (no restart)");
            Check(FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length==1,"One existing Canvas");
            for(int theme=0;theme<5;theme++) yield return Frame("Mode_"+cycle.Catalog.themes[theme].id,theme*5+2.25,theme%2!=0);
            foreach(double time in new[]{0,4.5,4.75,4.999,5,24.5,24.75,24.999,25}) yield return Frame("Boundary_"+time.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture),time,false);
            yield return Frame("Rabbit_Left",2.25,true);yield return Frame("Rabbit_Right",2.25,false);
            var safe=entry.Navigation.GetComponentInChildren<SafeAreaLayout>();var sr=(RectTransform)safe.transform;
            safe.enabled=false;sr.anchorMin=new Vector2(0,96f/Screen.height);sr.anchorMax=new Vector2(1,1-96f/Screen.height);
            yield return Frame("SafeArea96_Mode",12.25,true);
            Click(entry.ModeScreen.Back);yield return null;yield return Frame("SafeArea96_Start",12.25,true);
            Click(entry.StartScreen.Play);yield return null;safe.enabled=true;safe.Apply();yield return null;Canvas.ForceUpdateCanvases();
            foreach(var route in new[]{(entry.ModeScreen.Campaign,"SC02_ThemeSelect","ThemeBackButton"),(entry.ModeScreen.Shop,"SC11_Store","StoreBackButton"),(entry.ModeScreen.Settings,"SC13_Settings","SettingsBackButton")}) {
                Click(route.Item1);yield return new WaitForSecondsRealtime(.2f);
                Check(entry.Navigation.CurrentScreenId==route.Item2 && !cycle.IsVisible,"Unchanged route, background hidden: "+route.Item2);
                Click(entry.Navigation.GetComponentsInChildren<Button>().First(b=>b.name==route.Item3));yield return new WaitForSecondsRealtime(.2f);
                Check(entry.Navigation.CurrentScreenId==PortraitEntryController.ModeId && cycle.IsVisible,"Return to mode: "+route.Item2);
            }
            foreach(var b in new[]{entry.ModeScreen.Ad,entry.ModeScreen.Competition,entry.ModeScreen.Cooperation}) {
                Check(b.gameObject.activeInHierarchy && !b.interactable,"Visible disabled: "+b.name);
                ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
                Check(entry.Navigation.CurrentScreenId==PortraitEntryController.ModeId,"Disabled button unchanged: "+b.name);
            }
            Check(entry.ModeScreen.Currency.text=="--","No fabricated balance/rewards");
            cycle.InspectionTime=null;cycle.InspectionLeftward=null;
            if(Screen.height==1920) yield return RecordLive();
            Check(errors.Count==0,"No console errors during QA");
            File.WriteAllLines(Output+Screen.width+"x"+Screen.height+"_checks.txt",checks);
            File.WriteAllText(Output+Screen.width+"x"+Screen.height+".complete","PASS actual Unity Canvas / Play mode evidence");Destroy(gameObject);
        }
        private IEnumerator Frame(string label,double time,bool left)
        {
            var c=FindFirstObjectByType<MenuThemeCycle>();c.InspectionTime=time;c.InspectionLeftward=left;
            // Let the existing UI navigation tween finish; only the decorative
            // clock is sampled, never capture a half-visible newly opened button.
            yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
            var entry=FindFirstObjectByType<PortraitEntryController>();
            var screen=entry.Navigation.transform.Find("SafeArea/ScreenHost/"+entry.Navigation.CurrentScreenId);
            var safe=Bounds((RectTransform)entry.Navigation.transform.Find("SafeArea"));
            var buttons=screen.GetComponentsInChildren<Button>().Select(b=>new ButtonAudit {name=b.name,interactable=b.interactable,bounds=Bounds((RectTransform)b.transform)}).ToArray();
            foreach(var b in buttons) Check(safe.Contains(b.bounds.min) && safe.Contains(b.bounds.max),"SafeArea "+label+"/"+b.name);
            if(entry.Navigation.CurrentScreenId==PortraitEntryController.StartId)
                Check(!buttons[0].bounds.Overlaps(Bounds(c.Rabbit.rectTransform)),"Play button does not cover rabbit "+label);
            foreach(var t in screen.GetComponentsInChildren<TMP_Text>()) {
                var preferred=t.GetPreferredValues(t.text,t.rectTransform.rect.width,Mathf.Infinity);
                Check(preferred.x<=t.rectTransform.rect.width+1 && preferred.y<=t.rectTransform.rect.height+1,"Text fits "+label+"/"+t.name);
                Check(t.text.All(ch=>char.IsControl(ch)||t.font.HasCharacter(ch,true,true)),"Glyphs "+label+"/"+t.name);
            }
            Check(c.Rabbit.rectTransform.sizeDelta==new Vector2(96,128),"Rabbit 4x "+label);
            var pan=MenuThemeCycle.PanAt(time,(long)Math.Floor(time/5));
            Check(Math.Abs(-c.Rabbit.rectTransform.anchoredPosition.y+56-(1488+pan.y*4))<.01,"Foot follows scene pan "+label);
            var bg=Bounds(c.Outgoing.rectTransform);Check(bg.xMin<=0 && bg.xMax>=Screen.width && bg.yMin<=0 && bg.yMax>=Screen.height,"No uncovered edges "+label);
            Check(!c.Rabbit.raycastTarget && !c.Outgoing.raycastTarget && !c.Incoming.raycastTarget,"Decorations cannot intercept input "+label);
            string path=Output+label+"_"+Screen.width+"x"+Screen.height;
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path+".png",image.EncodeToPNG());Destroy(image);
            File.WriteAllText(path+".json",JsonUtility.ToJson(new Audit {width=Screen.width,height=Screen.height,sampleTime=time,theme=c.Catalog.themes[c.ThemeIndex].id,blend=c.Blend,leftward=c.Leftward,rabbitFrame=c.Rabbit.sprite.name,footTopPx=-c.Rabbit.rectTransform.anchoredPosition.y+56,sceneBounds=bg,rabbitBounds=Bounds(c.Rabbit.rectTransform),safeArea=safe,buttons=buttons},true));
        }
        private void Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var r=(RectTransform)button.transform;var p=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);
            Check(button.IsInteractable() && hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"Pointer route "+button.name);
            ExecuteEvents.Execute(button.gameObject,p,ExecuteEvents.pointerClickHandler);
        }
        private void Check(bool result,string label) {if(!result) throw new InvalidOperationException(label);checks.Add("PASS "+label);}
        private static Rect Bounds(RectTransform r) {var points=new Vector3[4];r.GetWorldCorners(points);var a=RectTransformUtility.WorldToScreenPoint(null,points[0]);var b=RectTransformUtility.WorldToScreenPoint(null,points[2]);return Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));}
        [Serializable] private sealed class Audit {public int width,height;public double sampleTime;public string theme,rabbitFrame;public float blend,footTopPx;public bool leftward;public Rect sceneBounds,rabbitBounds,safeArea;public ButtonAudit[] buttons;}
        [Serializable] private sealed class ButtonAudit {public string name;public bool interactable;public Rect bounds;}
    }
}
#endif
