using System;
using System.Collections.Generic;

namespace ANIMOL.Art.CoastConnectedDecorV01
{
    [Serializable] public sealed class CoastDecorNetworkCell
    {
        public int x, y, logicalLayer, heightKey, sortingOrder;
        public float worldZ;
        public string family, connectionGroup;
        public CoastDecorNetworkCell() { }
        public CoastDecorNetworkCell(int x, int y, string family, string connectionGroup,
            int logicalLayer = 0, int heightKey = 0, int sortingOrder = 0, float worldZ = 0)
        { this.x = x; this.y = y; this.family = family; this.connectionGroup = connectionGroup;
            this.logicalLayer = logicalLayer; this.heightKey = heightKey; this.sortingOrder = sortingOrder; this.worldZ = worldZ; }
    }
    [Serializable] public sealed class CoastDecorGroupPlacement
    {
        public string placementId, groupKey;
        public int anchorX, anchorY, logicalLayer, heightKey, sortingOrder;
        public float worldZ;
        public CoastDecorGroupPlacement() { }
        public CoastDecorGroupPlacement(string placementId, string groupKey, int anchorX, int anchorY,
            int logicalLayer = 0, int heightKey = 0, int sortingOrder = 0, float worldZ = 0)
        { this.placementId = placementId; this.groupKey = groupKey; this.anchorX = anchorX; this.anchorY = anchorY;
            this.logicalLayer = logicalLayer; this.heightKey = heightKey; this.sortingOrder = sortingOrder; this.worldZ = worldZ; }
    }
    public struct CoastDecorCell : IEquatable<CoastDecorCell>
    {
        public readonly int x, y;
        public CoastDecorCell(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(CoastDecorCell other) { return x == other.x && y == other.y; }
        public override bool Equals(object value) { return value is CoastDecorCell && Equals((CoastDecorCell)value); }
        public override int GetHashCode() { unchecked { return x * 397 ^ y; } }
    }
    public struct CoastDecorClaim
    {
        public readonly CoastDecorCell cell;
        public readonly int logicalLayer, heightKey;
        public readonly string owner, family;
        public readonly bool isGroup;
        public CoastDecorClaim(CoastDecorCell cell, int layer, int height, string owner, string family, bool isGroup)
        { this.cell = cell; logicalLayer = layer; heightKey = height; this.owner = owner; this.family = family; this.isGroup = isGroup; }
    }
    public struct CoastDecorGroupSize
    {
        public readonly int width, height;
        public CoastDecorGroupSize(int width, int height) { this.width = width; this.height = height; }
    }
    public sealed class CoastDecorNetworkSelection
    {
        public readonly int x, y, logicalLayer, heightKey, sortingOrder, neighborMask;
        public readonly float worldZ;
        public readonly string family, connectionGroup, file, periodicPostFile;
        internal CoastDecorNetworkSelection(CoastDecorNetworkCell row, int mask, string file, string post)
        { x = row.x; y = row.y; logicalLayer = row.logicalLayer; heightKey = row.heightKey; sortingOrder = row.sortingOrder;
            worldZ = row.worldZ; family = row.family; connectionGroup = row.connectionGroup; neighborMask = mask;
            this.file = file; periodicPostFile = post; }
    }
    public sealed class CoastDecorGroupSelection
    {
        public readonly string placementId, groupKey;
        public readonly int anchorX, anchorY, logicalLayer, heightKey, sortingOrder, widthCells, heightCells;
        public readonly float worldZ;
        public double CenterX { get { return anchorX + widthCells * .5; } }
        public double CenterY { get { return anchorY + heightCells * .5; } }
        internal CoastDecorGroupSelection(CoastDecorGroupPlacement row, CoastDecorGroupSize size)
        { placementId = row.placementId; groupKey = row.groupKey; anchorX = row.anchorX; anchorY = row.anchorY;
            logicalLayer = row.logicalLayer; heightKey = row.heightKey; sortingOrder = row.sortingOrder; worldZ = row.worldZ;
            widthCells = size.width; heightCells = size.height; }
    }
    public sealed class CoastDecorPlan
    {
        private readonly CoastDecorNetworkSelection[] networks;
        private readonly CoastDecorGroupSelection[] groups;
        private readonly CoastDecorClaim[] claims;
        public int NetworkCount { get { return networks.Length; } }
        public int GroupCount { get { return groups.Length; } }
        public int ClaimCount { get { return claims.Length; } }
        public CoastDecorNetworkSelection GetNetwork(int index) { return networks[index]; }
        public CoastDecorGroupSelection GetGroup(int index) { return groups[index]; }
        public CoastDecorClaim GetClaim(int index) { return claims[index]; }
        internal CoastDecorPlan(CoastDecorNetworkSelection[] networks, CoastDecorGroupSelection[] groups, CoastDecorClaim[] claims)
        { this.networks = networks; this.groups = groups; this.claims = claims; }
    }

