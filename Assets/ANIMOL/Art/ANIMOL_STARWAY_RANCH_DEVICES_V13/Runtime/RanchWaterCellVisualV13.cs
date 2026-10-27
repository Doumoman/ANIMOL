using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.RanchDevicesV13
{
    public sealed class RanchWaterCellVisualV13 : MonoBehaviour
    {
        readonly List<GameObject> owned = new List<GameObject>();
        // neighborMask is same-state connected channel neighbors: N1 NE2 E4 SE8 S16 SW32 W64 NW128.
        // The host supplies this after its atomic water-state decision. No collider/occupancy is inferred here.
        public bool Apply(RanchDevicesV13Catalog catalog, bool wet, int neighborMask, float cellSize, int layer, int order, out string error)
        {
            error = null; ClearOwned();
            if (catalog == null || !catalog.Validate(out error)) { error = error ?? "Catalog required."; return false; }
            if (neighborMask < 0 || neighborMask > 255 || !RanchDevicesV13Catalog.Finite(cellSize) || cellSize <= 0 || transform.rotation != Quaternion.identity)
            { error = "Invalid water visual cell."; return false; }
            var scale = transform.lossyScale;
            if (scale.x <= 0 || scale.y <= 0 || Mathf.Abs(scale.x - scale.y) > .0001f) { error = "Use a positive uniform parent."; return false; }
            var surface = Make("WaterSurface", cellSize, scale.x, layer, order);
            var view = surface.AddComponent<RanchDeviceVisualV13>();
            if (!view.Configure(catalog, out error) || !view.SetState(wet ? "water/wet" : "water/dry")) { ClearOwned(); return false; }
            string[] edges = { "n", "e", "s", "w" }; int[] bits = { 1, 4, 16, 64 };
            for (int i = 0; i < 4; i++) if ((neighborMask & bits[i]) == 0 && !Add(catalog, "water/bank/edge_" + edges[i], cellSize, scale.x, layer, order + 1, out error)) return false;
            string[] corners = { "ne", "se", "sw", "nw" }; int[] diagonals = { 2, 8, 32, 128 };
            for (int i = 0; i < 4; i++)
            {
                bool a = (neighborMask & bits[i]) != 0, b = (neighborMask & bits[(i + 1) % 4]) != 0;
                string kind = !a && !b ? "outer" : a && b && (neighborMask & diagonals[i]) == 0 ? "inner" : null;
                if (kind != null && !Add(catalog, "water/bank/" + kind + "_" + corners[i], cellSize, scale.x, layer, order + 2, out error)) return false;
            }
            return true;
        }
        bool Add(RanchDevicesV13Catalog catalog, string key, float size, float parentScale, int layer, int order, out string error)
        {
            Sprite sprite; error = null;
            if (!catalog.TrySprite(key, out sprite)) { ClearOwned(); error = "Missing water part " + key; return false; }
            Make(key, size, parentScale, layer, order).GetComponent<SpriteRenderer>().sprite = sprite; return true;
        }
        GameObject Make(string name, float size, float parentScale, int layer, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.localScale = new Vector3(size / parentScale, size / parentScale, 1);
            var r = go.AddComponent<SpriteRenderer>(); r.sortingLayerID = layer; r.sortingOrder = order; owned.Add(go); return go;
        }
        void OnDisable() { ClearOwned(); }
        void ClearOwned() { foreach (var go in owned) if (go != null) { go.SetActive(false); if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); } owned.Clear(); }
    }
}
