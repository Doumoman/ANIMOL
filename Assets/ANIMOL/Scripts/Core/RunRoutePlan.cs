using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Core
{
    [Serializable]
    public sealed class RunLeg
    {
        public string Id;
        public int X;
        public int FloorY;
        public int Length;
        public int Direction;
        public RectInt Corridor => new RectInt(X, FloorY, Length, 4);
        public RunLeg(string id, int x, int y, int length, int direction)
        { Id = id; X = x; FloorY = y; Length = length; Direction = direction; }
    }

    [Serializable]
    public sealed class TurnConnector
    {
        public RectInt ReservedSpace;
        public RectInt Approach;
        public RectInt Landing;
        public Vector2Int SpringCell;
        public Vector2 SpringImpulse;
        public bool Ascending;
    }

    /// <summary>Macro reservations precede terrain decoration; storage remains StageMapDefinition's 16-cell chunks.</summary>
    public sealed class RunRoutePlan : ScriptableObject
    {
        public const int MinimumRunTiles = 5 * StageMapDefinition.TilesPerChunk;
        public const string SolidTile = "RUN_PROTO_SOLID";
        public const string OneWayTile = "RUN_PROTO_ONEWAY";
        public const string ApproachTile = "RUN_PROTO_APPROACH_ONEWAY";
        public RunLeg[] Legs;
        public TurnConnector Turn;
        public RectInt OptionalZone;
        public int Seed;

        public static RunRoutePlan Reserve(bool ascending, int seed = 8016)
        {
            var plan = CreateInstance<RunRoutePlan>();
            plan.Seed = seed;
            // Both legs span five complete storage chunks. Connector starts at x=80, never in the 80-tile budget.
            plan.Legs = new[] { new RunLeg("RunLeg-A", 0, ascending ? 0 : 8, MinimumRunTiles, 1),
                                new RunLeg("RunLeg-B", 0, ascending ? 8 : 0, MinimumRunTiles, -1) };
            plan.Turn = new TurnConnector {
                Ascending = ascending,
                ReservedSpace = new RectInt(80, -2, 18, 24),
                Approach = new RectInt(80, plan.Legs[0].FloorY, 12, 1),
                Landing = new RectInt(80, plan.Legs[1].FloorY, 16, 1),
                SpringCell = new Vector2Int(94, plan.Legs[0].FloorY - 1),
                SpringImpulse = new Vector2(-8, ascending ? 23 : 0)
            };
            plan.OptionalZone = new RectInt(-16, plan.Legs[0].FloorY, 12, 8);
            return plan;
        }

        public bool IsReserved(int x, int y) => Turn.ReservedSpace.Contains(new Vector2Int(x, y)) ||
            Legs.Any(leg => leg.Corridor.Contains(new Vector2Int(x, y)));

        public void PopulateEmptyMap(StageMapDefinition map)
        {
            if (map.Cells.Count != 0 || map.Objects.Count != 0)
                throw new InvalidOperationException("Only an empty prototype map may be populated.");
            if (Legs.Any(leg => leg.Length < MinimumRunTiles)) throw new InvalidOperationException("RunLeg is shorter than five chunks.");
            map.EditorInitializeIdentity(Turn.Ascending ? "RUN-PROTO-UP" : "RUN-PROTO-DOWN", "T01");
            map.EditorTrySetChunkBounds(new RectInt(-1, -1, 8, 3), false, out _);
            // Phase 1 reservations are complete above. Phase 2 materializes the continuous required route.
            foreach (var leg in Legs)
                WriteFloor(map, new RectInt(leg.X, leg.FloorY, leg.Length, 1), SurfaceFor(leg.FloorY));
            // A one-way approach lets the descending rebound pass its underside without snagging the corner.
            WriteFloor(map, Turn.Approach, ApproachTile);
            WriteFloor(map, Turn.Landing, SurfaceFor(Legs[1].FloorY));
            // Safe starting / finishing aprons are outside the measured RunLegs.
            foreach (var leg in Legs) WriteFloor(map, new RectInt(-4, leg.FloorY, 4, 1), SurfaceFor(leg.FloorY));
            // Reachable optional stepping stones branch left of the starting apron, away from the required route.
            for (int step = 1; step <= 3; step++)
            {
                int x = -3 - step * 3, y = Legs[0].FloorY + step;
                if (!IsReserved(x, y)) WriteFloor(map, new RectInt(x, y, 2, 1), OneWayTile);
            }
            map.EditorInitializeVariableChunksFromAuthoredContent(1f);
            map.EditorTrySetChunkBounds(new RectInt(-1, -1, 8, 3), false, out _);
            map.EditorMarkCollisionDataSynchronized();
        }

        private string SurfaceFor(int floorY) => Turn.Ascending && floorY == Legs[1].FloorY ? OneWayTile : SolidTile;
        private static void WriteFloor(StageMapDefinition map, RectInt floor, string tile)
        {
            for (int x = floor.xMin; x < floor.xMax; x++) map.EditorSetCell(x, floor.yMin, StageMapLayer.Terrain, tile);
        }

        public int ContinuousFloorLength(StageMapDefinition map, RunLeg leg)
        {
            int count = 0;
            for (int x = leg.X; x < leg.X + leg.Length; x++)
            {
                if (map.FindCell(x, leg.FloorY, StageMapLayer.Terrain) == null) break;
                count++;
            }
            return count;
        }
    }
}
