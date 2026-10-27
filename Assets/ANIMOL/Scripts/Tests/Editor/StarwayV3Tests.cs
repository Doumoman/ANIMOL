#if UNITY_EDITOR
using ANIMOL.Puzzle.Editor;
using NUnit.Framework;

namespace ANIMOL.Tests
{
    public sealed class StarwayV3Tests
    {
        [Test] public void AllTwentyUnitySessionsMatchV3TracesAndStrictBranches()
        {
            var result = StarwayV3Validation.Run();
            Assert.That(result.errors, Is.Empty, result.ToString());
            Assert.That(result.maps, Is.EqualTo(20));
            Assert.That(result.correctInputs, Is.EqualTo(1650));
            Assert.That(result.wrongBaseInputs, Is.EqualTo(4950));
            Assert.That(result.wrongStrictInputs, Is.EqualTo(4950));
            Assert.That(result.exitMaskCases, Is.EqualTo(160));
            Assert.That(result.invalidMapCases, Is.EqualTo(60));
            Assert.That(result.invalidOverrideCases, Is.EqualTo(40));
            Assert.That(result.repeatBubbleCases, Is.EqualTo(60));
        }
    }
}
#endif
