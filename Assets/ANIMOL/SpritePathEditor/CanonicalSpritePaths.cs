using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.SpritePathEditor
{
    // Source-package names remain import keys; this table resolves their canonical assets.
    public static class CanonicalSpritePaths
    {
        [Serializable] private sealed class Entry { public string source; public string destination; }
        [Serializable] private sealed class Manifest { public Entry[] sprites; }
        private static Dictionary<string, string> paths;
        public static string Resolve(string path)
        {
            if (paths == null)
            {
                paths = new Dictionary<string, string>(StringComparer.Ordinal);
                var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/ANIMOL/Sprites/sprite_path_manifest.json"));
                foreach (var entry in manifest.sprites) paths.Add(entry.source, entry.destination);
            }
            if (paths.TryGetValue(path, out var canonical)) return canonical;
            // A few legacy builders search by package + basename rather than subdirectory.
            int marker = path.IndexOf("/Sprites/", StringComparison.Ordinal);
            if (marker >= 0)
            {
                string package = path.Substring(0, marker + 9);
                string name = Path.GetFileName(path);
                foreach (var entry in paths)
                    if (entry.Key.StartsWith(package, StringComparison.Ordinal) && Path.GetFileName(entry.Key) == name)
                        return entry.Value;
            }
            return path;
        }
    }
}
