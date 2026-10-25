using ANIMOL.Menu;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.SanctuaryStory
{
    // Active-project bridge. The current lobby has no campaign save or puzzle host.
    // Checkpoints therefore live only for this running session until a save owner exists.
    public sealed class SanctuaryMainStoryBridge : MonoBehaviour
    {
        [SerializeField] private MainLobbyController lobby;
        [SerializeField] private GameObject storyScreen;
        [SerializeField] private GameObject unsafeAreaMatte;
        [SerializeField] private SanctuaryStoryRunner runner;
        [SerializeField] private SanctuarySpriteStage stage;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TMPro.TMP_Text openLabel;
        [SerializeField] private Text openLegacyLabel;
        [SerializeField] private TMPro.TMP_Text pauseLabel;

        private string sessionCheckpoint;
        private bool sessionCompleted;
        private bool hostPaused;
        private bool lastReducedMotion;
        private Action openAction;

        private void Awake()
        {
            openAction = Open;
            if (lobby) lobby.CampaignContinueOverride = openAction;
            if (openButton) openButton.onClick.AddListener(Open);
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (pauseButton) pauseButton.onClick.AddListener(TogglePause);
            if (runner != null)
            {
                runner.CheckpointChanged += OnCheckpoint;
                runner.PlaybackCompleted += OnPlaybackCompleted;
            }
            if (storyScreen) storyScreen.SetActive(false);
            if (unsafeAreaMatte) unsafeAreaMatte.SetActive(false);
            RefreshLabels();
        }

        private void OnDestroy()
        {
            if (lobby != null && lobby.CampaignContinueOverride == openAction) lobby.CampaignContinueOverride = null;
            if (runner != null)
            {
                runner.CheckpointChanged -= OnCheckpoint;
                runner.PlaybackCompleted -= OnPlaybackCompleted;
            }
            if (openButton) openButton.onClick.RemoveListener(Open);
            if (closeButton) closeButton.onClick.RemoveListener(Close);
            if (pauseButton) pauseButton.onClick.RemoveListener(TogglePause);
        }

        private void Update()
        {
            if (!storyScreen || !storyScreen.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            if (lobby && stage && lastReducedMotion != lobby.ReducedMotion)
            {
                lastReducedMotion = lobby.ReducedMotion;
                stage.SetReducedMotion(lastReducedMotion);
            }
        }

        public void Open()
        {
            if (!storyScreen || !runner || storyScreen.activeSelf) return;
            storyScreen.SetActive(true);
            if (unsafeAreaMatte) unsafeAreaMatte.SetActive(true);
            lastReducedMotion = lobby && lobby.ReducedMotion;
            if (stage) stage.SetReducedMotion(lastReducedMotion);
            runner.SetHostPaused(false);
            hostPaused = false;
            RefreshLabels();
            if (sessionCompleted) runner.BeginReplay();
            else if (!string.IsNullOrEmpty(sessionCheckpoint)) runner.ResumeFromJson(sessionCheckpoint);
            else runner.BeginChapter();
        }

        public void Close()
        {
            if (!storyScreen || !storyScreen.activeSelf) return;
            if (runner) runner.StopPlayback();
            storyScreen.SetActive(false);
            if (unsafeAreaMatte) unsafeAreaMatte.SetActive(false);
            hostPaused = false;
            if (runner) runner.SetHostPaused(false);
            RefreshLabels();
        }

        public void SetHostPaused(bool paused)
        {
            hostPaused = paused;
            if (runner) runner.SetHostPaused(paused);
            RefreshLabels();
        }

        private void TogglePause() { SetHostPaused(!hostPaused); }

        private void OnCheckpoint(SanctuaryCheckpoint checkpoint)
        {
            if (checkpoint == null) return;
            sessionCheckpoint = JsonUtility.ToJson(checkpoint);
            if (checkpoint.completed) sessionCompleted = true;
            RefreshLabels();
        }

        private void OnPlaybackCompleted()
        {
            // Flow emits its completed checkpoint before this callback. No reward or save is committed here.
            Close();
        }

        private void RefreshLabels()
        {
            if (openLabel) openLabel.text = sessionCompleted ? "천조 날조국 · 재감상" : "낙일 성소 · 이야기";
            if (openLegacyLabel) openLegacyLabel.text = sessionCompleted ? "천조 날조국 · 재감상" : "낙일 성소 · 이야기";
            if (pauseLabel) pauseLabel.text = hostPaused ? "계속" : "일시정지";
        }
    }
}
