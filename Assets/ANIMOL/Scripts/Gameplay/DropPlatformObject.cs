using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(BoxCollider2D), typeof(PlatformEffector2D))]
    public sealed class DropPlatformObject : StageMapRuntimeObject
    {
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell);
            var collider = GetComponent<BoxCollider2D>(); collider.usedByEffector = true; collider.isTrigger = false;
            collider.size = new Vector2(cellSize, cellSize * .2f); collider.offset = new Vector2(cellSize * .5f, cellSize * .85f);
            var effector = GetComponent<PlatformEffector2D>(); effector.useOneWay = true; effector.surfaceArc = 160f;
            PlayIdlePhase();
        }
        public override void ResetRuntimeState() { transform.position = initialPosition; PlayIdlePhase(); }
    }
}
