using UnityEngine;
namespace ANIMOL.Puzzle
{
    [DefaultExecutionOrder(200)]
    public sealed class PuzzleParallax : MonoBehaviour
    {
        public Transform cameraTransform;
        [Range(0, 1)] public float factor;
        public Vector2 frameSize;
        Vector3 origin, cameraOrigin;
        Vector2 offset;
        public Vector2 CurrentOffset => offset;
        public Vector2 OffsetLimit => frameSize * (.003f + .02f * factor);
        public void Initialize(Transform camera, float amount, Vector2 size)
        { cameraTransform = camera; factor = amount; frameSize = size; origin = transform.position; cameraOrigin = camera.position; offset = Vector2.zero; }
        void LateUpdate()
        {
            if (!cameraTransform) return;
            var delta = cameraTransform.position - cameraOrigin;
            var drift = new Vector2(Mathf.Sin(Time.unscaledTime * .17f), Mathf.Cos(Time.unscaledTime * .13f)) * .2f;
            var depth = new Vector2((float)System.Math.Tanh(delta.x * .12f), (float)System.Math.Tanh(delta.y * .12f));
            var limit = OffsetLimit;
            var target = Vector2.Scale((depth + drift) / 1.2f, limit);
            offset = Vector2.Lerp(offset, target, 1 - Mathf.Exp(-1.5f * Time.unscaledDeltaTime));
            transform.position = origin + (Vector3)offset;
        }
    }
}
