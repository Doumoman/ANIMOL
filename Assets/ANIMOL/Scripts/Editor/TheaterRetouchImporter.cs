#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Animol.Art.TheaterRetouch;

namespace Animol.Art.TheaterRetouch.Editor
{
    /// <summary>Native-resolution retouch + PUSH import. A single-character package has 108 frames and 18 clips.</summary>
    public static class TheaterRetouchImporter
    {
        [Serializable] public sealed class Manifest
        {
            public bool nativeResolution;
            public Frame[] frames;
            public Clip[] clips;
        }
        [Serializable] public sealed class Contact
        {
            public float[] handPixel;
            public float targetFaceOffsetWorld;
            public string confidence;
        }
        [Serializable] public sealed class Frame
        {
            public string file, character, direction, state;
            public int width, height, index;
            public float pixelsPerUnit;
            public float[] pivot;
            public Contact contact;
        }
        [Serializable] public sealed class Clip
        {
            public string name, character, direction, state;
            public float fps;
            public bool loop;
            public string[] frames;
        }

        private static readonly HashSet<string> Characters = new HashSet<string>(new[] { "seri", "roan", "mion", "piko" });
        private static readonly string[] Directions = { "front", "side", "back" };
        private static readonly string[] States = { "idle", "move", "sliding", "jump", "victory", "push" };

        [MenuItem("ANIMOL/Art/Import Theater Retouch + Push")]
        public static void ImportMenu()
        {
            string root = EditorUtility.OpenFolderPanel("Select folder containing the retouch manifest", Application.dataPath, "");
            if (!string.IsNullOrEmpty(root)) ImportAllCharactersFromFolder(root);
        }

        // Unity -batchmode -quit -projectPath <project> -executeMethod
        // Animol.Art.TheaterRetouch.Editor.TheaterRetouchImporter.ImportBatch
        // -theaterRetouchRoot <absolute Assets folder> [-theaterRetouchManifest <manifest basename>]
        public static void ImportBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            string root = null, manifestName = null, bindingPath = null;
            for (int i = 0; i + 1 < args.Length; ++i)
            {
                if (args[i] == "-theaterRetouchRoot") root = args[i + 1];
                if (args[i] == "-theaterRetouchManifest") manifestName = args[i + 1];
                if (args[i] == "-theaterSpriteBindingPath") bindingPath = args[i + 1] == "." ? "" : args[i + 1];
            }
            if (string.IsNullOrEmpty(root)) throw new ArgumentException("Missing -theaterRetouchRoot");
            if (string.IsNullOrEmpty(manifestName)) ImportAllCharactersFromFolder(root, bindingPath);
            else ImportCharacterFromFolder(root, manifestName, bindingPath);
        }

        public static void ImportFromFolder(string root) { ImportCharacterFromFolder(root); }

        public static void ImportAllCharactersFromFolder(string root, string spriteRendererBindingPath = null)
        {
            root = Normalize(root).TrimEnd('/');
            bool found = false;
            foreach (string id in new[] { "seri", "roan", "mion", "piko" })
            {
                string name = "theater_" + id + "_retouch_animations.json";
                if (!File.Exists(Path.Combine(root, name))) continue;
                ImportCharacterFromFolder(root, name, spriteRendererBindingPath);
                found = true;
            }
            if (!found) ImportCharacterFromFolder(root, null, spriteRendererBindingPath);
        }

