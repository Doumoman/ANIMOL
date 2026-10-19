using UnityEngine;

namespace ANIMOL.Puzzle
{
    public sealed class MemorySkyTheaterInspection : MonoBehaviour
    {
        public Camera lens;
        public PuzzleMap map;
        public MemorySkyTheaterArt art;
        public MemorySkyTheaterLayout layout;
        public bool hideBackground = true;
        public SpriteRenderer[] replacedEnvironment = new SpriteRenderer[0];
        void Start()
        {
            // forceRenderingOff is runtime-only; keep explicit scene references across reloads.
            foreach (var image in replacedEnvironment) if (image) image.forceRenderingOff = true;
            if (art && art.v02)
            {
                foreach (var old in GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>()) old.enabled = false;
                gameObject.AddComponent<EchoTheaterV02MapRenderer>().Bind(map, art.v02, layout);
                gameObject.AddComponent<EchoTheaterV02Background>().Configure(art.v02, lens);
                return;
            }
            var renderer = GetComponent<MemorySkyTheaterRenderer>(); renderer.Bind(map, art, layout);
            var backdrop = gameObject.AddComponent<SkyIslandBackdrop>();
            backdrop.hideBackground = hideBackground;
            backdrop.Configure(art.far, art.mid, art.near, art.compositor, lens);
        }
    }
}
