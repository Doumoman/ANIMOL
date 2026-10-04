#if UNITY_STANDALONE && !UNITY_EDITOR
namespace ANIMOL.Gameplay
{
    // Unity's mobile Handheld API is absent from standalone players. Existing feedback
    // already checks Application.isMobilePlatform; desktop builds intentionally have no haptic.
    // Keep this shim out of Editor/mobile so those targets continue using UnityEngine.Handheld.
    internal static class Handheld
    {
        public static void Vibrate() { }
    }
}
#endif
