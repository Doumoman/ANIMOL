using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ANIMOL.Art.EchoRailCartV01
{
    // Builds a continuous native-textured rail ribbon; sleepers use global arc length, never per-tile resets.
    public sealed class EchoRailTrackViewV02 : MonoBehaviour
    {
        [SerializeField] EchoRailCartArtCatalogV01 catalog;
        [SerializeField] Vector2Int[] orderedCells;
        [SerializeField] bool closed = true;
        [SerializeField] float cellSize = 1;
        [SerializeField] string sortingLayerName = "Default";
        [SerializeField] int sortingOrder = -20;
        GameObject ownedRoot; Mesh ownedMesh; Material ownedMaterial;
        public EchoRailPathV02 Path { get; private set; }
        public float CellSize { get { return cellSize; } }
        public void SetSorting(string layer, int order)
        { sortingLayerName = string.IsNullOrEmpty(layer) ? "Default" : layer; sortingOrder = order; }
        public bool Configure(EchoRailCartArtCatalogV01 value, Vector2Int[] cells, bool loop, float size, out string error)
        {
            catalog = value; orderedCells = cells == null ? null : (Vector2Int[])cells.Clone(); closed = loop; cellSize = size; return Rebuild(out error);
        }
        public bool Rebuild(out string error)
        {
            error = null; EchoRailPathV02 path; Sprite tie, profile;
            if (cellSize <= 0 || float.IsNaN(cellSize) || float.IsInfinity(cellSize)) { error = "Positive finite cell size required."; return false; }
            if (catalog == null || !catalog.Validate(out error)) { if (error == null) error = "Catalog required."; return false; }
            if (!EchoRailPathV02.TryCreate(orderedCells, closed, out path, out error)) return false;
            if (!catalog.TrySprite("rail/sleeper", out tie) || !catalog.TrySprite("rail/material", out profile)) { error = "R2 sleeper/material missing."; return false; }
            if (profile.packed || profile.rect.x != 0 || profile.rect.y != 0 || profile.rect.width != profile.texture.width || profile.rect.height != profile.texture.height)
            { error = "Exclude the native rail material texture from SpriteAtlas packing."; return false; }
            int steps = Mathf.Max(2, Mathf.CeilToInt(path.Length * 64));
            if (steps > 65535) { error = "Split this unusually long art path into smaller views."; return false; }
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) { error = "A compatible sprite shader is required."; return false; }
            Clear(); Path = path;
            ownedRoot = new GameObject("__EchoRailR2_VisualOnly"); ownedRoot.transform.SetParent(transform, false);
            for (int i = 0; i < path.SleeperCount; i++)
            {
                Vector2 p, t; path.Sample(path.SleeperDistance(i), out p, out t);
                var go = new GameObject("Sleeper_" + i); go.transform.SetParent(ownedRoot.transform, false);
                go.transform.localPosition = p * cellSize; go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg - 90);
                go.transform.localScale = Vector3.one * cellSize;
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = tie; sr.sortingLayerName = sortingLayerName; sr.sortingOrder = sortingOrder;
            }
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
            for (int side = -1; side <= 1; side += 2)
            {
                int first = vertices.Count;
                for (int i = 0; i <= steps; i++)
                {
                    float d = path.Length * i / steps; Vector2 p, t; path.Sample(d, out p, out t); Vector2 n = new Vector2(-t.y, t.x);
                    Vector2 center = p + n * (side * EchoRailPathV02.Gauge * .5f);
                    vertices.Add((center - n * (EchoRailPathV02.HeadWidth * .5f)) * cellSize);
                    vertices.Add((center + n * (EchoRailPathV02.HeadWidth * .5f)) * cellSize);
                    uv.Add(new Vector2(d * 2, 0)); uv.Add(new Vector2(d * 2, 1));
                    if (i == 0) continue;
                    int a = first + (i - 1) * 2, b = a + 2;
                    indices.Add(a); indices.Add(b); indices.Add(a + 1); indices.Add(a + 1); indices.Add(b); indices.Add(b + 1);
                }
            }
            ownedMesh = new Mesh { name = "EchoRailR2_NativeTexturedRibbon", indexFormat = IndexFormat.UInt32 };
            ownedMesh.SetVertices(vertices); ownedMesh.SetUVs(0, uv); ownedMesh.SetTriangles(indices, 0); ownedMesh.RecalculateBounds();
            var rail = new GameObject("ContinuousRails"); rail.transform.SetParent(ownedRoot.transform, false);
            rail.AddComponent<MeshFilter>().sharedMesh = ownedMesh;
            ownedMaterial = new Material(shader) { name = "EchoRailR2_NativeMetal", mainTexture = profile.texture };
            var mr = rail.AddComponent<MeshRenderer>(); mr.sharedMaterial = ownedMaterial; mr.sortingLayerName = sortingLayerName; mr.sortingOrder = sortingOrder + 1;
            return true;
        }
        public Vector3 ToWorld(Vector2 localCells) { return transform.TransformPoint(localCells * cellSize); }
        public void Clear()
        {
            if (ownedRoot != null) { ownedRoot.SetActive(false); Release(ownedRoot); }
            if (ownedMesh != null) Release(ownedMesh);
            if (ownedMaterial != null) Release(ownedMaterial);
            ownedRoot = null; ownedMesh = null; ownedMaterial = null; Path = null;
        }
        static void Release(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        void OnEnable() { if (catalog != null && orderedCells != null && orderedCells.Length >= 2) { string error; if (!Rebuild(out error)) Debug.LogError(error, this); } }
        void OnDisable() { Clear(); }
    }
}
