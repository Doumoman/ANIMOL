using System;
using UnityEngine;

namespace William.NotebookUI
{
    /// <summary>Visual replacement only. Keep the game's existing collision and collection authority.</summary>
    [DisallowMultipleComponent]
    public sealed class NotebookGameplayWorldStar : MonoBehaviour
    {
        public SpriteRenderer target;
        public Sprite[] frames = Array.Empty<Sprite>();
        [Range(1, 12)] public float framesPerSecond = 4;
        private float age;
        private bool consumed;
        private void OnEnable() { age = 0; consumed = false; if (target) target.enabled = true; Refresh(); }
        private void Update() { if (!consumed) { age += Time.deltaTime; Refresh(); } }
        public void Configure(SpriteRenderer renderer, Sprite[] fourFrames)
        {
            target = renderer; frames = fourFrames ?? Array.Empty<Sprite>(); age = 0;
            if (target) target.color = Color.white;
            Refresh();
        }
        private void Refresh()
        {
            if (target && frames != null && frames.Length > 0)
                target.sprite = frames[Mathf.FloorToInt(age * framesPerSecond) % frames.Length];
        }
        /// <summary>Call after the game confirms collection. This class never grants a star or removes a collider.</summary>
        public bool HideAfterConfirmedCollection()
        {
            if (consumed) return false;
            consumed = true;
            if (target) target.enabled = false;
            return true;
        }
        public void ResetVisual()
        {
            consumed = false; age = 0;
            if (target) target.enabled = true;
            Refresh();
        }
    }
}
