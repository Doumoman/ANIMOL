using UnityEngine;

namespace ANIMOL.FiveThemeMenu
{
    /// <summary>Current lobby controls only; backgrounds belong exclusively to the v7 catalog.</summary>
    public sealed class MenuThemeCatalog : ScriptableObject
    {
        public Sprite back, ad, shop, settings, coin, campaign, competition, cooperation;
    }
}
