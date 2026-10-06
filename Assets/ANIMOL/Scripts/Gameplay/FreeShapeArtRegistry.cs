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
        private bool validated;
        public static FreeShapeArtRegistry Load()=>Load(FreeShapeLayer.Contract,1);
        public static FreeShapeArtRegistry Load(FreeShapeLayer layer)=>Load(layer?.artContract??FreeShapeLayer.Contract,layer?.artVersion??1);
        public static FreeShapeArtRegistry Load(string contract,int artVersion)
        {
            if(contract!=FreeShapeLayer.Contract || !FreeShapeLayer.SupportsArtVersion(artVersion))
                throw new InvalidOperationException("Unsupported free-shape art contract/version: "+contract+" / "+artVersion);
            var registry=Resources.Load<FreeShapeArtRegistry>(artVersion==1?"ANIMOL_FreeShapeArt":"ANIMOL_FreeShapeArtV2");
            if(registry==null || registry.contractId!=contract || registry.version!=artVersion)
                throw new InvalidOperationException("Missing free-shape art v"+artVersion+". Run its Terrain Free Shape initialization menu.");
            if(!registry.validated)registry.ValidateComplete();
            return registry;
        }
        public void ValidateComplete()
        {
            validated=false;lookup=null;
            if(contractId!=FreeShapeLayer.Contract || !FreeShapeLayer.SupportsArtVersion(version) || material==null ||
                canonicalMasks?.Length!=47 || rawToCanonical?.Length!=256 || rawToIndex?.Length!=256 ||
                canonicalMasks.Distinct().Count()!=47 || styles?.Length!=20 || fixtures==null || fixtures.Length==0)
                throw new InvalidOperationException("Incomplete free-shape registry v"+version);
            for(int raw=0;raw<256;raw++)
                if(rawToCanonical[raw]!=FreeShapeTopology.Canonical(raw) || rawToIndex[raw]<0 || rawToIndex[raw]>=47 || canonicalMasks[rawToIndex[raw]]!=rawToCanonical[raw])
                    throw new InvalidOperationException("Invalid free-shape topology mapping: "+raw);
            var ids=new HashSet<string>();var sprites=new HashSet<Sprite>();
            foreach(var s in styles)
            {
                if(s==null || !ids.Add(s.styleId) || s.cells?.Length!=188 || s.motif==null || !sprites.Add(s.motif))
                    throw new InvalidOperationException("Missing/duplicate free-shape style or motif.");
                foreach(var cell in s.cells)if(cell==null || !sprites.Add(cell))throw new InvalidOperationException("Missing/duplicate cell Sprite: "+s.styleId);
            }
            for(int theme=1;theme<=5;theme++)foreach(char style in "ABCD")
                if(!ids.Contains("T"+theme.ToString("D2")+"_"+style))throw new InvalidOperationException("Missing required free-shape style.");
            validated=true;
        }
        public FreeShapeStyleArt Style(string id)
        {if(lookup==null)lookup=styles.ToDictionary(s=>s.styleId,StringComparer.Ordinal);return lookup.TryGetValue(id,out var s)?s:throw new InvalidOperationException("Missing free-shape style: "+id);}
        public Sprite Cell(string style,int raw,int variant)
        {if(contractId!=FreeShapeLayer.Contract||!FreeShapeLayer.SupportsArtVersion(version))throw new InvalidOperationException("Unsupported art registry.");return Style(style).cells[variant*47+rawToIndex[raw]]??throw new InvalidOperationException("Missing free-shape Sprite.");}
        private void OnEnable(){lookup=null;validated=false;}
        private void OnValidate(){lookup=null;validated=false;}
    }
}
