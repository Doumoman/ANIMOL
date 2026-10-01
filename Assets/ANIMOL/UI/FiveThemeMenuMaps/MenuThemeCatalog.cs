using System;
using UnityEngine;

namespace ANIMOL.FiveThemeMenu
{
    public sealed class MenuThemeCatalog : ScriptableObject
    {
        [Serializable] public sealed class Theme
        {
            public string id;
            public Sprite scene, light;
            public int cropTop, footY;
        }
        public Theme[] themes;
        public Sprite[] rabbitFrames;
        public Sprite back, ad, shop, settings, coin, campaign, competition, cooperation;
    }
}
