using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace ANIMOL.StarwayV04
{
    // Bind only to an authoritative puzzle state. Unbound art QA never invents COST 24 or 0/3.
    public sealed class StarwayHud : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public TMP_Text cost, bubbles;
        public Image pause;
        public Sprite normal, press, release, disabled;
        public RectTransform safeArea;
        public Action PauseRequested;
        bool bound;
        Rect lastArea;
        Vector2 lastScreen;
        public void Present(int remainingCost,int collected,int target,bool canPause)
        {
            bound=canPause;cost.text="COST "+remainingCost;bubbles.text=collected+"/"+target;
            pause.sprite=canPause?normal:disabled;
        }
        public void Unbind() { bound=false;PauseRequested=null;cost.text="COST --";bubbles.text="--/--";pause.sprite=disabled; }
        void Start() { Unbind();ApplySafeArea(Screen.safeArea,new Vector2(Screen.width,Screen.height)); }
        void Update() {var screen=new Vector2(Screen.width,Screen.height);if(lastArea!=Screen.safeArea || lastScreen!=screen)ApplySafeArea(Screen.safeArea,screen);}
        public void ApplySafeArea(Rect area,Vector2 screen)
        {lastArea=area;lastScreen=screen;safeArea.anchorMin=area.min/screen;safeArea.anchorMax=area.max/screen;safeArea.offsetMin=safeArea.offsetMax=Vector2.zero;}
        public void OnPointerDown(PointerEventData data) {if(bound)pause.sprite=press;}
        public void OnPointerUp(PointerEventData data) {if(!bound)return;pause.sprite=release;PauseRequested?.Invoke();}
        public void OnPointerExit(PointerEventData data) {if(bound)pause.sprite=normal;}
    }
}
