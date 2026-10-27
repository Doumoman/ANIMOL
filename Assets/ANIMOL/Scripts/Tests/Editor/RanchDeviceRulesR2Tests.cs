using NUnit.Framework;
using ANIMOL.Gameplay.RanchDevicesR2;

namespace ANIMOL.Tests.RanchDevicesR2
{
    // Copy to an EditMode test assembly after the host has installed Unity Test Framework.
    public sealed class RanchDeviceRulesR2Tests
    {
        static RanchCellR2 C(int x, int y = 0) { return new RanchCellR2(x, y); }
        static readonly RanchCellR2 E = new RanchCellR2(1, 0), W = new RanchCellR2(-1, 0);
        static RanchRuleBoardR2 Board()
        {
            var b = new RanchRuleBoardR2 { player = C(0) };
            for (int y = -1; y <= 1; y++) for (int x = 0; x <= 6; x++) b.ground.Add(C(x, y), RanchGroundR2.Floor);
            return b;
        }
        static RanchRuleBoardR2 Pond()
        {
            var b = Board(); b.ponds.Add(new RanchPondPairR2 { instanceId = "p", a = C(0), b = C(1), wetA = false }); return b;
        }
        static RanchRuleBoardR2 Sheep()
        { var b = Board(); b.sheep.Add("s", C(1)); return b; }

