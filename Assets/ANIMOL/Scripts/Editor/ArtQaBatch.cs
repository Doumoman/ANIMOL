namespace ANIMOL.Presentation.Editor
{
    public static class ArtQaBatch
    {
        public static void ApplyAndCapture()
        {
            ArtAssetBuilder.Build();
            ArtAssetBuilder.Capture();
        }
    }
}
