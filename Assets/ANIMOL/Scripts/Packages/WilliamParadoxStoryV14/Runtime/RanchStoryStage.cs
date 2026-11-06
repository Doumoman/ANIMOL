using Animol.Starway;
using UnityEngine;
using UnityEngine.UI;

namespace WilliamParadox.Story
{
    public static class RanchStoryCuePolicy
    {
        public static bool BlocksInput => false;
        public static bool UsesFullScreenFlash => false;
        public static bool UsesHaptics => false;
        public static string Intensity(string id) => id == "distance_returns_and_grasp" ? "Strong" :
            id == "reactions" || id == "latch_seats" ? "Subtle" : "Standard";
        public static float Duration(string id)
        {
            switch (id)
            {
                case "distance_increases": return 1.6f;
                case "enter_distance": return 0;
                case "distance_returns_and_grasp": return 0.42f;
                case "isol_crosses_foreground": return 1;
                case "reactions": return 0.3f;
                case "foreground_cross": return 0.35f;
                case "runner_reverse": return 0.65f;
                case "rustle": case "eyes_glint": return 0.12f;
                case "boar_charge": return 0.35f;
                case "boar_settle": return 0.2f;
                case "impact_left": case "impact_right": case "impact_center": return 0.1f;
                case "latch_seats": return 0.2f;
                default: return -1;
            }
        }
    }
    /// <summary>Presentation only. StoryCuePlayer supplies every sample; no clock, input or save writes.</summary>
    public sealed class RanchStoryStage : MonoBehaviour
    {
        public StoryView view;
        public RectTransform composition;
        public Image background, friends, hand, runnerImage, isolReaction, toriReaction, impact;
        public Image boarRush, leftEye, rightEye;
        public RanchConstellationOverlay farStars, nearStars;
        public Sprite distanceBackground, frontPlate, reverseShot, runSprite;
        public AudioSource soundSource;
        public AudioClip latchClip;
        public const float DistanceSeconds = 1.6f;
        public const float ReturnSeconds = 0.42f;
        public bool BlocksInput => false;
        public float DistanceProgress { get; private set; }
        public float ReturnBlend { get; private set; }
        public bool ReducedMotion { get; private set; }
        private bool distanceActive, returning, latchPlayed;

        public static bool IsKnownCue(string id)
        {
            return RanchStoryCuePolicy.Duration(id) >= 0;
        }

        public void Present(StoryNode node, bool reducedMotion, bool restoring)
        {
            Clear();
            ReducedMotion = reducedMotion;
            latchPlayed = restoring;
            bool distance = node.artId == "R15" && (node.sceneId == "R15" || node.sceneId == "R16");
            returning = node.id == "R16_B4_L001";
            SetDialogueTop(distance || returning || node.sceneId == "R07" || node.sceneId == "R11" || node.sceneId == "R17");
            if (distance || returning && !restoring)
            {
                distanceActive = true;
                composition.gameObject.SetActive(true);
                background.sprite = distanceBackground;
                background.enabled = friends.enabled = hand.enabled = true;
                view.artImage.enabled = false;
                // Reading subsequent lines does not increase distance. Only the entry shot animates it.
                SetDistance(node.id == "R15_B1_L001" && !restoring && !reducedMotion ? 0 : 1, 0);
            }
        }

