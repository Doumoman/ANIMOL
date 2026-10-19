using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using ANIMOL.Presentation;

namespace ANIMOL.Puzzle
{
    [Serializable] public class TheaterBlob
    {
        public int outer, inner;
        public Tile wood, underside, velvet;
    }
    [Serializable] public class TheaterProp
    {
        public string id;
        public Sprite sprite;
    }
    [CreateAssetMenu(menuName = "ANIMOL/Memory Theater Environment Art")]
    public sealed class MemorySkyTheaterArt : ScriptableObject
    {
        public EchoTheaterV02Art v02;
        public TheaterBlob[] blob47;
        public TheaterProp[] props;
        public Sprite far, mid, near;
        public Material compositor;
        public SpriteFrameSequence curtainLeft, curtainRight, footlights;
        public MemorySkyTheaterLayout defaultLayout;
        public TheaterBlob Find(TerrainShape shape) => Array.Find(blob47, p => p.outer == shape.exposure && p.inner == shape.innerCorners);
        public Sprite Prop(string id) => Array.Find(props, p => p.id == id)?.sprite;
    }
    public static class TheaterTopology
    {
        public static TerrainShape Resolve(Func<Vector2Int, bool> occupied, Vector2Int p)
        {
            if (!occupied(p)) return default;
            var n = occupied(p + Vector2Int.up); var e = occupied(p + Vector2Int.right);
            var s = occupied(p + Vector2Int.down); var w = occupied(p + Vector2Int.left);
            int outer = (n ? 0 : 1) | (e ? 0 : 2) | (s ? 0 : 4) | (w ? 0 : 8);
            int inner = n && w && !occupied(p + new Vector2Int(-1, 1)) ? 1 : 0;
            if (n && e && !occupied(p + new Vector2Int(1, 1))) inner |= 2;
            if (s && e && !occupied(p + new Vector2Int(1, -1))) inner |= 4;
            if (s && w && !occupied(p + new Vector2Int(-1, -1))) inner |= 8;
            return new TerrainShape(outer, inner);
        }
        // Image top-left to Unity pivot, relative to the stage's upper-left corner.
        public static Vector2 PropPosition(Vector2 topLeft, Vector2 canvas, Vector2 pivot)
            => new Vector2((topLeft.x + pivot.x * canvas.x) / 80, -(topLeft.y + (1 - pivot.y) * canvas.y) / 80);
    }
}
