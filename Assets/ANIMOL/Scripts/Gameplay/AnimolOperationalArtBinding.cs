using System;
using System.Linq;
using Animol;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public enum AnimolOperationalArtMode { Whole, BalancePlate, MoonStair }

    [DisallowMultipleComponent]
    public sealed class AnimolOperationalArtBinding : MonoBehaviour
    {
        [SerializeField] private AnimolOperationalArtMode mode;
        [SerializeField] private GameObject artRoot;
        [SerializeField] private int variant;

        public GameObject ArtRoot => artRoot;
        public int Variant => variant;
        public AnimolPhasePlayer PhasePlayer => artRoot == null ? null : artRoot.GetComponent<AnimolPhasePlayer>();

        public void Configure(GameObject root, AnimolOperationalArtMode bindingMode)
        {
            artRoot = root;
            mode = bindingMode;
            DisableArtColliders();
            ApplyVariant();
        }

        public void SetVariant(int value)
        {
            variant = Mathf.Max(0, value);
            ApplyVariant();
        }

        private void Awake() { DisableArtColliders(); ApplyVariant(); }

        private void DisableArtColliders()
        {
            if (artRoot == null) return;
            foreach (var collider in artRoot.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
            foreach (var body in artRoot.GetComponentsInChildren<Rigidbody2D>(true)) body.simulated = false;
        }

        private void ApplyVariant()
        {
            if (artRoot == null || mode == AnimolOperationalArtMode.Whole) return;
            var names = mode == AnimolOperationalArtMode.BalancePlate
                ? new[] { "left_plate", "right_plate" }
                : new[] { "stair_1", "stair_2", "stair_3" };
            var selected = names[Mathf.Clamp(variant, 0, names.Length - 1)];
            foreach (var renderer in artRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var part = names.FirstOrDefault(name => renderer.transform.name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (part != null) renderer.enabled = part == selected;
                else if (mode == AnimolOperationalArtMode.BalancePlate)
                    renderer.enabled = variant == 0 && renderer.transform.name.Equals("base", StringComparison.OrdinalIgnoreCase);
                else renderer.enabled = variant == 0 && renderer.transform.name.Equals("moon_dial", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
