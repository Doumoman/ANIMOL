using UnityEngine;

namespace Animol.QA
{
    [RequireComponent(typeof(Camera))]
    public sealed class UnityTemporaryCamera2D : MonoBehaviour
    {
        public Transform Target;
        public float WorldWidth = 22, ViewHeight = 20;
        void LateUpdate()
        {
            if (Target == null) return;
            var camera = GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = ViewHeight * .5f;
            float halfWidth = Mathf.Min(WorldWidth * .5f, camera.orthographicSize * camera.aspect);
            float x = Mathf.Clamp(Target.position.x, halfWidth, WorldWidth - halfWidth);
            transform.position = new Vector3(Mathf.Round(x * 32) / 32, ViewHeight * .5f, -10);
        }
    }
}