        [Test] public void CrossingSwapsBothCellsAndMovesPlayerForOneInput()
        {
            var before = Pond(); RanchRuleBoardR2 after; RanchRuleResultR2 r;
            Assert.IsTrue(RanchDeviceRulesR2.TryCrossPond(before, E, out after, out r));
            Assert.AreEqual(C(1), after.player); Assert.IsTrue(after.IsWet(C(0))); Assert.IsFalse(after.IsWet(C(1)));
            Assert.AreEqual(1, after.costSpent); Assert.AreEqual(1, after.deviceTurns);
            Assert.AreEqual(RanchRuleEventR2.PondCrossed, r.eventKind); Assert.AreEqual(2, r.actualRoute.Length);
        }
        [Test] public void CrossingDoesNotTreatTheDepartingPlayerAsAFloodBlocker()
        {
            RanchRuleBoardR2 next; RanchRuleResultR2 r;
            Assert.IsTrue(RanchDeviceRulesR2.TryCrossPond(Pond(), E, out next, out r));
        }
        [Test] public void CrossingBackRestoresWaterArrangement()
        {
            RanchRuleBoardR2 one, two; RanchRuleResultR2 r;
            Assert.IsTrue(RanchDeviceRulesR2.TryCrossPond(Pond(), E, out one, out r));
            Assert.IsTrue(RanchDeviceRulesR2.TryCrossPond(one, W, out two, out r));
            Assert.AreEqual(C(0), two.player); Assert.IsTrue(two.IsWet(C(1))); Assert.AreEqual(2, two.costSpent);
        }
        [Test] public void OccupiedPondDestinationRejectsWithoutPartialSwapOrCost()
        {
            var b = Pond(); b.solidOccupants.Add(C(1)); RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsFalse(RanchDeviceRulesR2.TryCrossPond(b, E, out n, out r));
            Assert.AreSame(b, n); Assert.IsTrue(n.IsWet(C(1))); Assert.AreEqual(0, r.costDelta); Assert.AreEqual(0, r.turnDelta);
        }
        [Test] public void UnresolvedFloodOccupantReturnsDiagnosticWithoutInventingPolicy()
        {
            var b = Pond(); b.floodPolicyHolds.Add(C(0)); RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsFalse(RanchDeviceRulesR2.TryCrossPond(b, E, out n, out r));
            Assert.AreEqual(RanchRuleCodeR2.FloodPolicyRequired, r.code); Assert.AreSame(b, n);
        }
        [Test] public void UnpairedOrdinaryStepIsLeftToTheHost()
        {
            RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsFalse(RanchDeviceRulesR2.TryCrossPond(Board(), E, out n, out r));
            Assert.AreEqual(RanchRuleCodeR2.NotApplicable, r.code); Assert.AreEqual(0, r.costDelta);
        }
        [Test] public void OverlappingPondPairsAreRejectedAsInvalidData()
        {
            var b = Pond(); b.ponds.Add(new RanchPondPairR2 { instanceId = "q", a = C(1), b = C(2), wetA = false });
            string error; Assert.IsFalse(RanchDeviceRulesR2.Validate(b, out error));
        }
        [Test] public void DirectSheepPushLeavesPlayerStationary()
        {
            var b = Sheep(); RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsTrue(RanchDeviceRulesR2.TryPushSheep(b, "s", E, out n, out r));
            Assert.AreEqual(b.player, n.player); Assert.AreEqual(C(2), n.sheep["s"]);
            Assert.AreEqual(1, n.costSpent); Assert.AreEqual(1, n.deviceTurns);
        }
        [Test] public void SheepEnteringGrassRemovesBothSheepAndThatGrass()
        {
            var b = Sheep(); b.ground[C(2)] = RanchGroundR2.TallGrass; RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsTrue(RanchDeviceRulesR2.TryPushSheep(b, "s", E, out n, out r));
            Assert.IsFalse(n.sheep.ContainsKey("s")); Assert.AreEqual(RanchGroundR2.Floor, n.ground[C(2)]);
            Assert.AreEqual(C(0), n.player); Assert.IsTrue(r.ateGrass && r.removedSheep);
        }
        [Test] public void SuccessfulTransactionsPreserveOriginalSnapshotForUndo()
        {
            var b = Sheep(); b.ground[C(2)] = RanchGroundR2.TallGrass; RanchRuleBoardR2 n; RanchRuleResultR2 r;
            RanchDeviceRulesR2.TryPushSheep(b, "s", E, out n, out r);
            Assert.AreEqual(C(1), b.sheep["s"]); Assert.AreEqual(RanchGroundR2.TallGrass, b.ground[C(2)]); Assert.AreEqual(0, b.costSpent);
        }
        [Test] public void WallBlocksSheepWithoutSpendingInput()
        {
            var b = Sheep(); b.ground[C(2)] = RanchGroundR2.Wall; RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsFalse(RanchDeviceRulesR2.TryPushSheep(b, "s", E, out n, out r)); Assert.AreSame(b, n); Assert.AreEqual(0, r.costDelta);
        }
        [Test] public void AnotherSheepIsNotChainPushed()
        {
            var b = Sheep(); b.sheep.Add("t", C(2)); RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsFalse(RanchDeviceRulesR2.TryPushSheep(b, "s", E, out n, out r)); Assert.AreSame(b, n);
        }
        [Test] public void WindCarriesSheepToLastActualWindCellForZeroAdditionalCost()
        {
            var b = Sheep(); RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsTrue(RanchDeviceRulesR2.TryWindSheep(b, "s", E, new[] { C(1), C(2), C(3) }, 3, out n, out r));
            Assert.AreEqual(C(3), n.sheep["s"]); Assert.AreEqual(3, r.actualRoute.Length);
            Assert.AreEqual(0, n.costSpent); Assert.AreEqual(0, n.deviceTurns); Assert.IsTrue(r.automatic);
        }
        [Test] public void WindStopsBeforeWall()
        {
            var b = Sheep(); b.ground[C(3)] = RanchGroundR2.Wall; RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsTrue(RanchDeviceRulesR2.TryWindSheep(b, "s", E, new[] { C(1), C(2), C(3) }, 3, out n, out r));
            Assert.AreEqual(C(2), n.sheep["s"]); Assert.AreEqual(2, r.actualRoute.Length);
        }
        [Test] public void WindDoesNotMovePlayerOutOfTheWay()
        {
            var b = Sheep(); b.player = C(2); RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsFalse(RanchDeviceRulesR2.TryWindSheep(b, "s", E, new[] { C(1), C(2), C(3) }, 3, out n, out r));
            Assert.AreSame(b, n); Assert.AreEqual(C(2), n.player);
        }
        [Test] public void WindEatsOnlyFirstGrassAndRemovesSheep()
        {
            var b = Sheep(); b.ground[C(2)] = b.ground[C(3)] = RanchGroundR2.TallGrass; RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsTrue(RanchDeviceRulesR2.TryWindSheep(b, "s", E, new[] { C(1), C(2), C(3) }, 3, out n, out r));
            Assert.IsFalse(n.sheep.ContainsKey("s")); Assert.AreEqual(RanchGroundR2.Floor, n.ground[C(2)]);
            Assert.AreEqual(RanchGroundR2.TallGrass, n.ground[C(3)]); Assert.AreEqual(2, r.actualRoute.Length);
        }
        [Test] public void WindAfterDirectPushDoesNotSpendASecondTurn()
        {
            var b = Sheep(); b.ground[C(3)] = RanchGroundR2.TallGrass; RanchRuleBoardR2 pushed, blown; RanchRuleResultR2 r;
            Assert.IsTrue(RanchDeviceRulesR2.TryPushSheep(b, "s", E, out pushed, out r));
            Assert.IsTrue(RanchDeviceRulesR2.TryWindSheep(pushed, "s", E, new[] { C(1), C(2), C(3) }, 3, out blown, out r));
            Assert.AreEqual(1, blown.costSpent); Assert.AreEqual(1, blown.deviceTurns); Assert.IsFalse(blown.sheep.ContainsKey("s"));
        }
        [Test] public void SheepOutsideWindIsUnaffected()
        {
            var b = Sheep(); RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsFalse(RanchDeviceRulesR2.TryWindSheep(b, "s", E, new[] { C(2), C(3), C(4) }, 3, out n, out r));
            Assert.AreEqual(RanchRuleCodeR2.NotApplicable, r.code);
        }
        [Test] public void DiscontinuousWindInputIsRejected()
        {
            RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsFalse(RanchDeviceRulesR2.TryWindSheep(Sheep(), "s", E, new[] { C(1), C(3) }, 3, out n, out r));
            Assert.AreEqual(RanchRuleCodeR2.InvalidWind, r.code);
        }
        [Test] public void DiagonalPushDoesNotChangeAnything()
        {
            var b = Sheep(); RanchRuleBoardR2 n; RanchRuleResultR2 r;
            Assert.IsFalse(RanchDeviceRulesR2.TryPushSheep(b, "s", C(1, 1), out n, out r));
            Assert.AreEqual(RanchRuleCodeR2.InvalidDirection, r.code); Assert.AreSame(b, n);
        }
        [Test] public void RemovedSheepCannotEatAgain()
        {
            var b = Sheep(); b.ground[C(2)] = RanchGroundR2.TallGrass; RanchRuleBoardR2 eaten, next; RanchRuleResultR2 r;
            RanchDeviceRulesR2.TryPushSheep(b, "s", E, out eaten, out r);
            Assert.IsFalse(RanchDeviceRulesR2.TryWindSheep(eaten, "s", E, new[] { C(1), C(2), C(3) }, 3, out next, out r));
            Assert.AreSame(eaten, next); Assert.AreEqual(1, next.costSpent);
        }
    }
}
