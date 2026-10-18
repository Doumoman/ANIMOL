using System;
using System.IO;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using ANIMOL.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Animol.Art.StarwaveCoastNative
{
    public static class TwilightSeaCharacterBuilder
    {
        const string Root = "Assets/ANIMOL/Art/StarwaveCoastCharacters";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/TwilightSeaCharacters.asset";
        const string MenuPath = "Assets/ANIMOL/Data/UI/MainMenuCatalog.asset";
        const string LobbyScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";
        static readonly string[] Ids = { "haerin", "stranger", "danu", "eden" };
        static readonly string[] Names = { "\uD574\uB9B0", "\uC774\uBC29\uC778", "\uB2E8\uC6B0", "\uC774\uB4E0" };

        [Serializable] sealed class EdenCostumeFile
        {
            public string character;
            public string @default;
            public EdenCostumeRow[] costumes;
        }

        [Serializable] sealed class EdenCostumeRow
        {
            public string id, assetSet, illustration;
            public string[] clipNames;
        }

        [MenuItem("ANIMOL/Art/Connect Starwave Coast Characters")]
        public static void Build()
        {
            var manifest = JsonUtility.FromJson<AnimolStarwaveCoastNativeImporter.Manifest>(
                File.ReadAllText(Root + "/starwave_coast_native_animations.json"));
            if (manifest.version != 6 || !manifest.nativeResolution || manifest.frames == null ||
                manifest.frames.Length != 450 || manifest.clips == null || manifest.clips.Length != 75)
                throw new InvalidDataException("Starwave Coast native manifest must have 450 frames and 75 clips.");
            var costumes = JsonUtility.FromJson<EdenCostumeFile>(File.ReadAllText(Root + "/eden_costumes.json"));
            if (costumes == null || costumes.character != "eden" || costumes.@default != "rabbit" ||
                costumes.costumes == null || costumes.costumes.Length != 2)
                throw new InvalidDataException("Expected Eden rabbit and puppy costume mapping.");
            var rabbit = Array.Find(costumes.costumes, row => row.id == "rabbit");
            var puppy = Array.Find(costumes.costumes, row => row.id == "puppy");
            if (rabbit == null || rabbit.assetSet != "eden" || puppy == null || puppy.assetSet != "edenpuppy")
                throw new InvalidDataException("Eden costume asset sets must be eden and edenpuppy.");

            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<StarwayCharacterCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.defaultCharacterId = "haerin";
            catalog.characters = new StarwayCharacterDefinition[Ids.Length];
            for (var i = 0; i < Ids.Length; i++)
            {
                var id = Ids[i];
                var entry = new StarwayCharacterDefinition
                {
                    id = id,
                    displayName = Names[i],
                    defaultCostumeId = id == "eden" ? costumes.@default : "default"
                };
                entry.illustration = id == "eden" ? LoadIllustrationPath(rabbit.illustration) : LoadIllustration(id);
                entry.motions = LoadMotions(manifest, id, id, id == "eden" ? rabbit.clipNames : null);
                if (id == "eden")
                    entry.costumes = new[] { new StarwayCharacterCostume
                    {
                        id = puppy.id,
                        assetSet = puppy.assetSet,
                        illustration = LoadIllustrationPath(puppy.illustration),
                        motions = LoadMotions(manifest, "eden", puppy.assetSet, puppy.clipNames)
                    } };
                catalog.characters[i] = entry;
            }
            EditorUtility.SetDirty(catalog);
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset",
                "Assets/ANIMOL/Data/Art/TwilightCoast/PuzzleTheme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                if (!theme) throw new FileNotFoundException("Missing Starwave Coast theme", path);
                theme.twilightSeaCharacters = catalog;
                theme.twilightSeaCharacterId = "haerin";
                theme.twilightSeaCostumeId = "default";
                EditorUtility.SetDirty(theme);
            }
            ConnectMainMenu(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("ANIMOL Starwave Coast native: connected four character IDs, five appearance sets, 75 clips, and five illustrations while preserving twilight_sea keys.");
        }

        public static void BuildBatch() => Build();

        static void ConnectMainMenu(StarwayCharacterCatalog catalog)
        {
            var menu = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(MenuPath);
            if (!menu || menu.themes == null || menu.themes.Length < 4 || menu.themes[3].id != "sea")
                throw new InvalidDataException("Missing existing coast lobby entry");
            var entry = menu.themes[3];
            entry.displayName = "별너울 해안";
            entry.resourceId = "starwave_coast";
            entry.resourceAliases = new[] { "twilight_sea" };
            entry.gameCharacters = catalog;
            entry.people = new MainMenuPerson[Ids.Length];
            for (var i = 0; i < Ids.Length; i++)
            {
                var id = Ids[i];
                var person = new MainMenuPerson { characterId = id };
                person.appearances = id == "eden"
                    ? new[] {
                        new MainMenuAppearance { artId = "eden", costumeId = "rabbit",
                            illustration = catalog.Find("eden").IllustrationFor("rabbit") },
                        new MainMenuAppearance { artId = "edenpuppy", costumeId = "puppy",
                            illustration = catalog.Find("eden").IllustrationFor("puppy") }
                    }
                    : new[] { new MainMenuAppearance { artId = id, costumeId = "default",
                        illustration = catalog.Find(id).illustration } };
                entry.people[i] = person;
            }
            EditorUtility.SetDirty(menu);

            var previous = EditorSceneManager.GetActiveScene();
            if (previous.IsValid() && previous.isDirty)
                throw new InvalidOperationException("Save the active scene before updating the lobby theme caption.");
            var lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var changed = 0;
            foreach (var label in Resources.FindObjectsOfTypeAll<Text>())
                if (label.gameObject.scene == lobby && (label.text == "황혼의 바다" || label.text == "별너울 해안"))
                {
                    label.text = "별너울 해안";
                    EditorUtility.SetDirty(label);
                    changed++;
                }
            if (changed == 0) throw new InvalidDataException("Coast lobby caption not found");
            EditorSceneManager.MarkSceneDirty(lobby);
            EditorSceneManager.SaveScene(lobby);
            if (previous.IsValid() && !string.IsNullOrEmpty(previous.path) && previous.path != LobbyScenePath)
                EditorSceneManager.OpenScene(previous.path, OpenSceneMode.Single);
        }

        static StarwayCharacterMotion[] LoadMotions(AnimolStarwaveCoastNativeImporter.Manifest manifest,
            string characterId, string assetSet, string[] clipNames)
        {
            var motions = new StarwayCharacterMotion[15];
            var index = 0;
            var names = clipNames ?? Array.ConvertAll(
                Array.FindAll(manifest.clips, data => data.character == characterId && data.assetSet == assetSet),
                data => data.name);
            if (names.Length != 15) throw new InvalidDataException("Expected 15 clip names for " + assetSet);
            foreach (var name in names)
            {
                var data = Array.Find(manifest.clips, clip => clip.name == name);
                if (data == null || data.character != characterId || data.assetSet != assetSet)
                    throw new InvalidDataException("Invalid costume clip: " + name);
                if (data.frames == null || data.frames.Length != 6)
                    throw new InvalidDataException("Expected six frames: " + data.name);
                var path = Root + "/Animations/" + characterId + "/" + data.name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (!clip) throw new FileNotFoundException("Missing playable clip", path);
                var motion = new StarwayCharacterMotion { clip = clip, fps = data.fps, loop = data.loop };
                for (var frame = 0; frame < 6; frame++)
                {
                    motion.frames[frame] = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + data.frames[frame]);
                    if (!motion.frames[frame])
                        throw new InvalidDataException("Missing frame: " + data.name + "/" + frame);
                }
                motions[index++] = motion;
            }
            if (index != 15) throw new InvalidDataException("Expected 15 clips for " + assetSet);
            return motions;
        }

        static Sprite LoadIllustration(string assetSet)
        {
            return LoadIllustrationPath("Illustrations/animol_twilight_sea_" + assetSet + "_illustration.png");
        }

        static Sprite LoadIllustrationPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || relativePath.Contains("..") ||
                !relativePath.StartsWith("Illustrations/", StringComparison.Ordinal))
                throw new InvalidDataException("Invalid illustration path: " + relativePath);
            var path = Root + "/" + relativePath;
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
            var illustration = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!illustration) throw new InvalidDataException("Missing illustration: " + path);
            return illustration;
        }
    }
}
