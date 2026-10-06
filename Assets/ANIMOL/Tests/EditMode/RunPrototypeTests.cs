using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.Tests.EditMode
{
    public sealed class RunPrototypeTests
    {
        [TestCase(false)] [TestCase(true)]
        public void MacroReservationsPrecedeContinuousFiveChunkLegs(bool up)
        {
            var plan = RunRoutePlan.Reserve(up); var map = ScriptableObject.CreateInstance<StageMapDefinition>();
            try
            {
                Assert.That(plan.Legs.Length, Is.EqualTo(2));
                plan.PopulateEmptyMap(map);
                foreach (var leg in plan.Legs)
                {
                    Assert.That(plan.ContinuousFloorLength(map, leg), Is.EqualTo(80));
                    Assert.That(leg.Corridor.Overlaps(plan.Turn.ReservedSpace), Is.False);
                    for (int x = leg.X; x < leg.X + leg.Length; x++)
                        for (int y = leg.FloorY + 1; y < leg.FloorY + 4; y++) Assert.That(map.FindCell(x, y, StageMapLayer.Terrain), Is.Null);
                }
                Assert.That(plan.Turn.Landing.width, Is.GreaterThanOrEqualTo(12));
                Assert.That(plan.OptionalZone.Overlaps(plan.Turn.ReservedSpace), Is.False);
                Assert.That(plan.Legs.Any(l => l.Corridor.Overlaps(plan.OptionalZone)), Is.False);
                Assert.That(map.WorldUnitsPerCell, Is.EqualTo(1));
                Assert.Throws<System.InvalidOperationException>(() => plan.PopulateEmptyMap(map));
            }
            finally { Object.DestroyImmediate(plan); Object.DestroyImmediate(map); }
        }
        [Test]
        public void PrototypeScenesAreOptInAndProtectedScenesStayByteIdentical()
        {
            string[] protectedPaths = { "Assets/ANIMOL/Data/Campaign/Maps/T01-S01.asset", "Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity" };
            var bytes = protectedPaths.Select(File.ReadAllBytes).ToArray();
            foreach (var path in new[] { RunPrototypeBuilder.DownScene, RunPrototypeBuilder.UpScene })
            {
                Assert.That(EditorBuildSettings.scenes.Any(s => s.path == path), Is.False);
                var scene = EditorSceneManager.OpenPreviewScene(path); EditorSceneManager.ClosePreviewScene(scene);
            }
            for (int i = 0; i < bytes.Length; i++) Assert.That(File.ReadAllBytes(protectedPaths[i]), Is.EqualTo(bytes[i]));
        }
    }
}
