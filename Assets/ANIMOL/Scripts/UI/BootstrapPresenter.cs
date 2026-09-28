using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.UI
{
    public sealed class BootstrapPresenter : MonoBehaviour
    {
        private Button continueButton;
        private void OnEnable()
        {
            continueButton = FindByName<Button>(transform, "ContinueButton");
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
        }
        private void OnDisable()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        }
        public void Continue() => SceneManager.LoadScene("Lobby");

        private static T FindByName<T>(Transform root, string name) where T : Component
        {
            foreach (var component in root.GetComponentsInChildren<T>(true)) if (component.name == name) return component;
            return null;
        }
    }
}
