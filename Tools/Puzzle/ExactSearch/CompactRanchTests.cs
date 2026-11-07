using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using UnityEngine;

public static class CompactRanchTests
{
    static readonly Vector2Int[] Directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
    static int AuthorityBfs(PuzzleMap map)
    {
        var root=new PuzzleDeviceSession(map);var queue=new Queue<PuzzleDeviceSession>();
        var seen=new HashSet<string>{root.SearchStateKey()};queue.Enqueue(root);
        while(queue.Count>0)
        {
            var current=queue.Dequeue();if(current.Won)return current.Cost;
            foreach(var dir in Directions)
            {
                var child=current.Copy();if(!child.SearchStep(dir)||child.Failed||!seen.Add(child.SearchStateKey()))continue;
                queue.Enqueue(child);
            }
            if(seen.Count>200000)throw new Exception("Small-board authority BFS exceeded its test budget");
        }
        return -1;
    }
    public static object Run()
    {
        var checks=new List<object>();
        for(int test=0;test<2;test++)
        {
            string path="Temp/compact_ranch_test_"+test+".json";
            var rules=new PuzzleScenario{themeId="starway"};
            var map=new PuzzleMap{name="CompactTest"+test,validated=true,width=4,height=4,
                assetPath=path,scenario=rules,start=Vector2Int.zero,exit=new Vector2Int(3,0),targetBubbles=test==0?1:2};
            for(int x=0;x<4;x++)for(int y=0;y<4;y++)map.terrain.Add(new TerrainCell{position=new(x,y)});
            rules.nests.Add(new(3,3));map.bubbles.Add(new(3,3));
            rules.devices.Add(new PrototypeDevice{id="chick",role=PrototypeRole.Chick,position=new(1,1),number=1});
            rules.devices.Add(new PrototypeDevice{id="crate",role=PrototypeRole.MetalCrate,position=test==0?new(2,0):new(3,1)});
            rules.ranchV13Art.Add(new RanchV13ArtPlacement{family="sheep",instanceId="sheep",position=test==0?new(0,2):new(1,0)});
            rules.ranchV13Art.Add(new RanchV13ArtPlacement{family="tall_grass",instanceId="grass",position=test==0?new(2,3):new(2,0)});
            if(test!=0)map.bubbles.Add(new(2,0));
            File.WriteAllText(path,"Synthetic compact-rule test "+test);
            try
            {
                var model=CompactRanchModel.TryCreate(map) ?? throw new Exception("Small test model rejected");
                var parity=model.VerifyAuthority(1000);
                int expected=AuthorityBfs(map);var result=new CompactRanchSolver(model,200000,256).Run();
                if(expected<0||!result.Solved||result.distance!=expected)throw new Exception("Compact A* differs from exhaustive authority BFS");
                var replay=new PuzzleDeviceSession(map);
                foreach(char action in result.route)if(!replay.Apply(Directions["NESW".IndexOf(action)]).accepted)throw new Exception("Small route rejected");
                if(!replay.Won||replay.Cost!=expected)throw new Exception("Small clear/cost mismatch");
                var authoritySearch=new CompactRanchSolver(model,200000,256,true).Run();
                if(!authoritySearch.Solved||authoritySearch.distance!=expected)throw new Exception("Authority-backed compact search differs from BFS");
                var capped=new CompactRanchSolver(model,1,256).Run();
                if(capped.status!="limit"||capped.distance!=-1||capped.route!="")throw new Exception("Compact cap was treated as a completed search");
                var staleSearch=new CompactRanchSolver(model,200000,256);
                File.AppendAllText(path," changed");var stale=staleSearch.Run();
                if(stale.status!="stale"||stale.distance!=-1||stale.route!="")throw new Exception("Changed source was accepted");
                File.WriteAllText(path,"Synthetic compact-rule test "+test);
                if(test==1)
                {
                    var noSheep=new PuzzleDeviceSession(map);
                    ((Dictionary<string,Vector2Int>)typeof(PuzzleDeviceSession).GetField("ranchSheep",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(noSheep)).Clear();
                    if(model.Estimate(model.FromSession(noSheep))!=int.MaxValue)throw new Exception("Missing mandatory sheep was not detected");
                    map.targetBubbles=1;var optional=CompactRanchModel.TryCreate(map);
                    if(optional.Estimate(optional.FromSession(noSheep))==int.MaxValue)throw new Exception("Optional grass bubble was treated as mandatory");
                    map.targetBubbles=2;
                }
                rules.devices.Add(new PrototypeDevice{id="unsupported",role=PrototypeRole.Wind});
                if(CompactRanchModel.TryCreate(map)!=null)throw new Exception("Unsupported wind accepted");
                rules.devices.RemoveAt(rules.devices.Count-1);
                rules.goals.Add(new PrototypeGoal{kind=PrototypeGoalKind.Visited,cell=new(1,2)});
                if(CompactRanchModel.TryCreate(map)!=null)throw new Exception("Unsupported goal accepted");
                checks.Add(new{test=test,passed=true,expectedDistance=expected,actualDistance=result.distance,authoritySearchDistance=authoritySearch.distance,
                    publicReplayWon=replay.Won,limitRemainedIncomplete=true,sourceChangeRejected=true,parity=parity});
            }
            finally{File.Delete(path);}
        }
        return checks;
    }
}
