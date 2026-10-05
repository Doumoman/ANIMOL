using UnityEngine;
using UnityEngine.Tilemaps;

namespace Animol.TerrainStructure
{
    /// <summary>
    /// One immutable style family reused by every painted cell. Cell appearance is calculated
    /// from context; no per-cell writes or runtime cloning of this ScriptableObject are needed.
    /// This visual TileBase always supplies colliderType None.
    /// </summary>
    [CreateAssetMenu(fileName = "ANIMOL_TerrainStyle", menuName = "ANIMOL/Terrain Structure/Style Tile")]
    public sealed class AnimolTerrainTile : TileBase
    {
        public string StyleId;
        [Tooltip("Exactly 47 sprites ordered by ascending AnimolTerrainTopology.CanonicalMasks.")]
        public Sprite[] ShapeSprites = new Sprite[AnimolTerrainTopology.ShapeCount];
        [Tooltip("Four optional full-cell interior variants; empty entries fall back to canonical mask 255.")]
        public Sprite[] InteriorSprites = new Sprite[4];

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = null;
            tileData.gameObject = null;
            tileData.color = Color.white;
            tileData.transform = Matrix4x4.identity;
            tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;
            tileData.colliderType = Tile.ColliderType.None;

            AnimolTerrainContext context = tilemap.GetComponent<AnimolTerrainContext>();
            // Do not infer solids from visual tiles, material identity or Sprite alpha.
            if (context == null || !context.IsSolid(position)) return;

            int mask = context.GetCanonicalMask(position);
            int index = AnimolTerrainTopology.IndexOfMask(mask);
            if (ShapeSprites != null && index >= 0 && index < ShapeSprites.Length)
                tileData.sprite = ShapeSprites[index];

            if (mask == AnimolTerrainTopology.InteriorMask &&
                InteriorSprites != null && InteriorSprites.Length == 4)
            {
                int variant = AnimolTerrainTopology.ChooseInteriorVariant(
                    position.x, position.y, context.Seed, InteriorSprites.Length, StyleId);
                if (InteriorSprites[variant] != null)
                    tileData.sprite = InteriorSprites[variant];
            }
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            // Refresh removed-cell neighbours as well; do not require the centre to still be painted.
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    tilemap.RefreshTile(position + new Vector3Int(dx, dy, 0));
        }

        /// <summary>Read-only validation; does not resize arrays or alter referenced family assets.</summary>
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(StyleId))
            {
                error = "StyleId is required.";
                return false;
            }
            if (ShapeSprites == null || ShapeSprites.Length != AnimolTerrainTopology.ShapeCount)
            {
                error = "ShapeSprites must contain exactly 47 slots.";
                return false;
            }
            for (int i = 0; i < ShapeSprites.Length; i++)
            {
                if (ShapeSprites[i] == null)
                {
                    error = "Missing shape sprite at slot " + i + ", mask " + AnimolTerrainTopology.CanonicalMasks[i];
                    return false;
                }
                if (!IsCellSprite(ShapeSprites[i]))
                {
                    error = "Shape slot " + i + " must be 32x32 pixels, PPU 32, centre pivot.";
                    return false;
                }
            }
            if (InteriorSprites != null && InteriorSprites.Length != 0 && InteriorSprites.Length != 4)
            {
                error = "InteriorSprites must be empty or contain exactly four slots.";
                return false;
            }
            if (InteriorSprites != null)
                for (int i = 0; i < InteriorSprites.Length; i++)
                    if (InteriorSprites[i] != null && !IsCellSprite(InteriorSprites[i]))
                    {
                        error = "Interior slot " + i + " must be 32x32 pixels, PPU 32, centre pivot.";
                        return false;
                    }
            error = null;
            return true;
        }
        private static bool IsCellSprite(Sprite sprite)
        {
            return sprite.rect.width == 32f && sprite.rect.height == 32f &&
                Mathf.Abs(sprite.pixelsPerUnit - 32f) < 0.001f &&
                (sprite.pivot - new Vector2(16f,16f)).sqrMagnitude < 0.000001f;
        }
    }
}
