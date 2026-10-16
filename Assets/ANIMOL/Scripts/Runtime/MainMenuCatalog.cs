using System;
using ANIMOL.Presentation;
using UnityEngine;

namespace ANIMOL.Menu
{
    [Serializable]
    public sealed class MainMenuAppearance
    {
        public string artId;
        public string costumeId;
        public Sprite illustration;
    }

    [Serializable]
    public sealed class MainMenuPerson
    {
        // This is the saved/game character ID. artId may be a costume set such as edenpuppy.
        public string characterId;
        public MainMenuAppearance[] appearances = Array.Empty<MainMenuAppearance>();
    }

    [Serializable]
    public sealed class MainMenuTheme
    {
        public string id;
        public string displayName;
        public Sprite scenery;
        public StarwayCharacterCatalog gameCharacters;
        public MainMenuPerson[] people = Array.Empty<MainMenuPerson>();
    }

    [CreateAssetMenu(menuName = "ANIMOL/Menu/Art Catalog")]
    public sealed class MainMenuCatalog : ScriptableObject
    {
        public MainMenuTheme[] themes = Array.Empty<MainMenuTheme>();
        // No authoritative campaign required-stage catalog exists yet. Empty means locked.
        public string[] requiredStageIds = Array.Empty<string>();

        public MainMenuPerson FindPerson(int themeIndex, string characterId)
        {
            if (themes == null || themeIndex < 0 || themeIndex >= themes.Length) return null;
            return Array.Find(themes[themeIndex].people, item => item != null && item.characterId == characterId);
        }
    }

    // An actual progress service can implement this when required stage IDs are defined.
    // Unknown, failure, or absent data always keeps battle locked.
    public interface IRequiredStageProgress
    {
        bool TryGetCleared(string stageId, out bool cleared);
    }
}
