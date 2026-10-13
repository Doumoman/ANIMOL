using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.PortraitArtV1
{
    public sealed class PortraitArtScreen : MonoBehaviour
    {
        public RectTransform CharacterBackdropSlot;
        public Button Play, Back, Ad, Shop, Settings, Campaign, Competition, Cooperation;
        public TMP_Text Currency, CurrencyState, CompetitionState, CooperationState;

        // Keep the supplied whole-control artwork's palette when legacy scene polish runs.
        // Existing UiButtonFeedback supplies only the project's normal, non-blocking tap feedback.
        private void LateUpdate()
        {
            foreach(var label in GetComponentsInChildren<TMP_Text>(true)) {
                Point(label.font);
                foreach(var fallback in label.font.fallbackFontAssetTable) Point(fallback);
            }
            foreach(var button in GetComponentsInChildren<Button>(true)) {
                if(button.targetGraphic is Image image && image.color != Color.white) image.color=Color.white;
                var colors=button.colors;
                colors.normalColor=Color.white;
                colors.highlightedColor=new Color32(255,245,223,255);
                colors.selectedColor=colors.highlightedColor;
                colors.pressedColor=new Color32(217,197,169,255);
                colors.disabledColor=new Color32(182,177,174,140);
                colors.colorMultiplier=1; colors.fadeDuration=.08f;
                if(button.colors != colors) button.colors=colors;
                if(button==Competition || button==Cooperation)
                    foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
                        label.color=button.interactable ? new Color32(77,43,30,255) : new Color32(242,225,195,255);
            }
        }
        private static void Point(TMP_FontAsset font)
        {
            if(font==null) return;
            foreach(var texture in font.atlasTextures) if(texture!=null) { texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Clamp;texture.anisoLevel=0; }
        }
    }
}
