using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Animol.Starway;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace WilliamParadox.Story.Tests.PlayMode
{
    public sealed class RanchStoryUiPlayTests
    {
        private const string Output = "Docs/Validation/StarwayRanchUi";
        private const string SavePackage = "william-paradox.ranch.dialogue-preview";
        private string priorSave; private bool hadSave;
        private StoryRunner runner;
        private GameObject standalone;
        private StoryDialogueView Ui => runner.view.dialogueBridge.dialogue;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void PreserveSave()
        {
            hadSave = PlayerPrefs.HasKey(StorySaveStore.Key(SavePackage)); priorSave = PlayerPrefs.GetString(StorySaveStore.Key(SavePackage));
        }
        [TearDown] public void RestoreSave()
        {
            foreach (var r in UnityEngine.Object.FindObjectsByType<StoryRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None)) r.persistProgress = false;
            if (standalone != null) UnityEngine.Object.Destroy(standalone);
            if (hadSave) PlayerPrefs.SetString(StorySaveStore.Key(SavePackage), priorSave); else PlayerPrefs.DeleteKey(StorySaveStore.Key(SavePackage));
            PlayerPrefs.Save();
        }

        [UnityTest] public IEnumerator StandaloneTypingSkipAndSeparateActionsRespectTheContract()
        {
#if UNITY_EDITOR
            standalone = new GameObject("Standalone Dialogue Test", typeof(Canvas), typeof(CanvasScaler));
            standalone.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Story/StoryDialogueUI.prefab"), standalone.transform);
            var ui = go.GetComponent<StoryDialogueView>();
            var data = new StoryDialogueData { scenes = new[] {
                new StoryDialogueScene { id="R16", lines=new[] { Line("one"), Line("lamp","block_lamp"), Line("return") } },
                new StoryDialogueScene { id="R17", lines=new[] { Line("record"), Line("finish","chapter_complete") } }
            } };
            Assert.That(ui.SetData(data), Is.True); yield return null;
            ui.SetReducedMotion(false); Assert.That(ui.OpenScene("R16"), Is.True);
            Assert.That(ui.IsTyping, Is.True); ui.Advance();
            Assert.That(ui.IsTyping, Is.False); Assert.That(ui.CurrentLine.id, Is.EqualTo("one"));
            int requested = 0, finished = 0;
            ui.ActionRequested.AddListener(_ => requested++); ui.SceneFinished.AddListener(_ => finished++);
            ui.SkipButton.onClick.Invoke(); Assert.That(ui.CurrentLine.id, Is.EqualTo("lamp"));
            for (int i = 0; i < 3; i++) { ui.SkipCurrentScene(); ui.Advance(); }
            Assert.That(ui.PendingAction, Is.EqualTo("block_lamp")); Assert.That(finished, Is.Zero);
            ui.ActionButton.onClick.Invoke(); Assert.That(requested, Is.EqualTo(1));
            Assert.That(ui.CurrentLine.id, Is.EqualTo("lamp")); Assert.That(ui.ConfirmAction("chapter_complete"), Is.False);
            Assert.That(ui.ConfirmAction("block_lamp"), Is.True); Assert.That(ui.CurrentLine.id, Is.EqualTo("return"));
            ui.SetReducedMotion(true); ui.SkipCurrentScene(); Assert.That(finished, Is.EqualTo(1));
            ui.OpenNextScene(); ui.SkipCurrentScene(); Assert.That(ui.CurrentLine.id, Is.EqualTo("finish"));
            ui.Advance(); ui.SkipCurrentScene(); Assert.That(finished, Is.EqualTo(1));
            ui.ActionButton.onClick.Invoke(); Assert.That(finished, Is.EqualTo(2));
            ui.Close(); Assert.That(ui.IsOpen, Is.False); Assert.That(go.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
#else
            Assert.Ignore("Editor prefab required."); yield break;
#endif
        }

        [UnityTest] public IEnumerator All178UtterancesAndTheRecordUseSeparateCompletionInput()
        {
#if UNITY_EDITOR
            yield return Open(true);
            var seen = new HashSet<string>(); bool lamp = false, book = false;
            var endingEvents = new List<string>();
            Ui.ActionConfirmed.AddListener(action => { if (action == "play_chronicle") endingEvents.Add("confirmed"); });
            Ui.SceneFinished.AddListener(scene => { if (scene == "R17") endingEvents.Add("finished"); });
            for (int steps = 0; steps < 600 && runner.CurrentNode.kind != "end"; steps++)
            {
                Arm(); var node = runner.CurrentNode; seen.Add(Ui.CurrentLine.id);
                Assert.That(runner.ValidationErrors, Is.Empty, node.id);
                var panel = (RectTransform)runner.view.dialogueGroup.transform;
                Assert.That(panel.anchorMin.y, Is.Zero, node.id);
                Assert.That(panel.anchoredPosition.y, Is.EqualTo(20), node.id);
                Assert.That(Ui.ProgressLabel == null || !Ui.ProgressLabel.gameObject.activeSelf, Is.True);
                if (node.kind != "credits")
                {
                    Ui.BodyLabel.ForceMeshUpdate();
                    Assert.That(Ui.BodyLabel.preferredHeight, Is.LessThanOrEqualTo(250), Ui.CurrentLine.id);
                }
                if (node.kind == "gate")
                {
                    if (node.gateId == "BLOCK_LAMP")
                    {
                        Ui.SkipCurrentScene(); Assert.That(runner.HasFlag("PLAYER_BLOCKS_LAMP"), Is.False);
                        Arm(); lamp = true;
                    }
                    Ui.ActionButton.onClick.Invoke();
                }
                else if (node.kind == "credits")
                {
                    var record = Ui.Chronicle; book = true;
                    typeof(StoryChronicleView).GetField("hasFocus", Private).SetValue(record, true);
                    Assert.That(record.CurrentPhase, Is.EqualTo(StoryChronicleView.Phase.StaticPages));
                    Assert.That(runner.HasFlag("ISOL_PRESENT"), Is.True);
                    Assert.That(runner.ConfirmDialogueAction("play_chronicle"), Is.False);
                    for (int page = 0; page < 5; page++) record.AdvanceStaticPage();
                    runner.persistProgress = true; runner.SaveCheckpoint(); runner.StartStory(true); runner.persistProgress = false; Arm();
                    record = Ui.Chronicle;
                    typeof(StoryChronicleView).GetField("hasFocus", Private).SetValue(record, true);
                    Assert.That(record.StaticPage, Is.Zero); Assert.That(runner.HasFlag("RANCH_RULE_EPILOGUE_SEEN"), Is.False);
                    for (int page = 0; page < 12; page++) record.AdvanceStaticPage();
                    Assert.That(record.WasReadThrough, Is.True); Assert.That(record.WasSkipped, Is.False);
                    Assert.That(runner.HasFlag("RANCH_RULE_EPILOGUE_SEEN"), Is.True);
                    Assert.That(runner.CurrentNode.kind, Is.EqualTo("credits"));
                    Assert.That(runner.ConfirmDialogueAction("play_chronicle"), Is.False);
                    Arm(); record.RequestContinue();
                    Assert.That(runner.CurrentNode.kind, Is.EqualTo("end"));
                }
                else Ui.Advance();
                yield return null;
            }
            Assert.That(lamp && book, Is.True); Assert.That(seen.Count, Is.EqualTo(179));
            Assert.That(runner.CurrentNode.kind, Is.EqualTo("end"));
            CollectionAssert.AreEqual(new[] { "confirmed", "finished" }, endingEvents);
            Assert.That(runner.HasFlag("ISOL_PRESENT") && runner.HasFlag("RETURN_PATH_USED"), Is.True);
            Assert.That(runner.HasFlag("CHAPTER_R_COMPLETE"), Is.False, "Preview must not grant live campaign progress.");
#else
            Assert.Ignore("Editor scene required."); yield break;
#endif
        }

        [UnityTest] public IEnumerator SkippedRecordDoesNotGrantReadThroughAndNeedsNewContinue()
        {
#if UNITY_EDITOR
            yield return Open(true);
            for (int steps = 0; steps < 600 && runner.CurrentNode.kind != "credits"; steps++)
            {
                Arm();
                if (runner.CurrentNode.kind == "gate") Ui.ActionButton.onClick.Invoke(); else Ui.Advance();
                yield return null;
            }
            Arm(); var record = Ui.Chronicle;
            typeof(StoryChronicleView).GetField("hasFocus", Private).SetValue(record, true);
            Ui.SkipCurrentScene();
            Assert.That(record.WasSkipped, Is.True); Assert.That(record.WasReadThrough, Is.False);
            Assert.That(runner.HasFlag("RANCH_RULE_EPILOGUE_SEEN"), Is.False);
            runner.persistProgress = true; runner.SaveCheckpoint(); runner.StartStory(true); runner.persistProgress = false; Arm();
            Assert.That(Ui.Chronicle.StaticPage, Is.Zero); Assert.That(runner.HasFlag("RANCH_RULE_EPILOGUE_SEEN"), Is.False);
            record = Ui.Chronicle; typeof(StoryChronicleView).GetField("hasFocus", Private).SetValue(record, true);
            Ui.SkipCurrentScene(); Assert.That(runner.CurrentNode.kind, Is.EqualTo("credits"));
            Arm(); record.RequestContinue(); Assert.That(runner.CurrentNode.kind, Is.EqualTo("end"));
            Assert.That(runner.HasFlag("RANCH_RULE_EPILOGUE_SEEN"), Is.False);
#else
            Assert.Ignore("Editor scene required."); yield break;
#endif
        }

        [UnityTest] public IEnumerator PauseSkipAndReentryClearPendingBoarAndKeepTheDistanceGate()
        {
#if UNITY_EDITOR
            yield return Open(false); Arm(); Assert.That(runner.JumpTo("R12_005"), Is.True); yield return null;
            var stage = runner.view.ranchStage; runner.TogglePause();
            var scale = stage.boarRush.transform.localScale; int visible = Ui.BodyLabel.maxVisibleCharacters; float timeScale = Time.timeScale;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(stage.boarRush.transform.localScale, Is.EqualTo(scale)); Assert.That(Ui.BodyLabel.maxVisibleCharacters, Is.EqualTo(visible));
            Ui.SkipCurrentScene(); Assert.That(runner.CurrentNodeId, Is.EqualTo("R12_005")); Assert.That(Time.timeScale, Is.EqualTo(timeScale));
            runner.TogglePause(); Arm(); var cues = new List<string>(); Ui.CueRequested.AddListener(c => cues.Add(c));
            Ui.SkipButton.onClick.Invoke(); Assert.That(Ui.CurrentLine.id, Is.EqualTo("R13_001"));
            Assert.That(stage.boarRush.enabled || stage.impact.enabled || stage.isolReaction.enabled || stage.toriReaction.enabled, Is.False);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Ui.CurrentLine.id, Is.EqualTo("R13_001")); Assert.That(cues.Any(c => c.StartsWith("impact_")), Is.False);
            Arm(); Assert.That(runner.JumpTo("R08_B1_L001"), Is.True); Ui.SkipCurrentScene();
            Assert.That(stage.runnerImage.enabled, Is.False); Assert.That(stage.composition.gameObject.activeSelf, Is.False);
            Arm(); Assert.That(runner.JumpTo("R15_B1_L001"), Is.True); runner.enabled = false;
            Assert.That(stage.composition.gameObject.activeSelf, Is.False); runner.enabled = true;
            Assert.That(stage.composition.gameObject.activeSelf, Is.True); Assert.That(stage.DistanceProgress, Is.EqualTo(1));
            runner.SetReducedMotion(true); Assert.That(stage.farStars.material.GetFloat("_ReducedMotion"), Is.EqualTo(1));
            Arm(); Ui.SkipCurrentScene(); Arm(); Ui.SkipCurrentScene();
            Assert.That(runner.CurrentNode.gateId, Is.EqualTo("BLOCK_LAMP")); Assert.That(runner.HasFlag("PLAYER_BLOCKS_LAMP"), Is.False);
            Assert.That(stage.composition.gameObject.activeSelf, Is.True);
            yield return Capture("R15_reduced_gate");
#else
            Assert.Ignore("Editor scene required."); yield break;
#endif
        }

        private static StoryDialogueLine Line(string id, string action = "") => new StoryDialogueLine { id = id, speaker = "이솔", text = "처음 문장을 읽습니다.", shot = "R16", action = action };
        private IEnumerator Open(bool reduced)
        {
#if UNITY_EDITOR
            var setup = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Presentation.Editor.PortraitGameView")).First(t => t != null);
            setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] {1080,1920,"Ranch UI QA"});
            EditorSceneManager.LoadSceneInPlayMode("Assets/WilliamParadoxStoryV14/Scenes/RanchStoryPreview.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            runner = UnityEngine.Object.FindFirstObjectByType<StoryRunner>(); runner.persistProgress = false; runner.restoreOnStart = false;
            runner.SetReducedMotion(reduced); runner.StartStory(false); Arm();
#else
            yield break;
#endif
        }
        private void Arm()
        {
            // Exercise host/UI dispatch independently of OS focus and debounce timing; separate tests cover pause/rapid input.
            typeof(StoryRunner).GetField("hasFocus", Private).SetValue(runner, true);
            typeof(StoryRunner).GetField("inputReadyAt", Private).SetValue(runner, 0f);
            typeof(StoryRunner).GetField("lastInputFrame", Private).SetValue(runner, -1);
            typeof(StoryRunner).GetField("lastInputTime", Private).SetValue(runner, -100f);
            runner.view.SetPaused(runner.IsPaused);
        }
        private IEnumerator Capture(string name)
        {
            Directory.CreateDirectory(Output); Canvas.ForceUpdateCanvases(); yield return null; yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Output + "/" + name + ".png", texture.EncodeToPNG()); UnityEngine.Object.Destroy(texture);
        }
    }
}
