using System;
using UnityEngine;
namespace ANIMOL.Presentation
{
    [Serializable] public class DiceFaces {public int top,bottom,north,south,east,west;}
    [Serializable] public class DiceTransition {public string direction,toOrientation,animation;public int top;}
    [Serializable] public class DiceOrientation {public string id,staticSprite;public DiceFaces faces;public DiceTransition[] transitions;}
    public sealed class DiceOrientationTable : ScriptableObject
    {
        public string initialOrientation;
        public DiceFaces initialFaces;
        public DiceOrientation[] orientations;
        public DiceOrientation Find(string id)=>Array.Find(orientations,o=>o.id==id);
        public DiceOrientation FindFaces(int top,int bottom,int north,int south,int east,int west)=>Array.Find(orientations,o=>o.faces.top==top&&o.faces.bottom==bottom&&o.faces.north==north&&o.faces.south==south&&o.faces.east==east&&o.faces.west==west);
        // Caller supplies already committed from/to states; this lookup never rolls logical state.
        public bool PresentCommittedRoll(DeviceVisualView view,string actionId,string from,string to,string direction)
        {
            var orientation=Find(from);if(orientation==null)return false;
            var transition=Array.Find(orientation.transitions,t=>t.direction==direction&&t.toOrientation==to);
            return transition!=null&&view.Notify(actionId,"actual_one_cell_die_roll",transition.animation.Split('/')[1]);
        }
    }
}
