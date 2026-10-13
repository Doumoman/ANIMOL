using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DevExitGateView : MonoBehaviour
    {
        [SerializeField] private Renderer visual;

        private void Start() => SetOpenVisual(false);

        public void SetOpenVisual(bool open)
        {
            if (visual == null) visual = GetComponentInChildren<Renderer>();
            if (visual != null) visual.material.color = open ? new Color(0.15f, 0.95f, 0.45f) : new Color(0.9f, 0.2f, 0.2f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<DevPlayerController>() == null) return;
            FindFirstObjectByType<DevTestSession>()?.TryEnterExit();
        }
    }
}
