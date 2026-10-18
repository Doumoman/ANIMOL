using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using Animol.Art.SunsetSanctumNative;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public class SunsetSanctumCharacterArtTests
    {
        const string Root = "Assets/ANIMOL/Art/SunsetSanctumCharacters";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/LetterObservatoryCharacters.asset";

        [Test] public void NativeManifestConnects450SpritesAnd75ClipsAtSourceSizeAndPivot()
        {
            var manifest = JsonUtility.FromJson<AnimolSunsetSanctumNativeImporter.Manifest>(
                File.ReadAllText(Root + "/sunset_sanctum_native_animations.json"));
            Assert.That(manifest.nativeResolution, Is.True);
            Assert.That(manifest.frames.Length, Is.EqualTo(450));
            Assert.That(manifest.clips.Length, Is.EqualTo(75));
            var frameMap = manifest.frames.ToDictionary(frame => frame.file);
            var paths = new HashSet<string>();
            foreach (var data in manifest.clips)
            {
                Assert.That(data.frames.Length, Is.EqualTo(6), data.name);
                var path = Root + "/Animations/" + data.character + "/" + data.name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                Assert.That(clip, Is.Not.Null, path);
                Assert.That(clip.frameRate, Is.EqualTo(data.fps), path);
                Assert.That(clip.wrapMode, Is.EqualTo(data.loop ? WrapMode.Loop : WrapMode.Once), path);
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Assert.That(bindings.Length, Is.EqualTo(1), path);
                Assert.That(bindings[0].path, Is.Empty, path);
                Assert.That(bindings[0].type, Is.EqualTo(typeof(SpriteRenderer)), path);
                Assert.That(bindings[0].propertyName, Is.EqualTo("m_Sprite"), path);
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                Assert.That(keys.Length, Is.EqualTo(7), path);
                for (var index = 0; index < 6; index++)
                {
                    var relative = data.frames[index];
                    var spritePath = Root + "/" + relative;
                    Assert.That(paths.Add(spritePath), Is.True, spritePath);
                    var metadata = frameMap[relative];
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                    Assert.That(sprite, Is.Not.Null, spritePath);
                    Assert.That(keys[index].value, Is.SameAs(sprite), spritePath);
                    Assert.That(keys[index].time, Is.EqualTo(index / data.fps).Within(.00001f), spritePath);
                    Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(metadata.width, metadata.height)), spritePath);
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(metadata.pixelsPerUnit).Within(.01f), spritePath);
                    Assert.That(sprite.pivot.x, Is.EqualTo(metadata.pivot[0] * metadata.width).Within(.1f), spritePath);
                    Assert.That(sprite.pivot.y, Is.EqualTo(metadata.pivot[1] * metadata.height).Within(.1f), spritePath);
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), spritePath);
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), spritePath);
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), spritePath);
                    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), spritePath);
                    Assert.That(importer.mipmapEnabled, Is.False, spritePath);
                    Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), spritePath);
                    Assert.That(importer.GetPlatformTextureSettings("Android").overridden, Is.False, spritePath);
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), spritePath);
                }
                Assert.That(keys[6].value, Is.SameAs(keys[5].value), path);
            }
            Assert.That(paths.Count, Is.EqualTo(450));
        }

        [Test] public void FiveCharactersRemainSeparateUnderLegacyThemeId()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            Assert.That(catalog.defaultCharacterId, Is.EqualTo("sein"));
            Assert.That(catalog.characters.Select(entry => entry.id),
                Is.EqualTo(new[] { "sein", "daon", "lumi", "soyul", "doyun" }));
            foreach (var entry in catalog.characters)
            {
                Assert.That(entry.illustration, Is.Not.Null, entry.id);
                Assert.That(entry.motions.Length, Is.EqualTo(15), entry.id);
                Assert.That(entry.motions.All(motion => motion != null && motion.clip &&
                    motion.frames.Length == 6 && motion.frames.All(frame => frame)), Is.True, entry.id);
            }
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset");
            Assert.That(theme.letterObservatoryCharacters, Is.SameAs(catalog));
            Assert.That(theme.CharacterId, Is.EqualTo("sein"));
            Assert.That(theme.player, Is.Not.Null);
        }
    }
}
