using System;
using System.IO;
using System.Linq;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using Animol.Art.LetterObservatory160;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public class LetterObservatoryCharacterArtTests
    {
        const string Root = "Assets/ANIMOL/Art/LetterObservatoryCharacters160";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/LetterObservatoryCharacters.asset";

        [Test] public void V2ManifestImports60ClipsAnd360FramesForVisualSpriteRenderer()
        {
            var manifest = JsonUtility.FromJson<AnimolLetterObservatory160Importer.Manifest>(
                File.ReadAllText(Root + "/letter_observatory_animations.json"));
            Assert.That(manifest.version, Is.EqualTo(2));
            Assert.That(manifest.outfitRevision, Is.EqualTo(3));
            Assert.That(manifest.clips.Length, Is.EqualTo(60));
            var paths = new System.Collections.Generic.HashSet<string>();
            foreach (var data in manifest.clips)
            {
                Assert.That(data.frames.Length, Is.EqualTo(6), data.name);
                Assert.That(new[] { "sein", "daon", "lumi", "soyul" }.Contains(data.character), Is.True, data.name);
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
            Assert.That(paths.Count, Is.EqualTo(360));
        }

        [Test] public void FourIdsConnectOnlyLetterObservatoryAndSoyulDoesNotReplaceDoyun()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.defaultCharacterId, Is.EqualTo("sein"));
            Assert.That(catalog.characters.Select(item => item.id),
                Is.EqualTo(new[] { "sein", "daon", "lumi", "soyul" }));
            Assert.That(catalog.Find("doyun"), Is.Null);
            foreach (var entry in catalog.characters)
            {
                Assert.That(entry.illustration, Is.Not.Null, entry.id);
                Assert.That(entry.motions.Length, Is.EqualTo(15), entry.id);
                Assert.That(entry.motions.All(m => m != null && m.clip && m.frames.Length == 6 &&
                    m.frames.All(f => f)), Is.True, entry.id);
            }
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset");
            Assert.That(theme.letterObservatoryCharacters, Is.SameAs(catalog));
            Assert.That(theme.CharacterId, Is.EqualTo("sein"));
            Assert.That(theme.player, Is.Not.Null);
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset",
                "Assets/ANIMOL/Data/Art/MemorySkyTheater/PuzzleTheme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset",
                "Assets/ANIMOL/Data/Art/TwilightCoast/PuzzleTheme.asset" })
                Assert.That(AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path).letterObservatoryCharacters,
                    Is.Null, path);
        }
    }
}
