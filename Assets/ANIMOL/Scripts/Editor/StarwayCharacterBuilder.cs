using System;
using System.IO;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using ANIMOL.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Animol.Art.StarwayNative
{
    public static class StarwayCharacterBuilder
    {
        const string Root = "Assets/ANIMOL/Art/StarwayCharacters";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/StarwayCharacters.asset";
        const string MenuPath = "Assets/ANIMOL/Data/UI/MainMenuCatalog.asset";
        const string QaScene = "Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Character_QA.unity";
        static readonly string[] Ids = { "choa", "uram", "yeoul", "tori" };
        static readonly string[] Names = { "초아", "이솔", "여울", "토리" };

        [MenuItem("ANIMOL/Art/Connect Starway Characters")]
        public static void Build()
        {
            var manifest = JsonUtility.FromJson<AnimolStarwayNativeImporter.Manifest>(
                File.ReadAllText(Root + "/starway_native_animations.json"));
            if (!manifest.nativeResolution || manifest.frames == null || manifest.frames.Length != 360 ||
                manifest.clips == null || manifest.clips.Length != 60)
                throw new InvalidDataException("Starway native manifest must have 360 frames and 60 clips.");
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<StarwayCharacterCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.defaultCharacterId = "choa";
            catalog.characters = new StarwayCharacterDefinition[Ids.Length];
            for (var i = 0; i < Ids.Length; i++)
            {
                var id = Ids[i];
                var entry = new StarwayCharacterDefinition { id = id, displayName = Names[i] };
                var illustrationPath = Root + "/Illustrations/animol_starway_" + id + "_illustration.png";
                ConfigureIllustration(illustrationPath);
                entry.illustration = AssetDatabase.LoadAssetAtPath<Sprite>(illustrationPath);
                if (!entry.illustration) throw new InvalidDataException("Missing illustration: " + id);
                var index = 0;
                foreach (var data in manifest.clips)
                {
                    if (data.character != id) continue;
                    var path = Root + "/Animations/" + id + "/" + data.name + ".anim";
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (!clip) throw new InvalidDataException("Missing playable clip: " + path);
                    var motion = new StarwayCharacterMotion { clip = clip, fps = data.fps, loop = data.loop };
                    for (var frame = 0; frame < 6; frame++)
                    {
                        motion.frames[frame] = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + data.frames[frame]);
                        if (!motion.frames[frame]) throw new InvalidDataException("Missing frame: " + data.name + "/" + frame);
                    }
                    entry.motions[index] = motion;
                    index++;
                }
                if (index != 15) throw new InvalidDataException("Expected 15 clips for " + id);
                catalog.characters[i] = entry;
            }
            EditorUtility.SetDirty(catalog);
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                if (!theme) throw new FileNotFoundException("Missing starway theme", path);
                theme.starwayCharacters = catalog;
                theme.starwayCharacterId = catalog.defaultCharacterId;
                // Other theme previews copy this historical static fallback. Runtime uses the catalog Visual.
                theme.player = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/ANIMOL/Sprites/Player/rabbit/Frames/spr_player_rabbit_idle_s.png");
                EditorUtility.SetDirty(theme);
            }
            ConnectMainMenu(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("ANIMOL Starway native: connected four characters, 60 clips, and four illustrations; uram save ID displays as 이솔.");
        }

        public static void BuildBatch() => Build();

        static void ConnectMainMenu(StarwayCharacterCatalog catalog)
        {
            var menu = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(MenuPath);
            if (!menu || menu.themes == null || menu.themes.Length < 1 || menu.themes[0].id != "starway")
                throw new InvalidDataException("Missing existing Starway lobby entry");
            var entry = menu.themes[0];
            entry.gameCharacters = catalog;
            entry.people = new MainMenuPerson[Ids.Length];
            for (var i = 0; i < Ids.Length; i++)
            {
                var id = Ids[i];
                entry.people[i] = new MainMenuPerson {
                    characterId = id,
                    appearances = new[] { new MainMenuAppearance {
                        artId = id, costumeId = "default", illustration = catalog.Find(id).illustration
                    } }
                };
            }
            EditorUtility.SetDirty(menu);
        }

        static void ConfigureIllustration(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new FileNotFoundException("Missing illustration importer", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.RGBA32;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }

        static void BuildQaScene(StarwayCharacterCatalog catalog)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.GetComponent<Camera>().orthographic = true;
            camera.GetComponent<Camera>().orthographicSize = 4;
            camera.GetComponent<Camera>().backgroundColor = new Color(.1f, .14f, .2f);
            var root = new GameObject("Starway Character QA");
            var player = StarwayCharacterVisual.Create(root.transform, Vector2.zero, 40, catalog, catalog.defaultCharacterId);
            player.catalog = catalog;
            player.characterId = catalog.defaultCharacterId;
            player.visual = player.GetComponent<SpriteRenderer>();
            EditorUtility.SetDirty(player);
            var qa = root.AddComponent<StarwayCharacterQaController>();
            qa.catalog = catalog;
            qa.character = player;
            EditorSceneManager.SaveScene(scene, QaScene);
        }
    }
}
