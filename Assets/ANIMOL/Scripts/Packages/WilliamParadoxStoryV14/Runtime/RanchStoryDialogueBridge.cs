using System;
using System.Linq;
using Animol.Starway;
using UnityEngine;

namespace WilliamParadox.Story
{
    /// <summary>The package UI displays authored lines; the existing runner alone advances validated progress.</summary>
    public sealed class RanchStoryDialogueBridge : MonoBehaviour
    {
        public StoryRunner runner;
        public StoryDialogueView dialogue;
        private StoryDialogueData data;
        private void Awake()
        {
            data = JsonUtility.FromJson<StoryDialogueData>(dialogue.DialogueJson.text);
            dialogue.LoadJson(dialogue.DialogueJson);
            dialogue.AdvanceRequested.AddListener(Advance);
            dialogue.SceneSkipRequested.AddListener(Skip);
            dialogue.ActionRequested.AddListener(Action);
            dialogue.Closed.AddListener(Closed);
        }
        private void OnDestroy()
        {
            if (dialogue == null) return;
            dialogue.AdvanceRequested.RemoveListener(Advance);
            dialogue.SceneSkipRequested.RemoveListener(Skip);
            dialogue.ActionRequested.RemoveListener(Action);
            dialogue.Closed.RemoveListener(Closed);
        }
        private void Advance() { runner.Advance(); }
        private void Skip() { runner.SkipCurrentScene(); }
        private void Action(string action) { if (!runner.ConfirmDialogueAction(action)) Present(true); }
        private void Closed(string unused) { runner.CancelTransientPresentation(); }
        public void Present(bool restoring)
        {
            var node = runner.CurrentNode;
            if (node == null || data == null) return;
            if (node.kind == "end") { dialogue.Close(); return; }
            string id = node.dialogueId;
            if (node.kind == "credits") id = "R17_" + (8 + runner.CreditParagraph).ToString("000");
            if (string.IsNullOrEmpty(id)) return;
            var scene = data.scenes.First(s => s.lines.Any(l => l.id == id));
            string action = node.kind == "credits" ? runner.CreditParagraph == 5 ? "read_chronicle" : "" :
                node.kind == "gate" ? node.gateId == "BLOCK_LAMP" ? "block_lamp" :
                node.gateId == "PREVIEW_FINAL_CONTINUE" ? "chapter_complete" : "puzzle_handoff" : "";
            dialogue.SetReducedMotion(runner.reducedMotion);
            dialogue.ShowHostedLine(scene.id, id, action, restoring || node.kind == "cue" || node.kind == "gate");
            dialogue.SetContextLabel(node.artId == "R13B" ? "다음 날" : "");
            // The new panel owns book text too. Keep the old clipped book UI only as a save/layout adapter.
            StoryView.SetGroup(runner.view.creditsGroup, false);
            StoryView.SetGroup(runner.view.dialogueGroup, true);
            RefreshControls();
        }
        public void RefreshControls()
        {
            if (dialogue != null && runner != null)
                dialogue.SetHostState(runner.IsPaused, runner.CurrentNode == null || runner.CurrentNode.kind != "credits" || runner.CreditsCanContinue);
        }
    }
}
