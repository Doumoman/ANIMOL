using ANIMOL.Typography;
using UnityEngine;

namespace ANIMOL.MissingUiV1.Project
{
    public sealed class MissingUiArt : ScriptableObject
    {
        public PixelTypographyProfile Typography;
        public Font LegacyFont;
        public Sprite Panel, Primary, Secondary, Back, Row, Product, Price, Growth, Emotes, Restore, ScrollTrack, ScrollThumb;
    }
}
