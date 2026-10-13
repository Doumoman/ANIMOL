using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI
{
    public sealed class ToastAndErrorPresenter : MonoBehaviour
    {
        [SerializeField] private Text messageLabel;
        [SerializeField] private float durationSeconds = 2f;
        private Coroutine hideRoutine;

        public void Show(string message)
        {
            if (messageLabel == null) messageLabel = GetComponentInChildren<Text>(true);
            if (messageLabel != null) messageLabel.text = message;
            gameObject.SetActive(true);
            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideLater());
        }

        private IEnumerator HideLater()
        {
            yield return new WaitForSecondsRealtime(durationSeconds);
            gameObject.SetActive(false);
            hideRoutine = null;
        }
    }
}
