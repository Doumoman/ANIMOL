using System;
using System.IO;
using System.Linq;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using Animol.Art.TwilightSea160;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public class TwilightSeaCharacterArtTests
    {
        const string Root = "Assets/ANIMOL/Art/TwilightSeaCharacters160";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/TwilightSeaCharacters.asset";

        [Test] public void V5ManifestImportsAll75ClipsAnd450FramesForVisualSpriteRenderer()
        {
            var manifest = JsonUtility.FromJson<AnimolTwilightSea160Importer.Manifest>(
                File.ReadAllText(Root + "/twilight_sea_animations.json"));
            Assert.That(manifest.version, Is.EqualTo(5));
            Assert.That(manifest.outfitRevision, Is.EqualTo(5));
            Assert.That(manifest.clips.Length, Is.EqualTo(75));
            var paths = new System.Collections.Generic.HashSet<string>();
            foreach (var data in manifest.clips)
            {
                Assert.That(data.frames.Length, Is.EqualTo(6), data.name);
                Assert.That(new[] { "haerin", "stranger", "danu", "eden" }.Contains(data.character),
                    Is.True, data.name);
                if (data.assetSet == "edenpuppy")
                {
                    Assert.That(data.character, Is.EqualTo("eden"), data.name);
                    Assert.That(data.costume, Is.EqualTo("puppy"), data.name);
                }
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Animations/" +
                    data.character + "/" + data.name + ".anim");
                Assert.That(clip, Is.Not.Null, data.name);
                Assert.That(clip.legacy, Is.False, data.name);
                Assert.That(clip.frameRate, Is.EqualTo(data.fps), data.name);
                Assert.That(clip.wrapMode, Is.EqualTo(data.loop ? WrapMode.Loop : WrapMode.Once), data.name);
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Assert.That(bindings.Length, Is.EqualTo(1), data.name);
                Assert.That(bindings[0].path, Is.Empty, data.name);
                Assert.That(bindings[0].type, Is.EqualTo(typeof(SpriteRenderer)), data.name);
                Assert.That(bindings[0].propertyName, Is.EqualTo("m_Sprite"), data.name);
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                Assert.That(keys.Length, Is.EqualTo(7), data.name);
                for (var frame = 0; frame < 6; frame++)
                {
                    var path = Root + "/" + data.frames[frame];
                    Assert.That(paths.Add(path), Is.True, path);
                    Assert.That(Path.GetFileNameWithoutExtension(path).EndsWith("_" + frame.ToString("00")), Is.True, path);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    Assert.That(sprite, Is.Not.Null, path);
                    Assert.That(keys[frame].value, Is.SameAs(sprite), path);
                    Assert.That(keys[frame].time, Is.EqualTo(frame / data.fps).Within(.00001f), path);
                    Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(160, 160)), path);
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(160), path);
                    Assert.That(sprite.pivot, Is.EqualTo(new Vector2(80, 20)), path);
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                    Assert.That(importer.crunchedCompression || importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
                    var android = importer.GetPlatformTextureSettings("Android");
                    Assert.That(android.overridden && android.format == TextureImporterFormat.RGBA32 &&
                        android.maxTextureSize >= 160 && !android.crunchedCompression, Is.True, path);
                }
                Assert.That(keys[6].value, Is.SameAs(keys[5].value), data.name);
            }
            Assert.That(paths.Count, Is.EqualTo(450));
        }

        [Test] public void FourCanonicalIdsAndEdenCostumesConnectOnlyTwilightSeaThemes()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.defaultCharacterId, Is.EqualTo("haerin"));
            Assert.That(catalog.characters.Select(item => item.id),
                Is.EqualTo(new[] { "haerin", "stranger", "danu", "eden" }));
            foreach (var entry in catalog.characters)
            {
                Assert.That(entry.illustration, Is.Not.Null, entry.id);
                Assert.That(entry.motions.Length, Is.EqualTo(15), entry.id);
                Assert.That(entry.motions.All(m => m != null && m.clip && m.frames.Length == 6 &&
                    m.frames.All(f => f)), Is.True, entry.id);
            }
            var eden = catalog.Find("eden");
            Assert.That(eden.defaultCostumeId, Is.EqualTo("rabbit"));
            Assert.That(eden.costumes.Length, Is.EqualTo(1));
            Assert.That(eden.costumes[0].id, Is.EqualTo("puppy"));
            Assert.That(eden.costumes[0].assetSet, Is.EqualTo("edenpuppy"));
            Assert.That(eden.costumes[0].illustration, Is.Not.Null);
            Assert.That(eden.costumes[0].motions.Length, Is.EqualTo(15));
            Assert.That(eden.Motion("front", "idle", "rabbit").clip.name, Is.EqualTo("eden_front_idle"));
            Assert.That(eden.Motion("front", "idle", "puppy").clip.name, Is.EqualTo("edenpuppy_front_idle"));
            Assert.That(catalog.Find("edenpuppy"), Is.Null);
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset",
                "Assets/ANIMOL/Data/Art/TwilightCoast/PuzzleTheme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                Assert.That(theme.twilightSeaCharacters, Is.SameAs(catalog), path);
                Assert.That(theme.CharacterId, Is.EqualTo("haerin"), path);
                Assert.That(theme.CharacterCostumeId, Is.EqualTo("default"), path);
                Assert.That(theme.player, Is.Not.Null, path);
            }
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset",
                "Assets/ANIMOL/Data/Art/MemorySkyTheater/PuzzleTheme.asset" })
                Assert.That(AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path).twilightSeaCharacters,
                    Is.Null, path);
        }
    }
}
