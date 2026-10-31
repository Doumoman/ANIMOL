using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Art.EchoRailCartV01
{
    public static class EchoRailCartBuilderV01
    {
        [Serializable] public sealed class RawSprite { public string key, file, sha256; public int width, height, ppu; public float pivotX, pivotY; }
        [Serializable] public sealed class RawClip { public string key; public string[] frames; public float fps; public bool loop; }
        [Serializable] public sealed class RawCatalog { public string packageId; public int spriteCount, clipCount; public RawSprite[] sprites; public RawClip[] clips; }
        static string Absolute(string assetPath) { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath); }
        static string Hash(string path)
        {
            using (var h = SHA256.Create()) using (var f = File.OpenRead(path)) return BitConverter.ToString(h.ComputeHash(f)).Replace("-", "").ToLowerInvariant();
        }
        [MenuItem("Tools/ANIMOL/Echo Rail Cart V01/Build art catalog and clips")]
        public static void MenuBuild()
        {
            string path = EditorUtility.OpenFilePanel("Select rail-cart animation_catalog.json", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            string project = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/'); path = path.Replace('\\', '/');
            if (!path.StartsWith(project + "/Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("Install under Assets first.");
            Build(path.Substring(project.Length + 1));
        }
        public static void BuildFromCommandLine()
        {
            string[] a = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < a.Length; i++) if (a[i] == "-echoRailCartCatalog") { Build(a[i + 1].Replace('\\', '/')); return; }
            throw new InvalidOperationException("Supply -echoRailCartCatalog Assets/.../animation_catalog.json");
        }
        public static void Build(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..") || !path.EndsWith("/" + EchoRailCartPinV01.Package + "/animation_catalog.json", StringComparison.Ordinal))
                throw new InvalidOperationException("Select this package's catalog under Assets.");
            if (Hash(Absolute(path)) != EchoRailCartPinV01.Sha256) throw new InvalidOperationException("Catalog SHA256 mismatch.");
            var raw = JsonUtility.FromJson<RawCatalog>(File.ReadAllText(Absolute(path)));
            if (raw == null || raw.packageId != EchoRailCartPinV01.Package || raw.sprites == null || raw.sprites.Length != 14 || raw.clips == null || raw.clips.Length != 12)
                throw new InvalidOperationException("Incomplete rail-cart package.");
            string root = path.Substring(0, path.LastIndexOf('/')), output = root + "/_GeneratedRailCartV01";
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in raw.sprites)
                if (s == null || string.IsNullOrEmpty(s.key) || !names.Add(s.key) || string.IsNullOrEmpty(s.file) || !s.file.StartsWith("Sprites/", StringComparison.Ordinal) || !s.file.EndsWith(".png", StringComparison.Ordinal) || s.file.Contains("..")
                    || s.width != 320 || s.height != 320 || s.ppu != 256 || s.pivotX != .5f || s.pivotY != .5f || Hash(Absolute(root + "/" + s.file)) != s.sha256)
                    throw new InvalidOperationException("Invalid sprite source.");
            var clipNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in raw.clips)
            {
                if (c == null || string.IsNullOrEmpty(c.key) || !clipNames.Add(c.key) || c.frames == null || c.frames.Length == 0 || c.fps < 1 || c.fps > 60 || float.IsNaN(c.fps) || float.IsInfinity(c.fps)) throw new InvalidOperationException("Invalid clip.");
                foreach (string f in c.frames) if (!names.Contains(f)) throw new InvalidOperationException("Missing clip frame.");
                string target = output + "/" + c.key.Replace('/', '_') + ".anim";
                var existing = AssetDatabase.LoadMainAssetAtPath(target);
                if (existing != null && (!(existing is AnimationClip) ||
                    existing.name != Path.GetFileNameWithoutExtension(target) &&
                    existing.name != "EchoRailCartV01|" + c.key || EditorUtility.IsDirty(existing)))
                    throw new InvalidOperationException("Foreign or dirty output clip.");
            }
            string catalogPath = output + "/EchoRailCartCatalog.asset";
            var old = AssetDatabase.LoadMainAssetAtPath(catalogPath);
            if (old != null && (!(old is EchoRailCartArtCatalogV01) || ((EchoRailCartArtCatalogV01)old).packageId != EchoRailCartPinV01.Package || EditorUtility.IsDirty(old))) throw new InvalidOperationException("Foreign or dirty output catalog.");
            Directory.CreateDirectory(Absolute(output)); AssetDatabase.Refresh();
            var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal); var entries = new List<EchoRailCartArtCatalogV01.SpriteEntry>();
            foreach (var s in raw.sprites)
            {
                string asset = root + "/" + s.file; var importer = AssetImporter.GetAtPath(asset) as TextureImporter;
                if (importer == null) { AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport); importer = AssetImporter.GetAtPath(asset) as TextureImporter; }
                if (importer == null) throw new InvalidOperationException("Texture importer missing: " + asset);
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteAlignment = 9; settings.spritePivot = new Vector2(.5f, .5f); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 256;
                importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 512;
                foreach (string platform in new[] { "Android", "iPhone", "Standalone", "WebGL" }) importer.ClearPlatformTextureSettings(platform);
                importer.SaveAndReimport(); Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(asset);
                if (sprite == null || sprite.rect.width != 320 || sprite.rect.height != 320) throw new InvalidOperationException("Invalid imported sprite.");
                sprites.Add(s.key, sprite); entries.Add(new EchoRailCartArtCatalogV01.SpriteEntry { key = s.key, sprite = sprite });
            }
            var clips = new List<EchoRailCartArtCatalogV01.ClipEntry>();
            foreach (var c in raw.clips)
            {
                string asset = output + "/" + c.key.Replace('/', '_') + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(asset); bool fresh = clip == null;
                if (fresh) clip = new AnimationClip(); clip.name = "EchoRailCartV01|" + c.key; clip.frameRate = c.fps;
                var keys = new ObjectReferenceKeyframe[c.frames.Length + 1];
                for (int i = 0; i < c.frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / c.fps, value = sprites[c.frames[i]] };
                keys[keys.Length - 1] = new ObjectReferenceKeyframe { time = c.frames.Length / c.fps, value = sprites[c.frames[c.frames.Length - 1]] };
                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
                var setting = AnimationUtility.GetAnimationClipSettings(clip); setting.loopTime = c.loop; AnimationUtility.SetAnimationClipSettings(clip, setting);
                if (fresh) AssetDatabase.CreateAsset(clip, asset); else EditorUtility.SetDirty(clip);
                clips.Add(new EchoRailCartArtCatalogV01.ClipEntry { key = c.key, clip = clip });
            }
            var catalog = old as EchoRailCartArtCatalogV01; bool create = catalog == null;
            if (create) catalog = ScriptableObject.CreateInstance<EchoRailCartArtCatalogV01>();
            catalog.packageId = EchoRailCartPinV01.Package; catalog.sourceSha256 = EchoRailCartPinV01.Sha256; catalog.sprites = entries.ToArray(); catalog.clips = clips.ToArray();
            if (create) AssetDatabase.CreateAsset(catalog, catalogPath); else EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets(); Debug.Log("Echo Rail Cart V01: 14 sprites / 12 clips generated. Gameplay is owned by the project host.");
        }
    }
}