    /// <summary>
    /// Pure VISUAL decoration planner. Full-board snapshots only, never viewport/chunk
    /// subsets. Claims do not mutate gameplay occupancy, collision, actors or costs.
    /// +Y North. The host supplies actual decoration eligibility for EVERY claimed cell.
    /// A rejected multi-cell group rejects the entire replacement plan atomically.
    /// </summary>
    public static class CoastConnectedDecorV01Planner
    {
        public const string RopeDockFence = "rope_dock_fence";
        public const string DriftwoodPalisade = "driftwood_palisade";
        public const string FishingNetScreen = "fishing_net_screen";
        public const string RaisedDuneGrass = "raised_dune_grass";
        public const string CoralRockBorder = "coral_rock_border";
        public const string HarborLanternChannel = "harbor_lantern_channel";
        public const int N4 = 1, E4 = 2, S4 = 4, W4 = 8;
        private static readonly string[] networkFamilies = { RopeDockFence, DriftwoodPalisade,
            FishingNetScreen, RaisedDuneGrass, CoralRockBorder, HarborLanternChannel };
        private static readonly string[] groupKeys = {
            "harbor_market_stall_3x2", "fisher_workbench_3x1", "driftwood_bench_3x1", "net_drying_rack_3x1",
            "cargo_pallet_2x2", "shell_display_2x1", "picnic_setting_2x1", "folded_sail_store_2x2",
            "dock_notice_board_2x1", "tidepool_garden_2x2", "beached_skiff_3x2", "shore_steps_2x1",
            "dock_lantern", "mooring_bollard", "flotsam_bundle", "coral_planter", "shell_cairn", "message_post", "folded_deck_chair", "beacon_marker" };
        // Return copies: caller code cannot change this package's exact art families.
        public static string[] NetworkFamilies { get { return (string[])networkFamilies.Clone(); } }
        public static string[] GroupKeys { get { return (string[])groupKeys.Clone(); } }
        private struct LayerCell : IEquatable<LayerCell>
        {
            public int x, y, layer, height;
            public LayerCell(int x, int y, int layer, int height) { this.x = x; this.y = y; this.layer = layer; this.height = height; }
            public bool Equals(LayerCell other) { return x == other.x && y == other.y && layer == other.layer && height == other.height; }
            public override bool Equals(object value) { return value is LayerCell && Equals((LayerCell)value); }
            public override int GetHashCode() { unchecked { return (((x * 397) ^ y) * 397 ^ layer) * 397 ^ height; } }
        }
        public static bool IsNetworkFamily(string value)
        { foreach (string family in networkFamilies) if (value == family) return true; return false; }
        public static bool SupportsPeriodicPost(string family)
        { return family == RopeDockFence || family == DriftwoodPalisade; }
        public static bool TryGetDeclaredGroupSize(string key, out CoastDecorGroupSize size)
        {
            switch (key)
            {
                case "harbor_market_stall_3x2": size = new CoastDecorGroupSize(3, 2); return true;
                case "fisher_workbench_3x1": size = new CoastDecorGroupSize(3, 1); return true;
                case "driftwood_bench_3x1": size = new CoastDecorGroupSize(3, 1); return true;
                case "net_drying_rack_3x1": size = new CoastDecorGroupSize(3, 1); return true;
                case "cargo_pallet_2x2": size = new CoastDecorGroupSize(2, 2); return true;
                case "shell_display_2x1": size = new CoastDecorGroupSize(2, 1); return true;
                case "picnic_setting_2x1": size = new CoastDecorGroupSize(2, 1); return true;
                case "folded_sail_store_2x2": size = new CoastDecorGroupSize(2, 2); return true;
                case "dock_notice_board_2x1": size = new CoastDecorGroupSize(2, 1); return true;
                case "tidepool_garden_2x2": size = new CoastDecorGroupSize(2, 2); return true;
                case "beached_skiff_3x2": size = new CoastDecorGroupSize(3, 2); return true;
                case "shore_steps_2x1": size = new CoastDecorGroupSize(2, 1); return true;
                case "dock_lantern": size = new CoastDecorGroupSize(1, 1); return true;
                case "mooring_bollard": size = new CoastDecorGroupSize(1, 1); return true;
                case "flotsam_bundle": size = new CoastDecorGroupSize(1, 1); return true;
                case "coral_planter": size = new CoastDecorGroupSize(1, 1); return true;
                case "shell_cairn": size = new CoastDecorGroupSize(1, 1); return true;
                case "message_post": size = new CoastDecorGroupSize(1, 1); return true;
                case "folded_deck_chair": size = new CoastDecorGroupSize(1, 1); return true;
                case "beacon_marker": size = new CoastDecorGroupSize(1, 1); return true;
                default: size = new CoastDecorGroupSize(); return false;
            }
        }
        public static bool IsPointGroup(string key)
        { CoastDecorGroupSize size; return TryGetDeclaredGroupSize(key, out size) && size.width == 1 && size.height == 1; }
        public static int CardinalMask(Func<int, int, bool> sameConnection, int x, int y)
        {
            if (sameConnection == null) throw new ArgumentNullException("sameConnection");
            return (sameConnection(x, checked(y + 1)) ? N4 : 0) | (sameConnection(checked(x + 1), y) ? E4 : 0)
                | (sameConnection(x, checked(y - 1)) ? S4 : 0) | (sameConnection(checked(x - 1), y) ? W4 : 0);
        }
        public static string NetworkFile(string family, int mask)
        {
            if (!IsNetworkFamily(family)) throw new ArgumentException("Unknown exact decor family.");
            if (mask < 0 || mask > 15) throw new ArgumentOutOfRangeException("mask");
            return "decor/connected/" + family + "/n" + mask.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + ".png";
        }
        public static CoastDecorCell FromRowDownCell(int column, int row) { return new CoastDecorCell(column, checked(-row)); }
        public static CoastDecorCell FromRowDownGroupBottomLeft(int leftColumn, int topRow, int heightCells)
        { if (heightCells < 1) throw new ArgumentOutOfRangeException("heightCells"); return new CoastDecorCell(leftColumn, checked(-topRow - heightCells + 1)); }
        public static uint StableHash(int seed, int x, int y, int layer, int height, string connectionGroup)
        {
            unchecked
            {
                uint h = 2166136261u;
                string tag = connectionGroup ?? "";
                foreach (char c in tag) { h ^= c; h *= 16777619u; }
                h ^= (uint)seed; h *= 16777619u; h ^= (uint)x * 0x9e3779b9u; h ^= (uint)y * 0x85ebca6bu;
                h ^= (uint)layer * 0xc2b2ae35u; h ^= (uint)height * 0x27d4eb2fu;
                h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15; h *= 0x846ca68bu; return h ^ (h >> 16);
            }
        }
        public static bool AddPeriodicPost(int mask4, int x, int y, int seed, int layer, int height, string group, int spacing = 4)
        {
            if (spacing != 3 && spacing != 4) throw new ArgumentOutOfRangeException("spacing");
            if (mask4 != (N4 | S4) && mask4 != (E4 | W4)) return false;
            int phase = (int)(StableHash(seed, 0, 0, layer, height, group) % (uint)spacing);
            long coordinate = mask4 == (N4 | S4) ? y : x;
            return ((coordinate - phase) % spacing + spacing) % spacing == 0;
        }
        public static CoastDecorPlan CreatePlan(IList<CoastDecorNetworkCell> networkCells,
            IList<CoastDecorGroupPlacement> groupPlacements, Func<string, CoastDecorGroupSize> resolveGroupSize,
            Func<CoastDecorClaim, bool> hostAllowDecorCell, int seed = 0, int postSpacing = 4,
            Func<string, bool> familyHasPeriodicPost = null)
        {
            if (postSpacing != 3 && postSpacing != 4) throw new ArgumentOutOfRangeException("postSpacing");
            var rows = networkCells ?? new CoastDecorNetworkCell[0]; var placed = groupPlacements ?? new CoastDecorGroupPlacement[0];
            if ((rows.Count != 0 || placed.Count != 0) && hostAllowDecorCell == null)
                throw new ArgumentNullException("hostAllowDecorCell", "The actual host must explicitly approve every visual decoration claim.");
            var byCell = new Dictionary<LayerCell, CoastDecorNetworkCell>(); var occupied = new HashSet<LayerCell>();
            var claims = new List<CoastDecorClaim>(); var groups = new List<CoastDecorGroupSelection>();
            foreach (CoastDecorNetworkCell row in rows)
            {
                if (row == null || !IsNetworkFamily(row.family) || string.IsNullOrWhiteSpace(row.connectionGroup)
                    || row.sortingOrder < -32768 || row.sortingOrder > 32766 || !Finite(row.worldZ)
                    || row.x == int.MinValue || row.x == int.MaxValue || row.y == int.MinValue || row.y == int.MaxValue)
                    throw new ArgumentException("Invalid network cell/connection group/visual order.");
                var key = new LayerCell(row.x, row.y, row.logicalLayer, row.heightKey);
                if (!occupied.Add(key)) throw new ArgumentException("Duplicate decoration cell in the same layer/height plane.");
                var copy = new CoastDecorNetworkCell(row.x, row.y, row.family, row.connectionGroup, row.logicalLayer, row.heightKey, row.sortingOrder, row.worldZ);
                byCell.Add(key, copy);
                var claim = new CoastDecorClaim(new CoastDecorCell(row.x, row.y), row.logicalLayer, row.heightKey,
                    "network:" + row.family + ":" + row.connectionGroup, row.family, false);
                if (!hostAllowDecorCell(claim)) throw new ArgumentException("Host rejected decoration cell; no replacement plan may be committed.");
                claims.Add(claim);
            }
            var placementIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CoastDecorGroupPlacement row in placed)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.placementId) || string.IsNullOrWhiteSpace(row.groupKey)
                    || !placementIds.Add(row.placementId) || row.sortingOrder < -32768 || row.sortingOrder > 32767 || !Finite(row.worldZ))
                    throw new ArgumentException("Missing/duplicate multi-cell placement identity or visual data.");
                if (resolveGroupSize == null) throw new ArgumentNullException("resolveGroupSize");
                CoastDecorGroupSize expected, size = resolveGroupSize(row.groupKey);
                if (!TryGetDeclaredGroupSize(row.groupKey, out expected) || size.width != expected.width || size.height != expected.height)
                    throw new ArgumentException("Unknown group/point key or footprint differs from the exact current art contract.");
                for (int y = 0; y < size.height; y++) for (int x = 0; x < size.width; x++)
                {
                    int cx = checked(row.anchorX + x), cy = checked(row.anchorY + y);
                    var key = new LayerCell(cx, cy, row.logicalLayer, row.heightKey);
                    if (!occupied.Add(key)) throw new ArgumentException("Multi-cell visual footprints overlap in the same layer/height plane.");
                    var claim = new CoastDecorClaim(new CoastDecorCell(cx, cy), row.logicalLayer, row.heightKey, row.placementId, row.groupKey, true);
                    if (!hostAllowDecorCell(claim)) throw new ArgumentException("Host rejected one footprint member; no partial group may be placed.");
                    claims.Add(claim);
                }
                groups.Add(new CoastDecorGroupSelection(row, size));
            }
            var selected = new List<CoastDecorNetworkSelection>();
            foreach (CoastDecorNetworkCell row in byCell.Values)
            {
                Func<int, int, bool> same = (x, y) =>
                {
                    CoastDecorNetworkCell neighbor;
                    return byCell.TryGetValue(new LayerCell(x, y, row.logicalLayer, row.heightKey), out neighbor)
                        && neighbor.family == row.family && neighbor.connectionGroup == row.connectionGroup;
                };
                int mask = CardinalMask(same, row.x, row.y);
                bool postCandidate = SupportsPeriodicPost(row.family) && familyHasPeriodicPost != null
                    && familyHasPeriodicPost(row.family) && AddPeriodicPost(mask, row.x, row.y, seed,
                    row.logicalLayer, row.heightKey, row.connectionGroup, postSpacing);
                // Keep optional straight posts away from existing terminal/corner/T/cross
                // posts. Both immediate along-line neighbours must also be straight.
                if (postCandidate)
                {
                    int dx = mask == (E4 | W4) ? 1 : 0, dy = mask == (N4 | S4) ? 1 : 0;
                    postCandidate = CardinalMask(same, checked(row.x - dx), checked(row.y - dy)) == mask
                        && CardinalMask(same, checked(row.x + dx), checked(row.y + dy)) == mask;
                }
                string post = postCandidate ? "decor/connected/" + row.family + "/periodic_post.png" : null;
                selected.Add(new CoastDecorNetworkSelection(row, mask, NetworkFile(row.family, mask), post));
            }
            // Canonical order prevents host collection iteration order from changing render creation.
            selected.Sort((a, b) => Compare(a.logicalLayer, a.heightKey, a.y, a.x, b.logicalLayer, b.heightKey, b.y, b.x));
            groups.Sort((a, b) => string.CompareOrdinal(a.placementId, b.placementId));
            return new CoastDecorPlan(selected.ToArray(), groups.ToArray(), claims.ToArray());
        }
        private static int Compare(int al, int ah, int ay, int ax, int bl, int bh, int by, int bx)
        { int c = al.CompareTo(bl); if (c == 0) c = ah.CompareTo(bh); if (c == 0) c = ay.CompareTo(by); return c == 0 ? ax.CompareTo(bx) : c; }
        private static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
    }
}
