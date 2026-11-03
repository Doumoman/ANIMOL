using System;
using System.Collections.Generic;
using NUnit.Framework;
using ANIMOL.Art.SunsetSanctumV02;

public sealed class SunsetSanctumV02LogicTests
{
    private sealed class Board : ISanctumLightBoard
    {
        public readonly Dictionary<SanctumCell,SanctumLightVisit> cells=new Dictionary<SanctumCell,SanctumLightVisit>();
        public bool slash=true;
        public SanctumLightVisit Visit(SanctumCell c,SanctumHeading h)
        { SanctumLightVisit value;return cells.TryGetValue(c,out value)?value:new SanctumLightVisit(SanctumLightCellKind.Open); }
        public bool TryReflect(SanctumCell c,SanctumHeading h,out SanctumHeading next)
        { next=slash?SanctumDirections.ReflectSlash(h):SanctumDirections.ReflectBackslash(h);return true; }
    }
    [Test]
    public void GlassPassesLightButBodyOnBrokenGlassStopsItAndDropsReceiver()
    {
        var b=new Board();var glass=new SanctumCell(1,0);var sensor=new SanctumCell(3,0);
        b.cells[glass]=new SanctumLightVisit(SanctumLightCellKind.Glass);
        b.cells[sensor]=new SanctumLightVisit(SanctumLightCellKind.Receiver,"actual_sensor");
        var before=SanctumLightTrace.Trace(b,new SanctumCell(0,0),SanctumHeading.East,8);
        Assert.IsTrue(before.LightsReceiver("actual_sensor"));Assert.AreEqual(3,before.cells.Count);
        b.cells[glass]=new SanctumLightVisit(SanctumLightCellKind.Opaque);
        var after=SanctumLightTrace.Trace(b,new SanctumCell(0,0),SanctumHeading.East,8);
        Assert.AreEqual(0,after.cells.Count);Assert.IsFalse(after.LightsReceiver("actual_sensor"));
        Assert.AreEqual(SanctumLightStop.Opaque,after.stop);
    }
    [Test]
    public void MirrorBeamAndReceiverUseSameOrderedRoute()
    {
        var b=new Board();b.cells[new SanctumCell(2,0)]=new SanctumLightVisit(SanctumLightCellKind.Mirror);
        b.cells[new SanctumCell(2,2)]=new SanctumLightVisit(SanctumLightCellKind.Receiver,"linked_door");
        var r=SanctumLightTrace.Trace(b,new SanctumCell(0,0),SanctumHeading.East,8);
        Assert.AreEqual(4,r.cells.Count);Assert.AreEqual(SanctumHeading.North,r.cells[1].outgoing);
        Assert.IsTrue(r.cells[3].cell.Equals(new SanctumCell(2,2)));Assert.IsTrue(r.LightsReceiver("linked_door"));
        b.slash=false;var changed=SanctumLightTrace.Trace(b,new SanctumCell(0,0),SanctumHeading.East,8);
        Assert.IsFalse(changed.LightsReceiver("linked_door"));
    }
    [Test]
    public void MirrorCycleStopsAtRepeatedDirectedCell()
    {
        var b=new CycleBoard();var r=SanctumLightTrace.Trace(b,new SanctumCell(0,0),SanctumHeading.East,100);
        Assert.AreEqual(SanctumLightStop.DirectedCycle,r.stop);Assert.LessOrEqual(r.cells.Count,12);
    }
    private sealed class CycleBoard : ISanctumLightBoard
    {
        public SanctumLightVisit Visit(SanctumCell c,SanctumHeading h)
        { return new SanctumLightVisit((c.x==2 || c.x==0) && (c.y==0 || c.y==2)?SanctumLightCellKind.Mirror:SanctumLightCellKind.Open); }
        public bool TryReflect(SanctumCell c,SanctumHeading h,out SanctumHeading o)
        { bool slash=(c.x==2 && c.y==0)||(c.x==0 && c.y==2);o=slash?SanctumDirections.ReflectSlash(h):SanctumDirections.ReflectBackslash(h);return true; }
    }
    [Test]
    public void PortalPreservesAllFourWorldHeadingsAndBothMotionStates()
    {
        var a=new SanctumCell(3,4);var b=new SanctumCell(11,8);
        foreach(SanctumHeading h in Enum.GetValues(typeof(SanctumHeading)))
        foreach(SanctumMotion m in Enum.GetValues(typeof(SanctumMotion)))
        {
            var from=SanctumDirections.Step(a,h,-1);var p=SanctumPortalPreview.Evaluate(from,a,b,h,m,true);
            Assert.IsTrue(p.allowed);Assert.AreEqual(h,p.travel);Assert.AreEqual(m,p.motion);
            Assert.IsTrue(p.beyondExit.Equals(SanctumDirections.Step(b,h)));
            Assert.AreEqual(m==SanctumMotion.Sliding,p.continueSliding);
            var blocked=SanctumPortalPreview.Evaluate(from,a,b,h,m,false);
            Assert.IsFalse(blocked.allowed);Assert.IsTrue(blocked.beforeEntry.Equals(from));
            Assert.IsTrue(blocked.showBlockedExit);Assert.IsFalse(blocked.continueSliding);
        }
    }
    [Test]
    public void JumpPolicyIsNeverAssumedWhenSourceLeavesItUnresolved()
    {
        var h=new JumpVisualHint(new SanctumCell(2,2),SanctumHeading.North,null);
        Assert.AreEqual(JumpHintStatus.HostPolicyUnresolved,h.status);
        Assert.IsTrue(h.landing.Equals(new SanctumCell(2,5)));
    }
    [Test]
    public void FullEightNeighborMappingKeepsAll256MasksDistinct()
    {
        var set=new HashSet<string>();for(int m=0;m<256;m++)set.Add(SunsetSanctumV02TerrainSelector.File(SunsetSanctumV02TerrainSelector.Material.SanctumFloor,m));
        Assert.AreEqual(256,set.Count);
        Assert.AreNotEqual(SunsetSanctumV02TerrainSelector.File(SunsetSanctumV02TerrainSelector.Material.SanctumFloor,0),
            SunsetSanctumV02TerrainSelector.File(SunsetSanctumV02TerrainSelector.Material.SanctumFloor,2));
    }
    [Test]
    public void LongUndersidesRemainContinuousDeterministicAndNeverHaveOneCellTail()
    {
        for(int n=1;n<=100;n++) {
            int[] parts=SunsetSanctumV02TerrainSelector.PlanContinuousSpanLengths(n,41,-12,35);
            int total=0;foreach(int p in parts) { total+=p;Assert.That(p,Is.InRange(n<=4?1:2,4)); }
            Assert.AreEqual(n,total);if(n<=4)Assert.AreEqual(1,parts.Length);
            CollectionAssert.AreEqual(parts,SunsetSanctumV02TerrainSelector.PlanContinuousSpanLengths(n,41,-12,35));
        }
    }
}
