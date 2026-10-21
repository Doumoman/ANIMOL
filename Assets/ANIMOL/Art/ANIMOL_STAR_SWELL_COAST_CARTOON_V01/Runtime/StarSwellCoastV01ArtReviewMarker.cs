using UnityEngine;

namespace ANIMOL.Art.StarSwellCoastV01
{
    /// <summary>
    /// Marks an isolated, generated ART REVIEW scene. This is not a game mode,
    /// simulation adapter, spawn root or proof that production scenes were migrated.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StarSwellCoastV01ArtReviewMarker : MonoBehaviour
    {
        [SerializeField] private bool artReviewOnly;
        [SerializeField] private string sourceLayoutSha256;
        [SerializeField] private string pinnedCatalogSha256;

        public bool IsArtReviewOnly { get { return artReviewOnly; } }
        public string SourceLayoutSha256 { get { return sourceLayoutSha256; } }
        public string PinnedCatalogSha256 { get { return pinnedCatalogSha256; } }

        public void ConfigureForArtReview(string layoutSha256, string catalogSha256)
        {
            sourceLayoutSha256 = layoutSha256;
            pinnedCatalogSha256 = catalogSha256;
            artReviewOnly = true;
        }
    }
}
