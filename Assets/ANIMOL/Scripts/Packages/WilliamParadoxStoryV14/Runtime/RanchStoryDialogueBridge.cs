using System.Linq;
using Animol.Starway;
using UnityEngine;
using UnityEngine.Events;

namespace WilliamParadox.Story
{
    /// <summary>Presentation notifications feed the existing validated StorySession.</summary>
    public sealed class RanchStoryDialogueBridge : MonoBehaviour
    {
        public StoryRunner runner;
        public StoryDialogueView dialogue;
        public UnityEvent ChronicleStarted = new UnityEvent();
        public UnityEvent ChronicleRewindStarted = new UnityEvent();
        private StoryDialogueData data;
        private void Awake()
        {
            data = JsonUtility.FromJson<StoryDialogueData>(dialogue.DialogueJson.text);
            dialogue.LoadJson(dialogue.DialogueJson);
            dialogue.AdvanceRequested.AddListener(Advance);
            dialogue.SceneSkipRequested.AddListener(Skip);
            dialogue.ActionRequested.AddListener(Action);
            dialogue.Closed.AddListener(Closed);
            dialogue.Chronicle.ReadThrough.AddListener(ReadRecord);
            dialogue.Chronicle.Skipped.AddListener(SkippedRecord);
            dialogue.Chronicle.RewindStarted.AddListener(RewindRecord);
        }
        private void OnDestroy()
        {
            if (dialogue == null) return;
            dialogue.AdvanceRequested.RemoveListener(Advance);
            dialogue.SceneSkipRequested.RemoveListener(Skip);
            dialogue.ActionRequested.RemoveListener(Action);
            dialogue.Closed.RemoveListener(Closed);
            dialogue.Chronicle.ReadThrough.RemoveListener(ReadRecord);
            dialogue.Chronicle.Skipped.RemoveListener(SkippedRecord);
            dialogue.Chronicle.RewindStarted.RemoveListener(RewindRecord);
        }
        private void Advance() { runner.Advance(); }
        private void Skip() { runner.SkipCurrentScene(); }
        private void Action(string action) { runner.ConfirmDialogueAction(action); }
        private void ReadRecord() { runner.RecordChronicleResult(true, false); }
        private void SkippedRecord() { runner.RecordChronicleResult(false, true); }
        private void RewindRecord() { ChronicleRewindStarted.Invoke(); }
        private void Closed(string unused) { runner.CancelTransientPresentation(); }
        public void Present(bool restoring)
        {
            var node = runner.CurrentNode;
            if (node == null || data == null) return;
            if (node.kind == "end") { dialogue.Close(); return; }
            string id = node.dialogueId;
            if (string.IsNullOrEmpty(id)) return;
            var scene = data.scenes.First(s => s.lines.Any(l => l.id == id));
            string action = node.kind == "credits" ? "play_chronicle" :
                node.kind == "gate" ? node.gateId == "BLOCK_LAMP" ? "block_lamp" : "puzzle_handoff" : "";
            dialogue.SetReducedMotion(runner.reducedMotion);
            dialogue.ShowHostedLine(scene.id, id, action, restoring || node.kind == "cue" || node.kind == "gate");
            StoryView.SetGroup(runner.view.creditsGroup, false);
            if (node.kind == "credits") ChronicleStarted.Invoke();
            RefreshControls();
        }
        public void RefreshControls()
        {
            if (dialogue != null && runner != null) dialogue.SetHostState(runner.IsPaused, true);
        }
    }
}
