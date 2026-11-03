using System;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI.StyleV19
{
    public enum PaperButtonRole { Mode, Primary, Utility, Profile }

    [Serializable]
    public sealed class PaperButtonSprites
    {
        public Sprite normal, highlighted, pressed, disabled, active;
    }

    [CreateAssetMenu(menuName = "ANIMOL/UI V19 Skin")]
    public sealed class AnimolUiV19Skin : ScriptableObject
    {
        public PaperButtonSprites mode, primary, utility, profile;
        public Sprite paperStrip, paperPanel;

        public PaperButtonSprites Get(PaperButtonRole role)
        {
            switch (role)
            {
                case PaperButtonRole.Primary: return primary;
                case PaperButtonRole.Utility: return utility;
                case PaperButtonRole.Profile: return profile;
                default: return mode;
            }
        }

        public void Apply(Selectable control, PaperButtonRole role, bool active = false)
        {
            if (!control) return;
            var image = control.targetGraphic as Image;
            var sprites = Get(role);
            if (!image || sprites == null || !sprites.normal)
            {
                Debug.LogError("V19: assign the actual button background Image as Target Graphic.", control);
                return;
            }
            control.transform.localScale = Vector3.one;
            control.transform.localRotation = Quaternion.identity;
            if (image.transform != control.transform)
            {
                var frame = image.rectTransform;
                frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
                frame.offsetMin = frame.offsetMax = Vector2.zero;
            }
            ConfigureImage(image, active && sprites.active ? sprites.active : sprites.normal, true);
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = colors.highlightedColor = colors.pressedColor =
                colors.selectedColor = colors.disabledColor = Color.white;
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0;
            control.colors = colors;
            control.transition = Selectable.Transition.SpriteSwap;
            control.spriteState = new SpriteState
            {
                highlightedSprite = sprites.highlighted,
                pressedSprite = sprites.pressed,
                selectedSprite = sprites.highlighted,
                disabledSprite = sprites.disabled
            };
            // Legacy red/yellow drop effects on this frame only. Text/icon children stay intact.
            foreach (var shadow in image.GetComponents<Shadow>()) shadow.enabled = false;
        }

        public void ApplyStrip(Image image) { ConfigureImage(image, paperStrip, false); }

        private static void ConfigureImage(Image image, Sprite sprite, bool raycast)
        {
            if (!image || !sprite) return;
            image.sprite = sprite;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.localRotation = Quaternion.identity;
            image.overrideSprite = null;
            image.color = Color.white;
            image.material = null;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1;
            image.raycastTarget = raycast;
        }
    }
}
