using UnityEngine;
using TMPro;

namespace ANIMOL.Menu
{
    // Presentation data only. The lobby controller remains the sole owner of navigation and progress.
    [CreateAssetMenu(menuName = "ANIMOL/Menu/Skin")]
    public sealed class MainMenuSkin : ScriptableObject
    {
        public TMP_FontAsset font;
        public Sprite tab;
        public Sprite selectedTab;
        public Sprite disabledTab;
        public Sprite primary;
        public Sprite profile;
        public Sprite utility;
        public Sprite dialog;
        public Sprite card;
        public Sprite ad;
        public Sprite title;
    }

}
