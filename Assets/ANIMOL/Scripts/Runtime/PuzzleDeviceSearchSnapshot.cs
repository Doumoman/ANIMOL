#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    public sealed partial class PuzzleDeviceSession
    {
        // Editor search checkpoints contain settled gameplay state, without Undo,
        // presentation events or derived light/wind graphs. Never used by game saves.
        public sealed class SearchSnapshot
        {
            internal readonly PuzzleMap map;
            internal readonly byte[] bytes;
            internal readonly string boat, failure;
            public int ByteCount => bytes.Length;
            internal SearchSnapshot(PuzzleMap map, byte[] bytes, string boat, string failure)
            { this.map = map; this.bytes = bytes; this.boat = boat; this.failure = failure; }
        }

        sealed class SearchBuffer
        {
            readonly List<byte> output;
            readonly byte[] input;
            int cursor;
            public SearchBuffer() { output = new List<byte>(256); }
            public SearchBuffer(byte[] bytes) { input = bytes; }
            public byte[] ToArray() => output.ToArray();
            public void Write(int value)
            {
                uint bits = unchecked((uint)((value << 1) ^ (value >> 31)));
                while (bits >= 128) { output.Add((byte)((bits & 127) | 128)); bits >>= 7; }
                output.Add((byte)bits);
            }
            public int Read()
            {
                uint value = 0; int shift = 0; byte next;
                do { next = input[cursor++]; value |= (uint)(next & 127) << shift; shift += 7; }
                while ((next & 128) != 0);
                return unchecked((int)((value >> 1) ^ (uint)-(int)(value & 1)));
            }
            public void Point(Vector2Int p) { Write(p.x); Write(p.y); }
            public Vector2Int Point() => new Vector2Int(Read(), Read());
            public void Cells(IEnumerable<Vector2Int> cells, int count)
            { Write(count); foreach (var cell in cells) Point(cell); }
            public void Cells(ICollection<Vector2Int> cells)
            { int count = Read(); for (int i = 0; i < count; i++) cells.Add(Point()); }
        }

        public SearchSnapshot CaptureSearchSnapshot()
        {
            var data = new SearchBuffer();
            data.Point(Cell); data.Point(Direction); data.Write(Cost); data.Write(PortalCuts);
            data.Write((Won ? 1 : 0) | (Failed ? 2 : 0));
            foreach (var d in definitions)
            {
                var s = states[d.id];
                data.Point(s.cell); data.Point(s.direction); data.Point(s.lookDirection);
                data.Write((s.active ? 1 : 0) | (s.removed ? 2 : 0) | (s.locked ? 4 : 0) | (s.converted ? 8 : 0));
                data.Write(s.phase); data.Write(s.timer);
                data.Write(s.dice.Length); foreach (int face in s.dice) data.Write(face);
                data.Cells(s.trail, s.trail.Count);
            }
            // Preserve iteration order as well as IDs: wind processes sheep in this order.
            data.Write(ranchSheep.Count);
            foreach (var sheep in ranchSheep)
            { data.Write(Array.IndexOf(searchSheepIds, sheep.Key)); data.Point(sheep.Value); }
            foreach (string id in searchSheepIds) data.Point(RanchSheepDirection(id));
            foreach (string id in searchPondIds) data.Write(ranchPonds[id] ? 1 : 0);
            data.Cells(consumedRanchGrass, consumedRanchGrass.Count);
            data.Cells(consumedNests, consumedNests.Count);
            data.Cells(collected, collected.Count); data.Cells(visited, visited.Count);
            return new SearchSnapshot(Map, data.ToArray(), BoatId, Failure);
        }

        // The template supplies immutable map context; checkpoint data is read only.
        public PuzzleDeviceSession RestoreSearchSnapshot(SearchSnapshot snapshot)
        {
            if (snapshot == null || snapshot.map != Map)
                throw new ArgumentException("Search checkpoint belongs to another map.");
            return new PuzzleDeviceSession(this, snapshot);
        }

        PuzzleDeviceSession(PuzzleDeviceSession template, SearchSnapshot snapshot)
        {
            Map = template.Map; definitions = template.definitions;
            ranchPondCells = template.ranchPondCells; ranchGrassPositions = template.ranchGrassPositions;
            searchUnlabelledCrates = template.searchUnlabelledCrates;
            searchSheepOrderMatters = template.searchSheepOrderMatters;
            searchSheepIds = template.searchSheepIds; searchPondIds = template.searchPondIds;
            var data = new SearchBuffer(snapshot.bytes);
            Cell = data.Point(); Direction = data.Point(); Cost = data.Read(); PortalCuts = data.Read();
            int flags = data.Read(); Won = (flags & 1) != 0; Failed = (flags & 2) != 0;
            BoatId = snapshot.boat; Failure = snapshot.failure;
            foreach (var d in definitions)
            {
                var s = new PrototypeDeviceState { cell = data.Point(), direction = data.Point(), lookDirection = data.Point() };
                flags = data.Read(); s.active = (flags & 1) != 0; s.removed = (flags & 2) != 0;
                s.locked = (flags & 4) != 0; s.converted = (flags & 8) != 0;
                s.phase = data.Read(); s.timer = data.Read();
                s.dice = new int[data.Read()]; for (int i = 0; i < s.dice.Length; i++) s.dice[i] = data.Read();
                data.Cells(s.trail); states.Add(d.id, s);
            }
            int sheepCount = data.Read();
            for (int i = 0; i < sheepCount; i++) ranchSheep.Add(searchSheepIds[data.Read()], data.Point());
            foreach (string id in searchSheepIds) ranchSheepDirections.Add(id, data.Point());
            foreach (string id in searchPondIds) ranchPonds.Add(id, data.Read() != 0);
            data.Cells(consumedRanchGrass); data.Cells(consumedNests); data.Cells(collected); data.Cells(visited);
            TraceLight(); TraceWind(); events.Clear();
        }
    }
}
#endif
