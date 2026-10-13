using ANIMOL.UI;
using UnityEngine;
using UnityEngine.UI;
using static ANIMOL.MissingUiV1.Project.MissingUiLayout;

namespace ANIMOL.MissingUiV1.Project
{
    public sealed class MissingControlPreview:MonoBehaviour
    {
        public RectTransform ScreenWindow,ReferenceRoot;
        public MobileControlLayoutApplier Layout;
        private Vector2 previousSize;
        public static MissingControlPreview Build(Transform parent,MissingUtilityArt art)
        {
            var root=Node("CurrentHudPreview",parent);Height(root,720);
            var preview=root.gameObject.AddComponent<MissingControlPreview>();
            var device=Node("Device",root);device.anchorMin=device.anchorMax=new Vector2(.5f,.5f);device.sizeDelta=new Vector2(480,720);Picture(device,art.Device);
            var area=Node("InnerScreen",device);Rect(area,new Vector2(26f/160,18f/240),new Vector2(134f/160,222f/240),Vector2.zero,Vector2.zero);
            var screen=Node("PortraitViewport",area);Fill(screen);
            var fitter=screen.gameObject.AddComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fitter.aspectRatio=1080f/1920;
            preview.ScreenWindow=screen;
            var reference=Node("CurrentHudVisuals",screen);reference.anchorMin=reference.anchorMax=reference.pivot=new Vector2(.5f,.5f);reference.sizeDelta=MobileControlLayoutApplier.ReferenceSize;
            preview.ReferenceRoot=reference;
            foreach(var source in art.HudSources)
            {
                var rect=Node(source.name,reference);var image=Picture(rect,source.sprite,source.type==Image.Type.Sliced);image.color=source.color;image.raycastTarget=false;
                var original=source.GetComponentInChildren<Text>(true);
                if(original!=null){var label=Label("Label",rect,original.text,original.fontSize,art.Common);Fill(label.rectTransform,8);label.alignment=TextAnchor.MiddleCenter;}
            }
            preview.Layout=reference.gameObject.AddComponent<MobileControlLayoutApplier>();
            return preview;
        }
        private void LateUpdate()
        {
            if(ScreenWindow==null||ReferenceRoot==null)return;
            var size=ScreenWindow.rect.size;
            ReferenceRoot.localScale=Vector3.one*Mathf.Min(size.x/MobileControlLayoutApplier.ReferenceSize.x,size.y/MobileControlLayoutApplier.ReferenceSize.y);
            if(previousSize!=size){previousSize=size;Layout.Apply();}
        }
        public void Apply()=>Layout.Apply();
    }
}
