using UnityEngine;

namespace ANIMOL.FiveThemeMenu
{
    public sealed class FantasyBackgroundCatalog : ScriptableObject
    {
        [System.Serializable] public sealed class Theme { public Texture2D far, mid, platform, near; }
        public string sourceVersion;
        public Theme[] themes;
        public Texture2D rabbitRight, rabbitLeft;
        public Shader compositor;
    }
}
