using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Animol.Starway
{
    [Serializable]
    public sealed class StoryCrayonState
    {
        public Sprite frameA;
        public Material animated;
        public Material still;
    }

    /// <summary>Decorative child Image; never replaces a Button's input or state graphic.</summary>
    [DisallowMultipleComponent, AddComponentMenu("ANIMOL/Story Crayon Frame")]
    public sealed class StoryCrayonFrame : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image decoration;
        [SerializeField] private Selectable control;
        [SerializeField] private StoryCrayonSkin scope;
        [SerializeField] private StoryCrayonState normal;
        [SerializeField] private StoryCrayonState highlighted;
        [SerializeField] private StoryCrayonState pressed;
        [SerializeField] private StoryCrayonState disabled;
        private bool hovered, down, focused;

        public void Configure(Image image, Selectable input, StoryCrayonSkin owner, StoryCrayonState[] states)
        {
            decoration = image; control = input; scope = owner;
            normal = states != null && states.Length > 0 ? states[0] : null;
            highlighted = states != null && states.Length > 1 ? states[1] : normal;
            pressed = states != null && states.Length > 2 ? states[2] : normal;
            disabled = states != null && states.Length > 3 ? states[3] : normal;
            RefreshNow();
        }
        public void ConfigureFrom(StoryCrayonFrame template, Image image, Selectable input, StoryCrayonSkin owner)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            Configure(image, input, owner, new[] { template.normal, template.highlighted, template.pressed, template.disabled });
        }
        private void OnEnable() { hovered = down = focused = false; RefreshNow(); }
        private void LateUpdate() { RefreshNow(); }
        private void OnDisable() { hovered = down = focused = false; }
        private void OnApplicationFocus(bool focusedNow) { if (!focusedNow) { hovered = down = focused = false; RefreshNow(); } }
        public void RefreshNow()
        {
            if (decoration == null || normal == null || normal.frameA == null) return;
            StoryCrayonState state = normal;
            if (control != null)
            {
                if (!control.IsInteractable()) state = disabled;
                else if (down) state = pressed;
                else if (hovered || focused) state = highlighted;
            }
            if (state == null || state.frameA == null) state = normal;
            bool still = scope == null || scope.ReducedMotion;
            Material material = still ? state.still : state.animated;
            if (material == null || material.shader == null || !material.shader.isSupported) material = null;
            if (decoration.sprite != state.frameA) decoration.sprite = state.frameA;
            decoration.type = Image.Type.Sliced;
            decoration.fillCenter = true;
            decoration.raycastTarget = false;
            decoration.color = Color.white;
            if (decoration.material != material) decoration.material = material;
        }
        public void OnPointerEnter(PointerEventData e) { hovered = true; RefreshNow(); }
        public void OnPointerExit(PointerEventData e) { hovered = down = false; RefreshNow(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { down = true; RefreshNow(); } }
        public void OnPointerUp(PointerEventData e) { down = false; RefreshNow(); }
        public void OnSelect(BaseEventData e) { focused = true; RefreshNow(); }
        public void OnDeselect(BaseEventData e) { focused = down = false; RefreshNow(); }
    }
}
