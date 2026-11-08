using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ANIMOL.UI.Crayon
{
    // UI art inspection only: no combat clock, collision, HP calculation, rewards or spawning.
    public sealed class BattleCrayonArtPreview : MonoBehaviour
    {
        const string PreviewScene = "Assets/ANIMOL/Scenes/Battle/ANIMOL_StarRain_Battle.unity";
        const string MainScene = "Assets/ANIMOL/Scenes/Main/NotebookUI.unity";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterSceneIsolation()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void IsolateFromMainGame()
        {
            var main = SceneManager.GetSceneByPath(MainScene);
            var preview = SceneManager.GetSceneByPath(PreviewScene);
            if (!main.IsValid() || !main.isLoaded || !preview.IsValid() || !preview.isLoaded) return;
            var roots = preview.GetRootGameObjects();
            bool artPreview = false;
            foreach (var root in roots)
                if (root.GetComponentInChildren<BattleCrayonArtPreview>(true)) { artPreview = true; break; }
            if (!artPreview) return;
            // This scene is an independent art mockup. Its overlay, camera and input
            // system must not remain behind the menu when a real puzzle is opened.
            if (SceneManager.GetActiveScene() == preview) SceneManager.SetActiveScene(main);
            foreach (var root in roots) root.SetActive(false);
        }
        static void OnSceneLoaded(Scene unused, LoadSceneMode mode) => IsolateFromMainGame();

        public enum Screen { Start, Hud, Pause, Results, Augment }
        public GameObject startPanel, pausePanel, resultPanel, augmentPanel, augmentSelection;
        public Button startButton, pauseButton, resumeButton, retryButton, resultRetryButton;
        public Button augmentButton, augmentSelectButton, augmentBackButton, resultsPreviewButton;
        public Toggle reducedMotionToggle;
        public Image isolHealthFill, curseHealthFill;
        public TMP_Text isolHealthText, curseHealthText, shieldCount, misblockCount;
        public CrayonUIMotion motion;
        public Screen CurrentScreen { get; private set; }
        public bool AugmentSelected { get; private set; }
        public bool BlocksInput => false;

        public void ShowStart() => Show(Screen.Start);
        public void ShowHud() => Show(Screen.Hud);
        public void ShowPause() => Show(Screen.Pause);
        public void ShowResults() => Show(Screen.Results);
        public void ShowAugment() => Show(Screen.Augment);
        public void Show(Screen screen)
        {
            CurrentScreen = screen;
            if (startPanel) startPanel.SetActive(screen == Screen.Start);
            if (pausePanel) pausePanel.SetActive(screen == Screen.Pause);
            if (resultPanel) resultPanel.SetActive(screen == Screen.Results);
            if (augmentPanel) augmentPanel.SetActive(screen == Screen.Augment);
            bool hud = screen == Screen.Hud;
            SetAvailable(pauseButton, hud);
            SetAvailable(augmentButton, hud);
            if (resultsPreviewButton) resultsPreviewButton.gameObject.SetActive(hud);
        }
        static void SetAvailable(Button button, bool available)
        {
            if (!button) return;
            button.interactable = available;
            var group = button.GetComponent<CanvasGroup>();
            if (group) group.alpha = available ? 1f : .35f;
        }
        public void ToggleAugmentSelection()
        {
            AugmentSelected = !AugmentSelected;
            if (augmentSelection) augmentSelection.SetActive(AugmentSelected);
        }
        public void SetReducedMotion(bool value)
        {
            CrayonUIMotion.SetGlobalReducedMotion(value);
            if (reducedMotionToggle) reducedMotionToggle.SetIsOnWithoutNotify(value);
        }
        void OnEnable()
        {
            if (reducedMotionToggle)
                reducedMotionToggle.SetIsOnWithoutNotify(CrayonUIMotion.GlobalReducedMotion);
            ShowStart();
        }
    }
}
