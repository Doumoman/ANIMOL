using System;

namespace ANIMOL.StarwayMaps.Editor
{
    // Expected observations from the REPORT MODEL, not a second game engine.
    // Compare to the actual PuzzleDeviceSession at settled logical turn boundaries.
    [Serializable] public sealed class StarwayChickObservation
    {
        public StarwayCell position;
        public bool returned;
        public StarwayCell[] trail;
    }
    [Serializable] public sealed class StarwaySheepObservation
    {
        public bool exists;
        public StarwayCell position; // Ignored when exists=false.
    }
    [Serializable] public sealed class StarwayTurnObservation
    {
        public int turn;
        public string action;
        public StarwayCell playerBefore;
        public StarwayCell playerAfter;
        public StarwayCell[] playerPath;
        public StarwayChickObservation[] chicks;
        public StarwaySheepObservation[] sheep;
        public StarwayCell[] remainingGrass;
        public bool[] wetA;
        public int[] windPhases;
        public int activeSocketMask;
        public int bubbleMask;
    }
    [Serializable] public sealed class StarwayReplayExpectations
    {
        public string schema;
        public string id;
        public string verificationScope;
        public string actions;
        public int inputCount;
        public StarwayTurnObservation[] steps;
    }
}
