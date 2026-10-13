using UnityEngine;
namespace ANIMOL.Presentation
{
    public sealed class DeviceArtQaController : MonoBehaviour
    {
        public DeviceVisualView[] views;
        public Camera qaCamera;
        public DeviceVisualView[] masters;
        public ResolvedCellPreview[] previews;
        public int pageCount=4;
        public int previewPage=3;
        public float guiTop;
        public bool adaptPortraitToolbar;
        public string title="DEVICE ART QA - gameplay is not connected";
        bool paused;
        int page,device,state;
        public void ResetVisuals()
        {
            paused=false;foreach(var v in views)v.ResetVisual();
            if(previews!=null)foreach(var p in previews)if(p)p.Clear();
        }
        void OnGUI()
        {
            float top=adaptPortraitToolbar?guiTop*Mathf.Min(1,Screen.height/1920f):guiTop;
            GUI.BeginGroup(new Rect(0,top,Screen.width,Screen.height-top));
            GUI.Box(new Rect(12,12,480,116),title);
            if(GUI.Button(new Rect(24,42,100,28),paused?"Resume":"Pause")){paused=!paused;foreach(var v in views)v.paused=paused;}
            if(GUI.Button(new Rect(132,42,100,28),"Restart"))foreach(var v in views)v.Present(v.CurrentState,true);
            if(GUI.Button(new Rect(240,42,100,28),"Reset"))ResetVisuals();
            if(GUI.Button(new Rect(348,42,120,28),"Next panel")){page=(page+1)%pageCount;qaCamera.transform.position=new Vector3(page*24,0,-10);}
            GUI.Label(new Rect(24,78,450,40),"All orientations / states. No cost, movement or success simulation.");
            if(masters!=null && masters.Length>0)
            {
                var v=masters[device];
                if(GUI.Button(new Rect(24,134,120,28),"Device "+v.deviceId)){device=(device+1)%masters.Length;state=0;}
                if(GUI.Button(new Rect(152,134,190,28),v.states[state].name))state=(state+1)%v.states.Length;
                if(GUI.Button(new Rect(350,134,120,28),"Preview event")){var s=v.states[state];if(string.IsNullOrEmpty(s.eventName))v.Present(s.name,true);else v.Notify(s.eventName,s.name);page=previewPage;qaCamera.transform.position=new Vector3(page*24,0,-10);}
            }
            GUI.EndGroup();
        }
    }
}
