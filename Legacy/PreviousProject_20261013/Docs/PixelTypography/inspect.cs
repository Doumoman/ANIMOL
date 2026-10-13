using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;
public static class FontInspection
{
    public static void ImportResources() => AssetDatabase.ImportPackage("Library/PackageCache/com.unity.ugui@bb329a87fcdc/Package Resources/TMP Essential Resources.unitypackage", false);
    public static string FinalFontSettings()
    {
        var rows=new List<string>();
        foreach(var guid in AssetDatabase.FindAssets("t:TMP_FontAsset",new[]{"Assets/ANIMOL/Typography"})) {
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            rows.Add(font.name+" | "+font.atlasPopulationMode+" | "+font.atlasRenderMode+" | point="+font.faceInfo.pointSize+" | "+font.atlasWidth+"x"+font.atlasHeight+" | pages="+font.atlasTextureCount+" | chars="+font.characterTable.Count+" | shader="+font.material.shader.name);
            foreach(var atlas in font.atlasTextures) rows.Add("  "+atlas.name+" | "+atlas.filterMode+" | mips="+atlas.mipmapCount+" | alpha levels="+string.Join(",",atlas.GetPixels32().Select(p=>p.a).Distinct().OrderBy(a=>a)));
        }
        File.WriteAllLines("Docs/PixelTypography/final-font-settings.txt",rows);return string.Join("\n",rows);
    }
    public static string Main()
    {
        var rows = new List<string>();
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/ANIMOL/Prefabs/UI"})) {
            var path=AssetDatabase.GUIDToAssetPath(guid); var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            rows.Add(path+" | Text="+go.GetComponentsInChildren<Text>(true).Length+" TMP="+go.GetComponentsInChildren<TMP_Text>(true).Length);
        }
        var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/ANIMOL/Fonts/pixelroborobo.otf");
        FontEngine.InitializeFontEngine(); FontEngine.LoadFontFace(font,16);
        rows.Add(JsonUtility.ToJson(FontEngine.GetFaceInfo(),true));
        rows.Add("TMP shaders: "+(Shader.Find("TextMeshPro/Bitmap") != null)+" / "+(Shader.Find("TextMeshPro/Mobile/Bitmap") != null));
        rows.Add("Existing TMP assets: "+AssetDatabase.FindAssets("t:TMP_FontAsset").Length);
        rows.Add("TMP settings: "+(TMP_Settings.instance != null));
        File.WriteAllLines("Docs/PixelTypography/inventory.txt",rows);
        return string.Join("\n",rows);
    }
}
