#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.PortraitArtV1;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.FiveThemeMenu
{
    /// <summary>Explicit real Game View evidence harness. Never installed by normal play.</summary>
    public sealed class MainUiV6Capture : MonoBehaviour
    {
        private const string Root="Docs/MainUiV6/Captures/";
        private readonly List<string> checks=new List<string>();
        private readonly List<string> errors=new List<string>();
        private bool background;
        public static void Begin() {
            if(!Application.isPlaying || FindFirstObjectByType<MainUiV6Capture>()!=null) throw new InvalidOperationException("Run once in Play mode.");
            var go=new GameObject("V6 QA capture");DontDestroyOnLoad(go);var c=go.AddComponent<MainUiV6Capture>();c.StartCoroutine(c.Run());
        }
        private void Awake() { background=Application.runInBackground;Application.runInBackground=true;Application.logMessageReceived+=Log; }
        private void OnDestroy() {Application.runInBackground=background;Application.logMessageReceived-=Log;var c=FindFirstObjectByType<MenuThemeCycle>();if(c!=null)c.InspectionTime=null;}
        private void Log(string message,string stack,LogType type) {if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(message);}
        private void Check(bool value,string label) {
            if(!value) throw new InvalidOperationException(label);checks.Add("PASS "+label);
            File.WriteAllLines(Root+Screen.width+"x"+Screen.height+"_checks.txt",checks);
        }
        private IEnumerator Run()
        {
            Directory.CreateDirectory(Root);yield return new WaitForSecondsRealtime(.5f);
            var entry=FindFirstObjectByType<PortraitEntryController>();
            Check(entry.Navigation.CurrentScreenId==PortraitEntryController.StartId,"Authored Bootstrap Start");
            yield return Frame("Start",2.25);
            Click(entry.StartScreen.Play);yield return new WaitForSecondsRealtime(.5f);
            entry=FindFirstObjectByType<PortraitEntryController>();var cycle=FindFirstObjectByType<MenuThemeCycle>();
            Check(SceneManager.GetActiveScene().name=="Lobby" && entry.Navigation.CurrentScreenId==PortraitEntryController.ModeId,"Play -> real Lobby");
            for(int i=0;i<5;i++) yield return Frame("Lobby_T0"+(i+1),i*5+2.25);
            foreach(double t in new[]{5.0,5.18,5.36,25.18}) yield return Frame("Wipe_"+t.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture),t);
            foreach(var route in new[]{(entry.ModeScreen.Campaign,"SC02_ThemeSelect","ThemeBackButton"),(entry.ModeScreen.Shop,"SC11_Store","StoreBackButton"),(entry.ModeScreen.Settings,"SC13_Settings","SettingsBackButton")}) {
                Click(route.Item1);yield return new WaitForSecondsRealtime(.3f);
                Check(entry.Navigation.CurrentScreenId==route.Item2 && !cycle.IsVisible,"Route and hidden background "+route.Item2);
                double before=cycle.Elapsed;yield return new WaitForSecondsRealtime(.15f);Check(cycle.Elapsed==before,"Clock paused "+route.Item2);
                Click(FindButton(route.Item3));yield return new WaitForSecondsRealtime(.3f);
                Check(entry.Navigation.CurrentScreenId==PortraitEntryController.ModeId && cycle.IsVisible,"Back -> lobby "+route.Item2);
            }
            var modal=entry.Navigation.GetComponent<UiModalStack>();Check(modal.Push("ConfirmExitModal"),"Existing exit modal opens");
            yield return new WaitForSecondsRealtime(.3f);yield return Frame("Modal",2.25);
            Click(FindButton("ExitCancelButton"));yield return null;Check(modal.Count==0,"Modal cancel pointer route");
            Click(entry.ModeScreen.Back);yield return new WaitForSecondsRealtime(.3f);Check(entry.Navigation.CurrentScreenId==PortraitEntryController.StartId,"Lobby back -> Start");
            Click(entry.StartScreen.Play);yield return new WaitForSecondsRealtime(.3f);Check(entry.Navigation.CurrentScreenId==PortraitEntryController.ModeId,"Start -> Lobby reentry");
            var texture=cycle.NativeFrame;cycle.InspectionTime=null;
            cycle.SendMessage("OnApplicationFocus",false);double paused=cycle.Elapsed;
            yield return new WaitForSecondsRealtime(.15f);Check(cycle.Elapsed==paused,"Focus loss pauses clock");
            cycle.SendMessage("OnApplicationFocus",true);yield return null;yield return null;Check(cycle.Elapsed-paused<.1,"Focus regain does not catch up");
            cycle.SendMessage("OnApplicationPause",true);paused=cycle.Elapsed;yield return new WaitForSecondsRealtime(.15f);Check(cycle.Elapsed==paused,"Application pause freezes clock");
            cycle.SendMessage("OnApplicationPause",false);yield return null;yield return null;
            Check(cycle.NativeFrame==texture && cycle.OwnedTextureCount==3,"Resources reused after routes and pause");
            Check(FindObjectsByType<MenuThemeCycle>(FindObjectsSortMode.None).Length==1,"Single compositor");
            if(Screen.height==1920) yield return Live(cycle);
            // Exercise the real campaign launch, without fabricating progress or rewards.
            Click(entry.ModeScreen.Campaign);yield return new WaitForSecondsRealtime(.3f);
            Click(FindButton("ThemeCard_01"));yield return new WaitForSecondsRealtime(.3f);
            Click(FindButton("StageCard_01"));yield return new WaitForSecondsRealtime(.3f);
            var start=FindButton("StageStartButton");
            if(start.interactable) Click(start);
            else {
                var reason=FindObjectsByType<Text>(FindObjectsSortMode.None).First(t=>t.name=="AvailabilityText").text;
                File.WriteAllText(Root+"operational-start-blocked.txt",reason);
                string before=entry.Navigation.CurrentScreenId;
                ExecuteEvents.Execute(start.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
                Check(entry.Navigation.CurrentScreenId==before,"Unavailable operational stage rejects click truthfully");
                Click(FindButton("MapDevTestButton"));
            }
            yield return new WaitForSecondsRealtime(2);
            Check(SceneManager.GetActiveScene().name!="Lobby","Existing available gameplay/DEV start left Lobby");
            Check(FindFirstObjectByType<MenuThemeCycle>()==null,"No menu background in gameplay");
            SceneManager.LoadScene("Lobby");yield return new WaitForSecondsRealtime(.5f);
            Check(FindObjectsByType<MenuThemeCycle>(FindObjectsSortMode.None).Length==1,"Scene reentry creates one compositor");
            Check(errors.Count==0,"No runtime console errors");
            File.WriteAllLines(Root+Screen.width+"x"+Screen.height+"_checks.txt",checks);
            File.WriteAllText(Root+Screen.width+"x"+Screen.height+".complete","PASS actual Unity Game View and pointer raycast routes");
            Destroy(gameObject);
        }
        private IEnumerator Live(MenuThemeCycle c)
        {
            c.InspectionTime=null;yield return null;yield return null;
            var rows=new List<string>{"real,local,theme,frame,cameraDirection,rabbitDirection,rabbitX,rabbitY,footY"};
            double begin=Time.realtimeSinceStartupAsDouble,lastCapture=-1;
            while(Time.realtimeSinceStartupAsDouble-begin<26) {
                double real=Time.realtimeSinceStartupAsDouble-begin;
                rows.Add(FormattableString.Invariant($"{real:F4},{c.DisplayTime:F4},{c.ThemeIndex},{c.FrameIndex},{c.CameraDirection},{c.RabbitDirection},{FantasyBackgroundPolicy.RabbitX(c.DisplayTime%5/5,c.RabbitDirection)},436,526"));
                if(real-lastCapture>=.5) {lastCapture=real;yield return new WaitForEndOfFrame();SaveScreen("Live_"+rows.Count.ToString("D4"));}
                yield return new WaitForSecondsRealtime(.05f);
            }
            Check(c.DisplayTime>25,"Over 25 seconds of actual live unscaled animation");
            File.WriteAllLines(Root+"Live_26s.csv",rows);
        }
        private IEnumerator Frame(string label,double time)
        {
            var c=FindFirstObjectByType<MenuThemeCycle>();c.InspectionTime=time;
            yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
            SaveScreen(label);
            var old=RenderTexture.active;RenderTexture.active=c.NativeFrame;
            var native=new Texture2D(352,704,TextureFormat.RGBA32,false);native.ReadPixels(new Rect(0,0,352,704),0,0);native.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Root+label+"_native.png",native.EncodeToPNG());Destroy(native);
            var rt=c.Output.rectTransform;var corners=new Vector3[4];rt.GetWorldCorners(corners);
            Check(corners[0].x<=.1 && corners[0].y<=.1 && corners[2].x>=Screen.width-.1 && corners[2].y>=Screen.height-.1,"Cover "+label);
            Check(Mathf.Abs(rt.rect.width/rt.rect.height-.5f)<.0001,"1:2 aspect "+label);
            var foot=rt.TransformPoint(new Vector3(0,rt.rect.height*(.5f-526f/704f),0));
            Check(foot.y>0 && foot.y<Screen.height,"Foot line visible "+label);
        }
        private static void SaveScreen(string label) {
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+label+"_"+Screen.width+"x"+Screen.height+".png",image.EncodeToPNG());Destroy(image);
        }
        private static Button FindButton(string name) => FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name==name);
        private void Click(Button b)
        {
            Canvas.ForceUpdateCanvases();var r=(RectTransform)b.transform;
            var p=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);
            Check(b.IsInteractable() && hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==b.gameObject,"Pointer route "+b.name);
            ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerClickHandler);
        }
    }
}
#endif
