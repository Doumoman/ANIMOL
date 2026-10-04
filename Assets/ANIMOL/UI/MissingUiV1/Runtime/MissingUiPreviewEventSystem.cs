using UnityEngine;
using UnityEngine.EventSystems;

namespace ANIMOL.MissingUiV1
{
    [RequireComponent(typeof(EventSystem))]
    public sealed class MissingUiPreviewEventSystem : MonoBehaviour
    {
        private void Awake()
        {
            foreach (var candidate in FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (candidate != null && candidate.gameObject != gameObject)
                {
                    gameObject.SetActive(false);
                    return;
                }
        }
    }
}
