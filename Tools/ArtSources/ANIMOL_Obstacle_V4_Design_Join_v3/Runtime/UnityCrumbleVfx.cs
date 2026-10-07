using System;
using UnityEngine;

namespace Animol.GraphicJoin
{
    // Native Unity presentation component. The stage's pool and authoritative event adapter own its lifetime.
    // Import the four C05 fragment sprites at PPU32, center pivot. Put this component in an independent VFX root.
    public sealed class UnityCrumbleVfx : MonoBehaviour
    {
        static readonly int[] Starts = { 0, 8, 18, 26 };
        static readonly int[] Widths = { 8, 10, 8, 6 };
        static readonly float[] Vx = { -.10f, -.03f, .04f, .10f };
        static readonly float[] Vy = { -1.2f, -1.6f, -1.35f, -1.8f };
        readonly SpriteRenderer[] pieces = new SpriteRenderer[4];
        Vector3 cellOrigin;
        double startSeconds;
        string eventKey;
        public bool IsPlaying { get; private set; }

        void EnsurePieces()
        {
            for (int i = 0; i < 4; i++) if (pieces[i] == null)
            {
                var child = new GameObject("C05_Fragment_" + i);
                child.transform.SetParent(transform, false);
                pieces[i] = child.AddComponent<SpriteRenderer>();
                pieces[i].enabled = false;
            }
        }

        public bool TryPlay(string uniqueEventKey, Sprite[] sprites, Vector3 worldCellOrigin,
            double eventSeconds, int sortingLayerId, int sortingOrder, Material sharedSpriteMaterial)
        {
            if (string.IsNullOrEmpty(uniqueEventKey) || sprites == null || sprites.Length != 4 ||
                double.IsNaN(eventSeconds) || double.IsInfinity(eventSeconds)) throw new ArgumentException("Valid C05 event and four fragment sprites required");
            if (IsPlaying) return false; // The pool must rent another instance for a simultaneous collapse.
            if (eventKey == uniqueEventKey) return false;
            for (int i = 0; i < 4; i++) if (sprites[i] == null) throw new ArgumentException("Missing fragment sprite");
            EnsurePieces(); eventKey = uniqueEventKey; cellOrigin = worldCellOrigin;
            startSeconds = eventSeconds; IsPlaying = true;
            for (int i = 0; i < 4; i++)
            {
                pieces[i].sprite = sprites[i]; pieces[i].enabled = true;
                pieces[i].sortingLayerID = sortingLayerId; pieces[i].sortingOrder = sortingOrder;
                if (sharedSpriteMaterial != null) pieces[i].sharedMaterial = sharedSpriteMaterial;
                pieces[i].transform.rotation = Quaternion.identity;
                pieces[i].transform.localScale = Vector3.one;
                pieces[i].color = Color.white;
            }
            Sample(eventSeconds); return true;
        }

        // Call from the render adapter with the same game clock used for obstacle state and pause.
        // No collider, Rigidbody2D, game-state write, coroutine allocation, or per-frame Sprite.Create.
        public void Sample(double gameSeconds)
        {
            if (!IsPlaying) return;
            if (double.IsNaN(gameSeconds) || double.IsInfinity(gameSeconds)) throw new ArgumentException("Valid game time required");
            float t = Mathf.Max(0, (float)(gameSeconds - startSeconds));
            if (t >= .4f) { Stop(); return; }
            float alpha = t <= .08f ? 1 : Mathf.Clamp01(1 - (t - .08f) / .32f);
            for (int i = 0; i < 4; i++)
            {
                float x = cellOrigin.x + (Starts[i] + Widths[i] * .5f) / 32f + Mathf.Round(Vx[i] * t * 32) / 32f;
                float y = cellOrigin.y + 1 - 4f / 32f + Mathf.Round((Vy[i] * t - 2.5f * t * t) * 32) / 32f;
                pieces[i].transform.position = new Vector3(x, y, cellOrigin.z);
                pieces[i].color = new Color(1, 1, 1, alpha);
            }
        }

        public void Stop()
        {
            IsPlaying = false;
            for (int i = 0; i < 4; i++) if (pieces[i] != null) pieces[i].enabled = false;
        }
        void OnDisable() { Stop(); }
    }
}
