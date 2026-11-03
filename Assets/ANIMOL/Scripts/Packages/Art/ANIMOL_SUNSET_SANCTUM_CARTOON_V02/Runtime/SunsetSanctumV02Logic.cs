using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ANIMOL.Art.SunsetSanctumV02
{
    public enum SanctumHeading { North, East, South, West }
    public enum SanctumMotion { Walking, Sliding }
    [Serializable]
    public struct SanctumCell : IEquatable<SanctumCell>
    {
        public readonly int x, y;
        public SanctumCell(int x, int y) { this.x=x; this.y=y; }
        public bool Equals(SanctumCell other) { return x==other.x && y==other.y; }
        public override bool Equals(object value) { return value is SanctumCell && Equals((SanctumCell)value); }
        public override int GetHashCode() { unchecked { return (x*397)^y; } }
    }
    public static class SanctumDirections
    {
        public static bool Valid(SanctumHeading h) { return (int)h>=0 && (int)h<4; }
        public static SanctumCell Delta(SanctumHeading h)
        {
            switch(h) {
                case SanctumHeading.North:return new SanctumCell(0,1);
                case SanctumHeading.East:return new SanctumCell(1,0);
                case SanctumHeading.South:return new SanctumCell(0,-1);
                case SanctumHeading.West:return new SanctumCell(-1,0);
                default:throw new ArgumentOutOfRangeException("h");
            }
        }
        public static SanctumCell Step(SanctumCell c, SanctumHeading h, int count=1)
        { SanctumCell d=Delta(h); return new SanctumCell(checked(c.x+d.x*count),checked(c.y+d.y*count)); }
        public static bool Perpendicular(SanctumHeading a, SanctumHeading b)
        { return Valid(a) && Valid(b) && (((int)a-(int)b)&1)==1; }
        // These are board-plane mirror orientations, NOT sprite rotations or device IDs.
        public static SanctumHeading ReflectSlash(SanctumHeading h)
        {
            switch(h) { case SanctumHeading.East:return SanctumHeading.North;
                case SanctumHeading.North:return SanctumHeading.East;
                case SanctumHeading.West:return SanctumHeading.South;
                case SanctumHeading.South:return SanctumHeading.West;
                default:throw new ArgumentOutOfRangeException("h"); }
        }
        public static SanctumHeading ReflectBackslash(SanctumHeading h)
        {
            switch(h) { case SanctumHeading.East:return SanctumHeading.South;
                case SanctumHeading.South:return SanctumHeading.East;
                case SanctumHeading.West:return SanctumHeading.North;
                case SanctumHeading.North:return SanctumHeading.West;
                default:throw new ArgumentOutOfRangeException("h"); }
        }
    }

    public enum PortalPlanResult { Allowed, NotAdjacentEntry, OutputBlocked }
    public struct PortalVisualPlan
    {
        public readonly PortalPlanResult result;
        public readonly SanctumCell beforeEntry, entry, paired, beyondExit;
        public readonly SanctumHeading travel;
        public readonly SanctumMotion motion;
        public bool allowed { get { return result==PortalPlanResult.Allowed; } }
        public bool continueSliding { get { return allowed && motion==SanctumMotion.Sliding; } }
        public bool showBlockedExit { get { return result==PortalPlanResult.OutputBlocked; } }
        internal PortalVisualPlan(PortalPlanResult r, SanctumCell from, SanctumCell a,
            SanctumCell b, SanctumCell output, SanctumHeading h, SanctumMotion m)
        { result=r;beforeEntry=from;entry=a;paired=b;beyondExit=output;travel=h;motion=m; }
    }
    public static class SanctumPortalPreview
    {
        // A pure pre-commit plan. Does not teleport, charge a turn, or change movement state.
        // The host checks the actual paired portal and the cell BEYOND it before invoking.
        public static PortalVisualPlan Evaluate(SanctumCell actualFrom, SanctumCell entry,
            SanctumCell paired, SanctumHeading travel, SanctumMotion motion, bool beyondExitIsTraversable)
        {
            if (!SanctumDirections.Valid(travel)) throw new ArgumentOutOfRangeException("travel");
            if (motion!=SanctumMotion.Walking && motion!=SanctumMotion.Sliding)
                throw new ArgumentOutOfRangeException("motion");
            if (entry.Equals(paired)) throw new ArgumentException("A portal pair needs two different actual cells.");
            SanctumCell output=SanctumDirections.Step(paired,travel);
            PortalPlanResult result=!SanctumDirections.Step(actualFrom,travel).Equals(entry)
                ? PortalPlanResult.NotAdjacentEntry
                : beyondExitIsTraversable ? PortalPlanResult.Allowed : PortalPlanResult.OutputBlocked;
            return new PortalVisualPlan(result,actualFrom,entry,paired,output,travel,motion);
        }
    }

    public enum JumpHintStatus { HostPolicyUnresolved, HostAllowed, HostBlocked }
    public struct JumpVisualHint
    {
        public readonly SanctumCell source, landing;
        public readonly SanctumHeading travel;
        public readonly JumpHintStatus status;
        public JumpVisualHint(SanctumCell source, SanctumHeading travel, bool? authoritativeHostAllows)
        {
            this.source=source;this.travel=travel;
            landing=SanctumDirections.Step(source,travel,3);
            status=!authoritativeHostAllows.HasValue ? JumpHintStatus.HostPolicyUnresolved
                : authoritativeHostAllows.Value ? JumpHintStatus.HostAllowed : JumpHintStatus.HostBlocked;
        }
        // Hint only. Exact input timing/blocked landing/transit reactions are source-unresolved.
        // The existing gameplay controller remains responsible for all acceptance and movement.
    }

    public enum SanctumLightCellKind { Open, Glass, Mirror, Receiver, Opaque, Void }
    public enum SanctumLightStop { RangeEnd, Opaque, Void, Receiver, DirectedCycle }
    public struct SanctumLightVisit
    {
        public readonly SanctumLightCellKind kind;
        public readonly string receiverKey;
        public readonly bool receiverStopsRay;
        public SanctumLightVisit(SanctumLightCellKind kind, string receiverKey=null, bool receiverStopsRay=true)
        { this.kind=kind;this.receiverKey=receiverKey;this.receiverStopsRay=receiverStopsRay; }
    }
    public interface ISanctumLightBoard
    {
        // Evaluate current authoritative occupancy. Glass transmits; bodies/walls do not.
        // After glass breaks an actor on that cell MUST still classify as Opaque.
        SanctumLightVisit Visit(SanctumCell cell, SanctumHeading incoming);
        // Convert the real mirror orientation. A non-reflecting back face must return false.
        bool TryReflect(SanctumCell cell, SanctumHeading incoming, out SanctumHeading outgoing);
    }
    public struct SanctumLightCell
    {
        public readonly SanctumCell cell;
        public readonly SanctumHeading incoming,outgoing;
        public readonly SanctumLightCellKind kind;
        public readonly string receiverKey;
        public SanctumLightCell(SanctumCell c,SanctumHeading a,SanctumHeading b,SanctumLightVisit visit)
        { cell=c;incoming=a;outgoing=b;kind=visit.kind;receiverKey=visit.receiverKey; }
    }
    public sealed class SanctumLightRoute
    {
        public readonly SanctumCell emitter;
        public readonly SanctumHeading heading;
        public readonly ReadOnlyCollection<SanctumLightCell> cells;
        public readonly ReadOnlyCollection<string> litReceiverKeys;
        public readonly SanctumLightStop stop;
        public readonly SanctumCell? excludedCell;
        internal SanctumLightRoute(SanctumCell e,SanctumHeading h,List<SanctumLightCell> c,
            List<string> receivers,SanctumLightStop stop,SanctumCell? excluded)
        { emitter=e;heading=h;cells=c.AsReadOnly();litReceiverKeys=receivers.AsReadOnly();this.stop=stop;excludedCell=excluded; }
        public bool LightsReceiver(string actualKey) { return litReceiverKeys.Contains(actualKey); }
        // Public adapter for an ALREADY authoritative host trace. It avoids a second
        // visual-only ray computation and remains usable across project asmdef boundaries.
        public static SanctumLightRoute FromAuthoritativeTrace(SanctumCell emitter,
            SanctumHeading heading,IEnumerable<SanctumLightCell> orderedCells,
            SanctumLightStop stop,SanctumCell? excludedCell=null)
        {
            if(!SanctumDirections.Valid(heading))throw new ArgumentOutOfRangeException("heading");
            if(orderedCells==null)throw new ArgumentNullException("orderedCells");
            var list=new List<SanctumLightCell>();var receivers=new List<string>();
            var seenReceivers=new HashSet<string>(StringComparer.Ordinal);
            SanctumCell previous=emitter;SanctumHeading travel=heading;
            foreach(SanctumLightCell c in orderedCells)
            {
                if(!SanctumDirections.Step(previous,travel).Equals(c.cell) || c.incoming!=travel)
                    throw new ArgumentException("Host light cells must be ordered and cardinally contiguous.");
                if((int)c.kind<0 || (int)c.kind>(int)SanctumLightCellKind.Void)throw new ArgumentException("Unknown light cell kind.");
                if(c.kind==SanctumLightCellKind.Opaque || c.kind==SanctumLightCellKind.Void)
                    throw new ArgumentException("Blocking cells are excluded from a visible light route.");
                if(c.kind==SanctumLightCellKind.Mirror ? !SanctumDirections.Perpendicular(c.incoming,c.outgoing) : c.incoming!=c.outgoing)
                    throw new ArgumentException("Host light turn must occur at a perpendicular mirror.");
                if(!string.IsNullOrEmpty(c.receiverKey) && seenReceivers.Add(c.receiverKey))receivers.Add(c.receiverKey);
                list.Add(c);previous=c.cell;travel=c.outgoing;
            }
            return new SanctumLightRoute(emitter,heading,list,receivers,stop,excludedCell);
        }
    }
    public static class SanctumLightTrace
    {
        private struct DirectedCell : IEquatable<DirectedCell>
        {
            private readonly SanctumCell cell;private readonly SanctumHeading heading;
            public DirectedCell(SanctumCell c,SanctumHeading h) { cell=c;heading=h; }
            public bool Equals(DirectedCell x) { return cell.Equals(x.cell) && heading==x.heading; }
            public override bool Equals(object x) { return x is DirectedCell && Equals((DirectedCell)x); }
            public override int GetHashCode() { return cell.GetHashCode()*31+(int)heading; }
        }
        // Same immutable result must drive BOTH receiver/linked state and the beam mesh.
        // If the project already has an authoritative trace, adapt that trace instead of
        // running a competing calculation. No input, movement, timer, or cost is committed here.
        public static SanctumLightRoute Trace(ISanctumLightBoard board,SanctumCell emitter,
            SanctumHeading heading,int existingTraceCellLimit)
        {
            if(board==null)throw new ArgumentNullException("board");
            if(!SanctumDirections.Valid(heading))throw new ArgumentOutOfRangeException("heading");
            if(existingTraceCellLimit<1)throw new ArgumentOutOfRangeException("existingTraceCellLimit");
            var cells=new List<SanctumLightCell>();var receivers=new List<string>();
            var receiverSet=new HashSet<string>(StringComparer.Ordinal);var visited=new HashSet<DirectedCell>();
            SanctumCell current=emitter;SanctumHeading travel=heading;
            visited.Add(new DirectedCell(emitter,heading));
            for(int i=0;i<existingTraceCellLimit;i++)
            {
                SanctumCell next=SanctumDirections.Step(current,travel);
                SanctumLightVisit v=board.Visit(next,travel);
                if(v.kind==SanctumLightCellKind.Void || v.kind==SanctumLightCellKind.Opaque)
                    return new SanctumLightRoute(emitter,heading,cells,receivers,
                        v.kind==SanctumLightCellKind.Void?SanctumLightStop.Void:SanctumLightStop.Opaque,next);
                if(!visited.Add(new DirectedCell(next,travel)))
                    return new SanctumLightRoute(emitter,heading,cells,receivers,SanctumLightStop.DirectedCycle,next);
                SanctumHeading outgoing=travel;
                if(v.kind==SanctumLightCellKind.Mirror)
                {
                    if(!board.TryReflect(next,travel,out outgoing))
                        return new SanctumLightRoute(emitter,heading,cells,receivers,SanctumLightStop.Opaque,next);
                    if(!SanctumDirections.Perpendicular(travel,outgoing))
                        throw new InvalidOperationException("Authoritative mirror callback must return a90degree reflection.");
                }
                if(v.kind<SanctumLightCellKind.Open || v.kind>SanctumLightCellKind.Void)
                    throw new ArgumentException("Unknown light classification.");
                cells.Add(new SanctumLightCell(next,travel,outgoing,v));
                if(!string.IsNullOrEmpty(v.receiverKey) && receiverSet.Add(v.receiverKey))receivers.Add(v.receiverKey);
                if(v.kind==SanctumLightCellKind.Receiver && v.receiverStopsRay)
                    return new SanctumLightRoute(emitter,heading,cells,receivers,SanctumLightStop.Receiver,null);
                current=next;travel=outgoing;
            }
            return new SanctumLightRoute(emitter,heading,cells,receivers,SanctumLightStop.RangeEnd,null);
        }
    }
}
