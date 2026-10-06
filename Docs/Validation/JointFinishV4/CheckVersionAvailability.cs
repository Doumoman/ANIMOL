var versions=new System.Collections.Generic.List<object>();
foreach(int v in new[]{1,2,3,4,5}){
 try{var art=ANIMOL.Gameplay.FreeShapeArtRegistry.Load(Animol.TerrainStructure.FreeShapeLayer.Contract,v);if(v==3||v==5)throw new System.Exception("Unexpected installed/unsupported version");versions.Add(new{version=v,installed=true,path=UnityEditor.AssetDatabase.GetAssetPath(art),message=""});}
 catch(System.InvalidOperationException ex){if(v!=3&&v!=5)throw;versions.Add(new{version=v,installed=false,path="",message=ex.Message});}
}
System.IO.File.WriteAllText("Docs/Validation/JointFinishV4/VersionAvailability.json",Newtonsoft.Json.JsonConvert.SerializeObject(versions,Newtonsoft.Json.Formatting.Indented));
return versions;
