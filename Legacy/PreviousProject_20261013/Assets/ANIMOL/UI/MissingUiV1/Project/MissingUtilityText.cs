using ANIMOL.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.MissingUiV1.Project
{
    [DefaultExecutionOrder(1800)]
    [RequireComponent(typeof(Text))]
    public sealed class MissingUtilityText : MonoBehaviour
    {
        public string Korean, English;
        public int BaseSize=32;
        public Color TextColor=MissingUiLayout.Ink;
        private Text label;
        private void LateUpdate()=>Apply();
        public void Apply()
        {
            if(label==null)label=GetComponent<Text>();
            label.text=MobileControlPreferences.Localize(Korean,English);
            // Keep a visibly larger step on the existing 16px physical glyph grid.
            label.fontSize=MobileControlPreferences.LargeText?Mathf.CeilToInt(BaseSize*1.22f/16f)*16:BaseSize;
            label.resizeTextForBestFit=false;label.fontStyle=FontStyle.Normal;label.color=TextColor;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;
        }
    }
}