        public void Sample(string eventId, float progress, bool reducedMotion)
        {
            float t = Mathf.Clamp01(progress);
            bool reduced = reducedMotion || ReducedMotion;
            switch (eventId)
            {
                case "foreground_cross":
                    composition.gameObject.SetActive(t < 1 && !reduced);
                    view.artImage.enabled = t >= 1 || reduced;
                    background.enabled = true; background.sprite = frontPlate;
                    runnerImage.enabled = t < 1 && !reduced;
                    runnerImage.sprite = runSprite; runnerImage.color = new Color(0.12f, 0.10f, 0.22f, 1);
                    runnerImage.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-1000, 1000, t), -60);
                    if (t >= 1 || reduced) view.SetArtwork("R08_FrontPlate");
                    break;
                case "runner_reverse": break; // Baked distant Isol: an honest static reverse shot.
                case "rustle": break; // No separated shrub/recorded rustle in this package.
                case "eyes_glint":
                    if (leftEye != null) leftEye.enabled = t < 1 && !reduced;
                    if (rightEye != null) rightEye.enabled = t < 1 && !reduced;
                    break;
                case "boar_charge":
                    if (boarRush != null)
                    {
                        boarRush.enabled = t < 1 && !reduced;
                        boarRush.transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.45f, t);
                    }
                    view.SetDim(t >= 1 || reduced ? 0 : Mathf.Clamp01((t - 0.7f) / 0.3f) * 0.9f);
                    break;
                case "boar_settle": break; // Shot change is driven by the dialogue line, not a timer.
                case "distance_increases":
                case "enter_distance": SetDistance(reduced ? 1 : t, 0); break;
                case "distance_returns_and_grasp":
                    if (distanceActive) SetDistance(1, reduced ? 1 : t);
                    if (t >= 1 || reduced) { Clear(); SetDialogueTop(true); }
                    break;
                case "isol_crosses_foreground":
                    // R08B contains a painted distant Isol. Do not add a second moving Isol to it.
                    composition.gameObject.SetActive(t < 1 && !reduced);
                    view.artImage.enabled = t >= 1 || reduced;
                    if (t >= 1 || reduced) { view.SetArtwork("R08B"); runnerImage.enabled = false; break; }
                    background.enabled = true; background.sprite = t < 0.34f ? frontPlate : reverseShot;
                    runnerImage.enabled = t < 0.34f;
                    if (runnerImage.enabled)
                    {
                        runnerImage.sprite = runSprite;
                        runnerImage.color = new Color(0.12f, 0.10f, 0.22f, 1);
                        runnerImage.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-1000, 1000, t / 0.34f), -60);
                    }
                    break;
                case "reactions":
                    isolReaction.enabled = !reduced;
                    toriReaction.enabled = !reduced;
                    float scale = reduced ? 1 : Mathf.Lerp(0.96f, 1, t);
                    isolReaction.transform.localScale = toriReaction.transform.localScale = Vector3.one * scale;
                    break;
                case "impact_left": ShowImpact(new Vector2(0.26f, 0.49f), t, reduced); break;
                case "impact_right": ShowImpact(new Vector2(0.72f, 0.61f), t, reduced); break;
                case "impact_center": ShowImpact(new Vector2(0.57f, 0.32f), t, reduced); break;
                case "latch_seats":
                    if (t >= 1 && !latchPlayed)
                    {
                        latchPlayed = true;
                        if (soundSource != null && latchClip != null && !ReducedMotion) soundSource.PlayOneShot(latchClip, 0.28f);
                    }
                    break;
            }
        }

        private void SetDistance(float progress, float open)
        {
            if (!distanceActive) return;
            DistanceProgress = progress; ReturnBlend = open;
            float depth = progress * (1 - open);
            // Full source canvas, contain; CSS origin (50%,59.3%) in Unity's bottom-left coordinates.
            var rect = friends.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.407f);
            rect.sizeDelta = new Vector2(1080, 1920);
            rect.anchoredPosition = new Vector2(0, Mathf.Lerp(0.06f, 0.11f, depth) * 1920);
            rect.localScale = Vector3.one * Mathf.Lerp(0.94f, 0.56f, depth);
            friends.color = Color.white;
            var drift = new Vector2(0.05f, -0.08f);
            farStars.SetCue(progress, open, drift, 0.70f, ReducedMotion);
            nearStars.SetCue(progress, open, drift, 0.70f, ReducedMotion);
        }

        private void ShowImpact(Vector2 position, float t, bool reduced)
        {
            impact.rectTransform.anchorMin = impact.rectTransform.anchorMax = position;
            impact.enabled = t < 1 && !reduced;
        }

        public void SetReducedMotion(bool value)
        {
            ReducedMotion = value;
            if (value)
            {
                if (distanceActive) SetDistance(1, returning ? 1 : 0);
                if (returning) { Clear(); SetDialogueTop(true); }
                isolReaction.enabled = toriReaction.enabled = impact.enabled = false;
                if (boarRush != null) boarRush.enabled = false;
                if (leftEye != null) leftEye.enabled = false;
                if (rightEye != null) rightEye.enabled = false;
            }
        }

        public void Clear()
        {
            farStars?.ClearCue(); nearStars?.ClearCue();
            distanceActive = false;
            if (composition != null) composition.gameObject.SetActive(false);
            foreach (var image in new[] { background, friends, hand, runnerImage, isolReaction, toriReaction, impact, boarRush, leftEye, rightEye })
                if (image != null) image.enabled = false;
            if (view != null && view.artImage != null) view.artImage.enabled = true;
            if (soundSource != null) soundSource.Stop();
            if (view != null) view.SetDim(0);
            SetDialogueTop(false);
        }
        private void SetDialogueTop(bool top)
        {
            if (view == null || view.dialogueGroup == null) return;
            var rect = (RectTransform)view.dialogueGroup.transform;
            rect.anchorMin = new Vector2(0, top ? 1 : 0); rect.anchorMax = new Vector2(1, top ? 1 : 0);
            rect.pivot = new Vector2(0.5f, top ? 1 : 0);
            if (view.dialogueBridge != null)
            {
                rect.offsetMin = new Vector2(44, top ? -464 : 54);
                rect.offsetMax = new Vector2(-44, top ? -42 : 476);
                var action = (RectTransform)view.dialogueBridge.dialogue.ActionButton.transform;
                action.anchorMin = action.anchorMax = new Vector2(0.5f, top ? 1 : 0);
                action.pivot = new Vector2(0.5f, top ? 1 : 0);
                action.anchoredPosition = new Vector2(0, top ? -490 : 500);
                return;
            }
            rect.offsetMin = new Vector2(24, top ? -470 : 118);
            rect.offsetMax = new Vector2(-24, top ? -130 : 480);
        }
        private void OnDisable() { Clear(); }
    }
}
