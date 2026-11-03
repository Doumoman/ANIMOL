using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.RanchConnectedDecorV01
{
    /// <summary>
    /// Explicit host-driven STATIC decoration renderer. Call Configure then SetLayout
    /// after an authoritative map/decor edit, load or undo. No autonomous Start/Update,
    /// input, random frame state, colliders, gameplay registrations or turn callbacks.
    /// One renderer per complete multi-cell group. Source pivots and PPU stay intact.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RanchConnectedDecorV01Controller : MonoBehaviour
    {
        [SerializeField] private RanchConnectedDecorV01Catalog catalog;
        [SerializeField] private string expectedManifestSha256;
        [SerializeField] private string sortingLayerName;
        [SerializeField] private List<SpriteRenderer> owned = new List<SpriteRenderer>();
        private bool configured, hasExplicitLayout;
        private RanchDecorPlan currentPlan;
        private string lastError;
        private RanchConnectedDecorV01Catalog.SpriteRecord[] flowerAccents = new RanchConnectedDecorV01Catalog.SpriteRecord[0];
        public string LastDiagnostic { get { return lastError; } }
        public RanchDecorPlan CurrentPlan { get { return currentPlan; } }
        public int OwnedVisualCount { get { return owned == null ? 0 : owned.Count; } }

        // FromRowDownCell(0,0) denotes grid cell(0,0). SetLayout's worldOrigin is
        // that cell's BOTTOM-LEFT. Convert a host board TOP-LEFT before calling it.
        public static Vector3 RowDownTopLeftToWorldOrigin(Vector3 boardTopLeft, float cellSize)
        {
            if (!Finite(boardTopLeft.x) || !Finite(boardTopLeft.y) || !Finite(boardTopLeft.z) || !Finite(cellSize) || cellSize <= 0f)
                throw new ArgumentException("Invalid row-down board origin or cell size.");
            return new Vector3(boardTopLeft.x, boardTopLeft.y - cellSize, boardTopLeft.z);
        }

        private void Awake() { if (!hasExplicitLayout) HideOwned(); }
        private void OnEnable()
        {
            HideOwned();
            string error;
            // Preserve Configure+SetLayout supplied to an inactive pooled object before
            // its first Awake. A mere re-enable never resurrects an older saved layout.
            if (configured && hasExplicitLayout && catalog != null && catalog.ValidateIntegrity(expectedManifestSha256, out error))
                ShowOwned();
            else { configured = false; hasExplicitLayout = false; currentPlan = null; }
        }
        private void OnDisable()
        { configured = false; hasExplicitLayout = false; currentPlan = null; HideOwned(); }
        private void OnDestroy() { ClearOwned(); }

        public bool Configure(RanchConnectedDecorV01Catalog latest, string pinnedManifestSha256, string actualSortingLayerName)
        {
            ClearOwned(); configured = false; hasExplicitLayout = false; currentPlan = null;
            // Persist even a rejected/null NEW configuration; never restore an older one.
            catalog = latest; expectedManifestSha256 = pinnedManifestSha256; sortingLayerName = actualSortingLayerName;
            flowerAccents = new RanchConnectedDecorV01Catalog.SpriteRecord[0];
            if (catalog == null) return Fail("Missing latest decor catalog.");
            catalog.Invalidate(); string error;
            if (!catalog.ValidateIntegrity(expectedManifestSha256, out error)) return Fail(error);
            bool layerExists = false;
            foreach (SortingLayer layer in SortingLayer.layers) if (layer.name == sortingLayerName) layerExists = true;
            if (!layerExists) return Fail("The host's actual decoration Sorting Layer does not exist: " + sortingLayerName);
            var accents = new List<RanchConnectedDecorV01Catalog.SpriteRecord>();
            for (int i = 0; i < catalog.SpriteCount; i++)
            {
                var row = catalog.GetSpriteAt(i);
                if (row.Family == RanchConnectedDecorV01Planner.Meadow && row.Kind == "meadow_flower_accent") accents.Add(row);
            }
            accents.Sort((a, b) => string.CompareOrdinal(a.File, b.File)); flowerAccents = accents.ToArray();
            configured = true; lastError = null; return true;
        }

        // hostAllowDecorCell must be a PURE QUERY, not a per-cell reservation/mutation.
        // Only a successful whole-plan result may be committed by the existing host.
        // The arrays are the WHOLE authoritative decoration board, never a camera crop.
        public bool SetLayout(IList<RanchDecorNetworkCell> networkCells, IList<RanchDecorGroupPlacement> groupPlacements,
            Vector3 worldOrigin, float cellSize, Func<RanchDecorClaim, bool> hostAllowDecorCell,
            int seed = 0, int postSpacing = 4)
        {
            if (!configured || catalog == null) return Fail("Configure the latest catalog before applying the host's decoration snapshot.");
            string error;
            if (!catalog.ValidateIntegrity(expectedManifestSha256, out error)) return Fail(error);
            if (!Finite(worldOrigin.x) || !Finite(worldOrigin.y) || !Finite(worldOrigin.z) || !Finite(cellSize) || cellSize <= 0f)
                return Fail("Invalid explicit world origin or logical cell size.");
            Vector3 parentScale = transform.lossyScale;
            if (Quaternion.Angle(transform.rotation, Quaternion.identity) > .01f
                || !Finite(parentScale.x) || !Finite(parentScale.y) || parentScale.x <= 0 || parentScale.y <= 0)
                return Fail("Decoration root requires an unrotated, non-mirrored XY hierarchy.");
            var pending = new List<PendingSprite>();
            RanchDecorPlan plan;
            try
            {
                plan = RanchConnectedDecorV01Planner.CreatePlan(networkCells, groupPlacements, key =>
                {
                    RanchConnectedDecorV01Catalog.GroupRecord group; RanchConnectedDecorV01Catalog.SpriteRecord art; string reason;
                    if (!catalog.TryGetGroup(key, expectedManifestSha256, out group, out art, out reason)) throw new InvalidOperationException(reason);
                    return new RanchDecorGroupSize(group.WidthCells, group.HeightCells);
                }, hostAllowDecorCell, seed, postSpacing);
                for (int i = 0; i < plan.NetworkCount; i++)
                {
                    RanchDecorNetworkSelection row = plan.GetNetwork(i);
                    AddPending(pending, "Network_" + row.family + "_" + i, row.file,
                        WorldPosition(worldOrigin, row.x + .5, row.y + .5, row.worldZ, cellSize), row.sortingOrder, 1, 1);
                    if (!string.IsNullOrEmpty(row.periodicPostFile))
                        AddPending(pending, "PeriodicPost_" + i, row.periodicPostFile,
                            WorldPosition(worldOrigin, row.x + .5, row.y + .5, row.worldZ, cellSize), row.sortingOrder + 1, 1, 1);
                    if (row.family == RanchConnectedDecorV01Planner.Meadow && flowerAccents.Length != 0
                        && RanchConnectedDecorV01Planner.AddMeadowAccent(row.accentHash))
                    {
                        var accent = flowerAccents[(int)((row.accentHash / 5u) % (uint)flowerAccents.Length)];
                        AddPending(pending, "FlowerAccent_" + i, accent.File,
                            WorldPosition(worldOrigin, row.x + .5, row.y + .5, row.worldZ, cellSize), row.sortingOrder + 1, 1, 1);
                    }
                }
                for (int i = 0; i < plan.GroupCount; i++)
                {
                    RanchDecorGroupSelection row = plan.GetGroup(i);
                    RanchConnectedDecorV01Catalog.GroupRecord group; RanchConnectedDecorV01Catalog.SpriteRecord art; string reason;
                    if (!catalog.TryGetGroup(row.groupKey, expectedManifestSha256, out group, out art, out reason)) throw new InvalidOperationException(reason);
                    AddPending(pending, "Group_" + row.placementId, art.File,
                        WorldPosition(worldOrigin, row.CenterX, row.CenterY, row.worldZ, cellSize), row.sortingOrder, row.widthCells, row.heightCells);
                }
            }
            catch (Exception e) { return Fail("Decoration snapshot rejected atomically: " + e.Message); }

            var next = new List<SpriteRenderer>();
            try
            {
                // Every path/footprint/pivot is resolved before even creating new nodes.
                // New renderers stay disabled until the complete replacement is ready.
                foreach (PendingSprite row in pending)
                {
                    var node = new GameObject("RANCH_DECOR_VISUAL_ONLY__" + row.name);
                    node.transform.SetParent(transform, false); node.transform.position = row.position;
                    node.transform.rotation = Quaternion.identity;
                    // Uniform world cell scale; NEVER divide by canvas/alpha/footprint.
                    node.transform.localScale = new Vector3(cellSize / parentScale.x, cellSize / parentScale.y, 1f);
                    var renderer = node.AddComponent<SpriteRenderer>(); renderer.enabled = false;
                    next.Add(renderer); renderer.sprite = row.art.Sprite; renderer.sortingLayerName = sortingLayerName; renderer.sortingOrder = row.order;
                    if (renderer.sprite == null) throw new InvalidOperationException("Latest sprite unloaded during replacement.");
                }
            }
            catch (Exception e)
            { DestroyRenderers(next); return Fail("Decoration visual construction failed closed: " + e.Message); }
            ClearOwned(); owned = next; currentPlan = plan; hasExplicitLayout = true; lastError = null;
            if (isActiveAndEnabled) ShowOwned(); return true;
        }
        private sealed class PendingSprite
        { public string name; public RanchConnectedDecorV01Catalog.SpriteRecord art; public Vector3 position; public int order; }
        private void AddPending(List<PendingSprite> rows, string name, string file, Vector3 position, int order, int cellsX, int cellsY)
        {
            RanchConnectedDecorV01Catalog.SpriteRecord art; string error;
            if (!catalog.TryGetExact(file, expectedManifestSha256, out art, out error)) throw new InvalidOperationException(error);
            if (art.CellsX != cellsX || art.CellsY != cellsY) throw new InvalidOperationException("Requested art slot differs from current sprite metadata: " + file);
            rows.Add(new PendingSprite { name = name, art = art, position = position, order = order });
        }
        private static Vector3 WorldPosition(Vector3 origin, double centreX, double centreY, float z, float cellSize)
        {
            double x = origin.x + centreX * cellSize, y = origin.y + centreY * cellSize, worldZ = origin.z + z;
            if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y)
                || x > float.MaxValue || x < -float.MaxValue || y > float.MaxValue || y < -float.MaxValue
                || double.IsNaN(worldZ) || double.IsInfinity(worldZ) || worldZ > float.MaxValue || worldZ < -float.MaxValue)
                throw new ArgumentException("Decor world position overflow.");
            return new Vector3((float)x, (float)y, (float)worldZ);
        }
        public void ClearLayout()
        { hasExplicitLayout = false; currentPlan = null; ClearOwned(); }
        private void HideOwned() { if (owned != null) foreach (SpriteRenderer row in owned) if (row != null) row.enabled = false; }
        private void ShowOwned() { if (owned != null) foreach (SpriteRenderer row in owned) if (row != null && row.sprite != null) row.enabled = true; }
        private void ClearOwned()
        { HideOwned(); DestroyRenderers(owned); if (owned == null) owned = new List<SpriteRenderer>(); else owned.Clear(); }
        private static void DestroyRenderers(List<SpriteRenderer> rows)
        {
            if (rows == null) return;
            foreach (SpriteRenderer renderer in rows)
            {
                if (renderer == null) continue; renderer.enabled = false; renderer.sprite = null;
                GameObject node = renderer.gameObject;
                if (Application.isPlaying) UnityEngine.Object.Destroy(node); else UnityEngine.Object.DestroyImmediate(node);
            }
        }
        private bool Fail(string error)
        {
            hasExplicitLayout = false; currentPlan = null; ClearOwned();
            if (lastError != error) Debug.LogError("[Ranch Connected Decor V01] " + error, this);
            lastError = error; return false;
        }
        private static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
    }
}
