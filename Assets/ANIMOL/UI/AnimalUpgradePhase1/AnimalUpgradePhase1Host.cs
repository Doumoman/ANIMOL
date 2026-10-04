using ANIMOL.AnimalUiV2;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.AnimalUpgradePhase1
{
    /// <summary>Read-only task 1 entry. Both existing hub buttons retain their navigation route.</summary>
    [DefaultExecutionOrder(800)]
    public sealed class AnimalUpgradePhase1Host : MonoBehaviour
    {
        public const string ScreenId = "SC10B_CharacterUpgrade";
        public const string ResourcePath = "ANIMOLAnimalUpgradePhase1/CharacterUpgrade";
        public AnimalUiPresenter Presenter;
        public AnimalUpgradeIdMap IdMap;
        private UiNavigationService navigation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => SceneManager.sceneLoaded -= Install;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= Install;
            SceneManager.sceneLoaded += Install;
        }

        public static void Install(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var nav in root.GetComponentsInChildren<UiNavigationService>(true))
            {
                var screen = nav.transform.Find("SafeArea/ScreenHost/" + ScreenId);
                if (screen != null && nav.GetComponent<AnimalUpgradePhase1Entry>() == null)
                    nav.gameObject.AddComponent<AnimalUpgradePhase1Entry>();
            }
        }

        private void OnEnable()
        {
            navigation = GetComponentInParent<UiNavigationService>();
            Presenter.Cancelled += ReturnToHub;
            Presenter.OpenUpgrade(string.IsNullOrEmpty(Presenter.InspectedAnimalId) ? "Rabbit" : Presenter.InspectedAnimalId);
        }

        private void OnDisable() => Presenter.Cancelled -= ReturnToHub;

        private void ReturnToHub(AnimalUiMode mode, AnimalLoadout original)
        {
            if (navigation != null && !navigation.Back()) navigation.Navigate("SC10_UpgradeHub", false);
        }
    }
}
