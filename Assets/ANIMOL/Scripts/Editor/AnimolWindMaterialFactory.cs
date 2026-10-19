#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Art.DevicePatch01.Editor
{
    public static class AnimolWindMaterialFactory
    {
        public static Material CreateOrUpdateAt(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal)
                || !assetPath.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Use an existing Assets/ directory and .mat asset path.", "assetPath");
            Shader shader = Shader.Find("ANIMOL/DevicePatch01/VividWindRoute");
            if (shader == null) throw new InvalidOperationException("Patch wind shader is missing or failed import.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader) { name = "ANIMOL Vivid Wind Range" };
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else { Undo.RecordObject(material, "Update ANIMOL Wind Shader"); material.shader = shader; }
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
#endif
