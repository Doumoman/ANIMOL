using System;
using System.IO;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace Animol.Art.LetterObservatory160
{
    public static class LetterObservatoryCharacterBuilder
    {
        const string Root = "Assets/ANIMOL/Art/LetterObservatoryCharacters160";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/LetterObservatoryCharacters.asset";
        const string ThemePath = "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset";
        static readonly string[] Ids = { "sein", "daon", "lumi", "soyul" };
        static readonly string[] Names = { "\uC138\uC778", "\uB2E4\uC628", "\uB8E8\uBBF8", "\uC18C\uC728(\uAC00\uCE6D)" };

        [MenuItem("ANIMOL/Art/Connect Letter Observatory Characters")]
        public static void Build()
        {
            var manifest = JsonUtility.FromJson<AnimolLetterObservatory160Importer.Manifest>(
                File.ReadAllText(Root + "/letter_observatory_animations.json"));
            if (manifest.version != 2 || manifest.outfitRevision != 3 || manifest.clips == null ||
                manifest.clips.Length != 60)
                throw new InvalidDataException("Letter Observatory character manifest must be v2 with 60 clips.");

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
                var entry = new StarwayCharacterDefinition { id = id, displayName = Names[i] };
                var illustrationPath = Root + "/Illustrations/animol_letter_observatory_" + id + "_illustration.png";
                ConfigureIllustration(illustrationPath);
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
                if (index != 15) throw new InvalidDataException("Expected 15 clips for " + id);
                catalog.characters[i] = entry;
            }
            EditorUtility.SetDirty(catalog);
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(ThemePath);
            if (!theme) throw new FileNotFoundException("Missing Letter Observatory theme", ThemePath);
            theme.letterObservatoryCharacters = catalog;
            theme.letterObservatoryCharacterId = catalog.defaultCharacterId;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("ANIMOL Letter Observatory v2: connected sein, daon, lumi, soyul, 60 clips, four illustrations, and the playable Letter Observatory theme.");
        }

        public static void BuildBatch() => Build();

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
