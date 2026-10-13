using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class HalfBlockObject : StageMapRuntimeObject
    {
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            var collider = GetComponent<BoxCollider2D>(); collider.isTrigger = false;
            collider.size = new Vector2(cellSize, cellSize * .5f);
            collider.offset = new Vector2(cellSize * .5f, cellSize * (settings.HalfPlacement == HalfBlockPlacement.Upper ? .75f : .25f));
            var visual = GetComponentInChildren<SpriteRenderer>();
            if (visual != null) { visual.transform.localPosition = new Vector3(cellSize * .5f, cellSize * .5f, 0f); visual.flipY = settings.HalfPlacement == HalfBlockPlacement.Upper; }
            PlayIdlePhase();
        }
        public override void ResetRuntimeState() { transform.position = initialPosition; PlayIdlePhase(); }
    }
}
