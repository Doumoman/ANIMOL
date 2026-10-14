using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Art-only reference fixture, explicitly separate from the playable puzzle.
    public sealed class SkyIslandInspection : MonoBehaviour
    {
        public PuzzleMap map;
        public SkyIslandArt art;
        void Start()
        {
            var renderer = GetComponent<SkyIslandMapRenderer>(); renderer.Bind(map, art); renderer.PopulateAll();
            var backdrop = new GameObject("Inspection pixel presentation").AddComponent<SkyIslandBackdrop>(); backdrop.Configure(art, Camera.main); backdrop.hideBackground = true;
        }
    }
}
