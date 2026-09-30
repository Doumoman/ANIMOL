using System.Collections.Generic;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class MoonRabbitBowlObject : StageMapRuntimeObject
    {
        private readonly HashSet<DevPlayerController> occupants = new HashSet<DevPlayerController>(); private SpriteRenderer visual;
        public bool Charged { get; private set; } public bool Compressed => occupants.Count > 0; public int AssistCount { get; private set; }
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell) { base.Configure(value, unitsPerCell); visual = GetComponentInChildren<SpriteRenderer>(); ResetRuntimeState(); }
        public void Land(DevPlayerController player) { if (player == null || !occupants.Add(player)) return; if (visual != null) visual.transform.localScale = new Vector3(1f, .72f, 1f); PlayPhase("charge"); }
        public void Leave(DevPlayerController player) { if (player == null || !occupants.Remove(player)) return; Charged = true; player.SetNextJumpVerticalAssist(this, settings.VerticalImpulse * Mathf.Max(.1f, settings.EffectStrength)); AssistCount++; if (visual != null) visual.transform.localScale = Vector3.one; PlayPhase("active"); }
        private void OnCollisionEnter2D(Collision2D collision) { var player = collision.collider.GetComponentInParent<DevPlayerController>(); if (player != null && collision.collider.bounds.min.y >= GetComponent<Collider2D>().bounds.center.y - .15f) Land(player); }
        private void OnCollisionExit2D(Collision2D collision) => Leave(collision.collider.GetComponentInParent<DevPlayerController>());
        public void NotifyAssistConsumed() { Charged = false; PlayPhase("recover"); }
        public override void ResetRuntimeState() { foreach (var player in occupants) if (player != null) player.ClearNextJumpVerticalAssist(this); occupants.Clear(); Charged = false; AssistCount = 0; transform.position = initialPosition; if (visual != null) visual.transform.localScale = Vector3.one; PlayIdlePhase(); }
    }
}
