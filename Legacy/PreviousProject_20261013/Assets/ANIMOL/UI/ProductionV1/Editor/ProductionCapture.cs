using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Gameplay;
using ANIMOL.UI;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.ProductionV1.Editor
{
    /// <summary>Explicit Editor QA only. Captures the actual GameView render target including Overlay UI.</summary>
    public static class ProductionCapture
    {
        private static readonly Queue<Action> steps=new Queue<Action>();
        private static readonly List<object> evidence=new List<object>();
        private static double next;
        private static int height;
        private static bool previousRunInBackground;
        private static int lastCapturedFrame;
        public static bool Running=>steps.Count>0;
        public static string LastError {get;private set;}
        public static void BeginUi()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Lobby Play Mode first.");
            StartRendering();
            steps.Clear();evidence.Clear();LastError=null;
            foreach(int h in new[]{1920,2400})
            {
                int resolution=h;
                steps.Enqueue(()=>{
                    height=resolution;var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(x=>x!=null);
                    t.GetMethod("Use1080x"+height).Invoke(null,null);
                    P.Navigation.Navigate("SC01_Lobby",false);
                });
                steps.Enqueue(()=>Capture("lobby"));
                steps.Enqueue(()=>P.Navigation.Navigate(ProductionController.Screens[0]));steps.Enqueue(()=>Capture("themes"));
                steps.Enqueue(()=>P.SelectTheme(0));steps.Enqueue(()=>Capture("stages"));
                steps.Enqueue(()=>P.StageScroll.verticalNormalizedPosition=0);steps.Enqueue(()=>Capture("stages_bottom"));
                steps.Enqueue(()=>P.SelectStage(0));steps.Enqueue(()=>Capture("detail"));
                steps.Enqueue(()=>P.Navigation.Navigate(ProductionController.Screens[3]));steps.Enqueue(()=>Capture("loading_preparation"));
                steps.Enqueue(()=>P.Navigation.Navigate(ProductionController.Screens[4]));steps.Enqueue(()=>Capture("hub"));
                steps.Enqueue(()=>P.Navigation.Navigate(ProductionController.Screens[5]));steps.Enqueue(()=>Capture("account"));
                steps.Enqueue(()=>P.Navigation.Navigate(ProductionController.Screens[6]));steps.Enqueue(()=>Capture("animal"));
                steps.Enqueue(()=>{
                    var safe=P.transform.Find("SafeArea") as RectTransform;
                    var a=SafeAreaLayout.CalculateNormalizedAnchors(new Rect(0,90,1080,height-180),new Vector2(1080,height));
                    safe.anchorMin=new Vector2(a.x,a.y);safe.anchorMax=new Vector2(a.z,a.w);
                });
                steps.Enqueue(()=>Capture("animal_safearea_90"));
                steps.Enqueue(()=>P.transform.Find("SafeArea").GetComponent<SafeAreaLayout>().Apply());
                foreach(ProductionState s in Enum.GetValues(typeof(ProductionState)))
                {
                    var state=s;steps.Enqueue(()=>P.ShowState(state,"검수용 상태입니다. 실제 운영 구매 결과가 아닙니다.\n긴 한글: 계정 서비스 응답과 콘텐츠 설정을 확인하고 다시 시도해 주세요.\n보유 2,147,483,647 / 필요 1,234,567,890"));
                    steps.Enqueue(()=>Capture("state_"+state));steps.Enqueue(()=>P.Back());
                }
            }
            steps.Enqueue(()=>{File.WriteAllText("Docs/UiProductionV1/gameview-evidence.json",JsonConvert.SerializeObject(evidence,Formatting.Indented));Application.runInBackground=previousRunInBackground;});
            next=EditorApplication.timeSinceStartup+1;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        private static ProductionController P=>UnityEngine.Object.FindFirstObjectByType<ProductionController>();
        private static void StartRendering()
        {
            previousRunInBackground=Application.runInBackground;Application.runInBackground=true;lastCapturedFrame=-1;
            var view=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Repaint();
        }
        private static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            if(steps.Count==0){EditorApplication.update-=Tick;return;}
            try{steps.Dequeue()();}catch(Exception e){LastError=e.ToString();steps.Clear();Application.runInBackground=previousRunInBackground;File.WriteAllText("Docs/UiProductionV1/capture-error.txt",LastError);Debug.LogError(e);}
            next=EditorApplication.timeSinceStartup+.45;
        }
        public static void Capture(string name)
        {
            if(lastCapturedFrame==Time.frameCount)throw new InvalidOperationException("GameView did not advance; refusing a stale capture.");
            lastCapturedFrame=Time.frameCount;
            Canvas.ForceUpdateCanvases();
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
            type.GetMethod("CaptureGameView").Invoke(null,new object[]{1080,height,null,"UiProductionV1_QA/capture.png",false,0,"screen"});
            string source="Assets/UiProductionV1_QA/capture.png";
            string destination="Docs/UiProductionV1/Captures/1080x"+height+"/"+name+".png";
            Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(source,destination,true);
            var p=P;
            evidence.Add(new{name,path=destination,width=Screen.width,height=Screen.height,frame=Time.frameCount,screen=p==null?SceneManager.GetActiveScene().name:p.Navigation.CurrentScreenId,
                imageCount=p==null?0:p.GetComponentsInChildren<Image>(true).Length,
                missingScripts=p==null?0:p.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))});
        }
        public static void BeginGameSurvey()
        {
            StartRendering();
            steps.Clear();evidence.Clear();height=1920;LastError=null;
            steps.Enqueue(()=>Capture("T01_start"));
            steps.Enqueue(()=>{DevPlayerController.Instance.ApplySwipe(987,1);});
            for(int i=0;i<5;i++)
            {
                int n=i;steps.Enqueue(()=>{
                    var player=DevPlayerController.Instance;var bg=UnityEngine.Object.FindFirstObjectByType<ProductionGameBackdrop>();
                    evidence.Add(new{sample=n,player=player.transform.position.ToString(),grounded=player.IsGrounded,camera=Camera.main.transform.position.ToString(),
                        layers=bg.Layers.Select(r=>new{r.name,r.sortingOrder,position=r.transform.position.ToString(),uniform=r.transform.localScale.x==r.transform.localScale.y}).ToArray()});
                    Capture("T01_move_"+n);
                });
            }
            steps.Enqueue(()=>{File.WriteAllText("Docs/UiProductionV1/t01-movement-evidence.json",JsonConvert.SerializeObject(evidence,Formatting.Indented));Application.runInBackground=previousRunInBackground;});
            next=EditorApplication.timeSinceStartup+.25;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
    }
}
