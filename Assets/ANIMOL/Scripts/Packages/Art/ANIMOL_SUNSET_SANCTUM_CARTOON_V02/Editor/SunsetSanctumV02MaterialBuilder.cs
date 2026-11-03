#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
namespace ANIMOL.Art.SunsetSanctumV02
{
    public static class SunsetSanctumV02MaterialBuilder
    {
        public static void BuildFromCommandLine()
        {
            const string flag="-sanctumV02BeamMaterial";string[] args=Environment.GetCommandLineArgs();string path=null;
            for(int i=0;i<args.Length;i++) { if(args[i]!=flag)continue;
                if(path!=null || i+1>=args.Length || args[i+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("Missing/duplicate "+flag);
                path=args[++i]; }
            CreateOrUpdate(path);
        }
        public static Material CreateOrUpdate(string path)
        {
            if(string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/",StringComparison.Ordinal)
                || !path.EndsWith(".mat",StringComparison.OrdinalIgnoreCase) || path.IndexOf("..",StringComparison.Ordinal)>=0)
                throw new ArgumentException("Use -sanctumV02BeamMaterial Assets/.../Materials/LightBeam.mat");
            string folder="Assets";string[] parts=path.Substring(0,path.LastIndexOf('/')).Split('/');
            for(int i=1;i<parts.Length;i++) { string child=folder+"/"+parts[i];if(!AssetDatabase.IsValidFolder(child))AssetDatabase.CreateFolder(folder,parts[i]);folder=child; }
            Shader shader=Shader.Find("ANIMOL/SunsetSanctumV02/LightBeam");
            if(shader==null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Latest beam shader failed import/compile/support.");
            Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(shader) {name="Sunset Sanctum Flat Light Beam"};AssetDatabase.CreateAsset(material,path); }
            else { Undo.RecordObject(material,"Update Sanctum beam");material.shader=shader; }
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);return material;
        }
    }
}
#endif
