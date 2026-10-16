using System;
using System.IO;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using Animol.Art.Starway160;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public class StarwayCharacterArtTests
    {
        const string Root = "Assets/ANIMOL/Art/StarwayCharacters160";

        [Test] public void V6ManifestFramesImportAndBindToVisualSpriteRenderer()
        {
            var manifest = JsonUtility.FromJson<AnimolStarway160Importer.Manifest>(File.ReadAllText(Root + "/starway_animations.json"));
            Assert.That(manifest.version, Is.EqualTo(6));
            Assert.That(manifest.clips.Length, Is.EqualTo(60));
            var count = 0;
            foreach (var data in manifest.clips)
            {
                Assert.That(data.frames.Length, Is.EqualTo(6), data.name);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Animations/" + data.character + "/" + data.name + ".anim");
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
                for (var f = 0; f < 6; f++)
                {
                    var path = Root + "/" + data.frames[f];
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    Assert.That(sprite, Is.Not.Null, path);
                    Assert.That(keys[f].value, Is.EqualTo(sprite), path);
                    Assert.That(keys[f].time, Is.EqualTo(f / data.fps).Within(.00001f), path);
                    Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(160, 160)), path);
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(160), path);
                    Assert.That(sprite.pivot, Is.EqualTo(new Vector2(80, 20)), path);
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                    Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                    Assert.That(importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.crunchedCompression, Is.False, path);
                    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
                    var android = importer.GetPlatformTextureSettings("Android");
                    Assert.That(android.overridden && android.format == TextureImporterFormat.RGBA32 &&
                        android.maxTextureSize >= 160 && !android.crunchedCompression, Is.True, path);
                    count++;
                }
                Assert.That(keys[6].value, Is.EqualTo(keys[5].value), data.name);
            }
            Assert.That(count, Is.EqualTo(360));
        }

        [Test] public void CatalogAndBothStarwayThemesReferenceAllFourIllustrationsAndClips()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>("Assets/ANIMOL/Data/Art/StarwayCharacters.asset");
            var manifest = JsonUtility.FromJson<AnimolStarway160Importer.Manifest>(File.ReadAllText(Root + "/starway_animations.json"));
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.defaultCharacterId, Is.EqualTo("choa"));
            Assert.That(Array.ConvertAll(catalog.characters, c => c.id), Is.EqualTo(new[] { "choa", "uram", "yeoul", "tori" }));
            foreach (var entry in catalog.characters)
            {
                Assert.That(entry.illustration, Is.Not.Null, entry.id);
                Assert.That(entry.motions.Length, Is.EqualTo(15), entry.id);
                for (var i = 0; i < 15; i++)
                {
                    var motion = entry.motions[i];
                    Assert.That(motion.clip, Is.Not.Null, entry.id);
                    var data = Array.Find(manifest.clips, clip => clip.name == motion.clip.name);
                    Assert.That(data, Is.Not.Null, motion.clip.name);
                    Assert.That(motion.fps, Is.EqualTo(data.fps), data.name);
                    Assert.That(motion.loop, Is.EqualTo(data.loop), data.name);
                    Assert.That(motion.frames.Length, Is.EqualTo(6), entry.id);
                    for (var frame = 0; frame < 6; frame++)
                        Assert.That(motion.frames[frame], Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + data.frames[frame])),
                            data.name + "/" + frame);
                }
            }
            foreach (var path in new[] { "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset", "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                Assert.That(theme.starwayCharacters, Is.SameAs(catalog), path);
                Assert.That(theme.starwayCharacterId, Is.EqualTo("choa"), path);
                Assert.That(theme.player, Is.Not.Null, path);
            }
            foreach (var path in new[] { "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                Assert.That(theme.starwayCharacters, Is.Null, path);
            }
        }
    }
}
