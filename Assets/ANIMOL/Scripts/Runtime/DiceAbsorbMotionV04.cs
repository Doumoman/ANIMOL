using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    public static class DiceAbsorbMotionV04
    {
        public const float DurationSeconds = .80f;
        public const float FrontPlaneY = -.375f;
        public static void Sample(float progress, out float dieY, out float lidClosed)
        {
            float p = Mathf.Clamp01(progress);
            if (p < .12f) dieY = Mathf.Sin(p / .12f * Mathf.PI) * .02f;
            else dieY = -1.02f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, .67f, p));
            lidClosed = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.67f, .94f, p));
        }
    }
}
