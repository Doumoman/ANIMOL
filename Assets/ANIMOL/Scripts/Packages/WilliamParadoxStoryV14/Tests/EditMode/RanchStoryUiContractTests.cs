using System.IO;
using System.Linq;
using Animol.Starway;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace WilliamParadox.Story.Tests
{
    public sealed class RanchStoryUiContractTests
    {
        private const string UiPath = "Assets/ANIMOL/Prefabs/UI/Story/StoryDialogueUI.prefab";
        private const string Entry = "Assets/WilliamParadoxStoryV14/Prefabs/StoryPreviewCanvas.prefab";
        [Test] public void EveryTemporaryUtteranceIsMappedWithoutChangingText()
        {
            var source = JsonUtility.FromJson<StoryDialogueData>(File.ReadAllText("Assets/ANIMOL/Data/StarwayRanch/Story/ranch_dialogue.json"));
            var host = JsonUtility.FromJson<StoryPackage>(Resources.Load<TextAsset>("WilliamParadox/V14/Story/ranch_story").text);
            Assert.That(source.scenes.Sum(s => s.lines.Length), Is.EqualTo(184));
            var book = host.nodes.Single(n => n.kind == "credits");
            foreach (var line in source.scenes.SelectMany(s => s.lines))
            {
                if (line.shot == "R17B")
                    Assert.That(book.credits[int.Parse(line.id.Substring(4)) - 8], Is.EqualTo(line.text));
                else
                {
                    var node = host.nodes.First(n => n.dialogueId == line.id && (n.kind == "line" || n.kind == "gate"));
                    Assert.That(node.text, Is.EqualTo(line.text), line.id); Assert.That(node.speaker, Is.EqualTo(line.speaker), line.id);
                }
            }
        }
        [Test] public void FontAndSkipControlMatchTheSuppliedUiContract()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(UiPath); var ui = root.GetComponent<StoryDialogueView>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ANIMOL/Fonts/Notebook/Galmuri14.asset");
            Assert.That(ui.DialogueJson, Is.Not.Null);
            foreach (var panel in root.GetComponentsInChildren<StoryNeonPanel>(true)) Assert.That(panel.GetComponent<CanvasRenderer>(), Is.Not.Null);
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.That(label.font, Is.SameAs(font), label.name); Assert.That(label.fontStyle, Is.EqualTo(FontStyles.Normal));
                Assert.That(label.fontWeight, Is.EqualTo(FontWeight.Regular)); Assert.That(label.enableAutoSizing, Is.False);
                Assert.That(label.raycastTarget, Is.False);
            }
            Assert.That(ui.BodyLabel.fontSize, Is.EqualTo(45)); Assert.That(ui.SpeakerLabel.fontSize, Is.EqualTo(45));
            var skip = (RectTransform)ui.SkipButton.transform.parent;
            Assert.That(skip.anchorMin, Is.EqualTo(Vector2.one)); Assert.That(skip.sizeDelta, Is.EqualTo(new Vector2(104, 88)));
            Assert.That(skip.anchoredPosition, Is.EqualTo(new Vector2(-44, -42)));
            Assert.That(skip.GetComponentInChildren<TMP_Text>().text, Is.EqualTo(">>"));
            Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "R01"), Is.False);
        }
        [Test] public void ExistingEntryReusesTheNewViewAndTheExistingGalmuriAsset()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Entry); var runner = root.GetComponent<StoryRunner>();
            Assert.That(runner.view.dialogueBridge, Is.Not.Null);
            Assert.That(runner.view.dialogueGroup, Is.Not.Null);
            Assert.That(runner.view.dialogueGroup.transform, Is.SameAs(runner.view.dialogueBridge.dialogue.BodyLabel.transform.parent));
            Assert.That(root.GetComponentsInChildren<StoryDialogueView>(true).Length, Is.EqualTo(1));
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                Assert.That(label.font, Is.SameAs(runner.view.dialogueBridge.dialogue.BodyLabel.font), label.name);
            Assert.That(runner.view.spriteBindings.Length, Is.GreaterThanOrEqualTo(32));
            Assert.That(runner.view.ranchStage.boarRush.sprite.name, Is.EqualTo("R12_BoarRush"));
            Assert.That(runner.finishLegacyCueBeforeAdvancing, Is.False);
        }
        [Test] public void DinnerRepairAndToriReliefRemainDistinct()
        {
            var data = JsonUtility.FromJson<StoryDialogueData>(File.ReadAllText("Assets/ANIMOL/Data/StarwayRanch/Story/ranch_dialogue.json"));
            var lines = data.scenes.SelectMany(s => s.lines).ToArray();
            Assert.That(lines.Single(l => l.id == "R13_001").text, Does.Contain("그날 저녁"));
            Assert.That(lines.Single(l => l.id == "R13_007").shot, Is.EqualTo("R13B"));
            foreach (var line in data.scenes.Single(s => s.id == "R17").lines.Where(l => l.speaker == "토리"))
                Assert.That(line.emotion, Is.EqualTo("relieved"));
        }
    }
}
