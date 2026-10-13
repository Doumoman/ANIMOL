using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Gameplay
{
    public sealed class DevGameplayUiPresenter : MonoBehaviour
    {
        private GameObject pauseModal;
        private bool initialized;

        private void Awake() => InitializeUi();
        private void Start() => InitializeUi();

        private void InitializeUi()
        {
            if (initialized) return;
            pauseModal = transform.Find("SafeArea/ModalHost/PauseModal")?.gameObject;
            if (pauseModal == null) return;
            pauseModal.SetActive(false);
            Bind("SafeArea/ScreenHost/SC15_GameplayHud/PauseButton", TogglePause);
            Bind("SafeArea/ModalHost/PauseModal/ContinueButton", TogglePause);
            Bind("SafeArea/ModalHost/PauseModal/RestartButton", () => { Time.timeScale = 1f; SceneManager.LoadScene("Gameplay"); });
            Bind("SafeArea/ModalHost/PauseModal/LobbyButton", () => { Time.timeScale = 1f; SceneManager.LoadScene("Lobby"); });
            initialized = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        }

        private void OnDestroy() => Time.timeScale = 1f;

        private void TogglePause()
        {
            if (pauseModal == null) return;
            var show = !pauseModal.activeSelf;
            pauseModal.SetActive(show);
            Time.timeScale = show ? 0f : 1f;
        }

        private void Bind(string path, UnityEngine.Events.UnityAction action)
        {
            var button = transform.Find(path)?.GetComponent<Button>();
            if (button != null) button.onClick.AddListener(action);
        }
    }
}
