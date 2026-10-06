using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using ANIMOL.Gameplay.ObstacleGraphics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    [Category("CommonObstaclesV6")]
    public sealed class CommonObstaclePlayModeTests
    {
        private readonly List<Object> created=new();
        [TearDown] public void Cleanup(){foreach(var o in created)if(o!=null)Object.DestroyImmediate(o);created.Clear();}
        private CommonObstacleObject Device(int number,Vector2 position=default)
        {
            var type=CommonObstacleCatalog.Load().Find((StageMapObjectKind)(1000+number));
            var go=new GameObject("C"+number);created.Add(go);var owner=go.AddComponent<CommonObstacleObject>();
            var settings=type.DefaultSettings.Clone();settings.EditorSetObstacleStyle("T01_A");
            owner.Configure(new StageMapObjectPlacement("device",type.Kind,(int)position.x,(int)position.y,type.StableTypeId,null,settings),1);
            owner.enabled=false;return owner;
        }
        private DevPlayerController Player(float x,float y,bool enabled=false)
        {
            var go=new GameObject("Real player",typeof(Rigidbody2D),typeof(BoxCollider2D));created.Add(go);
            var box=go.GetComponent<BoxCollider2D>();box.size=new Vector2(.45f,.8f);
            var p=go.AddComponent<DevPlayerController>();p.enabled=enabled;p.Body.gravityScale=0;p.Body.constraints=RigidbodyConstraints2D.FreezeRotation;p.Body.position=new Vector2(x,y);
            CommonObstaclePlayerMotion.For(p);Physics2D.SyncTransforms();return p;
        }
        [Test] public void ReflectionPreservesSpeedVerticalStaminaAndCooldown()
        {
            var d=Device(2);var p=Player(1.3f,.5f);float stamina=p.Stamina,cooldown=p.ManualTransformCooldownRemaining;
            Assert.That(d.TryContact(p,new Vector2(-7,-3),false,0),Is.True);
            Assert.That(p.Body.linearVelocity,Is.EqualTo(new Vector2(7,-3)));Assert.That(p.Stamina,Is.EqualTo(stamina));Assert.That(p.ManualTransformCooldownRemaining,Is.EqualTo(cooldown));
            Assert.That(d.TryContact(p,new Vector2(-7,-3),false,0),Is.False);Assert.That(d.ActivationCount,Is.EqualTo(1));
            var wrong=Player(-.5f,.5f);Assert.That(d.TryContact(wrong,new Vector2(7,0),false,0),Is.False);
        }
        [UnityTest] public IEnumerator ActualCollisionReflectsUsingPreSolverVelocity()
        {
            var d=Device(2);d.enabled=true;var p=Player(1.8f,.5f);p.Body.linearVelocity=new Vector2(-6,0);
            for(int i=0;i<18 && d.ActivationCount==0;i++)yield return new WaitForFixedUpdate();
            Assert.That(d.ActivationCount,Is.EqualTo(1));Assert.That(p.Body.linearVelocity.x,Is.EqualTo(6).Within(.05f));
        }
        [Test] public void SpringAndCushionAreVerticalOnlyAndDifferentStrengths()
        {
            foreach(int n in new[]{3,6})
            {
                var d=Device(n,new Vector2(n*3,0));var p=Player(n*3+.5f,1.4f);p.Body.linearVelocity=new Vector2(4,-2);
                Assert.That(d.TryContact(p,new Vector2(4,-2),true,0),Is.True);
                Assert.That(p.Body.linearVelocity.x,Is.EqualTo(4));Assert.That(p.Body.linearVelocity.y,Is.EqualTo(n==3?11:5));
            }
        }
        [UnityTest] public IEnumerator SpringRiseSurvivesExistingPlayerJumpReleaseLogic()
        {
            var d=Device(3);var p=Player(.5f,1.45f,true);d.TryContact(p,new Vector2(0,-2),true,0);
            for(int i=0;i<8;i++)yield return new WaitForFixedUpdate();
            Assert.That(p.Body.position.y,Is.GreaterThan(2.3f));
        }
        [Test] public void DepartureWaitsForLastPlayerAndBlocksUnsafeRecovery()
        {
            var d=Device(5);var a=Player(.25f,1.4f);var b=Player(.75f,1.4f);d.Tick(.02f,0);
            a.Body.position=new Vector2(4,4);Physics2D.SyncTransforms();d.Tick(1,1);Assert.That(d.SurfaceEnabled,Is.True);Assert.That(d.Pose,Is.EqualTo("idle"));
            b.Body.position=new Vector2(4,4);Physics2D.SyncTransforms();d.Tick(.1f,2);Assert.That(d.Pose,Is.EqualTo("warn"));Assert.That(d.SurfaceEnabled,Is.True);
            d.Tick(.5f,3);Assert.That(d.SurfaceEnabled,Is.False);
            a.Body.position=new Vector2(.5f,.95f);Physics2D.SyncTransforms();d.Tick(5,8);Assert.That(d.SurfaceEnabled,Is.False);
            a.Body.position=new Vector2(4,4);Physics2D.SyncTransforms();d.Tick(.1f,9);Assert.That(d.SurfaceEnabled,Is.True);
        }
        [Test] public void ApproachPendingAndDepartureWarningHaveDifferentSurfaces()
        {
            var d=Device(9);var p=Player(2,1);d.Tick(.02f,0);Assert.That(d.State,Is.EqualTo(CommonObstacleState.Pending));Assert.That(d.Pose,Is.EqualTo("warn"));Assert.That(d.SurfaceEnabled,Is.False);
            p.Body.position=new Vector2(.5f,.95f);Physics2D.SyncTransforms();d.Tick(1,1);Assert.That(d.SurfaceEnabled,Is.False);
            p.Body.position=new Vector2(2,1);Physics2D.SyncTransforms();d.Tick(.1f,2);Assert.That(d.SurfaceEnabled,Is.True);
            p.Body.position=new Vector2(10,10);Physics2D.SyncTransforms();d.Tick(.1f,3);Assert.That(d.Pose,Is.EqualTo("warn"));Assert.That(d.SurfaceEnabled,Is.True);
            d.Tick(1,4);Assert.That(d.SurfaceEnabled,Is.False);
        }
        [Test] public void HazardUsesSharedClockAndAirHasNoSolidSurface()
        {
            var a=Device(4);var b=Device(4,new Vector2(10,0));var wind=Device(7,new Vector2(20,0));
            foreach(float time in new[]{0f,1.1f,1.8f,7.4f}){a.Tick(.02f,time);b.Tick(.4f,time);Assert.That(a.Pose,Is.EqualTo(b.Pose));}
            Assert.That(a.SurfaceEnabled,Is.False);Assert.That(wind.SurfaceEnabled,Is.False);Assert.That(a.GetComponent<BoxCollider2D>().isTrigger,Is.True);
        }
        [UnityTest] public IEnumerator WindBeltAndNaturalFrictionAffectActualBodies()
        {
            var wind=Device(7);wind.enabled=true;var a=Player(.5f,.5f);
            var belt=Device(8,new Vector2(10,0));belt.enabled=true;var b=Player(10.5f,1.4f,true);
            var ice=Device(10,new Vector2(20,0));ice.enabled=true;var c=Player(20.5f,1.4f,true);c.Body.linearVelocity=new Vector2(4,0);
            for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
            Assert.That(a.Body.linearVelocity.y,Is.GreaterThan(1));Assert.That(b.Body.linearVelocity.x,Is.GreaterThan(1.5f));
            Assert.That(c.Body.linearVelocity.x,Is.InRange(3.8f,4f));Assert.That(c.Stamina,Is.LessThanOrEqualTo(100));
        }
        [Test] public void HazardRespawnsActualPlayerWithoutResettingOtherDevices()
        {
            var hazard=Device(4,new Vector2(100,0));var p=Player(100.5f,.5f);
            var lab=new GameObject("Local play service");created.Add(lab);var session=lab.AddComponent<ObjectLabSession>();
            hazard.Tick(.02f,1.8f);
            Assert.That(session.RespawnCount,Is.EqualTo(1));Assert.That(p.Body.position,Is.EqualTo(Vector2.zero));
            Assert.That(hazard.Pose,Is.EqualTo("active"));
        }
        [Test] public void EightPlayersCannotRemoveOccupiedSurface()
        {
            var d=Device(5);var players=Enumerable.Range(0,8).Select(i=>Player(.5f,1.4f)).ToArray();d.Tick(.02f,0);
            for(int i=0;i<7;i++)players[i].Body.position=new Vector2(5,5);
            Physics2D.SyncTransforms();d.Tick(5,5);Assert.That(d.SurfaceEnabled,Is.True);Assert.That(d.Pose,Is.EqualTo("idle"));
            players[7].Body.position=new Vector2(5,5);Physics2D.SyncTransforms();d.Tick(.1f,6);Assert.That(d.Pose,Is.EqualTo("warn"));
        }
        [Test] public void CurrentCatalogHasOnlyTenNewTypesAndNoLegacySpritePrefabs()
        {
            var types=CommonObstacleCatalog.Load().Types;Assert.That(types.Count,Is.EqualTo(10));
            foreach(var t in types){Assert.That(CommonObstacleCatalog.IsCurrent(t.Kind),Is.True);Assert.That(t.Prefab.GetComponentsInChildren<SpriteRenderer>(),Is.Empty);Assert.That(t.Prefab.GetComponent<CommonObstacleObject>(),Is.Not.Null);}
            Assert.That(CommonObstacleCatalog.IsRetired(StageMapObjectKind.DropPlatform),Is.True);Assert.That(CommonObstacleCatalog.IsRetired(StageMapObjectKind.MoonLanternStep),Is.True);
        }
        [Test] public void RetiredPlacementsDoNotSpawnRenderCollideOrReserveCells()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();created.Add(map);map.EditorInitializeIdentity("RETIRE-TEST","T01");map.EditorInitializeBoundsFromAuthoredContent(1);
            var old=new GameObject("Old square",typeof(SpriteRenderer),typeof(BoxCollider2D),typeof(DropPlatformObject));created.Add(old);old.SetActive(false);
            map.EditorPlaceObject("old",StageMapObjectKind.DropPlatform,0,0,"",old,new StageMapObjectSettings());
            Assert.That(StageMapDefinition.EnumeratePlacementCells(map.Objects.Single()),Is.Empty);map.EditorMarkCollisionDataSynchronized();
            var root=new GameObject("loader");created.Add(root);var loader=root.AddComponent<StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureStandaloneDevelopmentOwner();loader.Load(map);
            Assert.That(root.GetComponentsInChildren<StageMapRuntimeObject>(),Is.Empty);Assert.That(root.GetComponentsInChildren<SpriteRenderer>(),Is.Empty);
            Assert.That(map.Objects.Count,Is.EqualTo(1));
        }
    }
}
