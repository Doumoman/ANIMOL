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
            SetText("ResultRewardBreakdownDetail", "기본 B: 서버 미확정 | 시간 T: 서버 미확정 | 부가 O: 서버 미확정\n광고 추가 B: 지급 없음 | 총액 2B+T+O: 서버 승인 대기");
            SetText("ResultAdQuotaState", "오늘 남은 횟수: 계정 서버 조회 불가 · 초기화 시각 미확정");
            var rewardButton = FindButton("SafeArea/ScreenHost/SC16_Result/ResultDoubleBaseAdButton");
            if (rewardButton != null)
            {
                rewardButton.interactable = false;
                var label = rewardButton.GetComponentInChildren<Text>(true);
                if (label != null) label.text = "기본 B 1회 추가 · 지급 불가";
            }
            Bind("SafeArea/ScreenHost/SC16_Result/RetryButton", () => SceneManager.LoadScene("Gameplay"));
            Bind("SafeArea/ScreenHost/SC16_Result/LobbyButton", () => SceneManager.LoadScene("Lobby"));
        }

        private void Bind(string path, UnityEngine.Events.UnityAction action)
        {
            var button = transform.Find(path)?.GetComponent<Button>();
            if (button != null) button.onClick.AddListener(action);
        }

        private Button FindButton(string path) => transform.Find(path)?.GetComponent<Button>();

        private void SetText(string name, string value)
        {
            foreach (var label in GetComponentsInChildren<Text>(true))
                if (label.name == name) { label.text = value; return; }
        }
    }
}
