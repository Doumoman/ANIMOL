using System.Collections;
using ANIMOL.UI;
using UnityEngine;

namespace ANIMOL.AnimalUpgradePhase1
{
    /// <summary>Lives on the existing navigation owner, including while the upgrade child is closed.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class AnimalUpgradePhase1Entry : MonoBehaviour
    {
        private UiNavigationService navigation;
        private AnimalUpgradePhase1Host host;

        private IEnumerator Start()
        {
            // The existing UiScenePolish and ProductionController run their initial styling after one frame.
            // Install our already-authored presentation after them so transparent hit targets stay transparent.
            yield return null;
            yield return null;
            navigation = GetComponent<UiNavigationService>();
            var screen = navigation.transform.Find("SafeArea/ScreenHost/" + AnimalUpgradePhase1Host.ScreenId);
            var prefab = Resources.Load<GameObject>(AnimalUpgradePhase1Host.ResourcePath);
            if (screen == null || prefab == null) yield break;
            host = screen.GetComponentInChildren<AnimalUpgradePhase1Host>(true);
            if (host == null)
            {
                foreach (Transform child in screen) child.gameObject.SetActive(false);
                var instance = Instantiate(prefab, screen, false);
                instance.name = "CharacterUpgrade";
                host = instance.GetComponent<AnimalUpgradePhase1Host>();
            }
            navigation.ScreenChanged += OnScreen;
            OnScreen(navigation.CurrentScreenId);
        }

        private void OnScreen(string screenId)
        {
            if (screenId == AnimalUpgradePhase1Host.ScreenId && host != null)
                host.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (navigation != null) navigation.ScreenChanged -= OnScreen;
        }
    }
}
