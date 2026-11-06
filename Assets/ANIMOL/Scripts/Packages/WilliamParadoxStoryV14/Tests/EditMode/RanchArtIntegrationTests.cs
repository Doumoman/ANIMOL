using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace WilliamParadox.Story.Tests
{
    public sealed class RanchArtIntegrationTests
    {
        private StoryGraph Graph()
        {
            var text = Resources.Load<TextAsset>("WilliamParadox/V14/Story/ranch_story");
            Assert.That(text, Is.Not.Null);
            Assert.That(StoryGraph.TryCreate(JsonUtility.FromJson<StoryPackage>(text.text), out var graph, out var errors), Is.True, string.Join("\n", errors));
            return graph;
        }
        [Test] public void OpeningTwentyOneUtterancesMatchTemporaryDialogue()
        {
            var text = System.IO.File.ReadAllText("Assets/ANIMOL/Data/StarwayRanch/Story/ranch_dialogue.json");
            var data = JsonUtility.FromJson<Animol.Starway.StoryDialogueData>(text);
            var opening = Graph().Package.nodes.Where(n => n.sceneId == "R01" && n.kind == "line").ToArray();
            Assert.That(opening.Length, Is.EqualTo(21));
            CollectionAssert.AreEqual(data.scenes[0].lines.Select(l => l.speaker + "\0" + l.text), opening.Select(n => n.speaker + "\0" + n.text));
        }
        [Test] public void ReprimandPrecedesRepairAndLatchPrecedesEveryJoke()
        {
            var graph = Graph(); var session = new StorySession(graph); bool reprimand = false, latch = false;
            while (session.Current.kind != "end")
            {
                var n = session.Current;
                if (new[] { "R12", "R12B", "R13", "R13B" }.Contains(n.sceneId) && n.kind == "line")
                    Assert.That(new[] { "이솔", "토리", "사냥꾼" }.Contains(n.speaker) || n.dialogueId == "R13_001" && n.speaker == "기록", Is.True, n.id);
                if (n.dialogueId == "R13_006") reprimand = true;
                if (n.artId == "R13B") Assert.That(reprimand, Is.True);
                if (n.id == "R13B_LATCH_SEATED") latch = true;
                if (n.text != null && n.text.Contains("고함에도")) Assert.That(latch, Is.True);
                if (n.kind == "gate") session.ActivateGate(); else if (n.kind == "credits") session.ContinueCredits(); else session.Acknowledge();
            }
            Assert.That(reprimand && latch, Is.True);
        }
        [Test] public void EveryRetiredCheckpointRestoresWithoutInventingAFlag()
        {
            var graph = Graph();
            var fixture = JsonUtility.FromJson<CheckpointFixture>(System.IO.File.ReadAllText("Docs/Validation/StarwayRanchUi/checkpoint_compatibility.json"));
            foreach (var alias in fixture.cases)
            {
                var session = new StorySession(graph);
                var saved = new StorySaveData { packageId = graph.Package.packageId, contentVersion = graph.Package.contentVersion, nodeId = alias.oldId, flags = alias.flags };
                Assert.That(session.Restore(saved), Is.True, alias.oldId);
                Assert.That(session.Current.id, Is.EqualTo(alias.nodeId)); CollectionAssert.AreEquivalent(session.Flags, alias.flags);
                saved.flags = alias.flags.Concat(new[] { "UNKNOWN_INJECTED" }).ToArray();
                Assert.That(new StorySession(graph).Restore(saved), Is.False);
            }
        }
        [Serializable] private sealed class CheckpointFixture { public CheckpointCase[] cases; }
        [Serializable] private sealed class CheckpointCase { public string oldId, nodeId; public string[] flags; }
        [Test] public void FullRoutePreservesExplicitLampAndSeparateChronicleCompletion()
        {
            var graph = Graph(); var session = new StorySession(graph);
            while (session.Current.gateId != "BLOCK_LAMP")
                Assert.That(session.Current.kind == "gate" ? session.ActivateGate() : session.Acknowledge(), Is.True, session.Current.id);
            Assert.That(session.HasFlag("CURRENT_CALLS_DONE") && session.HasFlag("ISOL_WANTS_RETURN"), Is.True);
            Assert.That(session.Acknowledge(), Is.False); Assert.That(session.HasFlag("PLAYER_BLOCKS_LAMP"), Is.False);
            Assert.That(session.ActivateGate(), Is.True);
            while (session.Current.kind != "credits") Assert.That(session.Acknowledge(), Is.True);
            Assert.That(session.HasFlag("ISOL_PRESENT") && session.HasFlag("RETURN_PATH_USED"), Is.True);
            Assert.That(session.HasFlag("RANCH_RULE_EPILOGUE_SEEN"), Is.False);
            Assert.That(session.Acknowledge(), Is.False);
            Assert.That(session.ContinueCredits(), Is.True); Assert.That(session.HasFlag("RANCH_RULE_EPILOGUE_SEEN"), Is.False);
            Assert.That(session.ActivateGate(), Is.True); Assert.That(session.HasFlag("RANCH_RULE_EPILOGUE_SEEN"), Is.True);
        }
        [Test] public void AllAuthoredEffectsUseCentralPolicyWithoutFlashHapticsOrInputLock()
        {
            Assert.That(RanchStoryCuePolicy.BlocksInput || RanchStoryCuePolicy.UsesFullScreenFlash || RanchStoryCuePolicy.UsesHaptics, Is.False);
            foreach (var c in Graph().Package.nodes.SelectMany(n => n.cues).Where(c => c.type == "ranchCut"))
                Assert.That(c.duration, Is.EqualTo(RanchStoryCuePolicy.Duration(c.eventId)).Within(0.001f));
            Assert.That(RanchStoryCuePolicy.Duration("impact_left"), Is.EqualTo(0.1f));
        }
        [Test] public void FinishedCGsAndTransparentSourcesResolveAsUncutSprites()
        {
            foreach (var asset in Graph().Package.assets.Where(a => a.resourcePath.StartsWith("ANIMOL/Story/Starway/")))
            {
                Assert.That(StorySpriteRect.IsSpecified(asset.rect), Is.False, asset.id);
                Assert.That(Resources.Load<Sprite>(asset.resourcePath), Is.Not.Null, asset.id);
            }
        }
        [Test] public void AllCurrentArtHasAGuidDependencyFromTheMainBuildScene()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WilliamParadoxStoryV14/Prefabs/StoryPreviewCanvas.prefab");
            var bindings = prefab.GetComponent<StoryView>().spriteBindings;
            Assert.That(bindings.Length, Is.GreaterThanOrEqualTo(29));
            var dependencies = UnityEditor.AssetDatabase.GetDependencies("Assets/ANIMOL/Scenes/Main/NotebookUI.unity", true);
            foreach (var binding in bindings)
                Assert.That(dependencies.Contains(UnityEditor.AssetDatabase.GetAssetPath(binding.sprite)), Is.True, binding.id);
        }
    }
}
