using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public sealed class WideWorldCameraPolicy : MonoBehaviour
    {
        private Camera worldCamera;
        public static float VisibleWorldWidth(float orthographicSize, float aspect) => orthographicSize * 2f * aspect;
        private void Awake()
        {
            worldCamera = GetComponent<Camera>();
            worldCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }
}
