using UnityEngine;

namespace ANIMOL.MissingUiV1
{
    // The production host owns permissions, async state, cancellation and final outcomes.
    // Implement against inspected project services, never by echoing UI data as approval.
    public abstract class MissingUiHostBridge : MonoBehaviour
    {
        public abstract bool CanInvoke(string screenId, string semanticKey, string action);
        public abstract void Invoke(MissingUiAction request);
    }
}
