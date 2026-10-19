using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Art.EchoTheaterV02
{
    // Installs the supplied PNGs into a serialized runtime lookup without changing theme/save IDs.
    public static class EchoTheaterV02Builder
    {
        const string Root = "Assets/ANIMOL/Sprites/Environment/memory_theater";
        const string Data = "Assets/ANIMOL/Data/Art/MemorySkyTheater/Environment";
        const string Catalog = Data + "/EnvironmentArt.asset";
        const string Existing = "Assets/ANIMOL/Data/Art/MemorySkyTheater/EnvironmentArt.asset";

        [Serializable] sealed class Settings { public SpriteSetting[] spriteSettings; }
        [Serializable] sealed class SpriteSetting
        {
            public string file;
            public int width, height;
            public float ppu;
            public float[] pivot;
        }

        [MenuItem("ANIMOL/Art QA/Apply Echo Theater V02 art")]
        public static string Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before applying Echo Theater art.");
            AssetDatabase.Refresh();
            var settingsFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "/sprite_import_settings.json");
            var settings = JsonUtility.FromJson<Settings>(settingsFile.text);
            if (settings.spriteSettings == null || settings.spriteSettings.Length != 1782)
                throw new InvalidOperationException("V02 requires all 1782 runtime sprite settings.");
            var entries = new List<EchoTheaterV02SpriteEntry>(settings.spriteSettings.Length);
            foreach (var row in settings.spriteSettings)
            {
                var path = Root + "/" + row.file;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer && (Mathf.Abs(importer.spritePixelsPerUnit - row.ppu) > .001f ||
                    importer.filterMode != FilterMode.Bilinear))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (!sprite) throw new InvalidOperationException("Missing V02 sprite: " + path);
                entries.Add(new EchoTheaterV02SpriteEntry { path = row.file, sprite = sprite });
            }
            var art = AssetDatabase.LoadAssetAtPath<EchoTheaterV02Art>(Catalog);
            if (!art)
            {
                art = ScriptableObject.CreateInstance<EchoTheaterV02Art>();
                AssetDatabase.CreateAsset(art, Catalog);
            }
            art.sprites = entries.ToArray();
            art.backgroundLayout = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "/background_scene_layout.json");
            if (!art.backgroundLayout) throw new InvalidOperationException("Missing V02 background layout.");
            EditorUtility.SetDirty(art);
            var existing = AssetDatabase.LoadAssetAtPath<MemorySkyTheaterArt>(Existing);
            if (!existing) throw new InvalidOperationException("Existing theater art catalog is missing.");
            existing.v02 = art;
            EditorUtility.SetDirty(existing);
            var bound = BindConfirmedStaticDevices(art);
            AssetDatabase.SaveAssets();
            return Validate() + "; " + bound + " existing static device states rebound";
        }

        static int BindConfirmedStaticDevices(EchoTheaterV02Art art)
        {
            // The package contains no direction sets or transition frames. Keep every
            // original sequence and every unmatched directional state intact.
            var matches = new Dictionary<string, Dictionary<string, string>>
            {
                ["d04_stage_shutter"] = new Dictionary<string, string> { ["a_horizontal_closed"] = "devices/alternating_shutter/closed.png" },
                ["d06_moving_stage"] = new Dictionary<string, string> { ["idle_e"] = "devices/moving_stage/idle.png" },
                ["d07_conveyor"] = new Dictionary<string, string> { ["idle_e"] = "devices/conveyor/idle.png" },
                ["d08_rotating_bookshelf"] = new Dictionary<string, string>
                {
                    ["horizontal"] = "devices/rotating_bookcase/front.png",
                    ["horizontal_reversed"] = "devices/rotating_bookcase/turned.png"
                },
                ["d09_one_way_gate"] = new Dictionary<string, string>
                {
                    ["closed_e"] = "devices/oneway_gate/closed.png",
                    ["open_e"] = "devices/oneway_gate/open.png"
                },
                ["d13_domino_wall"] = new Dictionary<string, string>
                {
                    ["standing_e"] = "devices/domino_wall/upright.png",
                    ["folded_e"] = "devices/domino_wall/fallen.png"
                },
                ["d17_charging_statue"] = new Dictionary<string, string> { ["idle_e"] = "devices/charging_statue/idle.png" },
                ["d18_bounce_corner"] = new Dictionary<string, string> { ["idle_ne"] = "devices/bounce_corner/idle.png" },
                ["d19_brake_pad"] = new Dictionary<string, string>
                {
                    ["idle_horizontal"] = "devices/brake_pad/idle.png",
                    ["pressed_horizontal"] = "devices/brake_pad/pressed.png"
                }
            };
            var count = 0;
            foreach (var device in matches)
            {
                var path = "Assets/ANIMOL/Prefabs/Themes/memory_theater/" + device.Key + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var view = root.GetComponent<DeviceVisualView>();
                    if (!view) throw new InvalidOperationException("Missing existing device visual: " + path);
                    var changed = false;
                    foreach (var state in view.states)
                    {
                        if (state.sequence || !device.Value.TryGetValue(state.name, out var image)) continue;
                        var sprite = art.Find(image);
                        if (!sprite) throw new InvalidOperationException("Missing device image: " + image);
                        state.sprite = sprite;
                        count++; changed = true;
                    }
                    if (!changed) continue;
                    view.ResetVisual();
                    EditorUtility.SetDirty(view);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return count;
        }

        [MenuItem("ANIMOL/Art QA/Validate Echo Theater V02 import")]
        public static string Validate()
        {
            var settings = JsonUtility.FromJson<Settings>(AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "/sprite_import_settings.json").text);
            var art = AssetDatabase.LoadAssetAtPath<EchoTheaterV02Art>(Catalog);
            if (!art || art.sprites == null || art.sprites.Length != 1782)
                throw new InvalidOperationException("V02 art catalog has an incomplete sprite table.");
            foreach (var row in settings.spriteSettings)
            {
                var path = Root + "/" + row.file;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var textureSettings = new TextureImporterSettings();
                if (importer) importer.ReadTextureSettings(textureSettings);
                if (!sprite || !importer || sprite.rect.width != row.width || sprite.rect.height != row.height ||
                    Mathf.Abs(sprite.pixelsPerUnit - row.ppu) > .001f ||
                    Vector2.Distance(sprite.pivot, new Vector2(row.pivot[0] * row.width, row.pivot[1] * row.height)) > .02f ||
                    importer.filterMode != FilterMode.Bilinear || importer.mipmapEnabled ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed ||
                    importer.spriteImportMode != SpriteImportMode.Single ||
                    importer.npotScale != TextureImporterNPOTScale.None ||
                    textureSettings.spriteMeshType != SpriteMeshType.FullRect)
                    throw new InvalidOperationException("V02 sprite import mismatch: " + path);
                if (art.Find(row.file) != sprite) throw new InvalidOperationException("V02 art reference missing: " + row.file);
            }
            var layout = JsonUtility.FromJson<EchoTheaterV02BackgroundLayout>(art.backgroundLayout.text);
            if (layout.layers == null || layout.layers.Length != 130 ||
                layout.layers.Count(l => l.file.Contains("/stars/")) != 120)
                throw new InvalidOperationException("V02 independent background counts are incorrect.");
            return "Validated 1782 native sprites, import settings and 131 background renderers";
        }
    }
}
