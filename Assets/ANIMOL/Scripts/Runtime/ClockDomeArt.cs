using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using ANIMOL.Presentation;

namespace ANIMOL.Puzzle
{
    [Serializable] public class ClockDomeBlob
    {
        public int outer, inner;
        public Tile floor, underside;
    }
    [CreateAssetMenu(menuName = "ANIMOL/Clock Dome Environment Art")]
    public sealed class ClockDomeArt : ScriptableObject
    {
        public ClockDomeBlob[] blob47;
        public TheaterProp[] parts;
        public Sprite far, mid, near;
        public Material compositor;
        public SpriteFrameSequence pendulum;
        public GameObject roomPrefab, telescopePrefab;
        public Vector3 parallax = new Vector3(.15f, .30f, .45f);
        public ClockDomeBlob Find(TerrainShape shape) => Array.Find(blob47, p => p.outer == shape.exposure && p.inner == shape.innerCorners);
        public Sprite Part(string id) => Array.Find(parts, p => p.id == id)?.sprite;
    }
}
