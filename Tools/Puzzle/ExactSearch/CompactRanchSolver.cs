using System.Numerics;
using System.Reflection;
using System.Diagnostics;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using UnityEngine;

// Restricted value-state implementation. The source-linked authority remains
// the oracle: unknown rules are rejected, every build verifies parity, and Unity
// replays any final route. No scene, authored map or game-save writes occur here.
public readonly record struct CompactRanchState(ulong Crates, ulong Sheep, uint Info);

public sealed class CompactRanchModel
{
    public readonly PuzzleMap Map;
    public readonly Vector2Int[] Floor;
    readonly Dictionary<Vector2Int,int> cells;
    readonly int[,] adjacent;
    readonly int[] grass, bubble, baseDistances, chickDistances;
    readonly PuzzleSearchLowerBound authorityBound;
    readonly int[,] sheepDistances;
    readonly string chickId;
    readonly PuzzleDeviceSession authorityTemplate;
    readonly PrototypeDevice[] crateDefinitions;
    readonly string[] sheepIds;
    static readonly PropertyInfo CellProperty=typeof(PuzzleDeviceSession).GetProperty("Cell");
    static readonly PropertyInfo CostProperty=typeof(PuzzleDeviceSession).GetProperty("Cost");
    static readonly FieldInfo SheepField=typeof(PuzzleDeviceSession).GetField("ranchSheep",Private);
    static readonly FieldInfo CollectedField=typeof(PuzzleDeviceSession).GetField("collected",Private);
    static readonly FieldInfo GrassField=typeof(PuzzleDeviceSession).GetField("consumedRanchGrass",Private);
    static readonly FieldInfo NestsField=typeof(PuzzleDeviceSession).GetField("consumedNests",Private);
    static readonly MethodInfo VictoryMethod=typeof(PuzzleDeviceSession).GetMethod("Victory",Private);
    readonly int n, nest, exit;
    public readonly CompactRanchState Initial;
    public int CellCount => n;
    static readonly Vector2Int[] Directions = {Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    public static CompactRanchModel TryCreate(PuzzleMap map)
    {
        var rules=map.scenario;
        var chick=rules.devices.Where(d=>d.role==PrototypeRole.Chick).ToArray();
        if(rules.themeId!="starway" || rules.echoTheaterV3Rules || rules.costLimit!=0 ||
            rules.goals.Count!=0 || rules.water.Count!=0 || rules.railPaths.Count!=0 ||
            map.devices.Any(d=>d.kind!=DeviceKind.None) || chick.Length!=1 ||
            chick[0].active || Math.Clamp(chick[0].number,1,8)!=1 || rules.NestCells.Count()!=1 ||
            rules.devices.Any(d=>d.role!=PrototypeRole.MetalCrate && d.role!=PrototypeRole.Chick || d.active || d.orientation!=0) ||
            rules.ranchV13Art.Any(a=>a==null || a.family!="sheep" && a.family!="tall_grass") ||
            rules.ranchV13Art.Count(a=>a.family=="sheep")>4 ||
            map.bubbles.Count>3 || map.bubbles.Distinct().Count()!=map.bubbles.Count ||
            rules.ranchV13Art.Count(a=>a.family=="tall_grass")>10) return null;
        int count=map.terrain.Select(t=>t.position).Distinct().Count(map.Walkable);
        if(count==0 || count>63 || rules.devices.Any(d=>!map.Walkable(d.position))) return null;
        return new CompactRanchModel(map,chick[0]);
    }

    CompactRanchModel(PuzzleMap map,PrototypeDevice chick)
    {
        Map=map; chickId=chick.id;
        Floor=map.terrain.Select(t=>t.position).Distinct().Where(map.Walkable).ToArray(); n=Floor.Length;
        cells=Floor.Select((p,i)=>(p,i)).ToDictionary(x=>x.p,x=>x.i);
        adjacent=new int[n,4]; grass=Enumerable.Repeat(-1,n).ToArray(); bubble=new int[n];
        for(int p=0;p<n;p++)for(int d=0;d<4;d++)adjacent[p,d]=cells.TryGetValue(Floor[p]+Directions[d],out int to)?to:-1;
        nest=cells[map.scenario.NestCells.Single()]; exit=cells[map.exit];
        int g=0;foreach(var placement in map.scenario.ranchV13Art.Where(a=>a.family=="tall_grass"))grass[cells[placement.position]]=g++;
        for(int b=0;b<map.bubbles.Count;b++)bubble[cells[map.bubbles[b]]]=1<<b;
        sheepDistances=new int[map.bubbles.Count,n];
        for(int b=0;b<map.bubbles.Count;b++)
        {
            for(int p=0;p<n;p++)sheepDistances[b,p]=-1;
            int goal=cells[map.bubbles[b]];var queue=new Queue<int>();queue.Enqueue(goal);sheepDistances[b,goal]=0;
            while(queue.Count>0)
            {
                int current=queue.Dequeue();
                for(int d=0;d<4;d++)
                {
                    int previous=adjacent[current,(d+2)%4];
                    // A manual sheep push always needs the player behind the sheep.
                    // Ignore all occupants/grass here, allowing more routes than reality.
                    if(previous<0 || adjacent[previous,(d+2)%4]<0 || sheepDistances[b,previous]>=0)continue;
                    sheepDistances[b,previous]=sheepDistances[b,current]+1;queue.Enqueue(previous);
                }
            }
        }
        var authority=new PuzzleDeviceSession(map); Initial=FromSession(authority);
        authorityTemplate=authority;
        crateDefinitions=authority.Devices.Where(d=>d.role==PrototypeRole.MetalCrate).ToArray();
        sheepIds=authority.RanchSheep.Keys.OrderBy(id=>id,StringComparer.Ordinal).ToArray();
        var bound=PuzzleSearchLowerBound.TryCreate(map) ?? throw new NotSupportedException("Missing authority lower bound");
        authorityBound=bound;
        baseDistances=(int[])typeof(PuzzleSearchLowerBound).GetField("distances",Private).GetValue(bound);
        var order=(Dictionary<Vector2Int,int>)typeof(PuzzleSearchLowerBound).GetField("cells",Private).GetValue(bound);
        if(order.Any(p=>cells[p.Key]!=p.Value))throw new Exception("Heuristic cell order differs");
        var chickBound=typeof(PuzzleSearchLowerBound).GetField("chickBound",Private).GetValue(bound);
        chickDistances=chickBound==null?null:(int[])chickBound.GetType().GetField("distances",Private).GetValue(chickBound);
    }

    static ulong Bit(int p)=>1UL<<p;
    public static int Player(CompactRanchState s)=>(int)(s.Info&63);
    int Chick(CompactRanchState s)=>(int)((s.Info>>6)&63);
    int Trail(CompactRanchState s)=>(int)((s.Info>>12)&63);
    static int Mask(CompactRanchState s)=>(int)((s.Info>>18)&7);
    static int Grass(CompactRanchState s)=>(int)(s.Info>>21);
    static uint Pack(int p,int c,int t,int mask,int consumed)=>(uint)(p | c<<6 | t<<12 | mask<<18 | consumed<<21);
    bool Near(int a,int b)=>(Floor[a]-Floor[b]).sqrMagnitude==1;
    bool GrassPresent(int cell,int consumed)=>grass[cell]>=0 && (consumed&(1<<grass[cell]))==0;

    public CompactRanchState FromSession(PuzzleDeviceSession source)
    {
        ulong crates=0,sheep=0;int consumed=0,mask=0;
        foreach(var d in source.Devices.Where(d=>d.role==PrototypeRole.MetalCrate))
        {
            var state=source.States[d.id];
            if(state.active || state.removed || state.locked || state.converted || state.phase!=0 || state.timer!=0)
                throw new NotSupportedException("Unexpected mutable crate rule");
            crates|=Bit(cells[state.cell]);
        }
        foreach(var p in source.RanchSheep.Values)sheep|=Bit(cells[p]);
        for(int p=0;p<n;p++)if(grass[p]>=0 && !source.RanchGrassPresent(Floor[p]))consumed|=1<<grass[p];
        for(int b=0;b<Map.bubbles.Count;b++)if(source.IsCollected(Map.bubbles[b]))mask|=1<<b;
        var chick=source.States[chickId];
        if(chick.active!=chick.removed || chick.trail.Count>1 || source.NestPresent(Floor[nest])==chick.removed)
            throw new NotSupportedException("Unexpected nest/follower state");
        int c=chick.removed?n:cells[chick.cell],t=chick.trail.Count==0?n:cells[chick.trail[0]];
        return new(crates,sheep,Pack(cells[source.Cell],c,t,mask,consumed));
    }

    public bool Won(CompactRanchState state)=>Player(state)==exit && BitOperations.PopCount((uint)Mask(state))>=Map.targetBubbles;

    public PuzzleDeviceSession RestoreAuthority(CompactRanchState state,int cost)
    {
        var result=authorityTemplate.Copy();
        CellProperty.SetValue(result,Floor[Player(state)]);CostProperty.SetValue(result,cost);
        int index=0;
        for(ulong bits=state.Crates;bits!=0;bits&=bits-1)result.States[crateDefinitions[index++].id].cell=Floor[BitOperations.TrailingZeroCount(bits)];
        if(index!=crateDefinitions.Length)throw new Exception("Crate count changed");
        var sheep=(Dictionary<string,Vector2Int>)SheepField.GetValue(result);sheep.Clear();index=0;
        for(ulong bits=state.Sheep;bits!=0;bits&=bits-1)sheep.Add(sheepIds[index++],Floor[BitOperations.TrailingZeroCount(bits)]);
        var chick=result.States[chickId];int c=Chick(state),trail=Trail(state);
        chick.active=chick.removed=c==n;chick.cell=Floor[c==n?nest:c];chick.trail.Clear();if(trail<n)chick.trail.Add(Floor[trail]);
        var collected=(HashSet<Vector2Int>)CollectedField.GetValue(result);collected.Clear();
        for(int b=0;b<Map.bubbles.Count;b++)if((Mask(state)&(1<<b))!=0)collected.Add(Map.bubbles[b]);
        var consumed=(HashSet<Vector2Int>)GrassField.GetValue(result);consumed.Clear();
        for(int p=0;p<n;p++)if(grass[p]>=0 && (Grass(state)&(1<<grass[p]))!=0)consumed.Add(Floor[p]);
        var nests=(HashSet<Vector2Int>)NestsField.GetValue(result);nests.Clear();if(c==n)nests.Add(Floor[nest]);
        VictoryMethod.Invoke(result,null);
        if(FromSession(result)!=state || result.Won!=Won(state))throw new Exception("Authority restore mismatch");
        return result;
    }

    public bool TryStep(CompactRanchState state,int direction,out CompactRanchState next)
    {
        next=state;if(Won(state))return false;
        int p=Player(state),c=Chick(state),t=Trail(state),mask=Mask(state),consumed=Grass(state);
        int to=adjacent[p,direction];if(to<0)return false;
        ulong crates=state.Crates,sheep=state.Sheep,at=Bit(to);
        if((sheep&at)!=0)
        {
            int target=adjacent[to,direction];
            if(target<0 || target==c || target==nest && c<n || ((crates|sheep)&Bit(target))!=0)return false;
            sheep^=at;
            if(GrassPresent(target,consumed))consumed|=1<<grass[target];else sheep|=Bit(target);
            next=new(crates,sheep,Pack(p,c,t,mask,consumed));return true;
        }
        if(GrassPresent(to,consumed) || to==nest && c<n || to==c)return false;
        if((crates&at)!=0)
        {
            int target=adjacent[to,direction];
            if(target<0 || target==c || target==nest && c<n || GrassPresent(target,consumed) || ((crates|sheep)&Bit(target))!=0)return false;
            crates=(crates^at)|Bit(target);
            next=new(crates,sheep,Pack(p,c,t,mask,consumed));return true;
        }
        mask|=bubble[to];
        if(c<n)
        {
            if(c==nest || Near(c,nest)){c=n;t=n;}
            else
            {
                int target=t;t=p;
                if(target<n && Near(target,c) && target!=to && !GrassPresent(target,consumed) && ((crates|sheep)&Bit(target))==0)
                {
                    c=target;
                    if(c==nest || Near(c,nest)){c=n;t=n;}
                }
            }
        }
        next=new(crates,sheep,Pack(to,c,t,mask,consumed));return true;
    }

    public int Estimate(CompactRanchState state)
    {
        int sheep=RequiredSheepPushes(state);
        return sheep==int.MaxValue?int.MaxValue:MovementEstimate(state)+sheep;
    }
    int MovementEstimate(CompactRanchState state)
    {
        int p=Player(state),c=Chick(state),t=Trail(state),mask=Mask(state);
        int h=Math.Max(0,baseDistances[mask*n+p]);
        if(chickDistances==null)return h;
        int heading=0;
        if(t<n){for(int d=0;d<4;d++)if(adjacent[p,d]==t){heading=d+1;break;}}
        return Math.Max(h,Math.Max(0,chickDistances[(((mask*(n+1)+c)*5+heading)*n)+p]));
    }

    int RequiredSheepPushes(CompactRanchState state)
    {
        if(Map.targetBubbles!=Map.bubbles.Count)return 0;
        int needed=0,consumed=Grass(state),mask=Mask(state);
        for(int p=0;p<n;p++)if(bubble[p]!=0 && (mask&bubble[p])==0 && GrassPresent(p,consumed))needed|=bubble[p];
        if(BitOperations.PopCount(state.Sheep)<BitOperations.PopCount((uint)needed))return int.MaxValue;
        return Assign(needed,state.Sheep);
    }
    int Assign(int needed,ulong available)
    {
        if(needed==0)return 0;
        int goal=BitOperations.TrailingZeroCount((uint)needed),rest=needed&~(1<<goal),best=int.MaxValue;
        for(ulong choices=available;choices!=0;choices&=choices-1)
        {
            int sheep=BitOperations.TrailingZeroCount(choices),distance=sheepDistances[goal,sheep];
            if(distance<0)continue;
            int tail=Assign(rest,available&~Bit(sheep));
            if(tail!=int.MaxValue)best=Math.Min(best,distance+tail);
        }
        return best;
    }

    public object VerifyAuthority(int samples)
    {
        var random=new Random(571);var root=new PuzzleDeviceSession(Map);var authority=root.Copy();var state=Initial;
        int edges=0,acceptedCount=0;var seen=new HashSet<CompactRanchState>();
        for(int sample=0;sample<samples;sample++)
        {
            if(state!=FromSession(authority))throw new Exception("Compact before-state mismatch");
            var restored=RestoreAuthority(state,authority.Cost);
            if(restored.SearchStateKey()!=authority.SearchStateKey())throw new Exception("Restored canonical key mismatch");
            if(MovementEstimate(state)!=authorityBound.Estimate(authority))throw new Exception("Compact authority heuristic mismatch");
            seen.Add(state);var choices=new List<(PuzzleDeviceSession,CompactRanchState)>();
            for(int d=0;d<4;d++)
            {
                var child=authority.Copy();bool actual=child.Apply(Directions[d]).accepted;
                bool accepted=TryStep(state,d,out var result);
                var restoredChild=restored.Copy();bool restoredAccepted=restoredChild.Apply(Directions[d]).accepted;
                if(restoredAccepted!=actual || restoredChild.SearchStateKey()!=child.SearchStateKey() || restoredChild.Won!=child.Won || restoredChild.Cost!=child.Cost)
                    throw new Exception("Restored authority successor mismatch");
                if(actual!=accepted || result!=FromSession(child) || Won(result)!=child.Won || child.Failed || child.Cost!=authority.Cost+(actual?1:0))
                    throw new Exception("Compact authority mismatch: "+Map.name+" sample "+sample+" direction "+d);
                if(MovementEstimate(result)!=authorityBound.Estimate(child))throw new Exception("Compact successor heuristic mismatch");
                int before=RequiredSheepPushes(state),after=RequiredSheepPushes(result);
                if(accepted && (before==int.MaxValue && after!=int.MaxValue || before!=int.MaxValue && after!=int.MaxValue && before>after+1))
                {
                    throw new Exception("Compact sheep-bound consistency mismatch: "+Map.name+" sample="+sample);
                }
                if(child.Won && after!=0)throw new Exception("Nonzero compact goal bound");
                if(accepted){acceptedCount++;choices.Add((child,result));}edges++;
            }
            if(choices.Count==0 || sample%1000==999){authority=root.Copy();state=Initial;continue;}
            var chosen=choices[random.Next(choices.Count)];authority=chosen.Item1;state=chosen.Item2;
            if(Won(state)){authority=root.Copy();state=Initial;}
        }
        return new{map=Map.name,passed=true,states=samples,uniqueStates=seen.Count,edges=edges,acceptedEdges=acceptedCount};
    }
}

public sealed class CompactRanchSolver
{
    // 32-byte nodes; state bits occupy Info's lower 32, g/h its upper halves.
    struct Node { public ulong crates,sheep,info; public uint parentAction; }
    const int BlockBits=18,BlockSize=1<<BlockBits;
    readonly List<Node[]> blocks=new(); readonly List<int> heap=new();
    int[] table=new int[1<<20];int nodes,visited,expanded;
    readonly CompactRanchModel model;readonly int stateLimit,memoryLimit;
    readonly bool authorityTransitions;
    public long AuthorityEdgesChecked { get; private set; }
    readonly string sourceSha256;
    readonly long baseline;readonly Stopwatch clock=Stopwatch.StartNew();
    public CompactRanchSolver(CompactRanchModel model,int stateLimit,int memoryLimit,bool authorityTransitions=false)
    {if(stateLimit<1||memoryLimit<1)throw new ArgumentException("Positive search budgets required");this.model=model;this.stateLimit=stateLimit;this.memoryLimit=memoryLimit;this.authorityTransitions=authorityTransitions;sourceSha256=PuzzleShortestPathSolver.HashAsset(model.Map);baseline=GC.GetTotalMemory(true);}
    ref Node At(int i)=>ref blocks[i>>BlockBits][i&(BlockSize-1)];
    static CompactRanchState State(Node node)=>new(node.crates,node.sheep,(uint)node.info);
    static int Cost(Node node)=>(int)((node.info>>32)&65535);
    static int Priority(Node node)=>Cost(node)+(int)(node.info>>48);
    static ulong Hash(CompactRanchState state)
    {
        ulong h=state.Crates ^ BitOperations.RotateLeft(state.Sheep,23) ^ ((ulong)state.Info*0x9e3779b97f4a7c15UL);
        h^=h>>30;h*=0xbf58476d1ce4e5b9UL;h^=h>>27;h*=0x94d049bb133111ebUL;return h^(h>>31);
    }
    int Slot(CompactRanchState state,int[] index)
    {
        int slot=(int)(Hash(state)&(uint)(index.Length-1));
        while(index[slot]!=0 && State(At(index[slot]-1))!=state)slot=(slot+1)&(index.Length-1);
        return slot;
    }
    void Grow()
    {
        var larger=new int[checked(table.Length*2)];
        foreach(int entry in table)if(entry!=0)larger[Slot(State(At(entry-1)),larger)]=entry;
        table=larger;
    }
    bool Before(int a,int b)
    {var x=At(a);var y=At(b);return Priority(x)<Priority(y)||Priority(x)==Priority(y)&&Cost(x)>Cost(y);}
    void Push(int node)
    {
        int i=heap.Count;heap.Add(node);
        while(i>0){int parent=(i-1)/2;if(!Before(node,heap[parent]))break;heap[i]=heap[parent];i=parent;}heap[i]=node;
    }
    int Pop()
    {
        int result=heap[0],last=heap.Count-1,tail=heap[last];heap.RemoveAt(last);if(last==0)return result;
        int i=0;while(i*2+1<last){int child=i*2+1;if(child+1<last&&Before(heap[child+1],heap[child]))child++;if(!Before(heap[child],tail))break;heap[i]=heap[child];i=child;}heap[i]=tail;return result;
    }
    void Add(CompactRanchState state,int cost,int parent,int direction)
    {
        int slot=Slot(state,table),prior=table[slot]-1;if(prior>=0 && Cost(At(prior))<=cost)return;
        int h=model.Estimate(state);if(h==int.MaxValue)return;
        if(cost>65535||h>65535||nodes>=1<<29)throw new InvalidOperationException("Compact representation capacity reached");
        if(nodes%BlockSize==0)blocks.Add(new Node[BlockSize]);int id=nodes++;
        At(id)=new Node{crates=state.Crates,sheep=state.Sheep,info=state.Info|((ulong)cost<<32)|((ulong)h<<48),parentAction=parent<0?0:((uint)(parent+1)<<2)|(uint)direction};
        table[slot]=id+1;if(prior<0)visited++;Push(id);
        if(visited>(long)table.Length*7/10)Grow();
    }
    public PuzzleShortestPathSolver.Result Run()
    {
        Add(model.Initial,0,-1,0);double nextReport=5,nextMemory=1;
        string status="unreachable",detail="Search exhausted after impossible sheep-to-required-grass assignments were pruned.";int goal=-1;
        while(heap.Count>0)
        {
            if(visited>=stateLimit){status="limit";detail="State limit reached before the search was exhaustive.";break;}
            if(clock.Elapsed.TotalSeconds>=nextMemory)
            {
                if(GC.GetTotalMemory(false)-baseline>memoryLimit*1048576L && GC.GetTotalMemory(true)-baseline>memoryLimit*1048576L)
                {status="limit";detail="Additional managed memory budget reached before the search was exhaustive.";break;}
                nextMemory=clock.Elapsed.TotalSeconds+1;
            }
            int current=Pop();var node=At(current);var state=State(node);
            if(table[Slot(state,table)]!=current+1)continue;expanded++;
            if(model.Won(state)){goal=current;status="solved";detail="";break;}
            if(authorityTransitions)
            {
                var authority=model.RestoreAuthority(state,Cost(node));
                for(int d=0;d<4;d++)
                {
                    var source=authority.Copy();bool accepted=source.SearchStep(d==0?Vector2Int.up:d==1?Vector2Int.right:d==2?Vector2Int.down:Vector2Int.left);
                    bool predicted=model.TryStep(state,d,out var child);var actual=model.FromSession(source);AuthorityEdgesChecked++;
                    if(predicted!=accepted || actual!=child || source.Failed || source.Won!=model.Won(child))throw new Exception("Authority expansion differs from compact rule at node "+current);
                    // The authority generates every successor used by this search.
                    if(accepted)Add(actual,source.Cost,current,d);
                }
            }
            else for(int d=0;d<4;d++)if(model.TryStep(state,d,out var child))Add(child,Cost(node)+1,current,d);
            if(clock.Elapsed.TotalSeconds>=nextReport)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{map=model.Map.name,visited,expanded,frontier=heap.Count,lowerBound=heap.Count==0?-1:Priority(At(heap[0])),seconds=clock.Elapsed.TotalSeconds,managedMiB=GC.GetTotalMemory(false)/1048576}));nextReport=clock.Elapsed.TotalSeconds+5;
            }
        }
        var route=new List<char>();for(int at=goal;at>0;){var node=At(at);route.Add("NESW"[(int)(node.parentAction&3)]);at=(int)(node.parentAction>>2)-1;}route.Reverse();
        if(PuzzleShortestPathSolver.HashAsset(model.Map)!=sourceSha256){status="stale";detail="Source map changed during search.";route.Clear();goal=-1;}
        return new PuzzleShortestPathSolver.Result{map=model.Map,sourceSha256=sourceSha256,status=status,detail=detail,route=new string(route.ToArray()),distance=goal<0?-1:Cost(At(goal)),visited=visited,expanded=expanded,elapsedSeconds=clock.Elapsed.TotalSeconds,algorithm=authorityTransitions?"Exact A* (authority transitions; compact canonical keys; additive sheep bound)":"Exact compact A* (restricted ranch rules; authority-checked)"};
    }
}
