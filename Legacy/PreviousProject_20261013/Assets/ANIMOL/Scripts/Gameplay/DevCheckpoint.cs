using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DevCheckpoint : MonoBehaviour
    {
        [SerializeField] private string checkpointId = string.Empty;
        public string CheckpointId => checkpointId;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<DevPlayerController>() == null) return;
            FindFirstObjectByType<DevTestSession>()?.ActivateCheckpoint(checkpointId, transform.position);
        }
    }
}
