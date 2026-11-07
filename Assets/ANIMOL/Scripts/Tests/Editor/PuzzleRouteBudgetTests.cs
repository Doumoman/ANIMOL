using System;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using William.NotebookUI;
using Object = UnityEngine.Object;

namespace ANIMOL.Tests
{
    public sealed class PuzzleRouteBudgetTests
    {
        static TextAsset Catalog => AssetDatabase.LoadAssetAtPath<TextAsset>(StarwayShortestPathCatalog.AssetPath);
        [TestCase("NESW.", "↑→↓←○", 4)]
        [TestCase(" ↑ → ○\n↓ ← ", "↑→○↓←", 4)]
        [TestCase("wwnneess", "←←↑↑→→↓↓", 8)]
        public void ArrowNotationKeepsInputsAndFreeWaits(string source, string arrows, int cost)
        {
            Assert.That(PuzzleRouteNotation.ToArrows(source), Is.EqualTo(arrows));
            Assert.That(PuzzleRouteNotation.ToArrows(PuzzleRouteNotation.Display(source)), Is.EqualTo(arrows));
            Assert.That(PuzzleRouteNotation.InputCount(source), Is.EqualTo(cost));
        }
        [Test] public void UnknownDirectionsAreRejected() => Assert.Throws<FormatException>(() => PuzzleRouteNotation.ToArrows("↑X"));

        static void Replay(PuzzleDeviceSession session, string route)
        {
            if (session.Devices.Any(d => d.role == PrototypeRole.Wind)) session.AdvanceWind();
            foreach (char action in route)
            {
                if (session.Failed) break;
                if (char.IsWhiteSpace(action)) continue;
                if (PuzzleRouteNotation.IsWait(action)) Assert.That(session.AdvanceWind().accepted, Is.True);
                else
                {
                    Assert.That(PuzzleRouteNotation.TryDirection(action, out var direction), Is.True);
                    Assert.That(session.Apply(direction).accepted, Is.True);
                }
            }
        }

        [Test] public void EverySolvedMapUsesItsOwnVerifiedBudgetAndWinsExactlyAtZero()
        {
            int configured = 0;
            foreach (var entry in PuzzleShortestPathRecord.Read().maps)
            {
                var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(entry.mapAsset);
                Assert.That(PuzzleShortestPathRecord.IsCurrent(source, entry), Is.True, entry.mapId);
                Assert.That(entry.route, Is.EqualTo(PuzzleRouteNotation.ToArrows(entry.route)));
                bool applied = StarwayShortestPathCatalog.TryGetBudget(source, Catalog, out int budget);
                if (entry.status != "solved") { Assert.That(applied, Is.False); continue; }
                Assert.That(applied, Is.True, entry.mapId); Assert.That(budget, Is.EqualTo(entry.distance)); configured++;
                int sourceLimit = source.scenario.costLimit;
                var map = Object.Instantiate(source); map.scenario = Object.Instantiate(source.scenario);
                try
                {
                    map.scenario.costLimit = budget;
                    var exact = new PuzzleDeviceSession(map); Replay(exact, entry.route);
                    Assert.That(exact.Won, Is.True, entry.mapId); Assert.That(exact.Failed, Is.False);
                    Assert.That(budget - exact.Cost, Is.Zero);
                    map.scenario.costLimit = budget - 1;
                    var shortBudget = new PuzzleDeviceSession(map); Replay(shortBudget, entry.route);
                    Assert.That(shortBudget.Won, Is.False, entry.mapId); Assert.That(shortBudget.Failed, Is.True);
                    Assert.That(shortBudget.Cost, Is.EqualTo(budget - 1));
                    Assert.That(source.scenario.costLimit, Is.EqualTo(sourceLimit));
                }
                finally { Object.DestroyImmediate(map.scenario); Object.DestroyImmediate(map); }
            }
            Assert.That(configured, Is.EqualTo(19));
        }
        [Test] public void StaleCatalogDoesNotApplyAChangedProof()
        {
            var entry = PuzzleShortestPathRecord.Read().maps.First(e => e.status == "solved");
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(entry.mapAsset);
            var stale = new TextAsset(Catalog.text.Replace(entry.mapSha256, "invalid-source-hash"));
            try { Assert.That(StarwayShortestPathCatalog.TryGetBudget(map, stale, out _), Is.False); }
            finally { Object.DestroyImmediate(stale); }
        }
        [Test] public void ActiveNotebookPrefabReferencesTheVerifiedCatalog()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab");
            var gateway = prefab.GetComponentInChildren<NotebookProjectGateway>(true);
            Assert.That(gateway.shortestPathCatalog, Is.SameAs(Catalog));
        }
    }
}
