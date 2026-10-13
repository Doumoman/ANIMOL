using UnityEngine;
using System;

namespace Animol
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animation))]
    public sealed class AnimolPhasePlayer : MonoBehaviour
    {
        private Animation _animation;

        public string CurrentPhase { get; private set; } = string.Empty;

        private void Awake()
        {
            _animation = GetComponent<Animation>();
        }

        public bool PlayPhase(string phase)
        {
            if (_animation == null) _animation = GetComponent<Animation>();
            if (string.IsNullOrWhiteSpace(phase)) return false;
            var stateName = ResolveStateName(phase);
            if (stateName == null) return false;
            if (CurrentPhase == phase && _animation.IsPlaying(stateName)) return true;
            _animation.Stop();
            var played = _animation.Play(stateName);
            if (played) CurrentPhase = phase;
            return played;
        }

        public float GetPhaseDuration(string phase)
        {
            if (_animation == null) _animation = GetComponent<Animation>();
            if (string.IsNullOrWhiteSpace(phase)) return 0f;
            var stateName = ResolveStateName(phase);
            var clip = stateName == null ? null : _animation.GetClip(stateName);
            return clip == null ? 0f : clip.length;
        }

        public bool HasPhase(string phase)
        {
            if (_animation == null) _animation = GetComponent<Animation>();
            return !string.IsNullOrWhiteSpace(phase) && ResolveStateName(phase) != null;
        }

        private string ResolveStateName(string phase)
        {
            if (_animation.GetClip(phase) != null) return phase;
            var suffix = "_" + phase;
            foreach (AnimationState state in _animation)
                if (state != null && state.name.EndsWith(suffix, StringComparison.Ordinal)) return state.name;
            return null;
        }

        public void PlayIdle() => PlayPhase("idle");
        public void PlayWarn() => PlayPhase("warn");
        public void PlayActive() => PlayPhase("active");
        public void PlayRecover() => PlayPhase("recover");
    }
}
