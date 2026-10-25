using System;
using System.Collections.Generic;
using UnityEngine;

namespace WilliamParadox.Story
{
    [Serializable]
    public sealed class StoryNodeSound
    {
        public string nodeId;
        [Tooltip("First assigned clip is used deterministically. Additional clips are retained for explicit authoring.")]
        public AudioClip[] clips;
        [Range(0, 1)] public float volume = 1;
    }

    /// <summary>Optional transient UI/story sounds. No haptics and no replay after restore/focus resume.</summary>
    [DisallowMultipleComponent]
    public sealed class StoryAudioFeedback : MonoBehaviour
    {
        public StoryRunner runner;
        [Tooltip("Explicit dedicated, non-looping 2D SFX AudioSource. Never assign the music source.")]
        public AudioSource source;
        public StoryNodeSound[] nodeSounds = Array.Empty<StoryNodeSound>();
        public AudioClip advanceClip;
        [Range(0, 1)] public float advanceVolume = 0.4f;
        public bool muted;

        private readonly HashSet<string> seenNodes = new HashSet<string>(StringComparer.Ordinal);
        private bool hasFocus = true, applicationPaused;
        private float lastAdvanceTime = -100;
        private int lastAdvanceFrame = -1, lastNodeSoundFrame = -1;
        public int SeenNodeCount { get { return seenNodes.Count; } }
        public bool HasSeenNode(string nodeId) { return !string.IsNullOrEmpty(nodeId) && seenNodes.Contains(nodeId); }
        private bool Blocked { get { return !isActiveAndEnabled || muted || !hasFocus || applicationPaused || (runner != null && runner.IsPaused); } }

        public void PlayNode(string nodeId, bool restoring)
        {
            if (string.IsNullOrEmpty(nodeId) || !seenNodes.Add(nodeId)) return;
            // Restored and suppressed nodes are remembered, never queued to replay later.
            if (restoring || Blocked || source == null) return;
            foreach (var cue in nodeSounds ?? Array.Empty<StoryNodeSound>())
            {
                if (cue == null || cue.nodeId != nodeId) continue;
                foreach (var clip in cue.clips ?? Array.Empty<AudioClip>())
                {
                    if (clip == null) continue;
                    PlayClip(clip, cue.volume); lastNodeSoundFrame = Time.frameCount; return;
                }
                return;
            }
        }
        /// <summary>Call only after StoryRunner has accepted the advance input, never on raw pointer down.</summary>
        public void PlayAdvance()
        {
            if (Blocked || source == null || advanceClip == null || lastAdvanceFrame == Time.frameCount
                || lastNodeSoundFrame == Time.frameCount || Time.unscaledTime - lastAdvanceTime < 0.15f) return;
            lastAdvanceFrame = Time.frameCount; lastAdvanceTime = Time.unscaledTime;
            PlayClip(advanceClip, advanceVolume);
        }
        private void PlayClip(AudioClip clip, float volume)
        {
            if (source == null || !source.isActiveAndEnabled) return;
            // One dedicated transient voice: a node cue replaces a same-frame tap instead of stacking sounds.
            source.Stop(); source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
        public void ResetSession()
        {
            StopFeedback(); seenNodes.Clear(); lastAdvanceFrame = lastNodeSoundFrame = -1; lastAdvanceTime = -100;
        }
        public void StopFeedback() { if (source != null) source.Stop(); }
        private void Update() { if (Blocked) StopFeedback(); }
        private void LateUpdate() { if (Blocked) StopFeedback(); }
        private void OnDisable() { StopFeedback(); }
        private void OnApplicationFocus(bool value) { hasFocus = value; if (!value) StopFeedback(); }
        private void OnApplicationPause(bool value) { applicationPaused = value; if (value) StopFeedback(); }
    }
}
