using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    [Serializable] public class TheaterPlacement
    {
        public GameObject prefab;
        public Vector2 position;
    }
    // Appearance data only. No walkable flag, devices, turns or rewards.
    [CreateAssetMenu(menuName = "ANIMOL/Memory Theater Visual Layout")]
    public sealed class MemorySkyTheaterLayout : ScriptableObject
    {
        [Tooltip("Adapt border decks and prop placements to current map dimensions. Carpet coordinates stay an independent authored mask.")]
        public bool fitBorderToMap;
        public List<Vector2Int> visualDeck = new List<Vector2Int>();
        public List<Vector2Int> velvet = new List<Vector2Int>();
        public List<TheaterPlacement> props = new List<TheaterPlacement>();
    }
}
