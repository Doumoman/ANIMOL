using System;
using System.Collections.Generic;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public enum CommonObstacleState { Idle, Pending, Warning, Inactive }

    [DefaultExecutionOrder(-200)]
    public sealed class CommonObstacleObject : StageMapRuntimeObject
    {
        private BoxCollider2D surface;
        private PhysicsMaterial2D material;
        private readonly List<Collider2D> hits=new();
        private readonly HashSet<DevPlayerController> players=new(), topPlayers=new(), contacts=new();
        private float remaining, activeUntil;
        private bool wasOccupied;
        public StageMapObjectKind Kind => placement.Kind;
        public CommonObstacleState State { get; private set; }
        public bool SurfaceEnabled => surface!=null && surface.enabled && !surface.isTrigger;
        public string Pose { get; private set; }="idle";
        public int ActivationCount { get; private set; }
        private bool Air => Kind is StageMapObjectKind.CommonC04 or StageMapObjectKind.CommonC07;
        private bool Top => Kind is StageMapObjectKind.CommonC01 or StageMapObjectKind.CommonC05 or StageMapObjectKind.CommonC06 or StageMapObjectKind.CommonC09;
        private Vector2 Center => (Vector2)transform.position+Vector2.one*(cellSize*.5f);
        private static ContactFilter2D Filter => new ContactFilter2D { useTriggers=false };

        public override void Configure(StageMapObjectPlacement value,float unitsPerCell)
        {
            if(!CommonObstacleCatalog.IsCurrent(value.Kind))throw new InvalidOperationException("Retired obstacle cannot use common runtime.");
            if(value.Settings.FootprintCells!=Vector2.one)throw new InvalidOperationException("Common devices occupy one cell.");
            CommonObstacleCatalog.ValidateSettings(value);
            base.Configure(value,unitsPerCell);
            surface=GetComponent<BoxCollider2D>();
            if(surface==null)surface=gameObject.AddComponent<BoxCollider2D>();
            surface.isTrigger=Air;
            if(Kind==StageMapObjectKind.CommonC10)
            {
                material=new PhysicsMaterial2D("C10 natural glide"){friction=0,bounciness=0,hideFlags=HideFlags.HideAndDontSave};
                surface.sharedMaterial=material;
            }
            surface.size=Top?new Vector2(cellSize,.12f*cellSize):Vector2.one*cellSize;
            surface.offset=Top?new Vector2(.5f,.94f)*cellSize:Vector2.one*(cellSize*.5f);
            if(Top)
            {
                var effector=GetComponent<PlatformEffector2D>();
                if(effector==null)effector=gameObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay=true;effector.useOneWayGrouping=true;effector.surfaceArc=160;
                surface.usedByEffector=true;
            }
            ResetRuntimeState();
        }
        public override void ResetRuntimeState()
        {
            if(surface==null)return;
            State=Kind==StageMapObjectKind.CommonC09?CommonObstacleState.Inactive:CommonObstacleState.Idle;
            surface.enabled=Kind!=StageMapObjectKind.CommonC09;
            Pose=State==CommonObstacleState.Inactive?"inactive":"idle";
            remaining=0;activeUntil=0;wasOccupied=false;contacts.Clear();ActivationCount=0;
        }
        private void OnDestroy(){if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
        private void FixedUpdate() => Tick(Time.fixedDeltaTime,Time.fixedTime);
        public void Tick(float dt,float sharedTime)
        {
            if(surface==null)return;
            players.Clear();topPlayers.Clear();
            Query(Center,Vector2.one*cellSize);
            foreach(var h in hits)if(h.GetComponentInParent<DevPlayerController>() is { } p)players.Add(p);
            Query((Vector2)transform.position+new Vector2(.5f,1.04f)*cellSize,new Vector2(.96f,.16f)*cellSize);
            foreach(var h in hits)if(h.GetComponentInParent<DevPlayerController>() is { } p && h.bounds.min.y>transform.position.y+.82f*cellSize)topPlayers.Add(p);
            bool occupied=topPlayers.Count>0;
            foreach(var p in players)CommonObstaclePlayerMotion.For(p);
            foreach(var p in topPlayers)CommonObstaclePlayerMotion.For(p);
            Query(Center,Vector2.one*cellSize*3);
            foreach(var h in hits)if(h.GetComponentInParent<DevPlayerController>() is { } nearby)CommonObstaclePlayerMotion.For(nearby);
            if(Kind==StageMapObjectKind.CommonC04)
            {
                float idle=Mathf.Max(.02f,settings.RecoverSeconds),warn=Mathf.Max(.02f,settings.WarningSeconds),active=Mathf.Max(.02f,settings.ActiveSeconds);
                float phase=Mathf.Repeat(sharedTime+settings.PhaseSeed*.02f,idle+warn+active);
                Pose=phase<idle?"idle":phase<idle+warn?"warn":"active";
                if(Pose=="active")foreach(var p in players)StageMapRespawnRouter.TryRespawn(p);
                return;
            }
            if(Kind==StageMapObjectKind.CommonC05)
            {
                if(occupied && SurfaceEnabled){State=CommonObstacleState.Idle;remaining=0;}
                else if(wasOccupied && State==CommonObstacleState.Idle){State=CommonObstacleState.Warning;remaining=settings.WarningSeconds;}
                if(State==CommonObstacleState.Warning && !occupied && (remaining-=dt)<=0)
                {surface.enabled=false;State=CommonObstacleState.Inactive;remaining=settings.ActiveSeconds;}
                else if(State==CommonObstacleState.Inactive && (remaining-=dt)<=0 && !Blocked())
                {surface.enabled=true;State=CommonObstacleState.Idle;}
                Pose=State==CommonObstacleState.Warning?"warn":SurfaceEnabled?"idle":"inactive";
            }
            else if(Kind==StageMapObjectKind.CommonC09)
            {
                bool near=HasNearbyPlayer();
                if(State==CommonObstacleState.Inactive && near){State=CommonObstacleState.Pending;remaining=settings.WarningSeconds;}
                else if(State==CommonObstacleState.Pending)
                {
                    if(!near)State=CommonObstacleState.Inactive;
                    else if((remaining-=dt)<=0 && !Blocked()){surface.enabled=true;State=CommonObstacleState.Idle;}
                }
                else if(State==CommonObstacleState.Idle && !occupied && !near){State=CommonObstacleState.Warning;remaining=settings.WarningSeconds;}
                else if(State==CommonObstacleState.Warning)
                {
                    if(occupied||near)State=CommonObstacleState.Idle;
                    else if((remaining-=dt)<=0){surface.enabled=false;State=CommonObstacleState.Inactive;}
                }
                Pose=State is CommonObstacleState.Pending or CommonObstacleState.Warning?"warn":SurfaceEnabled?"idle":"inactive";
            }
            else if(Kind==StageMapObjectKind.CommonC07)
            {
                var direction=settings.ObstacleFacing==ObstacleFacing.Up?Vector2.up:Vector2.right*(settings.ObstacleFacing==ObstacleFacing.Left?-1:1);
                foreach(var p in players)CommonObstaclePlayerMotion.For(p).Environment(0,direction*settings.EffectStrength,0);
                Pose=players.Count>0?"active":"idle";
            }
            else if(Kind==StageMapObjectKind.CommonC08)
            {
                foreach(var p in topPlayers)CommonObstaclePlayerMotion.For(p).Environment(settings.MovementSpeed*(settings.ObstacleFacing==ObstacleFacing.Left?-1:1),Vector2.zero,0);
                Pose=occupied?"active":"idle";
            }
            else if(Kind==StageMapObjectKind.CommonC10)
            {
                foreach(var p in topPlayers)CommonObstaclePlayerMotion.For(p).Environment(0,Vector2.zero,Mathf.Max(.05f,settings.EffectStrength));
                Pose=occupied?"active":"idle";
            }
            else
            {
                if(Kind==StageMapObjectKind.CommonC01)
                    foreach(var p in players)if(Physics2D.GetIgnoreCollision(p.GetComponent<Collider2D>(),surface))activeUntil=sharedTime+.1f;
                Pose=sharedTime<activeUntil?"active":"idle";
            }
            wasOccupied=occupied;
        }
        private void Query(Vector2 center,Vector2 size){hits.Clear();Physics2D.OverlapBox(center,size,0,Filter,hits);}
        private bool HasNearbyPlayer()
        {
            Query(Center,Vector2.one*(cellSize*(1+2*settings.ActivationRangeCells)));
            foreach(var h in hits)if(h.GetComponentInParent<DevPlayerController>()!=null)return true;
            return false;
        }
        private bool Blocked()
        {
            Query((Vector2)transform.position+surface.offset,surface.size*.98f);
            foreach(var h in hits)if(h!=surface && h.gameObject.activeInHierarchy)return true;
            return false;
        }
        public bool TryContact(DevPlayerController player,Vector2 incoming,bool onTop,float sharedTime)
        {
            if(player==null || contacts.Contains(player))return false;
            var motion=CommonObstaclePlayerMotion.For(player);
            if(Kind==StageMapObjectKind.CommonC02)
            {
                float side=settings.ObstacleFacing==ObstacleFacing.Left?-1:1;
                if((player.Body.position.x-Center.x)*side<0 || incoming.x*side>=0)return false;
                motion.Reflect(incoming);
            }
            else if(Kind is StageMapObjectKind.CommonC03 or StageMapObjectKind.CommonC06)
            {
                if(!onTop||incoming.y>0)return false;
                motion.Launch(settings.VerticalImpulse);
            }
            else return false;
            contacts.Add(player);ActivationCount++;activeUntil=sharedTime+.15f;Pose="active";return true;
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            var p=collision.collider.GetComponentInParent<DevPlayerController>();if(p==null)return;
            var motion=CommonObstaclePlayerMotion.For(p);
            TryContact(p,motion.PhysicsVelocity,collision.collider.bounds.min.y>=transform.position.y+.82f*cellSize,Time.fixedTime);
        }
        private void OnCollisionExit2D(Collision2D collision)
        {var p=collision.collider.GetComponentInParent<DevPlayerController>();if(p!=null)contacts.Remove(p);}
    }
}
