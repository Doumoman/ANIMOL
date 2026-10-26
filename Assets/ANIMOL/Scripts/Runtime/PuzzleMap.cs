using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    public enum TerrainMaterial { Grass, Stone, Sand }
    public enum DeviceKind { None, Conveyor, Wind, Ice, Brake, Portal }
    [Serializable] public struct TerrainCell
    {
        public Vector2Int position;
        public TerrainMaterial material;
        public bool blocked;
        public TerrainCell(Vector2Int p, TerrainMaterial m, bool b = false) { position = p; material = m; blocked = b; }
    }
    [Serializable] public struct DeviceCell
    {
        public Vector2Int position;
        public DeviceKind kind;
        public Vector2Int direction;
        public Vector2Int portalTarget;
    }
    [Serializable] public struct DecorationCell
    {
        public Vector2Int position;
        public Sprite sprite;
        public Color tint;
        // Visual decoration ownership only. The source Sprite is the existing art ID;
        // placementId is independent of the shared connectionGroup.
        public string placementId, connectionGroup;
        public string visualFamily;
        public int logicalLayer, heightKey;
    }
    [Serializable] public class PuzzleCameraSettings
    {
        [Range(5, 25)] public float visibleRows = 11;
        [Range(1, 40)] public float followSharpness = 16;
        [Tooltip("Reserved height for the existing HUD in 540px-wide portrait reference units.")][Range(0, 150)] public float topInset = 90;
        [Tooltip("Optional camera inset; swipe controls need no bottom UI area.")][Range(0, 300)] public float bottomInset = 0;
        [Range(0, 1)] public float farParallax = .08f;
        [Range(0, 1)] public float midParallax = .3f;
        [Range(0, 1)] public float nearParallax = .65f;
    }

    [CreateAssetMenu(menuName = "ANIMOL/Puzzle Map")]
    public sealed class PuzzleMap : ScriptableObject
    {
        [SerializeField, Range(1, 256)] int width = 12;
        [SerializeField, Range(1, 256)] int height = 18;
        public int Width => width;
        public int Height => height;
        public PuzzleTheme theme;
        public PuzzleScenario scenario;
        [Min(0)] public int targetBubbles = 3;
        public PuzzleCameraSettings cameraSettings = new PuzzleCameraSettings();
        public Vector2Int start = new Vector2Int(1, 1);
        public Vector2Int exit = new Vector2Int(10, 16);
        public List<TerrainCell> terrain = new List<TerrainCell>();
        public List<DeviceCell> devices = new List<DeviceCell>();
        public List<DecorationCell> decorations = new List<DecorationCell>();
        public List<Vector2Int> bubbles = new List<Vector2Int>();
        public event Action<Vector2Int> TerrainChanged;
        public event Action Resized;
        public event Action DecorationsChanged;
        Dictionary<Vector2Int, TerrainCell> terrainIndex;
        Dictionary<Vector2Int, DeviceCell> deviceIndex;

        public bool Contains(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < width && p.y < height;
        public void InvalidateIndex() { terrainIndex = null; deviceIndex = null; }
        public void NotifyDecorationsChanged() { DecorationsChanged?.Invoke(); }
        void Index()
        {
            if (terrainIndex != null) return;
            terrainIndex = new Dictionary<Vector2Int, TerrainCell>();
            deviceIndex = new Dictionary<Vector2Int, DeviceCell>();
            foreach (var t in terrain) terrainIndex[t.position] = t;
            foreach (var d in devices) deviceIndex[d.position] = d;
        }
        public bool TryTerrain(Vector2Int p, out TerrainCell cell)
        { Index(); cell = default; return Contains(p) && terrainIndex.TryGetValue(p, out cell); }
        public bool DecorationAt(Vector2Int p) => Contains(p) &&
            decorations.Any(cell => RanchConnectedDecorHost.Occupies(this, cell, p));
        public bool Walkable(Vector2Int p) => TryTerrain(p, out var t) && !t.blocked && !DecorationAt(p);
        public DeviceCell DeviceAt(Vector2Int p)
        { Index(); return Contains(p) && deviceIndex.TryGetValue(p, out var d) ? d : default; }
        public void SetTerrain(Vector2Int p, TerrainMaterial material, bool blocked = false)
        {
            terrain.RemoveAll(t => t.position == p); terrain.Add(new TerrainCell(p, material, blocked));
            InvalidateIndex(); TerrainChanged?.Invoke(p);
        }
        public void RemoveTerrain(Vector2Int p)
        { terrain.RemoveAll(t => t.position == p); InvalidateIndex(); TerrainChanged?.Invoke(p); }
        public void SetDevice(DeviceCell cell)
        { devices.RemoveAll(d => d.position == cell.position); if (cell.kind != DeviceKind.None) devices.Add(cell); InvalidateIndex(); }
        // Cropped records remain serialized and recover when the map grows; never silently delete.
        public void Resize(int w, int h)
        { width = Mathf.Clamp(w, 1, 256); height = Mathf.Clamp(h, 1, 256); InvalidateIndex(); Resized?.Invoke(); }
        public int OutsideCount(int w, int h)
        {
            bool Outside(Vector2Int p) => p.x < 0 || p.y < 0 || p.x >= w || p.y >= h;
            return terrain.Count(t => Outside(t.position)) + devices.Count(d => Outside(d.position)) +
                decorations.Sum(d => {
                    var source = RanchConnectedDecorHost.SourceMapping(this, d);
                    var theater = TheaterConnectedDecorHost.TryMap(this, d, out var theaterSource) ? theaterSource : null;
                    var sanctum = SanctumConnectedDecorHost.TryMap(this, d, out var sanctumSource) ? sanctumSource : null;
                    var coast = CoastConnectedDecorHost.TryMap(this, d, out var coastSource) ? coastSource : null;
                    var width = theater != null ? theater.width : sanctum != null ? sanctum.width : coast != null ? coast.width : source == null ? 1 : source.occupiedWidth;
                    var height = theater != null ? theater.height : sanctum != null ? sanctum.height : coast != null ? coast.height : source == null ? 1 : source.occupiedHeight;
                    var count = 0;
                    for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                        if (Outside(d.position + new Vector2Int(x, y))) count++;
                    return count;
                }) + bubbles.Count(Outside) + (Outside(start) ? 1 : 0) + (Outside(exit) ? 1 : 0) +
                (scenario ? scenario.devices.Count(d => Outside(d.position)) + scenario.water.Count(Outside) + scenario.devices.Sum(d => d.cells.Count(Outside)) +
                    scenario.devices.Count(d => d.role == PrototypeRole.StarSocket && d.variant == "socket" &&
                        d.socketReward == StarSocketReward.RevealBubble && Outside(d.socketBubbleCell)) +
                    scenario.devices.Count(d => d.role == PrototypeRole.Chick && d.target != Vector2Int.zero && Outside(d.target)) : 0);
        }
        public List<string> ValidateMap()
        {
            var errors = new List<string>();
            if (!Walkable(start)) errors.Add("Start must be on an in-bounds walkable tile.");
            if (!Walkable(exit)) errors.Add("Exit must be on an in-bounds walkable tile.");
            var possibleBubbles = bubbles.Concat(scenario ? scenario.devices.Where(d =>
                d.role == PrototypeRole.StarSocket && d.variant == "socket" && d.socketReward == StarSocketReward.RevealBubble && Contains(d.position))
                .Select(d => d.socketBubbleCell) : Enumerable.Empty<Vector2Int>());
            if (targetBubbles < 0 || possibleBubbles.Distinct().Count(Walkable) < targetBubbles)
                errors.Add("Not enough in-bounds walkable bubble placements for the target.");
            foreach (var p in bubbles.Where(Contains)) if (!Walkable(p)) errors.Add("Bubble has no walkable terrain: " + p);
            foreach (var d in devices.Where(d => Contains(d.position)))
            {
                if (!Walkable(d.position)) errors.Add("Device has no walkable terrain: " + d.position);
                if (d.kind == DeviceKind.Portal && !Walkable(d.portalTarget)) errors.Add("Portal destination is outside or blocked: " + d.position);
                if ((d.kind == DeviceKind.Conveyor || d.kind == DeviceKind.Wind) && !PuzzleSession.IsCardinal(d.direction)) errors.Add("Device direction must be cardinal: " + d.position);
            }
            if (theme && theme.starwayRanch && decorations.Any(d =>
                !string.IsNullOrEmpty(d.visualFamily) || RanchConnectedDecorHost.TryMap(this, d, out _)))
            {
                if (!theme.ranchConnectedDecorV01Catalog || !theme.ranchConnectedDecorV01ManifestPin)
                    errors.Add("Ranch connected decoration catalog or manifest pin is missing.");
                else if (!RanchConnectedDecorHost.TryCreatePlan(this, theme.ranchConnectedDecorV01Catalog,
                    theme.ranchConnectedDecorV01ManifestPin.text.Trim(), decorations, out _, out _, out _, out var decorError))
                    errors.Add("Ranch connected decoration layout is invalid: " + decorError);
            }
            if (TheaterConnectedDecorHost.IsTheater(this) && decorations.Any(d =>
                TheaterConnectedDecorHost.IsDeclaredKey(d.visualFamily) || TheaterConnectedDecorHost.TryMap(this, d, out _)) &&
                !TheaterConnectedDecorHost.TryCreateSnapshot(this, decorations,
                    new Vector3(-.5f, -.5f), 1, 0, out _, out var theaterError))
                errors.Add("Theater connected decoration layout is invalid: " + theaterError);
            if (scenario) errors.AddRange(scenario.Validate(this));
            return errors;
        }
        void OnValidate() { width = Mathf.Clamp(width, 1, 256); height = Mathf.Clamp(height, 1, 256); InvalidateIndex(); }
    }
}
