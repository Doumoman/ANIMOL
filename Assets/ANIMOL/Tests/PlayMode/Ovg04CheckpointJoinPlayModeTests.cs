using System.Collections;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Development;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ANIMOL.Tests.PlayMode
{
    public sealed class Ovg04CheckpointJoinPlayModeTests
    {
        private GameObject root;
        private StageMapDefinition snapshot;
        private CampaignMapDevSession session;
        private DevPlayerController player;
        private StageMapRuntimeLoader loader;
        private Camera camera;
        private CampaignMapBubblePickup[] bubbles;
        private CampaignMapCheckpoint checkpoint;
        private CampaignMapExit exit;

        [UnitySetUp] public IEnumerator Setup()
        {
#if UNITY_EDITOR
            var map=AssetDatabase.LoadAssetAtPath<StageMapDefinition>("Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-OVG04-JOIN.asset");
            snapshot=Object.Instantiate(map);
            root=new GameObject("OVG04 actual runtime");root.SetActive(false);
            loader=root.AddComponent<StageMapRuntimeLoader>();loader.enabled=false;
            loader.ConfigureStandaloneDevelopmentOwner();loader.ConfigureDefinition(snapshot,CommonObstacleCatalog.Load(),true);loader.Load(snapshot);
            camera=new GameObject("OVG04 portrait camera",typeof(Camera),typeof(DirectionalCameraFollow),typeof(PortraitWorldCameraPolicy),typeof(ExitRevealCameraController)).GetComponent<Camera>();
            camera.transform.SetParent(root.transform,false);camera.orthographic=true;camera.tag="MainCamera";camera.backgroundColor=new Color(.1f,.12f,.2f);camera.clearFlags=CameraClearFlags.SolidColor;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Gameplay/DevPlayer.prefab");
            player=Object.Instantiate(prefab,root.transform).GetComponent<DevPlayerController>();
            player.transform.position=CampaignMapMarkerGeometry.BodyAtFeet(player,CampaignMapMarkerGeometry.Feet(snapshot,snapshot.Objects.Single(o=>o.Kind==StageMapObjectKind.PlayerStart)));
            session=root.AddComponent<CampaignMapDevSession>();session.ConfigureEditorPreview(snapshot,camera.GetComponent<ExitRevealCameraController>());
            root.SetActive(true);player.enabled=false;player.Body.gravityScale=0;
            camera.transform.position=player.transform.position+Vector3.back*20;
            yield return null;yield return new WaitForFixedUpdate();
            bubbles=root.GetComponentsInChildren<CampaignMapBubblePickup>(true).OrderBy(p=>p.transform.position.x).ToArray();
            checkpoint=root.GetComponentInChildren<CampaignMapCheckpoint>();exit=root.GetComponentInChildren<CampaignMapExit>();
#else
            Assert.Ignore("Editor asset fixture required.");yield break;
#endif
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(root!=null)Object.Destroy(root);if(snapshot!=null)Object.Destroy(snapshot);
            yield return null;
        }
        private IEnumerator Move(Vector2 position)
        {
            player.Body.position=position;player.Body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();yield return null;
        }

        [UnityTest] public IEnumerator PhysicalCheckpointRetainsBeforeAndRestoresAfterOnFallAndRelocksExit()
        {
            Assert.That(bubbles.Length,Is.EqualTo(3));Assert.That(session.ActiveCheckpointId,Is.Empty);
            Assert.That(root.GetComponentsInChildren<DevPlayerController>().Length,Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<ObjectLabSession>().Length,Is.Zero);
            var seed=session.SelectedBubbleSeed;var slots=session.SelectedBubbleSlots.ToArray();
            yield return Move(bubbles[0].transform.position);Assert.That(session.CollectedCount,Is.EqualTo(1));
            yield return Move(checkpoint.transform.position);Assert.That(session.ActiveCheckpointId,Is.EqualTo(checkpoint.CheckpointId));
            yield return Move(bubbles[1].transform.position);Assert.That(session.CollectedCount,Is.EqualTo(2));
            // Enter the actual loader's bottom Physics2D hazard, rather than call Respawn directly.
            yield return Move(new Vector2(16.5f,-5.75f));
            Assert.That(session.CollectedCount,Is.EqualTo(1));Assert.That(bubbles[0].gameObject.activeSelf,Is.False);Assert.That(bubbles[1].gameObject.activeSelf,Is.True);
            Assert.That(player.GetComponent<Collider2D>().bounds.min.y,Is.EqualTo(0).Within(.03f));
            Assert.That(player.Body.position.x,Is.EqualTo(6.5f).Within(.03f));Capture("checkpoint-retained-1");
            yield return Move(bubbles[1].transform.position);yield return Move(bubbles[2].transform.position);
            Assert.That(session.CollectedCount,Is.EqualTo(3));Assert.That(session.State,Is.EqualTo(RunSessionState.ExitEligible));Assert.That(exit.IsOpen,Is.True);
            Assert.That(session.HasCompleted,Is.False);var time=session.RemainingSeconds;float stamina=player.Stamina;
            Capture("exit-open-3");yield return Move(new Vector2(16.5f,-5.75f));
            Assert.That(session.CollectedCount,Is.EqualTo(1));Assert.That(session.State,Is.EqualTo(RunSessionState.Playing));Assert.That(exit.IsOpen,Is.False);
            Assert.That(session.TryEnterExit(),Is.False);Assert.That(session.RemainingSeconds,Is.LessThanOrEqualTo(time));Assert.That(player.Stamina,Is.EqualTo(stamina));
            Assert.That(session.SelectedBubbleSeed,Is.EqualTo(seed));Assert.That(session.SelectedBubbleSlots,Is.EquivalentTo(slots));Capture("exit-relocked-1");
        }

        [UnityTest] public IEnumerator NoCheckpointRepeatedContactsAndActualExitUseExistingOwners()
        {
            var start=player.Body.position;
            yield return Move(bubbles[0].transform.position);Assert.That(session.TryCollect(bubbles[0].SlotId),Is.False);
            yield return Move(new Vector2(16.5f,-5.75f));Assert.That(Vector2.Distance(player.Body.position,start),Is.LessThan(.03f));
            Assert.That(session.CollectedCount,Is.Zero);Assert.That(session.ActiveCheckpointId,Is.Empty);
            yield return Move(exit.transform.position);Assert.That(session.HasCompleted,Is.False);
            yield return Move(bubbles[0].transform.position);yield return Move(checkpoint.transform.position);
            for(int i=0;i<4;i++)yield return new WaitForFixedUpdate();
            Assert.That(session.ActivateCheckpoint(checkpoint.CheckpointId,player.transform.position),Is.False);
            yield return Move(bubbles[1].transform.position);yield return Move(bubbles[2].transform.position);
            Assert.That(session.CollectedCount,Is.EqualTo(3));Assert.That(session.TryCollect(bubbles[2].SlotId),Is.False);
            Assert.That(session.HasCompleted,Is.False);yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(session.HasCompleted,Is.False,"Camera reveal cannot resolve the map.");
            yield return Move(exit.transform.position);Assert.That(session.HasCompleted,Is.True);Assert.That(session.State,Is.EqualTo(RunSessionState.Result));
            Assert.That(session.LocalEditorPreview,Is.True);Capture("actual-exit-cleared");
        }

        [UnityTest] public IEnumerator SpringCollisionPlatesKeepCoilsTraversableAndResetDoesNotMoveCollider()
        {
            var side=root.GetComponentsInChildren<CommonObstacleObject>().Single(o=>o.Kind==StageMapObjectKind.CommonC02);
            var top=root.GetComponentsInChildren<CommonObstacleObject>().Single(o=>o.Kind==StageMapObjectKind.CommonC03);
            var sideBox=side.GetComponent<BoxCollider2D>();var topBox=top.GetComponent<BoxCollider2D>();
            Assert.That(sideBox.size.x,Is.EqualTo(.125f));Assert.That(top.GetComponent<PlatformEffector2D>().useOneWay,Is.True);
            yield return Move(new Vector2(21.5f,2.5f));player.Body.linearVelocity=Vector2.down*3;
            for(int i=0;i<14;i++)yield return new WaitForFixedUpdate();Assert.That(player.Body.position.y,Is.LessThan(1.75f),"No side spring ceiling/full-cell body.");
            yield return Move(new Vector2(19.5f,-.4f));player.Body.linearVelocity=Vector2.up*4;
            for(int i=0;i<25;i++)yield return new WaitForFixedUpdate();Assert.That(player.Body.position.y,Is.GreaterThan(1.1f),"Upward pass through coil and one-way plate.");
            Assert.That(top.ActivationCount,Is.Zero);
            yield return Move(new Vector2(19.5f,2.4f));player.Body.linearVelocity=Vector2.down*3;
            for(int i=0;i<20&&top.ActivationCount==0;i++)yield return new WaitForFixedUpdate();
            Assert.That(top.ActivationCount,Is.EqualTo(1));Assert.That(player.Body.linearVelocity.y,Is.GreaterThan(9));
            var offset=topBox.offset;var size=topBox.size;loader.ResetCommonObstacleState();
            Assert.That(topBox.offset,Is.EqualTo(offset));Assert.That(topBox.size,Is.EqualTo(size));Assert.That(top.ActivationCount,Is.Zero);
            Assert.That(root.GetComponentsInChildren<CommonObstacleObject>().Single(o=>o.Kind==StageMapObjectKind.CommonC05).SurfaceEnabled,Is.True);
            Capture("spring-runtime");
        }

        private void Capture(string name)
        {
            var path="Docs/Validation/OVG04/"+name+".png";Directory.CreateDirectory(Path.GetDirectoryName(path));
            var target=new RenderTexture(288,512,24);var prior=camera.targetTexture;var active=RenderTexture.active;
            var texture=new Texture2D(288,512,TextureFormat.RGBA32,false);
            try {camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,288,512),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally {camera.targetTexture=prior;RenderTexture.active=active;target.Release();Object.Destroy(target);Object.Destroy(texture);}
        }

        [UnityTest] public IEnumerator ExistingSaveLocalPlayScreenLoadsSnapshotRestartsSameSlotsAndUsesMobileRouter()
        {
#if UNITY_EDITOR
            root.SetActive(false);Object.Destroy(root);Object.Destroy(snapshot);yield return null;
            var source=AssetDatabase.LoadAssetAtPath<StageMapDefinition>("Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-OVG04-JOIN.asset");
            var before=File.ReadAllBytes(AssetDatabase.GetAssetPath(source));
            var type=System.Type.GetType("ANIMOL.Editor.TerrainEditorAdapter, ANIMOL.Editor");
            var adapter=(ITerrainEditorAdapter)System.Activator.CreateInstance(type,AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)));
            root=new GameObject("OVG04 existing test screen");var screen=root.AddComponent<TerrainEditorScreen>();
            screen.Initialize(adapter,new TerrainEditorViewState());screen.EnterTest();
            yield return null;yield return new WaitForFixedUpdate();
            Assert.That(screen.TestLoader.Map,Is.Not.SameAs(source));Assert.That(screen.TestLoader.Map.Objects.Select(o=>o.StableId),Is.EquivalentTo(source.Objects.Select(o=>o.StableId)));
            Assert.That(screen.TestSession,Is.Not.Null);Assert.That(root.GetComponentsInChildren<ObjectLabSession>().Length,Is.Zero);
            Assert.That(screen.PresentationCamera.enabled,Is.True);Assert.That(screen.PresentationCamera.targetTexture,Is.Null);
            Assert.That(screen.PresentationCamera.cullingMask,Is.Zero);
            Assert.That(screen.CanvasRect.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution,Is.EqualTo(new Vector2(720,1280)));
            var slots=screen.TestSession.SelectedBubbleSlots.ToArray();var seed=screen.TestSession.SelectedBubbleSeed;
            player=screen.TestPlayer;session=screen.TestSession;camera=root.GetComponentsInChildren<Camera>().Single(c=>c.name=="Test camera");
            Assert.That(camera.aspect,Is.EqualTo(9f/16f).Within(.001));
            var router=root.GetComponentInChildren<DevMobileInputRouter>();Assert.That(router,Is.Not.Null);
            Assert.That(router.Begin(601,MobileTouchAction.SwipeMove),Is.True);router.MoveSwipe(601,1);
            Assert.That(player.MoveDirection,Is.EqualTo(1));router.End(601);
            Assert.That(router.Begin(602,MobileTouchAction.Jump),Is.True);router.End(602);
            yield return new WaitForFixedUpdate();Capture("save-local-play-mobile");
            var old=screen.TestPlayer;screen.RestartTest();yield return null;yield return new WaitForFixedUpdate();
            Assert.That(screen.TestPlayer,Is.Not.SameAs(old));Assert.That(screen.TestSession.CollectedCount,Is.Zero);
            Assert.That(screen.TestSession.ActiveCheckpointId,Is.Empty);Assert.That(screen.TestSession.ExitIsOpen,Is.False);
            Assert.That(screen.TestSession.SelectedBubbleSeed,Is.EqualTo(seed));Assert.That(screen.TestSession.SelectedBubbleSlots,Is.EquivalentTo(slots));
            Assert.That(screen.TestLoader.GetComponentsInChildren<CommonObstacleObject>().All(o=>o.SurfaceEnabled),Is.True);
            screen.ReturnToEdit(false);yield return null;
            Assert.That(screen.Adapter.Map,Is.SameAs(source));Assert.That(screen.IsTesting,Is.False);
            Assert.That(screen.CanvasRect.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution,Is.EqualTo(new Vector2(1280,720)));
            Assert.That(File.ReadAllBytes(AssetDatabase.GetAssetPath(source)),Is.EqualTo(before),"Runtime state must not be authored back.");
#else
            yield break;
#endif
        }
    }
}
