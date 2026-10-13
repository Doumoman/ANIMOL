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
    public sealed class MainUiV7Capture : MonoBehaviour
    {
        private const string Root="Docs/MainUiV7/Captures/";
        private readonly List<string> checks=new List<string>();
        private readonly List<string> errors=new List<string>();
        private bool background;
        public static void Begin() {
            if(!Application.isPlaying || FindFirstObjectByType<MainUiV7Capture>()!=null) throw new InvalidOperationException("Run once in Play mode.");
            var go=new GameObject("V7 QA capture");DontDestroyOnLoad(go);var c=go.AddComponent<MainUiV7Capture>();c.StartCoroutine(c.Run());
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
            yield return Audit(cycle);
            yield return Live(cycle);
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
        private IEnumerator Audit(MenuThemeCycle c)
        {
            var sheets=new[]{Load("rabbit-run-left"),Load("rabbit-run-right")};
            var sample=new Texture2D(64,96,TextureFormat.RGBA32,false);
            var rows=new List<string>{"theme,p,camera,rabbit,frame,opaque,upperOpaque,changed,upperChanged,viewportClipped"};
            int count=0;
            Check(c.Output.GetComponentInParent<Mask>()==null && c.Output.GetComponentInParent<RectMask2D>()==null,"No parent mask clips rabbit");
            for(int theme=0;theme<5;theme++) foreach(double p in new[]{.2,.35,.5,.65,.8}) foreach(int camera in new[]{-1,1}) foreach(int run in new[]{-1,1}) for(int frame=0;frame<8;frame++) {
                c.InspectionFrame=frame;c.InspectionCameraDirection=camera;c.InspectionRabbitDirection=run;
                c.InspectionTime=theme*5+p*5;c.RenderAt(c.InspectionTime.Value);
                int x=FantasyBackgroundPolicy.RabbitX(p,run);
                var old=RenderTexture.active;RenderTexture.active=c.NativeFrame;
                sample.ReadPixels(new Rect(x,704-436-96,64,96),0,0);sample.Apply();RenderTexture.active=old;
                var actual=sample.GetPixels32();var source=sheets[run==1?1:0].GetPixels32();
                int opaque=0,upper=0,changed=0,upperChanged=0,clipped=0,minY=96,maxY=0;
                for(int yy=0;yy<96;yy++) for(int xx=0;xx<64;xx++) if(source[yy*512+frame*64+xx].a!=0) {minY=Math.Min(minY,yy);maxY=Math.Max(maxY,yy);}
                for(int yy=0;yy<96;yy++) for(int xx=0;xx<64;xx++) {
                    var expected=source[yy*512+frame*64+xx];if(expected.a==0)continue;
                    bool head=yy>=maxY-(maxY-minY+1)*.6;opaque++;if(head)upper++;
                    var got=actual[yy*64+xx];if(got.r!=expected.r || got.g!=expected.g || got.b!=expected.b){changed++;if(head)upperChanged++;}
                    var rect=c.Output.rectTransform;var pos=rect.TransformPoint(new Vector3((x+xx+.5f-176)/352*rect.rect.width,(352-436-96+yy+.5f)/704*rect.rect.height,0));
                    if(pos.x<0 || pos.x>=Screen.width || pos.y<0 || pos.y>=Screen.height)clipped++;
                }
                rows.Add(FormattableString.Invariant($"{theme+1},{p},{camera},{run},{frame},{opaque},{upper},{changed},{upperChanged},{clipped}"));count++;
                if(changed!=0 || clipped!=0) throw new InvalidOperationException("Rabbit pixels lost: "+rows.Last());
                // Every frame, both directions, including library and mine, captured from the actual Game View.
                if(p==.5 && ((theme==2 && camera==-1)||(theme==4 && camera==1))) {
                    yield return new WaitForEndOfFrame();SaveScreen($"Rabbit_T{theme+1}_cam{camera}_run{run}_f{frame}");
                }
                if(count%80==0)yield return null;
            }
            foreach(double p in new[]{64.0/416,352.0/416}) foreach(int run in new[]{-1,1}) {
                int x=FantasyBackgroundPolicy.RabbitX(p,run);var rect=c.Output.rectTransform;
                var a=rect.TransformPoint(new Vector3((x-176)/352f*rect.rect.width,(352-532)/704f*rect.rect.height,0));
                var b=rect.TransformPoint(new Vector3((x+64-176)/352f*rect.rect.width,(352-436)/704f*rect.rect.height,0));
                Check(a.x>=-.1 && b.x<=Screen.width+.1 && a.y>=0 && b.y<=Screen.height,"Full-cell interval endpoint visible "+p+" direction "+run);
            }
            File.WriteAllLines(Root+Screen.width+"x"+Screen.height+"_rabbit-audit.csv",rows);
            Check(count==800,"800 GPU silhouette/near/viewport samples: zero changed opaque pixels");
            c.InspectionTime=null;c.InspectionFrame=null;c.InspectionCameraDirection=null;c.InspectionRabbitDirection=null;
            foreach(var sheet in sheets)Destroy(sheet);Destroy(sample);
        }
        private static Texture2D Load(string name) {
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes("Docs/Inbox/ANIMOL_main_ui_v7/runtime/assets/"+name+".png"));return t;
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
            File.WriteAllLines(Root+Screen.width+"x"+Screen.height+"_Live_26s.csv",rows);
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
            Check(Mathf.Abs(corners[0].x)<.1 && Mathf.Abs(corners[2].x-Screen.width)<.1,"Full horizontal corridor "+label);
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
