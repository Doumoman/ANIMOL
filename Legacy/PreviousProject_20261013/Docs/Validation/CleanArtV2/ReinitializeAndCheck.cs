// Run using eval_file after tests complete. Results are persisted even if the RPC times out.
    var registry=ANIMOL.Gameplay.FreeShapeArtRegistry.Load(Animol.TerrainStructure.FreeShapeLayer.Contract,2);
    var before=registry.styles.SelectMany(s=>s.cells.Concat(new[]{s.motif})).Select(s=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(s).ToString()).ToArray();
    var watch=System.Diagnostics.Stopwatch.StartNew();
    ANIMOL.Editor.FreeShapeArtV2Builder.Initialize();
    var after=registry.styles.SelectMany(s=>s.cells.Concat(new[]{s.motif})).Select(s=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(s).ToString()).ToArray();
    var memory=new System.Collections.Generic.List<object>();
    foreach(int version in new[]{1,2})
    {
        var a=ANIMOL.Gameplay.FreeShapeArtRegistry.Load(Animol.TerrainStructure.FreeShapeLayer.Contract,version);
        var textures=a.styles.SelectMany(s=>s.cells.Concat(new[]{s.motif})).Select(s=>s.texture).Distinct().ToArray();
        memory.Add(new{version=version,textures=textures.Length,bytes=textures.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t))});
    }
    var output=new{stable=before.SequenceEqual(after),references=before.Length,milliseconds=watch.ElapsedMilliseconds,memory=memory,before=before,after=after};
    System.IO.File.WriteAllText("Docs/Validation/CleanArtV2/Reinitialize.json",Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));
return output;
