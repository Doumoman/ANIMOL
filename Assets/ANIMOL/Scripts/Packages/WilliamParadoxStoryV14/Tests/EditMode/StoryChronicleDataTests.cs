using System.IO;
using System.Linq;
using Animol.Starway;
using NUnit.Framework;
using UnityEngine;

namespace WilliamParadox.Story.Tests
{
    public sealed class StoryChronicleDataTests
    {
        [Test] public void AuthoredRecordHasSixPairsAndExactTiming()
        {
            var data = JsonUtility.FromJson<StoryChronicleData>(File.ReadAllText("Assets/ANIMOL/Data/StarwayRanch/Story/ranch_chronicle.json"));
            Assert.That(data.records.Length, Is.EqualTo(6));
            Assert.That(data.records.Select(r => r.id).Distinct().Count(), Is.EqualTo(6));
            Assert.That(data.records.All(r => r.asteran.Split('\n').Length == 3 && r.higher.Split('\n').Length == 3), Is.True);
            Assert.That(data.titles.asteran, Is.EqualTo("별길 목장의 귀환 기록"));
            Assert.That(data.titles.higher, Is.EqualTo("같은 밤의 정정 기록"));
            Assert.That(data.scrollSpeed, Is.EqualTo(84)); Assert.That(data.holdMultiplier, Is.EqualTo(3));
            Assert.That(data.introDuration, Is.EqualTo(0.6f)); Assert.That(data.rewindDuration, Is.EqualTo(4.2f));
            Assert.That(data.afterAsteranPause, Is.EqualTo(1)); Assert.That(data.afterRewritePause, Is.EqualTo(0.65f));
        }
        [TestCase(true)] [TestCase(false)] public void RestoredReadAndSkipRemainDistinct(bool read)
        {
            Assert.That(StoryGraph.TryCreate(JsonUtility.FromJson<StoryPackage>(Resources.Load<TextAsset>("WilliamParadox/V14/Story/ranch_story").text), out var graph, out var errors), Is.True, string.Join("\n", errors));
            var session = new StorySession(graph);
            for (int i = 0; i < 600 && session.Current.kind != "credits"; i++)
                Assert.That(session.Current.kind == "gate" ? session.ActivateGate() : session.Acknowledge(), Is.True);
            Assert.That(session.RecordChronicleResult(read, !read), Is.True);
            var save = new StorySaveData { packageId = graph.Package.packageId, contentVersion = graph.Package.contentVersion,
                nodeId = session.Current.id, flags = session.Flags, chronicleRevision = "0.28", chronicleReadThrough = read, chronicleSkipped = !read };
            var restored = new StorySession(graph); Assert.That(restored.Restore(save), Is.True);
            Assert.That(restored.ChronicleReadThrough, Is.EqualTo(read)); Assert.That(restored.ChronicleSkipped, Is.EqualTo(!read));
            Assert.That(restored.HasFlag("RANCH_RULE_EPILOGUE_SEEN"), Is.EqualTo(read));
            Assert.That(restored.ContinueCredits(), Is.True);
        }
    }
}
