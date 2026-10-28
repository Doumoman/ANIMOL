#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Art.EchoTheaterV03;
using ANIMOL.Art.V12;
using ANIMOL.Presentation;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Art.DevicePatch01.Editor
{
    // Rebind only the five changed device families. Other themed prefabs retain their art.
    public static class DevicePatchP01PrefabBinder
    {
        const string Ranch = "Assets/ANIMOL/Prefabs/Themes/starway";
        const string Theater = "Assets/ANIMOL/Prefabs/Themes/memory_theater";

        public static string Bind()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var ranch = AssetDatabase.LoadAssetAtPath<StarwayRanchV12VisualCatalog>(
                StarwayRanchV12ProjectBuilder.Root + "/_Generated/StarwayRanchV12VisualCatalog.asset");
            var theater = AssetDatabase.LoadAssetAtPath<EchoTheaterV03VisualCatalog>(
                EchoTheaterV03ProjectBuilder.Root + "/_Generated/EchoTheaterV03VisualCatalog.asset");
            StarwayRanchV12AnimationBuilder.ValidateBuiltCatalog(ranch);
            EchoTheaterV03AnimationBuilder.ValidateBuiltCatalog(theater);
            int prefabs = 0, states = 0;
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { Ranch, Theater })
                         .Select(AssetDatabase.GUIDToAssetPath).OrderBy(x => x, StringComparer.Ordinal))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                int role = name.StartsWith("d09_", StringComparison.Ordinal) ? 9 :
                    name.StartsWith("d20_", StringComparison.Ordinal) && path.StartsWith(Ranch, StringComparison.Ordinal) ? 20 :
                    name.StartsWith("d13_", StringComparison.Ordinal) && path.StartsWith(Theater, StringComparison.Ordinal) ? 13 :
                    name.StartsWith("d08_", StringComparison.Ordinal) && path.StartsWith(Theater, StringComparison.Ordinal) ? 8 : 0;
                if (role == 0) continue;
                bool isRanch = path.StartsWith(Ranch, StringComparison.Ordinal);
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var view = prefab.GetComponent<DeviceVisualView>();
                    if (!view || !view.visual || view.states == null)
                        throw new InvalidDataException("Missing P01 device visual: " + path);
                    int changed = 0;
                    foreach (var state in view.states)
                    {
                        string alias = state.name == "static" ? StaticAlias(name, role) : state.name;
                        if (role == 8 && alias.StartsWith("plate_", StringComparison.Ordinal)) continue;
                        var actionEvent = state.name == "static" && !name.EndsWith("_static", StringComparison.Ordinal)
                            ? "p01_preview" : state.eventName;
                        string key = Key(role, alias, actionEvent, state.v12ClipKey, state.v03ClipKey);
                        float progress = Progress(role, alias);
                        if (isRanch)
                        {
                            if (!ranch.TryGetClip(key, ranch.SourceManifestSha256, out var clip, out var error))
                                throw new InvalidDataException(path + "/" + alias + ": " + error);
                            state.v12ClipKey = key; state.v12StaticProgress = progress;
                            state.clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                                StarwayRanchV12AnimationBuilder.ClipAssetPath(StarwayRanchV12ProjectBuilder.Root, key));
                            state.sequence = Animate(alias, actionEvent)
                                ? Sequence(StarwayRanchV12ProjectBuilder.Root,
                                    Path.GetFileNameWithoutExtension(StarwayRanchV12AnimationBuilder.ClipAssetPath(
                                        StarwayRanchV12ProjectBuilder.Root, key)), clip.FrameCount,
                                    clip.Fps, clip.Loop, i => clip.GetFrame(i).Sprite) : null;
                            state.sprite = state.sequence ? null : clip.GetFrame(progress >= .5f ? clip.FrameCount - 1 : 0).Sprite;
                            state.terminal = state.sequence ? clip.GetFrame(clip.FrameCount - 1).Sprite : null;
                        }
                        else
                        {
                            if (!theater.TryGetClip(key, theater.SourceManifestSha256, out var clip, out var error))
                                throw new InvalidDataException(path + "/" + alias + ": " + error);
                            state.v03ClipKey = key; state.v03StaticProgress = progress;
                            state.clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                                EchoTheaterV03AnimationBuilder.ClipAssetPath(EchoTheaterV03ProjectBuilder.Root, key));
                            state.sequence = Animate(alias, actionEvent)
                                ? Sequence(EchoTheaterV03ProjectBuilder.Root,
                                    Path.GetFileNameWithoutExtension(EchoTheaterV03AnimationBuilder.ClipAssetPath(
                                        EchoTheaterV03ProjectBuilder.Root, key)), clip.FrameCount,
                                    clip.Fps, clip.Loop, i => clip.GetFrame(i).Sprite) : null;
                            state.sprite = state.sequence ? null : clip.GetFrame(progress >= .5f ? clip.FrameCount - 1 : 0).Sprite;
                            state.terminal = state.sequence ? clip.GetFrame(clip.FrameCount - 1).Sprite : null;
                        }
                        if (!state.clip) throw new InvalidDataException("Generated P01 clip missing: " + key);
                        changed++; states++;
                    }
                    if (changed == 0) continue;
                    var initial = Array.Find(view.states, s => s.name == view.initialState);
                    if (initial == null) throw new InvalidDataException("Initial state missing: " + path);
                    view.visual.sprite = initial.sprite ? initial.sprite :
                        (initial.sequence && initial.sequence.frames.Length > 0 ? initial.sequence.frames[0] : null);
                    view.visual.drawMode = SpriteDrawMode.Simple;
                    view.visual.transform.localScale = Vector3.one;
                    view.visual.transform.localRotation = Quaternion.identity;
                    if (role == 8 || role == 13 || role == 9)
                        view.visual.transform.localPosition = Vector3.zero;
                    EditorUtility.SetDirty(view);
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    prefabs++;
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            AssetDatabase.SaveAssets();
            return "P01 prefabs=" + prefabs + ", states=" + states;
        }

        static SpriteFrameSequence Sequence(string root, string name, int count, float fps,
            bool loop, Func<int, Sprite> sprite)
        {
            var folder = root + "/_Generated/Sequences";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(root + "/_Generated", "Sequences");
            var path = folder + "/" + name + ".asset";
            var sequence = AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(path);
            if (!sequence)
            { sequence = ScriptableObject.CreateInstance<SpriteFrameSequence>(); AssetDatabase.CreateAsset(sequence, path); }
            sequence.frames = Enumerable.Range(0, count).Select(sprite).ToArray();
            sequence.durations = Enumerable.Repeat(1f / fps, count).ToArray();
            sequence.loop = loop;
            EditorUtility.SetDirty(sequence);
            return sequence;
        }
        static bool Animate(string alias, string eventName) =>
            !string.IsNullOrEmpty(eventName) && (alias.StartsWith("open_", StringComparison.Ordinal) ||
            alias.StartsWith("close_", StringComparison.Ordinal) ||
            alias.StartsWith("blocked_", StringComparison.Ordinal) ||
            alias.StartsWith("fold_", StringComparison.Ordinal) ||
            alias.StartsWith("rotate_", StringComparison.Ordinal) ||
            alias.StartsWith("blower_run_", StringComparison.Ordinal) ||
            alias.StartsWith("wind_push_", StringComparison.Ordinal));
        static string StaticAlias(string prefab, int role)
        {
            string prefix = role == 8 ? "d08_rotating_bookshelf_" :
                role == 9 ? "d09_one_way_gate_" : "d13_domino_wall_";
            if (!prefab.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException("Unrecognized P01 static prefab: " + prefab);
            var alias = prefab.Substring(prefix.Length);
            return alias.EndsWith("_static", StringComparison.Ordinal)
                ? alias.Substring(0, alias.Length - "_static".Length) : alias;
        }
        static string Direction(string alias)
        {
            string end = alias.Substring(alias.LastIndexOf('_') + 1);
            switch (end)
            { case "e": return "east"; case "n": return "north";
              case "w": return "west"; case "s": return "south";
              default: throw new InvalidDataException("P01 cardinal alias required: " + alias); }
        }
        static string Key(int role, string alias, string eventName, string ranchKey, string theaterKey)
        {
            if (role == 20)
            {
                if (string.IsNullOrEmpty(ranchKey)) throw new InvalidDataException("Wind state lacks V12 key: " + alias);
                return ranchKey;
            }
            if (role == 9)
            {
                string state = alias.StartsWith("closed_", StringComparison.Ordinal) ? "closed" :
                    alias.StartsWith("open_", StringComparison.Ordinal) ?
                        (string.IsNullOrEmpty(eventName) ? "open" : "opening") :
                    alias.StartsWith("close_", StringComparison.Ordinal) ? "closing" :
                    alias.StartsWith("blocked_", StringComparison.Ordinal) ? "blocked" :
                    throw new InvalidDataException("Unsupported gate alias: " + alias);
                return "oneway_gate_" + Direction(alias) + "_" + state;
            }
            if (role == 13) return "domino_wall.fall." + Direction(alias);
            if (role == 8)
            {
                bool horizontal = alias.Contains("horizontal") || alias.Contains("rotate_0_") ||
                    alias.Contains("rotate_180_");
                bool vertical = alias.Contains("vertical") || alias.Contains("rotate_90_") ||
                    alias.Contains("rotate_270_");
                if (!horizontal && !vertical) throw new InvalidDataException("Unsupported bookcase alias: " + alias);
                return horizontal ? "id08_bookcase_H_to_V" : "id08_bookcase_V_to_H";
            }
            throw new InvalidDataException("Unsupported P01 role: " + role);
        }
        static float Progress(int role, string alias)
        {
            if (role == 9) return 0f;
            if (role == 13) return alias.StartsWith("folded_", StringComparison.Ordinal) ? 1f : 0f;
            if (role == 8) return alias.StartsWith("vertical", StringComparison.Ordinal) ||
                alias.StartsWith("preview_vertical", StringComparison.Ordinal) ? 0f : 0f;
            return 0f;
        }
    }
}
#endif
