using UnityEngine;

namespace ANIMOL.Gameplay
{
    [DefaultExecutionOrder(100)]
    public sealed class CommonObstaclePlayerMotion : MonoBehaviour
    {
        private DevPlayerController player;
        private CommonObstaclePlayerSample sample;
        private float environmentTick=-1, belt, drag;
        private Vector2 wind;
        private bool externalRise;
        public Vector2 PhysicsVelocity { get; private set; }
        public float PreviousBelt { get; private set; }
        public static CommonObstaclePlayerMotion For(DevPlayerController player)
        {
            var result=player.GetComponent<CommonObstaclePlayerMotion>();
            if(result==null)result=player.gameObject.AddComponent<CommonObstaclePlayerMotion>();
            return result;
        }
        private void Awake()
        {
            player=GetComponent<DevPlayerController>();
            sample=GetComponent<CommonObstaclePlayerSample>();
            if(sample==null)sample=gameObject.AddComponent<CommonObstaclePlayerSample>();
            PhysicsVelocity=player.Body.linearVelocity;
        }
        public void Environment(float beltSpeed, Vector2 windAcceleration, float naturalDrag)
        {
            if(environmentTick!=Time.fixedTime){environmentTick=Time.fixedTime;belt=0;wind=Vector2.zero;drag=0;}
            // Adjacent cells of the same belt do not double its speed.
            if(Mathf.Abs(beltSpeed)>Mathf.Abs(belt))belt=beltSpeed;
            if(windAcceleration.sqrMagnitude>wind.sqrMagnitude)wind=windAcceleration;
            if(naturalDrag>0)drag=drag==0?naturalDrag:Mathf.Min(drag,naturalDrag);
        }
        public void ClearEffects()
        {environmentTick=-1;belt=0;wind=Vector2.zero;drag=0;PreviousBelt=0;externalRise=false;}
        public void Launch(float verticalSpeed)
        {
            player.Body.linearVelocity=new Vector2(player.Body.linearVelocity.x,verticalSpeed);
            externalRise=true;
        }
        public void Reflect(Vector2 preImpact)
        {
            player.Body.linearVelocity=new Vector2(-preImpact.x,preImpact.y);
        }
        private void Update()
        {if(Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.S))player.RequestDropThroughOneWayPlatform();}
        private void FixedUpdate()
        {
            var velocity=player.Body.linearVelocity;
            if(externalRise || environmentTick==Time.fixedTime && wind.y>0)
            {
                if(sample.Incoming.y<=0 || (player.IsGrounded && sample.Incoming.y<=.01f))externalRise=false;
                else if(Mathf.Approximately(velocity.y,sample.Incoming.y*.5f))velocity.y=sample.Incoming.y;
            }
            if(environmentTick==Time.fixedTime)
            {
                if(drag>0 && player.Pace==ANIMOL.Core.LocomotionPace.Idle && !player.IsStunned && !player.IsChanneling)
                    velocity.x=Mathf.MoveTowards(sample.Incoming.x-PreviousBelt,0,drag*Time.fixedDeltaTime);
                velocity+=wind*Time.fixedDeltaTime;
                velocity.x+=belt;
                PreviousBelt=belt;
            }
            else PreviousBelt=0;
            player.Body.linearVelocity=velocity;
            PhysicsVelocity=velocity;
        }
    }
}
