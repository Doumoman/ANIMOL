using System;
using System.IO;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace Animol.Art.TwilightSea160
{
    public static class TwilightSeaCharacterBuilder
    {
        const string Root = "Assets/ANIMOL/Art/TwilightSeaCharacters160";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/TwilightSeaCharacters.asset";
        static readonly string[] Ids = { "haerin", "stranger", "danu", "eden" };
        static readonly string[] Names = { "\uD574\uB9B0", "\uC774\uBC29\uC778", "\uB2E8\uC6B0", "\uC774\uB4E0" };

        [MenuItem("ANIMOL/Art/Connect Twilight Sea Characters")]
        public static void Build()
        {
            var manifest = JsonUtility.FromJson<AnimolTwilightSea160Importer.Manifest>(
                File.ReadAllText(Root + "/twilight_sea_animations.json"));
            if (manifest.version != 5 || manifest.outfitRevision != 5 || manifest.clips == null ||
                manifest.clips.Length != 75)
                throw new InvalidDataException("Twilight Sea character manifest must be v5 with 75 clips.");

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
                    defaultCostumeId = id == "eden" ? "rabbit" : "default"
                };
                entry.illustration = LoadIllustration(id);
                entry.motions = LoadMotions(manifest, id, id);
                if (id == "eden")
                    entry.costumes = new[] { new StarwayCharacterCostume
                    {
                        id = "puppy",
                        assetSet = "edenpuppy",
                        illustration = LoadIllustration("edenpuppy"),
                        motions = LoadMotions(manifest, "eden", "edenpuppy")
                    } };
                catalog.characters[i] = entry;
            }
            EditorUtility.SetDirty(catalog);
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset",
                "Assets/ANIMOL/Data/Art/TwilightCoast/PuzzleTheme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                if (!theme) throw new FileNotFoundException("Missing Twilight Sea theme", path);
                theme.twilightSeaCharacters = catalog;
                theme.twilightSeaCharacterId = "haerin";
                theme.twilightSeaCostumeId = "default";
                EditorUtility.SetDirty(theme);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("ANIMOL Twilight Sea v5: connected four character IDs, five appearance sets, 75 clips, five illustrations, and both Twilight Sea themes.");
        }

        public static void BuildBatch() => Build();

        static StarwayCharacterMotion[] LoadMotions(AnimolTwilightSea160Importer.Manifest manifest,
            string characterId, string assetSet)
        {
            var motions = new StarwayCharacterMotion[15];
            var index = 0;
            foreach (var data in manifest.clips)
            {
                if (data.character != characterId || data.assetSet != assetSet) continue;
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
            var path = Root + "/Illustrations/animol_twilight_sea_" + assetSet + "_illustration.png";
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
            var illustration = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!illustration) throw new InvalidDataException("Missing illustration: " + path);
            return illustration;
        }
    }
}
