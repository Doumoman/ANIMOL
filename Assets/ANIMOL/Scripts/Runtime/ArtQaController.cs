using UnityEngine;
namespace ANIMOL.Presentation
{
    // QA controls affect presentation only. This is not a playable puzzle.
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ANIMOL.StarwayV04", "ANIMOL.StarwayV04", "StarwayArtQa")]
    public sealed class ArtQaController : MonoBehaviour
    {
        public GameObject collectionFx;
        bool paused;
        void OnGUI()
        {
            float scale=Screen.width/1080f;var previous=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            var style=new GUIStyle(GUI.skin.label){fontSize=24};
            GUI.Label(new Rect(30,185,1020,40),"ART QA - puzzle state not connected",style);
            if(GUI.Button(new Rect(30,235,240,65),"Touch foliage"))foreach(var v in FindObjectsByType<SpriteFramePlayer>(FindObjectsSortMode.None))v.PlayTouch();
            if(GUI.Button(new Rect(300,235,240,65),paused?"Resume visuals":"Pause visuals"))
            {paused=!paused;foreach(var v in FindObjectsByType<SpriteFramePlayer>(FindObjectsSortMode.None))v.paused=paused;}
            if(GUI.Button(new Rect(570,235,240,65),"Reset visuals"))foreach(var v in FindObjectsByType<SpriteFramePlayer>(FindObjectsSortMode.None))v.ResetVisual();
            if(GUI.Button(new Rect(840,235,210,65),"Collect FX") && collectionFx){collectionFx.SetActive(true);collectionFx.GetComponent<SpriteFramePlayer>().PlayTouch();}
            GUI.matrix=previous;
        }
    }
}
