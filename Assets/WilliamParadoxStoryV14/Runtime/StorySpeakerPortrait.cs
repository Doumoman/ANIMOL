using UnityEngine;
using UnityEngine.UI;

namespace WilliamParadox.Story
{
    /// <summary>
    /// Optional dialogue-tray portrait card. The card may be an authored Sprite rect from a
    /// flattened image; independent means its UI transform is separate from that image.
    /// No Update/coroutine: the owning StoryPresentationPlayer is its sole animation clock.
    /// </summary>
    public sealed class StorySpeakerPortrait : MonoBehaviour
    {
        public RectTransform actorRect;
        public Image actorImage;

        private RectTransform capturedRect;
        private Image displayedImage;
        private Vector2 homePosition;
        private Vector3 homeScale;
        private Quaternion homeRotation;
        private float elapsed, strength;
        private bool playing;

        public bool IsPlaying { get { return playing; } }
        public bool IsVisible { get { return displayedImage != null && displayedImage.enabled && displayedImage.sprite != null; } }

        public bool Attach(StoryView view)
        {
            if (!SafeBinding(view)) { Hide(); return false; }
            if (capturedRect != actorRect || displayedImage != actorImage)
            {
                Hide();
                capturedRect = actorRect;
                displayedImage = actorImage;
                homePosition = actorRect.anchoredPosition;
                homeScale = actorRect.localScale;
                homeRotation = actorRect.localRotation;
                actorImage.raycastTarget = false;
                actorImage.preserveAspect = true;
                actorImage.enabled = false;
            }
            return true;
        }

        public bool Show(StoryView view, Sprite sprite, bool independentLayer)
        {
            if (!independentLayer || sprite == null || !Attach(view)) { Hide(); return false; }
            FinishAll();
            displayedImage.sprite = sprite;
            displayedImage.enabled = true;
            return true;
        }

        public void BeginBounce(float value)
        {
            FinishAll();
            if (!IsVisible || !StoryGraph.Finite(value) || value <= 0) return;
            strength = Mathf.Clamp01(value);
            elapsed = 0;
            playing = true;
        }

        public void Tick(float delta)
        {
            if (!playing || capturedRect == null || !StoryGraph.Finite(delta)) return;
            elapsed += Mathf.Max(0, delta);
            float t = Mathf.Clamp01(elapsed / StoryPresentationMath.BounceDuration);
            var sample = StoryPresentationMath.Bounce(t);
            capturedRect.anchoredPosition = homePosition + new Vector2(0, sample.y * strength);
            var scale = Vector3.one + (sample.scale - Vector3.one) * strength;
            capturedRect.localScale = Vector3.Scale(homeScale, scale);
            capturedRect.localRotation = homeRotation * Quaternion.Euler(0, 0, sample.rotation * strength);
            if (t >= 1) FinishAll();
        }

        public void FinishAll()
        {
            if (capturedRect != null)
            {
                capturedRect.anchoredPosition = homePosition;
                capturedRect.localScale = homeScale;
                capturedRect.localRotation = homeRotation;
            }
            playing = false;
            elapsed = 0;
        }

        public void Hide()
        {
            FinishAll();
            // Never touch an unvalidated Image (it could be the main CG by mistake).
            if (displayedImage != null) { displayedImage.enabled = false; displayedImage.sprite = null; }
        }

        private bool SafeBinding(StoryView view)
        {
            if (view == null || view.dialogueGroup == null || actorRect == null || actorImage == null) return false;
            var dialogue = view.dialogueGroup.transform;
            if (actorRect == dialogue || !actorRect.IsChildOf(dialogue)) return false;
            if (actorImage.rectTransform != actorRect && !actorImage.rectTransform.IsChildOf(actorRect)) return false;
            // Do not animate the CG, a CG descendant, or any parent that contains the CG.
            if (view.artImage == actorImage || view.artImage != null &&
                (actorRect == view.artImage.rectTransform || view.artImage.transform.IsChildOf(actorRect) || actorRect.IsChildOf(view.artImage.transform))) return false;
            if (view.artworkRoot != null && (actorRect == view.artworkRoot || actorRect.IsChildOf(view.artworkRoot) || view.artworkRoot.IsChildOf(actorRect))) return false;
            // Moving the image card must not move dialogue glyphs or controls with it.
            if (view.bodyText != null && view.bodyText.transform.IsChildOf(actorRect)) return false;
            if (view.speakerText != null && view.speakerText.transform.IsChildOf(actorRect)) return false;
            if (view.advanceButton != null && view.advanceButton.transform.IsChildOf(actorRect)) return false;
            return true;
        }

        private void OnDisable() { FinishAll(); }
        private void OnDestroy() { FinishAll(); }
    }
}
