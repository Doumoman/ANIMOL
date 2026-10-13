using UnityEngine;
using UnityEditor;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Development;
using ANIMOL.Gameplay;
public static class Ovg04GuiRuntime
{
public static object Main()
{
var screen=TerrainEditorScreen.Instance;
if(!EditorApplication.isPlaying||screen==null||!screen.IsTesting)throw new System.Exception("Actual local session not ready");
screen.RestartTest();
var source=screen.Adapter.Map;var runtime=screen.TestLoader.Map;var player=screen.TestPlayer;
var sameIds=runtime.Objects.Select(o=>o.StableId).OrderBy(x=>x).SequenceEqual(source.Objects.Select(o=>o.StableId).OrderBy(x=>x));
var side=runtime.Objects.Single(o=>o.Kind==StageMapObjectKind.CommonC02);
var router=screen.GetComponentInChildren<DevMobileInputRouter>();
var swipe=router.Begin(700,MobileTouchAction.SwipeMove);router.MoveSwipe(700,1);var direction=player.MoveDirection;router.End(700);
var jump=router.Begin(701,MobileTouchAction.Jump);router.End(701);
var down=router.Begin(702,MobileTouchAction.DropThrough);router.End(702);
var oneController=screen.GetComponentsInChildren<DevPlayerController>().Length==1;
var oneObjective=screen.GetComponentsInChildren<CampaignMapDevSession>().Length==1;
var noTemporaryOwner=screen.GetComponentsInChildren<ObjectLabSession>().Length==0;
var x=player.Body.position.x;var feet=player.GetComponent<BoxCollider2D>().bounds.min.y;
if(!sameIds||side.X!=22||side.Settings.ObstacleFacing!=ObstacleFacing.Left||!swipe||direction!=1||!jump||!down||!oneController||!oneObjective||!noTemporaryOwner)throw new System.Exception("Local play identity/input/owner mismatch");
screen.RestartTest();
PortraitGameViewSetup.SelectFixedResolution(540,960,"OVG04 V6 Portrait");
var game=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));game.position=new Rect(50,50,600,1050);game.Focus();game.Repaint();
return new {playing=true,sameIds,sideX=side.X,facing=side.Settings.ObstacleFacing.ToString(),spawnX=x,spawnFeet=feet,swipe,direction,jump,down,oneController,oneObjective,noTemporaryOwner,selectionPendingStart=true,checkpoint=screen.TestSession.ActiveCheckpointId,source=AssetDatabase.GetAssetPath(source),displayCameraEnabled=screen.PresentationCamera.enabled};
}
}
