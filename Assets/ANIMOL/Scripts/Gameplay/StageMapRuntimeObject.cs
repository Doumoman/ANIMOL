using System;
using ANIMOL.Core;
using Animol;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public interface IStageMapRuntimeResettable
    {
        void ResetRuntimeState();
    }

    public abstract class StageMapRuntimeObject : MonoBehaviour, IStageMapRuntimeResettable
    {
        protected StageMapObjectPlacement placement;
        protected StageMapObjectSettings settings;
        protected float cellSize = 1f;
        protected Vector2 initialPosition;
        private AnimolPhasePlayer phasePlayer;

        public string StableId => placement?.StableId ?? string.Empty;
        public StageMapObjectSettings Settings => settings;
        public Vector2 InitialPosition => initialPosition;

        public virtual void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            placement = value ?? throw new ArgumentNullException(nameof(value));
            settings = value.Settings.Clone();
            cellSize = Mathf.Max(.01f, unitsPerCell);
            initialPosition = new Vector2(value.X * cellSize, value.Y * cellSize);
            transform.position = initialPosition;
            name = value.StableId;
            phasePlayer = GetComponentInChildren<AnimolPhasePlayer>(true);
        }

        protected Vector2 WorldForCell(Vector2Int cell) => new Vector2(cell.x * cellSize, cell.y * cellSize);
        protected bool PlayPhase(string phase)
        {
            if (phasePlayer == null) phasePlayer = GetComponentInChildren<AnimolPhasePlayer>(true);
            return phasePlayer != null && phasePlayer.PlayPhase(phase);
        }
        protected void PlayIdlePhase() => PlayPhase("idle");
        protected float GetPhaseDuration(string phase)
        {
            if (phasePlayer == null) phasePlayer = GetComponentInChildren<AnimolPhasePlayer>(true);
            return phasePlayer == null ? 0f : phasePlayer.GetPhaseDuration(phase);
        }
        public abstract void ResetRuntimeState();
    }

    public static class StageMapRuntimeFactory
    {
        public static StageMapRuntimeObject Configure(GameObject instance, StageMapObjectPlacement placement, float cellSize)
        {
            if (instance == null || placement == null) return null;
            var runtime = instance.GetComponent<StageMapRuntimeObject>();
            runtime?.Configure(placement, cellSize);
            return runtime;
        }
    }

    public static class StageMapRespawnRouter
    {
        public static bool TryRespawn(DevPlayerController player)
        {
            if (player == null) return false;
            var objectLab = UnityEngine.Object.FindFirstObjectByType<ObjectLabSession>();
            if (objectLab != null) return objectLab.RespawnAtCheckpoint();
            var campaign = UnityEngine.Object.FindFirstObjectByType<CampaignMapDevSession>();
            return campaign != null && campaign.RespawnAtCheckpoint();
        }
    }
}
