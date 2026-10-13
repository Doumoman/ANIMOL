using System;
using UnityEngine;

namespace Animol.QA
{
    // The fixed QA fixture has one C05. Production scenes use their existing visual resolver/dirty path.
    public sealed class UnityTemporaryStageVisuals2D : MonoBehaviour
    {
        [Serializable] public sealed class Entry
        {
            public SpriteRenderer Body, Cap;
            public Sprite InitialBody, InitialCap, CollapsedBody, CollapsedCap;
        }
        public UnityTemporaryRun2D Run;
        public Entry[] Entries;
        void OnEnable() { if (Run != null) Run.CrumbleSurfaceChanged += Refresh; }
        void OnDisable() { if (Run != null) Run.CrumbleSurfaceChanged -= Refresh; }
        void Refresh(bool enabled)
        {
            foreach (var e in Entries ?? Array.Empty<Entry>())
            {
                if (e.Body != null) { e.Body.sprite = enabled ? e.InitialBody : e.CollapsedBody; e.Body.enabled = e.Body.sprite != null; }
                if (e.Cap != null) { e.Cap.sprite = enabled ? e.InitialCap : e.CollapsedCap; e.Cap.enabled = e.Cap.sprite != null; }
            }
        }
    }
}
