using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Menu;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using Animol.Art.StarwaveCoastNative;
using Animol.Art.CoastRetouch.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public class TwilightSeaCharacterArtTests
    {
        const string Root = "Assets/ANIMOL/Art/StarwaveCoastCharacters";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/TwilightSeaCharacters.asset";

        [Test] public void RetouchManifestConnects540SpritesAnd90ClipsAtCalibratedSizeAndPivot()
        {
            var manifest = JsonUtility.FromJson<AnimolStarwaveCoastNativeImporter.Manifest>(
                File.ReadAllText(Root + "/starwave_coast_native_animations.json"));
            Assert.That(manifest.version, Is.EqualTo(7));
            Assert.That(manifest.nativeResolution, Is.True);
            Assert.That(manifest.frames.Length, Is.EqualTo(540));
            Assert.That(manifest.clips.Length, Is.EqualTo(90));
            var frameMap = manifest.frames.ToDictionary(frame => frame.file);
            var paths = new HashSet<string>();
            foreach (var data in manifest.clips)
            {
                Assert.That(data.frames.Length, Is.EqualTo(6), data.name);
                if (data.state == "push")
                {
                    Assert.That(data.loop, Is.True, data.name);
                    Assert.That(data.fps, Is.GreaterThanOrEqualTo(10f), data.name);
                }
                Assert.That(new[] { "haerin", "stranger", "danu", "eden" }, Does.Contain(data.character), data.name);
                if (data.assetSet == "edenpuppy")
                {
                    Assert.That(data.character, Is.EqualTo("eden"), data.name);
                    Assert.That(data.costume, Is.EqualTo("puppy"), data.name);
                    Assert.That(data.name, Does.StartWith("edenpuppy_"), data.name);
                }
                var clipPath = Root + "/Animations/" + data.character + "/" + data.name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                Assert.That(clip, Is.Not.Null, clipPath);
                Assert.That(clip.legacy, Is.False, clipPath);
                Assert.That(clip.frameRate, Is.EqualTo(data.fps), clipPath);
                Assert.That(clip.wrapMode, Is.EqualTo(data.loop ? WrapMode.Loop : WrapMode.Once), clipPath);
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Assert.That(bindings.Length, Is.EqualTo(1), clipPath);
                Assert.That(bindings[0].path, Is.Empty, clipPath);
                Assert.That(bindings[0].type, Is.EqualTo(typeof(SpriteRenderer)), clipPath);
                Assert.That(bindings[0].propertyName, Is.EqualTo("m_Sprite"), clipPath);
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                Assert.That(keys.Length, Is.EqualTo(7), clipPath);
                for (var index = 0; index < 6; index++)
                {
                    var relative = data.frames[index];
                    var path = Root + "/" + relative;
                    Assert.That(paths.Add(path), Is.True, path);
                    var metadata = frameMap[relative];
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    Assert.That(sprite, Is.Not.Null, path);
                    Assert.That(keys[index].value, Is.SameAs(sprite), path);
                    Assert.That(keys[index].time, Is.EqualTo(index / data.fps).Within(.00001f), path);
                    Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(metadata.width, metadata.height)), path);
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(metadata.pixelsPerUnit).Within(.01f), path);
                    Assert.That(sprite.pivot.x, Is.EqualTo(metadata.pivot[0] * metadata.width).Within(.1f), path);
                    Assert.That(sprite.pivot.y, Is.EqualTo(metadata.pivot[1] * metadata.height).Within(.1f), path);
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                    Assert.That(importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                    foreach (var platform in new[] { "Android", "iPhone", "Standalone", "WebGL" })
                        Assert.That(importer.GetPlatformTextureSettings(platform).overridden, Is.False, path + "/" + platform);
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
                }
                Assert.That(keys[6].value, Is.SameAs(keys[5].value), clipPath);
            }
            Assert.That(paths.Count, Is.EqualTo(540));
        }

        [Test] public void FourIdsAndFiveAppearanceSetsConnectToThemesAndLobby()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            var manifest = JsonUtility.FromJson<AnimolStarwaveCoastNativeImporter.Manifest>(
                File.ReadAllText(Root + "/starwave_coast_native_animations.json"));
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.defaultCharacterId, Is.EqualTo("haerin"));
            Assert.That(catalog.characters.Select(item => item.id),
                Is.EqualTo(new[] { "haerin", "stranger", "danu", "eden" }));
            foreach (var entry in catalog.characters)
            {
                Assert.That(entry.illustration, Is.Not.Null, entry.id);
                CheckIllustration(entry.illustration);
                CheckMotions(entry.id, entry.id, entry.motions, manifest);
                Assert.That(entry.coastPushContacts, Is.Not.Null, entry.id);
                Assert.That(entry.coastPushContacts.characterId, Is.EqualTo(entry.id));
                Assert.That(entry.coastPushContacts.assetSet, Is.EqualTo(entry.id));
                Assert.That(entry.coastPushContacts.entries.Length, Is.EqualTo(18));
                Assert.That(entry.coastGraphicsScaleXFactor, Is.InRange(.9f, 1f));
                Assert.That(entry.FirstFrame("front", "idle").rect.height /
                    entry.FirstFrame("front", "idle").pixelsPerUnit * .85f,
                    Is.EqualTo(entry.calibratedStandingHeight).Within(.001f), entry.id);
            }
            var eden = catalog.Find("eden");
            Assert.That(eden.defaultCostumeId, Is.EqualTo("rabbit"));
            Assert.That(eden.costumes.Length, Is.EqualTo(1));
            Assert.That(eden.costumes[0].id, Is.EqualTo("puppy"));
            Assert.That(eden.costumes[0].assetSet, Is.EqualTo("edenpuppy"));
            Assert.That(eden.costumes[0].coastPushContacts, Is.Not.Null);
            Assert.That(eden.costumes[0].coastPushContacts.characterId, Is.EqualTo("eden"));
            Assert.That(eden.costumes[0].coastPushContacts.assetSet, Is.EqualTo("edenpuppy"));
            Assert.That(eden.costumes[0].coastPushContacts.costume, Is.EqualTo("puppy"));
            Assert.That(eden.costumes[0].coastPushContacts.entries.Length, Is.EqualTo(18));
            CheckIllustration(eden.costumes[0].illustration);
            CheckMotions("eden", "edenpuppy", eden.costumes[0].motions, manifest);
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
            }
            var lobby = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>("Assets/ANIMOL/Data/UI/MainMenuCatalog.asset");
            Assert.That(lobby.themes[3].id, Is.EqualTo("sea"));
            Assert.That(lobby.themes[3].displayName, Is.EqualTo("\uBCC4\uB108\uC6B8 \uD574\uC548"));
            Assert.That(lobby.FindThemeByResourceId("starwave_coast"), Is.SameAs(lobby.themes[3]));
            Assert.That(lobby.FindThemeByResourceId("twilight_sea"), Is.SameAs(lobby.themes[3]));
            Assert.That(lobby.themes[3].people.Length, Is.EqualTo(4));
            Assert.That(lobby.FindPerson(3, "eden").appearances.Length, Is.EqualTo(2));
            Assert.That(lobby.FindPerson(3, "eden").appearances.Select(item => item.artId),
                Is.EqualTo(new[] { "eden", "edenpuppy" }));
            Assert.That(lobby.FindPerson(3, "eden").appearances[1].illustration,
                Is.SameAs(eden.IllustrationFor("puppy")));
        }

        [Test] public void NinetyCoastPushContactsUseOpaquePalmsAndCalibratedSprites()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            var count = 0;
            foreach (var assetSet in new[] { "haerin", "stranger", "danu", "eden", "edenpuppy" })
            {
                var id = assetSet == "edenpuppy" ? "eden" : assetSet;
                var costume = assetSet == "edenpuppy" ? "puppy" : assetSet == "eden" ? "rabbit" : "default";
                var contacts = catalog.Find(id).CoastContactsFor(costume);
                Assert.That(contacts, Is.Not.Null, assetSet);
                Assert.That(contacts.characterId, Is.EqualTo(id), assetSet);
                Assert.That(contacts.assetSet, Is.EqualTo(assetSet));
                Assert.That(contacts.costume, Is.EqualTo(costume));
                Assert.That(contacts.entries.Length, Is.EqualTo(18));
                var manifest = JsonUtility.FromJson<CoastRetouchImporter.Manifest>(File.ReadAllText(
                    Root + "/coast_" + assetSet + "_retouch_animations.json"));
                foreach (var frame in manifest.frames.Where(item => item.state == "push"))
                {
                    var path = Root + "/" + frame.file;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    Assert.That(contacts.TryGet(sprite, out var anchor), Is.True, path);
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

        static void CheckMotions(string character, string assetSet, StarwayCharacterMotion[] motions,
            AnimolStarwaveCoastNativeImporter.Manifest manifest)
        {
            Assert.That(motions.Length, Is.EqualTo(18), assetSet);
            foreach (var motion in motions)
            {
                Assert.That(motion.clip, Is.Not.Null, assetSet);
                var data = Array.Find(manifest.clips, clip => clip.name == motion.clip.name);
                Assert.That(data, Is.Not.Null, motion.clip.name);
                Assert.That(data.character, Is.EqualTo(character), data.name);
                Assert.That(data.assetSet, Is.EqualTo(assetSet), data.name);
                Assert.That(motion.fps, Is.EqualTo(data.fps), data.name);
                Assert.That(motion.loop, Is.EqualTo(data.loop), data.name);
                Assert.That(motion.frames.Length, Is.EqualTo(6), data.name);
                for (var index = 0; index < 6; index++)
                    Assert.That(motion.frames[index], Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(
                        Root + "/" + data.frames[index])), data.name + "/" + index);
            }
        }

        static void CheckIllustration(Sprite sprite)
        {
            Assert.That(sprite, Is.Not.Null);
            var path = AssetDatabase.GetAssetPath(sprite);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.GetPlatformTextureSettings("Android").format,
                Is.EqualTo(TextureImporterFormat.RGBA32), path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
        }
    }
}
