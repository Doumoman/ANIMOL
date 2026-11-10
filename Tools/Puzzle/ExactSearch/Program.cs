using System.Diagnostics;
using System.Text.Json;
using System.Security.Cryptography;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using UnityEngine;

public sealed class Export
{
    public string id,assetPath,sourceSha256,scenario;
    public int width,height,targetBubbles;
    public bool validated;
    public Vector2Int start,exit;
    public List<TerrainCell> terrain;
    public List<Vector2Int> decorations,bubbles;
}
class Program
{
    static readonly JsonSerializerOptions Json = new() { IncludeFields=true,WriteIndented=true,
        Encoder=System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All) };
    static Dictionary<string,string> SourceHashes()
    {
        var assembly=typeof(Program).Assembly;
        return assembly.GetManifestResourceNames().OrderBy(n=>n).ToDictionary(n=>n,
            n=>Convert.ToHexStringLower(SHA256.HashData(assembly.GetManifestResourceStream(n))));
    }
    static PuzzleMap Load(Export export)
    {
        var map=new PuzzleMap { name=export.id,assetPath=export.assetPath,width=export.width,height=export.height,
            start=export.start,exit=export.exit,targetBubbles=export.targetBubbles,terrain=export.terrain,
            decorations=new(export.decorations),bubbles=export.bubbles,validated=export.validated,
            scenario=JsonSerializer.Deserialize<PuzzleScenario>(export.scenario,Json) };
        if(PuzzleShortestPathSolver.HashAsset(map)!=export.sourceSha256)throw new Exception("Source hash changed");
        bool ranch=map.scenario.themeId=="starway" && !map.scenario.echoTheaterV3Rules && map.scenario.railPaths.Count==0 &&
            !map.scenario.devices.Any(d=>d.role!=PrototypeRole.Chick && d.role!=PrototypeRole.MetalCrate &&
                d.role!=PrototypeRole.WoodCrate && d.role!=PrototypeRole.StarSocket &&
                d.role!=PrototypeRole.Wind && d.role!=PrototypeRole.Bounce && d.role!=PrototypeRole.Conveyor &&
                d.role!=PrototypeRole.OneWayGate && d.role!=PrototypeRole.Brake);
        bool theater=EchoExactSolver.Supports(map);
        if(!ranch && !theater)
            throw new NotSupportedException("Unsupported board in portable runner");
        return map;
    }
    static Vector2Int Heading(char c) => c=='N'?Vector2Int.up:c=='E'?Vector2Int.right:c=='S'?Vector2Int.down:Vector2Int.left;
    static bool Move(PuzzleDeviceSession state,char action) => action=='.'?state.AdvanceWind().accepted:state.Apply(Heading(action)).accepted;
    static PuzzleDeviceSession Root(PuzzleMap map)
    {
        var root=new PuzzleDeviceSession(map);
        if(map.scenario.devices.Any(d=>d.role==PrototypeRole.Wind))root.AdvanceWind();
        return root;
    }
    static void Main(string[] args)
    {
        var input=JsonSerializer.Deserialize<List<Export>>(File.ReadAllText(args[0]),Json);
        if(args[1]=="verify")
        {
            int states=0,edges=0,canonicalKeySuccessorEdges=0;
            foreach(var group in JsonDocument.Parse(File.ReadAllText(args[2])).RootElement.EnumerateArray())
            {
                var map=Load(input.Single(e=>e.id==group.GetProperty("id").GetString()));
                var root=Root(map);var state=root.Copy();
                foreach(var row in group.GetProperty("rows").EnumerateArray())
                {
                    if(state.SearchStateKey()!=row.GetProperty("key").GetString() || state.Cost!=row.GetProperty("cost").GetInt32())throw new Exception("Before-state mismatch: "+map.name);
                    states++;
                    if(map.scenario.themeId=="memory_theater")
                        canonicalKeySuccessorEdges+=EchoExactSolver.VerifyCanonicalKeys(state);
                    foreach(var expected in row.GetProperty("successors").EnumerateArray())
                    {
                        var child=state.Copy();bool accepted=Move(child,expected.GetProperty("action").GetString()[0]);
                        if(accepted!=expected.GetProperty("accepted").GetBoolean() || child.SearchStateKey()!=expected.GetProperty("key").GetString() ||
                            child.Cost!=expected.GetProperty("cost").GetInt32() || child.Won!=expected.GetProperty("won").GetBoolean() || child.Failed!=expected.GetProperty("failed").GetBoolean())
                            throw new Exception("Successor mismatch: "+map.name+" state "+states);
                        edges++;
                    }
                    var next=state.Copy();Move(next,row.GetProperty("nextAction").GetString()[0]);
                    state=row.GetProperty("resetAfter").GetBoolean()?root.Copy():next;
                }
            }
            var compactChecks=new List<object>();
            foreach(var export in input)
            {
                var model=CompactRanchModel.TryCreate(Load(export));
                if(model!=null){compactChecks.Add(model.VerifyAuthority(25000));Console.WriteLine("Compact authority parity passed: "+export.id);}
            }
            var selfTests=input.Any(e=>Load(e).scenario.themeId=="starway") ? CompactRanchTests.Run() : null;
            var statueDeadlockTests=input.Any(e=>Load(e).scenario.themeId=="memory_theater") ? EchoStatueDeadlock.VerifySmallBoards() : null;
            var report=new {passed=true,states=states,edges=edges,canonicalKeySuccessorEdges=canonicalKeySuccessorEdges,
                engine="Source-linked PuzzleDeviceSession",
                compactParityPassed=true,compactChecks=compactChecks,compactSelfTests=selfTests,statueDeadlockTests=statueDeadlockTests,
                sourceHashes=SourceHashes(),inputSha256=Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(args[0]))),fixtureSha256=Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(args[2])))};
            File.WriteAllText(args[3],JsonSerializer.Serialize(report,Json)+"\n");Console.WriteLine(JsonSerializer.Serialize(report,Json));return;
        }
        if(args.Length<5)throw new ArgumentException("Search requires input, map ID, output, state cap and a verification report.");
        var verification=JsonDocument.Parse(File.ReadAllText(args[4])).RootElement;
        var inputHash=Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(args[0])));
        if(!verification.GetProperty("passed").GetBoolean() || verification.GetProperty("inputSha256").GetString()!=inputHash ||
            verification.GetProperty("sourceHashes").EnumerateObject().Count()!=SourceHashes().Count ||
            SourceHashes().Any(p=>verification.GetProperty("sourceHashes").GetProperty(p.Key).GetString()!=p.Value))
            throw new InvalidDataException("Verify this input with this build before searching.");
        var searchMap=Load(input.Single(e=>e.id==args[1]));
        PuzzleShortestPathSolver.Result result=null;
        int stateLimit=int.Parse(args[3]), memoryLimit=args.Length>5?int.Parse(args[5]):3072;
        bool authorityTransitions=args.Length>6 && args[6]=="authority";
        bool compact=authorityTransitions || args.Length>6 && args[6]=="compact";
        CompactRanchSolver compactSolver=null;
        if(compact)
        {
            if(!verification.GetProperty("compactParityPassed").GetBoolean())throw new Exception("Compact authority parity is required");
            var model=CompactRanchModel.TryCreate(searchMap) ?? throw new NotSupportedException("Board is outside compact ranch rules");
            compactSolver=new CompactRanchSolver(model,stateLimit,memoryLimit,authorityTransitions);
            result=compactSolver.Run();
        }
        else if(searchMap.scenario.themeId=="memory_theater")
            result=EchoExactSolver.Run(searchMap,stateLimit,memoryLimit);
        else PuzzleShortestPathSolver.Start(searchMap,r=>result=r,stateLimit,true,memoryLimit);
        var timer=Stopwatch.StartNew();double nextReport=5;
        while(result==null)
        {
            UnityEditor.EditorApplication.Pump();
            if(timer.Elapsed.TotalSeconds>=nextReport)
            {
                var search=PuzzleShortestPathSolver.Active;
                Console.WriteLine(JsonSerializer.Serialize(new {map=searchMap.name,visited=search?.Visited,expanded=search?.Expanded,
                    frontier=search?.FrontierCount,seconds=timer.Elapsed.TotalSeconds,managedMiB=GC.GetTotalMemory(false)/1048576}));
                nextReport=timer.Elapsed.TotalSeconds+5;
            }
        }
        var record=new {map=searchMap.name,status=result.status,distance=result.distance,route=PuzzleRouteNotation.ToArrows(result.route),
            sourceSha256=result.sourceSha256,visited=result.visited,expanded=result.expanded,
            elapsedSeconds=result.elapsedSeconds,detail=result.detail,algorithm=result.algorithm,
            runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            stateLimit=stateLimit,additionalManagedMemoryLimitMiB=memoryLimit,
            compactMode=compact,
            authorityTransitions=authorityTransitions,authorityEdgesChecked=compactSolver?.AuthorityEdgesChecked ?? 0,
            permanentStatueDeadlocksPruned=searchMap.scenario.themeId=="memory_theater"?EchoExactSolver.LastDeadlockPruned:0,
            sourceHashes=SourceHashes(),inputSha256=inputHash,
            verificationSha256=Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(args[4])))};
        File.WriteAllText(args[2],JsonSerializer.Serialize(record,Json)+"\n");
        Console.WriteLine(JsonSerializer.Serialize(record,Json));
    }
}
