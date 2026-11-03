using System;
using UnityEngine;

namespace ANIMOL.StarwayMaps
{
    // This DTO is an interchange format, never a replacement for PuzzleMap/PuzzleScenario.
    [Serializable]
    public struct StarwayCell : IEquatable<StarwayCell>
    {
        public int x;
        public int y;
        public StarwayCell(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(StarwayCell other) { return x == other.x && y == other.y; }
        public override bool Equals(object obj) { return obj is StarwayCell && Equals((StarwayCell)obj); }
        public override int GetHashCode() { unchecked { return (x * 397) ^ y; } }
        public override string ToString() { return "(" + x + "," + y + ")"; }
        public Vector2Int ToVector2Int() { return new Vector2Int(x, y); }
    }

    [Serializable]
    public struct StarwayGridSize { public int width; public int height; }

    [Serializable]
    public sealed class StarwayPrototype
    {
        public string id;
        public int role;
        public string variant;
        public StarwayCell position;
        public StarwayCell direction;
        public StarwayCell target;
        public int number;
        public int orientation;
        public string socketReward;
    }

    [Serializable]
    public sealed class StarwayRanchObject
    {
        public string instanceId;
        public string family;
        public StarwayCell position;
    }

    [Serializable]
    public sealed class StarwaySluice
    {
        public string instanceId;
        public StarwayCell position;  // A. Never reorder A/B by coordinate.
        public StarwayCell pondB;     // B.
        public bool hasPondB;
        public bool rightState;      // wetA, NOT "right cell is wet".
    }

    [Serializable]
    public sealed class StarwayGoal
    {
        public string deviceId;
        public string kind;
        public StarwayCell position;
        public int number;
    }

    [Serializable]
    public sealed class StarwayMapDto
    {
        public string id;
        public int index;
        public string title;
        public string shape;
        public int difficulty;
        public string difficultyName;
        public string themeId;
        public StarwayGridSize size;
        public string outsideRegion;
        public StarwayCell[] walkableCells;
        public StarwayCell[] regionCells;
        public StarwayCell[] blockedCells;
        public StarwayCell start;
        public StarwayCell exit;
        public StarwayCell[] bubbles;
        public int targetBubbles;
        public int costLimit;
        public StarwayGoal[] goals;
        public StarwayPrototype[] devices;
        public StarwayRanchObject[] ranchV13Art;
        public StarwaySluice[] ranchSluiceDesign;
        public StarwayCell[] water;
    }

    [Serializable]
    public sealed class StarwayCameraSpec
    {
        public int visibleColumns;
        public int visibleRows;
        public string layoutMode;
        public string boundsSource;
        public bool clampToBounds;
        public bool centerSmallMaps;
        public bool snapOnStageLoad;
    }

    [Serializable]
    public sealed class StarwaySheepPlan
    {
        public int sheepIndex;
        public int grassIndex;
        public StarwayCell initialPosition;
        public StarwayCell grassPosition;
        public int initialManhattanDistance;
        public int physicalTravel;
        public int directPushInputs;
        public int windTravel;
        public int directionChanges;
        public StarwayCell[] positions;
        // Proof/authoring data only. Do not turn the pairing into a game rule.
    }

    [Serializable]
    public sealed class StarwayMapInput
    {
        public string schema;
        public int designVersion;
        public string sourceSha256;
        public string solutionSha256;
        public string verificationScope;
        public int referenceSuccessfulInputs;
        public string referenceActions;
        public string noStarMatchingActions;
        public StarwayMapDto map;
        public StarwayCameraSpec camera;
        public StarwaySheepPlan[] sheepPlanning;
    }

    [Serializable]
    public sealed class StarwayPackEntry
    {
        public string id;
        public string inputSha256;
        public string sourceSha256;
        public string solutionSha256;
        public string replaySha256;
    }

    [Serializable]
    public sealed class StarwayPackIndex
    {
        public string schema;
        public int version;
        public int mapCount;
        public StarwayPackEntry[] entries;
    }
}
