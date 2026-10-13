using System.Collections.Generic;
using UnityEngine;

namespace Animol.TerrainStructure
{
    /// <summary>
    /// Data for one complete large structure. Rows are north/top first; '#': solid, '.': empty.
    /// The full Sprite is decorative. Only Rows may populate the authoritative solid grid.
    /// Local cell (0,0) and the art Sprite pivot are both at the footprint bottom left.
    /// Ownership, reservations, overlap policy and Undo belong to the placement system.
    /// </summary>
    [CreateAssetMenu(fileName = "ANIMOL_TerrainStamp", menuName = "ANIMOL/Terrain Structure/Stamp Definition")]
    public sealed class AnimolTerrainStampDefinition : ScriptableObject
    {
        public string StyleId;
        [Min(1)] public int Width = 8;
        [Min(1)] public int Height = 6;
        [Tooltip("Exactly Height rows, north/top first. '#' is solid; '.' is empty. Never infer this from art alpha.")]
        public string[] Rows = new string[0];
        [Tooltip("Strict 32px-per-cell canvas. Leave empty when SourceArtFrame is assigned.")]
        public Sprite ArtSprite;
        [Tooltip("Optional source-art adapter preserving the shown design at its native pixel dimensions.")]
        public AnimolTerrainArtFrameDefinition SourceArtFrame;

        public Sprite VisualSprite { get { return SourceArtFrame != null ? SourceArtFrame.Sprite : ArtSprite; } }
        public Vector3 VisualOffset { get { return SourceArtFrame != null ? SourceArtFrame.VisualOffset : Vector3.zero; } }
        public float VisualScale { get { return SourceArtFrame != null ? SourceArtFrame.UniformScale : 1f; } }
        public Material VisualMaterial { get { return SourceArtFrame != null ? SourceArtFrame.Material : null; } }

        public bool IsSolidLocal(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height || Rows == null || Rows.Length != Height)
                return false;
            int row = Height - 1 - y;
            return Rows[row] != null && Rows[row].Length == Width && Rows[row][x] == '#';
        }

        /// <summary>Enumerates only explicit solid cells. Validate before mutating any Tilemap.</summary>
        public IEnumerable<Vector3Int> GetSolidCells(Vector3Int bottomLeftOrigin)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (IsSolidLocal(x, y))
                        yield return bottomLeftOrigin + new Vector3Int(x, y, 0);
        }

        /// <summary>Checks shape metadata; no Sprite pixels, physics shapes or alpha are inspected.</summary>
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(StyleId))
            {
                error = "StyleId is required.";
                return false;
            }
            if (Width <= 0 || Height <= 0)
            {
                error = "Width and Height must be positive.";
                return false;
            }
            if (Rows == null || Rows.Length != Height)
            {
                error = "Rows must contain exactly Height north-first rows.";
                return false;
            }
            bool hasSolid = false;
            for (int row = 0; row < Height; row++)
            {
                if (Rows[row] == null || Rows[row].Length != Width)
                {
                    error = "Row " + row + " must contain exactly Width characters.";
                    return false;
                }
                for (int x = 0; x < Width; x++)
                {
                    char cell = Rows[row][x];
                    if (cell != '#' && cell != '.')
                    {
                        error = "Invalid mask character at row " + row + ", column " + x + ": " + cell;
                        return false;
                    }
                    if (cell == '#') hasSolid = true;
                }
            }
            if (!hasSolid)
            {
                error = "A terrain stamp must have at least one explicit solid cell.";
                return false;
            }
            if (ArtSprite != null && SourceArtFrame != null)
            {
                error = "Choose either strict ArtSprite or SourceArtFrame, never both.";
                return false;
            }
            if (SourceArtFrame != null && !SourceArtFrame.ValidateForFootprint(Width, Height, out error))
                return false;
            if (ArtSprite != null)
            {
                if (ArtSprite.rect.width != Width * 32f || ArtSprite.rect.height != Height * 32f ||
                    Mathf.Abs(ArtSprite.pixelsPerUnit - 32f) > 0.001f ||
                    ArtSprite.pivot.sqrMagnitude > 0.000001f)
                {
                    error = "Stamp art must use Width*32 by Height*32 pixels, PPU 32, and bottom-left pivot (0,0).";
                    return false;
                }
            }
            error = null;
            return true;
        }
    }
}
