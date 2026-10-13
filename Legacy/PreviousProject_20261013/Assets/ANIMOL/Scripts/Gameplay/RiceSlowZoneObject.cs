using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class RiceSlowZoneObject : StageMapRuntimeObject
    {
        private readonly HashSet<DevPlayerController> players = new HashSet<DevPlayerController>();
        public int OccupantCount => players.Count;

        public void Enter(DevPlayerController player)
        {
            if (player == null || !players.Add(player)) return;
            player.SetGroundHorizontalSpeedModifier(this, settings.GroundSpeedMultiplier);
            PlayFirstPhase("disturbed", "active");
        }

        public void Exit(DevPlayerController player)
        {
            if (player == null || !players.Remove(player)) return;
            player.ClearGroundHorizontalSpeedModifier(this);
            if (players.Count == 0) PlayFirstPhase("recover", "idle");
        }

        private void OnTriggerEnter2D(Collider2D other) => Enter(other.GetComponentInParent<DevPlayerController>());
        private void OnTriggerExit2D(Collider2D other) => Exit(other.GetComponentInParent<DevPlayerController>());
        private void OnDisable() => ResetRuntimeState();
        public override void ResetRuntimeState()
        {
            foreach (var player in players.ToArray()) if (player != null) player.ClearGroundHorizontalSpeedModifier(this);
            players.Clear(); transform.position = initialPosition;
            PlayIdlePhase();
        }
    }
}
