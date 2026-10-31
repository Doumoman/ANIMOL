using NUnit.Framework;
using Die = ANIMOL.Art.EchoTheaterV03.EchoTheaterV03DiceOrientation;

namespace ANIMOL.Presentation.DiceHintV01.Tests
{
    public sealed class DiceGoalConversionIntentV03Tests
    {
        [Test]
        public void OnlyARealMatchingRollConvertsAnUnconvertedGoal()
        {
            foreach (var state in Die.All24)
                for (int target = 1; target <= 6; target++)
                    Assert.That(DiceGoalConversionIntentV03.ShouldAbsorb(state, target, true, true, false), Is.EqualTo(state.Up == target));
            Assert.That(DiceGoalConversionIntentV03.ShouldAbsorb(Die.Initial, 1, false, true, false), Is.False);
            Assert.That(DiceGoalConversionIntentV03.ShouldAbsorb(Die.Initial, 1, true, false, false), Is.False);
            Assert.That(DiceGoalConversionIntentV03.ShouldAbsorb(Die.Initial, 1, true, true, true), Is.False);
            Assert.That(DiceGoalConversionIntentV03.ShouldAbsorb(default(Die.State), 1, true, true, false), Is.False);
        }
    }
}
