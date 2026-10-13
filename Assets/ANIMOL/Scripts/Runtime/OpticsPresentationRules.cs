using System;
using UnityEngine;
namespace ANIMOL.Presentation
{
    [Serializable] public class MirrorVisualRule {public string mirror,incoming,outgoing;public Sprite corner;}
    public sealed class OpticsPresentationRules : ScriptableObject
    {
        public MirrorVisualRule[] reflections;
        public TextAsset opticsMetadata,motionMetadata;
        // Maps presentation of an already traced reflection; never computes the board's path.
        public MirrorVisualRule Find(string mirror,string incoming)=>Array.Find(reflections,r=>r.mirror==mirror&&r.incoming==incoming);
    }
}
