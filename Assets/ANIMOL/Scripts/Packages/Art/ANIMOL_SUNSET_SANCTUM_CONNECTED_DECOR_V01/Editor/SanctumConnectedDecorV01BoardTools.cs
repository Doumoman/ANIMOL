#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    /// <summary>Explicit, package-scoped board importer and actual example prefab/scene builder.</summary>
    public static class SanctumConnectedDecorV01BoardTools
    {
        public const string OwnerLabel = "ANIMOL_SANCTUM_CONNECTED_DECOR_V01_EXAMPLE";
        [Serializable] private sealed class BuildReport
        {
            public int installationRevision = 1;
            public bool exampleBoardAssetsSaved, examplePrefabsSaved, exampleScenesSaved;
            public bool actualProductionBindingsVerified = false, actualPlayModeVerified = false, productionScenesModified = false;
            public string manifest, manifestSha256, sortingLayer, stageSnapshotJson, all26SnapshotJson;
            public string[] generatedAssets, generatedGuids;
            public string[] inspectionLighting;
            public int stageNetworkCells, stagePlacements, all26NetworkCells, all26Placements;
            public string scope = "Actual Editor build-time rendering validation only when this command is executed. Inspection examples are not migrated campaign/editor/load hooks.";
        }
        private sealed class Example
        {
            public string key, jsonPath, directory, boardPath, prefabPath, scenePath;
            public SanctumConnectedDecorV01BoardSnapshot snapshot;
        }

        public static void BuildExamplesFromCommandLine()
        {
            var flags = new Dictionary<string, string>(StringComparer.Ordinal);
            var accepted = new HashSet<string>(new[] { "-sanctumDecorManifest", "-sanctumDecorStageSnapshot", "-sanctumDecorAllSnapshot", "-sanctumDecorSortingLayer", "-sanctumDecorCreateScenes" }, StringComparer.Ordinal);
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (!accepted.Contains(args[i])) continue;
                string flag = args[i];
                Require(!flags.ContainsKey(flag) && i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal), "Missing/duplicate " + flag);
                flags.Add(flag, args[++i]);
            }
            foreach (string flag in new[] { "-sanctumDecorManifest", "-sanctumDecorStageSnapshot", "-sanctumDecorAllSnapshot", "-sanctumDecorSortingLayer" })
                Require(flags.ContainsKey(flag) && !string.IsNullOrWhiteSpace(flags[flag]), "Required " + flag);
            bool createScenes = false;
            if (flags.ContainsKey("-sanctumDecorCreateScenes"))
                Require(bool.TryParse(flags["-sanctumDecorCreateScenes"], out createScenes), "-sanctumDecorCreateScenes must be true or false.");
            BuildExamples(flags["-sanctumDecorManifest"], flags["-sanctumDecorStageSnapshot"], flags["-sanctumDecorAllSnapshot"], flags["-sanctumDecorSortingLayer"], createScenes);
        }

        public static void BuildExamples(string manifestPath, string stageSnapshotPath, string all26SnapshotPath, string sortingLayer, bool createScenes)
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling, "Build inspection boards outside Play Mode and compilation.");
            Require(PrefabStageUtility.GetCurrentPrefabStage() == null,
                "Save/leave the open Prefab Stage before building inspection boards; the builder will not change or close the user's stage.");
            string root = PackageRoot(manifestPath);
            string pin; SanctumConnectedDecorV01CatalogBuilder.LoadManifest(root, out pin);
            RequireSortingLayer(sortingLayer);
            var stage = ReadExample(root, "Stage", stageSnapshotPath, pin);
            var all = ReadExample(root, "All26", all26SnapshotPath, pin);
            Require(stage.snapshot.boardId != all.snapshot.boardId, "Stage and all26 examples require distinct boardId values.");
            ValidateAll26(all.snapshot);
            string ownerRootLabel = "sanctum_root_" + AssetDatabase.AssetPathToGUID(root);
            Require(ownerRootLabel.Length == "sanctum_root_".Length + 32, "Current package root GUID is unavailable.");
            foreach (var example in new[] { stage, all }) PreflightOutputs(example, ownerRootLabel, createScenes);
            string reportPath = root + "/_Generated/InspectionBoards/inspection_build_report.json";
            RequireOwnedOrAbsent(reportPath, typeof(TextAsset), ownerRootLabel);

            // All JSON, claims, source pins, output ownership and layer names pass before modifying assets.
            var catalog = SanctumConnectedDecorV01CatalogBuilder.BuildFromManifest(root + "/manifest.json");
            SanctumConnectedDecorV01CatalogBuilder.ValidateBuiltCatalog(catalog);
            var lighting = new List<string>();
            foreach (var example in new[] { stage, all })
            {
                EnsureFolder(root + "/_Generated/InspectionBoards"); EnsureFolder(example.directory);
                var board = SaveBoard(example, ownerRootLabel);
                lighting.Add(BuildRenderableExample(example, board, catalog, pin, sortingLayer, ownerRootLabel, createScenes));
            }
            var paths = new List<string>();
            foreach (var example in new[] { stage, all })
            { paths.Add(example.boardPath); paths.Add(example.prefabPath); if (createScenes) paths.Add(example.scenePath); }
            var guids = new List<string>();
            foreach (string path in paths)
            { string guid = AssetDatabase.AssetPathToGUID(path); Require(SanctumConnectedDecorV01Catalog.IsHash(guid, 32), "Generated asset GUID unavailable: " + path); guids.Add(guid); }
            var report = new BuildReport { manifest = root + "/manifest.json", manifestSha256 = pin, sortingLayer = sortingLayer,
                stageSnapshotJson = stage.jsonPath, all26SnapshotJson = all.jsonPath, exampleBoardAssetsSaved = true,
                examplePrefabsSaved = true, exampleScenesSaved = createScenes, generatedAssets = paths.ToArray(), generatedGuids = guids.ToArray(), inspectionLighting = lighting.ToArray(),
                stageNetworkCells = stage.snapshot.networkCells.Length, stagePlacements = stage.snapshot.groupPlacements.Length,
                all26NetworkCells = all.snapshot.networkCells.Length, all26Placements = all.snapshot.groupPlacements.Length };
            File.WriteAllText(FullPath(reportPath), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(reportPath, ImportAssetOptions.ForceSynchronousImport);
            SetOwnedLabels(AssetDatabase.LoadMainAssetAtPath(reportPath), ownerRootLabel);
            Debug.Log("[Sanctum Connected Decor V01] Two typed boards and working example prefabs generated. Inspection scenes: " + createScenes + ". Actual production binding/Play Mode verification remains NOT_RUN.");
        }

        private static Example ReadExample(string root, string key, string jsonPath, string pin)
        {
            jsonPath = AssetPath(jsonPath);
            Require(jsonPath.StartsWith(root + "/", StringComparison.Ordinal) && jsonPath.EndsWith(".json", StringComparison.Ordinal), "Example JSON must be inside this exact package.");
            string json = File.ReadAllText(FullPath(jsonPath), new UTF8Encoding(false, true));
            var snapshot = SanctumConnectedDecorV01BoardJson.Parse(json);
            Require(snapshot.isExampleOnly && snapshot.isWholeBoard, "Inspection builder accepts whole-board, example-only snapshots.");
            Require(string.Equals(snapshot.sourceManifestSha256, pin, StringComparison.OrdinalIgnoreCase), "Example references a different art manifest pin.");
            Require(snapshot.networkCells.Length + snapshot.groupPlacements.Length > 0, "Inspection examples must contain actual decoration placements.");
            string directory = root + "/_Generated/InspectionBoards/" + key;
            return new Example { key = key, jsonPath = jsonPath, snapshot = snapshot, directory = directory,
                boardPath = directory + "/SanctumDecor" + key + "Board.asset", prefabPath = directory + "/SanctumDecor" + key + ".prefab",
                scenePath = directory + "/SanctumDecor" + key + ".unity" };
        }
        private static void ValidateAll26(SanctumConnectedDecorV01BoardSnapshot snapshot)
        {
            var networks = new HashSet<string>(StringComparer.Ordinal); var groups = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in snapshot.networkCells) networks.Add(row.family);
            foreach (var row in snapshot.groupPlacements) groups.Add(row.groupKey);
            foreach (string family in SanctumConnectedDecorV01Planner.NetworkFamilies) Require(networks.Contains(family), "All26 board is missing network " + family);
            foreach (string key in SanctumConnectedDecorV01Planner.GroupKeys) Require(groups.Contains(key), "All26 board is missing prop " + key);
            Require(networks.Count == 6 && groups.Count == 20, "All26 board semantic inventory differs.");
        }
        private static void PreflightOutputs(Example example, string rootLabel, bool scenes)
        {
            PreflightFolder(example.directory);
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            Require(prefabStage == null || !string.Equals(prefabStage.assetPath, example.prefabPath, StringComparison.OrdinalIgnoreCase),
                "Close/save the generated inspection prefab's open Prefab Stage before rebuilding it: " + example.prefabPath);
            if (scenes)
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    Require(!string.Equals(SceneManager.GetSceneAt(i).path, example.scenePath, StringComparison.OrdinalIgnoreCase),
                        "Close/save the loaded generated inspection scene before rebuilding it: " + example.scenePath);
            RequireOwnedOrAbsent(example.boardPath, typeof(SanctumConnectedDecorV01BoardAsset), rootLabel);
            RequireOwnedOrAbsent(example.prefabPath, typeof(GameObject), rootLabel);
            if (scenes) RequireOwnedOrAbsent(example.scenePath, typeof(SceneAsset), rootLabel);
        }
        private static SanctumConnectedDecorV01BoardAsset SaveBoard(Example example, string rootLabel)
        {
            var output = AssetDatabase.LoadAssetAtPath<SanctumConnectedDecorV01BoardAsset>(example.boardPath);
            bool isNew = output == null;
            if (isNew) output = ScriptableObject.CreateInstance<SanctumConnectedDecorV01BoardAsset>();
            string error;
            if (!output.TrySetSnapshot(example.snapshot, out error))
            { if (isNew) UnityEngine.Object.DestroyImmediate(output); throw new InvalidDataException(error); }
            if (isNew) AssetDatabase.CreateAsset(output, example.boardPath);
            SetOwnedLabels(output, rootLabel); EditorUtility.SetDirty(output); AssetDatabase.SaveAssetIfDirty(output);
            return output;
        }

        private static string BuildRenderableExample(Example example, SanctumConnectedDecorV01BoardAsset board,
            SanctumConnectedDecorV01Catalog catalog, string pin, string sortingLayer, string ownerRootLabel, bool saveScene)
        {
            Scene previous = SceneManager.GetActiveScene(); Scene inspection = default(Scene);
            string lighting = "Scenes not requested; no camera or light created.";
            try
            {
                inspection = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var root = new GameObject("SANCTUM_DECOR_INSPECTION_ONLY__" + example.key); root.SetActive(false);
                SceneManager.MoveGameObjectToScene(root, inspection);
                Require(root.scene == inspection, "Could not move inspection root to isolated scene.");
                var controller = root.AddComponent<SanctumConnectedDecorV01Controller>();
                var source = root.AddComponent<SanctumConnectedDecorV01AuthoredSnapshotSource>();
                var binding = root.AddComponent<SanctumConnectedDecorV01Binding>();
                string error;
                Require(source.AssignBoard(board, out error), error);
                Require(binding.Bind(controller, catalog, pin, sortingLayer, source, out error, true), error);
                root.SetActive(true);
                // Edit-mode construction cannot rely on OnEnable. This is a real concrete caller.
                Require(binding.RefreshNow(out error), error);
                Require(controller.CurrentPlan != null && controller.CurrentPlan.NetworkCount == example.snapshot.networkCells.Length
                    && controller.CurrentPlan.GroupCount == example.snapshot.groupPlacements.Length && controller.OwnedVisualCount > 0,
                    "Inspection binding did not construct the complete current visual plan.");
                SpriteRenderer[] rendered = root.GetComponentsInChildren<SpriteRenderer>(true);
                Require(rendered.Length == controller.OwnedVisualCount, "Unexpected/missing inspection renderers.");
                foreach (var renderer in rendered)
                    Require(renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sprite != null,
                        "Inspection renderer was created but is not visibly enabled with its current Sprite.");
                bool success;
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, example.prefabPath, out success);
                Require(success && prefab != null, "Could not save the generated inspection prefab."); SetOwnedLabels(prefab, ownerRootLabel);
                if (saveScene)
                {
                    FrameInspectionCamera(root, inspection);
                    lighting = CreateInspectionLight(sortingLayer, inspection);
                    Require(EditorSceneManager.SaveScene(inspection, example.scenePath), "Could not save the generated inspection scene.");
                    AssetDatabase.ImportAsset(example.scenePath, ImportAssetOptions.ForceSynchronousImport);
                    SetOwnedLabels(AssetDatabase.LoadAssetAtPath<SceneAsset>(example.scenePath), ownerRootLabel);
                }
                return lighting;
            }
            finally
            {
                // Never save, close or restore/reopen any pre-existing user scene (including unsaved scenes).
                if (previous.IsValid() && previous.isLoaded && SceneManager.GetActiveScene() != previous)
                    SceneManager.SetActiveScene(previous);
                if (inspection.IsValid() && inspection.isLoaded) EditorSceneManager.CloseScene(inspection, true);
            }
        }
        private static string CreateInspectionLight(string sortingLayer, Scene inspection)
        {
            // Optional URP dependency: no URP assembly reference or project/pipeline mutation.
            Type type = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            { type = assembly.GetType("UnityEngine.Rendering.Universal.Light2D", false); if (type != null) break; }
            if (type == null) return "URP Light2D type unavailable: no light created; confirm the host sprite material/pipeline in Play Mode.";
            PropertyInfo lightType = type.GetProperty("lightType"), intensity = type.GetProperty("intensity"), color = type.GetProperty("color");
            if (lightType == null || !lightType.CanWrite || !lightType.PropertyType.IsEnum || !Enum.IsDefined(lightType.PropertyType, "Global")
                || intensity == null || !intensity.CanWrite || intensity.PropertyType != typeof(float)
                || color == null || !color.CanWrite || color.PropertyType != typeof(Color))
            {
                Debug.LogWarning("[Sanctum Decor V01] Current URP Light2D API differs. No optional inspection light created; verify lighting on the host.");
                return "URP Light2D API unavailable/changed: no light created; actual host lighting verification required.";
            }
            GameObject lightObject = null;
            try
            {
                lightObject = new GameObject("SANCTUM_INSPECTION_ONLY_GLOBAL_LIGHT");
                SceneManager.MoveGameObjectToScene(lightObject, inspection);
                Component light = lightObject.AddComponent(type);
                lightType.SetValue(light, Enum.Parse(lightType.PropertyType, "Global"), null);
                intensity.SetValue(light, 1f, null); color.SetValue(light, Color.white, null);
                // Inspect the actual serialized URP component. A missing field is diagnosed, never silently claimed configured.
                var serialized = new SerializedObject(light); SerializedProperty layers = serialized.FindProperty("m_ApplyToSortingLayers");
                if (layers != null && layers.isArray)
                {
                    layers.arraySize = 1; layers.GetArrayElementAtIndex(0).intValue = SortingLayer.NameToID(sortingLayer);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    return "Inspection-only Global Light2D created; actual serialized target sorting layer set to " + sortingLayer + ". GPU/Play Mode not verified by this builder.";
                }
                Debug.LogWarning("[Sanctum Decor V01] Inspection Global Light2D created, but this URP version exposes no known target-layer field. Select the actual decoration Sorting Layer on the light and verify Play Mode.", light);
                return "Inspection-only Global Light2D created; target sorting-layer field unavailable, manual host check required.";
            }
            catch (Exception error)
            {
                if (lightObject != null) UnityEngine.Object.DestroyImmediate(lightObject);
                Debug.LogWarning("[Sanctum Decor V01] Optional inspection lighting not configured: " + error.Message);
                return "Optional inspection light failed: " + error.Message + "; host lighting check required.";
            }
        }
        private static void FrameInspectionCamera(GameObject root, Scene inspection)
        {
            SpriteRenderer[] sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
            Require(sprites.Length > 0, "No inspection sprites to frame.");
            Bounds bounds = sprites[0].bounds;
            foreach (var sprite in sprites) bounds.Encapsulate(sprite.bounds);
            var cameraObject = new GameObject("SANCTUM_INSPECTION_CAMERA__UNTAGGED"); var camera = cameraObject.AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(cameraObject, inspection);
            camera.orthographic = true; camera.aspect = 1080f / 1920f;
            camera.orthographicSize = Mathf.Max(bounds.extents.y + .75f, (bounds.extents.x + .75f) / camera.aspect);
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 10f);
            camera.transform.rotation = Quaternion.identity; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.10f, .07f, .17f, 1f); camera.nearClipPlane = .01f; camera.farClipPlane = Mathf.Max(100f, bounds.size.z + 20f);
        }

        [MenuItem("Tools/ANIMOL/Sanctum Connected Decor V01/Refresh selected binding")]
        public static void RefreshSelectedBinding()
        {
            var binding = SelectedBinding(); string error;
            if (!TryRefreshBinding(binding, out error)) Debug.LogError("[Sanctum Decor V01] " + error, binding);
        }
        [MenuItem("Tools/ANIMOL/Sanctum Connected Decor V01/Clear selected binding visuals")]
        public static void ClearSelectedBinding()
        {
            var binding = SelectedBinding(); RequireSceneBinding(binding); binding.ClearVisuals();
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(binding.gameObject.scene);
        }
        [MenuItem("Tools/ANIMOL/Sanctum Connected Decor V01/Validate selected board asset")]
        public static void ValidateSelectedBoard()
        {
            var board = Selection.activeObject as SanctumConnectedDecorV01BoardAsset;
            Require(board != null, "Select a SanctumConnectedDecorV01BoardAsset.");
            SanctumConnectedDecorV01BoardSnapshot snapshot; string error;
            Require(board.TryGetSnapshot(out snapshot, out error), error);
            Debug.Log("[Sanctum Decor V01] Typed board and complete claims valid: " + snapshot.boardId + ". Current catalog and real game bindings require separate validation.", board);
        }
        public static bool TryRefreshBinding(SanctumConnectedDecorV01Binding binding, out string error)
        {
            error = null;
            try
            {
                RequireSceneBinding(binding);
                SanctumConnectedDecorV01CatalogBuilder.ValidateBuiltCatalog(binding.Catalog);
                if (!binding.RefreshNow(out error)) return false;
                if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(binding.gameObject.scene);
                SceneView.RepaintAll(); return true;
            }
            catch (Exception e)
            {
                if (binding != null && !EditorUtility.IsPersistent(binding) && binding.gameObject.scene.IsValid()) binding.ClearVisuals();
                error = e.Message; return false;
            }
        }
        private static SanctumConnectedDecorV01Binding SelectedBinding()
        { return Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponent<SanctumConnectedDecorV01Binding>(); }
        private static void RequireSceneBinding(SanctumConnectedDecorV01Binding binding)
        {
            Require(binding != null && !EditorUtility.IsPersistent(binding) && binding.gameObject.scene.IsValid() && binding.gameObject.scene.isLoaded,
                "Select a loaded scene/Prefab Stage binding, not a persistent prefab asset. Open the prefab first.");
        }
        private static void RequireOwnedOrAbsent(string path, Type expectedType, string rootLabel)
        {
            var prior = AssetDatabase.LoadMainAssetAtPath(path);
            if (prior == null)
            { Require(!File.Exists(FullPath(path)) && !Directory.Exists(FullPath(path)), "Refusing to overwrite an unimported/unrelated path: " + path); return; }
            var labels = new HashSet<string>(AssetDatabase.GetLabels(prior), StringComparer.Ordinal);
            Require(expectedType.IsInstanceOfType(prior) && labels.Contains(OwnerLabel) && labels.Contains(rootLabel),
                "Refusing to overwrite an unrelated asset: " + path);
            Require(!EditorUtility.IsDirty(prior), "Save the generated asset's unsaved changes before rebuilding it: " + path);
        }
        private static void SetOwnedLabels(UnityEngine.Object asset, string rootLabel)
        {
            Require(asset != null, "Generated asset import failed.");
            var labels = new HashSet<string>(AssetDatabase.GetLabels(asset), StringComparer.Ordinal) { OwnerLabel, rootLabel };
            var output = new string[labels.Count]; labels.CopyTo(output); Array.Sort(output, StringComparer.Ordinal); AssetDatabase.SetLabels(asset, output);
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Require(!File.Exists(FullPath(path)) && !Directory.Exists(FullPath(path)), "Generated folder occupied by an unrelated path: " + path);
            int slash = path.LastIndexOf('/'); Require(slash > 0, "Invalid generated folder.");
            EnsureFolder(path.Substring(0, slash));
            Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1))), "Generated folder creation failed.");
        }
        private static void PreflightFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Require(!File.Exists(FullPath(path)) && !Directory.Exists(FullPath(path)), "Generated folder occupied by an unrelated/unimported path: " + path);
            int slash = path.LastIndexOf('/'); Require(slash > 0, "Invalid generated folder path.");
            PreflightFolder(path.Substring(0, slash));
        }
        public static string PackageRoot(string manifestPath)
        {
            manifestPath = AssetPath(manifestPath);
            Require(manifestPath.EndsWith("/" + SanctumConnectedDecorV01Catalog.PackageId + "/manifest.json", StringComparison.Ordinal), "Select the current exact package manifest.json.");
            string root = manifestPath.Substring(0, manifestPath.Length - "/manifest.json".Length);
            Require(AssetDatabase.IsValidFolder(root), "Current package folder is not imported."); return root;
        }
        private static string AssetPath(string path)
        {
            Require(!string.IsNullOrWhiteSpace(path), "Missing Assets path."); path = path.Replace('\\', '/');
            Require(path.StartsWith("Assets/", StringComparison.Ordinal) && !path.Contains("//") && !path.Contains(":"), "Expected an Assets-relative path.");
            foreach (string part in path.Split('/')) Require(part != "." && part != ".." && part.Length > 0, "Noncanonical asset path.");
            return path;
        }
        private static string FullPath(string assetPath)
        {
            string project = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(project, AssetPath(assetPath)));
        }
        private static void RequireSortingLayer(string name)
        {
            foreach (var layer in SortingLayer.layers) if (layer.name == name) return;
            throw new InvalidDataException("Choose an existing actual decoration Sorting Layer: " + name);
        }
        private static void Require(bool condition, string error) { if (!condition) throw new InvalidDataException(error ?? "Sanctum board build failed."); }
    }
}
#endif
