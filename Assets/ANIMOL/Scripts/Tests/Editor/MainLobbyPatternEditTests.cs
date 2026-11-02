using System.Linq;
using ANIMOL.Menu;
using DiagonalPattern;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Menu.Tests
{
    public class MainLobbyPatternEditTests
    {
        const string Root = "Assets/ANIMOL/Sprites/UI/Pattern";
        const string Scene = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";

        [Test] public void SixteenIndependentFacesAndStarKeepTheirTransparentCanvas()
        {
            var faces = AssetDatabase.FindAssets("t:Sprite", new[] { Root + "/Faces" })
                .Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
            Assert.That(faces.Length, Is.EqualTo(16));
            foreach (var path in faces.Append(Root + "/Ornaments/Star_FourPoint.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var size = path.Contains("/Faces/") ? 576 : 128;
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(size, size)), path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100), path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.maxTextureSize, Is.GreaterThanOrEqualTo(1024), path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(size / 2f, size / 2f)), path);
            }
            Assert.That(AssetDatabase.FindAssets("t:SpriteAtlas", new[] { Root }), Is.Empty);
        }

        [Test] public void OneRepeatTextureServesEightBlankSurfacesWithoutInterceptingTouch()
        {
            var path = Root + "/Textures/CharacterFacesStars_All_2048.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(texture.width, Is.EqualTo(2048));
            Assert.That(texture.height, Is.EqualTo(2048));
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Default));
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(importer.maxTextureSize, Is.GreaterThanOrEqualTo(2048));

            EditorSceneManager.OpenScene(Scene);
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            Assert.That(lobby, Is.Not.Null);
            var patterns = lobby.canvas.GetComponentsInChildren<DiagonalPatternBackground>(true);
            Assert.That(patterns.Length, Is.EqualTo(8));
            Assert.That(patterns.Select(p => p.GetComponent<RawImage>().texture).Distinct().Single(), Is.SameAs(texture));
            var opacities = patterns.Select(p => new SerializedObject(p).FindProperty("patternOpacity").floatValue)
                .OrderBy(value => value).ToArray();
            Assert.That(opacities, Is.EqualTo(new[] { .065f, .085f, .10f, .10f, .12f, .12f, .12f, .12f })
                .Within(.0001f));
            foreach (var pattern in patterns)
            {
                var raw = pattern.GetComponent<RawImage>();
                var data = new SerializedObject(pattern);
                Assert.That(raw.raycastTarget, Is.False);
                Assert.That(raw.color, Is.EqualTo(Color.white));
                Assert.That(raw.uvRect, Is.EqualTo(new Rect(0, 0, 1, 1)));
                Assert.That(pattern.transform.GetSiblingIndex(), Is.Zero);
                Assert.That(data.FindProperty("tileSize").vector2Value, Is.EqualTo(new Vector2(960, 960)));
                Assert.That(data.FindProperty("backgroundColor").colorValue.a, Is.Zero);
                var tint = data.FindProperty("patternTint").colorValue;
                Assert.That(tint.r, Is.EqualTo(166f / 255f).Within(.001f));
                Assert.That(tint.g, Is.EqualTo(110f / 255f).Within(.001f));
                Assert.That(tint.b, Is.EqualTo(72f / 255f).Within(.001f));
                Assert.That(data.FindProperty("useUnscaledTime").boolValue, Is.True);
                Assert.That(pattern.VisualVelocity, Is.EqualTo(new Vector2(8, 8)));
            }
            Assert.That(lobby.themeCards.Length, Is.EqualTo(4));
            Assert.That(lobby.backgroundImage.sprite, Is.SameAs(lobby.wideScenery[0]));
        }

        [Test] public void V18KeepsTitleOnArtAndTelescopeInsideCampaignOnly()
        {
            EditorSceneManager.OpenScene(Scene);
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            Assert.That(lobby.topPaper, Is.Not.Null);
            Assert.That(lobby.topPaper.parent, Is.SameAs(lobby.frame));
            Assert.That(lobby.topPaper.Find("Top Bar"), Is.Not.Null);
            Assert.That(lobby.logoRect.parent, Is.SameAs(lobby.artArea));
            Assert.That(lobby.dock.Find("Ad Row"), Is.Null);
            Assert.That(lobby.removeAdsButton, Is.Null);
            Assert.That(lobby.frame.GetComponent<RectMask2D>(), Is.Null);
            Assert.That(lobby.frame.Find("Campaign Telescope"), Is.Null);
            Assert.That(lobby.campaignPanel.transform.Find("Campaign Telescope"), Is.Not.Null);
            Assert.That(lobby.campaignPanel.activeSelf, Is.False);
            Assert.That(lobby.wideScenery.Length, Is.EqualTo(4));
            for (var i = 0; i < 4; i++)
            {
                var sprite = lobby.wideScenery[i];
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(1536, 1024)));
                Assert.That(sprite, Is.Not.SameAs(lobby.catalog.themes[i].scenery));
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            }
        }
    }
}
