using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ANIMOL.NamedArt.Editor
{
    // Names are the durable identity. GUID/file IDs are only caches and owner locators.
    [Serializable] public sealed class ArtSource
    {
        public string Key, Path, Guid, Name, Kind;
    }
    [Serializable] public sealed class ArtImporter
    {
        public string Path, Guid, FileName;
        public JObject Settings;
    }
    [Serializable] public sealed class ArtSlot
    {
        public string Owner, Property, Key;
    }
    [Serializable] public sealed class ArtContainer
    {
        public string Path, Guid;
        public List<ArtSlot> Slots = new List<ArtSlot>();
    }
    [Serializable] public sealed class NamedArtIndex
    {
        public int Version = 1;
        public string Root = "Assets/ANIMOL/";
        public List<ArtSource> Sources = new List<ArtSource>();
        public List<ArtImporter> Importers = new List<ArtImporter>();
        public List<ArtContainer> Containers = new List<ArtContainer>();

        public static NamedArtIndex Read(string path) => File.Exists(path)
            ? JsonConvert.DeserializeObject<NamedArtIndex>(File.ReadAllText(path)) : new NamedArtIndex();
        public void Write(string path)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            string json = JsonConvert.SerializeObject(this, Formatting.Indented) + "\n";
            if (!File.Exists(path) || File.ReadAllText(path) != json) File.WriteAllText(path, json);
        }
        public bool InScope(string path) => path.StartsWith(Root, StringComparison.Ordinal);
        public string Register(Object value)
        {
            if (!(value is Sprite) && !(value is Texture2D)) return null;
            string path = AssetDatabase.GetAssetPath(value);
            if (!InScope(path)) return null; // Built-in white textures and generated preview pixels are not replaceable files.
            string kind = value is Sprite ? "Sprite" : "Texture2D";
            var source = Sources.FirstOrDefault(s => s.Path == path && s.Name == value.name && s.Kind == kind);
            if (source == null)
            {
                // Directory namespaces prevent existing button_play/capsule_currency duplicates colliding.
                source = new ArtSource { Key = path.Substring(Root.Length) + "::" + kind + "/" + value.name,
                    Path = path, Guid = AssetDatabase.AssetPathToGUID(path), Name = value.name, Kind = kind };
                Sources.Add(source);
            }
            return source.Key;
        }
        public void CaptureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || !InScope(path)) return;
            var record = Importers.FirstOrDefault(s => s.Path == path);
            if (record == null) { record = new ArtImporter(); Importers.Add(record); }
            record.Path = path; record.Guid = AssetDatabase.AssetPathToGUID(path); record.FileName = System.IO.Path.GetFileName(path);
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            var native = JObject.Parse(EditorJsonUtility.ToJson(importer));
            record.Settings = new JObject {
                ["TextureSettings"] = JObject.Parse(JsonUtility.ToJson(settings)),
                ["Platforms"] = new JArray(new[] { "DefaultTexturePlatform", "Standalone", "Android", "iPhone", "WebGL" }
                    .Select(n => JObject.Parse(JsonUtility.ToJson(importer.GetPlatformTextureSettings(n))))),
                ["SpriteSheet"] = native["m_SpriteSheet"],
                ["UserData"] = importer.userData,
                ["BundleName"] = importer.assetBundleName,
                ["BundleVariant"] = importer.assetBundleVariant
            };
        }
        public ArtImporter SettingsFor(string path)
        {
            var exact = Importers.FirstOrDefault(i => i.Path == path);
            if (exact != null) return exact;
            var sameName = Importers.Where(i => i.FileName == System.IO.Path.GetFileName(path) && !File.Exists(i.Path)).ToArray();
            return sameName.Length == 1 ? sameName[0] : null;
        }
        public bool RestoreImporter(TextureImporter importer)
        {
            var record = SettingsFor(importer.assetPath);
            if (record == null || (!importer.importSettingsMissing && record.Guid == AssetDatabase.AssetPathToGUID(importer.assetPath))) return false;
            var settings = new TextureImporterSettings();
            JsonUtility.FromJsonOverwrite(record.Settings["TextureSettings"].ToString(Formatting.None), settings);
            importer.SetTextureSettings(settings);
            foreach (var token in record.Settings["Platforms"])
            {
                var platform = new TextureImporterPlatformSettings(); JsonUtility.FromJsonOverwrite(token.ToString(Formatting.None), platform);
                importer.SetPlatformTextureSettings(platform);
            }
            using (var so = new SerializedObject(importer))
            {
                ApplySheet(so.FindProperty("m_SpriteSheet"), record.Settings["SpriteSheet"]);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            importer.userData = (string)record.Settings["UserData"];
            importer.SetAssetBundleNameAndVariant((string)record.Settings["BundleName"], (string)record.Settings["BundleVariant"]);
            return true;
        }
        // Sprite Editor is not installed in this project. Use Unity's serialized importer API
        // for its verified sheet fields, including custom outlines/physics shapes and named slices.
        private static void ApplySheet(SerializedProperty p, JToken value)
        {
            if (p == null || value == null || value.Type == JTokenType.Null) return;
            if (value is JArray array)
            {
                p.arraySize = array.Count;
                for (int i = 0; i < array.Count; i++) ApplySheet(p.GetArrayElementAtIndex(i), array[i]);
                return;
            }
            float V(string n) => (float?)value[n] ?? 0;
            switch (p.propertyType)
            {
                case SerializedPropertyType.String: p.stringValue = (string)value; return;
                case SerializedPropertyType.Integer: case SerializedPropertyType.Enum: p.longValue = (long)value; return;
                case SerializedPropertyType.Boolean: p.boolValue = (bool)value; return;
                case SerializedPropertyType.Float: p.doubleValue = (double)value; return;
                case SerializedPropertyType.Vector2: p.vector2Value = new Vector2(V("x"), V("y")); return;
                case SerializedPropertyType.Vector3: p.vector3Value = new Vector3(V("x"), V("y"), V("z")); return;
                case SerializedPropertyType.Vector4: p.vector4Value = new Vector4(V("x"), V("y"), V("z"), V("w")); return;
                case SerializedPropertyType.Quaternion: p.quaternionValue = new Quaternion(V("x"), V("y"), V("z"), V("w")); return;
                case SerializedPropertyType.Rect: p.rectValue = new Rect(V("x"), V("y"), V("width"), V("height")); return;
            }
            if (value is JObject obj) foreach (var field in obj.Properties()) ApplySheet(p.FindPropertyRelative(field.Name), field.Value);
        }
        public Object Resolve(ArtSource source, string[] candidates, out string reason)
        {
            reason = null;
            // Same path is deterministic even where filenames are duplicated across art packages.
            string path = File.Exists(source.Path) ? source.Path : AssetDatabase.GUIDToAssetPath(source.Guid);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                var matches = candidates.Where(p => System.IO.Path.GetFileName(p) == System.IO.Path.GetFileName(source.Path)).ToArray();
                if (matches.Length != 1) { reason = matches.Length == 0 ? "Missing file" : "Ambiguous filename"; return null; }
                path = matches[0];
            }
            var objects = AssetDatabase.LoadAllAssetsAtPath(path).Where(o => o != null &&
                (source.Kind == "Sprite" ? o is Sprite : o is Texture2D) && o.name == source.Name).ToArray();
            if (objects.Length != 1) { reason = "Missing or ambiguous named subasset"; return null; }
            return objects[0];
        }
    }
}
