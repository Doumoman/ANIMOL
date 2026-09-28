using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Gameplay
{
    public sealed class DevResultsPresenter : MonoBehaviour
    {
        private void Start()
        {
            var summary = transform.Find("SafeArea/ScreenHost/SC16_Result/ResultSummary")?.GetComponent<Text>();
            if (summary != null)
                summary.text = DevRunResultState.HasResult
                    ? $"DEV-TEST-01 · {DevRunResultState.Outcome} · 방울 {DevRunResultState.BubbleCount}/3 · {DevRunResultState.TimeSeconds:0.0}s\n개발 결과이며 캠페인 진행·최고 시간·코인에 반영하지 않습니다."
                    : "개발 런 결과 없음 · 로비로 돌아가 다시 시작하세요.";
            Bind("SafeArea/ScreenHost/SC16_Result/RetryButton", () => SceneManager.LoadScene("Gameplay"));
            Bind("SafeArea/ScreenHost/SC16_Result/LobbyButton", () => SceneManager.LoadScene("Lobby"));
        }

        private void Bind(string path, UnityEngine.Events.UnityAction action)
        {
            var button = transform.Find(path)?.GetComponent<Button>();
            if (button != null) button.onClick.AddListener(action);
        }
    }
}
