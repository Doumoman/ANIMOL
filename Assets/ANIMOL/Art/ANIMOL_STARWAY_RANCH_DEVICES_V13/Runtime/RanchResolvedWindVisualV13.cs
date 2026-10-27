using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.RanchDevicesV13
{
    [Serializable] public struct RanchWindCellV13 { public Vector3 center; public string direction; }
    // Input is the host's actual unobstructed wind path. This renderer NEVER calculates wind physics.
    public sealed class RanchResolvedWindVisualV13 : MonoBehaviour
    {
        readonly List<GameObject> owned = new List<GameObject>();
        static bool Direction(string d) { return d == "south" || d == "east" || d == "north" || d == "west"; }
        public bool SetResolvedPath(RanchDevicesV13Catalog catalog, RanchWindCellV13[] resolved, int range, float cellSize, int sortingLayerID, int sortingOrder, out string error)
        {
            error = null;
            if (catalog == null || !catalog.Validate(out error)) { ClearOwned(); error = error ?? "Catalog required."; return false; }
            if (resolved == null || (range != 0 && range != 3 && range != 6 && range != 9) || resolved.Length > range || !RanchDevicesV13Catalog.Finite(cellSize) || cellSize <= 0)
            { ClearOwned(); error = "Resolved wind path must fit exact range 0/3/6/9."; return false; }
            var positions = new HashSet<Vector3>();
            for (int i = 0; i < resolved.Length; i++)
            {
                var c = resolved[i];
                if (!Direction(c.direction) || !positions.Add(c.center) || !RanchDevicesV13Catalog.Finite(c.center.x) || !RanchDevicesV13Catalog.Finite(c.center.y) || !RanchDevicesV13Catalog.Finite(c.center.z))
                { ClearOwned(); error = "Invalid/duplicate wind path cell."; return false; }
                if (i > 0)
                {
                    var delta = c.center - resolved[i - 1].center;
                    if (Mathf.Abs(delta.z) > .0001f || Mathf.Abs(Mathf.Abs(delta.x) + Mathf.Abs(delta.y) - cellSize) > .001f || (Mathf.Abs(delta.x) > .001f && Mathf.Abs(delta.y) > .001f))
                    { ClearOwned(); error = "Wind cells must form a contiguous cardinal route."; return false; }
                }
            }
            ClearOwned();
            foreach (var c in resolved)
            {
                var go = new GameObject("OwnedWindV13"); go.transform.SetParent(transform, false); go.transform.position = c.center;
                // Detached visual root: preserve world cell scale under a scaled caller without changing caller transforms.
                go.transform.rotation = Quaternion.identity;
                var scale = go.transform.lossyScale;
                if (scale.x <= 0 || scale.y <= 0 || Mathf.Abs(scale.x - scale.y) > .0001f || transform.rotation != Quaternion.identity)
                { DestroyOwned(go); ClearOwned(); error = "Use an unrotated positive uniform visual parent."; return false; }
                go.transform.localScale = new Vector3(cellSize / scale.x, cellSize / scale.y, 1);
                var renderer = go.AddComponent<SpriteRenderer>(); renderer.sortingLayerID = sortingLayerID; renderer.sortingOrder = sortingOrder;
                var view = go.AddComponent<RanchDeviceVisualV13>();
                owned.Add(go);
                if (!view.Configure(catalog, out error)) { ClearOwned(); return false; }
                string state = owned.Count == resolved.Length ? "terminal" : "flow";
                if (!view.SetState("wind_field/" + c.direction + "/" + state)) { ClearOwned(); error = "Missing wind visual key."; return false; }
            }
            return true;
        }
        void OnDisable() { ClearOwned(); }
        public void ClearOwned() { foreach (var go in owned) if (go != null) DestroyOwned(go); owned.Clear(); }
        static void DestroyOwned(GameObject go) { go.SetActive(false); if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
    }
}
