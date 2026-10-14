using UnityEngine;

namespace ANIMOL.Puzzle
{
    public sealed class ClockDomeInspection : MonoBehaviour
    {
        public Camera lens;
        public PuzzleMap map;
        public ClockDomeArt art;
        public bool hideBackground = true, architectureEnabled;
        public SpriteRenderer[] replacedEnvironment = new SpriteRenderer[0];
        void Start()
        {
            foreach (var image in replacedEnvironment) if (image) image.forceRenderingOff = true;
            var renderer = GetComponent<ClockDomeRenderer>(); renderer.architectureEnabled = architectureEnabled; renderer.Bind(map, art);
            var backdrop = gameObject.AddComponent<SkyIslandBackdrop>(); backdrop.hideBackground = hideBackground;
            backdrop.Configure(art.far, art.mid, art.near, art.compositor, lens, art.parallax);
        }
    }
}
