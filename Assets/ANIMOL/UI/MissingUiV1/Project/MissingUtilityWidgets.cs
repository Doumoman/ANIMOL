using UnityEngine;
using UnityEngine.UI;
using static ANIMOL.MissingUiV1.Project.MissingUiLayout;

namespace ANIMOL.MissingUiV1.Project
{
    public static class MissingUtilityWidgets
    {
        public static MissingUtilityText Words(Transform parent,string name,string ko,string en,MissingUtilityArt art,int size=32,bool flowing=true)
        {
            var label=Label(name,parent,ko,size,art.Common,flowing);
            var text=label.gameObject.AddComponent<MissingUtilityText>();
            text.Korean=ko;text.English=en;text.BaseSize=size;label.color=Ink;
            return text;
        }
        public static Button Action(Transform parent,string name,string ko,string en,MissingUtilityArt art,bool primary=false)
        {
            var button=Button(name,parent,ko,art.Common,primary);
            var label=button.GetComponentInChildren<Text>(true);
            var text=label.gameObject.AddComponent<MissingUtilityText>();text.Korean=ko;text.English=en;
            text.TextColor=primary?White:Ink;
            return button;
        }
        public static RectTransform Row(Transform parent,string name,string ko,string en,Sprite icon,MissingUtilityArt art)
        {
            var card=Node(name,parent);Picture(card,art.Common.Row,true);Vertical(card,32,20);
            var heading=Node("Heading",card);Height(heading,80);
            var image=Node("Icon",heading);Rect(image,Vector2.zero,Vector2.up,Vector2.zero,new Vector2(80,0));Picture(image,icon);
            var title=Words(heading,"HeadingText",ko,en,art,32,false);
            Rect((RectTransform)title.transform,Vector2.zero,Vector2.one,new Vector2(104,0),Vector2.zero);
            title.GetComponent<Text>().alignment=TextAnchor.MiddleLeft;
            return card;
        }
        public static Slider Slider(Transform parent,string name,MissingUtilityArt art,float min,float max,bool available=true)
        {
            var touch=Node(name,parent);Height(touch,112);
            var hit=Picture(touch,null);hit.color=Color.clear;hit.raycastTarget=true;
            var slider=touch.gameObject.AddComponent<Slider>();slider.minValue=min;slider.maxValue=max;
            var rail=Node("Rail",touch);Rect(rail,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(32,-8),new Vector2(-32,8));Picture(rail,art.SliderRail,true);
            var fillArea=Node("FillArea",touch);Rect(fillArea,Vector2.zero,Vector2.one,new Vector2(32,48),new Vector2(-32,-48));
            var fill=Node("Fill",fillArea);Fill(fill);Picture(fill,art.SliderFill,true);slider.fillRect=fill;
            var handleArea=Node("HandleArea",touch);Fill(handleArea);handleArea.offsetMin=new Vector2(32,0);handleArea.offsetMax=new Vector2(-32,0);
            var handle=Node("Handle",handleArea);handle.sizeDelta=new Vector2(64,104);var handleImage=Picture(handle,art.SliderThumb);handleImage.raycastTarget=true;
            slider.handleRect=handle;slider.targetGraphic=handleImage;slider.interactable=available;
            if(!available){handle.gameObject.SetActive(false);fillArea.gameObject.SetActive(false);}
            return slider;
        }
        public static Toggle Toggle(Transform parent,string name,MissingUtilityArt art,bool available=true)
        {
            var touch=Node(name,parent);Height(touch,112);var hit=Picture(touch,null);hit.color=Color.clear;hit.raycastTarget=true;
            var toggle=touch.gameObject.AddComponent<Toggle>();toggle.transition=Selectable.Transition.None;toggle.graphic=null;
            var track=Node("Track",touch);track.anchorMin=track.anchorMax=new Vector2(.5f,.5f);track.sizeDelta=new Vector2(160,80);var image=Picture(track,art.ToggleOff);
            var knob=Node("Knob",track);knob.anchorMin=knob.anchorMax=new Vector2(.25f,.5f);knob.sizeDelta=new Vector2(56,56);Picture(knob,art.ToggleKnob);
            var visual=touch.gameObject.AddComponent<MissingUtilityToggle>();visual.Track=image;visual.Knob=knob;visual.Off=art.ToggleOff;visual.On=art.ToggleOn;
            toggle.targetGraphic=hit;toggle.interactable=available;
            if(!available)track.gameObject.SetActive(false); // Unknown is not an OFF value.
            return toggle;
        }
    }
}
