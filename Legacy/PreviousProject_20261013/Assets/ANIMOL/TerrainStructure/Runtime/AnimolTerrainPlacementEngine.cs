using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Animol.TerrainStructure
{
    /// <summary>
    /// Pure logical transactions. No GameObject, asset, Tilemap, alpha or collider
    /// inspection. A successful edit returns a deep map copy and advances revision
    /// exactly once; a failure returns null and never changes the input map.
    /// </summary>
    public static partial class AnimolTerrainPlacementEngine
    {
        public const string StructureKind = "Structure";
        public const string InteriorOverlayKind = "InteriorOverlay";
        public const string NoOrientation = "None";

        public static bool ValidateCatalog(AnimolTerrainCatalogData catalog, out string error)
        {
            error = null;
            if (catalog == null) return Fail("Catalog is missing.", out error);
            if (catalog.version <= 0) return Fail("Catalog version must be positive.", out error);
            if (catalog.cellUnits != 1f) return Fail("This contract requires cellUnits = 1.", out error);
            if (catalog.entries == null || catalog.entries.Length == 0)
                return Fail("Catalog entries are missing.", out error);

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (AnimolTerrainCatalogEntry entry in catalog.entries)
            {
                if (entry == null) return Fail("Catalog contains a null entry.", out error);
                if (!HasText(entry.id) || !ids.Add(entry.id))
                    return Fail("Catalog IDs must be nonempty and unique: " + entry.id, out error);
                if (!HasText(entry.themeId) || !HasText(entry.styleId) ||
                    !HasText(entry.frameId) || !HasText(entry.role))
                    return Fail(entry.id + ": theme, style, frame and role are required.", out error);
                if (entry.width <= 0 || entry.height <= 0)
                    return Fail(entry.id + ": footprint dimensions must be positive.", out error);
                if (entry.kind != StructureKind && entry.kind != InteriorOverlayKind)
                    return Fail(entry.id + ": unsupported kind " + entry.kind, out error);
                if (!ValidateRows(entry.solidRows, entry.width, entry.height, out error))
                {
                    error = entry.id + " solidRows: " + error;
                    return false;
                }
                if (!ValidateRows(entry.supportRows, entry.width, entry.height, out error))
                {
                    error = entry.id + " supportRows: " + error;
                    return false;
                }
                bool hasSolid = false;
                int solidCount = 0, supportCount = 0;
                for (int r = 0; r < entry.height; r++)
                {
                    for (int x = 0; x < entry.width; x++)
                    {
                        bool solid = entry.solidRows[r][x] == '#';
                        bool support = entry.supportRows[r][x] == '#';
                        hasSolid |= solid;
                        if (solid) solidCount++;
                        if (support) supportCount++;
                        if (entry.kind == StructureKind && support)
                            return Fail(entry.id + ": structures must not require overlay support.", out error);
                        if (entry.kind == InteriorOverlayKind && (solid || !support))
                            return Fail(entry.id + ": overlays must have no solids and require the entire footprint.", out error);
                    }
                }
                if (entry.kind == StructureKind && !hasSolid)
                    return Fail(entry.id + ": a structure must own at least one solid cell.", out error);
                if (entry.footprintCount != entry.width * entry.height || entry.solidCount != solidCount ||
                    entry.voidCount != entry.width * entry.height - solidCount || entry.supportCount != supportCount)
                    return Fail(entry.id + ": declared cell counts do not match masks.", out error);
                if (!IsHash(entry.maskHash) ||
                    !String.Equals(entry.maskHash, ComputeMaskHash(entry), StringComparison.OrdinalIgnoreCase))
                    return Fail(entry.id + ": maskHash does not match the catalog geometry.", out error);
            }
            return true;
        }

        /// <summary>SHA256 over the versioned UTF8 geometry contract, no final newline.</summary>
        public static string ComputeMaskHash(AnimolTerrainCatalogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");
            string text = String.Join("\n", new[]
            {
                "ANIMOL_GEOMETRY_V3", entry.id ?? "", entry.themeId ?? "", entry.styleId ?? "",
                entry.kind ?? "", entry.width.ToString(CultureInfo.InvariantCulture),
                entry.height.ToString(CultureInfo.InvariantCulture),
                String.Join("\n", entry.solidRows ?? new string[0]), "SUPPORT",
                String.Join("\n", entry.supportRows ?? new string[0])
            });
            using (SHA256 sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder hex = new StringBuilder(digest.Length * 2);
                foreach (byte b in digest) hex.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return hex.ToString();
            }
        }

        public static bool Resolve(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap map,
            out AnimolTerrainResolveResult result, out string error)
        {
            return ResolveCore(catalog, map, true, out result, out error);
        }

        public static bool CanPlace(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap map,
            string catalogId, Vector2Int origin, out string error)
        {
            AnimolTerrainSavedMap ignored;
            return TryPlace(catalog, map, catalogId, origin, out ignored, out error);
        }

        public static bool TryPlace(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap map,
            string catalogId, Vector2Int origin, out AnimolTerrainSavedMap candidate, out string error)
        {
            candidate = null;
            AnimolTerrainResolveResult resolved;
            if (!Resolve(catalog, map, out resolved, out error)) return false;
            AnimolTerrainCatalogEntry entry;
            if (catalogId == null || !resolved.entriesById.TryGetValue(catalogId, out entry))
                return Fail("Unknown catalog ID: " + catalogId, out error);
            if (entry.themeId != map.themeId)
                return Fail("Placement theme must match the map theme.", out error);
            AnimolTerrainSavedMap edited = CloneMap(map);
            string instanceId;
            do { instanceId = Guid.NewGuid().ToString("N"); }
            while (FindPlacement(edited, instanceId) != null);
            edited.placements.Add(new AnimolTerrainPlacement
            {
                instanceId = instanceId, catalogId = entry.id, catalogVersion = catalog.version,
                maskHash = entry.maskHash, themeId = entry.themeId, styleId = entry.styleId,
                x = origin.x, y = origin.y, orientation = NoOrientation
            });
            return CompleteTransaction(catalog, map, edited,
                entry.kind == InteriorOverlayKind ? instanceId : null, out candidate, out error);
        }

        public static bool TryDelete(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap map,
            string instanceId, out AnimolTerrainSavedMap candidate, out string error)
        {
            candidate = null;
            AnimolTerrainResolveResult ignored;
            if (!Resolve(catalog, map, out ignored, out error)) return false;
            if (!HasText(instanceId) || FindPlacement(map, instanceId) == null)
                return Fail("Unknown placement owner: " + instanceId, out error);
            AnimolTerrainSavedMap edited = CloneMap(map);
            edited.placements.RemoveAll(p => p.instanceId == instanceId);
            return CompleteTransaction(catalog, map, edited, null, out candidate, out error);
        }

        public static bool TryMove(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap map,
            string instanceId, Vector2Int newOrigin, out AnimolTerrainSavedMap candidate, out string error)
        {
            candidate = null;
            AnimolTerrainResolveResult resolved;
            if (!Resolve(catalog, map, out resolved, out error)) return false;
            AnimolTerrainPlacement previous = FindPlacement(map, instanceId);
            if (previous == null) return Fail("Unknown placement owner: " + instanceId, out error);
            if (previous.x == newOrigin.x && previous.y == newOrigin.y)
                return Fail("Placement is already at that origin.", out error);
            AnimolTerrainSavedMap edited = CloneMap(map);
            AnimolTerrainPlacement moved = FindPlacement(edited, instanceId);
            moved.x = newOrigin.x;
            moved.y = newOrigin.y;
            return CompleteTransaction(catalog, map, edited,
                resolved.entriesById[moved.catalogId].kind == InteriorOverlayKind ? instanceId : null,
                out candidate, out error);
        }

        /// <summary>
        /// Ordinary 1x1 brush. Owned # cells require whole-owner move/delete.
        /// Arch . cells are editable even inside a structure's visual footprint.
        /// </summary>
        public static bool TrySetBaseCell(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap map,
            Vector2Int cell, bool solid, string styleId,
            out AnimolTerrainSavedMap candidate, out string error)
        {
            candidate = null;
            AnimolTerrainResolveResult resolved;
            if (!Resolve(catalog, map, out resolved, out error)) return false;
            AnimolTerrainCellResult occupied;
            if (resolved.solids.TryGetValue(cell, out occupied) && HasText(occupied.ownerId))
                return Fail(occupied.ownerId.StartsWith("free:",StringComparison.Ordinal)
                    ? "자유형 지형 모드에서 이 셀을 편집하세요."
                    : "Cell is owned by structure " + occupied.ownerId + "; edit the complete placement.", out error);
            if (solid && !KnownStyle(resolved.entriesById, map.themeId, styleId))
                return Fail("Unknown style for this map theme: " + styleId, out error);
            AnimolTerrainBaseCell previous = FindBaseCell(map, cell);
            if (!solid && previous == null) return Fail("There is no ordinary cell to erase.", out error);
            if (solid && previous != null && previous.themeId == map.themeId && previous.styleId == styleId)
                return Fail("Ordinary cell already has that style.", out error);
            AnimolTerrainSavedMap edited = CloneMap(map);
            edited.baseCells.RemoveAll(c => c.x == cell.x && c.y == cell.y);
            if (solid)
                edited.baseCells.Add(new AnimolTerrainBaseCell
                { x = cell.x, y = cell.y, themeId = map.themeId, styleId = styleId });
            return CompleteTransaction(catalog, map, edited, null, out candidate, out error);
        }

        public static AnimolTerrainSavedMap CloneMap(AnimolTerrainSavedMap source)
        {
            if (source == null) return null;
            AnimolTerrainSavedMap copy = new AnimolTerrainSavedMap
            {
                schemaVersion = source.schemaVersion, revision = source.revision, themeId = source.themeId,
                freeShape = source.freeShape?.Copy() ?? new FreeShapeLayer(),
                baseCells = source.baseCells == null ? null : new List<AnimolTerrainBaseCell>(),
                placements = source.placements == null ? null : new List<AnimolTerrainPlacement>()
            };
            if (source.baseCells != null)
                foreach (AnimolTerrainBaseCell cell in source.baseCells)
                    copy.baseCells.Add(cell == null ? null : new AnimolTerrainBaseCell
                    { x = cell.x, y = cell.y, themeId = cell.themeId, styleId = cell.styleId });
            if (source.placements != null)
                foreach (AnimolTerrainPlacement placement in source.placements)
                    copy.placements.Add(ClonePlacement(placement));
            return copy;
        }

        public static int FloorDiv(int value, int divisor)
        {
            if (divisor <= 0) throw new ArgumentOutOfRangeException("divisor", "Divisor must be positive.");
            int quotient = value / divisor;
            return value % divisor < 0 ? quotient - 1 : quotient;
        }

        private static bool ResolveCore(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap map,
            bool validateOverlaySupport, out AnimolTerrainResolveResult result, out string error)
        {
            result = null;
            if (!ValidateCatalog(catalog, out error)) return false;
            if (map == null) return Fail("Map is missing.", out error);
            if (map.schemaVersion != 3) return Fail("Map schemaVersion must be 3.", out error);
            if (map.revision < 0) return Fail("Map revision cannot be negative.", out error);
            if (!HasText(map.themeId)) return Fail("Map theme is required.", out error);
            if (map.baseCells == null || map.placements == null)
                return Fail("Map cell and placement lists must be initialized.", out error);

            AnimolTerrainResolveResult built = new AnimolTerrainResolveResult();
            bool knownTheme = false;
            foreach (AnimolTerrainCatalogEntry entry in catalog.entries)
            {
                built.entriesById.Add(entry.id, CloneEntry(entry));
                knownTheme |= entry.themeId == map.themeId;
            }
            if (!knownTheme) return Fail("Map theme does not exist in this catalog.", out error);
            foreach (AnimolTerrainBaseCell cell in map.baseCells)
            {
                if (cell == null) return Fail("Map contains a null base cell.", out error);
                if (cell.themeId != map.themeId || !KnownStyle(built.entriesById, cell.themeId, cell.styleId))
                    return Fail("Base cell theme/style does not match the map catalog.", out error);
                Vector2Int position = new Vector2Int(cell.x, cell.y);
                if (built.solids.ContainsKey(position))
                    return Fail("Duplicate ordinary cell at " + position, out error);
                built.solids.Add(position, new AnimolTerrainCellResult
                { cell = position, ownerId = "", themeId = cell.themeId, styleId = cell.styleId });
            }

            try
            {
                foreach(var cell in FreeShapeTopology.Index(map.freeShape).Values)
                {
                    if(!KnownStyle(built.entriesById,map.themeId,cell.styleId))return Fail("Free-shape style does not match map theme: "+cell.styleId,out error);
                    if(built.solids.ContainsKey(cell.Position))return Fail("Free-shape overlaps existing terrain at "+cell.Position,out error);
                    built.solids.Add(cell.Position,new AnimolTerrainCellResult{cell=cell.Position,ownerId="free:"+cell.x+","+cell.y,themeId=map.themeId,styleId=cell.styleId});
                }
            }
            catch(Exception ex){return Fail(ex.Message,out error);}
            HashSet<string> owners = new HashSet<string>(StringComparer.Ordinal);
            foreach (AnimolTerrainPlacement placement in map.placements)
            {
                if (placement == null) return Fail("Map contains a null placement.", out error);
                if (!HasText(placement.instanceId) || !owners.Add(placement.instanceId))
                    return Fail("Placement owner IDs must be nonempty and unique.", out error);
                AnimolTerrainCatalogEntry entry;
                if (placement.catalogId == null || !built.entriesById.TryGetValue(placement.catalogId, out entry))
                    return Fail("Unknown placed catalog ID: " + placement.catalogId, out error);
                if (placement.catalogVersion != catalog.version ||
                    !String.Equals(placement.maskHash, entry.maskHash, StringComparison.OrdinalIgnoreCase))
                    return Fail(placement.instanceId + ": pinned catalog version/hash changed; explicit migration is required.", out error);
                if (placement.themeId != map.themeId || placement.themeId != entry.themeId ||
                    placement.styleId != entry.styleId)
                    return Fail(placement.instanceId + ": placement theme/style differs from its catalog/map.", out error);
                if (placement.orientation != NoOrientation)
                    return Fail(placement.instanceId + ": only orientation None is supported.", out error);
                if (!ValidFootprint(entry, placement.x, placement.y, out error))
                {
                    error = placement.instanceId + ": " + error;
                    return false;
                }
                List<AnimolTerrainPlacement> family = entry.kind == StructureKind ? built.placements : built.overlays;
                foreach (AnimolTerrainPlacement other in family)
                    if (FootprintsOverlap(entry, placement, built.entriesById[other.catalogId], other))
                        return Fail(placement.instanceId + ": footprint overlaps another " + entry.kind + ".", out error);
                family.Add(ClonePlacement(placement));
                if (entry.kind == InteriorOverlayKind) continue;
                for (int row = 0; row < entry.height; row++)
                {
                    int y = (int)((long)placement.y + entry.height - 1L - row);
                    for (int x = 0; x < entry.width; x++)
                    {
                        if (entry.solidRows[row][x] != '#') continue;
                        Vector2Int cell = new Vector2Int((int)((long)placement.x + x), y);
                        if (built.solids.ContainsKey(cell))
                            return Fail(placement.instanceId + ": solid cell overlaps existing terrain at " + cell, out error);
                        built.solids.Add(cell, new AnimolTerrainCellResult
                        { cell = cell, ownerId = placement.instanceId, themeId = entry.themeId, styleId = entry.styleId });
                    }
                }
            }
            if (validateOverlaySupport)
                foreach (AnimolTerrainPlacement overlay in built.overlays)
                    if (!OverlaySupported(built, overlay, out error)) return false;
            result = built;
            error = null;
            return true;
        }

        private static bool CompleteTransaction(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap source,
            AnimolTerrainSavedMap edited, string requiredOverlayId,
            out AnimolTerrainSavedMap candidate, out string error)
        {
            candidate = null;
            if (source.revision == Int32.MaxValue) return Fail("Map revision has reached its maximum.", out error);
            AnimolTerrainResolveResult resolved;
            if (!ResolveCore(catalog, edited, false, out resolved, out error)) return false;
            HashSet<string> invalidOverlays = new HashSet<string>(StringComparer.Ordinal);
            foreach (AnimolTerrainPlacement overlay in resolved.overlays)
            {
                string supportError;
                if (OverlaySupported(resolved, overlay, out supportError)) continue;
                // A requested new/moved overlay must be valid. Existing overlays
                // losing support are removed as part of this same transaction.
                if (overlay.instanceId == requiredOverlayId)
                {
                    error = supportError;
                    return false;
                }
                invalidOverlays.Add(overlay.instanceId);
            }
            if (invalidOverlays.Count != 0)
                edited.placements.RemoveAll(p => invalidOverlays.Contains(p.instanceId));
            AnimolTerrainResolveResult checkedResult;
            if (!ResolveCore(catalog, edited, true, out checkedResult, out error)) return false;
            edited.revision = source.revision + 1;
            candidate = edited;
            error = null;
            return true;
        }

        private static bool OverlaySupported(AnimolTerrainResolveResult resolved,
            AnimolTerrainPlacement overlay, out string error)
        {
            AnimolTerrainCatalogEntry entry = resolved.entriesById[overlay.catalogId];
            for (int row = 0; row < entry.height; row++)
            {
                long y = (long)overlay.y + entry.height - 1L - row;
                for (int x = 0; x < entry.width; x++)
                {
                    if (entry.supportRows[row][x] != '#') continue;
                    long worldX = (long)overlay.x + x;
                    Vector2Int cell = new Vector2Int((int)worldX, (int)y);
                    AnimolTerrainCellResult support;
                    if (!resolved.solids.TryGetValue(cell, out support) ||
                        support.themeId != overlay.themeId || support.styleId != overlay.styleId)
                        return Fail(overlay.instanceId + ": overlay needs matching theme/style solid support at " + cell, out error);
                    // Canonical mask 255 requires all eight occupied neighbors.
                    // Neighbor material may differ; only the support cell's style
                    // selects this overlay. No visual-alpha inference is involved.
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            long nx = worldX + dx;
                            long ny = y + dy;
                            if (nx < Int32.MinValue || nx > Int32.MaxValue ||
                                ny < Int32.MinValue || ny > Int32.MaxValue ||
                                !resolved.solids.ContainsKey(new Vector2Int((int)nx, (int)ny)))
                                return Fail(overlay.instanceId + ": overlay support must be fully interior (mask 255).", out error);
                        }
                }
            }
            error = null;
            return true;
        }

        private static bool ValidateRows(string[] rows, int width, int height, out string error)
        {
            if (rows == null || rows.Length != height) return Fail("Row count differs from height.", out error);
            foreach (string row in rows)
            {
                if (row == null || row.Length != width) return Fail("Row length differs from width.", out error);
                foreach (char character in row)
                    if (character != '#' && character != '.') return Fail("Only # and . are allowed.", out error);
            }
            error = null;
            return true;
        }

        private static bool ValidFootprint(AnimolTerrainCatalogEntry entry, int x, int y, out string error)
        {
            if (entry.width <= 0 || entry.height <= 0) return Fail("Footprint dimensions must be positive.", out error);
            if ((long)x + entry.width - 1L > Int32.MaxValue || (long)y + entry.height - 1L > Int32.MaxValue)
                return Fail("Footprint exceeds the integer cell coordinate range.", out error);
            error = null;
            return true;
        }

        private static bool FootprintsOverlap(AnimolTerrainCatalogEntry a, AnimolTerrainPlacement pa,
            AnimolTerrainCatalogEntry b, AnimolTerrainPlacement pb)
        {
            return (long)pa.x < (long)pb.x + b.width && (long)pb.x < (long)pa.x + a.width &&
                (long)pa.y < (long)pb.y + b.height && (long)pb.y < (long)pa.y + a.height;
        }

        private static bool KnownStyle(Dictionary<string, AnimolTerrainCatalogEntry> entries,
            string themeId, string styleId)
        {
            if (!HasText(styleId)) return false;
            foreach (AnimolTerrainCatalogEntry entry in entries.Values)
                if (entry.themeId == themeId && entry.styleId == styleId) return true;
            return false;
        }

        private static AnimolTerrainPlacement FindPlacement(AnimolTerrainSavedMap map, string id)
        {
            if (map == null || map.placements == null || id == null) return null;
            foreach (AnimolTerrainPlacement placement in map.placements)
                if (placement != null && placement.instanceId == id) return placement;
            return null;
        }

        private static AnimolTerrainBaseCell FindBaseCell(AnimolTerrainSavedMap map, Vector2Int cell)
        {
            foreach (AnimolTerrainBaseCell item in map.baseCells)
                if (item.x == cell.x && item.y == cell.y) return item;
            return null;
        }

        private static AnimolTerrainPlacement ClonePlacement(AnimolTerrainPlacement source)
        {
            if (source == null) return null;
            return new AnimolTerrainPlacement
            {
                instanceId = source.instanceId, catalogId = source.catalogId,
                catalogVersion = source.catalogVersion, maskHash = source.maskHash,
                themeId = source.themeId, styleId = source.styleId,
                x = source.x, y = source.y, orientation = source.orientation
            };
        }

        private static AnimolTerrainCatalogEntry CloneEntry(AnimolTerrainCatalogEntry source)
        {
            return new AnimolTerrainCatalogEntry
            {
                id = source.id, themeId = source.themeId, styleId = source.styleId,
                frameId = source.frameId, role = source.role, kind = source.kind,
                maskHash = source.maskHash, width = source.width, height = source.height,
                footprintCount = source.footprintCount, solidCount = source.solidCount, voidCount = source.voidCount, supportCount = source.supportCount,
                solidRows = (string[])source.solidRows.Clone(), supportRows = (string[])source.supportRows.Clone()
            };
        }

        private static bool HasText(string value) { return !String.IsNullOrWhiteSpace(value); }

        private static bool IsHash(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (char c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
            return true;
        }

        private static bool Fail(string message, out string error) { error = message; return false; }
    }
}
