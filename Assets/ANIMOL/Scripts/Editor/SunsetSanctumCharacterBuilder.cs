using System;
using System.IO;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using ANIMOL.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Animol.Art.SanctumRetouch;

namespace Animol.Art.SunsetSanctumNative
{
    public static class SunsetSanctumCharacterBuilder
    {
        const string Root = "Assets/ANIMOL/Art/SunsetSanctumCharacters";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/LetterObservatoryCharacters.asset";
        const string ThemePath = "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset";
        const string MenuPath = "Assets/ANIMOL/Data/UI/MainMenuCatalog.asset";
        const string LobbyScenePath = "Assets/ANIMOL/Scenes/Archive/PreviousMain.unity";
        static readonly string[] Ids = { "sein", "daon", "lumi", "soyul", "doyun" };
        static readonly string[] RetouchNames = { "\uC138\uC778", "\uB2E4\uC628", "\uB8E8\uBBF8", "\uC18C\uC728", "\uB3C4\uC724" };
        [Serializable] sealed class RetouchManifest { public Calibration projectCalibration; }
        [Serializable] sealed class Calibration
        {
            public float standingHeightWorld;
            public float restingOffsetLocalX;
            public float restingOffsetLocalY;
        }
        static readonly string[] Names = { "세인", "다온", "루미", "소율", "도윤" };

        public static void Build()
        {
            var manifest = JsonUtility.FromJson<AnimolSunsetSanctumNativeImporter.Manifest>(
                File.ReadAllText(Root + "/sunset_sanctum_native_animations.json"));
            if (!manifest.nativeResolution || manifest.frames == null || manifest.frames.Length != 540 ||
                manifest.clips == null || manifest.clips.Length != 90)
                throw new InvalidDataException("Sunset Sanctum retouch manifest must have 540 frames and 90 clips.");

            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<StarwayCharacterCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.defaultCharacterId = "sein";
            catalog.characters = new StarwayCharacterDefinition[Ids.Length];
            for (var i = 0; i < Ids.Length; i++)
            {
                var id = Ids[i];
                var calibration = JsonUtility.FromJson<RetouchManifest>(File.ReadAllText(
                    Root + "/sanctum_" + id + "_retouch_animations.json"));
                if (calibration?.projectCalibration == null)
                    throw new InvalidDataException("Missing scene calibration for " + id);
                var entry = new StarwayCharacterDefinition {
                    id = id, displayName = RetouchNames[i], motions = new StarwayCharacterMotion[18],
                    sanctumPushContacts = AssetDatabase.LoadAssetAtPath<SanctumPushSpriteContacts>(
                        Root + "/PushContacts/sanctum_push_contacts_" + id + ".asset"),
                    restingLocalPosition = new Vector3(calibration.projectCalibration.restingOffsetLocalX,
                        calibration.projectCalibration.restingOffsetLocalY, 0f),
                    calibratedStandingHeight = calibration.projectCalibration.standingHeightWorld,
                    pushApproachLimit = .22f
                };
                if (!entry.sanctumPushContacts) throw new InvalidDataException("Missing PUSH contacts for " + id);
                var illustrationPath = Root + "/Illustrations/animol_sunset_sanctum_" + id + "_illustration.png";
                entry.illustration = AssetDatabase.LoadAssetAtPath<Sprite>(illustrationPath);
                if (!entry.illustration) throw new InvalidDataException("Missing illustration: " + id);
                var index = 0;
                foreach (var data in manifest.clips)
                {
                    if (data.character != id) continue;
                    if (data.frames == null || data.frames.Length != 6)
                        throw new InvalidDataException("Expected six frames: " + data.name);
                    var path = Root + "/Animations/" + id + "/" + data.name + ".anim";
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (!clip) throw new FileNotFoundException("Missing playable clip", path);
                    var motion = new StarwayCharacterMotion { clip = clip, fps = data.fps, loop = data.loop };
                    for (var frame = 0; frame < 6; frame++)
                    {
                        motion.frames[frame] = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + data.frames[frame]);
                        if (!motion.frames[frame])
                            throw new InvalidDataException("Missing frame: " + data.name + "/" + frame);
                    }
                    entry.motions[index++] = motion;
                }
                if (index != 18) throw new InvalidDataException("Expected 18 clips for " + id);
                catalog.characters[i] = entry;
            }
            EditorUtility.SetDirty(catalog);
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(ThemePath);
            if (!theme) throw new FileNotFoundException("Missing Letter Observatory theme", ThemePath);
            if (theme.letterObservatoryCharacters != catalog || theme.letterObservatoryCharacterId != catalog.defaultCharacterId)
            {
                theme.letterObservatoryCharacters = catalog;
                theme.letterObservatoryCharacterId = catalog.defaultCharacterId;
                EditorUtility.SetDirty(theme);
                AssetDatabase.SaveAssetIfDirty(theme);
            }
            var menu = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(MenuPath);
            if (!menu || menu.themes == null || menu.themes.Length < 3 || menu.themes[2].gameCharacters != catalog ||
                menu.themes[2].displayName != "\uB099\uC77C \uC131\uC18C")
                throw new InvalidDataException("Existing Sanctum lobby binding must be reviewed before rebuilding it.");
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("ANIMOL Sunset Sanctum retouch: connected five characters, 90 clips, and five existing illustrations while preserving the letter_observatory save ID.");
        }

        public static void BuildBatch() => Build();

        static void ConnectMainMenu(StarwayCharacterCatalog catalog)
        {
            var menu = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(MenuPath);
            if (!menu || menu.themes == null || menu.themes.Length < 3 || menu.themes[2].id != "observatory")
                throw new InvalidDataException("Missing existing third-theme lobby entry");
            var entry = menu.themes[2];
            entry.displayName = "낙일 성소";
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
                if (label.gameObject.scene == lobby && label.text == "편지 천문대")
                {
                    label.text = "낙일 성소";
                    EditorUtility.SetDirty(label);
                    changed++;
                }
            if (changed == 0) throw new InvalidDataException("Third-theme lobby caption not found");
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
    }
}
