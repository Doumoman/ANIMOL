using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.MissingUiV1.Project
{
    public sealed class MissingUtilityToggle:MonoBehaviour
    {
        public Image Track;public RectTransform Knob;public Sprite Off,On;
        private Toggle toggle;
        private void LateUpdate()
        {
            if(toggle==null)toggle=GetComponent<Toggle>();if(Track==null||Knob==null)return;
            Track.sprite=toggle.isOn?On:Off;Knob.anchorMin=Knob.anchorMax=new Vector2(toggle.isOn?.75f:.25f,.5f);
        }
    }
}