        public static void ImportCharacterFromFolder(string root, string manifestName = null, string spriteRendererBindingPath = null)
        {
            root = Normalize(root).TrimEnd('/');
            string assets = Normalize(Application.dataPath).TrimEnd('/');
            if (!root.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a theme art folder inside this project's Assets directory.");
            string manifestPath = ResolveManifest(root, manifestName);
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            Validate(manifest);
            AssetDatabase.Refresh();

            var metadata = new Dictionary<string, Frame>(StringComparer.Ordinal);
            var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            var characterIds = new HashSet<string>();
            foreach (Frame frame in manifest.frames) { metadata.Add(frame.file, frame); characterIds.Add(frame.character); }
            try
            {
                for (int i = 0; i < manifest.frames.Length; ++i)
                {
                    Frame frame = manifest.frames[i];
                    EditorUtility.DisplayProgressBar("ANIMOL Theater native retouch", frame.file, (float)i / manifest.frames.Length);
                    string absolute = SafePath(root, frame.file);
                    string assetPath = AssetPath(assets, absolute);
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null) throw new FileNotFoundException("Missing PNG texture importer", assetPath);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = frame.pixelsPerUnit;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.maxTextureSize = 8192;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.ClearPlatformTextureSettings("Android");
                    importer.ClearPlatformTextureSettings("iPhone");
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(frame.pivot[0], frame.pivot[1]);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null || sprite.rect.width != frame.width || sprite.rect.height != frame.height)
                        throw new InvalidDataException("Native PNG dimensions differ from manifest: " + assetPath);
                    sprites.Add(frame.file, sprite);
                }

                foreach (Clip data in manifest.clips)
                {
                    string outDir = Path.Combine(root, "Animations", data.character);
                    Directory.CreateDirectory(outDir);
                    AssetDatabase.Refresh();
                    string clipPath = AssetPath(assets, Normalize(Path.Combine(outDir, data.name + ".anim")));
                    AnimationClip clip = ResolveExistingClip(clipPath, data.name, out clipPath);
                    bool isNew = clip == null;
                    if (isNew && AssetDatabase.LoadMainAssetAtPath(clipPath) != null)
                        throw new InvalidDataException("Refusing to replace a different asset type: " + clipPath);
                    if (isNew) clip = new AnimationClip();
                    var binding = ResolveBinding(clip, assets, outDir, data.character, spriteRendererBindingPath);
                    var keys = new ObjectReferenceKeyframe[data.frames.Length + 1];
                    for (int i = 0; i < data.frames.Length; ++i)
                        keys[i] = new ObjectReferenceKeyframe { time = i / data.fps, value = sprites[data.frames[i]] };
                    keys[keys.Length - 1] = new ObjectReferenceKeyframe { time = data.frames.Length / data.fps, value = keys[keys.Length - 2].value };
                    clip.name = data.name;
                    clip.frameRate = data.fps;
                    clip.wrapMode = data.loop ? WrapMode.Loop : WrapMode.Once;
                    ReplaceMainSpriteCurve(clip, binding, keys);
                    if (isNew) AssetDatabase.CreateAsset(clip, clipPath);
                    var serialized = new SerializedObject(clip);
                    SerializedProperty loop = serialized.FindProperty("m_AnimationClipSettings.m_LoopTime");
                    if (loop != null) { loop.boolValue = data.loop; serialized.ApplyModifiedPropertiesWithoutUndo(); }
                    EditorUtility.SetDirty(clip);
                }

                string contactDir = Path.Combine(root, "PushContacts");
                Directory.CreateDirectory(contactDir);
                AssetDatabase.Refresh();
                foreach (string id in characterIds)
                {
                    var entries = new List<TheaterPushSpriteContacts.Entry>();
                    int missing = 0, fallback = 0;
                    foreach (Frame frame in manifest.frames)
                    {
                        if (frame.character != id || frame.state != "push") continue;
                        if (frame.contact == null || frame.contact.handPixel == null || frame.contact.handPixel.Length != 2)
                        { missing++; continue; }
                        if (frame.contact.confidence == "fallback") fallback++;
                        entries.Add(new TheaterPushSpriteContacts.Entry
                        {
                            sprite = sprites[frame.file],
                            handLocal = new Vector2(
                                (frame.contact.handPixel[0] - frame.pivot[0] * frame.width) / frame.pixelsPerUnit,
                                ((1f - frame.pivot[1]) * frame.height - frame.contact.handPixel[1]) / frame.pixelsPerUnit),
                            targetFaceOffsetWorld = Mathf.Max(0f, frame.contact.targetFaceOffsetWorld)
                        });
                    }
                    string assetPath = AssetPath(assets, Normalize(Path.Combine(contactDir, "theater_push_contacts_" + id + ".asset")));
                    var contacts = AssetDatabase.LoadAssetAtPath<TheaterPushSpriteContacts>(assetPath);
                    bool isNew = contacts == null;
                    if (isNew && AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
                        throw new InvalidDataException("Refusing to replace a different asset type: " + assetPath);
                    if (isNew) contacts = ScriptableObject.CreateInstance<TheaterPushSpriteContacts>();
                    contacts.entries = entries.ToArray();
                    contacts.characterId = id;
                    contacts.InvalidateCache();
                    if (isNew) AssetDatabase.CreateAsset(contacts, assetPath);
                    EditorUtility.SetDirty(contacts);
                    if (missing > 0 || fallback > 0)
                        Debug.LogWarning(id + " PUSH contacts: " + missing + " missing, " + fallback + " estimated anchors. Review all palm positions or supply a handSocket.");
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("ANIMOL Theater retouch: imported " + manifest.frames.Length + " native sprites, " + manifest.clips.Length + " clips, and PUSH contact assets. Matched existing clip GUIDs retained; absent clips created. Left uses side + flipX.");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static string ResolveManifest(string root, string name)
        {
            if (!string.IsNullOrEmpty(name)) return SafePath(root, name);
            string canonical = Path.Combine(root, "theater_retouch_animations.json");
            if (File.Exists(canonical)) return canonical;
            string[] candidates = Directory.GetFiles(root, "*retouch*animations*.json", SearchOption.TopDirectoryOnly);
            if (candidates.Length == 1) return candidates[0];
            throw new FileNotFoundException("Retouch manifest missing or ambiguous. Pass its exact basename with -theaterRetouchManifest.");
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        private static void ReplaceMainSpriteCurve(AnimationClip clip, EditorCurveBinding targetBinding, ObjectReferenceKeyframe[] keys)
        {
            var spriteBindings = new List<EditorCurveBinding>();
            bool targetExists = false;
            foreach (EditorCurveBinding existing in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                if (existing.type == typeof(SpriteRenderer) && existing.propertyName == "m_Sprite")
                {
                    spriteBindings.Add(existing);
                    if (existing.path == targetBinding.path) targetExists = true;
                }
            if (!targetExists && spriteBindings.Count > 1)
                throw new InvalidDataException("Renderer hierarchy changed and this clip has several sprite curves. Migrate the actual old main-renderer binding explicitly before import; accessory/face curves must not be guessed or deleted.");
            if (!targetExists && spriteBindings.Count == 1)
                AnimationUtility.SetObjectReferenceCurve(clip, spriteBindings[0], null);
            AnimationUtility.SetObjectReferenceCurve(clip, targetBinding, keys);
        }

        private static AnimationClip ResolveExistingClip(string preferredPath, string name, out string resolvedPath)
        {
            resolvedPath = preferredPath;
            AnimationClip preferred = AssetDatabase.LoadAssetAtPath<AnimationClip>(preferredPath);
            if (preferred != null) return preferred;
            var matches = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip " + name, new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null && (clip.name == name || Path.GetFileNameWithoutExtension(path) == name))
                    matches[path] = clip;
            }
            if (matches.Count > 1)
                throw new InvalidDataException("Several existing clips match " + name + ". Resolve the actual Animator/OverrideController reference and move that asset, preserving its GUID, to " + preferredPath + ". Candidates: " + string.Join(", ", matches.Keys));
            foreach (var match in matches) { resolvedPath = match.Key; return match.Value; }
            return null;
        }

        private static void AddSpriteBindings(AnimationClip clip, Dictionary<string, EditorCurveBinding> bindings)
        {
            if (clip == null) return;
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite") bindings[binding.path] = binding;
        }

        private static EditorCurveBinding ResolveBinding(AnimationClip currentClip, string assets, string directory, string characterId, string explicitPath)
        {
            if (explicitPath != null)
                return new EditorCurveBinding { path = explicitPath.Replace('\\', '/').Trim('/'), type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
            var bindings = new Dictionary<string, EditorCurveBinding>(StringComparer.Ordinal);
            AddSpriteBindings(currentClip, bindings);
            if (bindings.Count == 1) foreach (var binding in bindings.Values) return binding;
            if (bindings.Count > 1)
                throw new InvalidDataException("This clip has multiple SpriteRenderer bindings. Pass the actual main renderer path explicitly; do not guess a face/accessory renderer.");
            foreach (string path in Directory.GetFiles(directory, "*.anim", SearchOption.TopDirectoryOnly))
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetPath(assets, Normalize(path)));
                AddSpriteBindings(clip, bindings);
            }
            if (bindings.Count == 0)
                foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip " + characterId + "_", new[] { "Assets" }))
                {
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
                    if (clip != null && clip.name.StartsWith(characterId + "_", StringComparison.Ordinal)) AddSpriteBindings(clip, bindings);
                }
            if (bindings.Count == 1) foreach (var binding in bindings.Values) return binding;
            throw new InvalidDataException("No single main SpriteRenderer binding could be inferred. Pass AnimationUtility.CalculateTransformPath(mainRenderer.transform, animator.transform); use an explicit empty string when they share the same object.");
        }

        private static void Validate(Manifest manifest)
        {
            if (manifest == null || !manifest.nativeResolution || manifest.frames == null || manifest.clips == null)
                throw new InvalidDataException("Expected native-resolution retouch manifest.");
            var frames = new Dictionary<string, Frame>(StringComparer.Ordinal);
            var ids = new HashSet<string>();
            foreach (Frame frame in manifest.frames)
            {
                if (frame == null || string.IsNullOrEmpty(frame.file) || !Characters.Contains(frame.character) ||
                    frame.width <= 0 || frame.height <= 0 || !Finite(frame.pixelsPerUnit) || frame.pixelsPerUnit <= 0f ||
                    frame.pivot == null || frame.pivot.Length != 2 || !Finite(frame.pivot[0]) || !Finite(frame.pivot[1]) ||
                    frame.pivot[0] < 0f || frame.pivot[0] > 1f || frame.pivot[1] < 0f || frame.pivot[1] > 1f)
                    throw new InvalidDataException("Invalid native frame metadata.");
                frames.Add(frame.file, frame);
                ids.Add(frame.character);
                if (!("/" + frame.file.Replace('\\', '/')).Contains("/" + frame.character + "/"))
                    throw new InvalidDataException("Frame path does not belong to its character: " + frame.file);
                if (frame.contact != null && frame.contact.handPixel != null)
                    if (frame.contact.handPixel.Length != 2 || !Finite(frame.contact.handPixel[0]) || !Finite(frame.contact.handPixel[1]) ||
                        frame.contact.handPixel[0] < 0f || frame.contact.handPixel[0] > frame.width ||
                        frame.contact.handPixel[1] < 0f || frame.contact.handPixel[1] > frame.height ||
                        !Finite(frame.contact.targetFaceOffsetWorld) || frame.contact.targetFaceOffsetWorld < 0f)
                        throw new InvalidDataException("PUSH handPixel is outside the PNG or is not finite: " + frame.file);
            }
            if (ids.Count == 0 || manifest.frames.Length != ids.Count * 108 || manifest.clips.Length != ids.Count * 18)
                throw new InvalidDataException("Expected 108 frames and 18 clips per included Theater character.");
            var seen = new HashSet<string>();
            var clipNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (Clip clip in manifest.clips)
            {
                if (clip == null || !ids.Contains(clip.character) || string.IsNullOrEmpty(clip.name) ||
                    !Finite(clip.fps) || clip.fps <= 0f || clip.frames == null || clip.frames.Length != 6)
                    throw new InvalidDataException("Expected six frames per clip with valid positive FPS.");
                if (clip.name.IndexOfAny(new[] { '/', '\\' }) >= 0 || !clip.name.StartsWith(clip.character + "_", StringComparison.Ordinal))
                    throw new InvalidDataException("Clip name is outside its character scope.");
                if (!clipNames.Add(clip.name))
                    throw new InvalidDataException("Duplicate clip asset name: " + clip.name);
                if (clip.state == "push" && (!clip.loop || clip.fps < 10f))
                    throw new InvalidDataException("PUSH requires a loop at 10 FPS or higher.");
                if (!seen.Add(clip.character + "/" + clip.direction + "/" + clip.state))
                    throw new InvalidDataException("Duplicate character/direction/state clip.");
                for (int i = 0; i < clip.frames.Length; ++i)
                {
                    Frame frame;
                    if (!frames.TryGetValue(clip.frames[i], out frame) || frame.character != clip.character ||
                        frame.direction != clip.direction || frame.state != clip.state || frame.index != i)
                        throw new InvalidDataException("Clip frame order or metadata mismatch: " + clip.name);
                }
            }
            foreach (string id in ids)
                foreach (string direction in Directions)
                    foreach (string state in States)
                        if (!seen.Contains(id + "/" + direction + "/" + state))
                            throw new InvalidDataException("Missing clip: " + id + "/" + direction + "/" + state);
        }

        private static string Normalize(string path) { return Path.GetFullPath(path).Replace('\\', '/'); }
        private static string SafePath(string root, string relative)
        {
            string full = Normalize(Path.Combine(root, relative));
            if (!full.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Path escapes selected art folder: " + relative);
            return full;
        }
        private static string AssetPath(string assets, string absolute)
        {
            if (!absolute.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Asset must be inside Assets.");
            return "Assets" + absolute.Substring(assets.Length);
        }
    }
}
#endif
