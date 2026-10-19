#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Art.DevicePatch01.Editor
{
    public static class DevicePatchP01Batch
    {
        // Unity -executeMethod requires a parameterless static entry point.
        public static void BuildWindMaterialFromCommandLine()
        {
            const string flag = "-p01WindMaterial";
            string[] args = Environment.GetCommandLineArgs();
            string assetPath = null;
            for (int i=0; i<args.Length; i++)
            {
                if (args[i] != flag) continue;
                if (assetPath != null || i+1 >= args.Length || args[i+1].StartsWith("-",StringComparison.Ordinal))
                    throw new ArgumentException("Missing or duplicate " + flag);
                assetPath=args[++i];
            }
            if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith("Assets/",StringComparison.Ordinal)
                || !assetPath.EndsWith(".mat",StringComparison.OrdinalIgnoreCase) || assetPath.Contains(".."))
                throw new ArgumentException("Required -p01WindMaterial Assets/.../VividWindRoute.mat");
            int slash=assetPath.LastIndexOf('/');
            string[] parts=assetPath.Substring(0,slash).Split('/');
            string folder="Assets";
            for(int i=1;i<parts.Length;i++)
            {
                string child=folder+"/"+parts[i];
                if(!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(folder,parts[i]);
                folder=child;
            }
            Material material=AnimolWindMaterialFactory.CreateOrUpdateAt(assetPath);
            if(material==null || material.shader==null || !material.shader.isSupported || ShaderUtil.ShaderHasError(material.shader))
                throw new InvalidOperationException("P01 wind material shader failed import/compile/support. No legacy fallback.");
            Debug.Log("[ANIMOL P01] Wind material built at "+assetPath+". Actual force adapter and Play Mode remain separate required checks.",material);
        }
    }
}
#endif
