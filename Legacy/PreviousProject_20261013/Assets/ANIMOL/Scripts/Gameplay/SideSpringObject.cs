using System.Collections.Generic;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public sealed class SideSpringObject : StageMapRuntimeObject
    {
        private readonly HashSet<int> contactedBodies = new HashSet<int>();
        public int ActivationCount { get; private set; }

        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            transform.localScale = new Vector3(settings.Direction == StageMapObjectDirection.Left ? -1f : 1f, 1f, 1f);
        }

        public bool TryLaunch(DevPlayerController player, float playerCenterX)
        {
            if (player == null || contactedBodies.Contains(player.GetInstanceID())) return false;
            var facing = settings.Direction == StageMapObjectDirection.Left ? -1f : 1f;
            if (settings.SpringContactPolicy == SideSpringContactPolicy.FacingSideOnly &&
                Mathf.Sign(playerCenterX - (transform.position.x + cellSize * .5f)) != facing) return false;
            contactedBodies.Add(player.GetInstanceID());
            player.ApplyDirectionalSpringImpulse(new Vector2(facing * settings.HorizontalImpulse, settings.VerticalImpulse));
            ActivationCount++;
            PlayFirstPhase("launch", "active");
            return true;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<DevPlayerController>();
            if (player != null) TryLaunch(player, collision.collider.bounds.center.x);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<DevPlayerController>();
            if (player != null && contactedBodies.Remove(player.GetInstanceID())) PlayFirstPhase("reload", "recover", "idle");
        }

        public override void ResetRuntimeState()
        {
            contactedBodies.Clear(); ActivationCount = 0; transform.position = initialPosition;
            PlayIdlePhase();
        }
    }
}
