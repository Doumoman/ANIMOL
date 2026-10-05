using UnityEngine;
using UnityEngine.Tilemaps;

namespace Animol.TerrainStructure
{
    /// <summary>
    /// Attach to the VISUAL Tilemap GameObject. SolidTilemap must be a distinct,
    /// grid-aligned collision/occupancy Tilemap containing only authoritative solid cells.
    /// All materials connect through that one solid grid; Sprite alpha is never queried.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    public sealed class AnimolTerrainContext : MonoBehaviour
    {
        public Tilemap SolidTilemap;
        public int Seed = 1729;

        public bool IsSolid(Vector3Int position)
        {
            return SolidTilemap != null && SolidTilemap.HasTile(position);
        }

        public int GetCanonicalMask(Vector3Int position)
        {
            if (!IsSolid(position)) return 0;
            int raw = 0;
            if (IsSolid(position + new Vector3Int(0, 1, 0))) raw |= AnimolTerrainTopology.N;
            if (IsSolid(position + new Vector3Int(1, 1, 0))) raw |= AnimolTerrainTopology.NE;
            if (IsSolid(position + new Vector3Int(1, 0, 0))) raw |= AnimolTerrainTopology.E;
            if (IsSolid(position + new Vector3Int(1, -1, 0))) raw |= AnimolTerrainTopology.SE;
            if (IsSolid(position + new Vector3Int(0, -1, 0))) raw |= AnimolTerrainTopology.S;
            if (IsSolid(position + new Vector3Int(-1, -1, 0))) raw |= AnimolTerrainTopology.SW;
            if (IsSolid(position + new Vector3Int(-1, 0, 0))) raw |= AnimolTerrainTopology.W;
            if (IsSolid(position + new Vector3Int(-1, 1, 0))) raw |= AnimolTerrainTopology.NW;
            return AnimolTerrainTopology.Canonicalize(raw);
        }

        /// <summary>
        /// Call after a solid-grid edit, even when its visual tile was not edited.
        /// Refreshes the changed cell and the eight possible topology dependants.
        /// Meta-tile footprint reservations must be invalidated separately by their placement system.
        /// </summary>
        public void RefreshAround(Vector3Int changedCell)
        {
            Tilemap visual = GetComponent<Tilemap>();
            if (visual == null) return;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    visual.RefreshTile(changedCell + new Vector3Int(dx, dy, 0));
        }

        public bool Validate(out string error)
        {
            Tilemap visual = GetComponent<Tilemap>();
            if (SolidTilemap == null)
            {
                error = "Assign an authoritative SolidTilemap.";
                return false;
            }
            if (SolidTilemap == visual)
            {
                error = "SolidTilemap must be separate from the visual Tilemap.";
                return false;
            }
            if (visual == null || SolidTilemap.layoutGrid != visual.layoutGrid ||
                SolidTilemap.transform.localToWorldMatrix != visual.transform.localToWorldMatrix ||
                SolidTilemap.cellLayout != visual.cellLayout ||
                SolidTilemap.cellSwizzle != visual.cellSwizzle ||
                SolidTilemap.cellSize != visual.cellSize || SolidTilemap.cellGap != visual.cellGap)
            {
                error = "Solid and visual Tilemaps must share the same Grid and cell transforms.";
                return false;
            }
            error = null;
            return true;
        }
    }
}
