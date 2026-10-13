using UnityEngine;
namespace ANIMOL.Presentation
{
    public sealed class DeviceArtQaController : MonoBehaviour
    {
        public DeviceVisualView[] views;
        public Camera qaCamera;
        public DeviceVisualView[] masters;
        bool paused;
        int page,device,state;
        void OnGUI()
        {
            GUI.Box(new Rect(12,12,480,116),"DEVICE ART QA - gameplay is not connected");
            if(GUI.Button(new Rect(24,42,100,28),paused?"Resume":"Pause")){paused=!paused;foreach(var v in views)v.paused=paused;}
            if(GUI.Button(new Rect(132,42,100,28),"Restart"))foreach(var v in views)v.Present(v.CurrentState,true);
            if(GUI.Button(new Rect(240,42,100,28),"Reset")){paused=false;foreach(var v in views)v.ResetVisual();}
            if(GUI.Button(new Rect(348,42,120,28),"Next panel")){page=(page+1)%4;qaCamera.transform.position=new Vector3(page*24,0,-10);}
            GUI.Label(new Rect(24,78,450,40),"All orientations / states. No cost, movement or success simulation.");
            if(masters!=null && masters.Length>0)
            {
                var v=masters[device];
                if(GUI.Button(new Rect(24,134,120,28),"Device "+v.deviceId)){device=(device+1)%masters.Length;state=0;}
                if(GUI.Button(new Rect(152,134,190,28),v.states[state].name))state=(state+1)%v.states.Length;
                if(GUI.Button(new Rect(350,134,120,28),"Preview event")){var s=v.states[state];if(string.IsNullOrEmpty(s.eventName))v.Present(s.name,true);else v.Notify(s.eventName,s.name);qaCamera.transform.position=new Vector3(72,0,-10);page=3;}
            }
        }
    }
}
