// Headless value/map adapters. Every supported transition is source-linked from Unity.
using System.Text.Json;

namespace UnityEngine
{
    public class Object { public string name; public static implicit operator bool(Object value) => value is not null; }
    public class ScriptableObject : Object { }
    public class GameObject : Object { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public static class Debug { public static void LogException(Exception error) => Console.Error.WriteLine(error); }
    public static class Mathf { public static int Abs(int value) => Math.Abs(value); public static int Clamp(int value,int min,int max) => Math.Clamp(value,min,max); }
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x,y;
        public Vector2Int(int x,int y) { this.x=x;this.y=y; }
        public static Vector2Int zero => new(0,0);
        public static Vector2Int up => new(0,1); public static Vector2Int down => new(0,-1);
        public static Vector2Int left => new(-1,0); public static Vector2Int right => new(1,0);
        public int sqrMagnitude => x*x+y*y;
        public bool Equals(Vector2Int other) => x==other.x && y==other.y;
        public override bool Equals(object other) => other is Vector2Int v && Equals(v);
        public override int GetHashCode() => unchecked(x*73856093 ^ y*83492791);
        public static bool operator ==(Vector2Int a,Vector2Int b) => a.Equals(b);
        public static bool operator !=(Vector2Int a,Vector2Int b) => !a.Equals(b);
        public static Vector2Int operator +(Vector2Int a,Vector2Int b) => new(a.x+b.x,a.y+b.y);
        public static Vector2Int operator -(Vector2Int a,Vector2Int b) => new(a.x-b.x,a.y-b.y);
        public static Vector2Int operator -(Vector2Int a) => new(-a.x,-a.y);
        public static Vector2Int operator *(Vector2Int a,int b) => new(a.x*b,a.y*b);
        public override string ToString() => "("+x+", "+y+")";
    }
}
namespace UnityEditor
{
    public static class EditorApplication
    {
        public static bool isPlayingOrWillChangePlaymode => false;
        public static event Action update;
        public static void Pump() => update?.Invoke();
    }
    public static class EditorUtility { public static bool IsDirty(UnityEngine.Object value) => false; }
    public static class AssetDatabase { public static string GetAssetPath(ANIMOL.Puzzle.PuzzleMap map) => map.assetPath; }
}
namespace ANIMOL.Art.EchoRailCartV01
{
    public sealed class EchoRailPathV02
    {
        // Geometry validation belongs to the Unity export. Gameplay reads the
        // source-linked PuzzleScenario.RailCellsFor lists without calling this.
        public static bool TryCreate(UnityEngine.Vector2Int[] cells,bool closed,out EchoRailPathV02 path,out string error)
            => throw new NotSupportedException("Rail geometry must be validated by the Unity export.");
    }
}
namespace ANIMOL.Puzzle
{
    using UnityEngine;
    public enum DeviceKind { None }
    public struct TerrainCell { public Vector2Int position; public bool blocked; }
    public struct DeviceCell { public DeviceKind kind; }
    public class Theme : Object { public RanchTheme starwayRanch = new(); }
    public class RanchTheme : Object { public bool v13Devices = true; }
    public sealed class PuzzleMap : ScriptableObject
    {
        public string assetPath;
        public int width,height,targetBubbles;
        public bool validated;
        public int Width => width; public int Height => height;
        public Vector2Int start,exit;
        public PuzzleScenario scenario;
        public Theme theme = new();
        public bool starwayV3Certificate;
        public List<TerrainCell> terrain = new();
        public List<DeviceCell> devices = new();
        public List<Vector2Int> bubbles = new();
        public HashSet<Vector2Int> decorations = new();
        Dictionary<Vector2Int,TerrainCell> index;
        public bool Contains(Vector2Int p) => p.x>=0 && p.y>=0 && p.x<width && p.y<height;
        public bool DecorationAt(Vector2Int p) => Contains(p) && decorations.Contains(p);
        public bool TryTerrain(Vector2Int p, out TerrainCell cell)
        {
            index ??= terrain.ToDictionary(t=>t.position);
            cell = default; return Contains(p) && index.TryGetValue(p, out cell);
        }
        // The export validator rejects legacy device kinds; this adapter only supports None.
        public DeviceCell DeviceAt(Vector2Int p) => default;
        public bool Walkable(Vector2Int p)
        {
            index ??= terrain.ToDictionary(t=>t.position);
            return Contains(p) && index.TryGetValue(p,out var t) && !t.blocked && !DecorationAt(p);
        }
        public List<string> ValidateMap() => validated ? new() : throw new InvalidDataException("Map was not validated in Unity.");
        public void BeginReadOnlySearch() { }
        public void EndReadOnlySearch() { }
    }
    public static class PuzzleSession { public static bool IsCardinal(Vector2Int p) => Math.Abs(p.x)+Math.Abs(p.y)==1; }
}
