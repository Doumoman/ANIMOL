using System;
using System.Collections.Generic;
using UnityEngine;

namespace WilliamParadox.Story
{
    /// <summary>The host adapter must suspend input, abilities and its own clocks without issuing gameplay results.</summary>
    public interface IStoryPauseSink { void SetStorySuspended(bool suspended); }

    /// <summary>Optional, explicit host integration. Never changes Time.timeScale or guesses a timer implementation.</summary>
    [DisallowMultipleComponent]
    public sealed class StoryGameplayBridge : MonoBehaviour
    {
        public StoryRunner runner;
        [Tooltip("Assign explicit host adapters implementing IStoryPauseSink. Empty means gameplay is NOT integrated.")]
        public MonoBehaviour[] sinks = Array.Empty<MonoBehaviour>();

        private readonly Dictionary<MonoBehaviour, bool> delivered = new Dictionary<MonoBehaviour, bool>();
        private StoryRunner boundRunner;
        private bool requested, runnerWasActive;
        private string lastWarning;
        /// <summary>Requested suspension; HasAdapters must also be true for a host to receive it.</summary>
        public bool IsSuspended { get { return requested; } }
        public bool HasAdapters { get { return delivered.Count > 0; } }
        public int ConnectedAdapterCount { get { return delivered.Count; } }
        public string ConfigurationWarning { get; private set; }

        private void OnEnable()
        {
            BindRunner();
            requested = boundRunner != null && boundRunner.isActiveAndEnabled && (boundRunner.CurrentNode == null || boundRunner.CurrentNode.kind != "end");
            RefreshAdapters();
        }
        private void Update()
        {
            if (runner != boundRunner)
            {
                ReleaseStory(); UnbindRunner(); BindRunner(); RefreshAdapters();
                if (boundRunner != null && boundRunner.isActiveAndEnabled && (boundRunner.CurrentNode == null || boundRunner.CurrentNode.kind != "end")) BeginStory();
            }
            bool active = boundRunner != null && boundRunner.isActiveAndEnabled;
            if (boundRunner != null && (!active || boundRunner.ValidationErrors.Count > 0)) ReleaseStory();
            else if (active && !runnerWasActive && (boundRunner.CurrentNode == null || boundRunner.CurrentNode.kind != "end")) BeginStory();
            runnerWasActive = active;
        }
        private void BindRunner()
        {
            if (runner == null) runner = GetComponent<StoryRunner>();
            boundRunner = runner;
            runnerWasActive = boundRunner != null && boundRunner.isActiveAndEnabled;
            if (boundRunner == null) { Report("StoryGameplayBridge has no StoryRunner. No gameplay suspension is requested."); return; }
            if (boundRunner.onNodeChanged != null) boundRunner.onNodeChanged.AddListener(HandleNodeChanged);
            if (boundRunner.onCompleted != null) boundRunner.onCompleted.AddListener(HandleCompleted);
        }
        private void UnbindRunner()
        {
            if (boundRunner != null)
            {
                if (boundRunner.onNodeChanged != null) boundRunner.onNodeChanged.RemoveListener(HandleNodeChanged);
                if (boundRunner.onCompleted != null) boundRunner.onCompleted.RemoveListener(HandleCompleted);
            }
            boundRunner = null; runnerWasActive = false;
        }
        private void HandleNodeChanged(string unused)
        {
            if (boundRunner == null || boundRunner.CurrentNode == null) return;
            if (boundRunner.CurrentNode.kind == "end") ReleaseStory(); else BeginStory();
        }
        private void HandleCompleted() { ReleaseStory(); }
        public void SetStoryActive(bool value) { if (value) BeginStory(); else ReleaseStory(); }
        public void BeginStory()
        {
            if (!isActiveAndEnabled || runner == null || !runner.isActiveAndEnabled) return;
            requested = true; PushRequestedState();
        }
        public void ReleaseStory() { requested = false; PushRequestedState(); }

        public void RefreshAdapters()
        {
            var valid = new HashSet<MonoBehaviour>();
            var invalid = new List<string>();
            foreach (var component in sinks ?? Array.Empty<MonoBehaviour>())
            {
                if (component == null) continue;
                if (component is IStoryPauseSink) valid.Add(component);
                else invalid.Add(component.GetType().Name);
            }
            foreach (var component in new List<MonoBehaviour>(delivered.Keys))
            {
                if (component != null && valid.Contains(component)) continue;
                if (component != null && delivered[component]) Deliver(component, false);
                delivered.Remove(component);
            }
            foreach (var component in valid) if (!delivered.ContainsKey(component)) delivered.Add(component, false);
            var warning = new List<string>();
            if (boundRunner == null) warning.Add("No StoryRunner is assigned.");
            if (valid.Count == 0) warning.Add("No IStoryPauseSink adapters are assigned. Gameplay input, abilities and timers are NOT connected; assign explicit host adapters.");
            if (invalid.Count > 0) warning.Add("These components do not implement IStoryPauseSink: " + string.Join(", ", invalid));
            ConfigurationWarning = string.Join(" ", warning);
            if (ConfigurationWarning.Length > 0) Report("StoryGameplayBridge: " + ConfigurationWarning);
            else lastWarning = null;
            PushRequestedState();
        }
        private void PushRequestedState()
        {
            foreach (var component in new List<MonoBehaviour>(delivered.Keys))
            {
                if (component == null) { delivered.Remove(component); continue; }
                if (delivered[component] == requested) continue;
                var previous = delivered[component]; var value = requested;
                // Publish before invoking the host so a re-entrant ReleaseStory can balance this request.
                delivered[component] = value;
                if (!Deliver(component, value) && delivered.ContainsKey(component) && delivered[component] == value) delivered[component] = previous;
            }
        }
        private bool Deliver(MonoBehaviour component, bool value)
        {
            try { ((IStoryPauseSink)component).SetStorySuspended(value); return true; }
            catch (Exception exception)
            {
                Debug.LogError("StoryGameplayBridge adapter failed: " + component.GetType().Name + " — " + exception.Message, this);
                return false;
            }
        }
        private void Report(string warning)
        {
            if (lastWarning == warning) return;
            lastWarning = warning; Debug.LogWarning(warning, this);
        }
        private void OnDisable() { ReleaseStory(); UnbindRunner(); }
        private void OnDestroy() { ReleaseStory(); UnbindRunner(); }
    }
}
