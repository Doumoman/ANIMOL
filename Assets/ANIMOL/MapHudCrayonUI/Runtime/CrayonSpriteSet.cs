using System;
using UnityEngine;

namespace Animol.MapHudCrayonUI
{
    /// <summary>Three finished, matching PNG poses for one existing visual state.</summary>
    [Serializable]
    public sealed class CrayonSpriteSet
    {
        public Sprite source;
        public Sprite frameA, frameB, frameC;
        public Material animatedMaterial, stillMaterial;
    }
}
