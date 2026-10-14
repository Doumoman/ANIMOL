using UnityEngine;

namespace ANIMOL.Puzzle
{
    public sealed class TwilightCoastInspection : MonoBehaviour
    {
        public Camera lens;
        public PuzzleMap map;
        public TwilightCoastArt art;
        public bool hideBackground = true, environmentEnabled;
        void Start()
        {
            var renderer = GetComponent<TwilightCoastRenderer>(); renderer.environmentEnabled = environmentEnabled; renderer.Bind(map, art);
            var backdrop = gameObject.AddComponent<SkyIslandBackdrop>(); backdrop.hideBackground = hideBackground;
            backdrop.Configure(art.far, art.mid, art.near, art.compositor, lens, art.parallax); backdrop.SetNearAnimation(art.waterRipples);
        }
    }
}
