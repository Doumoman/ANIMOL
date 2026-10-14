using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using ANIMOL.Presentation;

namespace ANIMOL.Puzzle
{
    [Serializable] public class CoastBlob
    {
        public int outer, inner;
        public Tile sand, wetSandBody;
    }
    [CreateAssetMenu(menuName = "ANIMOL/Twilight Coast Environment Art")]
    public sealed class TwilightCoastArt : ScriptableObject
    {
        public CoastBlob[] blob47;
        public TheaterProp[] props;
        public Sprite far, mid, near, horizonCore, horizonWithoutSun, horizonLeft, horizonRight;
        public Material compositor;
        public SpriteFrameSequence palmIdle, palmTouch, waterRipples;
        public GameObject horizonPrefab, palmPrefab, rocksPrefab, fencePrefab;
        public Vector3 parallax = new Vector3(.12f, .30f, .50f);
        public CoastBlob Find(TerrainShape shape) => Array.Find(blob47, p => p.outer == shape.exposure && p.inner == shape.innerCorners);
        public Sprite Prop(string id) => Array.Find(props, p => p.id == id)?.sprite;
    }
}
