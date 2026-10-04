using System.Collections;
using ANIMOL.UI;
using UnityEngine;

namespace ANIMOL.AnimalStagePhase2
{
    [DefaultExecutionOrder(1000)]
    public sealed class AnimalStagePhase2Entry : MonoBehaviour
    {
        private UiNavigationService navigation;
        private AnimalStagePhase2Host host;
        private IEnumerator Start()
        {
            yield return null; yield return null;
            navigation = GetComponent<UiNavigationService>();
            var screen = navigation.transform.Find("SafeArea/ScreenHost/" + AnimalStagePhase2Host.ScreenId);
            var prefab = Resources.Load<GameObject>(AnimalStagePhase2Host.ResourcePath);
            if (screen == null || prefab == null) yield break;
            host = screen.GetComponentInChildren<AnimalStagePhase2Host>(true);
            if (host == null)
            {
                foreach (Transform child in screen) child.gameObject.SetActive(false);
                var instance = Instantiate(prefab, screen, false);
                instance.name = "StageAnimalSelect";
                host = instance.GetComponent<AnimalStagePhase2Host>();
            }
            navigation.ScreenChanged += OnScreen;
            OnScreen(navigation.CurrentScreenId);
        }
        private void OnScreen(string id)
        {
            if (id != AnimalStagePhase2Host.ScreenId || host == null) return;
            if (!host.gameObject.activeSelf) host.gameObject.SetActive(true);
        }
        private void OnDestroy() { if (navigation != null) navigation.ScreenChanged -= OnScreen; }
    }
}
