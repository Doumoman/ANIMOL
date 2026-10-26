using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    [Serializable]
    public sealed class SanctumDecorAllowedCell
    {
        public int x, y, logicalLayer, heightKey;
        public SanctumDecorAllowedCell() { }
        public SanctumDecorAllowedCell(int x, int y, int logicalLayer = 0, int heightKey = 0)
        { this.x = x; this.y = y; this.logicalLayer = logicalLayer; this.heightKey = heightKey; }
    }

    /// <summary>Flat, explicitly approved WHOLE-board decoration data. No gameplay state or jagged containers.</summary>
    [Serializable]
    public sealed class SanctumConnectedDecorV01BoardSnapshot
    {
        public const string ArtManifestSha256 = "db4fc32203d547866b9cecf72f17a5919ba261ed872f5b24479eb98b5f5970f8";
        public int schemaVersion = 1;
        public string packageId, sourceManifestSha256, boardId;
        public long revision;
        public bool isWholeBoard, isExampleOnly;
        public float originX, originY, originZ, cellSize;
        public int seed, postSpacing;
        // Intentionally no empty initializers: missing arrays fail; explicitly empty arrays can clear a board.
        public SanctumDecorNetworkCell[] networkCells;
        public SanctumDecorGroupPlacement[] groupPlacements;
        public SanctumDecorAllowedCell[] allowedDecorCells;

        private struct AllowedKey : IEquatable<AllowedKey>
        {
            private readonly int x, y, layer, height;
            public AllowedKey(int x, int y, int layer, int height)
            { this.x = x; this.y = y; this.layer = layer; this.height = height; }
            public bool Equals(AllowedKey other) { return x == other.x && y == other.y && layer == other.layer && height == other.height; }
            public override bool Equals(object other) { return other is AllowedKey && Equals((AllowedKey)other); }
            public override int GetHashCode() { unchecked { return (((x * 397) ^ y) * 397 ^ layer) * 397 ^ height; } }
        }
        public bool TryValidate(out string error)
        {
            error = null;
            if (schemaVersion != 1 || packageId != SanctumConnectedDecorV01Catalog.PackageId
                || !string.Equals(sourceManifestSha256, ArtManifestSha256, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(boardId) || revision < 0 || !isWholeBoard)
                return Reject("Snapshot requires the exact package/art pin, nonnegative revision, board identity and whole-board flag.", out error);
            if (!Finite(originX) || !Finite(originY) || !Finite(originZ) || !Finite(cellSize) || cellSize <= 0
                || (postSpacing != 3 && postSpacing != 4) || networkCells == null || groupPlacements == null || allowedDecorCells == null)
                return Reject("Missing snapshot arrays or invalid origin/cellSize/postSpacing.", out error);
            try
            {
                Func<SanctumDecorClaim, bool> allowed = CreateApprovedCellQuery();
                SanctumDecorPlan plan = SanctumConnectedDecorV01Planner.CreatePlan(networkCells, groupPlacements, key =>
                {
                    SanctumDecorGroupSize size;
                    if (!SanctumConnectedDecorV01Planner.TryGetDeclaredGroupSize(key, out size)) throw new ArgumentException("Unknown current group: " + key);
                    return size;
                }, allowed, seed, postSpacing);
                for (int i = 0; i < plan.NetworkCount; i++)
                { var row = plan.GetNetwork(i); RequireWorldPosition(row.x + .5, row.y + .5, row.worldZ); }
                for (int i = 0; i < plan.GroupCount; i++)
                { var row = plan.GetGroup(i); RequireWorldPosition(row.CenterX, row.CenterY, row.worldZ); }
                return true;
            }
            catch (Exception exception) { return Reject("Invalid whole decoration snapshot: " + exception.Message, out error); }
        }
        /// <summary>Captures a private immutable-by-convention whitelist. Subsequent DTO edits cannot change this query.</summary>
        public Func<SanctumDecorClaim, bool> CreateApprovedCellQuery()
        {
            if (allowedDecorCells == null) throw new ArgumentException("Snapshot has no explicit approved decoration cells.");
            var keys = new HashSet<AllowedKey>();
            foreach (SanctumDecorAllowedCell cell in allowedDecorCells)
            {
                if (cell == null || !keys.Add(new AllowedKey(cell.x, cell.y, cell.logicalLayer, cell.heightKey)))
                    throw new ArgumentException("Missing or duplicate approved decoration cell/plane.");
            }
            return claim => keys.Contains(new AllowedKey(claim.cell.x, claim.cell.y, claim.logicalLayer, claim.heightKey));
        }
        public SanctumConnectedDecorV01BoardSnapshot DeepCopy()
        {
            var copy = new SanctumConnectedDecorV01BoardSnapshot {
                schemaVersion = schemaVersion, packageId = packageId, sourceManifestSha256 = sourceManifestSha256,
                boardId = boardId, revision = revision, isWholeBoard = isWholeBoard, isExampleOnly = isExampleOnly,
                originX = originX, originY = originY, originZ = originZ, cellSize = cellSize, seed = seed, postSpacing = postSpacing
            };
            if (networkCells != null)
            {
                copy.networkCells = new SanctumDecorNetworkCell[networkCells.Length];
                for (int i = 0; i < networkCells.Length; i++)
                { var row = networkCells[i]; if (row != null) copy.networkCells[i] = new SanctumDecorNetworkCell(row.x, row.y, row.family,
                    row.connectionGroup, row.logicalLayer, row.heightKey, row.sortingOrder, row.worldZ); }
            }
            if (groupPlacements != null)
            {
                copy.groupPlacements = new SanctumDecorGroupPlacement[groupPlacements.Length];
                for (int i = 0; i < groupPlacements.Length; i++)
                { var row = groupPlacements[i]; if (row != null) copy.groupPlacements[i] = new SanctumDecorGroupPlacement(row.placementId,
                    row.groupKey, row.anchorX, row.anchorY, row.logicalLayer, row.heightKey, row.sortingOrder, row.worldZ); }
            }
            if (allowedDecorCells != null)
            {
                copy.allowedDecorCells = new SanctumDecorAllowedCell[allowedDecorCells.Length];
                for (int i = 0; i < allowedDecorCells.Length; i++)
                { var row = allowedDecorCells[i]; if (row != null) copy.allowedDecorCells[i] = new SanctumDecorAllowedCell(row.x, row.y, row.logicalLayer, row.heightKey); }
            }
            return copy;
        }
        public string ComputeContentHash()
        { return SanctumConnectedDecorV01Catalog.Sha256(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(this))); }
        private void RequireWorldPosition(double centreX, double centreY, float z)
        {
            double x = originX + centreX * cellSize, y = originY + centreY * cellSize, depth = (double)originZ + z;
            if (x > float.MaxValue || x < -float.MaxValue || y > float.MaxValue || y < -float.MaxValue
                || depth > float.MaxValue || depth < -float.MaxValue)
                throw new ArgumentException("Snapshot world positions exceed the host renderer's finite coordinate range.");
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool Reject(string reason, out string error) { error = reason; return false; }
    }
}
