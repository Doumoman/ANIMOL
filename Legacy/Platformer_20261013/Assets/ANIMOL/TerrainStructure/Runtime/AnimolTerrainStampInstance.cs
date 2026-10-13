using System.Collections.Generic;
using UnityEngine;

namespace Animol.TerrainStructure
{
    /// <summary>
    /// Scene-level ownership for a static DevLab stamp, separate from its reusable definition.
    /// OwnedSolidCells is the exact placement record used by the Editor's removal/Undo transaction.
    /// The whole rectangular footprint is reserved, including empty arches and decorative cells.
    /// This component does not mutate Tilemaps, infer collision or generate an owner ID at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimolTerrainStampInstance : MonoBehaviour
    {
        public string OwnerId;
        public AnimolTerrainStampDefinition Definition;
        public Vector3Int Origin;
        public Vector3Int[] OwnedSolidCells = new Vector3Int[0];
        [Tooltip("Placement record: true only when this instance owns a visible full-stamp SpriteRenderer.")]
        public bool OwnsVisual;
        [Tooltip("Placement record for the child Art renderer; independent of logical root transforms.")]
        public SpriteRenderer ArtRenderer;
        [Tooltip("Shared material applied at placement. Reading/writing Renderer.material would clone it.")]
        public Material AppliedVisualMaterial;

        public bool IsReserved(Vector3Int cell)
        {
            if (Definition == null || Definition.Width <= 0 || Definition.Height <= 0 || cell.z != Origin.z)
                return false;
            long localX = (long)cell.x - Origin.x;
            long localY = (long)cell.y - Origin.y;
            return localX >= 0 && localX < Definition.Width && localY >= 0 && localY < Definition.Height;
        }

        public IEnumerable<Vector3Int> GetReservedCells()
        {
            if (Definition == null || Definition.Width <= 0 || Definition.Height <= 0)
                yield break;
            for (int y = 0; y < Definition.Height; y++)
                for (int x = 0; x < Definition.Width; x++)
                    yield return new Vector3Int(checked(Origin.x + x), checked(Origin.y + y), Origin.z);
        }

        /// <summary>Read-only ownership validation; placement records must not contain duplicates.</summary>
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(OwnerId))
            {
                error = "A stable unique OwnerId is required for a placed stamp.";
                return false;
            }
            if (Definition == null)
            {
                error = "A stamp Definition is required.";
                return false;
            }
            if (!Definition.Validate(out error)) return false;
            if (OwnedSolidCells == null)
            {
                error = "OwnedSolidCells must be an explicit placement record.";
                return false;
            }
            HashSet<Vector3Int> seen = new HashSet<Vector3Int>();
            for (int i = 0; i < OwnedSolidCells.Length; i++)
            {
                Vector3Int cell = OwnedSolidCells[i];
                if (!IsReserved(cell) || !Definition.IsSolidLocal(cell.x - Origin.x, cell.y - Origin.y))
                {
                    error = "Owned cell is outside the definition's explicit solid mask: " + cell;
                    return false;
                }
                if (!seen.Add(cell))
                {
                    error = "Duplicate owned solid cell: " + cell;
                    return false;
                }
            }
            foreach (Vector3Int cell in Definition.GetSolidCells(Origin))
            {
                if (!seen.Contains(cell))
                {
                    error = "Missing solid cell in ownership placement record: " + cell;
                    return false;
                }
            }
            error = null;
            return true;
        }
    }
}
