using ANIMOL.Presentation.ShutterOneTileV01;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class ShutterOneTileTests
    {
        const string Root = "Assets/ANIMOL_ECHO_THEATER_SHUTTER_1TILE_V01";

        [Test] public void InstalledArtAndThemeUseEightOneCellFrames()
        {
            var art = AssetDatabase.LoadAssetAtPath<ShutterOneTileArtV01>(
                Root + "/_Generated/ShutterOneTileArt.asset");
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset");
            Assert.That(art, Is.Not.Null);
            Assert.That(theme.oneTileShutterArt, Is.SameAs(art));
            Assert.That(art.Validate(out var error), Is.True, error);
            foreach (var direction in new[] { "horizontal", "vertical" })
            {
                var frames = direction == "horizontal" ? art.horizontal : art.vertical;
                for (var i = 0; i < 6; i++)
                {
                    var name = i < 4 ? "frame_" + i.ToString("00") : i == 4 ? "closed" : "open";
                    var path = Root + "/Sprites/" + direction + "/" + name + ".png";
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    Assert.That(importer, Is.Not.Null, path);
                    Assert.That(sprite, Is.Not.Null, path);
                    Assert.That(sprite.texture.width, Is.EqualTo(256), path);
                    Assert.That(sprite.texture.height, Is.EqualTo(256), path);
                    Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(256, 256)), path);
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(256), path);
                    Assert.That(sprite.pivot, Is.EqualTo(new Vector2(128, 128)), path);
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                    Assert.That(importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                    foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
                        Assert.That(importer.GetPlatformTextureSettings(platform).overridden, Is.False,
                            path + " " + platform);
                    if (i < 4) Assert.That(frames[i], Is.SameAs(sprite), path);
                }
            }
        }

        [Test] public void RepeatedProgressUsesAbsoluteOneTileScale()
        {
            var art = AssetDatabase.LoadAssetAtPath<ShutterOneTileArtV01>(
                Root + "/_Generated/ShutterOneTileArt.asset");
            var anchor = new GameObject("Shutter tile anchor");
            var visual = new GameObject("Visual");
            try
            {
                anchor.transform.position = new Vector3(4, 5);
                visual.transform.SetParent(anchor.transform, false);
                var renderer = visual.AddComponent<SpriteRenderer>();
                var view = anchor.AddComponent<ShutterOneTileViewV01>();
                Assert.That(view.Configure(art, renderer, anchor.transform, 1f, out var error), Is.True, error);
                for (var i = 0; i < 20; i++)
                {
                    Assert.That(view.ShowOpenProgress(i % 2 == 0, (i % 4) / 3f), Is.True);
                    Assert.That(renderer.bounds.size.x, Is.EqualTo(1f).Within(.001f));
                    Assert.That(renderer.bounds.size.y, Is.EqualTo(1f).Within(.001f));
                    Assert.That(renderer.bounds.center, Is.EqualTo(anchor.transform.position));
                }
                anchor.transform.localScale = new Vector3(2f, .5f, 1f);
                Assert.That(view.ShowState(true, true), Is.True);
                Assert.That(renderer.bounds.size.x, Is.EqualTo(1f).Within(.001f));
                Assert.That(renderer.bounds.size.y, Is.EqualTo(1f).Within(.001f));
            }
            finally { Object.DestroyImmediate(anchor); }
        }
    }
}
