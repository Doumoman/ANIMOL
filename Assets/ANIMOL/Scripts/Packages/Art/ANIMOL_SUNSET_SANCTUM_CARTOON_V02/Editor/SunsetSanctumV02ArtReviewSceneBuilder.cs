#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.Art.SunsetSanctumV02
{
    /// <summary>
    /// Creates an isolated review scene additively. Never opens/closes/saves production
    /// scenes, changes Build Settings, registers device IDs or adds gameplay components.
    /// </summary>
    public static class SunsetSanctumV02ArtReviewSceneBuilder
    {
        [Serializable] private sealed class Layout
        {
            public int schemaVersion;
            public CameraRow camera;
            public SpriteRow[] sprites;
            public MotionRow[] motions;
        }
        [Serializable] private sealed class CameraRow
        { public float x, y, z, orthographicSize; }
        [Serializable] private sealed class SpriteRow
        {
            public string name, file;
            public float x, y, z, scaleX, scaleY, opacity;
            public int sortingOrder;
            public bool coverCamera, background;
            public float parallaxMultiplier, driftX, driftY, periodX, periodY;
        }
        [Serializable] private sealed class MotionRow
        {
            public string name;
            public string[] clipKeys;
            public float x, y, z, secondsPerClip;
            public int sortingOrder;
        }
        [Serializable] private sealed class Effects
        {
            public int schemaVersion;
            public float[] gridOrigin;
            public float cellSize;
            public int sortingOrder;
            public LightRouteRow lightRoute;
        }
        [Serializable] private sealed class LightRouteRow
        {
            public CellRow emitter;
            public string heading, stop;
            public LightCellRow[] cells;
        }
        [Serializable] private sealed class CellRow { public int x, y; }
        [Serializable] private sealed class LightCellRow
        {
            public int x, y;
            public string kind, incoming, outgoing, receiverKey;
        }
        private sealed class ResolvedEffects
        {
            public SanctumLightRoute route;
            public Vector2 gridOrigin;
            public float cellSize;
            public int sortingOrder;
            public string sourcePath, sourceSha256, materialPath, meshPath;
            public Material material;
        }
        [Serializable] private sealed class BuildReport
        {
            public bool artReviewOnly = true;
            public bool generatedReviewSceneSaved;
            public bool actualGameplayBindingsVerified = false;
            public bool productionScenesMigrated = false;
            public bool beamCreated;
            public string sourceLayout, sourceLayoutSha256, sourceCatalogSha256, sceneAssetPath;
            public string sourceEffects, sourceEffectsSha256, beamMaterialAssetPath, beamMeshAssetPath;
            public int staticSprites, reviewMotionPlayers, backgroundLayers;
            public string scope = "ART REVIEW ONLY: no colliders, input, turns, device IDs, maps or Build Settings were modified.";
        }

        private const string LayoutName = "review_scene_layout.json";
        private const string SceneName = "SunsetSanctumV02ArtReview";
        private const string GeneratedFolder = "_Generated";
        private const string CatalogName = "SunsetSanctumV02VisualCatalog.asset";

        [MenuItem("Tools/ANIMOL/Sunset Sanctum V02/Build selected review_scene_layout.json (ART REVIEW ONLY)")]
        private static void BuildSelected()
        {
            try { BuildFromLayout(AssetDatabase.GetAssetPath(Selection.activeObject)); }
            catch (Exception e) { Debug.LogException(e); }
        }

        // Parameterless static entry point required by Unity -executeMethod.
        public static void BuildFromCommandLine()
        {
            const string flag = "-sanctumV02ReviewLayout";
            string[] args = Environment.GetCommandLineArgs();
            string path = null;
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] != flag) continue;
                if (path != null || i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1])
                    || args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    throw new ArgumentException("Missing or duplicate " + flag);
                path = args[++i];
            }
            if (path == null)
                throw new ArgumentException("Required " + flag + " Assets/.../review_scene_layout.json");
            BuildFromLayout(path);
        }

        public static string BuildFromLayout(string layoutAssetPath)
        {
            string root = ValidateLayoutPath(layoutAssetPath);
            string catalogAssetPath = root + "/" + GeneratedFolder + "/" + CatalogName;
            string sceneAssetPath = root + "/" + GeneratedFolder + "/" + SceneName + ".unity";
            // Refuse to overwrite any loaded copy, including a user's unsaved review edit.
            for (int i = 0; i < SceneManager.sceneCount; ++i)
                Require(SceneManager.GetSceneAt(i).path != sceneAssetPath,
                    "Close the generated art-review scene before rebuilding; loaded scenes are never overwritten.");

            var catalog = AssetDatabase.LoadAssetAtPath<SunsetSanctumV02VisualCatalog>(catalogAssetPath);
            SunsetSanctumV02AnimationBuilder.ValidateBuiltCatalog(catalog);
            string manifestPath = root + "/animation_catalog.json";
            string pin = File.ReadAllText(FullPath(root + "/animation_catalog_sha256.txt")).Trim();
            string actual = SunsetSanctumV02VisualCatalog.Sha256Of(File.ReadAllBytes(FullPath(manifestPath)));
            Require(string.Equals(pin, actual, StringComparison.OrdinalIgnoreCase), "Catalog SHA pin differs from the current catalog JSON.");
            string integrityError;
            Require(catalog.ValidateIntegrity(pin, out integrityError), integrityError);

            byte[] layoutBytes = File.ReadAllBytes(FullPath(layoutAssetPath));
            string layoutJson = Encoding.UTF8.GetString(layoutBytes);
            var layout = JsonUtility.FromJson<Layout>(layoutJson);
            Require(layout != null && layout.schemaVersion == 1 && layout.camera != null,
                "Review layout needs schemaVersion1 and camera.");
            ValidateCamera(layout.camera);
            var rows = layout.sprites ?? new SpriteRow[0];
            List<string> spriteObjects = ReadTopLevelObjectArray(layoutJson, "sprites");
            Require(spriteObjects.Count == rows.Length, "Review sprites must be an array of JSON objects.");
            var motions = layout.motions ?? new MotionRow[0];
            Require(rows.Length + motions.Length > 0, "Review layout is empty.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            var parts = new SunsetSanctumV02VisualCatalog.Part[rows.Length];
            int coverCount = 0;
            // Resolve and validate every reference BEFORE creating even the additive scene.
            for (int i = 0; i < rows.Length; ++i)
            {
                SpriteRow row = rows[i];
                Require(row != null && ValidName(row.name) && names.Add(row.name), "Missing/duplicate static review node name.");
                ValidatePosition(row.x, row.y, row.z);
                Require(Finite(row.scaleX) && Finite(row.scaleY) && row.scaleX > 0f && row.scaleY > 0f,
                    "Review sprite scale must be finite and positive: " + row.name);
                // Preserve an explicit zero opacity while giving omitted fields a true1 default.
                if (!HasTopLevelField(spriteObjects[i], "opacity")) row.opacity = 1f;
                Require(Finite(row.opacity) && row.opacity >= 0f && row.opacity <= 1f,
                    "Review opacity must be finite and in0..1: " + row.name);
                ValidateOrder(row.sortingOrder);
                Require(SunsetSanctumV02VisualCatalog.IsRelativePngPath(row.file), "Invalid review part path: " + row.file);
                string error;
                Require(catalog.TryGetPart(row.file, pin, out parts[i], out error), error);
                Require(parts[i].Frame.File == row.file, "Review part key must be the exact latest file.");
                if (row.coverCamera) coverCount++;
                Require(Finite(row.parallaxMultiplier) && row.parallaxMultiplier >= 0f
                    && Finite(row.driftX) && Finite(row.driftY) && Finite(row.periodX) && Finite(row.periodY),
                    "Invalid review background motion: " + row.name);
                if (row.background && !row.coverCamera)
                    Require(row.periodX >= 1f && row.periodY >= 1f, "Background drift periods must be at least one second: " + row.name);
                Require(row.z > layout.camera.z + .01f && row.z < layout.camera.z + 999f,
                    "Review sprite is outside the generated camera clipping range: " + row.name);
            }
            Require(coverCount <= 1, "Only one camera-cover sky is allowed in the review scene.");
            for (int i = 0; i < motions.Length; ++i)
            {
                MotionRow row = motions[i];
                Require(row != null && ValidName(row.name) && names.Add(row.name), "Missing/duplicate motion review node name.");
                ValidatePosition(row.x, row.y, row.z);
                ValidateOrder(row.sortingOrder);
                Require(Finite(row.secondsPerClip) && row.secondsPerClip > 0f && row.clipKeys != null && row.clipKeys.Length > 0,
                    "Review motion needs exact clipKeys and a positive hold duration: " + row.name);
                Require(row.z > layout.camera.z + .01f && row.z < layout.camera.z + 999f,
                    "Review motion is outside the generated camera clipping range: " + row.name);
                foreach (string key in row.clipKeys)
                {
                    SunsetSanctumV02VisualCatalog.Clip clip;
                    string error;
                    Require(catalog.TryGetClip(key, pin, out clip, out error), error);
                }
            }

            // Optional frozen ART REVIEW trace. It is never re-traced from pictures,
            // input or animated mirror poses. All validation precedes scene creation.
            ResolvedEffects effects = ReadOptionalEffects(root);
            if (effects != null)
            {
                Require(!names.Contains("ART_REVIEW_ONLY__STATIC_LIGHT_BEAM"), "Reserved beam review node name is already in the layout.");
                UnityEngine.Object priorMaterial = AssetDatabase.LoadMainAssetAtPath(effects.materialPath);
                UnityEngine.Object priorMesh = AssetDatabase.LoadMainAssetAtPath(effects.meshPath);
                Require(priorMaterial == null || priorMaterial is Material, "Private review material path is occupied by another asset type.");
                Require(priorMesh == null || priorMesh is Mesh, "Private review mesh path is occupied by another asset type.");
                // The root factory verifies shader import/support/errors and saves only
                // this private material. It must not call global AssetDatabase.SaveAssets.
                effects.material = SunsetSanctumV02MaterialBuilder.CreateOrUpdate(effects.materialPath);
            }

            string layoutHash = SunsetSanctumV02VisualCatalog.Sha256Of(layoutBytes);
            Scene originalActive = SceneManager.GetActiveScene();
            Scene review = default(Scene);
            bool created = false;
            try
            {
                review = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                created = true;
                // Unity 6000.3 can return false for a new unsaved additive scene even
                // when it becomes active. Verify the resulting scene itself.
                SceneManager.SetActiveScene(review);
                Require(SceneManager.GetActiveScene() == review,
                    "Could not activate the new additive art-review scene.");
                var parent = new GameObject("ART_REVIEW_ONLY__NO_GAMEPLAY_BINDINGS");
                Require(parent.scene == review, "New review root was not created in the isolated additive scene.");
                parent.AddComponent<SunsetSanctumV02ArtReviewMarker>().ConfigureForArtReview(layoutHash, pin);

                var cameraObject = CreateNode("ArtReviewCamera", parent.transform,
                    new Vector3(layout.camera.x, layout.camera.y, layout.camera.z));
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = layout.camera.orthographicSize;
                camera.nearClipPlane = .01f;
                camera.farClipPlane = 1000f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.035f, .055f, .11f, 1f);
                cameraObject.tag = "Untagged"; // Do not change or compete for the project's MainCamera tag.

                var backgroundLayers = new List<SunsetSanctumV02BackgroundRig.Layer>();
                for (int i = 0; i < rows.Length; ++i)
                {
                    SpriteRow row = rows[i];
                    var node = CreateNode(row.name, parent.transform, new Vector3(row.x, row.y, row.z));
                    node.transform.localScale = new Vector3(row.scaleX, row.scaleY, 1f);
                    var renderer = node.AddComponent<SpriteRenderer>();
                    renderer.sprite = parts[i].Frame.Sprite;
                    renderer.color = new Color(1f, 1f, 1f, row.opacity);
                    renderer.sortingLayerName = "Default";
                    renderer.sortingOrder = row.sortingOrder;
                    if (row.coverCamera)
                    {
                        // Cover is camera-anchored and must not compete with the parallax rig.
                        node.AddComponent<SunsetSanctumV02CameraCover>().Configure(camera);
                    }
                    else if (row.background)
                    {
                        backgroundLayers.Add(new SunsetSanctumV02BackgroundRig.Layer
                        {
                            renderer = renderer,
                            sortingOrder = row.sortingOrder,
                            parallaxMultiplier = row.parallaxMultiplier,
                            driftAmplitude = new Vector2(row.driftX, row.driftY),
                            driftPeriodSeconds = new Vector2(row.periodX, row.periodY),
                            phaseDegrees = i * 37f,
                            twinkleAmount = 0f,
                            wrapToView = false
                        });
                    }
                }
                if (backgroundLayers.Count > 0)
                    parent.AddComponent<SunsetSanctumV02BackgroundRig>().Configure(camera, 0f,
                        "Default", backgroundLayers.ToArray());

                for (int i = 0; i < motions.Length; ++i)
                {
                    MotionRow row = motions[i];
                    var node = CreateNode(row.name, parent.transform, new Vector3(row.x, row.y, row.z));
                    var renderer = node.AddComponent<SpriteRenderer>();
                    renderer.sortingLayerName = "Default";
                    renderer.sortingOrder = row.sortingOrder;
                    node.AddComponent<SunsetSanctumV02ArtReviewPlayback>().ConfigureForArtReview(
                        catalog, renderer, pin, row.clipKeys, row.secondsPerClip, true);
                }

                bool beamCreated = false;
                if (effects != null)
                {
                    Require(parent.transform.position == Vector3.zero
                        && Quaternion.Angle(parent.transform.rotation, Quaternion.identity) < .001f
                        && parent.transform.lossyScale == Vector3.one,
                        "Review grid root must remain at world0 with identity rotation/scale.");
                    var beamObject = CreateNode("ART_REVIEW_ONLY__STATIC_LIGHT_BEAM", parent.transform, Vector3.zero);
                    var beam = beamObject.AddComponent<SunsetSanctumV02BeamRenderer>();
                    beam.Configure(effects.material, "Default", effects.sortingOrder);
                    beam.SetRoute(effects.route, parent.transform, effects.gridOrigin, effects.cellSize);
                    Require(beam.HasVisibleBeam, "The optional review trace produced no visible beam.");
                    PersistStaticBeamSnapshot(beamObject.GetComponent<MeshFilter>(), effects);
                    beamCreated = true;
                }

                EditorSceneManager.MarkSceneDirty(review);
                Require(EditorSceneManager.SaveScene(review, sceneAssetPath, false), "Could not save the generated art-review scene.");
                var report = new BuildReport
                {
                    generatedReviewSceneSaved = true,
                    sourceLayout = layoutAssetPath,
                    sourceLayoutSha256 = layoutHash,
                    sourceCatalogSha256 = pin,
                    sceneAssetPath = sceneAssetPath,
                    staticSprites = rows.Length,
                    reviewMotionPlayers = motions.Length,
                    backgroundLayers = backgroundLayers.Count,
                    beamCreated = beamCreated,
                    sourceEffects = effects == null ? null : effects.sourcePath,
                    sourceEffectsSha256 = effects == null ? null : effects.sourceSha256,
                    beamMaterialAssetPath = effects == null ? null : effects.materialPath,
                    beamMeshAssetPath = effects == null ? null : effects.meshPath
                };
                string reportPath = root + "/" + GeneratedFolder + "/art_review_scene_build_report.json";
                File.WriteAllText(FullPath(reportPath), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
                AssetDatabase.ImportAsset(reportPath);
                Debug.Log("[Sunset Sanctum V02] ART REVIEW ONLY scene created: " + sceneAssetPath
                    + ". Production scene migration, gameplay binding and Play Mode verification remain separate.");
                return sceneAssetPath;
            }
            finally
            {
                // Only the new temporary review scene is closed. Existing dirty scenes are untouched.
                try
                {
                    if (created && review.IsValid() && review.isLoaded)
                        EditorSceneManager.CloseScene(review, true);
                }
                finally
                {
                    if (originalActive.IsValid() && originalActive.isLoaded)
                        SceneManager.SetActiveScene(originalActive);
                }
            }
        }

        private static GameObject CreateNode(string name, Transform parent, Vector3 position)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            node.transform.position = position;
            return node;
        }

        private static ResolvedEffects ReadOptionalEffects(string root)
        {
            string path = root + "/review_scene_effects.json";
            if (!File.Exists(FullPath(path))) return null;
            byte[] bytes = File.ReadAllBytes(FullPath(path));
            string json = Encoding.UTF8.GetString(bytes);
            var input = JsonUtility.FromJson<Effects>(json);
            Require(input != null && input.schemaVersion == 1 && input.lightRoute != null,
                "Optional review effects need schemaVersion1 and lightRoute.");
            Require(input.gridOrigin != null && input.gridOrigin.Length == 2
                && Finite(input.gridOrigin[0]) && Finite(input.gridOrigin[1])
                && Finite(input.cellSize) && input.cellSize > 0f,
                "Optional review gridOrigin/cellSize is invalid.");
            var row = input.lightRoute;
            Require(row.emitter != null && row.cells != null && row.cells.Length > 0,
                "Optional review beam needs an emitter and nonempty ordered cells.");
            // This review DTO describes the lit receiver snapshot only. Other stop modes
            // need an explicit excluded-cell schema rather than a guessed visual endpoint.
            Require(row.stop == "receiver", "Optional review lightRoute.stop must be receiver.");
            var cells = new List<SanctumLightCell>();
            var directedVisits = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < row.cells.Length; ++i)
            {
                LightCellRow c = row.cells[i];
                Require(c != null, "Optional review light cell is null.");
                SanctumHeading incoming = ParseHeading(c.incoming);
                SanctumHeading outgoing = ParseHeading(c.outgoing);
                SanctumLightCellKind kind = ParseLightKind(c.kind);
                Require(kind == SanctumLightCellKind.Receiver
                    ? !string.IsNullOrWhiteSpace(c.receiverKey) : string.IsNullOrEmpty(c.receiverKey),
                    "Only a receiver cell may have a nonempty review receiverKey.");
                Require(kind != SanctumLightCellKind.Receiver || i == row.cells.Length - 1,
                    "The stopping review receiver must be the final trace cell.");
                string visit = c.x + "," + c.y + "," + c.incoming;
                Require(directedVisits.Add(visit), "Optional review trace repeats a directed cell.");
                cells.Add(new SanctumLightCell(new SanctumCell(c.x, c.y), incoming,
                    outgoing, new SanctumLightVisit(kind, c.receiverKey)));
            }
            Require(cells[cells.Count - 1].kind == SanctumLightCellKind.Receiver,
                "Optional review trace must end at its stopping receiver.");
            // This shared factory verifies exact cardinal adjacency, incoming continuity
            // and that every turn is a perpendicular mirror, without tracing new logic.
            SanctumLightRoute route = SanctumLightRoute.FromAuthoritativeTrace(
                new SanctumCell(row.emitter.x, row.emitter.y), ParseHeading(row.heading),
                cells, SanctumLightStop.Receiver);
            Require(route.LightsReceiver(cells[cells.Count - 1].receiverKey), "Review receiver is absent from the validated route.");
            int sorting = HasTopLevelField(json, "sortingOrder") ? input.sortingOrder : 5;
            ValidateOrder(sorting);
            return new ResolvedEffects
            {
                route = route,
                gridOrigin = new Vector2(input.gridOrigin[0], input.gridOrigin[1]),
                cellSize = input.cellSize,
                sortingOrder = sorting,
                sourcePath = path,
                sourceSha256 = SunsetSanctumV02VisualCatalog.Sha256Of(bytes),
                materialPath = root + "/" + GeneratedFolder + "/LightBeam.mat",
                meshPath = root + "/" + GeneratedFolder + "/SunsetSanctumV02ArtReviewBeam.asset"
            };
        }

        private static void PersistStaticBeamSnapshot(MeshFilter filter, ResolvedEffects effects)
        {
            Require(filter != null && filter.sharedMesh != null && filter.sharedMesh.vertexCount > 0,
                "Review beam mesh was not generated.");
            Mesh owned = filter.sharedMesh;
            Mesh snapshot = AssetDatabase.LoadAssetAtPath<Mesh>(effects.meshPath);
            if (snapshot == null)
            {
                snapshot = UnityEngine.Object.Instantiate(owned);
                snapshot.name = "Sunset Sanctum V02 ART REVIEW static beam snapshot";
                AssetDatabase.CreateAsset(snapshot, effects.meshPath);
            }
            else EditorUtility.CopySerialized(owned, snapshot);
            EditorUtility.SetDirty(snapshot);
            AssetDatabase.SaveAssetIfDirty(snapshot);
            // BeamRenderer still owns its transient original mesh and may destroy that
            // original on scene close. The saved review scene references this asset copy.
            filter.sharedMesh = snapshot;

            float length = 0f;
            Vector2[] uv = owned.uv;
            for (int i = 0; i < uv.Length; ++i) length = Mathf.Max(length, uv[i].x);
            Require(Finite(length) && length > 0f, "Review beam has an invalid path length.");
            // MaterialPropertyBlock is transient. Persist only static snapshot values in
            // the dedicated review material so reopening the scene retains the same beam.
            effects.material.SetFloat("_PathLength", length);
            effects.material.SetFloat("_PulseDistance", 0f);
            effects.material.SetFloat("_Active", 0f);
            EditorUtility.SetDirty(effects.material);
            AssetDatabase.SaveAssetIfDirty(effects.material);
        }

        private static SanctumHeading ParseHeading(string value)
        {
            switch (value)
            {
                case "east": return SanctumHeading.East;
                case "north": return SanctumHeading.North;
                case "west": return SanctumHeading.West;
                case "south": return SanctumHeading.South;
                default: throw new InvalidDataException("Unknown exact review heading: " + value);
            }
        }
        private static SanctumLightCellKind ParseLightKind(string value)
        {
            switch (value)
            {
                case "open": return SanctumLightCellKind.Open;
                case "glass": return SanctumLightCellKind.Glass;
                case "mirror": return SanctumLightCellKind.Mirror;
                case "receiver": return SanctumLightCellKind.Receiver;
                default: throw new InvalidDataException("Unknown visible review light kind: " + value);
            }
        }

        // JsonUtility supplies defaults for absent fields. Detect this one optional root
        // property without confusing it with a string value or a nested cell property.
        private static bool HasTopLevelField(string json, string field)
        { return TopLevelValueStart(json, field) >= 0; }

        private static int TopLevelValueStart(string json, string field)
        {
            int depth = 0;
            for (int i = 0; i < json.Length; ++i)
            {
                char c = json[i];
                if (c == '{' || c == '[') { depth++; continue; }
                if (c == '}' || c == ']') { depth--; continue; }
                if (c != '"') continue;
                var token = new StringBuilder();
                bool escapedName = false;
                for (i++; i < json.Length; i++)
                {
                    c = json[i];
                    if (c == '\\') { escapedName = true; i++; continue; }
                    if (c == '"') break;
                    token.Append(c);
                }
                int after = i + 1;
                while (after < json.Length && char.IsWhiteSpace(json[after])) after++;
                if (depth == 1 && !escapedName && token.ToString() == field
                    && after < json.Length && json[after] == ':')
                {
                    after++;
                    while (after < json.Length && char.IsWhiteSpace(json[after])) after++;
                    return after;
                }
            }
            return -1;
        }

        private static List<string> ReadTopLevelObjectArray(string json, string field)
        {
            var result = new List<string>();
            int start = TopLevelValueStart(json, field);
            if (start < 0 || json.Substring(start).StartsWith("null", StringComparison.Ordinal)) return result;
            Require(start < json.Length && json[start] == '[', "Review " + field + " must be a JSON array.");
            int depth = 0;
            int objectStart = -1;
            for (int i = start; i < json.Length; ++i)
            {
                char c = json[i];
                if (c == '"')
                {
                    Require(depth != 1, "Review " + field + " may contain only JSON objects.");
                    for (i++; i < json.Length; i++)
                    {
                        if (json[i] == '\\') { i++; continue; }
                        if (json[i] == '"') break;
                    }
                    continue;
                }
                if (c == '{' || c == '[')
                {
                    if (c == '{' && depth == 1) objectStart = i;
                    depth++;
                }
                else if (c == '}' || c == ']')
                {
                    depth--;
                    if (c == '}' && depth == 1 && objectStart >= 0)
                    {
                        result.Add(json.Substring(objectStart, i - objectStart + 1));
                        objectStart = -1;
                    }
                    if (depth == 0) break;
                }
                else if (depth == 1 && !char.IsWhiteSpace(c) && c != ',')
                    throw new InvalidDataException("Review " + field + " may contain only JSON objects.");
            }
            Require(depth == 0 && objectStart < 0, "Review object array is incomplete.");
            return result;
        }

        private static string ValidateLayoutPath(string path)
        {
            Require(!string.IsNullOrWhiteSpace(path) && path.StartsWith("Assets/", StringComparison.Ordinal)
                && path.IndexOf('\\') < 0 && path.IndexOf("/../", StringComparison.Ordinal) < 0
                && path.IndexOf("/./", StringComparison.Ordinal) < 0 && Path.GetFileName(path) == LayoutName,
                "Select the latest package's Assets-relative review_scene_layout.json.");
            string root = Path.GetDirectoryName(path).Replace('\\', '/');
            Require(root.EndsWith("/" + SunsetSanctumV02VisualCatalog.RequiredPackageId, StringComparison.Ordinal)
                && AssetDatabase.IsValidFolder(root) && File.Exists(FullPath(path)),
                "Review layout is outside the latest package root or does not exist.");
            return root;
        }
        private static void ValidateCamera(CameraRow row)
        {
            ValidatePosition(row.x, row.y, row.z);
            Require(Finite(row.orthographicSize) && row.orthographicSize > 0f,
                "Review camera orthographicSize must be positive.");
        }
        private static void ValidatePosition(float x, float y, float z)
        { Require(Finite(x) && Finite(y) && Finite(z), "Review transforms must be finite."); }
        private static void ValidateOrder(int order)
        { Require(order >= -32768 && order <= 32767, "Review sortingOrder is outside the SpriteRenderer range."); }
        private static bool ValidName(string name)
        { return !string.IsNullOrWhiteSpace(name) && name.Length <= 200; }
        private static bool Finite(float value)
        { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static string FullPath(string path)
        { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path)); }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidDataException(message); }
    }
}
#endif
