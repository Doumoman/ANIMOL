using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    /// <summary>Editor-only search evidence stored separately so authored MapData stays intact.</summary>
    public static class PuzzleShortestPathRecord
    {
        public const int SolverVersion = StarwayShortestPathCatalog.SolverVersion;
        public const string Algorithm = "Exact A* with relaxed board lower bound, otherwise 0-1 BFS; directional input costs 1; wind costs 0; synchronous-input wind included; sheep and crates enter reflectors only from their open sides";
        public const string Path = "Assets/ANIMOL/Data/Puzzle/MapData/starway/StarwayShortestPaths.json";
        [Serializable] public sealed class Entry
        {
            public string mapId, mapAsset, mapSha256, status, route, detail, algorithm;
            public int solverVersion, distance, expanded, visited;
            public double elapsedSeconds;
        }
        [Serializable] public sealed class Index
        {
            public int formatVersion = SolverVersion;
            public string algorithm = Algorithm;
            public List<Entry> maps = new List<Entry>();
        }

        public static Index Read()
        {
            if (!File.Exists(Path)) return new Index();
            var index = JsonUtility.FromJson<Index>(File.ReadAllText(Path, Encoding.UTF8));
            return index ?? new Index();
        }

        public static string MapId(PuzzleMap map)
        {
            var path = AssetDatabase.GetAssetPath(map);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            return name.EndsWith("_MapData", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "_MapData".Length) : name;
        }

        public static Entry Find(PuzzleMap map)
        {
            if (!map) return null;
            var id = MapId(map);
            return Read().maps.FirstOrDefault(entry => entry.mapId == id);
        }

        public static bool IsCurrent(PuzzleMap map, Entry entry) => entry != null &&
            entry.solverVersion == SolverVersion &&
            !PuzzleShortestPathSolver.SourceIsDirty(map) &&
            entry.mapSha256 == PuzzleShortestPathSolver.HashAsset(map);

        public static void Save(PuzzleShortestPathSolver.Result result)
        {
            if (result == null || !result.map) throw new ArgumentNullException(nameof(result));
            string id = MapId(result.map);
            if (id.Length != 4 || !id.StartsWith("SW", StringComparison.Ordinal) ||
                !int.TryParse(id.Substring(2), out var number) || number < 0 || number > 19)
                throw new InvalidOperationException("Only SW00–SW19 MapData can be recorded here: " + id);
            if (PuzzleShortestPathSolver.SourceIsDirty(result.map) ||
                result.sourceSha256 != PuzzleShortestPathSolver.HashAsset(result.map))
                throw new InvalidOperationException("MapData changed during the search; no result was recorded.");
            if (result.Solved) Verify(result.map, result.route, result.distance);
            var index = Read();
            index.formatVersion = SolverVersion;
            index.algorithm = Algorithm;
            index.maps.RemoveAll(entry => entry.mapId == id);
            index.maps.Add(new Entry { mapId = id, mapAsset = AssetDatabase.GetAssetPath(result.map),
                solverVersion = SolverVersion,
                mapSha256 = result.sourceSha256, status = result.status, route = PuzzleRouteNotation.ToArrows(result.route ?? ""),
                distance = result.distance, expanded = result.expanded, visited = result.visited,
                elapsedSeconds = result.elapsedSeconds, detail = result.detail, algorithm = result.algorithm });
            index.maps.Sort((a, b) => string.CompareOrdinal(a.mapId, b.mapId));
            string temporary = Path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(index, true) + "\n", new UTF8Encoding(false));
            if (File.Exists(Path)) File.Replace(temporary, Path, null);
            else File.Move(temporary, Path);
            AssetDatabase.ImportAsset(Path);
        }

        static void Verify(PuzzleMap map, string route, int expectedDistance)
        {
            if (route == null || PuzzleRouteNotation.InputCount(route) != expectedDistance)
                throw new InvalidDataException("Distance and route differ.");
            var session = new PuzzleDeviceSession(map);
            if (map.scenario.devices.Any(d => d.role == PrototypeRole.Wind)) session.AdvanceWind();
            foreach (char action in route)
            {
                if (char.IsWhiteSpace(action)) continue;
                if (PuzzleRouteNotation.IsWait(action))
                {
                    if (!session.AdvanceWind().accepted || session.Failed)
                        throw new InvalidDataException("Recorded wind wait was rejected.");
                    continue;
                }
                if (!PuzzleRouteNotation.TryDirection(action, out var heading) || !session.ReplayRuntimeInput(heading) || session.Failed)
                    throw new InvalidDataException("Shortest path replay rejected at input " + session.Cost);
            }
            if (!session.Won || session.Cost != expectedDistance)
                throw new InvalidDataException("Shortest path replay did not reach the verified clear state.");
        }
    }

    public static class StarwayShortestPathBatch
    {
        static int nextMap;
        static bool running;
        static int stateLimit;
        public static bool Running => running;
        public static int NextMap => nextMap;
        public static string LastMessage { get; private set; }

        public static void Start(int maxVisited = 1000000)
        {
            if (running || PuzzleShortestPathSolver.Active != null)
                throw new InvalidOperationException("A shortest-path search is already running.");
            if (maxVisited < 1) throw new ArgumentOutOfRangeException(nameof(maxVisited));
            stateLimit = maxVisited;
            nextMap = 0;
            running = true;
            LastMessage = "Starting SW00–SW19 exact shortest-input search.";
            StartNext();
        }

        public static void Cancel()
        {
            running = false;
            PuzzleShortestPathSolver.Cancel();
            LastMessage = "Batch cancelled.";
        }

        static void StartNext()
        {
            if (!running) return;
            while (nextMap < 20)
            {
                string id = "SW" + nextMap.ToString("00");
                nextMap++;
                var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                    "Assets/ANIMOL/Data/Puzzle/MapData/starway/" + id + "_MapData.asset");
                if (!map) { LastMessage = id + " is missing."; continue; }
                var prior = PuzzleShortestPathRecord.Find(map);
                if (prior != null && PuzzleShortestPathRecord.IsCurrent(map, prior) &&
                    (prior.status == "solved" || prior.status == "unreachable" ||
                        (prior.status == "limit" && prior.visited >= stateLimit))) continue;
                if (map.ValidateMap().Count > 0) { LastMessage = id + ": " + map.ValidateMap()[0]; continue; }
                PuzzleShortestPathSolver.Start(map, result =>
                {
                    try
                    {
                        if (result.status != "cancelled") PuzzleShortestPathRecord.Save(result);
                        LastMessage = PuzzleShortestPathRecord.MapId(result.map) + ": " + result.status +
                            (result.Solved ? " in " + result.distance + " inputs" : " — " + result.detail);
                        StartNext();
                    }
                    catch (Exception error)
                    {
                        running = false;
                        LastMessage = "Could not save " + id + ": " + error.Message;
                        Debug.LogException(error);
                    }
                }, stateLimit);
                return;
            }
            running = false;
            LastMessage = "SW00–SW19 search finished.";
        }
    }
}
