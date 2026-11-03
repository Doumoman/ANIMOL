using System;
using System.Collections.Generic;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    [Serializable] public sealed class SanctumDecorNetworkCell
    {
        public int x, y, logicalLayer, heightKey, sortingOrder;
        public float worldZ;
        public string family, connectionGroup;
        public SanctumDecorNetworkCell() { }
        public SanctumDecorNetworkCell(int x, int y, string family, string connectionGroup,
            int logicalLayer = 0, int heightKey = 0, int sortingOrder = 0, float worldZ = 0)
        { this.x = x; this.y = y; this.family = family; this.connectionGroup = connectionGroup;
            this.logicalLayer = logicalLayer; this.heightKey = heightKey; this.sortingOrder = sortingOrder; this.worldZ = worldZ; }
    }
    [Serializable] public sealed class SanctumDecorGroupPlacement
    {
        public string placementId, groupKey;
        public int anchorX, anchorY, logicalLayer, heightKey, sortingOrder;
        public float worldZ;
        public SanctumDecorGroupPlacement() { }
        public SanctumDecorGroupPlacement(string placementId, string groupKey, int anchorX, int anchorY,
            int logicalLayer = 0, int heightKey = 0, int sortingOrder = 0, float worldZ = 0)
        { this.placementId = placementId; this.groupKey = groupKey; this.anchorX = anchorX; this.anchorY = anchorY;
            this.logicalLayer = logicalLayer; this.heightKey = heightKey; this.sortingOrder = sortingOrder; this.worldZ = worldZ; }
    }
    public struct SanctumDecorCell : IEquatable<SanctumDecorCell>
    {
        public readonly int x, y;
        public SanctumDecorCell(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(SanctumDecorCell other) { return x == other.x && y == other.y; }
        public override bool Equals(object value) { return value is SanctumDecorCell && Equals((SanctumDecorCell)value); }
        public override int GetHashCode() { unchecked { return x * 397 ^ y; } }
    }
    public struct SanctumDecorClaim
    {
        public readonly SanctumDecorCell cell;
        public readonly int logicalLayer, heightKey;
        public readonly string owner, family;
        public readonly bool isGroup;
        public SanctumDecorClaim(SanctumDecorCell cell, int layer, int height, string owner, string family, bool isGroup)
        { this.cell = cell; logicalLayer = layer; heightKey = height; this.owner = owner; this.family = family; this.isGroup = isGroup; }
    }
    public struct SanctumDecorGroupSize
    {
        public readonly int width, height;
        public SanctumDecorGroupSize(int width, int height) { this.width = width; this.height = height; }
    }
    public sealed class SanctumDecorNetworkSelection
    {
        public readonly int x, y, logicalLayer, heightKey, sortingOrder, neighborMask;
        public readonly float worldZ;
        public readonly string family, connectionGroup, file, periodicPostFile;
        internal SanctumDecorNetworkSelection(SanctumDecorNetworkCell row, int mask, string file, string post)
        { x = row.x; y = row.y; logicalLayer = row.logicalLayer; heightKey = row.heightKey; sortingOrder = row.sortingOrder;
            worldZ = row.worldZ; family = row.family; connectionGroup = row.connectionGroup; neighborMask = mask;
            this.file = file; periodicPostFile = post; }
    }
    public sealed class SanctumDecorGroupSelection
    {
        public readonly string placementId, groupKey;
        public readonly int anchorX, anchorY, logicalLayer, heightKey, sortingOrder, widthCells, heightCells;
        public readonly float worldZ;
        public double CenterX { get { return anchorX + widthCells * .5; } }
        public double CenterY { get { return anchorY + heightCells * .5; } }
        internal SanctumDecorGroupSelection(SanctumDecorGroupPlacement row, SanctumDecorGroupSize size)
        { placementId = row.placementId; groupKey = row.groupKey; anchorX = row.anchorX; anchorY = row.anchorY;
            logicalLayer = row.logicalLayer; heightKey = row.heightKey; sortingOrder = row.sortingOrder; worldZ = row.worldZ;
            widthCells = size.width; heightCells = size.height; }
    }
    public sealed class SanctumDecorPlan
    {
        private readonly SanctumDecorNetworkSelection[] networks;
        private readonly SanctumDecorGroupSelection[] groups;
        private readonly SanctumDecorClaim[] claims;
        public int NetworkCount { get { return networks.Length; } }
        public int GroupCount { get { return groups.Length; } }
        public int ClaimCount { get { return claims.Length; } }
        public SanctumDecorNetworkSelection GetNetwork(int index) { return networks[index]; }
        public SanctumDecorGroupSelection GetGroup(int index) { return groups[index]; }
        public SanctumDecorClaim GetClaim(int index) { return claims[index]; }
        internal SanctumDecorPlan(SanctumDecorNetworkSelection[] networks, SanctumDecorGroupSelection[] groups, SanctumDecorClaim[] claims)
        { this.networks = networks; this.groups = groups; this.claims = claims; }
    }

    /// <summary>
    /// Pure VISUAL decoration planner. Full-board snapshots only, never viewport/chunk
    /// subsets. Claims do not mutate gameplay occupancy, collision, actors or costs.
    /// +Y North. The host supplies actual decoration eligibility for EVERY claimed cell.
    /// A rejected multi-cell group rejects the entire replacement plan atomically.
    /// </summary>
    public static class SanctumConnectedDecorV01Planner
    {
        public const string IvoryStarBalustrade = "ivory_star_balustrade";
        public const string BrassLatticeScreen = "brass_lattice_screen";
        public const string ArchiveShelfRun = "archive_shelf_run";
        public const string StarBannerPartition = "star_banner_partition";
        public const string BlueCrystalPlanter = "blue_crystal_planter";
        public const string VotiveLightChannel = "votive_light_channel";
        public const int N4 = 1, E4 = 2, S4 = 4, W4 = 8;
        private static readonly string[] networkFamilies = { IvoryStarBalustrade, BrassLatticeScreen,
            ArchiveShelfRun, StarBannerPartition, BlueCrystalPlanter, VotiveLightChannel };
        private static readonly string[] groupKeys = {
            "sanctum_altar_3x2", "archive_worktable_3x1", "ruined_colonnade_3x1", "prayer_pew_3x1",
            "celestial_chart_table_2x2", "reliquary_case_2x1", "folio_display_2x1", "scroll_archive_2x2",
            "banner_dais_2x1", "faceted_armillary_2x2", "ruined_mosaic_2x2", "offering_steps_2x1",
            "stone_lantern", "broken_pillar", "book_pedestal", "copper_fern", "scribe_inkstand", "votive_cluster", "scroll_basket", "star_boundary_marker" };
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
        { return family == IvoryStarBalustrade || family == BrassLatticeScreen; }
        public static bool TryGetDeclaredGroupSize(string key, out SanctumDecorGroupSize size)
        {
            switch (key)
            {
                case "sanctum_altar_3x2": size = new SanctumDecorGroupSize(3, 2); return true;
                case "archive_worktable_3x1": size = new SanctumDecorGroupSize(3, 1); return true;
                case "ruined_colonnade_3x1": size = new SanctumDecorGroupSize(3, 1); return true;
                case "prayer_pew_3x1": size = new SanctumDecorGroupSize(3, 1); return true;
                case "celestial_chart_table_2x2": size = new SanctumDecorGroupSize(2, 2); return true;
                case "reliquary_case_2x1": size = new SanctumDecorGroupSize(2, 1); return true;
                case "folio_display_2x1": size = new SanctumDecorGroupSize(2, 1); return true;
                case "scroll_archive_2x2": size = new SanctumDecorGroupSize(2, 2); return true;
                case "banner_dais_2x1": size = new SanctumDecorGroupSize(2, 1); return true;
                case "faceted_armillary_2x2": size = new SanctumDecorGroupSize(2, 2); return true;
                case "ruined_mosaic_2x2": size = new SanctumDecorGroupSize(2, 2); return true;
                case "offering_steps_2x1": size = new SanctumDecorGroupSize(2, 1); return true;
                case "stone_lantern": size = new SanctumDecorGroupSize(1, 1); return true;
                case "broken_pillar": size = new SanctumDecorGroupSize(1, 1); return true;
                case "book_pedestal": size = new SanctumDecorGroupSize(1, 1); return true;
                case "copper_fern": size = new SanctumDecorGroupSize(1, 1); return true;
                case "scribe_inkstand": size = new SanctumDecorGroupSize(1, 1); return true;
                case "votive_cluster": size = new SanctumDecorGroupSize(1, 1); return true;
                case "scroll_basket": size = new SanctumDecorGroupSize(1, 1); return true;
                case "star_boundary_marker": size = new SanctumDecorGroupSize(1, 1); return true;
                default: size = new SanctumDecorGroupSize(); return false;
            }
        }
        public static bool IsPointGroup(string key)
        { SanctumDecorGroupSize size; return TryGetDeclaredGroupSize(key, out size) && size.width == 1 && size.height == 1; }
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
        public static SanctumDecorCell FromRowDownCell(int column, int row) { return new SanctumDecorCell(column, checked(-row)); }
        public static SanctumDecorCell FromRowDownGroupBottomLeft(int leftColumn, int topRow, int heightCells)
        { if (heightCells < 1) throw new ArgumentOutOfRangeException("heightCells"); return new SanctumDecorCell(leftColumn, checked(-topRow - heightCells + 1)); }
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
        public static SanctumDecorPlan CreatePlan(IList<SanctumDecorNetworkCell> networkCells,
            IList<SanctumDecorGroupPlacement> groupPlacements, Func<string, SanctumDecorGroupSize> resolveGroupSize,
            Func<SanctumDecorClaim, bool> hostAllowDecorCell, int seed = 0, int postSpacing = 4,
            Func<string, bool> familyHasPeriodicPost = null)
        {
            if (postSpacing != 3 && postSpacing != 4) throw new ArgumentOutOfRangeException("postSpacing");
            var rows = networkCells ?? new SanctumDecorNetworkCell[0]; var placed = groupPlacements ?? new SanctumDecorGroupPlacement[0];
            if ((rows.Count != 0 || placed.Count != 0) && hostAllowDecorCell == null)
                throw new ArgumentNullException("hostAllowDecorCell", "The actual host must explicitly approve every visual decoration claim.");
            var byCell = new Dictionary<LayerCell, SanctumDecorNetworkCell>(); var occupied = new HashSet<LayerCell>();
            var claims = new List<SanctumDecorClaim>(); var groups = new List<SanctumDecorGroupSelection>();
            foreach (SanctumDecorNetworkCell row in rows)
            {
                if (row == null || !IsNetworkFamily(row.family) || string.IsNullOrWhiteSpace(row.connectionGroup)
                    || row.sortingOrder < -32768 || row.sortingOrder > 32766 || !Finite(row.worldZ)
                    || row.x == int.MinValue || row.x == int.MaxValue || row.y == int.MinValue || row.y == int.MaxValue)
                    throw new ArgumentException("Invalid network cell/connection group/visual order.");
                var key = new LayerCell(row.x, row.y, row.logicalLayer, row.heightKey);
                if (!occupied.Add(key)) throw new ArgumentException("Duplicate decoration cell in the same layer/height plane.");
                var copy = new SanctumDecorNetworkCell(row.x, row.y, row.family, row.connectionGroup, row.logicalLayer, row.heightKey, row.sortingOrder, row.worldZ);
                byCell.Add(key, copy);
                var claim = new SanctumDecorClaim(new SanctumDecorCell(row.x, row.y), row.logicalLayer, row.heightKey,
                    "network:" + row.family + ":" + row.connectionGroup, row.family, false);
                if (!hostAllowDecorCell(claim)) throw new ArgumentException("Host rejected decoration cell; no replacement plan may be committed.");
                claims.Add(claim);
            }
            var placementIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (SanctumDecorGroupPlacement row in placed)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.placementId) || string.IsNullOrWhiteSpace(row.groupKey)
                    || !placementIds.Add(row.placementId) || row.sortingOrder < -32768 || row.sortingOrder > 32767 || !Finite(row.worldZ))
                    throw new ArgumentException("Missing/duplicate multi-cell placement identity or visual data.");
                if (resolveGroupSize == null) throw new ArgumentNullException("resolveGroupSize");
                SanctumDecorGroupSize expected, size = resolveGroupSize(row.groupKey);
                if (!TryGetDeclaredGroupSize(row.groupKey, out expected) || size.width != expected.width || size.height != expected.height)
                    throw new ArgumentException("Unknown group/point key or footprint differs from the exact current art contract.");
                for (int y = 0; y < size.height; y++) for (int x = 0; x < size.width; x++)
                {
                    int cx = checked(row.anchorX + x), cy = checked(row.anchorY + y);
                    var key = new LayerCell(cx, cy, row.logicalLayer, row.heightKey);
                    if (!occupied.Add(key)) throw new ArgumentException("Multi-cell visual footprints overlap in the same layer/height plane.");
                    var claim = new SanctumDecorClaim(new SanctumDecorCell(cx, cy), row.logicalLayer, row.heightKey, row.placementId, row.groupKey, true);
                    if (!hostAllowDecorCell(claim)) throw new ArgumentException("Host rejected one footprint member; no partial group may be placed.");
                    claims.Add(claim);
                }
                groups.Add(new SanctumDecorGroupSelection(row, size));
            }
            var selected = new List<SanctumDecorNetworkSelection>();
            foreach (SanctumDecorNetworkCell row in byCell.Values)
            {
                Func<int, int, bool> same = (x, y) =>
                {
                    SanctumDecorNetworkCell neighbor;
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
                selected.Add(new SanctumDecorNetworkSelection(row, mask, NetworkFile(row.family, mask), post));
            }
            // Canonical order prevents host collection iteration order from changing render creation.
            selected.Sort((a, b) => Compare(a.logicalLayer, a.heightKey, a.y, a.x, b.logicalLayer, b.heightKey, b.y, b.x));
            groups.Sort((a, b) => string.CompareOrdinal(a.placementId, b.placementId));
            return new SanctumDecorPlan(selected.ToArray(), groups.ToArray(), claims.ToArray());
        }
        private static int Compare(int al, int ah, int ay, int ax, int bl, int bh, int by, int bx)
        { int c = al.CompareTo(bl); if (c == 0) c = ah.CompareTo(bh); if (c == 0) c = ay.CompareTo(by); return c == 0 ? ax.CompareTo(bx) : c; }
        private static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
    }
}
