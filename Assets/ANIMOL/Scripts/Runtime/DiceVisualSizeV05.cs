using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    // Presentation size only. Never apply this to the collider/board-entity root.
    public static class DiceVisualSizeV05
    {
        public const float RelativeToR4 = .75f;
        public const float OriginalCanvasWidthInCells = 320f / 256f;
        public const float CanvasWidthInCells = OriginalCanvasWidthInCells * RelativeToR4;

        // Pass the host's UNREDUCED visual scale each time; do not multiply an already reduced value.
        public static Vector3 FromOriginalScale(Vector3 originalVisualScale)
        {
            return new Vector3(originalVisualScale.x * RelativeToR4,
                originalVisualScale.y * RelativeToR4, originalVisualScale.z);
        }
    }

    // Keeps the unscaled prefab/spawn value through visual updates and pool reuse.
    [DisallowMultipleComponent]
    public sealed class DiceVisualScaleBindingV05 : MonoBehaviour
    {
        [SerializeField] SpriteRenderer graphics;
        [SerializeField] Vector3 baseDiceGraphicsScale;
        [SerializeField] bool hasBaseScale;

        public Vector3 BaseDiceGraphicsScale => baseDiceGraphicsScale;

        public bool Bind(SpriteRenderer diceRenderer, out string error)
        {
            error = null;
            if (!diceRenderer || diceRenderer.transform == transform ||
                !diceRenderer.transform.IsChildOf(transform))
            {
                error = "The die renderer must live on a visual child of its entity root.";
                return false;
            }
            if (!hasBaseScale || graphics != diceRenderer)
            {
                graphics = diceRenderer;
                baseDiceGraphicsScale = graphics.transform.localScale;
                hasBaseScale = true;
            }
            Apply();
            return true;
        }

        public void Apply()
        {
            if (hasBaseScale && graphics)
                graphics.transform.localScale = DiceVisualSizeV05.FromOriginalScale(baseDiceGraphicsScale);
        }

        void OnEnable() { Apply(); }
        void LateUpdate() { Apply(); }
    }
}
