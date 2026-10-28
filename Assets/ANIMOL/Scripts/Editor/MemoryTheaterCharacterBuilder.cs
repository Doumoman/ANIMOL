using System;
using System.IO;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using ANIMOL.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Animol.Art.AfterimageTheaterNative
{
    public static class MemoryTheaterCharacterBuilder
    {
        const string Root = "Assets/ANIMOL/Art/AfterimageTheaterCharacters";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/MemoryTheaterCharacters.asset";
        const string MenuPath = "Assets/ANIMOL/Data/UI/MainMenuCatalog.asset";
        const string LobbyScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";
        const string QaScene = "Assets/ANIMOL/Scenes/QA/ANIMOL_MemoryTheater_Character_QA.unity";
        // The source character_designs.json lists these four IDs in this order.
        static readonly string[] Ids = { "seri", "roan", "mion", "piko" };
        static readonly string[] Names = { "\uC138\uB9AC", "\uB85C\uC548", "\uBBF8\uC628", "\uD53C\uCF54" };

        public static void Build()
        {
            var manifest = JsonUtility.FromJson<AnimolAfterimageTheaterNativeImporter.Manifest>(
                File.ReadAllText(Root + "/afterimage_theater_native_animations.json"));
            if (!manifest.nativeResolution || manifest.frames == null || manifest.frames.Length != 360 ||
                manifest.clips == null || manifest.clips.Length != 60)
                throw new InvalidDataException("Afterimage Theater native manifest must have 360 frames and 60 clips.");
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<StarwayCharacterCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.defaultCharacterId = "seri";
            catalog.characters = new StarwayCharacterDefinition[Ids.Length];
            for (var i = 0; i < Ids.Length; i++)
            {
                var id = Ids[i];
                var entry = new StarwayCharacterDefinition { id = id, displayName = Names[i] };
                var illustrationPath = Root + "/Illustrations/animol_memory_theater_" + id + "_illustration.png";
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
                    entry.motions[index++] = motion;
                }
                if (index != 15) throw new InvalidDataException("Expected 15 clips for " + id);
                catalog.characters[i] = entry;
            }
            EditorUtility.SetDirty(catalog);
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset",
                "Assets/ANIMOL/Data/Art/MemorySkyTheater/PuzzleTheme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                if (!theme) throw new FileNotFoundException("Missing Afterimage Theater theme", path);
                theme.memoryTheaterCharacters = catalog;
                theme.memoryTheaterCharacterId = catalog.defaultCharacterId;
                EditorUtility.SetDirty(theme);
            }
            ConnectMainMenu(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("ANIMOL Afterimage Theater native: connected four characters, 60 clips, and four illustrations while preserving the memory_theater save key.");
        }

        public static void BuildBatch() => Build();

        static void ConnectMainMenu(StarwayCharacterCatalog catalog)
        {
            var menu = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(MenuPath);
            if (!menu || menu.themes == null || menu.themes.Length < 2 || menu.themes[1].id != "theater")
                throw new InvalidDataException("Missing existing theater lobby entry");
            var entry = menu.themes[1];
            entry.displayName = "잔영 극장";
            entry.resourceId = "afterimage_theater";
            entry.resourceAliases = new[] { "memory_theater" };
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

            var previous = EditorSceneManager.GetActiveScene();
            if (previous.IsValid() && previous.isDirty)
                throw new InvalidOperationException("Save the active scene before updating the lobby theme caption.");
            var lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var changed = 0;
            foreach (var label in Resources.FindObjectsOfTypeAll<Text>())
                if (label.gameObject.scene == lobby && (label.text == "기억 극장" || label.text == "잔영 극장"))
                {
                    label.text = "잔영 극장";
                    EditorUtility.SetDirty(label);
                    changed++;
                }
            if (changed == 0) throw new InvalidDataException("Theater lobby caption not found");
            EditorSceneManager.MarkSceneDirty(lobby);
            EditorSceneManager.SaveScene(lobby);
            if (previous.IsValid() && !string.IsNullOrEmpty(previous.path) && previous.path != LobbyScenePath)
                EditorSceneManager.OpenScene(previous.path, OpenSceneMode.Single);
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
            var root = new GameObject("Afterimage Theater Character QA");
            var player = StarwayCharacterVisual.Create(root.transform, Vector2.zero, 40, catalog, catalog.defaultCharacterId);
            var qa = root.AddComponent<MemoryTheaterCharacterQaController>();
            qa.catalog = catalog;
            qa.character = player;
            EditorSceneManager.SaveScene(scene, QaScene);
        }
    }
}
