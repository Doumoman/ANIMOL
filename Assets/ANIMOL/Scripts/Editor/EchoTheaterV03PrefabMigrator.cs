using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Presentation;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Art.EchoTheaterV03
{
    /// <summary>Rebinds only theater device prefabs; the puzzle role, ID and authority stay intact.</summary>
    public static class EchoTheaterV03PrefabMigrator
    {
        const string Prefabs = "Assets/ANIMOL/Prefabs/Themes/memory_theater";
        const string Sequences = EchoTheaterV03ProjectBuilder.Root + "/_Generated/Sequences";
        [Serializable] sealed class DiceDocument { public Orientation[] orientations; public Roll[] rolls; }
        [Serializable] sealed class Orientation
        { public string id, stableClipKey; public int up, north, east; }
        [Serializable] sealed class Roll { public string fromId, heading, clipKey; }
        sealed class Binding { public string key; public float progress; }

        [MenuItem("ANIMOL/Art QA/Migrate Echo Theater V03 device prefabs")]
        public static string Migrate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before migrating device prefabs.");
            var root = EchoTheaterV03ProjectBuilder.Root;
            var catalog = AssetDatabase.LoadAssetAtPath<EchoTheaterV03VisualCatalog>(
                root + "/_Generated/EchoTheaterV03VisualCatalog.asset");
            EchoTheaterV03AnimationBuilder.ValidateBuiltCatalog(catalog);
            var clips = Enumerable.Range(0, catalog.ClipCount).Select(catalog.GetClipAt)
                .ToDictionary(c => c.Key, StringComparer.Ordinal);
            var dice = JsonUtility.FromJson<DiceDocument>(File.ReadAllText(root + "/dice_orientation_map.json"));
            if (dice?.orientations?.Length != 24 || dice.rolls?.Length != 96)
                throw new InvalidDataException("Complete latest 24-orientation/96-roll dice map required.");
            var stableByOld = dice.orientations.ToDictionary(
                d => "t" + d.up + "_n" + d.north + "_e" + d.east, d => d, StringComparer.Ordinal);
            var rollByOld = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in dice.rolls)
            {
                var before = dice.orientations.FirstOrDefault(x => x.id == row.fromId);
                if (before == null) throw new InvalidDataException("Invalid dice map fromId.");
                var direction = row.heading.ToLowerInvariant();
                rollByOld.Add("roll_t" + before.up + "_n" + before.north + "_e" + before.east + "_" + direction,
                    row.clipKey);
            }
            if (!AssetDatabase.IsValidFolder(Sequences)) AssetDatabase.CreateFolder(root + "/_Generated", "Sequences");
            var generated = new Dictionary<string, SpriteFrameSequence>(StringComparer.Ordinal);
            foreach (var clip in clips.Values)
            {
                var name = Path.GetFileNameWithoutExtension(EchoTheaterV03AnimationBuilder.ClipAssetPath(root, clip.Key));
                var path = Sequences + "/" + name + ".asset";
                var sequence = AssetDatabase.LoadAssetAtPath<SpriteFrameSequence>(path);
                if (!sequence) { sequence = ScriptableObject.CreateInstance<SpriteFrameSequence>(); AssetDatabase.CreateAsset(sequence, path); }
                sequence.frames = Enumerable.Range(0, clip.FrameCount).Select(i => clip.GetFrame(i).Sprite).ToArray();
                sequence.durations = Enumerable.Repeat(1f / clip.Fps, clip.FrameCount).ToArray();
                sequence.loop = clip.Loop;
                EditorUtility.SetDirty(sequence);
                generated.Add(clip.Key, sequence);
            }

            int prefabsChanged = 0, statesChanged = 0;
            var skipped = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Prefabs }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                int role = Role(name);
                if (role == 0) continue;
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var view = prefab.GetComponent<DeviceVisualView>();
                    if (!view || !view.visual || view.states == null) { skipped.Add(name + ": missing view"); continue; }
                    bool changed = false;
                    foreach (var state in view.states)
                    {
                        string alias = state.name == "static" ? StaticAlias(name, role) : state.name;
                        var binding = Resolve(role, alias, stableByOld, rollByOld);
                        if (binding == null)
                        { skipped.Add(name + "/" + alias); continue; }
                        if (!clips.TryGetValue(binding.key, out var clip))
                            throw new InvalidDataException("Missing exact V03 clip: " + binding.key);
                        var animation = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                            EchoTheaterV03AnimationBuilder.ClipAssetPath(root, binding.key));
                        if (!animation) throw new InvalidDataException("Missing generated V03 .anim: " + binding.key);
                        bool animated = state.sequence != null;
                        state.v03ClipKey = binding.key;
                        state.v03StaticProgress = binding.progress;
                        state.clip = animation;
                        state.sequence = animated ? generated[binding.key] : null;
                        int frame = binding.progress >= .5f ? clip.FrameCount - 1 : 0;
                        state.sprite = animated ? null : clip.GetFrame(frame).Sprite;
                        state.terminal = animated ? clip.GetFrame(clip.FrameCount - 1).Sprite : null;
                        statesChanged++; changed = true;
                    }
                    if (!changed) continue;
                    var initial = Array.Find(view.states, s => s.name == view.initialState);
                    if (initial != null && !string.IsNullOrEmpty(initial.v03ClipKey))
                    {
                        var clip = clips[initial.v03ClipKey];
                        view.visual.sprite = initial.sprite ? initial.sprite : clip.GetFrame(0).Sprite;
                        view.visual.drawMode = SpriteDrawMode.Simple;
                        view.visual.transform.localScale = Vector3.one;
                        view.visual.transform.localRotation = Quaternion.identity;
                    }
                    EditorUtility.SetDirty(view);
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    prefabsChanged++;
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            AssetDatabase.SaveAssets();
            var report = "Migrated " + statesChanged + " states in " + prefabsChanged + " theater prefabs to V03; " +
                skipped.Count + " supporting/unknown state aliases retained.\n" + string.Join("\n", skipped);
            File.WriteAllText(root + "/_Generated/prefab_migration_report.txt", report);
            AssetDatabase.ImportAsset(root + "/_Generated/prefab_migration_report.txt");
            Debug.Log("[Echo Theater V03] " + report);
            return "prefabs=" + prefabsChanged + ", states=" + statesChanged + ", unresolved=" + skipped.Count;
        }

        static int Role(string name)
        {
            if (name.Length < 3 || name[0] != 'd' || !int.TryParse(name.Substring(1, 2), out var role)) return 0;
            return new[] { 4, 6, 7, 8, 9, 13, 17, 18, 19, 22, 23 }.Contains(role) ? role : 0;
        }
        static string StaticAlias(string prefab, int role)
        {
            string prefix = role == 4 ? "d04_stage_shutter_" : role == 6 ? "d06_moving_stage_" :
                role == 7 ? "d07_conveyor_" : role == 8 ? "d08_rotating_bookshelf_" :
                role == 9 ? "d09_one_way_gate_" : role == 13 ? "d13_domino_wall_" :
                role == 17 ? "d17_charging_statue_" : role == 18 ? "d18_bounce_corner_" :
                role == 19 ? "d19_brake_pad_" : role == 22 ? "d22_dice_block_" : "d23_coward_doll_";
            return prefab.StartsWith(prefix, StringComparison.Ordinal) && prefab.EndsWith("_static", StringComparison.Ordinal)
                ? prefab.Substring(prefix.Length, prefab.Length - prefix.Length - "_static".Length) : "static";
        }
        static Binding B(string key, float progress = 0f) => new Binding { key = key, progress = progress };
        static string Direction(string alias)
        {
            var last = alias.Substring(alias.LastIndexOf('_') + 1);
            switch (last)
            {
                case "e": return "east"; case "n": return "north";
                case "w": return "west"; case "s": return "south";
                default: return null;
            }
        }
        static string Abbrev(string direction) => direction == "east" ? "E" : direction == "north" ? "N" :
            direction == "west" ? "W" : "S";

        static Binding Resolve(int role, string alias, Dictionary<string, Orientation> dice,
            Dictionary<string, string> rolls)
        {
            string d = Direction(alias);
            switch (role)
            {
                case 4:
                    string axis = alias.Contains("horizontal") ? "horizontal" : alias.Contains("vertical") ? "vertical" : null;
                    if (axis == null) return null;
                    if (alias.EndsWith("_closed")) return B("id04_shutter_" + axis + "_open");
                    if (alias.EndsWith("_open")) return B("id04_shutter_" + axis + "_open", 1);
                    return B("id04_shutter_" + axis + "_close");
                case 6:
                    d = d ?? (alias.Contains("vertical") ? "north" : "east");
                    return B("id06_stage_" + Abbrev(d) + "_move_wide", alias.StartsWith("destination_") ? 1 : 0);
                case 7: return d == null ? null : B("conveyor_" + d + "_transport");
                case 8:
                    if (alias.StartsWith("plate_")) return null;
                    if (alias.StartsWith("rotate_"))
                    {
                        bool horizontal = alias.StartsWith("rotate_0_") || alias.StartsWith("rotate_180_");
                        return B(horizontal ? "id08_bookcase_H_to_V" : "id08_bookcase_V_to_H");
                    }
                    if (alias.Contains("horizontal")) return B("id08_bookcase_H_to_V");
                    if (alias.Contains("vertical")) return B("id08_bookcase_H_to_V", 1);
                    return null;
                case 9:
                    if (d == null) return null;
                    if (alias.StartsWith("open_")) return B("oneway_gate_" + d + "_opening", 1);
                    return B("oneway_gate_" + d + "_closing");
                case 13:
                    if (d == null) return null;
                    return B("domino_wall.fall." + d, alias.StartsWith("folded_") ? 1 : 0);
                case 17:
                    if (d == null) return null;
                    string phase = alias.StartsWith("warning_") ? "warning" : alias.StartsWith("charge_") ? "charge" :
                        alias.StartsWith("impact_") ? "impact" : alias.StartsWith("recover_") ? "recover" : "idle";
                    return B("charging_statue_" + Abbrev(d) + "_" + phase,
                        alias.StartsWith("warning_cell_") || alias.StartsWith("warning_hold_") ? 1 : 0);
                case 18:
                    var corner = alias.Substring(alias.LastIndexOf('_') + 1);
                    return new[] { "ne", "nw", "se", "sw" }.Contains(corner) ? B("bounce_corner_" + corner + "_rebound") : null;
                case 19:
                    var brake = alias.Contains("vertical") ? "vertical" : alias.Contains("horizontal") ? "horizontal" : null;
                    return brake == null ? null : B("brake_pad_" + brake + "_press_release", alias.StartsWith("pressed_") ? .5f : 0);
                case 22:
                    if (alias.StartsWith("roll_") && rolls.TryGetValue(alias, out var key)) return B(key);
                    if (dice.TryGetValue(alias, out var state)) return B(state.stableClipKey);
                    return null; // Face targets and preview parts are not in the V03 animation catalog.
                case 23:
                    if (d == null) return null;
                    phase = alias.StartsWith("startle_") || alias.StartsWith("startled_") ? "startle" :
                        alias.StartsWith("retreat_") || alias.StartsWith("retreat_preview_") || alias.StartsWith("step_") ? "retreat" :
                        alias.StartsWith("blocked_") ? "blocked_flinch" : "idle";
                    return B("timid_puppet_" + Abbrev(d) + "_" + phase,
                        alias.StartsWith("startled_") || alias.StartsWith("step_") ? 1 : 0);
                default: return null;
            }
        }
    }
}
