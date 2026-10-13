using System;
using System.Collections.Generic;

namespace ANIMOL.Gameplay
{
    public enum MobileTouchAction { SwipeMove, Jump, Special, Ability, AnimalGround, AnimalGlider }

    public sealed class TouchCaptureRegistry
    {
        private readonly Dictionary<int, MobileTouchAction> captures = new Dictionary<int, MobileTouchAction>();
        public int ActiveCount => captures.Count;
        public bool TryCapture(int pointerId, MobileTouchAction action)
        {
            if (captures.ContainsKey(pointerId)) return false;
            captures.Add(pointerId, action);
            return true;
        }
        public bool IsCapturedBy(int pointerId, MobileTouchAction action) => captures.TryGetValue(pointerId, out var captured) && captured == action;
        public bool Release(int pointerId) => captures.Remove(pointerId);
    }

}
