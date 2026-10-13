#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Animol.GraphicJoin;
using Animol.QA;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Import this package into its own Assets/AnimolObstacleQA_v3 folder, then compile first.
// This builder creates a new standalone QA scene and cached sprites. Existing scenes/maps are not edited.
public static class UnityTemporaryQASceneBuilder
{
    [Serializable] public sealed class Point { public float x, y; }
    [Serializable] public sealed class Bell { public int id; public float x, y; }
    [Serializable] public sealed class Goal { public float x, y, width, height; }
    [Serializable] public sealed class Stage { public string id; public int width, height; public Point spawn; public Bell[] bells; public Goal goal; }
    [Serializable] public sealed class PixelRect { public int x, y, width, height; }
    [Serializable] public sealed class Part { public string file; public PixelRect rect; }
    [Serializable] public sealed class Parts { public Part body, cap; }
    [Serializable] public sealed class Cell
    {
        public int x, y;
        public string kind, facing, idleFile, loadedFile;
        public string[] activeFiles, fragmentFiles;
        public Parts initial, collapsed;
    }
    [Serializable] public sealed class Graphics { public int schemaVersion, seed; public string styleId; public Cell[] cells; }
    sealed class SpriteCache
    {
        readonly string packageRoot, generatedRoot;
        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public SpriteCache(string packageRoot, string generatedRoot) { this.packageRoot = packageRoot; this.generatedRoot = generatedRoot; }
        public Sprite Load(string file, PixelRect rect = null)
        {
            if (string.IsNullOrEmpty(file)) return null;
            string key = file + (rect == null ? "|full" : $"|{rect.x},{rect.y},{rect.width},{rect.height}");
            if (sprites.TryGetValue(key, out var cached)) return cached;
            string assetPath = packageRoot + "/" + file;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null) throw new InvalidOperationException("Missing QA texture: " + assetPath);
            string spritePath = generatedRoot + "/QA_Sprite_" + Hash128.Compute(key) + ".asset";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                // Catalogs use top-left rects; Unity sprite rects use bottom-left.
                var unityRect = rect == null ? new Rect(0, 0, texture.width, texture.height) :
                    new Rect(rect.x, texture.height - rect.y - rect.height, rect.width, rect.height);
                sprite = Sprite.Create(texture, unityRect, new Vector2(.5f, .5f), 32, 0, SpriteMeshType.FullRect);
                sprite.name = Path.GetFileNameWithoutExtension(file) + "_" + Hash128.Compute(key);
                AssetDatabase.CreateAsset(sprite, spritePath);
            }
            sprites.Add(key, sprite); return sprite;
        }
        public Sprite Load(Part part) => part == null ? null : Load(part.file, part.rect);
        public Sprite[] LoadAll(string[] files)
        {
            var result = new Sprite[files == null ? 0 : files.Length];
            for (int i = 0; i < result.Length; i++) result[i] = Load(files[i]);
            return result;
        }
    }

    [MenuItem("ANIMOL/QA/Create Temporary Player Scene v3")]
    public static void CreateFromSelectedPackage()
    {
        string root = Selection.activeObject == null ? "" : AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!AssetDatabase.IsValidFolder(root) || !File.Exists(root + "/Data/temporary_qa_stage.json"))
            throw new InvalidOperationException("Select the imported QA package root folder containing Art, Data and Runtime.");
        string output = Create(root);
        Debug.Log("ANIMOL QA scene created: " + output + ". Open this scene alone to test the temporary player.");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(output));
    }

    // MCP may call this after compilation with a concrete Assets/... root, without using the menu.
    public static string Create(string packageRoot)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build the QA fixture outside Play Mode.");
        packageRoot = packageRoot.TrimEnd('/').Replace('\\', '/');
        if (!packageRoot.StartsWith("Assets/", StringComparison.Ordinal)) throw new ArgumentException("QA package must be under Assets/.");
        var stage = JsonUtility.FromJson<Stage>(File.ReadAllText(packageRoot + "/Data/temporary_qa_stage.json"));
        var graphics = JsonUtility.FromJson<Graphics>(File.ReadAllText(packageRoot + "/Data/temporary_qa_graphics.json"));
        if (stage.spawn == null || stage.bells == null || stage.bells.Length != 3 || stage.goal == null ||
            graphics.schemaVersion != 1 || graphics.styleId != "T01_A" || graphics.cells == null)
            throw new InvalidOperationException("QA data contract is incomplete.");
        var ids = new HashSet<int>();
        foreach (var bell in stage.bells) if (!ids.Add(bell.id)) throw new InvalidOperationException("Duplicate QA bell ID.");
        int crumbleCount = 0;
        foreach (var cell in graphics.cells) if (cell.kind == "C05") crumbleCount++;
        if (crumbleCount != 1) throw new InvalidOperationException("This fixed QA visual snapshot requires exactly one C05.");
        string generated = packageRoot + "/GeneratedQA";
        if (!AssetDatabase.IsValidFolder(generated)) AssetDatabase.CreateFolder(packageRoot, "GeneratedQA");
        ImportQaTextures(packageRoot);
        var cache = new SpriteCache(packageRoot, generated);
        Scene previousActive = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var qaRoot = new GameObject("ANIMOL_Temporary_QA_Only");
            var run = qaRoot.AddComponent<UnityTemporaryRun2D>();
            var stageVisuals = qaRoot.AddComponent<UnityTemporaryStageVisuals2D>(); stageVisuals.Run = run;
            var visualEntries = new List<UnityTemporaryStageVisuals2D.Entry>();
            var obstacles = new List<UnityTemporaryObstacle2D>();
            foreach (var cell in graphics.cells)
            {
                var root = Child(qaRoot.transform, cell.kind + "_" + cell.x + "_" + cell.y);
                root.transform.localPosition = new Vector3(cell.x + .5f, cell.y + .5f);
                var bodySprite = cache.Load(cell.initial.body); var afterBody = cache.Load(cell.collapsed.body);
                var capSprite = cache.Load(cell.initial.cap); var afterCap = cache.Load(cell.collapsed.cap);
                SpriteRenderer body = null, cap = null;
                if (bodySprite != null || afterBody != null) body = Draw(root.transform, "Body", bodySprite, Vector3.zero, 0);
                if (capSprite != null || afterCap != null) cap = Draw(root.transform, "Cap", capSprite, Vector3.up * .375f, 1);
                visualEntries.Add(new UnityTemporaryStageVisuals2D.Entry {
                    Body = body, Cap = cap, InitialBody = bodySprite, InitialCap = capSprite,
                    CollapsedBody = afterBody, CollapsedCap = afterCap });
                UnityTemporaryObstacle2D device = null;
                if (cell.kind != "terrain")
                {
                    device = root.AddComponent<UnityTemporaryObstacle2D>();
                    device.Run = run; device.Kind = cell.kind; device.Facing = cell.facing;
                    device.CellOrigin = new Vector2(cell.x, cell.y);
                    device.IdleSprite = cache.Load(cell.idleFile); device.LoadedSprite = cache.Load(cell.loadedFile);
                    device.ActiveFrames = cache.LoadAll(cell.activeFiles); device.CrumblePieces = cache.LoadAll(cell.fragmentFiles);
                    device.Overlay = Draw(root.transform, "Mechanism", device.IdleSprite, Vector3.zero, 2);
                    obstacles.Add(device);
                }
                var shape = root.AddComponent<BoxCollider2D>();
                shape.size = Vector2.one;
                switch (cell.kind)
                {
                    case "C02":
                        shape.size = new Vector2(4f / 32, 26f / 32);
                        shape.offset = new Vector2(cell.facing == "LEFT" ? -14f / 32 : 14f / 32, 0);
                        break;
                    case "C03":
                        shape.size = new Vector2(28f / 32, 4f / 32); shape.offset = Vector2.up * (14f / 32);
                        OneWay(root, shape); break;
                    case "C01": case "C05": case "C06":
                        shape.size = new Vector2(1, 8f / 32); shape.offset = Vector2.up * (12f / 32);
                        OneWay(root, shape); break;
                    case "C04":
                        shape.isTrigger = true; shape.size = new Vector2(30f / 32, 15f / 32);
                        shape.offset = Vector2.down * (8.5f / 32); break;
                    case "C07": shape.isTrigger = true; break;
                    case "terrain": case "C08": case "C10": break;
                    default: throw new InvalidOperationException("Unexpected QA kind: " + cell.kind);
                }
                if (device != null) device.Surface = shape;
                if (cell.kind == "C05")
                {
                    var vfxRoot = Child(qaRoot.transform, "C05_Native_VFX");
                    device.CrumbleVfx = vfxRoot.AddComponent<UnityCrumbleVfx>();
                }
            }
            stageVisuals.Entries = visualEntries.ToArray(); run.Obstacles = obstacles.ToArray();
            var spawn = Child(qaRoot.transform, "PlayerStart_Feet");
            spawn.transform.position = new Vector3(stage.spawn.x, stage.spawn.y); run.SpawnFeet = spawn.transform;
            Draw(qaRoot.transform, "StartFlag", cache.Load("Art/QAEntities/start_idle_1.png"),
                new Vector3(stage.spawn.x, stage.spawn.y + .5f), 4);
            var playerRoot = Child(qaRoot.transform, "TemporaryPlayer");
            playerRoot.transform.position = new Vector3(stage.spawn.x, stage.spawn.y + UnityTemporaryPlayer2D.BodyHeight * .5f);
            playerRoot.AddComponent<Rigidbody2D>(); var playerShape = playerRoot.AddComponent<CapsuleCollider2D>();
            playerShape.size = new Vector2(UnityTemporaryPlayer2D.BodyWidth, UnityTemporaryPlayer2D.BodyHeight);
            var player = playerRoot.AddComponent<UnityTemporaryPlayer2D>(); run.Player = player; player.Run = run;
            player.RightSprite = cache.Load("Art/QAEntities/player_idle_1.png"); player.LeftSprite = cache.Load("Art/QAEntities/player_idle_-1.png");
            player.Visual = Draw(playerRoot.transform, "PixelPlayer", player.RightSprite, Vector3.up * .06f, 6);
            var bells = new List<UnityTemporaryBell2D>();
            foreach (var point in stage.bells)
            {
                var bellRoot = Child(qaRoot.transform, "Bell_" + point.id);
                bellRoot.transform.position = new Vector3(point.x, point.y);
                var visual = bellRoot.AddComponent<SpriteRenderer>(); visual.sprite = cache.Load("Art/QAEntities/bell_idle_1.png"); visual.sortingOrder = 5;
                var trigger = bellRoot.AddComponent<CircleCollider2D>(); trigger.radius = .28f; trigger.isTrigger = true;
                var bell = bellRoot.AddComponent<UnityTemporaryBell2D>(); bell.BellId = point.id; bell.Run = run; bells.Add(bell);
            }
            run.Bells = bells.ToArray();
            var goalRoot = Child(qaRoot.transform, "Goal_Requires_Three_Bells");
            goalRoot.transform.position = new Vector3(stage.goal.x + stage.goal.width * .5f, stage.goal.y + 1);
            var goalVisual = goalRoot.AddComponent<SpriteRenderer>(); goalVisual.sortingOrder = 4;
            var goalTrigger = goalRoot.AddComponent<BoxCollider2D>(); goalTrigger.isTrigger = true;
            goalTrigger.size = new Vector2(stage.goal.width, stage.goal.height);
            goalTrigger.offset = new Vector2(0, stage.goal.height * .5f - 1);
            var goal = goalRoot.AddComponent<UnityTemporaryGoal2D>(); goal.Run = run; run.Goal = goal;
            goal.LockedSprite = cache.Load("Art/QAEntities/goal_locked_1.png"); goal.OpenSprite = cache.Load("Art/QAEntities/goal_open_1.png"); goal.SetOpen(false);
            var cameraRoot = Child(qaRoot.transform, "QA_Camera"); var camera = cameraRoot.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 10; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(26, 28, 44, 255); cameraRoot.transform.position = new Vector3(5.5f, 10, -10);
            var follow = cameraRoot.AddComponent<UnityTemporaryCamera2D>(); follow.Target = playerRoot.transform; follow.WorldWidth = stage.width;
            string output = AssetDatabase.GenerateUniqueAssetPath(generated + "/ANIMOL_TempPlayer_QA.unity");
            if (!EditorSceneManager.SaveScene(scene, output)) throw new IOException("Unable to save QA scene.");
            return output;
        }
        finally
        {
            // Leave the user's currently open scenes as they were. The saved QA scene can be opened alone.
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }
    static GameObject Child(Transform parent, string name)
    {
        var child = new GameObject(name); child.transform.SetParent(parent, false); return child;
    }
    static SpriteRenderer Draw(Transform parent, string name, Sprite sprite, Vector3 offset, int order)
    {
        var child = Child(parent, name); child.transform.localPosition = offset;
        var renderer = child.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order; renderer.enabled = sprite != null; return renderer;
    }
    static void OneWay(GameObject root, Collider2D collider)
    {
        var effector = root.AddComponent<PlatformEffector2D>(); effector.useOneWay = true;
        effector.useOneWayGrouping = true; effector.surfaceArc = 180; collider.usedByEffector = true;
    }
    static void ImportQaTextures(string packageRoot)
    {
        // Only the imported QA copies are configured. Original production texture GUIDs/importers are not touched.
        foreach (string file in Directory.GetFiles(packageRoot + "/Art", "*.png", SearchOption.AllDirectories))
        {
            string assetPath = file.Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer not ready: " + assetPath);
            importer.textureType = TextureImporterType.Default; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            var platform = importer.GetDefaultPlatformTextureSettings(); platform.overridden = false;
            platform.maxTextureSize = 4096; platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platform); importer.SaveAndReimport();
        }
    }
}
#endif
