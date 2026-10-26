using System;
using System.Collections.Generic;

namespace ANIMOL.Art.RanchConnectedDecorV02
{
    [Serializable] public sealed class RanchDecorNetworkCell
    {
        public int x, y, logicalLayer, heightKey, sortingOrder;
        public float worldZ;
        public string family, connectionGroup;
        public RanchDecorNetworkCell() { }
        public RanchDecorNetworkCell(int x, int y, string family, string connectionGroup,
            int logicalLayer = 0, int heightKey = 0, int sortingOrder = 0, float worldZ = 0)
        { this.x = x; this.y = y; this.family = family; this.connectionGroup = connectionGroup;
            this.logicalLayer = logicalLayer; this.heightKey = heightKey; this.sortingOrder = sortingOrder; this.worldZ = worldZ; }
    }
    [Serializable] public sealed class RanchDecorGroupPlacement
    {
        public string placementId, groupKey;
        public int anchorX, anchorY, logicalLayer, heightKey, sortingOrder;
        public float worldZ;
        public RanchDecorGroupPlacement() { }
        public RanchDecorGroupPlacement(string placementId, string groupKey, int anchorX, int anchorY,
            int logicalLayer = 0, int heightKey = 0, int sortingOrder = 0, float worldZ = 0)
        { this.placementId = placementId; this.groupKey = groupKey; this.anchorX = anchorX; this.anchorY = anchorY;
            this.logicalLayer = logicalLayer; this.heightKey = heightKey; this.sortingOrder = sortingOrder; this.worldZ = worldZ; }
    }
    public struct RanchDecorCell : IEquatable<RanchDecorCell>
    {
        public readonly int x, y;
        public RanchDecorCell(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(RanchDecorCell other) { return x == other.x && y == other.y; }
        public override bool Equals(object value) { return value is RanchDecorCell && Equals((RanchDecorCell)value); }
        public override int GetHashCode() { unchecked { return x * 397 ^ y; } }
    }
    public struct RanchDecorClaim
    {
        public readonly RanchDecorCell cell;
        public readonly int logicalLayer, heightKey;
        public readonly string owner, family;
        public readonly bool isGroup;
        public RanchDecorClaim(RanchDecorCell cell, int layer, int height, string owner, string family, bool isGroup)
        { this.cell = cell; logicalLayer = layer; heightKey = height; this.owner = owner; this.family = family; this.isGroup = isGroup; }
    }
    public struct RanchDecorGroupSize
    {
        public readonly int width, height;
        public RanchDecorGroupSize(int width, int height) { this.width = width; this.height = height; }
    }
    public sealed class RanchDecorNetworkSelection
    {
        public readonly int x, y, logicalLayer, heightKey, sortingOrder, neighborMask;
        public readonly float worldZ;
        public readonly string family, connectionGroup, file, periodicPostFile, flowerAccentFile;
        public readonly bool usesEightNeighbors;
        public readonly uint accentHash;
        internal RanchDecorNetworkSelection(RanchDecorNetworkCell row, int mask, string file, string post, uint hash)
        { x = row.x; y = row.y; logicalLayer = row.logicalLayer; heightKey = row.heightKey; sortingOrder = row.sortingOrder;
            worldZ = row.worldZ; family = row.family; connectionGroup = row.connectionGroup; neighborMask = mask;
            this.file = file; periodicPostFile = post; accentHash = hash;
            usesEightNeighbors = row.family == RanchConnectedDecorV02Planner.Meadow;
            flowerAccentFile = usesEightNeighbors && RanchConnectedDecorV02Planner.AddMeadowAccent(hash)
                ? RanchConnectedDecorV02Planner.MeadowAccentFile(RanchConnectedDecorV02Planner.MeadowAccentIndex(hash)) : null; }
    }
    public sealed class RanchDecorGroupSelection
    {
        public readonly string placementId, groupKey;
        public readonly int anchorX, anchorY, logicalLayer, heightKey, sortingOrder, widthCells, heightCells;
        public readonly float worldZ;
        public double CenterX { get { return anchorX + widthCells * .5; } }
        public double CenterY { get { return anchorY + heightCells * .5; } }
        internal RanchDecorGroupSelection(RanchDecorGroupPlacement row, RanchDecorGroupSize size)
        { placementId = row.placementId; groupKey = row.groupKey; anchorX = row.anchorX; anchorY = row.anchorY;
            logicalLayer = row.logicalLayer; heightKey = row.heightKey; sortingOrder = row.sortingOrder; worldZ = row.worldZ;
            widthCells = size.width; heightCells = size.height; }
    }
    public sealed class RanchDecorPlan
    {
        private readonly RanchDecorNetworkSelection[] networks;
        private readonly RanchDecorGroupSelection[] groups;
        private readonly RanchDecorClaim[] claims;
        public int NetworkCount { get { return networks.Length; } }
        public int GroupCount { get { return groups.Length; } }
        public int ClaimCount { get { return claims.Length; } }
        public RanchDecorNetworkSelection GetNetwork(int index) { return networks[index]; }
        public RanchDecorGroupSelection GetGroup(int index) { return groups[index]; }
        public RanchDecorClaim GetClaim(int index) { return claims[index]; }
        internal RanchDecorPlan(RanchDecorNetworkSelection[] networks, RanchDecorGroupSelection[] groups, RanchDecorClaim[] claims)
        { this.networks = networks; this.groups = groups; this.claims = claims; }
    }

