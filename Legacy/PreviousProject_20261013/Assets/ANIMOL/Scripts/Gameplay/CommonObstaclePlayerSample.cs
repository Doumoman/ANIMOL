using UnityEngine;

namespace ANIMOL.Gameplay
{
    // Runs before the existing player motor; the companion applies only active device effects afterwards.
    [DefaultExecutionOrder(-100)]
    public sealed class CommonObstaclePlayerSample : MonoBehaviour
    {
        public Vector2 Incoming { get; private set; }
        private CommonObstaclePlayerMotion motion;
        private void Awake() => motion=GetComponent<CommonObstaclePlayerMotion>();
        private void FixedUpdate()
        {
            var body=GetComponent<Rigidbody2D>();
            if(motion==null)motion=GetComponent<CommonObstaclePlayerMotion>();
            if(body==null)return;
            Incoming=body.linearVelocity;
            if(motion!=null)body.linearVelocity-=Vector2.right*motion.PreviousBelt;
        }
    }
}
