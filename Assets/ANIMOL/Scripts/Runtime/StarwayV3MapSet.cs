using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    [CreateAssetMenu(menuName = "ANIMOL/Starway V3 Map Set")]
    public sealed class StarwayV3MapSet : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string sourceId;
            public string stageName;
            public PuzzleMap map;
            public TextAsset trace;
            public TextAsset certificate;
        }

        public List<Entry> maps = new List<Entry>();
    }
}
