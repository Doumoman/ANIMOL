using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    [CreateAssetMenu(menuName = "ANIMOL/Dice goal platform art V03")]
    public sealed class DiceGoalPlatformArtV03 : ScriptableObject
    {
        public Sprite ready, halfPressed, nearlyFlush, walkable;
        public string sourceSha256;
        public bool Validate(out string error)
        {
            error = null;
            if (ready == null || halfPressed == null || nearlyFlush == null || walkable == null)
            { error = "All four goal-to-platform art states are required."; return false; }
            return true;
        }
    }
}
