using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.ProductionV1
{
    public static class ProductionWidgets
    {
        public static readonly Color Ink = new Color32(26,28,44,255);
        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0,1); r.pivot = new Vector2(0,1);
            r.anchoredPosition = new Vector2(x,-y); r.sizeDelta = new Vector2(w,h); return r;
        }
        public static void Stretch(RectTransform r, float left=0, float top=0, float right=0, float bottom=0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(left,bottom); r.offsetMax = new Vector2(-right,-top);
        }
        public static Image Art(Transform p, ProductionCatalog c, string id, float x,float y,float w,float h, bool slice=false)
        {
            var im=Rect(p,id,x,y,w,h).gameObject.AddComponent<Image>();
            im.sprite=id=="UI_Icon_Coin" && c.ApprovedCoin!=null ? c.ApprovedCoin : c.Find(id);
            im.raycastTarget=false; im.preserveAspect=!slice;
            im.type=slice ? Image.Type.Sliced : Image.Type.Simple;
            // Reference frames have 3x native corners at 1080 wide; no corner stretch.
            im.pixelsPerUnitMultiplier=1f/3f;
            return im;
        }
        public static TextMeshProUGUI Text(Transform p, ProductionCatalog c,string name,string value,float x,float y,float w,float h,float size=36,bool center=false)
        {
            var t=Rect(p,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
            t.font=c.Font; t.text=value; t.fontSize=size; t.color=Ink; t.raycastTarget=false;
            t.alignment=center ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
            t.textWrappingMode=TextWrappingModes.Normal; t.overflowMode=TextOverflowModes.Overflow;
            t.lineSpacing=12;
            t.enableAutoSizing=true;t.fontSizeMin=Mathf.Min(size,28);t.fontSizeMax=size;
            return t;
        }
        public static Image Panel(Transform p,ProductionCatalog c,float x,float y,float w,float h) => Art(p,c,"UI_Common_Panel_Main",x,y,w,h,true);
        public static Button Button(Transform p, ProductionCatalog c,string name,string label,float x,float y,float w,float h,Action action,bool primary=false)
        {
            var im=Art(p,c,primary?"UI_Common_Button_Primary":"UI_Common_Button_Secondary",x,y,w,h,true);
            im.name="PV1_"+name; im.raycastTarget=true;
            var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;
            if(action!=null)b.onClick.AddListener(()=>action());
            var t=Text(im.transform,c,"Label",label,20,8,w-40,h-16,40,true);if(primary)t.color=Color.white;
            var colors=b.colors;colors.disabledColor=new Color(.65f,.65f,.65f,1);b.colors=colors;
            return b;
        }
        public static ScrollRect Scroll(Transform p,string name,float top,float bottom,float contentHeight)
        {
            var r=Rect(p,name,0,0,1080,contentHeight);Stretch(r,0,top,0,bottom);
            var im=r.gameObject.AddComponent<Image>();im.color=Color.clear;im.raycastTarget=true;
            r.gameObject.AddComponent<RectMask2D>();
            var s=r.gameObject.AddComponent<ScrollRect>();s.horizontal=false;s.movementType=ScrollRect.MovementType.Clamped;s.scrollSensitivity=60;
            s.viewport=r;s.content=Rect(r,"Content",0,0,1080,contentHeight);
            return s;
        }
    }
}