    /// <summary>
    /// Pure VISUAL decoration planner. Full-board snapshots only, never viewport/chunk
    /// subsets. Claims do not mutate gameplay occupancy, collision, actors or costs.
    /// +Y North. The host supplies actual decoration eligibility for EVERY claimed cell.
    /// A rejected multi-cell group rejects the entire replacement plan atomically.
    /// </summary>
    public static class RanchConnectedDecorV02Planner
    {
        public const string CreamFence = "cream_fence";
        public const string MeadowStoneWall = "meadow_stone_wall";
        public const string RaisedFlowerMeadow = "raised_flower_meadow";
        public const string OrchardTrellis = "orchard_trellis";
        public const string HerbPlanterStrip = "herb_planter_strip";
        public const string RanchLanternStrip = "ranch_lantern_strip";
        public const int N4 = 1, E4 = 2, S4 = 4, W4 = 8;
        public const int N8 = 1, NE8 = 2, E8 = 4, SE8 = 8, S8 = 16, SW8 = 32, W8 = 64, NW8 = 128;
        public const string Meadow = RaisedFlowerMeadow;
        private static readonly string[] networkFamilies = { CreamFence, MeadowStoneWall,
            RaisedFlowerMeadow, OrchardTrellis, HerbPlanterStrip, RanchLanternStrip };
        private static readonly string[] groupKeys = {
            "hay_store_2x1", "feeding_corner_3x1", "tree_flower_corner_2x2", "barnyard_3x2",
            "orchard_bench_3x1", "seedling_rack_3x1", "harvest_cart_2x2", "chicken_coop_2x2",
            "apiary_corner_2x2", "picnic_arbor_3x2", "produce_stall_3x2", "garden_workbench_2x1",
            "milk_can_pair", "tool_basket", "ranch_notice_sign", "watering_can", "feed_sack_stack", "flower_bucket", "harvest_crate", "honey_jar_stand" };
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
        { return family == CreamFence || family == MeadowStoneWall; }
        public static bool TryGetDeclaredGroupSize(string key, out RanchDecorGroupSize size)
        {
            switch (key)
            {
                case "hay_store_2x1": size = new RanchDecorGroupSize(2, 1); return true;
                case "feeding_corner_3x1": size = new RanchDecorGroupSize(3, 1); return true;
                case "tree_flower_corner_2x2": size = new RanchDecorGroupSize(2, 2); return true;
                case "barnyard_3x2": size = new RanchDecorGroupSize(3, 2); return true;
                case "orchard_bench_3x1": size = new RanchDecorGroupSize(3, 1); return true;
                case "seedling_rack_3x1": size = new RanchDecorGroupSize(3, 1); return true;
                case "harvest_cart_2x2": size = new RanchDecorGroupSize(2, 2); return true;
                case "chicken_coop_2x2": size = new RanchDecorGroupSize(2, 2); return true;
                case "apiary_corner_2x2": size = new RanchDecorGroupSize(2, 2); return true;
                case "picnic_arbor_3x2": size = new RanchDecorGroupSize(3, 2); return true;
                case "produce_stall_3x2": size = new RanchDecorGroupSize(3, 2); return true;
                case "garden_workbench_2x1": size = new RanchDecorGroupSize(2, 1); return true;
                case "milk_can_pair": size = new RanchDecorGroupSize(1, 1); return true;
                case "tool_basket": size = new RanchDecorGroupSize(1, 1); return true;
                case "ranch_notice_sign": size = new RanchDecorGroupSize(1, 1); return true;
                case "watering_can": size = new RanchDecorGroupSize(1, 1); return true;
                case "feed_sack_stack": size = new RanchDecorGroupSize(1, 1); return true;
                case "flower_bucket": size = new RanchDecorGroupSize(1, 1); return true;
                case "harvest_crate": size = new RanchDecorGroupSize(1, 1); return true;
                case "honey_jar_stand": size = new RanchDecorGroupSize(1, 1); return true;
                default: size = new RanchDecorGroupSize(); return false;
            }
        }
        public static bool IsPointGroup(string key)
        { RanchDecorGroupSize size; return TryGetDeclaredGroupSize(key, out size) && size.width == 1 && size.height == 1; }
        public static int CardinalMask(Func<int, int, bool> sameConnection, int x, int y)
        {
            if (sameConnection == null) throw new ArgumentNullException("sameConnection");
            return (sameConnection(x, checked(y + 1)) ? N4 : 0) | (sameConnection(checked(x + 1), y) ? E4 : 0)
                | (sameConnection(x, checked(y - 1)) ? S4 : 0) | (sameConnection(checked(x - 1), y) ? W4 : 0);
        }
        public static int MaskCount(string family)
        {
            if (!IsNetworkFamily(family)) throw new ArgumentException("Unknown exact decor family.");
            return family == Meadow ? 256 : 16;
        }
        public static string NetworkKind(string family)
        { return family == Meadow ? "connected_meadow" : "connected_tile"; }
        public static int EightNeighborMask(Func<int, int, bool> sameConnection, int x, int y)
        {
            if (sameConnection == null) throw new ArgumentNullException("sameConnection");
            int east = checked(x + 1), west = checked(x - 1), north = checked(y + 1), south = checked(y - 1);
            return (sameConnection(x, north) ? N8 : 0) | (sameConnection(east, north) ? NE8 : 0)
                | (sameConnection(east, y) ? E8 : 0) | (sameConnection(east, south) ? SE8 : 0)
                | (sameConnection(x, south) ? S8 : 0) | (sameConnection(west, south) ? SW8 : 0)
                | (sameConnection(west, y) ? W8 : 0) | (sameConnection(west, north) ? NW8 : 0);
        }
        public static string NetworkFile(string family, int mask)
        {
            if (!IsNetworkFamily(family)) throw new ArgumentException("Unknown exact decor family.");
            if (mask < 0 || mask >= MaskCount(family)) throw new ArgumentOutOfRangeException("mask");
            return "decor/connected/" + family + "/n" + mask.ToString(family == Meadow ? "D3" : "D2", System.Globalization.CultureInfo.InvariantCulture) + ".png";
        }
        public static RanchDecorCell FromRowDownCell(int column, int row) { return new RanchDecorCell(column, checked(-row)); }
        public static RanchDecorCell FromRowDownGroupBottomLeft(int leftColumn, int topRow, int heightCells)
        { if (heightCells < 1) throw new ArgumentOutOfRangeException("heightCells"); return new RanchDecorCell(leftColumn, checked(-topRow - heightCells + 1)); }
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
        // Original Ranch density and variant policy; overlays do not add claims.
        public static bool AddMeadowAccent(uint hash) { return hash % 5u == 0; }
        public static int MeadowAccentIndex(uint hash) { return (int)((hash / 5u) % 4u); }
        public static string MeadowAccentFile(int index)
        {
            if (index < 0 || index >= 4) throw new ArgumentOutOfRangeException("index");
            return "decor/accents/meadow/flowers_" + (index + 1).ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + ".png";
        }
        public static bool AddPeriodicPost(int mask4, int x, int y, int seed, int layer, int height, string group, int spacing = 4)
        {
            if (spacing != 3 && spacing != 4) throw new ArgumentOutOfRangeException("spacing");
            if (mask4 != (N4 | S4) && mask4 != (E4 | W4)) return false;
            int phase = (int)(StableHash(seed, 0, 0, layer, height, group) % (uint)spacing);
            long coordinate = mask4 == (N4 | S4) ? y : x;
            return ((coordinate - phase) % spacing + spacing) % spacing == 0;
        }
        public static RanchDecorPlan CreatePlan(IList<RanchDecorNetworkCell> networkCells,
            IList<RanchDecorGroupPlacement> groupPlacements, Func<string, RanchDecorGroupSize> resolveGroupSize,
            Func<RanchDecorClaim, bool> hostAllowDecorCell, int seed = 0, int postSpacing = 4,
            Func<string, bool> familyHasPeriodicPost = null)
        {
            if (postSpacing != 3 && postSpacing != 4) throw new ArgumentOutOfRangeException("postSpacing");
            var rows = networkCells ?? new RanchDecorNetworkCell[0]; var placed = groupPlacements ?? new RanchDecorGroupPlacement[0];
            if ((rows.Count != 0 || placed.Count != 0) && hostAllowDecorCell == null)
                throw new ArgumentNullException("hostAllowDecorCell", "The actual host must explicitly approve every visual decoration claim.");
            var byCell = new Dictionary<LayerCell, RanchDecorNetworkCell>(); var occupied = new HashSet<LayerCell>();
            var claims = new List<RanchDecorClaim>(); var groups = new List<RanchDecorGroupSelection>();
            foreach (RanchDecorNetworkCell row in rows)
            {
                if (row == null || !IsNetworkFamily(row.family) || string.IsNullOrWhiteSpace(row.connectionGroup)
                    || row.sortingOrder < -32768 || row.sortingOrder > 32766 || !Finite(row.worldZ)
                    || row.x == int.MinValue || row.x == int.MaxValue || row.y == int.MinValue || row.y == int.MaxValue)
                    throw new ArgumentException("Invalid network cell/connection group/visual order.");
                var key = new LayerCell(row.x, row.y, row.logicalLayer, row.heightKey);
                if (!occupied.Add(key)) throw new ArgumentException("Duplicate decoration cell in the same layer/height plane.");
                var copy = new RanchDecorNetworkCell(row.x, row.y, row.family, row.connectionGroup, row.logicalLayer, row.heightKey, row.sortingOrder, row.worldZ);
                byCell.Add(key, copy);
                var claim = new RanchDecorClaim(new RanchDecorCell(row.x, row.y), row.logicalLayer, row.heightKey,
                    "network:" + row.family + ":" + row.connectionGroup, row.family, false);
                if (!hostAllowDecorCell(claim)) throw new ArgumentException("Host rejected decoration cell; no replacement plan may be committed.");
                claims.Add(claim);
            }
            var placementIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (RanchDecorGroupPlacement row in placed)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.placementId) || string.IsNullOrWhiteSpace(row.groupKey)
                    || !placementIds.Add(row.placementId) || row.sortingOrder < -32768 || row.sortingOrder > 32767 || !Finite(row.worldZ))
                    throw new ArgumentException("Missing/duplicate multi-cell placement identity or visual data.");
                if (resolveGroupSize == null) throw new ArgumentNullException("resolveGroupSize");
                RanchDecorGroupSize expected, size = resolveGroupSize(row.groupKey);
                if (!TryGetDeclaredGroupSize(row.groupKey, out expected) || size.width != expected.width || size.height != expected.height)
                    throw new ArgumentException("Unknown group/point key or footprint differs from the exact current art contract.");
                for (int y = 0; y < size.height; y++) for (int x = 0; x < size.width; x++)
                {
                    int cx = checked(row.anchorX + x), cy = checked(row.anchorY + y);
                    var key = new LayerCell(cx, cy, row.logicalLayer, row.heightKey);
                    if (!occupied.Add(key)) throw new ArgumentException("Multi-cell visual footprints overlap in the same layer/height plane.");
                    var claim = new RanchDecorClaim(new RanchDecorCell(cx, cy), row.logicalLayer, row.heightKey, row.placementId, row.groupKey, true);
                    if (!hostAllowDecorCell(claim)) throw new ArgumentException("Host rejected one footprint member; no partial group may be placed.");
                    claims.Add(claim);
                }
                groups.Add(new RanchDecorGroupSelection(row, size));
            }
            var selected = new List<RanchDecorNetworkSelection>();
            foreach (RanchDecorNetworkCell row in byCell.Values)
            {
                Func<int, int, bool> same = (x, y) =>
                {
                    RanchDecorNetworkCell neighbor;
                    return byCell.TryGetValue(new LayerCell(x, y, row.logicalLayer, row.heightKey), out neighbor)
                        && neighbor.family == row.family && neighbor.connectionGroup == row.connectionGroup;
                };
                int mask = row.family == Meadow ? EightNeighborMask(same, row.x, row.y) : CardinalMask(same, row.x, row.y);
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
                selected.Add(new RanchDecorNetworkSelection(row, mask, NetworkFile(row.family, mask), post,
                    StableHash(seed, row.x, row.y, row.logicalLayer, row.heightKey, row.connectionGroup)));
            }
            // Canonical order prevents host collection iteration order from changing render creation.
            selected.Sort((a, b) => Compare(a.logicalLayer, a.heightKey, a.y, a.x, b.logicalLayer, b.heightKey, b.y, b.x));
            groups.Sort((a, b) => string.CompareOrdinal(a.placementId, b.placementId));
            return new RanchDecorPlan(selected.ToArray(), groups.ToArray(), claims.ToArray());
        }
        private static int Compare(int al, int ah, int ay, int ax, int bl, int bh, int by, int bx)
        { int c = al.CompareTo(bl); if (c == 0) c = ah.CompareTo(bh); if (c == 0) c = ay.CompareTo(by); return c == 0 ? ax.CompareTo(bx) : c; }
        private static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
    }
}
