using System;
using System.Collections.Generic;
using System.Linq;
using Animol.TerrainStructure;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [Serializable] public sealed class FreeShapeStyleArt
    {
        public string styleId, themeId, themeName, displayName;
        public Sprite[] cells; // v0..v3, each in canonical catalog index order.
        public Sprite motif;
    }
    [Serializable] public sealed class FreeShapeFixture { public string id; public string[] rows; public Vector2Int origin; }
    public sealed class FreeShapeArtRegistry : ScriptableObject
    {
        public string contractId, sourceHash;
        public int version=1;
        public int[] canonicalMasks,rawToCanonical,rawToIndex;
        public FreeShapeStyleArt[] styles;
        public FreeShapeFixture[] fixtures;
        public Material material;
        private Dictionary<string,FreeShapeStyleArt> lookup;
        public static FreeShapeArtRegistry Load()=>Resources.Load<FreeShapeArtRegistry>("ANIMOL_FreeShapeArt")??throw new InvalidOperationException("ANIMOL/Terrain Free Shape/Initialize Package를 먼저 실행하세요.");
        public FreeShapeStyleArt Style(string id)
        {if(lookup==null)lookup=styles.ToDictionary(s=>s.styleId,StringComparer.Ordinal);return lookup.TryGetValue(id,out var s)?s:throw new InvalidOperationException("Missing free-shape style: "+id);}
        public Sprite Cell(string style,int raw,int variant)
        {if(contractId!=FreeShapeLayer.Contract||version!=1)throw new InvalidOperationException("Unsupported art registry.");return Style(style).cells[variant*47+rawToIndex[raw]]??throw new InvalidOperationException("Missing free-shape Sprite.");}
        private void OnEnable(){lookup=null;}
    }
}
