using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using Animol.Art.SunsetSanctumNative;
using Animol.Art.SanctumRetouch.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public class SunsetSanctumCharacterArtTests
    {
        const string Root = "Assets/ANIMOL/Art/SunsetSanctumCharacters";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/LetterObservatoryCharacters.asset";

        [Test] public void RetouchManifestConnects540SpritesAnd90ClipsAtCalibratedSizeAndPivot()
        {
            var manifest = JsonUtility.FromJson<AnimolSunsetSanctumNativeImporter.Manifest>(
                File.ReadAllText(Root + "/sunset_sanctum_native_animations.json"));
            Assert.That(manifest.nativeResolution, Is.True);
            Assert.That(manifest.version, Is.EqualTo(8));
            Assert.That(manifest.frames.Length, Is.EqualTo(540));
            Assert.That(manifest.clips.Length, Is.EqualTo(90));
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
                if (data.state == "push")
                { Assert.That(data.fps, Is.GreaterThanOrEqualTo(10), path); Assert.That(data.loop, Is.True, path); }
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
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), spritePath);
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
            Assert.That(paths.Count, Is.EqualTo(540));
        }

        [Test] public void FiveCharactersRemainSeparateUnderLegacyThemeId()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            Assert.That(catalog.defaultCharacterId, Is.EqualTo("sein"));
            Assert.That(catalog.characters.Select(entry => entry.id),
                Is.EqualTo(new[] { "sein", "daon", "lumi", "soyul", "doyun" }));
            Assert.That(catalog.characters.Select(entry => entry.displayName),
                Is.EqualTo(new[] { "\uC138\uC778", "\uB2E4\uC628", "\uB8E8\uBBF8", "\uC18C\uC728", "\uB3C4\uC724" }));
            foreach (var entry in catalog.characters)
            {
                Assert.That(entry.illustration, Is.Not.Null, entry.id);
                Assert.That(entry.motions.Length, Is.EqualTo(18), entry.id);
                Assert.That(entry.sanctumPushContacts, Is.Not.Null, entry.id);
                Assert.That(entry.sanctumPushContacts.characterId, Is.EqualTo(entry.id));
                Assert.That(entry.sanctumPushContacts.entries.Length, Is.EqualTo(18), entry.id);
                Assert.That(entry.pushContacts, Is.Null, entry.id);
                Assert.That(entry.theaterPushContacts, Is.Null, entry.id);
                var standing = entry.FirstFrame("front", "idle");
                Assert.That(standing.rect.height / standing.pixelsPerUnit * .85f,
                    Is.EqualTo(entry.calibratedStandingHeight).Within(.001f), entry.id);
                Assert.That(entry.motions.All(motion => motion != null && motion.clip &&
                    motion.frames.Length == 6 && motion.frames.All(frame => frame)), Is.True, entry.id);
            }
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset");
            Assert.That(theme.letterObservatoryCharacters, Is.SameAs(catalog));
            Assert.That(theme.CharacterId, Is.EqualTo("sein"));
            Assert.That(theme.player, Is.Not.Null);
        }

        [Test] public void NinetyPushContactsPointToOpaquePalmsAtImportedPpuAndPivot()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            var count = 0;
            foreach (var entry in catalog.characters)
            {
                var manifest = JsonUtility.FromJson<SanctumRetouchImporter.Manifest>(File.ReadAllText(
                    Root + "/sanctum_" + entry.id + "_retouch_animations.json"));
                foreach (var frame in manifest.frames.Where(item => item.state == "push"))
                {
                    var path = Root + "/" + frame.file;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    Assert.That(entry.sanctumPushContacts.TryGet(sprite, out var anchor), Is.True, path);
                    Assert.That(frame.contact.confidence, Is.Not.EqualTo("fallback"), path);
                    var x = Mathf.RoundToInt(frame.contact.handPixel[0]);
                    var y = Mathf.RoundToInt(frame.contact.handPixel[1]);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    try
                    {
                        Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
                        Assert.That(x, Is.InRange(0, texture.width - 1), path);
                        Assert.That(y, Is.InRange(0, texture.height - 1), path);
                        Assert.That(texture.GetPixel(x, texture.height - 1 - y).a,
                            Is.GreaterThan(.75f), path);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                    var expected = new Vector2(
                        (frame.contact.handPixel[0] - sprite.pivot.x) / sprite.pixelsPerUnit,
                        (sprite.rect.height - sprite.pivot.y - frame.contact.handPixel[1]) / sprite.pixelsPerUnit);
                    Assert.That(anchor.handLocal.x, Is.EqualTo(expected.x).Within(.001f), path);
                    Assert.That(anchor.handLocal.y, Is.EqualTo(expected.y).Within(.001f), path);
                    count++;
                }
            }
            Assert.That(count, Is.EqualTo(90));
        }
    }
}
