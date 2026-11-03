using UnityEngine;

namespace ANIMOL.Presentation.ShutterOneTileV01
{
    [CreateAssetMenu(menuName = "ANIMOL/One tile shutter art V01")]
    public sealed class ShutterOneTileArtV01 : ScriptableObject
    {
        public Sprite[] horizontal = new Sprite[4];
        public Sprite[] vertical = new Sprite[4];
        public bool Validate(out string error)
        {
            error = null;
            foreach (Sprite[] frames in new[] { horizontal, vertical })
            {
                if (frames == null || frames.Length != 4) { error = "Four shutter frames per direction are required."; return false; }
                foreach (Sprite s in frames)
                    if (s == null || s.rect.width != 256 || s.rect.height != 256 || Mathf.Abs(s.pixelsPerUnit - 256) > .001f
                        || Vector2.Distance(s.pivot, new Vector2(128, 128)) > .001f)
                    { error = "Shutter frames require 256x256, PPU256 and centered pivot."; return false; }
            }
            return true;
        }
    }
}
