using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.StarwayMaps.Editor
{
    public static class StarwayPackCommands
    {
        public const string Root = "Assets/ANIMOL/StarwayMapPack";
        public const string DataRoot = Root + "/Editor/Data";
        public const string GeneratedRoot = Root + "/Editor/Generated";
        public const string CatalogPath = GeneratedRoot + "/Starway20Sources.asset";

        public static StarwayMapInput[] LoadAndValidateInputs()
        {
            StarwayPackIndex manifest = JsonUtility.FromJson<StarwayPackIndex>(ReadText(DataRoot + "/pack_index.json"));
            if (manifest == null || manifest.schema != "animol.unity-pack-index.v1" || manifest.version != 1 || manifest.mapCount != 20 || manifest.entries == null || manifest.entries.Length != 20)
                throw new InvalidOperationException("Invalid Starway pack index.");
            StarwayMapInput[] result = new StarwayMapInput[20];
            for (int i = 0; i < 20; i++)
            {
                string id = "SW" + (i + 1).ToString("00");
                StarwayPackEntry e = manifest.entries[i];
                if (e == null || e.id != id) throw new InvalidOperationException("Pack index identity/order mismatch: " + id);
                string inputPath = DataRoot + "/UnityInput/" + id + ".json";
                VerifyHash(inputPath, e.inputSha256);
                VerifyHash(DataRoot + "/DesignIR/" + id + ".json", e.sourceSha256);
                VerifyHash(DataRoot + "/Solutions/" + id + ".json", e.solutionSha256);
                VerifyHash(DataRoot + "/UnityReplay/" + id + ".json", e.replaySha256);
                result[i] = JsonUtility.FromJson<StarwayMapInput>(ReadText(inputPath));
                if (result[i] == null || result[i].sourceSha256 != e.sourceSha256 || result[i].solutionSha256 != e.solutionSha256)
                    throw new InvalidOperationException(id + ": provenance hash mismatch.");
            }
            StarwayInputValidator.ValidateAll(result);
            return result;
        }

        [MenuItem("ANIMOL/Starway 20/1 Import or Update Design Sources")]
        public static void ImportOrUpdateSources()
        {
            StarwayMapInput[] inputs = LoadAndValidateInputs(); // ALL checks before writes.
            for (int i = 0; i < 20; i++) CheckExistingType<StarwayMapSource>(GeneratedRoot + "/Maps/" + inputs[i].map.id + ".asset");
            CheckExistingType<StarwayMapCatalog>(CatalogPath);
            EnsureFolder(GeneratedRoot + "/Maps");
            StarwayMapSource[] maps = new StarwayMapSource[20];
            for (int i = 0; i < 20; i++)
            {
                string id = inputs[i].map.id;
                maps[i] = Upsert<StarwayMapSource>(GeneratedRoot + "/Maps/" + id + ".asset");
                maps[i].input = inputs[i];
                maps[i].originalDesign = AssetDatabase.LoadAssetAtPath<TextAsset>(DataRoot + "/DesignIR/" + id + ".json");
                maps[i].originalSolution = AssetDatabase.LoadAssetAtPath<TextAsset>(DataRoot + "/Solutions/" + id + ".json");
                maps[i].normalizedReplay = AssetDatabase.LoadAssetAtPath<TextAsset>(DataRoot + "/UnityReplay/" + id + ".json");
                if (maps[i].originalDesign == null || maps[i].originalSolution == null || maps[i].normalizedReplay == null) throw new InvalidOperationException(id + ": TextAsset import failed.");
                EditorUtility.SetDirty(maps[i]);
            }
            StarwayMapCatalog catalog = Upsert<StarwayMapCatalog>(CatalogPath);
            catalog.designVersion = 3; catalog.maps = maps; EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[Starway20] Imported 20 EDITOR design sources. Native game maps/campaign/camera are not changed by this command.");
        }

        [MenuItem("ANIMOL/Starway 20/2 Validate Design Sources")]
        public static void ValidateSources()
        {
            StarwayMapInput[] inputs = LoadAndValidateInputs();
            StarwayMapSource[] sources = RequireSources();
            for (int i = 0; i < 20; i++)
                if (JsonUtility.ToJson(inputs[i]) != JsonUtility.ToJson(sources[i].input))
                    throw new InvalidOperationException(inputs[i].map.id + ": staging asset changed; reimport the source pack.");
            Debug.Log("[Starway20] Source hashes, DTO fields, 20 maps, 10/5/5, sheep distance and 7x11 focus passed. Unity gameplay replay is a separate check.");
        }

        [MenuItem("ANIMOL/Starway 20/4 Build Native Maps with Project Adapter")]
        public static void BuildNativeMaps()
        {
            ValidateSources();
            StarwayMapSource[] sources = RequireSources();
            StarwayNativeAdapter adapter = RequireAdapter();
            ValidateOwnedRoots(adapter.OwnedAssetRoots);
            List<string> errors = new List<string>(); adapter.Preflight(sources, errors); ThrowErrors(errors);
            adapter.BuildOrUpdateMaps(sources);
            AssetDatabase.SaveAssets();
            StarwayNativeLink[] links = VerifyNativeReadback(adapter, sources);
            adapter.RegisterCampaign(links);
            errors.Clear(); adapter.ValidateCampaign(links, errors); ThrowErrors(errors);
            AssetDatabase.SaveAssets();
            WriteNativeReport(adapter, links);
            Debug.Log("[Starway20] Native assets and campaign readback passed with " + adapter.DisplayName + ". Runtime replays/Undo/ad callbacks still require the project's tests.");
        }

        [MenuItem("ANIMOL/Starway 20/5 Validate Native Maps with Project Adapter")]
        public static void ValidateNativeMaps()
        {
            ValidateSources();
            StarwayNativeAdapter adapter = RequireAdapter();
            ValidateOwnedRoots(adapter.OwnedAssetRoots);
            StarwayMapSource[] sources = RequireSources();
            List<string> errors = new List<string>(); adapter.Preflight(sources, errors); ThrowErrors(errors);
            StarwayNativeLink[] links = VerifyNativeReadback(adapter, sources);
            adapter.ValidateCampaign(links, errors); ThrowErrors(errors);
            WriteNativeReport(adapter, links);
            Debug.Log("[Starway20] Native asset/campaign readback passed. This is not a gameplay replay result.");
        }

        public static StarwayMapSource[] RequireSources()
        {
            StarwayMapCatalog c = AssetDatabase.LoadAssetAtPath<StarwayMapCatalog>(CatalogPath);
            if (c == null || c.designVersion != 3 || c.maps == null || c.maps.Length != 20)
                throw new InvalidOperationException("Run ANIMOL > Starway 20 > 1 Import or Update Design Sources first.");
            for (int i = 0; i < 20; i++)
                if (c.maps[i] == null || c.maps[i].input == null || c.maps[i].input.map.id != "SW" + (i + 1).ToString("00"))
                    throw new InvalidOperationException("Missing or out-of-order design source.");
            return c.maps;
        }

        private static StarwayNativeAdapter RequireAdapter()
        {
            List<Type> found = new List<Type>();
            foreach (Type t in TypeCache.GetTypesDerivedFrom<StarwayNativeAdapter>())
                if (!t.IsAbstract && !t.ContainsGenericParameters && t.GetConstructor(Type.EmptyTypes) != null) found.Add(t);
            if (found.Count != 1) throw new InvalidOperationException("Expected exactly one project-specific StarwayNativeAdapter, found " + found.Count +
                ". Follow APPLY_TO_UNITY_KO.txt to implement the adapter against the actual PuzzleMap/PuzzleScenario source. No native maps have been written by this check.");
            return (StarwayNativeAdapter)Activator.CreateInstance(found[0]);
        }

        private static StarwayNativeLink[] VerifyNativeReadback(StarwayNativeAdapter adapter, StarwayMapSource[] sources)
        {
            StarwayNativeLink[] links = adapter.ReadNativeLinks(sources);
            if (links == null || links.Length != 20) throw new InvalidOperationException("Adapter must return 20 native maps.");
            string[] roots = adapter.OwnedAssetRoots;
            ValidateOwnedRoots(roots);
            HashSet<int> maps = new HashSet<int>(), scenarios = new HashSet<int>();
            List<string> errors = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                StarwayNativeLink link = links[i];
                if (link == null || link.id != sources[i].input.map.id || link.map == null || link.scenario == null)
                    throw new InvalidOperationException("Missing/wrong native map or scenario for index " + i);
                if (!maps.Add(link.map.GetInstanceID()) || !scenarios.Add(link.scenario.GetInstanceID()))
                    throw new InvalidOperationException("Maps/scenarios must be distinct per stage.");
                CheckOwnedAsset(link.map, roots); CheckOwnedAsset(link.scenario, roots);
                if (link.campaignStage != null) CheckOwnedAsset(link.campaignStage, roots);
                adapter.ValidateNativeMap(sources[i], link, errors);
            }
            ThrowErrors(errors); return links;
        }

        private static void ValidateOwnedRoots(string[] roots)
        {
            if (roots == null || roots.Length == 0) throw new InvalidOperationException("Adapter must declare scoped native output roots.");
            foreach (string root in roots)
                if (string.IsNullOrEmpty(root) || !root.StartsWith("Assets/", StringComparison.Ordinal) || root.Contains("..") || root.EndsWith("/", StringComparison.Ordinal) || root.Split('/').Length < 4)
                    throw new InvalidOperationException("Declare a dedicated Starway20 output subtree, not a broad project root.");
        }

        private static void CheckOwnedAsset(UnityEngine.Object asset, string[] roots)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            foreach (string root in roots) if (path.StartsWith(root + "/", StringComparison.Ordinal)) return;
            throw new InvalidOperationException("Native output outside the adapter's owned subtree: " + path);
        }

        private static void ThrowErrors(List<string> errors)
        { if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors.ToArray())); }

        private static T Upsert<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            else Undo.RecordObject(asset, "Update Starway design source");
            return asset;
        }

        private static void CheckExistingType<T>(string path) where T : UnityEngine.Object
        {
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            if ((existing != null && !(existing is T)) || (existing == null && File.Exists(FullPath(path))))
                throw new InvalidOperationException("Refusing to replace an unrelated/unreadable asset at " + path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        private static string FullPath(string assetPath)
        { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath); }
        private static string ReadText(string assetPath) { return File.ReadAllText(FullPath(assetPath)); }
        private static void VerifyHash(string assetPath, string expected)
        {
            using (SHA256 sha = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(FullPath(assetPath)))).Replace("-", "").ToLowerInvariant();
                if (actual != expected) throw new InvalidOperationException("Source file hash changed: " + assetPath);
            }
        }

        [Serializable] private sealed class NativeReport
        {
            public string adapter;
            public int mapCount;
            public bool nativeDataReadbackPassed;
            public bool unityGameplayReplayVerified;
            public string note;
            public string[] maps;
        }
        private static void WriteNativeReport(StarwayNativeAdapter adapter, StarwayNativeLink[] links)
        {
            string[] paths = new string[links.Length];
            for (int i = 0; i < links.Length; i++) paths[i] = AssetDatabase.GetAssetPath(links[i].map);
            NativeReport report = new NativeReport { adapter = adapter.DisplayName, mapCount = 20, nativeDataReadbackPassed = true,
                unityGameplayReplayVerified = false, note = "Asset/campaign readback only. Run actual PuzzleDeviceSession replay tests separately.", maps = paths };
            string logs = FullPath("Logs"); Directory.CreateDirectory(logs);
            File.WriteAllText(Path.Combine(logs, "starway20-native-import.json"), JsonUtility.ToJson(report, true));
        }
    }
}
