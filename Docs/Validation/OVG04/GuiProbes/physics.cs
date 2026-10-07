using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using ANIMOL.Development;
public static class Ovg04GuiPhysics
{
static TaskCompletionSource<object> completion;
public static Task<object> Main()
{
completion=new TaskCompletionSource<object>();TerrainEditorScreen.Instance.StartCoroutine(Guard(Run()));return completion.Task;
}
static IEnumerator Guard(IEnumerator work)
{
while(true){object next;try{if(!work.MoveNext())yield break;next=work.Current;}catch(Exception ex){completion.SetException(ex);yield break;}yield return next;}
}
static IEnumerator Move(DevPlayerController player,Vector2 position)
{
player.Body.position=position;player.Body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
}
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static IEnumerator Capture(string name)
{
yield return null;
bool done=false;Exception error=null;
EditorApplication.delayCall+=()=>{try{CaptureBuffer(name);}catch(Exception ex){error=ex;}finally{done=true;}};
while(!done)yield return null;
if(error!=null)completion.TrySetException(error);
}
static void CaptureBuffer(string name)
{
var window=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));window.Focus();
var w=(int)window.position.width;var h=(int)window.position.height;var rt=new RenderTexture(w,h,24);rt.Create();
Check(UnityEditorInternal.InternalEditorUtility.CaptureEditorWindow(window,rt),"GUI buffer capture failed");
var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(w,h,TextureFormat.RGB24,false);
texture.ReadPixels(new Rect(0,0,w,h),0,0);texture.Apply();
System.IO.File.WriteAllBytes("C:/Users/user/Documents/GitHub/ANIMOL/Docs/Validation/OVG04/gui-"+name+"-v6.png",texture.EncodeToPNG());
UnityEngine.Object.Destroy(texture);RenderTexture.active=previous;rt.Release();UnityEngine.Object.Destroy(rt);
}
static IEnumerator Run()
{
var screen=TerrainEditorScreen.Instance;screen.RestartTest();yield return null;yield return new WaitForFixedUpdate();
var player=screen.TestPlayer;player.enabled=false;player.Body.gravityScale=0;
var session=screen.TestSession;var bubbles=screen.GetComponentsInChildren<CampaignMapBubblePickup>(true).OrderBy(b=>b.transform.position.x).ToArray();
var checkpoint=screen.GetComponentInChildren<CampaignMapCheckpoint>();var exit=screen.GetComponentInChildren<CampaignMapExit>();
yield return Move(player,bubbles[0].transform.position);Check(session.CollectedCount==1,"First bubble");
yield return Move(player,checkpoint.transform.position);Check(session.ActiveCheckpointId==checkpoint.CheckpointId,"Checkpoint activation");
yield return Move(player,bubbles[1].transform.position);Check(session.CollectedCount==2,"Second bubble");
yield return Move(player,new Vector2(16.5f,-5.75f));
Check(session.CollectedCount==1&&!bubbles[0].gameObject.activeSelf&&bubbles[1].gameObject.activeSelf,"Retain before/restore after");
Check(Mathf.Abs(player.Body.position.x-6.5f)<.03f,"Safe checkpoint feet");yield return Capture("checkpoint-retained-1");
yield return Move(player,bubbles[1].transform.position);yield return Move(player,bubbles[2].transform.position);
Check(session.CollectedCount==3&&exit.IsOpen&&!session.HasCompleted,"Only unlock before exit contact");yield return Capture("exit-open-3");
yield return Move(player,new Vector2(16.5f,-5.75f));
Check(session.CollectedCount==1&&!exit.IsOpen&&session.State==RunSessionState.Playing,"Relock after checkpoint restore");yield return Capture("exit-relocked-1");
yield return Move(player,bubbles[1].transform.position);yield return Move(player,bubbles[2].transform.position);
yield return new WaitForSecondsRealtime(1.1f);Check(!session.HasCompleted,"Camera sequence cannot clear");
yield return Move(player,exit.transform.position);Check(session.HasCompleted&&session.State==RunSessionState.Result,"Physical exit clear");yield return Capture("actual-exit-cleared");
var seed=session.SelectedBubbleSeed;var slots=session.SelectedBubbleSlots.ToArray();
screen.GetComponentsInChildren<Button>().Single(b=>b.name=="Reset Test").onClick.Invoke();yield return null;yield return new WaitForFixedUpdate();
Check(screen.TestSession.CollectedCount==0&&screen.TestSession.ActiveCheckpointId==""&&!screen.TestSession.ExitIsOpen,"Manual restart initial state");
Check(seed==screen.TestSession.SelectedBubbleSeed&&slots.OrderBy(x=>x).SequenceEqual(screen.TestSession.SelectedBubbleSlots.OrderBy(x=>x)),"Manual restart seed policy");
completion.SetResult(new {physics2D=true,retainedBefore=1,restoredAfter=true,relockedCount=1,physicalExitCleared=true,cameraOnlyDidNotClear=true,manualRestart=true,sameSeedSlots=true});
}
}
