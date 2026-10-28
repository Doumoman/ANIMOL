using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    [CreateAssetMenu(menuName = "ANIMOL/Echo Theater Map Set")]
    public sealed class EchoTheaterMapSet : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string stageId;
            public PuzzleMap map;
        }
        public List<Entry> maps = new List<Entry>();
    }
}
