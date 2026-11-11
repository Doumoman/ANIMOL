using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // File names choose stage positions. These records keep progress attached to the
    // same asset when its file name (and therefore its stage position) changes.
    public sealed class StarwayStageCatalog : ScriptableObject
    {
        public const string AssetPath = "Assets/ANIMOL/Data/Puzzle/Resources/StarwayStageCatalog.asset";
        [Serializable] public sealed class Entry
        {
            public PuzzleMap map;
            public string sourceId, assetName, assetSha256;
        }
        [HideInInspector] public List<Entry> maps = new List<Entry>();

        public static StarwayStageCatalog Current
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayStageCatalog>(AssetPath);
#else
                return Resources.Load<StarwayStageCatalog>("StarwayStageCatalog");
#endif
            }
        }

        public Entry Find(PuzzleMap map) => map ? maps.Find(entry => entry.map == map) : null;
        public PuzzleMap FindSource(string sourceId) => maps.Find(entry => entry.sourceId == sourceId)?.map;

        public static string AssetName(PuzzleMap map)
        {
            if (!map) return null;
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(map);
            if (!string.IsNullOrEmpty(path)) return System.IO.Path.GetFileNameWithoutExtension(path);
#endif
            var catalog = Current;
            return catalog ? catalog.Find(map)?.assetName ?? map.name : map.name;
        }

        public static string SourceId(PuzzleMap map)
        {
            var catalog = Current;
            return catalog ? catalog.Find(map)?.sourceId : null;
        }
    }
}
