#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.ShutterOneTileV01.Editor
{
    public static class ShutterOneTileBuilderV01
    {
        [MenuItem("Tools/ANIMOL/One tile shutter/Build art set")]
        public static void BuildFromMenu()
        {
            string path = EditorUtility.OpenFilePanel("Select shutter_one_tile_manifest.json", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            BuildFromRoot(Path.GetDirectoryName(path));
        }
        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-oneTileShutterRoot") { BuildFromRoot(args[i + 1]); return; }
            throw new ArgumentException("Pass -oneTileShutterRoot Assets/<path>/ANIMOL_ECHO_THEATER_SHUTTER_1TILE_V01");
        }
        public static void BuildInstalledPackage() =>
            BuildFromRoot(Path.Combine(Application.dataPath, "ANIMOL_ECHO_THEATER_SHUTTER_1TILE_V01"));
        public static void ConnectInstalledPackageToTheaterTheme()
        {
            var art = AssetDatabase.LoadAssetAtPath<ShutterOneTileArtV01>(
                "Assets/ANIMOL_ECHO_THEATER_SHUTTER_1TILE_V01/_Generated/ShutterOneTileArt.asset");
            var theme = AssetDatabase.LoadAssetAtPath<ANIMOL.Puzzle.PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset");
            if (!art || !theme) throw new InvalidOperationException("Generated shutter art or theater theme is missing.");
            theme.oneTileShutterArt = art;
            EditorUtility.SetDirty(theme); AssetDatabase.SaveAssets();
        }
        public static void BuildFromRoot(string selectedRoot)
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
            string candidate = Path.IsPathRooted(selectedRoot) ? selectedRoot : Path.Combine(project, selectedRoot);
            string root = Path.GetFullPath(candidate).Replace('\\', '/');
            if (!root.StartsWith(project + "/Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("Install the package under Assets.");
            string assetRoot = root.Substring(project.Length + 1);
            if (!File.Exists(Path.Combine(root, "shutter_one_tile_manifest.json"))) throw new FileNotFoundException("Missing one tile shutter manifest.");
            string generated = assetRoot + "/_Generated";
            if (!AssetDatabase.IsValidFolder(generated)) AssetDatabase.CreateFolder(assetRoot, "_Generated");
            string output = generated + "/ShutterOneTileArt.asset";
            ShutterOneTileArtV01 art = AssetDatabase.LoadAssetAtPath<ShutterOneTileArtV01>(output);
            if (art == null) { art = ScriptableObject.CreateInstance<ShutterOneTileArtV01>(); AssetDatabase.CreateAsset(art, output); }
            foreach (string direction in new[] { "horizontal", "vertical" })
            {
                Sprite[] sprites = new Sprite[4];
                for (int i = 0; i < 6; i++)
                {
                    string fileName = i < 4 ? "frame_" + i.ToString("00") : i == 4 ? "closed" : "open";
                    string p = assetRoot + "/Sprites/" + direction + "/" + fileName + ".png";
                    AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
                    TextureImporter importer = AssetImporter.GetAtPath(p) as TextureImporter;
                    if (importer == null) throw new InvalidOperationException("Missing sprite: " + p);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 256; importer.spritePivot = new Vector2(.5f, .5f);
                    TextureImporterSettings settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Center; settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings); importer.filterMode = FilterMode.Bilinear;
                    importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
                    importer.npotScale = TextureImporterNPOTScale.None; importer.textureCompression = TextureImporterCompression.Uncompressed;
                    foreach (string platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
                        importer.ClearPlatformTextureSettings(platform);
                    importer.maxTextureSize = 256; importer.SaveAndReimport();
                    if (i < 4) sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                }
                if (direction == "horizontal") art.horizontal = sprites; else art.vertical = sprites;
            }
            string error; if (!art.Validate(out error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
            Debug.Log("One tile shutter art generated: " + output + ". Connect only shutter presentation to the existing host.");
        }
    }
}
#endif
