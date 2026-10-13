using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Animol
{
    [DisallowMultipleComponent]
    public sealed class AnimolQaPhaseCycler : MonoBehaviour
    {
        [SerializeField, Min(.25f)] private float phaseSeconds = 1.25f;

        private IEnumerator Start()
        {
            var player = GetComponent<AnimolPhasePlayer>();
            var animationComponent = GetComponent<Animation>();
            if (player == null || animationComponent == null) yield break;

            var phases = new List<string>();
            foreach (AnimationState state in animationComponent)
                if (state != null && state.name != "idle") phases.Add(state.name);
            phases.Sort(System.StringComparer.Ordinal);

            while (enabled)
            {
                player.PlayIdle();
                yield return new WaitForSeconds(phaseSeconds);
                foreach (var phase in phases)
                {
                    player.PlayPhase(phase);
                    yield return new WaitForSeconds(phaseSeconds);
                }
            }
        }
    }
}
