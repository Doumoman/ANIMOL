using System.Collections;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using ANIMOL.Gameplay.ObstacleGraphics;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    [Category("ObstacleGraphics")]
    public sealed class ObstacleGraphicsPlayModeTests
    {
        [UnityTest] public IEnumerator ExistingOneWayOwnerLetsActualPlayerRiseAndLandWithV6Graphics()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();map.EditorInitializeIdentity("OVG-PLAY","T01");map.EditorInitializeBoundsFromAuthoredContent(1);
            var settings=new StageMapObjectSettings();settings.EditorSetObstacleStyle("T01_A");
            var placement=new StageMapObjectPlacement("drop",StageMapObjectKind.DropPlatform,0,3,"",null,settings);
            var root=new GameObject("OVG one way",typeof(BoxCollider2D),typeof(PlatformEffector2D));var owner=root.AddComponent<DropPlatformObject>();StageMapRuntimeFactory.Configure(root,placement,1);
            var artRoot=new GameObject("OVG art");var art=artRoot.AddComponent<FreeShapeTerrainRenderer>();
            var player=new GameObject("Actual Dev player",typeof(Rigidbody2D),typeof(BoxCollider2D));var body=player.GetComponent<Rigidbody2D>();body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeRotation;
            var box=player.GetComponent<BoxCollider2D>();box.size=new Vector2(.5f,1);var controller=player.AddComponent<DevPlayerController>();controller.enabled=false;
            try
            {
                var snap=ObstacleSnapshotAdapter.Read(map,placement,owner);art.Rebuild(map.FreeShapeTerrain,1,new System.Collections.Generic.Dictionary<Vector2Int,GraphicCell>{{new Vector2Int(0,3),snap}});
                Assert.That(artRoot.GetComponentsInChildren<Collider2D>(),Is.Empty);
                player.transform.position=new Vector3(.5f,1,0);body.linearVelocity=new Vector2(0,8);Physics2D.SyncTransforms();
                for(int i=0;i<32;i++)yield return new WaitForFixedUpdate();Assert.That(body.position.y,Is.GreaterThan(4.5f));
                body.linearVelocity=new Vector2(0,-8);for(int i=0;i<42;i++)yield return new WaitForFixedUpdate();
                Assert.That(box.bounds.min.y,Is.EqualTo(root.GetComponent<BoxCollider2D>().bounds.max.y).Within(.08f));
                Assert.That(root.GetComponent<PlatformEffector2D>().useOneWay,Is.True);
            }
            finally{Object.Destroy(player);Object.Destroy(artRoot);Object.Destroy(root);Object.Destroy(map);}yield return null;
        }
        [UnityTest] public IEnumerator ReloadDoesNotReadDeferredOldOwnersOrLeaveDeletedDeviceGraphics()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();map.EditorInitializeIdentity("OVG-RELOAD","T01");map.EditorInitializeBoundsFromAuthoredContent(1);
            map.FreeShapeTerrain.cells.Add(new FreeShapeCell{x=-1,y=0,styleId="T01_A"});
            var template=new GameObject("Drop template",typeof(BoxCollider2D),typeof(PlatformEffector2D),typeof(DropPlatformObject));template.transform.position=new Vector3(1000,1000,0);
            var settings=new StageMapObjectSettings();settings.EditorSetObstacleStyle("T01_A");map.EditorPlaceObject("drop",StageMapObjectKind.DropPlatform,0,0,"",template,settings);
            var root=new GameObject("Reload loader");var loader=root.AddComponent<StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureStandaloneDevelopmentOwner();
            try
            {
                loader.Load(map);loader.Load(map);yield return null;
                Assert.That(root.GetComponentsInChildren<DropPlatformObject>().Length,Is.EqualTo(1));Assert.That(root.GetComponent<ObstacleRuntimeGraphics>(),Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<FreeShapeTerrainRenderer>().Length,Is.EqualTo(1));
                map.EditorRemoveObject("drop");loader.Load(map);yield return null;
                Assert.That(root.GetComponentsInChildren<DropPlatformObject>(),Is.Empty);Assert.That(root.GetComponent<ObstacleRuntimeGraphics>(),Is.Null);
                Assert.That(root.GetComponentInChildren<FreeShapeTerrainRenderer>().GetComponentsInChildren<SpriteRenderer>(),Is.Empty);
            }
            finally{Object.Destroy(root);Object.Destroy(template);Object.Destroy(map);}yield return null;
        }
        [UnityTest] public IEnumerator LanternSnapshotReadsRealWarningHiddenAndResetWithoutOwningState()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();map.EditorInitializeIdentity("OVG-LANTERN","T01");map.EditorInitializeBoundsFromAuthoredContent(1);
            var settings=new StageMapObjectSettings();settings.EditorSetObstacleStyle("T01_A");settings.EditorConfigureDesignBehavior(.04f,.2f,.1f,MapObjectPassengerPolicy.DeferStateChangeWhileOccupied,MapObjectTriggerMode.OnOccupancy,0,0,1,true);
            var p=new StageMapObjectPlacement("lantern",StageMapObjectKind.MoonLanternStep,0,0,"",null,settings);
            var root=new GameObject("Operational lantern",typeof(BoxCollider2D),typeof(PlatformEffector2D));var owner=root.AddComponent<MoonLanternStepObject>();StageMapRuntimeFactory.Configure(root,p,1);
            try
            {
                owner.NotifyOccupied(true);owner.NotifyOccupied(false);yield return new WaitForFixedUpdate();yield return null;
                var snap=ObstacleSnapshotAdapter.Read(map,p,owner);Assert.That(owner.State,Is.EqualTo(LanternStepState.Warning));Assert.That(snap.Pose,Is.EqualTo("warn"));Assert.That(snap.SurfaceEnabled,Is.True);
                for(int i=0;i<4;i++)yield return new WaitForFixedUpdate();snap=ObstacleSnapshotAdapter.Read(map,p,owner);Assert.That(snap.SurfaceEnabled,Is.False);Assert.That(snap.Pose,Is.EqualTo("inactive"));
                var state=owner.State;for(int i=0;i<100;i++)ObstacleSnapshotAdapter.Read(map,p,owner);Assert.That(owner.State,Is.EqualTo(state));
                owner.ResetRuntimeState();snap=ObstacleSnapshotAdapter.Read(map,p,owner);Assert.That(snap.SurfaceEnabled,Is.True);Assert.That(snap.Pose,Is.EqualTo("idle"));
            }
            finally{Object.Destroy(root);Object.Destroy(map);}yield return null;
        }
    }
}
