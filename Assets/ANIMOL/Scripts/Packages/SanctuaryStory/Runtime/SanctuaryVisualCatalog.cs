using System;
using UnityEngine;

namespace ANIMOL.SanctuaryStory
{
    [Serializable]
    public sealed class SanctuaryExpressionVisual
    {
        public string id;
        public Sprite sprite;
        public bool hasContentBounds;
        // Optional shared foot-center registration; arm width must not move the body.
        public bool hasRegistrationX;
        public float registrationX;
        // Normalized alpha-content bounds, with Y measured from the image bottom.
        public float contentMinX, contentMinY, contentMaxX = 1f, contentMaxY = 1f;
    }

    [Serializable]
    public sealed class SanctuaryBackgroundVisual
    {
        public string id;
        public Sprite sprite;
    }

    [Serializable]
    public sealed class SanctuaryCharacterVisual
    {
        public string characterId;
        [Tooltip("Optional production reference. Runtime animation uses the approved frame arrays below.")]
        public Sprite portrait;
        public Sprite[] idleFrames;
        public Sprite[] moveFrames;
        public Sprite[] jumpFrames;
        [Tooltip("A genuine raised-hand pose is required for the Watcher; do not copy the idle reference here.")]
        public Sprite[] prepareFrames;
        public Sprite[] lowerFrames;
        [Tooltip("Optional approved look-at-hand pose. The full idle silhouette is retained when absent.")]
        public Sprite[] boundaryFrames;
        public SanctuaryExpressionVisual[] expressionVariants;
        [Min(1f)] public float framesPerSecond = 8f;
        [Min(120f)] public float designHeight = 480f;

        public Sprite FirstIdle
        {
            get
            {
                if (idleFrames != null)
                    foreach (Sprite sprite in idleFrames) if (sprite != null) return sprite;
                return null;
            }
        }

        public SanctuaryExpressionVisual FindExpression(string id)
        {
            string key = id == "idle" ? "neutral" : id;
            if (expressionVariants != null)
                foreach (SanctuaryExpressionVisual expression in expressionVariants)
                    if (expression != null && string.Equals(expression.id, key, StringComparison.Ordinal)) return expression;
            return null;
        }

        public Sprite ResolveExpression(string id)
        {
            SanctuaryExpressionVisual expression = FindExpression(id);
            if (expression != null) return expression.sprite;
            if (id == "neutral" || id == "idle") return FirstIdle;
            if (id == "prepare" && prepareFrames != null && prepareFrames.Length > 0) return prepareFrames[0];
            return null; // Explicit authored expressions must never quietly turn into neutral.
        }

        public SanctuaryExpressionVisual FindSpriteMetadata(Sprite sprite)
        {
            if (expressionVariants != null)
                foreach (SanctuaryExpressionVisual expression in expressionVariants)
                    if (expression != null && expression.sprite == sprite) return expression;
            return null;
        }
    }

    /// <summary>Direct Sprite references: no Resources, Addressables or runtime file-path guesses.</summary>
    [CreateAssetMenu(menuName = "ANIMOL/Sanctuary Story/Visual Catalog", fileName = "SanctuaryVisualCatalog")]
    public sealed class SanctuaryVisualCatalog : ScriptableObject
    {
        public Sprite laboratoryBackground;
        public Sprite chaosBackground;
        public SanctuaryBackgroundVisual[] backgroundVariants;
        public SanctuaryCharacterVisual[] characters;
        [Header("Optional existing project audio; this package does not synthesize clips")]
        public AudioClip platformContactSound;
        public AudioClip rewindFoldSound;

        public SanctuaryCharacterVisual FindCharacter(string characterId)
        {
            if (characters == null) return null;
            foreach (SanctuaryCharacterVisual character in characters)
                if (character != null && string.Equals(character.characterId, characterId, StringComparison.Ordinal))
                    return character;
            return null;
        }

        public Sprite FindBackground(string id)
        {
            if (backgroundVariants != null)
                foreach (SanctuaryBackgroundVisual background in backgroundVariants)
                    if (background != null && string.Equals(background.id, id, StringComparison.Ordinal)) return background.sprite;
            if (id == "lab") return laboratoryBackground;
            if (id == "chaos") return chaosBackground;
            return null;
        }
    }
}
