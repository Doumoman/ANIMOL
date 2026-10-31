using Die = ANIMOL.Art.EchoTheaterV03.EchoTheaterV03DiceOrientation;

namespace ANIMOL.Presentation.DiceHintV01
{
    // Pure decision helper. A true result still needs an atomic commit in the real host model.
    public static class DiceGoalConversionIntentV03
    {
        public static bool ShouldAbsorb(Die.State actualAfterRoll, int requiredTop,
            bool actuallyRolled, bool endedOnThisGoal, bool alreadyConverted)
        {
            if (!actuallyRolled || !endedOnThisGoal || alreadyConverted || requiredTop < 1 || requiredTop > 6) return false;
            string error;
            return Die.Validate(actualAfterRoll, out error) && actualAfterRoll.Up == requiredTop;
        }
    }
}
