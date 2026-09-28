using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DevBubblePickup : MonoBehaviour
    {
        [SerializeField] private string slotId = string.Empty;
        public string SlotId => slotId;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<DevPlayerController>();
            if (player == null) return;
            var session = FindFirstObjectByType<DevTestSession>();
            if (session != null && session.TryCollect(slotId, "local-player")) gameObject.SetActive(false);
        }
    }
}
