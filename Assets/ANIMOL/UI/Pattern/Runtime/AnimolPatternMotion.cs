using DiagonalPattern;
using UnityEngine;

namespace Animol.UI.Pattern
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DiagonalPatternBackground))]
    public sealed class AnimolPatternMotion : MonoBehaviour
    {
        [SerializeField] private bool reducedMotion;
        [SerializeField] private bool modalOpen;
        private bool applicationPaused;
        private bool applicationFocused = true;
        private DiagonalPatternBackground effect;

        private void OnEnable() => ApplyMotion();
        private void OnDisable()
        {
            if (effect) effect.IsScrolling = false;
        }
        private void OnApplicationPause(bool value)
        {
            applicationPaused = value;
            ApplyMotion();
        }
        private void OnApplicationFocus(bool value)
        {
            applicationFocused = value;
            ApplyMotion();
        }
        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            ApplyMotion();
        }
        public void SetModalOpen(bool value)
        {
            modalOpen = value;
            ApplyMotion();
        }
        private void ApplyMotion()
        {
            if (!effect) effect = GetComponent<DiagonalPatternBackground>();
            effect.IsScrolling = isActiveAndEnabled && !reducedMotion && !modalOpen
                && !applicationPaused && applicationFocused;
        }
    }
}
