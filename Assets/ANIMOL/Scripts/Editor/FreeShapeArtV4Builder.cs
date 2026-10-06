using UnityEditor;

namespace ANIMOL.Editor
{
    public static class FreeShapeArtV4Builder
    {
        public const string Source="Tools/ArtSources/ANIMOL_FreeShape_Sprites_joint_finish_v4";
        public const string Root="Assets/ANIMOL/TerrainFreeShape/V4";
        public const string RegistryPath="Assets/ANIMOL/Resources/ANIMOL_FreeShapeArtV4.asset";

        [MenuItem("ANIMOL/Terrain Free Shape/Initialize Joint Finish Art V4")]
        public static void Initialize()=>FreeShapeArtV2Builder.InitializePackage(Source,Root,RegistryPath,4);
    }
}
