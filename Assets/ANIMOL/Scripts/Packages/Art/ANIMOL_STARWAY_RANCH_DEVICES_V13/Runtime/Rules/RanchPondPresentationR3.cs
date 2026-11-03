using System;

namespace ANIMOL.Gameplay.RanchDevicesR2
{
    // Pure presentation mapping. Pair label order may differ from screen orientation.
    public sealed class RanchPondCueR3
    {
        public string axis, waterDirection, startKey, transferKey, endKey;
        public RanchCellR2 waterFrom, waterTo;
    }

    public static class RanchPondPresentationR3
    {
        public static bool TryCue(RanchPondPairR2 before, out RanchPondCueR3 cue, out string error)
        {
            cue = null; error = null;
            if (before == null) { error = "Pond pair required."; return false; }
            long dx = (long)before.b.x - before.a.x, dy = (long)before.b.y - before.a.y;
            if (Math.Abs(dx) + Math.Abs(dy) != 1)
            { error = "Use two adjacent cardinal cells."; return false; }
            var from = before.Wet; var to = before.Dry;
            string axis = dx != 0 ? "horizontal" : "vertical";
            string flow = to.x > from.x ? "east" : to.x < from.x ? "west" : to.y > from.y ? "north" : "south";
            string sourceSide = flow == "east" ? "west" : flow == "west" ? "east" : flow == "north" ? "south" : "north";
            string prefix = "pond_pair/" + axis + "/";
            cue = new RanchPondCueR3 { axis = axis, waterDirection = flow, waterFrom = from, waterTo = to,
                startKey = prefix + sourceSide + "_wet", transferKey = prefix + "water_" + flow, endKey = prefix + flow + "_wet" };
            return true;
        }
    }
}
