using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01.Editor
{
    public static class DiceFaceSetBuilderV02
    {
        [Serializable] sealed class Manifest { public string packageId; public int installationRevision; public Face[] faces; public Part[] platforms; }
        [Serializable] sealed class Face { public int value, width, height, ppu; public string file, sha256; }
        [Serializable] sealed class Part { public string state, file, sha256; public int width, height, ppu; }
        static string Absolute(string asset) { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, asset); }
        static string Hash(string path)
        { using (var h = SHA256.Create()) using (var f = File.OpenRead(path)) return BitConverter.ToString(h.ComputeHash(f)).Replace("-", "").ToLowerInvariant(); }
        [MenuItem("Tools/ANIMOL/Dice face hints R3/Build face and platform sets")]
        public static void MenuBuild()
        {
            string path = EditorUtility.OpenFilePanel("Select R3 faces_manifest.json", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            string project = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/'); path = path.Replace('\\', '/');
            if (!path.StartsWith(project + "/Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("Install R3 under Assets first.");
            Build(path.Substring(project.Length + 1));
        }
        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-diceFaceManifest") { Build(args[i + 1].Replace('\\', '/')); return; }
            throw new InvalidOperationException("Supply -diceFaceManifest Assets/.../faces_manifest.json");
        }
        public static void Build(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..") || !path.EndsWith("/ANIMOL_ECHO_THEATER_DICE_HINT_V01/faces_manifest.json", StringComparison.Ordinal))
                throw new InvalidOperationException("Select the R3 face manifest under this package path.");
            if (Hash(Absolute(path)) != DiceFacePinV02.Sha256) throw new InvalidOperationException("Wrong face manifest SHA256.");
            Manifest m = JsonUtility.FromJson<Manifest>(File.ReadAllText(Absolute(path)));
            if (m == null || m.packageId != "ANIMOL_ECHO_THEATER_DICE_HINT_V01" || m.installationRevision != 3 || m.faces == null || m.faces.Length != 6 || m.platforms == null || m.platforms.Length != 4)
                throw new InvalidOperationException("R3 requires six faces and four platform states.");
            string root = path.Substring(0, path.LastIndexOf('/'));
            var seen = new bool[6];
            foreach (var face in m.faces)
            {
                if (face == null || face.value < 1 || face.value > 6 || seen[face.value - 1]
                    || face.file != "Sprites/Faces/face_" + face.value + ".png" || face.width != 256 || face.height != 256 || face.ppu != 256
                    || Hash(Absolute(root + "/" + face.file)) != face.sha256) throw new InvalidOperationException("Invalid or missing face image.");
                seen[face.value - 1] = true;
            }
            string folder = root + "/_GeneratedDiceHintsV02", target = folder + "/DiceFaceSet.asset";
            var old = AssetDatabase.LoadMainAssetAtPath(target);
            if (old != null && (!(old is DiceFaceSetV02) || EditorUtility.IsDirty(old))) throw new InvalidOperationException("Foreign or dirty face-set output.");
            string platformTarget = folder + "/DiceGoalPlatformArt.asset";
            var oldPlatform = AssetDatabase.LoadMainAssetAtPath(platformTarget);
            if (oldPlatform != null && (!(oldPlatform is DiceGoalPlatformArtV03) || EditorUtility.IsDirty(oldPlatform))) throw new InvalidOperationException("Foreign or dirty platform output.");
            string[] states = { "ready", "half_pressed", "nearly_flush", "walkable" };
            for (int i = 0; i < 4; i++)
            {
                var part = m.platforms[i];
                if (part == null || part.state != states[i] || part.file != "Sprites/GoalPlatform/" + states[i] + ".png"
                    || part.width != 320 || part.height != 320 || part.ppu != 256 || Hash(Absolute(root + "/" + part.file)) != part.sha256)
                    throw new InvalidOperationException("Invalid platform source.");
            }
            var sprites = new Sprite[6];
            foreach (var face in m.faces)
            {
                string asset = root + "/" + face.file; AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(asset) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Texture importer missing.");
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 256; importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 256;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = 9; settings.spritePivot = new Vector2(.5f, .5f); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.spritePixelsPerUnit = 256; importer.filterMode = FilterMode.Bilinear;
                foreach (string platformName in new[] { "Android", "iPhone", "Standalone", "WebGL" }) importer.ClearPlatformTextureSettings(platformName);
                importer.SaveAndReimport(); sprites[face.value - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(asset);
                if (sprites[face.value - 1] == null || sprites[face.value - 1].rect.width != 256 || sprites[face.value - 1].rect.height != 256) throw new InvalidOperationException("Face import failed.");
            }
            Directory.CreateDirectory(Absolute(folder)); AssetDatabase.Refresh();
            var result = old as DiceFaceSetV02; bool fresh = result == null;
            if (fresh) result = ScriptableObject.CreateInstance<DiceFaceSetV02>();
            result.faces = sprites; result.sourceSha256 = DiceFacePinV02.Sha256;
            if (fresh) AssetDatabase.CreateAsset(result, target); else EditorUtility.SetDirty(result);
            var plates = new Sprite[4];
            for (int i = 0; i < 4; i++) plates[i] = ImportPlatform(root + "/" + m.platforms[i].file);
            var platform = oldPlatform as DiceGoalPlatformArtV03; bool freshPlatform = platform == null;
            if (freshPlatform) platform = ScriptableObject.CreateInstance<DiceGoalPlatformArtV03>();
            platform.ready = plates[0]; platform.halfPressed = plates[1]; platform.nearlyFlush = plates[2]; platform.walkable = plates[3]; platform.sourceSha256 = DiceFacePinV02.Sha256;
            if (freshPlatform) AssetDatabase.CreateAsset(platform, platformTarget); else EditorUtility.SetDirty(platform);
            AssetDatabase.SaveAssets(); Debug.Log("R3 face and goal-to-platform art sets are ready. Gameplay conversion remains owned by the host.");
        }
        static Sprite ImportPlatform(string asset)
        {
            AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(asset) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Platform texture importer missing.");
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 256;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 512;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteAlignment = 9; settings.spritePivot = new Vector2(.5f, .5f); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = 256; importer.filterMode = FilterMode.Bilinear;
            foreach (string platform in new[] { "Android", "iPhone", "Standalone", "WebGL" }) importer.ClearPlatformTextureSettings(platform);
            importer.SaveAndReimport(); var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(asset);
            if (sprite == null || sprite.rect.width != 320 || sprite.rect.height != 320) throw new InvalidOperationException("Platform import failed.");
            return sprite;
        }
    }
}
