using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Social/Emote Catalog", fileName = "EmoteCatalog")]
    public sealed class EmoteCatalog : ScriptableObject
    {
        [SerializeField] private int version = 1;
        [SerializeField] private EmoteDefinition[] emotes = Array.Empty<EmoteDefinition>();
        public int Version => version;
        public IReadOnlyList<EmoteDefinition> Emotes => emotes;
    }
}
