using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Menu;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using Animol.Art.AfterimageTheaterNative;
using Animol.Art.TheaterRetouch.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public class MemoryTheaterCharacterArtTests
    {
        const string Root = "Assets/ANIMOL/Art/AfterimageTheaterCharacters";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/MemoryTheaterCharacters.asset";

        [Test] public void RetouchManifestConnects432SpritesAnd72ClipsAtCalibratedSizeAndPivot()
        {
            var manifest = JsonUtility.FromJson<AnimolAfterimageTheaterNativeImporter.Manifest>(
                File.ReadAllText(Root + "/afterimage_theater_native_animations.json"));
            Assert.That(manifest.version, Is.EqualTo(8));
            Assert.That(manifest.nativeResolution, Is.True);
            Assert.That(manifest.frames.Length, Is.EqualTo(432));
            Assert.That(manifest.clips.Length, Is.EqualTo(72));
            var frameMap = manifest.frames.ToDictionary(frame => frame.file);
            var paths = new HashSet<string>();
            foreach (var data in manifest.clips)
            {
                Assert.That(data.frames.Length, Is.EqualTo(6), data.name);
                var clipPath = Root + "/Animations/" + data.character + "/" + data.name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                Assert.That(clip, Is.Not.Null, clipPath);
                Assert.That(clip.legacy, Is.False, clipPath);
                Assert.That(clip.frameRate, Is.EqualTo(data.fps), clipPath);
                Assert.That(clip.wrapMode, Is.EqualTo(data.loop ? WrapMode.Loop : WrapMode.Once), clipPath);
                if (data.state == "push")
                { Assert.That(data.fps, Is.GreaterThanOrEqualTo(10), clipPath); Assert.That(data.loop, Is.True, clipPath); }
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
            Assert.That(paths.Count, Is.EqualTo(432));
        }

        [Test] public void FourIdsAndLegacyThemeAliasConnectToThemesAndLobby()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            var manifest = JsonUtility.FromJson<AnimolAfterimageTheaterNativeImporter.Manifest>(
                File.ReadAllText(Root + "/afterimage_theater_native_animations.json"));
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.defaultCharacterId, Is.EqualTo("seri"));
            Assert.That(catalog.characters.Select(entry => entry.id),
                Is.EqualTo(new[] { "seri", "roan", "mion", "piko" }));
            foreach (var entry in catalog.characters)
            {
                Assert.That(entry.illustration, Is.Not.Null, entry.id);
                var path = AssetDatabase.GetAssetPath(entry.illustration);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.GetPlatformTextureSettings("Android").format,
                    Is.EqualTo(TextureImporterFormat.RGBA32), path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
                Assert.That(entry.motions.Length, Is.EqualTo(18), entry.id);
                Assert.That(entry.theaterPushContacts, Is.Not.Null, entry.id);
                Assert.That(entry.theaterPushContacts.entries.Length, Is.EqualTo(18), entry.id);
                Assert.That(entry.pushContacts, Is.Null, entry.id);
                foreach (var motion in entry.motions)
                {
                    Assert.That(motion.clip, Is.Not.Null, entry.id);
                    var data = Array.Find(manifest.clips, clip => clip.name == motion.clip.name);
                    Assert.That(data, Is.Not.Null, motion.clip.name);
                    Assert.That(data.character, Is.EqualTo(entry.id), data.name);
                    Assert.That(motion.fps, Is.EqualTo(data.fps), data.name);
                    Assert.That(motion.loop, Is.EqualTo(data.loop), data.name);
                    Assert.That(motion.frames.Length, Is.EqualTo(6), data.name);
                    for (var index = 0; index < 6; index++)
                        Assert.That(motion.frames[index], Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(
                            Root + "/" + data.frames[index])), data.name + "/" + index);
                }
            }
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset",
                "Assets/ANIMOL/Data/Art/MemorySkyTheater/PuzzleTheme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                Assert.That(theme.memoryTheaterCharacters, Is.SameAs(catalog), path);
                Assert.That(theme.memoryTheaterCharacterId, Is.EqualTo("seri"), path);
            }
            var lobby = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>("Assets/ANIMOL/Data/UI/MainMenuCatalog.asset");
            Assert.That(lobby.themes[1].id, Is.EqualTo("theater"));
            Assert.That(lobby.themes[1].displayName, Is.EqualTo("잔영 극장"));
            Assert.That(lobby.FindThemeByResourceId("afterimage_theater"), Is.SameAs(lobby.themes[1]));
            Assert.That(lobby.FindThemeByResourceId("memory_theater"), Is.SameAs(lobby.themes[1]));
            foreach (var entry in catalog.characters)
                Assert.That(lobby.FindPerson(1, entry.id).appearances[0].illustration,
                    Is.SameAs(entry.illustration), entry.id);
        }

        [Test] public void IdleFeetStayRegisteredAcrossAllFramesAndDirections()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            var idle = new[] { "seri", "roan", "mion", "piko" }.SelectMany(character =>
                JsonUtility.FromJson<TheaterRetouchImporter.Manifest>(File.ReadAllText(
                    Root + "/theater_" + character + "_retouch_animations.json")).frames
                    .Where(frame => frame.state == "idle")).ToArray();
            Assert.That(idle.Length, Is.EqualTo(72));
            foreach (var character in new[] { "seri", "roan", "mion", "piko" })
            {
                var allFeet = new List<Vector2>();
                foreach (var direction in new[] { "front", "back", "side" })
                {
                    var feet = new List<Vector2>();
                    var silhouettes = new List<Vector2>();
                    foreach (var frame in idle.Where(item => item.character == character && item.direction == direction))
                    {
                        var path = Root + "/" + frame.file;
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        try
                        {
                            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
                            var pixels = texture.GetPixels32();
                            var width = texture.width;
                            var minY = texture.height;
                            var maxY = -1;
                            var minX = width;
                            var maxX = -1;
                            for (var index = 0; index < pixels.Length; index++)
                                if (pixels[index].a > 64)
                                {
                                    var x = index % width;
                                    var y = index / width;
                                    minX = Math.Min(minX, x);
                                    maxX = Math.Max(maxX, x);
                                    minY = Math.Min(minY, y);
                                    maxY = Math.Max(maxY, y);
                                }
                            Assert.That(maxY, Is.GreaterThanOrEqualTo(0), path);
                            var band = Math.Max(8, Mathf.RoundToInt((maxY - minY + 1) * .1f));
                            double sumX = 0;
                            var count = 0;
                            for (var y = minY; y < Math.Min(texture.height, minY + band); y++)
                                for (var x = 0; x < width; x++)
                                    if (pixels[y * width + x].a > 64) { sumX += x; count++; }
                            Assert.That(count, Is.GreaterThan(0), path);
                            feet.Add(new Vector2(
                                ((float)(sumX / count) - sprite.pivot.x) * .85f / sprite.pixelsPerUnit,
                                (minY - sprite.pivot.y) * .85f / sprite.pixelsPerUnit));
                            silhouettes.Add(new Vector2(
                                ((minX + maxX + 1) * .5f - sprite.pivot.x) * .85f / sprite.pixelsPerUnit,
                                ((minY + maxY + 1) * .5f - sprite.pivot.y) * .85f / sprite.pixelsPerUnit));
                        }
                        finally { UnityEngine.Object.DestroyImmediate(texture); }
                    }
                    Assert.That(feet.Count, Is.EqualTo(6), character + "/" + direction);
                    Assert.That(feet.Max(foot => foot.x) - feet.Min(foot => foot.x), Is.LessThan(.002f),
                        character + "/" + direction + " foot X");
                    Assert.That(silhouettes.Max(center => center.x) - silhouettes.Min(center => center.x),
                        Is.LessThan(.04f), character + "/" + direction + " silhouette X");
                    Assert.That(silhouettes.Max(center => center.y) - silhouettes.Min(center => center.y),
                        Is.LessThan(.04f), character + "/" + direction + " silhouette Y");
                    allFeet.AddRange(feet);
                }
                Assert.That(allFeet.Max(foot => foot.y) - allFeet.Min(foot => foot.y), Is.LessThan(.002f),
                    character + " foot Y");
                var restY = catalog.Find(character).restingLocalPosition.y;
                Assert.That(Mathf.Abs(allFeet[0].y + restY), Is.LessThan(.02f), character + " world foot Y");
            }
        }
    }
}
