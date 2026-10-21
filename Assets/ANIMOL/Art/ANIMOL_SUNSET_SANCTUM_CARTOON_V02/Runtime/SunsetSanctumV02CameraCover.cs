using UnityEngine;

namespace ANIMOL.Art.SunsetSanctumV02
{
    /// <summary>
    /// Opaque sky backplate. Follows camera XY and uniformly covers its orthographic
    /// view across aspect ratios and zoom. Never use this on gameplay sprites.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(10010)]
    public sealed class SunsetSanctumV02CameraCover : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField, Range(0f, 0.5f)] private float overscanFraction = 0.08f;
        private SpriteRenderer spriteRenderer;
        private float preservedScaleZ = 1f;

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            preservedScaleZ = transform.localScale.z;
            if (targetCamera == null) targetCamera = Camera.main;
        }

        public void Configure(Camera sceneCamera, float overscan = 0.08f)
        {
            targetCamera = sceneCamera;
            overscanFraction = Mathf.Clamp(overscan, 0f, 0.5f);
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            FitNow();
        }

        private void LateUpdate()
        {
            FitNow();
        }

        public void FitNow()
        {
            if (targetCamera == null || !targetCamera.orthographic ||
                spriteRenderer == null || spriteRenderer.sprite == null) return;
            if (Mathf.Abs(targetCamera.transform.forward.z) < 0.9999f ||
                Mathf.Abs(targetCamera.transform.up.y) < 0.9999f) return;
            // Parent and sprite rotation must remain identity for an XY backplate.
            if (Quaternion.Angle(transform.rotation, Quaternion.identity) > 0.01f) return;
            Vector3 nativeSize = spriteRenderer.sprite.bounds.size;
            if (nativeSize.x <= 0f || nativeSize.y <= 0f) return;

            float viewHeight = targetCamera.orthographicSize * 2f;
            float viewWidth = viewHeight * targetCamera.aspect;
            float uniformWorldScale = Mathf.Max(viewWidth / nativeSize.x, viewHeight / nativeSize.y) *
                (1f + Mathf.Clamp(overscanFraction, 0f, 0.5f));
            Vector3 parentScale = transform.parent == null ? Vector3.one : transform.parent.lossyScale;
            if (Mathf.Abs(parentScale.x) < 0.0001f || Mathf.Abs(parentScale.y) < 0.0001f) return;
            transform.localScale = new Vector3(uniformWorldScale / Mathf.Abs(parentScale.x),
                uniformWorldScale / Mathf.Abs(parentScale.y), preservedScaleZ);

            // Account for non-centred sprite pivots while preserving authored world Z.
            Vector3 centreOffset = transform.TransformVector(spriteRenderer.sprite.bounds.center);
            Vector3 position = transform.position;
            position.x = targetCamera.transform.position.x - centreOffset.x;
            position.y = targetCamera.transform.position.y - centreOffset.y;
            transform.position = position;
        }
    }
}
