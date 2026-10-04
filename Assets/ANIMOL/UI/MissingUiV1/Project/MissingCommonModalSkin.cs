using System.Linq;
using ANIMOL.Typography;
using UnityEngine;
using UnityEngine.UI;
using static ANIMOL.MissingUiV1.Project.MissingUiLayout;

namespace ANIMOL.MissingUiV1.Project
{
    public sealed class MissingCommonModalSkin : MonoBehaviour
    {
        private Text[] bodyTexts;
        private void LateUpdate()
        {
            if(bodyTexts==null)return;
            foreach(var text in bodyTexts)if(text!=null){text.color=Ink;text.fontStyle=FontStyle.Normal;}
        }
        public static void Apply(Transform modal,MissingUiArt art)
        {
            if(modal.GetComponent<MissingCommonModalSkin>()!=null)return;
            var card=modal.Find("Card") as RectTransform; if(card==null)return;
            var skin=modal.gameObject.AddComponent<MissingCommonModalSkin>();
            var texts=card.GetComponentsInChildren<Text>(true).Where(t=>t.GetComponentInParent<Button>(true)==null).ToArray();
            skin.bodyTexts=texts;
            var buttons=card.GetComponentsInChildren<Button>(true);
            Rect(card,new Vector2(.04f,.12f),new Vector2(.96f,.88f),Vector2.zero,Vector2.zero);
            var panel=card.GetComponent<Image>()??card.gameObject.AddComponent<Image>(); panel.sprite=art.Panel; panel.type=Image.Type.Sliced; panel.color=Color.white;
            var scroll=Scroll(card,art); Rect((RectTransform)scroll.transform,Vector2.zero,Vector2.one,new Vector2(38,190),new Vector2(-38,-46));
            foreach(var text in texts)
            {
                text.transform.SetParent(scroll.content,false); text.alignment=TextAnchor.UpperLeft;
                text.color=Ink; text.fontStyle=FontStyle.Normal; text.fontSize=text.name.Contains("Title")?48:32;
                text.resizeTextForBestFit=false; text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Overflow;
                var bridge=text.GetComponent<PixelTextBridge>()??text.gameObject.AddComponent<PixelTextBridge>();
                bridge.Profile=art.Typography; bridge.SizeScrollContentToText=true;
            }
            for(var i=0;i<buttons.Length;i++)
            {
                var button=buttons[i]; button.transform.SetParent(card,false);
                var r=(RectTransform)button.transform;
                Rect(r,new Vector2((float)i/buttons.Length,0),new Vector2((float)(i+1)/buttons.Length,0),new Vector2(28,38),new Vector2(-28,150));
                var img=button.GetComponent<Image>();
                bool secondary=button.name.Contains("Cancel")||button.name.Contains("Back")||button.name.Contains("Continue");
                if(img!=null) { img.sprite=secondary?art.Secondary:art.Primary; img.type=Image.Type.Sliced; img.color=Color.white; }
                var buttonSkin=button.GetComponent<MissingUiButtonSkin>()??button.gameObject.AddComponent<MissingUiButtonSkin>();buttonSkin.DarkText=secondary;
                foreach(var label in button.GetComponentsInChildren<Text>(true))
                { Fill(label.rectTransform,12);label.fontSize=32;label.resizeTextForBestFit=false;label.alignment=TextAnchor.MiddleCenter; }
            }
        }
        private void OnEnable()
        {
            var scroll=GetComponentInChildren<ScrollRect>(true); if(scroll!=null) { scroll.StopMovement();scroll.verticalNormalizedPosition=1; }
        }
    }
}
