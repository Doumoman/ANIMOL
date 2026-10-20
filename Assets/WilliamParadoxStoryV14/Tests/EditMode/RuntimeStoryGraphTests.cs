using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WilliamParadox.Story.Tests
{
    public sealed class RuntimeStoryGraphTests
    {
        private static StoryPackage Package()
        {
            return new StoryPackage
            {
                packageId = "test.story", contentVersion = "14.test", startNodeId = "wish",
                assets = Array.Empty<StoryAsset>(),
                nodes = new[]
                {
                    Node("wish", "line", "confirmed", "돌아갈래.", null, null),
                    Node("confirmed", "cue", "lamp", null, new[] { "ISOL_WANTS_RETURN" }, null),
                    new StoryNode { id = "lamp", sceneId = "R16", beatId = "B3", kind = "gate", gateId = "BLOCK_LAMP", gateLabel = "등불 가리기", nextId = "return", requiredFlags = new[] { "ISOL_WANTS_RETURN" }, setFlags = new[] { "PLAYER_BLOCKS_LAMP" } },
                    Node("return", "cue", "credits", null, new[] { "ISOL_PRESENT", "RETURN_PATH_USED" }, new[] { "PLAYER_BLOCKS_LAMP" }),
                    new StoryNode { id = "credits", sceneId = "R17", beatId = "B5", kind = "credits", nextId = "end", credits = new[] { "첫 기록", "마지막 기록" }, requiredFlags = new[] { "ISOL_PRESENT", "RETURN_PATH_USED" } },
                    Node("end", "end", null, null, new[] { "PREVIEW_SESSION_FINISHED" }, null)
                }
            };
        }
        private static StoryNode Node(string id, string kind, string next, string text, string[] sets, string[] requires)
        {
            return new StoryNode { id = id, sceneId = "R16", beatId = "B1", kind = kind, nextId = next, text = text, setFlags = sets, requiredFlags = requires };
        }
        private static StoryGraph Graph(StoryPackage p)
        {
            Assert.That(StoryGraph.TryCreate(p, out var graph, out var errors), Is.True, string.Join("\n", errors)); return graph;
        }
        private static StorySaveData Save(StorySession s)
        {
            return new StorySaveData { packageId = s.Graph.Package.packageId, contentVersion = s.Graph.Package.contentVersion, nodeId = s.Current.id, flags = s.Flags };
        }
        [Test]
        public void ImportedStoryKeepsFullImagesAndMeasuredInsetRects()
        {
            var source = Resources.Load<TextAsset>("WilliamParadox/V14/Story/ranch_story");
            Assert.That(source, Is.Not.Null);
            var package = JsonUtility.FromJson<StoryPackage>(source.text);
            var graph = Graph(package);
            Assert.That(graph.Asset("R01").rect, Is.Null);
            Assert.That(graph.Asset("BG14_epilogue_book").rect, Is.Null);
            Assert.That(graph.Asset("noodle_01").rect.width, Is.EqualTo(506));
            Assert.That(graph.Asset("carrot_pudding_full").rect.width, Is.EqualTo(284));
        }
        [Test]
        public void WishRequiresAcknowledgement_AndGenericAdvanceCannotBypassPlayerGate()
        {
            var s = new StorySession(Graph(Package()));
            Assert.That(s.HasFlag("ISOL_WANTS_RETURN"), Is.False);
            Assert.That(s.Acknowledge(), Is.True);
            Assert.That(s.HasFlag("ISOL_WANTS_RETURN"), Is.True);
            Assert.That(s.Acknowledge(), Is.True);
            Assert.That(s.Current.id, Is.EqualTo("lamp"));
            Assert.That(s.HasFlag("PLAYER_BLOCKS_LAMP"), Is.False);
            Assert.That(s.Acknowledge(), Is.False);
            Assert.That(s.Current.id, Is.EqualTo("lamp"));
            Assert.That(s.ActivateGate(), Is.True);
            Assert.That(s.HasFlag("PLAYER_BLOCKS_LAMP"), Is.True);
            Assert.That(s.Current.id, Is.EqualTo("return"));
        }
        [Test]
        public void ReturnIsSavedBeforeCredits_AndCreditsNeedsItsOwnContinuation()
        {
            var s = new StorySession(Graph(Package()));
            s.Acknowledge(); s.Acknowledge(); s.ActivateGate(); s.Acknowledge();
            Assert.That(s.Current.kind, Is.EqualTo("credits"));
            Assert.That(s.HasFlag("ISOL_PRESENT"), Is.True);
            Assert.That(s.HasFlag("RETURN_PATH_USED"), Is.True);
            Assert.That(s.Acknowledge(), Is.False);
            var restored = new StorySession(s.Graph);
            Assert.That(restored.Restore(Save(s)), Is.True);
            Assert.That(restored.Current.kind, Is.EqualTo("credits"));
            Assert.That(restored.ContinueCredits(), Is.True);
        }
        [Test]
        public void SaveAtGateDoesNotCarryUnperformedAction_AndFutureFlagInjectionFails()
        {
            var s = new StorySession(Graph(Package())); s.Acknowledge(); s.Acknowledge();
            var save = Save(s);
            Assert.That(new StorySession(s.Graph).Restore(save), Is.True);
            save.flags = save.flags.Concat(new[] { "PLAYER_BLOCKS_LAMP" }).ToArray();
            Assert.That(new StorySession(s.Graph).Restore(save), Is.False);
        }
        [Test]
        public void InvalidSaveVersionNodeAndNanFailWithoutChangingCurrentProgress()
        {
            var s = new StorySession(Graph(Package()));
            var save = Save(s); save.contentVersion = "old";
            Assert.That(s.Restore(save), Is.False);
            save = Save(s); save.nodeId = "missing";
            Assert.That(s.Restore(save), Is.False);
            save = Save(s); save.dimAlpha = float.NaN;
            Assert.That(s.Restore(save), Is.False);
            Assert.That(s.Current.id, Is.EqualTo("wish"));
        }
        [Test]
        public void DeveloperJumpCannotInventThePlayersAction()
        {
            var s = new StorySession(Graph(Package()));
            Assert.That(s.DebugJump("credits"), Is.False);
            Assert.That(s.HasFlag("PLAYER_BLOCKS_LAMP"), Is.False);
        }
        [Test]
        public void DuplicateIdsDanglingReferencesAndNegativeCueTimingsAreRejected()
        {
            var p = Package(); p.nodes[1].id = "wish";
            Assert.That(StoryGraph.Validate(p).Any(e => e.Contains("duplicate")), Is.True);
            p = Package(); p.nodes[0].nextId = "missing";
            Assert.That(StoryGraph.Validate(p).Any(e => e.Contains("nextId")), Is.True);
            p = Package(); p.nodes[0].cues = new[] { new StoryCue { type = "shake", duration = -1, amount = 6 } };
            Assert.That(StoryGraph.Validate(p).Any(e => e.Contains("timing")), Is.True);
        }
        [Test]
        public void RequiredFlagsMustBeProducedBeforeTheirUse()
        {
            var p = Package(); p.nodes[0].requiredFlags = new[] { "PLAYER_BLOCKS_LAMP" };
            Assert.That(StoryGraph.Validate(p).Any(e => e.Contains("cannot be reached")), Is.True);
        }
        [Test]
        public void UnknownAssetAndGraphCyclesAreRejected()
        {
            var p = Package(); p.nodes[0].artId = "unknown";
            Assert.That(StoryGraph.Validate(p).Any(e => e.Contains("artId")), Is.True);
            p = Package(); p.nodes[4].nextId = "wish";
            Assert.That(StoryGraph.Validate(p).Any(e => e.Contains("cycle")), Is.True);
        }
    }
}
