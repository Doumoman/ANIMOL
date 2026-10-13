using System.Collections.Generic;
using Animol.GraphicJoin;
using UnityEngine;

namespace Animol.QA
{
    public sealed class UnityTemporaryObstacle2D : MonoBehaviour
    {
        public string Kind, Facing = "UP";
        public UnityTemporaryRun2D Run;
        public Collider2D Surface;
        public SpriteRenderer Overlay;
        public Sprite IdleSprite, LoadedSprite;
        public Sprite[] ActiveFrames, CrumblePieces;
        public Vector2 CellOrigin;
        public UnityCrumbleVfx CrumbleVfx;
        public bool CanDropThrough => Kind == "C01" || Kind == "C05" || Kind == "C06";
        public bool SurfaceEnabled { get; private set; } = true;
        readonly HashSet<int> supportedPlayers = new HashSet<int>();
        double pressedAt = -100;
        int generation;
        bool pressed;

        public void Press()
        {
            if (Run == null || Run.IsResetting) return;
            pressedAt = Run.Elapsed; pressed = true;
        }
        void Update()
        {
            if (Run == null) return;
            if (CrumbleVfx != null) CrumbleVfx.Sample(Run.Elapsed);
            if (Overlay == null || !SurfaceEnabled) return;
            if (Kind == "C08" && ActiveFrames != null && ActiveFrames.Length > 0)
                Overlay.sprite = ActiveFrames[(int)(Run.Elapsed / .08) % ActiveFrames.Length];
            else if (pressed && ActiveFrames != null && ActiveFrames.Length > 0)
            {
                double dt = Kind == "C06" ? .04 : .03;
                int frame = (int)((Run.Elapsed - pressedAt) / dt);
                if (frame >= ActiveFrames.Length) { pressed = false; Overlay.sprite = IdleSprite; }
                else Overlay.sprite = ActiveFrames[Mathf.Clamp(frame, 0, ActiveFrames.Length - 1)];
            }
        }
        public void PlayerSupported(UnityTemporaryPlayer2D player)
        {
            if (Kind != "C05" || !SurfaceEnabled || Run == null || Run.IsResetting || player.Run != Run) return;
            supportedPlayers.Add(player.GetInstanceID());
            if (Overlay != null && LoadedSprite != null) Overlay.sprite = LoadedSprite;
        }
        void OnCollisionExit2D(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<UnityTemporaryPlayer2D>();
            if (Kind != "C05" || player == null || !supportedPlayers.Remove(player.GetInstanceID()) || supportedPlayers.Count != 0 || Run == null || Run.IsResetting) return;
            SurfaceEnabled = false;
            if (Surface != null) Surface.enabled = false;
            if (Overlay != null) Overlay.enabled = false;
            Run.NotifyCrumbleSurface(false);
            if (CrumbleVfx != null && Overlay != null && CrumblePieces != null && CrumblePieces.Length == 4)
                CrumbleVfx.TryPlay(GetInstanceID() + ":" + generation, CrumblePieces, CellOrigin,
                    Run.Elapsed, Overlay.sortingLayerID, Overlay.sortingOrder, Overlay.sharedMaterial);
        }
        void OnTriggerEnter2D(Collider2D other) { Touch(other); }
        void OnTriggerStay2D(Collider2D other) { Touch(other); }
        void Touch(Collider2D other)
        {
            var player = other.GetComponentInParent<UnityTemporaryPlayer2D>();
            if (player == null || Run == null || player.Run != Run || Run.IsResetting || Run.IsCleared) return;
            if (Kind == "C04") Run.ResetRun();
            else if (Kind == "C07") player.EnterWind(this);
        }
        void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponentInParent<UnityTemporaryPlayer2D>();
            if (player != null && Kind == "C07") player.ExitWind(this);
        }
        public void ResetDevice()
        {
            generation++; supportedPlayers.Clear(); SurfaceEnabled = true; pressed = false; pressedAt = -100;
            if (Surface != null) Surface.enabled = true;
            if (Overlay != null) { Overlay.enabled = true; Overlay.sprite = IdleSprite; }
            if (CrumbleVfx != null) CrumbleVfx.Stop();
        }
    }
}
