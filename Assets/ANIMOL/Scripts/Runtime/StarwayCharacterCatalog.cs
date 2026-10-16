using System;
using UnityEngine;

namespace ANIMOL.Presentation
{
    [Serializable]
    public sealed class StarwayCharacterMotion
    {
        public AnimationClip clip;
        public Sprite[] frames = new Sprite[6];
        public float fps;
        public bool loop;
    }

    [Serializable]
    public sealed class StarwayCharacterDefinition
    {
        public string id;
        public string displayName;
        public Sprite illustration;
        public StarwayCharacterMotion[] motions = new StarwayCharacterMotion[15];

        public StarwayCharacterMotion Motion(string direction, string state)
        {
            var name = id + "_" + direction + "_" + state;
            return Array.Find(motions, motion => motion != null && motion.clip && motion.clip.name == name);
        }

        public AnimationClip Clip(string direction, string state) => Motion(direction, state)?.clip;
        public Sprite FirstFrame(string direction, string state) => Motion(direction, state)?.frames[0];
    }

    [CreateAssetMenu(menuName = "ANIMOL/Art/Starway Character Catalog")]
    public sealed class StarwayCharacterCatalog : ScriptableObject
    {
        public string defaultCharacterId = "choa";
        public StarwayCharacterDefinition[] characters = Array.Empty<StarwayCharacterDefinition>();

        public StarwayCharacterDefinition Find(string id)
        {
            return Array.Find(characters, item => item != null && item.id == id);
        }

        public StarwayCharacterDefinition Default => Find(defaultCharacterId);
    }
}
