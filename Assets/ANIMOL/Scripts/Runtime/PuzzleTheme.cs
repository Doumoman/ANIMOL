using System;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    [Serializable] public class TerrainArt
    {
        public TerrainMaterial material;
        [Tooltip("Exposure masks N=1 E=2 S=4 W=8; exactly 16 entries.")] public Sprite[] outer = new Sprite[16];
        [Tooltip("Full-cell sprites with an inset corner NE, SE, SW, NW. Only the affected quadrant replaces the outer tile; alpha cuts remain transparent.")] public Sprite[] inner = new Sprite[4];
    }
    [CreateAssetMenu(menuName = "ANIMOL/Puzzle Theme")]
    public sealed class PuzzleTheme : ScriptableObject
    {
        public SkyIslandArt skyIsland;
        public StarwayRanchArt starwayRanch;
        public ANIMOL.Art.RanchConnectedDecorV01.RanchConnectedDecorV01Catalog ranchConnectedDecorV01Catalog;
        public TextAsset ranchConnectedDecorV01ManifestPin;
        public MemorySkyTheaterArt memorySkyTheater;
        public ANIMOL.Art.TheaterConnectedDecorV01.TheaterConnectedDecorV01Catalog theaterConnectedDecorV01Catalog;
        public TextAsset theaterConnectedDecorV01ManifestPin;
        public ClockDomeArt clockDome;
        public ANIMOL.Art.SunsetSanctumV02.SunsetSanctumV02VisualCatalog sunsetSanctumV02Catalog;
        public ANIMOL.Art.SanctumConnectedDecorV01.SanctumConnectedDecorV01Catalog sanctumConnectedDecorV01Catalog;
        public TextAsset sanctumConnectedDecorV01ManifestPin;
        public ANIMOL.Art.CoastConnectedDecorV01.CoastConnectedDecorV01Catalog coastConnectedDecorV01Catalog;
        public TextAsset coastConnectedDecorV01ManifestPin;
        public Material sunsetSanctumV02BeamMaterial;
        public TextAsset sunsetSanctumV02BackgroundLayout;
        public TwilightCoastArt twilightCoast;
        public ANIMOL.Art.StarSwellCoastV01.StarSwellCoastV01VisualCatalog starSwellCoastV01Catalog;
        public TextAsset starSwellCoastV01BackgroundLayout;
        public TerrainArt[] terrain;
        public Sprite player, bubble, exit, decoration;
        public ANIMOL.Presentation.StarwayCharacterCatalog starwayCharacters;
        public string starwayCharacterId;
        public ANIMOL.Presentation.StarwayCharacterCatalog memoryTheaterCharacters;
        public string memoryTheaterCharacterId;
        public ANIMOL.Presentation.StarwayCharacterCatalog twilightSeaCharacters;
        public string twilightSeaCharacterId;
        public string twilightSeaCostumeId;
        public ANIMOL.Presentation.StarwayCharacterCatalog letterObservatoryCharacters;
        public string letterObservatoryCharacterId;
        public ANIMOL.Presentation.StarwayCharacterCatalog CharacterCatalog =>
            letterObservatoryCharacters ? letterObservatoryCharacters :
            twilightSeaCharacters ? twilightSeaCharacters :
            memoryTheaterCharacters ? memoryTheaterCharacters : starwayCharacters;
        public string CharacterId => letterObservatoryCharacters ? letterObservatoryCharacterId :
            twilightSeaCharacters ? twilightSeaCharacterId :
            memoryTheaterCharacters ? memoryTheaterCharacterId : starwayCharacterId;
        public string CharacterCostumeId => letterObservatoryCharacters ? null :
            twilightSeaCharacters ? twilightSeaCostumeId : null;
        public Sprite far, mid, near;
        public Color background = new Color(.045f, .08f, .14f);
        public TerrainArt Art(TerrainMaterial material) => Array.Find(terrain ?? Array.Empty<TerrainArt>(), a => a.material == material);
    }

    public readonly struct TerrainShape
    {
        public readonly int exposure, innerCorners;
        public TerrainShape(int e, int i) { exposure = e; innerCorners = i; }
    }
    public static class PuzzleAutotile
    {
        public static readonly Vector2Int[] Cardinal = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        public static readonly Vector2Int[] Diagonal = { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1) };
        public static TerrainShape Resolve(PuzzleMap map, Vector2Int p)
        {
            if (!map.TryTerrain(p, out var cell)) return default;
            bool Same(Vector2Int q) => map.TryTerrain(q, out var t) && t.material == cell.material;
            var exposure = 0; var inner = 0;
            for (var i = 0; i < 4; i++) if (!Same(p + Cardinal[i])) exposure |= 1 << i;
            for (var i = 0; i < 4; i++)
                if (Same(p + Cardinal[i]) && Same(p + Cardinal[(i + 1) % 4]) && !Same(p + Diagonal[i])) inner |= 1 << i;
            return new TerrainShape(exposure, inner);
        }
    }
    public static class PuzzleTerrainSprites
    {
        static readonly System.Collections.Generic.Dictionary<string, Sprite> quadrants = new System.Collections.Generic.Dictionary<string, Sprite>();
        public static Sprite Quadrant(Sprite source, int corner)
        {
            if (!source) return null;
            var key = source.GetInstanceID() + ":" + corner;
            if (quadrants.TryGetValue(key, out var cached) && cached) return cached;
            var rect = source.rect; rect.width *= .5f; rect.height *= .5f;
            if (corner == 0 || corner == 1) rect.x += rect.width;
            if (corner == 0 || corner == 3) rect.y += rect.height;
            var sprite = Sprite.Create(source.texture, rect, new Vector2(.5f, .5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave; quadrants[key] = sprite; return sprite;
        }
        public static Vector2 Offset(int corner) => new Vector2(corner == 0 || corner == 1 ? .25f : -.25f, corner == 0 || corner == 3 ? .25f : -.25f);
    }
}
