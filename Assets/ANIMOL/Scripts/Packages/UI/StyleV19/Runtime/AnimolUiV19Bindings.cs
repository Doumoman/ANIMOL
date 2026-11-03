using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI.StyleV19
{
    [Serializable]
    public sealed class PaperButtonBinding
    {
        public Selectable control;
        public PaperButtonRole role;
        public bool active;
    }

    // Explicit references only: no object-name guesses and no replacement of navigation/services.
    [DisallowMultipleComponent]
    public sealed class AnimolUiV19Bindings : MonoBehaviour
    {
        public AnimolUiV19Skin skin;
        public Image topPaper, bottomPaper;
        public List<PaperButtonBinding> buttons = new List<PaperButtonBinding>();
        public bool applyOnEnable = true;

        private void OnEnable() { if (applyOnEnable) Apply(); }

        [ContextMenu("Apply V19 visuals")]
        public void Apply()
        {
            if (!skin) return;
            skin.ApplyStrip(topPaper);
            skin.ApplyStrip(bottomPaper);
            foreach (var binding in buttons)
                if (binding != null) skin.Apply(binding.control, binding.role, binding.active);
        }

        // Call from the existing mode controller. This does not call onClick or unlock anything.
        public void SetActive(Selectable control, bool value)
        {
            foreach (var binding in buttons)
                if (binding != null && binding.control == control)
                {
                    binding.active = value;
                    if (skin) skin.Apply(control, binding.role, value);
                    return;
                }
        }
    }
}
