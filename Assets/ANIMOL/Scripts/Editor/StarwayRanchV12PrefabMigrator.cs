using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Art.V12
{
    // Copies only ranch device visuals. Shared device prefabs stay bound to the other themes.
    public static class StarwayRanchV12PrefabMigrator
    {
        const string Source = "Assets/ANIMOL/Prefabs/Devices";
        const string Target = "Assets/ANIMOL/Prefabs/Themes/starway";
        const string Sequences = StarwayRanchV12ProjectBuilder.Root + "/_Generated/Sequences";
        static readonly string[] Names = { "d01_star_socket", "d02_pressure_vine_bridge", "d07_conveyor",
            "d09_one_way_gate", "d18_bounce_corner", "d19_brake_pad", "d20_wind_corridor", "d24_follower_chick" };
        sealed class Binding { public string key; public float progress; public bool animate; }
        static Binding B(string key, bool animate = false, float progress = 0f)
        { return new Binding { key = key, animate = animate, progress = progress }; }
        static string Direction(string name)
        {
            var suffix = name.Substring(name.LastIndexOf('_') + 1);
            switch (suffix)
            { case "e": return "east"; case "n": return "north"; case "w": return "west"; case "s": return "south"; default: return null; }
        }
        static string Axis(string name) => name.Contains("vertical") || name.EndsWith("_n") || name.EndsWith("_s")
            ? "vertical" : "horizontal";
        static string Capital(string dir) => dir == "east" ? "E" : dir == "north" ? "N" :
            dir == "west" ? "W" : "S";

        public static string Migrate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            var root = StarwayRanchV12ProjectBuilder.Root;
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayRanchV12VisualCatalog>(
                root + "/_Generated/StarwayRanchV12VisualCatalog.asset");
            StarwayRanchV12AnimationBuilder.ValidateBuiltCatalog(catalog);
            var clips = Enumerable.Range(0, catalog.ClipCount).Select(catalog.GetClipAt)
                .ToDictionary(c => c.Key, StringComparer.Ordinal);
            if (!AssetDatabase.IsValidFolder("Assets/ANIMOL/Prefabs/Themes/starway"))
                AssetDatabase.CreateFolder("Assets/ANIMOL/Prefabs/Themes", "starway");
            if (!AssetDatabase.IsValidFolder(Sequences)) AssetDatabase.CreateFolder(root + "/_Generated", "Sequences");
            var generated = new Dictionary<string, SpriteFrameSequence>(StringComparer.Ordinal);
            foreach (var clip in clips.Values)
            {
                var name = Path.GetFileNameWithoutExtension(StarwayRanchV12AnimationBuilder.ClipAssetPath(root, clip.Key));
                var path = Sequences + "/" + name + ".asset";
                var sequence = AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(path);
                if (!sequence) { sequence = ScriptableObject.CreateInstance<SpriteFrameSequence>(); AssetDatabase.CreateAsset(sequence, path); }
                sequence.frames = Enumerable.Range(0, clip.FrameCount).Select(i => clip.GetFrame(i).Sprite).ToArray();
                sequence.durations = Enumerable.Repeat(1f / clip.Fps, clip.FrameCount).ToArray();
                sequence.loop = clip.Loop;
                EditorUtility.SetDirty(sequence);
                generated.Add(clip.Key, sequence);
            }
            int stateCount = 0;
            foreach (var name in Names)
            {
                var original = Source + "/" + name + ".prefab";
                var output = Target + "/" + name + ".prefab";
                var prefab = PrefabUtility.LoadPrefabContents(original);
                try
                {
                    var view = prefab.GetComponent<DeviceVisualView>();
                    if (!view || !view.visual || view.states == null)
                        throw new InvalidDataException("Missing source device visual: " + original);
                    var role = int.Parse(name.Substring(1, 2));
                    var kept = new List<DeviceVisualState>();
                    foreach (var state in view.states)
                    {
                        if (role == 20 && state.name == "metal_crate") continue; // Separate common crate prefab.
                        var binding = Resolve(role, state.name);
                        if (binding == null || !clips.TryGetValue(binding.key, out var clip))
                            throw new InvalidDataException("No exact V12 binding: " + name + "/" + state.name);
                        var animation = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                            StarwayRanchV12AnimationBuilder.ClipAssetPath(root, binding.key));
                        if (!animation) throw new InvalidDataException("Missing V12 .anim: " + binding.key);
                        state.v12ClipKey = binding.key;
                        state.v12StaticProgress = binding.progress;
                        state.v03ClipKey = null;
                        state.clip = animation;
                        state.sequence = binding.animate ? generated[binding.key] : null;
                        int frame = binding.progress >= .5f ? clip.FrameCount - 1 : 0;
                        state.sprite = binding.animate ? null : clip.GetFrame(frame).Sprite;
                        state.terminal = binding.animate ? clip.GetFrame(clip.FrameCount - 1).Sprite : null;
                        kept.Add(state); stateCount++;
                    }
                    view.states = kept.ToArray();
                    var initial = Array.Find(view.states, s => s.name == view.initialState);
                    if (initial == null) throw new InvalidDataException("Missing V12 initial state: " + name);
                    view.visual.sprite = initial.sprite ? initial.sprite : clips[initial.v12ClipKey].GetFrame(0).Sprite;
                    view.visual.drawMode = SpriteDrawMode.Simple;
                    view.visual.transform.localRotation = Quaternion.identity;
                    view.visual.transform.localScale = role == 24 ? new Vector3(.62f, .62f, 1) : Vector3.one;
                    view.visual.transform.localPosition = role == 24 ? new Vector3(0, .30515625f, 0) : Vector3.zero;
                    EditorUtility.SetDirty(view);
                    PrefabUtility.SaveAsPrefabAsset(prefab, output);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            int scenarios = RebindScenarios();
            AssetDatabase.SaveAssets();
            var report = "V12 ranch prefabs=" + Names.Length + ", states=" + stateCount +
                ", scenarios=" + scenarios + ", shared device prefabs unchanged; common metal crate retains shared art.";
            File.WriteAllText(root + "/_Generated/prefab_migration_report.txt", report);
            AssetDatabase.ImportAsset(root + "/_Generated/prefab_migration_report.txt");
            return report;
        }

        static int RebindScenarios()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:PuzzleScenario", new[] { "Assets/ANIMOL/Data/Puzzle" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var scenario in AssetDatabase.LoadAllAssetsAtPath(path).OfType<PuzzleScenario>())
                {
                    if (scenario.themeId != "starway") continue;
                    bool changed = false;
                    foreach (var device in scenario.devices)
                    {
                        int role = (int)device.role;
                        if (!new[] { 1, 2, 7, 9, 18, 19, 20, 24 }.Contains(role)) continue;
                        if (device.artTheme != "starway") continue;
                        var oldName = device.visualPrefab ? device.visualPrefab.name : null;
                        if (oldName == null || !Names.Contains(oldName))
                            throw new InvalidDataException("Unexpected ranch source prefab: " + path + "/" + device.id + " -> " + oldName);
                        var latest = AssetDatabase.LoadAssetAtPath<GameObject>(Target + "/" + oldName + ".prefab");
                        if (!latest) throw new InvalidDataException("Missing V12 ranch prefab: " + oldName);
                        device.visualPrefab = latest;
                        changed = true;
                    }
                    if (changed) { EditorUtility.SetDirty(scenario); count++; }
                }
            }
            return count;
        }

        static Binding Resolve(int role, string name)
        {
            var d = Direction(name);
            switch (role)
            {
                case 1:
                    if (name == "stone") return B("id01_star_stone_push");
                    if (name == "socket_empty") return B("id01_star_socket_seat");
                    if (name == "socket_filled") return B("id01_star_socket_seat", false, 1);
                    if (name.StartsWith("stone_push_")) return B("id01_star_stone_push", true);
                    if (name == "socket_insert") return B("id01_star_socket_seat", true);
                    break;
                case 2:
                    if (name == "plate_idle") return B("id02_pressure_plate_press");
                    if (name == "plate_pressed") return B("id02_pressure_plate_press", false, 1);
                    if (name == "plate_press") return B("id02_pressure_plate_press", true);
                    if (name == "plate_release") return B("id02_pressure_plate_release", true);
                    if (name.StartsWith("bridge_extend_")) return B("id02_vine_bridge_" + Axis(name) + "_open", true);
                    if (name.StartsWith("bridge_retract_")) return B("id02_vine_bridge_" + Axis(name) + "_close", true);
                    if (name.StartsWith("retracted_")) return B("id02_vine_bridge_" + Axis(name) + "_open");
                    if (name.StartsWith("bridge_idle_") || name.StartsWith("bridge_") ||
                        name.StartsWith("link_") || name.StartsWith("cap_"))
                        return B("id02_vine_bridge_" + Axis(name) + "_open", false, 1);
                    break;
                case 7:
                    if (d != null) return B("conveyor_" + d + "_transport", name.StartsWith("run_"));
                    break;
                case 9:
                    if (d == null) break;
                    if (name.StartsWith("closed_") || name.StartsWith("blocked_")) return B("oneway_gate_" + d + "_opening");
                    if (name.StartsWith("open_")) return B("oneway_gate_" + d + "_opening", true);
                    if (name.StartsWith("close_")) return B("oneway_gate_" + d + "_closing", true);
                    break;
                case 18:
                    d = name.Substring(name.LastIndexOf('_') + 1);
                    if (new[] { "ne", "nw", "se", "sw" }.Contains(d))
                        return B("bounce_corner_" + d + "_rebound", name.StartsWith("impact_"));
                    break;
                case 19:
                    var key = "brake_patch_" + Axis(name) + "_press_release";
                    if (name.StartsWith("brake_")) return B(key, true);
                    return B(key, false, name.StartsWith("pressed_") ? .5f : 0);
                case 20:
                    if (d == null) break;
                    key = "wind_corridor_" + Capital(d) + (name.StartsWith("wind_push_") || name.StartsWith("blower_run_") ? "_active" : "_idle");
                    return B(key, name.StartsWith("wind_push_") || name.StartsWith("blower_run_"));
                case 24:
                    if (d == null) break;
                    return B("chick." + (name.StartsWith("walk_") ? "walk." : "idle.") + d,
                        name.StartsWith("walk_"));
            }
            return null;
        }
    }
}
