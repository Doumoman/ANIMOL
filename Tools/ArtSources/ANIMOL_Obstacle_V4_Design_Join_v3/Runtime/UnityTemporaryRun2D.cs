using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animol.QA
{
    public sealed class UnityTemporaryRun2D : MonoBehaviour
    {
        public UnityTemporaryPlayer2D Player;
        public Transform SpawnFeet;
        public UnityTemporaryBell2D[] Bells;
        public UnityTemporaryGoal2D Goal;
        public UnityTemporaryObstacle2D[] Obstacles;
        public bool ShowTemporaryHud = true;
        public int BellCount { get; private set; }
        public bool IsCleared { get; private set; }
        public bool IsResetting { get; private set; }
        public double Elapsed { get; private set; }
        public event Action Changed;
        public event Action<bool> CrumbleSurfaceChanged;
        readonly HashSet<int> brokenIds = new HashSet<int>();
        string message = "방울 3개를 모아 목적지로 가세요.";
        bool configured;

        void Start()
        {
            if (Player == null || SpawnFeet == null || Goal == null || Bells == null || Bells.Length != 3)
                throw new InvalidOperationException("QA requires player, spawn feet, goal and exactly three bells.");
            var ids = new HashSet<int>();
            foreach (var bell in Bells) if (bell == null || !ids.Add(bell.BellId))
                throw new InvalidOperationException("QA bell IDs must be unique.");
            Player.Run = this; Goal.Run = this;
            foreach (var bell in Bells) bell.Run = this;
            foreach (var obstacle in Obstacles ?? Array.Empty<UnityTemporaryObstacle2D>()) obstacle.Run = this;
            configured = true; ResetRun();
        }
        void Update() { if (configured && !IsCleared && !IsResetting) Elapsed += Time.deltaTime; }
        public void ResetRun()
        {
            if (!configured || IsResetting) return;
            IsResetting = true; brokenIds.Clear(); BellCount = 0; IsCleared = false; Elapsed = 0;
            foreach (var bell in Bells) bell.ResetBell();
            foreach (var obstacle in Obstacles ?? Array.Empty<UnityTemporaryObstacle2D>()) obstacle.ResetDevice();
            Goal.SetOpen(false); Player.RespawnAtFeet(SpawnFeet.position);
            CrumbleSurfaceChanged?.Invoke(true);
            message = "방울 3개를 모아 목적지로 가세요.";
            IsResetting = false; Changed?.Invoke();
        }
        public bool TryBreakBell(UnityTemporaryBell2D bell, UnityTemporaryPlayer2D player)
        {
            if (!configured || IsResetting || IsCleared || player != Player || bell == null ||
                Array.IndexOf(Bells, bell) < 0 || !brokenIds.Add(bell.BellId)) return false;
            BellCount = brokenIds.Count; bell.HideBroken(); Goal.SetOpen(BellCount == 3);
            message = BellCount == 3 ? "방울 3개 완료! 열린 목적지로 가세요." : "방울 " + BellCount + "/3";
            Changed?.Invoke(); return true;
        }
        public bool TryComplete(UnityTemporaryPlayer2D player)
        {
            if (!configured || IsResetting || IsCleared || player != Player) return false;
            if (BellCount != 3) { message = "목적지는 잠겨 있어요. 방울 " + BellCount + "/3"; return false; }
            IsCleared = true; Player.Body.linearVelocity = Vector2.zero; Player.ReleaseInput();
            message = "클리어! 시작 → 방울 3개 → 목적지 완료";
            Changed?.Invoke(); return true;
        }
        public void NotifyCrumbleSurface(bool enabled) { CrumbleSurfaceChanged?.Invoke(enabled); }
        void OnGUI()
        {
            if (!ShowTemporaryHud || !configured) return;
            GUI.Box(new Rect(8, 8, Mathf.Min(Screen.width - 16, 540), 84), "");
            GUI.Label(new Rect(16, 14, Screen.width - 32, 22), "방울 " + BellCount + "/3 · " + (BellCount == 3 ? "목적지 열림" : "목적지 잠김"));
            GUI.Label(new Rect(16, 36, Screen.width - 32, 22), message);
            if (GUI.Button(new Rect(16, 61, 100, 24), "다시 시작 (R)")) ResetRun();
        }
    }
}
