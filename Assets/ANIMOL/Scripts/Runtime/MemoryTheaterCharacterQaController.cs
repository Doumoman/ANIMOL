using UnityEngine;

namespace ANIMOL.Presentation
{
    // Preview only; puzzle state remains owned by the existing controllers.
    public sealed class MemoryTheaterCharacterQaController : MonoBehaviour
    {
        public StarwayCharacterCatalog catalog;
        public StarwayCharacterVisual character;
        static readonly string[] Directions = { "front", "back", "side", "left" };
        static readonly string[] States = { "idle", "move", "sliding", "jump", "victory" };

        void Start()
        {
            if (catalog && character) character.Initialize(catalog, catalog.defaultCharacterId);
        }

        public bool Preview(string id, string direction, string state)
        {
            if (!catalog || !character || catalog.Find(id) == null) return false;
            if (character.characterId != id && !character.Initialize(catalog, id)) return false;
            return character.PlayPose(direction, state);
        }

        void OnGUI()
        {
            if (!catalog || !character) return;
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1080f, Screen.width / 1080f, 1));
            GUI.Label(new Rect(30, 20, 950, 50), "MEMORY THEATER CHARACTER ART QA - preview only");
            for (var i = 0; i < catalog.characters.Length; i++)
            {
                var entry = catalog.characters[i];
                if (GUI.Button(new Rect(30 + i * 255, 80, 240, 65), entry.displayName))
                    Preview(entry.id, character.Direction, character.State);
            }
            for (var i = 0; i < Directions.Length; i++)
                if (GUI.Button(new Rect(30 + i * 255, 160, 240, 65), Directions[i]))
                    Preview(character.characterId, Directions[i], character.State);
            for (var i = 0; i < States.Length; i++)
                if (GUI.Button(new Rect(30 + i * 205, 240, 190, 65), States[i]))
                    Preview(character.characterId, character.Direction, States[i]);
            GUI.Label(new Rect(30, 320, 650, 45), character.characterId + " / " + character.Direction + " / " + character.State);
            var illustration = character.Illustration;
            if (illustration) GUI.DrawTexture(new Rect(650, 390, 390, 540), illustration.texture, ScaleMode.ScaleToFit, true);
            GUI.matrix = previous;
        }
    }
}
