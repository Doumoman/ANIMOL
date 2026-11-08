using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI.Crayon
{
    // Selects authored materials only. PNG selection and timing belong to the shader.
    public sealed class CrayonUIMotion : MonoBehaviour
    {
        [Serializable] public sealed class Binding
        {
            public Image image;
            public Material animated, still;
        }
        public List<Binding> bindings = new List<Binding>();
        public static bool GlobalReducedMotion { get; private set; }
        static event Action<bool> Changed;
        public bool ReducedMotion { get; private set; }
        public bool BlocksInput => false;

        public static void SetGlobalReducedMotion(bool value)
        {
            GlobalReducedMotion = value;
            Changed?.Invoke(value);
        }
        public void SetReducedMotion(bool value)
        {
            ReducedMotion = value;
            foreach (var binding in bindings)
                if (binding.image)
                    binding.image.material = value ? binding.still : binding.animated;
        }
        void OnEnable()
        {
            Changed += SetReducedMotion;
            SetReducedMotion(GlobalReducedMotion);
        }
        void OnDisable() => Changed -= SetReducedMotion;
    }
}
