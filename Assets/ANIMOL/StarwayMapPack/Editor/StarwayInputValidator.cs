using System;
using System.Collections.Generic;

namespace ANIMOL.StarwayMaps.Editor
{
    public static class StarwayInputValidator
    {
        public static void ValidateAll(StarwayMapInput[] inputs)
        {
            Require(inputs != null && inputs.Length == 20, "Expected exactly 20 maps.");
            int[] counts = new int[4];
            int lateSheep = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                Validate(inputs[i], i + 1);
                counts[inputs[i].map.difficulty]++;
                if (i >= 10) lateSheep += inputs[i].sheepPlanning.Length;
            }
            Require(counts[1] == 10 && counts[2] == 5 && counts[3] == 5, "Difficulty split must be 10/5/5.");
            Require(lateSheep == 17, "The v3 late stages contain 17 sheep.");
        }

        public static void Validate(StarwayMapInput input, int index)
        {
            string id = "SW" + index.ToString("00");
            Require(input != null && input.schema == "animol.unity-map-input.v1", id + ": unsupported input schema.");
            Require(input.designVersion == 3 && input.verificationScope == "REPORT_MODEL_ONLY", id + ": wrong design provenance.");
            StarwayMapDto m = input.map;
            Require(m != null && m.id == id && m.index == index, id + ": identity/order mismatch.");
            Require(m.difficulty == (index <= 10 ? 1 : index <= 15 ? 2 : 3), id + ": wrong difficulty.");
            Require(m.themeId == "starway" && m.outsideRegion == "void_nonwalkable", id + ": wrong theme/void contract.");
            Require(m.size.width > 0 && m.size.width <= 18 && m.size.height > 0 && m.size.height <= 22, id + ": invalid size.");
            Require(m.targetBubbles == 3 && m.costLimit == 0 && m.goals != null && m.goals.Length == 0, id + ": changed win/failure rules.");
            HashSet<StarwayCell> floor = Cells(m.walkableCells, id + " floor");
            HashSet<StarwayCell> region = Cells(m.regionCells, id + " region");
            HashSet<StarwayCell> walls = Cells(m.blockedCells, id + " walls");
            Require(floor.Count > 0 && floor.IsSubsetOf(region) && walls.IsSubsetOf(region), id + ": cells outside region.");
            Require(!floor.Overlaps(walls) && floor.Count + walls.Count == region.Count, id + ": floor/wall partition mismatch.");
            foreach (StarwayCell c in region)
                Require(c.x >= 0 && c.x < m.size.width && c.y >= 0 && c.y < m.size.height, id + ": out-of-bounds cell " + c);
            Require(floor.Contains(m.start) && floor.Contains(m.exit), id + ": start/exit without floor.");
            HashSet<StarwayCell> bubbles = Cells(m.bubbles, id + " bubbles");
            Require(bubbles.Count == 3 && bubbles.IsSubsetOf(floor), id + ": exactly three bubbles on base floor required.");
            Require(m.water != null && m.water.Length == 0, id + ": unexpected fixed water; review native blocking semantics.");
            Require(m.devices != null && m.ranchV13Art != null && m.ranchSluiceDesign != null, id + ": null device array.");
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<StarwayCell> occupied = new HashSet<StarwayCell>();
            HashSet<StarwayCell> nests = new HashSet<StarwayCell>();
            foreach (StarwayPrototype d in m.devices)
            {
                Require(d != null, id + ": null prototype.");
                AddId(ids, d.id, id);
                Require(floor.Contains(d.position) && occupied.Add(d.position), id + ": invalid/overlapping prototype position.");
                Require(AbsSum(d.direction) == 1, id + ": non-cardinal device direction.");
                Require(d.role == 1 || d.role == 7 || d.role == 9 || d.role == 18 || d.role == 19 || d.role == 20 || d.role == 24,
                    id + ": unknown role; do not invent enum values.");
                if (d.role == 24)
                {
                    Require((d.target.x != 0 || d.target.y != 0) && floor.Contains(d.target), id + ": missing chick nest.");
                    Require(!d.target.Equals(d.position) && nests.Add(d.target) && d.number >= 1 && d.number <= 8, id + ": invalid chick target/lag.");
                }
                if (d.role == 1 && d.variant == "socket") Require(d.socketReward == "None", id + ": star matching must remain optional.");
                if (d.role == 20) Require(d.variant == "blower" && d.orientation >= 0 && d.orientation <= 3, id + ": invalid blower.");
            }
            List<StarwayCell> grasses = new List<StarwayCell>();
            List<StarwayCell> sheep = new List<StarwayCell>();
            foreach (StarwayRanchObject d in m.ranchV13Art)
            {
                Require(d != null, id + ": null ranch object.");
                AddId(ids, d.instanceId, id);
                Require(d.family == "sheep" || d.family == "tall_grass", id + ": unknown ranch family.");
                Require(floor.Contains(d.position) && occupied.Add(d.position), id + ": invalid/overlapping ranch position.");
                if (d.family == "sheep") sheep.Add(d.position); else grasses.Add(d.position);
            }
            foreach (StarwaySluice d in m.ranchSluiceDesign)
            {
                Require(d != null, id + ": null sluice.");
                AddId(ids, d.instanceId, id);
                Require(d.hasPondB && Distance(d.position, d.pondB) == 1, id + ": sluice must occupy exactly two adjacent cells.");
                Require(floor.Contains(d.position) && floor.Contains(d.pondB) && occupied.Add(d.position) && occupied.Add(d.pondB), id + ": invalid/overlapping sluice.");
                Require(!(d.rightState ? d.position : d.pondB).Equals(m.start), id + ": start on wet cell.");
            }
            foreach (StarwayCell n in nests) Require(!occupied.Contains(n) && !n.Equals(m.start), id + ": nest initially occupied.");
            foreach (StarwayPrototype d in m.devices) Require(!d.position.Equals(m.start), id + ": occupied start.");
            foreach (StarwayRanchObject d in m.ranchV13Art) Require(!d.position.Equals(m.start), id + ": occupied start.");
            Require(input.camera != null && input.camera.visibleColumns == 7 && input.camera.visibleRows == 11,
                id + ": focus must be 7 by 11 cells.");
            Require(input.camera.layoutMode == "exact_focus_fit_inside_gameplay_area" && input.camera.boundsSource == "regionCellsAABB",
                id + ": unexpected camera layout/bounds contract.");
            Require(input.camera.clampToBounds && input.camera.centerSmallMaps && input.camera.snapOnStageLoad, id + ": camera flags changed.");
            ValidateActions(input.referenceActions, id);
            ValidateActions(input.noStarMatchingActions, id);
            Require(input.referenceActions.Length == input.referenceSuccessfulInputs, id + ": reference length mismatch.");
            int lo = index <= 10 ? 30 : index <= 15 ? 48 : 68;
            int hi = index <= 10 ? 32 : index <= 15 ? 52 : 72;
            Require(input.referenceSuccessfulInputs >= lo && input.referenceSuccessfulInputs <= hi, id + ": reference input budget changed.");
            Require(input.sheepPlanning != null, id + ": null sheep proof.");
            if (index >= 11)
            {
                Require(input.sheepPlanning.Length == sheep.Count, id + ": missing late sheep proof.");
                foreach (StarwayCell y in sheep) foreach (StarwayCell g in grasses)
                    Require(Distance(y, g) >= 3, id + ": sheep starts too near grass.");
                HashSet<int> planned = new HashSet<int>();
                foreach (StarwaySheepPlan p in input.sheepPlanning)
                {
                    Require(p != null && p.sheepIndex >= 1 && p.sheepIndex <= sheep.Count && planned.Add(p.sheepIndex), id + ": invalid sheep proof ID.");
                    Require(p.initialPosition.Equals(sheep[p.sheepIndex - 1]) && grasses.Contains(p.grassPosition), id + ": wrong sheep proof endpoints.");
                    Require(p.initialManhattanDistance == Distance(p.initialPosition, p.grassPosition) && p.initialManhattanDistance >= 3 && p.initialManhattanDistance <= 6,
                        id + ": sheep-grass separation outside 3-6.");
                    Require(p.physicalTravel >= 3 && p.physicalTravel <= 6 && p.positions != null && p.positions.Length == p.physicalTravel + 1,
                        id + ": invalid physical transport length.");
                    Require(p.positions[0].Equals(p.initialPosition) && p.positions[p.positions.Length - 1].Equals(p.grassPosition), id + ": route endpoint mismatch.");
                    for (int j = 1; j < p.positions.Length; j++) Require(Distance(p.positions[j - 1], p.positions[j]) == 1, id + ": nonadjacent sheep route.");
                    if (p.directPushInputs > 0) Require(p.directPushInputs >= 3 && p.directPushInputs <= 6 && p.directionChanges >= 1, id + ": direct sheep route lacks a turn.");
                }
            }
        }

        private static void ValidateActions(string actions, string id)
        {
            Require(!string.IsNullOrEmpty(actions), id + ": empty reference solution.");
            foreach (char c in actions) Require(c == 'N' || c == 'E' || c == 'S' || c == 'W', id + ": invalid input token.");
        }
        private static HashSet<StarwayCell> Cells(StarwayCell[] list, string label)
        {
            Require(list != null, label + ": null array.");
            HashSet<StarwayCell> set = new HashSet<StarwayCell>(list);
            Require(set.Count == list.Length, label + ": duplicate cell."); return set;
        }
        private static void AddId(HashSet<string> ids, string value, string label)
        { Require(!string.IsNullOrEmpty(value) && ids.Add(value), label + ": missing/duplicate instance ID."); }
        private static int AbsSum(StarwayCell c) { return Math.Abs(c.x) + Math.Abs(c.y); }
        private static int Distance(StarwayCell a, StarwayCell b) { return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y); }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
