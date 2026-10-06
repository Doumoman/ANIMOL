namespace ANIMOL.Editor
{
    // Explicit installation only. Never migrates, saves or selects a user's map.
    public static class FreeShapeArtV6Builder
    {
        public const string Source="Tools/ArtSources/ANIMOL_FreeShape_Sprites_theme_finish_v6";
        public const string Root="Assets/ANIMOL/TerrainFreeShape/V6";
        public const string RegistryPath="Assets/ANIMOL/Resources/ANIMOL_FreeShapeArtV6.asset";
        public static void Initialize()=>FreeShapeArtV2Builder.InitializePackage(Source,Root,RegistryPath,6);
    }
}
