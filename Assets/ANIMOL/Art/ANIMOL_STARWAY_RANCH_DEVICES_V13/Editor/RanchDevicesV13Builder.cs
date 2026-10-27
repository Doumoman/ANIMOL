using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ANIMOL.Art.RanchDevicesV13;

namespace ANIMOL.Editor.RanchDevicesV13
{
    [Serializable] public sealed class RawSpriteV13
    {
        public string key, file, sha256, role;
        public int width, height, ppu, footprintX, footprintY;
        public float pivotX, pivotY;
    }
    [Serializable] public sealed class RawClipV13
    {
        public string key;
        public string[] frames;
        public float fps;
        public bool loop;
    }
    [Serializable] public sealed class RawCatalogV13
    {
        public string packageId;
        public int spriteCount, clipCount;
        public RawSpriteV13[] sprites;
        public RawClipV13[] clips;
    }
    public static class RanchDevicesV13Builder
    {
        const string Generated = "/_GeneratedV13";
        static string Absolute(string assetPath) { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath); }
        internal static string Hash(string path)
        {
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        [MenuItem("Tools/ANIMOL/Ranch Devices V13/Build catalog, clips and inspection prefab")]
        public static void MenuBuild()
        {
            var selected = EditorUtility.OpenFilePanel("Choose V13 animation_catalog.json", Application.dataPath, "json");
            if (string.IsNullOrEmpty(selected)) return;
            string project = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
            selected = selected.Replace('\\', '/');
            if (!selected.StartsWith(project + "/Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("Catalog must be installed under this project's Assets.");
            Build(selected.Substring(project.Length + 1));
        }
        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-ranchDeviceCatalog") { Build(args[i + 1].Replace('\\', '/')); return; }
            throw new InvalidOperationException("Supply -ranchDeviceCatalog Assets/.../ANIMOL_STARWAY_RANCH_DEVICES_V13/animation_catalog.json");
        }
        public static void Build(string catalogAssetPath)
        {
            if (!catalogAssetPath.StartsWith("Assets/", StringComparison.Ordinal) || !catalogAssetPath.EndsWith("/" + RanchDevicesV13Pin.Package + "/animation_catalog.json", StringComparison.Ordinal) || catalogAssetPath.Contains(".."))
                throw new InvalidOperationException("Use the exact V13 package root under Assets.");
            string root = catalogAssetPath.Substring(0, catalogAssetPath.LastIndexOf('/'));
            if (Hash(Absolute(catalogAssetPath)) != RanchDevicesV13Pin.Sha256) throw new InvalidOperationException("Catalog SHA-256 mismatch.");
            var data = JsonUtility.FromJson<RawCatalogV13>(File.ReadAllText(Absolute(catalogAssetPath)));
            if (data == null || data.packageId != RanchDevicesV13Pin.Package || data.sprites == null || data.clips == null || data.sprites.Length != RanchDevicesV13Pin.SpriteCount || data.clips.Length != RanchDevicesV13Pin.ClipCount)
                throw new InvalidOperationException("Incomplete V13 package.");
            var source = new Dictionary<string, RawSpriteV13>(StringComparer.Ordinal);
            var clipPaths = new HashSet<string>(StringComparer.Ordinal);
            // All source and output ownership checks finish BEFORE importer/generated-asset writes.
            foreach (var s in data.sprites)
            {
                if (s == null || string.IsNullOrEmpty(s.key) || source.ContainsKey(s.key) || string.IsNullOrEmpty(s.file) || !s.file.StartsWith("Sprites/", StringComparison.Ordinal) || s.file.Contains("..") || !s.file.EndsWith(".png", StringComparison.Ordinal) || s.ppu != 256 || s.footprintX < 1 || s.footprintY < 1 || s.width != s.footprintX * 256 + 64 || s.height != s.footprintY * 256 + 64 || s.pivotX != .5f || s.pivotY != .5f)
                    throw new InvalidOperationException("Invalid sprite record.");
                string abs = Absolute(root + "/" + s.file);
                if (!File.Exists(abs) || Hash(abs) != s.sha256) throw new InvalidOperationException("Missing/corrupt sprite: " + s.file);
                source.Add(s.key, s);
            }
            string outRoot = root + Generated, catalogPath = outRoot + "/RanchDevicesV13Catalog.asset";
            string prefabPath = outRoot + "/InspectionOnly_AllClips.prefab";
            var oldCatalog = AssetDatabase.LoadMainAssetAtPath(catalogPath);
            if (oldCatalog != null && (!(oldCatalog is RanchDevicesV13Catalog) || ((RanchDevicesV13Catalog)oldCatalog).packageId != RanchDevicesV13Pin.Package || EditorUtility.IsDirty(oldCatalog)))
                throw new InvalidOperationException("Foreign/dirty generated catalog; resolve before build.");
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == prefabPath) throw new InvalidOperationException("Close the target inspection prefab stage before rebuild.");
            var oldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (oldPrefab != null && (oldPrefab.name != "RanchDevicesV13_InspectionOnly" || EditorUtility.IsDirty(oldPrefab))) throw new InvalidOperationException("Foreign/dirty inspection prefab.");
            foreach (var c in data.clips)
            {
                if (c == null || string.IsNullOrEmpty(c.key) || c.frames == null || c.frames.Length == 0 || c.fps < 1 || c.fps > 60 || float.IsNaN(c.fps) || float.IsInfinity(c.fps) || c.frames.Any(k => !source.ContainsKey(k))) throw new InvalidOperationException("Invalid clip record.");
                string cp = outRoot + "/Clips/" + c.key.Replace('/', '_') + ".anim";
                if (!clipPaths.Add(cp)) throw new InvalidOperationException("Duplicate clip path.");
                var old = AssetDatabase.LoadMainAssetAtPath(cp);
                if (old != null && (!(old is AnimationClip) || old.name != "RanchDevicesV13|" + c.key || EditorUtility.IsDirty(old))) throw new InvalidOperationException("Foreign/dirty generated clip: " + cp);
            }
            var imported = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (var s in data.sprites)
            {
                string path = root + "/" + s.file;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("No texture importer: " + path);
                Configure(importer); importer.SaveAndReimport();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null || Mathf.Abs(sprite.pixelsPerUnit - 256) > .001f || sprite.texture.width != s.width || sprite.texture.height != s.height)
                    throw new InvalidOperationException("Actual Sprite import failed: " + path);
                imported.Add(s.key, sprite);
            }
            Directory.CreateDirectory(Absolute(outRoot + "/Clips")); AssetDatabase.Refresh();
            var catalog = oldCatalog as RanchDevicesV13Catalog;
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<RanchDevicesV13Catalog>(); AssetDatabase.CreateAsset(catalog, catalogPath); }
            catalog.packageId = RanchDevicesV13Pin.Package; catalog.sourceSha256 = RanchDevicesV13Pin.Sha256;
            catalog.sprites = data.sprites.Select(s => new RanchV13Sprite { key = s.key, sprite = imported[s.key], footprint = new Vector2Int(s.footprintX, s.footprintY), role = s.role }).ToArray();
            catalog.clips = data.clips.Select(c => new RanchV13Clip { key = c.key, fps = c.fps, loop = c.loop, frames = c.frames.Select(k => imported[k]).ToArray() }).ToArray();
            string validation; if (!catalog.Validate(out validation)) throw new InvalidOperationException(validation);
            EditorUtility.SetDirty(catalog);
            foreach (var c in data.clips)
            {
                string path = outRoot + "/Clips/" + c.key.Replace('/', '_') + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
                clip.name = "RanchDevicesV13|" + c.key; clip.frameRate = c.fps;
                var keys = new ObjectReferenceKeyframe[c.frames.Length + 1];
                for (int i = 0; i < c.frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / c.fps, value = imported[c.frames[i]] };
                keys[c.frames.Length] = new ObjectReferenceKeyframe { time = c.frames.Length / c.fps, value = imported[c.frames[c.loop ? 0 : c.frames.Length - 1]] };
                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
                var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = c.loop; AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
            }
            var board = new GameObject("RanchDevicesV13_InspectionOnly");
            try
            {
                int i = 0;
                foreach (var c in catalog.clips)
                {
                    var go = new GameObject(c.key); go.transform.SetParent(board.transform, false); go.transform.localPosition = new Vector3((i % 8) * 2.3f, -(i / 8) * 1.7f, 0);
                    go.AddComponent<SpriteRenderer>(); var visual = go.AddComponent<RanchDeviceVisualV13>();
                    if (!visual.Configure(catalog, out validation) || !visual.SetState(c.key, true)) throw new InvalidOperationException(validation ?? c.key);
                    visual.ConfigureInspectionAutoplay(c.key); i++;
                }
                if (PrefabUtility.SaveAsPrefabAsset(board, prefabPath) == null) throw new InvalidOperationException("Inspection prefab creation failed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(board); }
            AssetDatabase.SaveAssets();
            Debug.Log("Ranch V13 built: " + imported.Count + " actual imported sprites / " + catalog.clips.Length + " actual .anim clips. Production gameplay binding still requires host integration.");
        }
        internal static void Configure(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 256;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048; importer.isReadable = false;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f, .5f); settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            foreach (string platform in new[] { "Android", "iPhone", "Standalone", "WebGL" }) importer.ClearPlatformTextureSettings(platform);
        }
    }
    public sealed class RanchDevicesV13TexturePostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath.Contains("/" + RanchDevicesV13Pin.Package + "/Sprites/") && assetPath.EndsWith(".png", StringComparison.Ordinal))
                RanchDevicesV13Builder.Configure((TextureImporter)assetImporter);
        }
    }
}
