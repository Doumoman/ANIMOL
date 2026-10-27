using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.RanchDevicesV13
{
    // Presentation only: call AFTER the host has committed a return and its approach proxy has arrived.
    // This component never changes a chick, nest occupancy, rewards, input cost or device turns.
    public sealed class RanchChickNestReturnVisualR4 : MonoBehaviour
    {
        [SerializeField] RanchDeviceVisualV13 nestVisual;
        readonly HashSet<string> playedTransactions = new HashSet<string>(StringComparer.Ordinal);
        bool configured;
        string settledKey;
        public bool IsPlaying { get; private set; }

        bool OwnsVisual()
        {
            return nestVisual != null && nestVisual.transform != transform && nestVisual.transform.IsChildOf(transform);
        }
        static bool Cardinal(string direction)
        {
            return direction == "south" || direction == "east" || direction == "north" || direction == "west";
        }
        public bool Configure(RanchDevicesV13Catalog catalog, RanchDeviceVisualV13 dedicatedChild, out string error)
        {
            ClearPresentation(); configured = false; playedTransactions.Clear(); error = null;
            if (dedicatedChild == null || dedicatedChild.transform == transform || !dedicatedChild.transform.IsChildOf(transform))
            { error = "Assign a dedicated nest visual child owned by this component."; return false; }
            nestVisual = dedicatedChild;
            if (!nestVisual.Configure(catalog, out error)) return false;
            if (!nestVisual.SetState("nest/idle", true)) { error = "Current nest catalog is required."; return false; }
            configured = true; return true;
        }
        // Use for initial state, load, undo/reset and pool reuse. No arrival animation is replayed.
        // This starts a new playback generation; do not call it every Update or while queuing old callbacks.
        public bool SetResolvedState(bool occupied, string settledDirection, out string error)
        {
            error = null;
            if (!configured || !OwnsVisual()) { error = "Configure the owned nest renderer first."; return false; }
            if (occupied && !Cardinal(settledDirection)) { error = "Supply the stored cardinal settled direction."; return false; }
            string key = occupied ? "nest_return/" + settledDirection + "/occupied" : "nest/idle";
            if (!nestVisual.SetState(key, true)) { error = "Resolved nest visual is missing."; return false; }
            IsPlaying = false; settledKey = null; playedTransactions.Clear(); return true;
        }
        // transactionId is the host's unique successful return-event identity, including board/reset generation.
        // Repeated delivery of the same event is a successful no-op. A different event while busy is left to the host queue.
        public bool PlayCommittedArrival(string transactionId, string arrivalDirection, out string error)
        {
            error = null;
            if (!configured || !OwnsVisual()) { error = "Configure the owned nest renderer first."; return false; }
            if (!isActiveAndEnabled || !nestVisual.isActiveAndEnabled)
            { error = "Enable the owned presentation before playing an arrival."; return false; }
            if (string.IsNullOrEmpty(transactionId) || !Cardinal(arrivalDirection))
            { error = "Use a committed return-event ID and cardinal final approach direction."; return false; }
            if (playedTransactions.Contains(transactionId)) return true;
            if (IsPlaying) { error = "This nest is presenting another arrival; queue the new event in the host."; return false; }
            if (!nestVisual.SetState("nest_return/" + arrivalDirection + "/arrive", true))
            { error = "Directional arrival clip is missing."; return false; }
            playedTransactions.Add(transactionId);
            settledKey = "nest_return/" + arrivalDirection + "/occupied"; IsPlaying = true; return true;
        }
        void LateUpdate()
        {
            if (!IsPlaying || !OwnsVisual() || !nestVisual.Finished) return;
            string key = settledKey; IsPlaying = false; settledKey = null;
            if (!nestVisual.SetState(key, true)) ClearPresentation();
        }
        // Reapply an authoritative resolved state after cancellation. Does not return a chick to gameplay.
        public void ClearPresentation()
        {
            IsPlaying = false; settledKey = null;
            if (OwnsVisual()) nestVisual.Clear();
        }
        void OnDisable() { ClearPresentation(); }
    }
}
