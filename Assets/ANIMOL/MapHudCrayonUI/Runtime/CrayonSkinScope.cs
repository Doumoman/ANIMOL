using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using William.NotebookUI;

namespace Animol.MapHudCrayonUI
{
    /// <summary>Clock and accessibility bridge for the explicitly bound map or puzzle HUD only.</summary>
    [DefaultExecutionOrder(9000), DisallowMultipleComponent]
    public sealed class CrayonSkinScope : MonoBehaviour
    {
        public NotebookRoot mapOwner;
        public NotebookGameplayHud gameplayOwner;
        public CrayonImageBinding[] bindings = System.Array.Empty<CrayonImageBinding>();
        // Optional support for a host that spawns UI pickup images inside its existing flight layer.
        public RectTransform dynamicFlightRoot;
        public CrayonSpriteSet[] dynamicFlightStates = System.Array.Empty<CrayonSpriteSet>();

        [SerializeField] private bool reducedMotion;
        private bool applicationPaused, applicationFocused = true;
        private int observedFlightChildren = -1;
        private float nextFlightScan;
        private readonly List<Image> flightImages = new List<Image>();
        private readonly List<CrayonImageBinding> dynamicBindings = new List<CrayonImageBinding>();
        private static readonly int Clock = Shader.PropertyToID("_ANIMOLCrayonUnscaledTime");

        public bool EffectiveReducedMotion => reducedMotion || applicationPaused || !applicationFocused
            || (mapOwner && mapOwner.IsReducedMotion) || (gameplayOwner && gameplayOwner.reducedMotion);

        /// <summary>Protected main/settings/popup trees remain outside this decorative skin.</summary>
        public bool CanDecorate(Image image)
        {
            if (!image || (!mapOwner && !gameplayOwner)) return false;
            Transform target = image.transform;
            if (mapOwner)
            {
                if (!mapOwner.stageScreen || !Inside(target, mapOwner.stageScreen)) return false;
                if (Inside(target, mapOwner.mainScreen) || Inside(target, mapOwner.settingsScreen) || Inside(target, mapOwner.modal)) return false;
            }
            if (gameplayOwner)
            {
                if (!target.IsChildOf(gameplayOwner.transform) && target != gameplayOwner.transform) return false;
                if (Inside(target, gameplayOwner.pauseOverlay) || Inside(target, gameplayOwner.clearOverlay)
                    || Inside(target, gameplayOwner.modalBackdrop)) return false;
            }
            return true;
        }

        private static bool Inside(Transform target, GameObject root) => root && (target == root.transform || target.IsChildOf(root.transform));

        private void OnEnable()
        {
            applicationFocused = Application.isFocused;
            RefreshBindings(); RefreshNow();
        }

        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled; RefreshNow();
        }

        /// <summary>Call after explicit editor/runtime bindings have been assigned.</summary>
        public void RefreshBindings()
        {
            foreach (CrayonImageBinding binding in bindings ?? System.Array.Empty<CrayonImageBinding>())
                if (binding) binding.scope = this;
            observedFlightChildren = -1;
            nextFlightScan = 0;
            DiscoverFlightImages();
        }

        public void RefreshNow()
        {
            Shader.SetGlobalFloat(Clock, Time.unscaledTime);
            foreach (CrayonImageBinding binding in bindings ?? System.Array.Empty<CrayonImageBinding>())
                if (binding && binding.isActiveAndEnabled) binding.RefreshNow();
            for (int i = dynamicBindings.Count - 1; i >= 0; i--)
            {
                CrayonImageBinding binding = dynamicBindings[i];
                if (!binding) { dynamicBindings.RemoveAt(i); continue; }
                if (binding.isActiveAndEnabled) binding.RefreshNow();
            }
        }

        private void LateUpdate()
        {
            Shader.SetGlobalFloat(Clock, Time.unscaledTime);
            DiscoverFlightImages();
        }

        private void DiscoverFlightImages()
        {
            if (!Application.isPlaying || !gameplayOwner || !dynamicFlightRoot || dynamicFlightRoot != gameplayOwner.flightLayer
                || dynamicFlightStates == null || dynamicFlightStates.Length == 0) return;
            int children = dynamicFlightRoot.childCount;
            if (observedFlightChildren == children && Time.unscaledTime < nextFlightScan) return;
            observedFlightChildren = children;
            // Reuses the list and also catches same-count replacements or nested host-created images.
            nextFlightScan = Time.unscaledTime + .2f;
            flightImages.Clear(); dynamicFlightRoot.GetComponentsInChildren(true, flightImages);
            foreach (Image image in flightImages)
            {
                if (!image || !CanDecorate(image) || image.GetComponent<CrayonImageBinding>() || image.GetComponentInParent<CrayonImageBinding>()) continue;
                Sprite sprite = image.overrideSprite ? image.overrideSprite : image.sprite;
                bool known = false;
                foreach (CrayonSpriteSet state in dynamicFlightStates)
                    if (state != null && state.source == sprite) { known = true; break; }
                if (!known) continue;
                var child = new GameObject("Prepainted Crayon Pickup Decoration", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                child.transform.SetParent(image.transform, false); child.transform.SetAsFirstSibling();
                RectTransform rect = (RectTransform)child.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
                Image decoration = child.GetComponent<Image>(); decoration.raycastTarget = false;
                child.GetComponent<LayoutElement>().ignoreLayout = true;
                CrayonImageBinding binding = image.gameObject.AddComponent<CrayonImageBinding>();
                binding.Configure(image, decoration, dynamicFlightStates, this); dynamicBindings.Add(binding);
            }
        }

        private void OnApplicationPause(bool paused) { applicationPaused = paused; RefreshNow(); }
        private void OnApplicationFocus(bool focused) { applicationFocused = focused; RefreshNow(); }
        private void OnDisable()
        {
            foreach (CrayonImageBinding binding in bindings ?? System.Array.Empty<CrayonImageBinding>()) if (binding) binding.RestoreOriginal();
            foreach (CrayonImageBinding binding in dynamicBindings) if (binding) binding.RestoreOriginal();
        }
    }
}
