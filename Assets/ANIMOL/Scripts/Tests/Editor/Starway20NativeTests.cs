using ANIMOL.Puzzle.Editor;
using ANIMOL.StarwayMaps;
using NUnit.Framework;
using UnityEngine;

namespace ANIMOL.Puzzle.Tests
{
    public sealed class Starway20NativeTests
    {
        [Test]
        public void AllTwentyNativeMapsReplayTheirSourceSolutions()
        {
            Assert.That(Starway20ReplayValidation.RunAll(), Is.EqualTo("20/20"));
        }

        [Test]
        public void StarwayFocusUsesSevenByElevenCellsAndClampsToRegion()
        {
            var focus = StarwayCameraMath.FocusWorldSize(Vector2.one);
            Assert.That(focus, Is.EqualTo(new Vector2(7, 11)));
            var region = new[] { new StarwayCell(2, 3), new StarwayCell(17, 19) };
            var bounds = StarwayCameraMath.CellBounds(region, Vector2.zero, Vector2.one);
            var center = StarwayCameraMath.ClampCenter(new Vector2(-100, 100), bounds, focus);
            Assert.That(center.x, Is.EqualTo(5.0f).Within(.001f));
            Assert.That(center.y, Is.EqualTo(14.0f).Within(.001f));
        }
    }
}
