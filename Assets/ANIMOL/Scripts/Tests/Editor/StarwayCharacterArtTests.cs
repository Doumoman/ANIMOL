using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Menu;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using Animol.Art.StarwayNative;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public class StarwayCharacterArtTests
    {
        const string Root = "Assets/ANIMOL/Art/StarwayCharacters";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/StarwayCharacters.asset";

        [Test] public void NativeManifestConnects360SpritesAnd60ClipsAtSourceSizeAndPivot()
        {
            var manifest = JsonUtility.FromJson<AnimolStarwayNativeImporter.Manifest>(
                File.ReadAllText(Root + "/starway_native_animations.json"));
            Assert.That(manifest.nativeResolution, Is.True);
            Assert.That(manifest.frames.Length, Is.EqualTo(360));
            Assert.That(manifest.clips.Length, Is.EqualTo(60));
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
                    Assert.That(importer.GetPlatformTextureSettings("iPhone").overridden, Is.False, spritePath);
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), spritePath);
                }
                Assert.That(keys[6].value, Is.SameAs(keys[5].value), path);
            }
            Assert.That(paths.Count, Is.EqualTo(360));
        }

        [Test] public void FourIdsAndIsolDisplayNameConnectToThemesAndLobby()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            var manifest = JsonUtility.FromJson<AnimolStarwayNativeImporter.Manifest>(
                File.ReadAllText(Root + "/starway_native_animations.json"));
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.defaultCharacterId, Is.EqualTo("choa"));
            Assert.That(catalog.characters.Select(entry => entry.id),
                Is.EqualTo(new[] { "choa", "uram", "yeoul", "tori" }));
            Assert.That(catalog.Find("uram").displayName, Is.EqualTo("이솔"));
            foreach (var entry in catalog.characters)
            {
                Assert.That(entry.illustration, Is.Not.Null, entry.id);
                Assert.That(entry.motions.Length, Is.EqualTo(15), entry.id);
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
                "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                Assert.That(theme.starwayCharacters, Is.SameAs(catalog), path);
                Assert.That(theme.starwayCharacterId, Is.EqualTo("choa"), path);
            }
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset" })
                Assert.That(AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path).starwayCharacters, Is.Null, path);
            var lobby = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>("Assets/ANIMOL/Data/UI/MainMenuCatalog.asset");
            Assert.That(lobby.themes[0].id, Is.EqualTo("starway"));
            foreach (var entry in catalog.characters)
                Assert.That(lobby.FindPerson(0, entry.id).appearances[0].illustration,
                    Is.SameAs(entry.illustration), entry.id);
        }
    }
}
