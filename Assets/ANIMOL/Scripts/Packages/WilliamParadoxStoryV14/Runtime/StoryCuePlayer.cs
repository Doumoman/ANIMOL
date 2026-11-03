using System;
using System.Collections.Generic;
using UnityEngine;

namespace WilliamParadox.Story
{
    /// <summary>Only the supplied presentation delta advances these jobs. No coroutines/timeScale/tween package.</summary>
    public sealed class StoryCuePlayer
    {
        private sealed class Job
        {
            public StoryCue cue;
            public float elapsed, startValue;
            public Vector2 startPosition;
            public bool started;
        }
        private readonly StoryView view;
        private readonly List<Job> jobs = new List<Job>();
        public bool IsPlaying { get { return jobs.Count > 0; } }
        public bool HasPendingSwapArt { get { return jobs.Exists(j => j.cue.type == "swapArt" && j.elapsed < j.cue.delay); } }
        public StoryCuePlayer(StoryView storyView) { view = storyView; }
        public void Begin(StoryCue[] cues, bool reducedMotion)
        {
            FinishAll();
            foreach (var cue in cues ?? Array.Empty<StoryCue>())
                if (cue != null && cue.type != "shake" && cue.type != "popup") jobs.Add(new Job { cue = cue });
            if (reducedMotion) FinishAll(); else Tick(0);
        }
        public void RestoreSettled(StoryCue[] cues, StorySaveData saved)
        {
            FinishAll();
            view.RestorePresentation(saved);
            // A checkpoint may have been written before an inset's fade/delay began.
            // Restore authored intent, not a transient alpha=0, without replaying motion.
            Begin(cues, true);
        }
        public void Tick(float delta)
        {
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                var job = jobs[i]; job.elapsed += Mathf.Max(0, delta);
                if (job.elapsed < job.cue.delay) continue;
                if (!job.started) Start(job);
                var t = job.cue.duration <= 0 ? 1 : Mathf.Clamp01((job.elapsed - job.cue.delay) / job.cue.duration);
                Apply(job, t);
                if (t >= 1) jobs.RemoveAt(i);
            }
        }
        public void FinishAll()
        {
            // Forward order makes a later cue win if authoring deliberately targets the same property.
            foreach (var job in jobs) { if (!job.started) Start(job); Apply(job, 1); }
            jobs.Clear();
        }
        private RectTransform Target(StoryCue c) { return c.target == "inset" ? view.insetRoot : view.artworkRoot; }
        private void Start(Job j)
        {
            j.started = true;
            var c = j.cue; var target = Target(c);
            j.startPosition = target == null ? Vector2.zero : target.anchoredPosition;
            switch (c.type)
            {
                case "swapArt": break; // Keep the before state until the authored delay has elapsed.
                case "dim": j.startValue = view.DimAlpha; break;
                case "zoom": j.startValue = target == null ? 1 : target.localScale.x; break;
                case "popup":
                    j.startValue = c.scale > 0 ? c.scale : 0.96f;
                    if (target != null) target.localScale = Vector3.one * j.startValue;
                    break;
                case "transition": if (view.artworkGroup != null) view.artworkGroup.alpha = 0; break;
                case "inset":
                    view.ShowInset(c.assetId, c.x, c.y, c.scale);
                    if (view.insetGroup != null) view.insetGroup.alpha = 0;
                    break;
                case "hideInset": j.startValue = view.insetGroup == null ? 0 : view.insetGroup.alpha; break;
            }
        }
        private void Apply(Job j, float t)
        {
            var c = j.cue; var target = Target(c);
            float eased = t * t * (3 - 2 * t);
            switch (c.type)
            {
                case "swapArt": if (t >= 1) view.SetArtwork(c.assetId); break;
                case "dim": view.SetDim(Mathf.Lerp(j.startValue, c.amount, eased)); break;
                case "zoom": if (target != null) target.localScale = Vector3.one * Mathf.Lerp(j.startValue, c.amount, eased); break;
                case "popup": if (target != null) target.localScale = Vector3.one * Mathf.Lerp(j.startValue, 1, eased); break;
                case "shake":
                    if (target != null) target.anchoredPosition = t >= 1 ? j.startPosition : j.startPosition + new Vector2(Mathf.Sin(t * 79), Mathf.Cos(t * 101)) * (c.amount * (1 - t));
                    break;
                case "transition": if (view.artworkGroup != null) view.artworkGroup.alpha = eased; break;
                case "inset": if (view.insetGroup != null) view.insetGroup.alpha = eased; break;
                case "hideInset":
                    if (view.insetGroup != null) view.insetGroup.alpha = Mathf.Lerp(j.startValue, 0, eased);
                    if (t >= 1) view.SetInsetVisible(false);
                    break;
            }
        }
    }
}
