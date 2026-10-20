using System;
using System.IO;
using ANIMOL.Menu;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Menu.Tests
{
    public class MainMenuSkinTests
    {
        const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";
        const string Root = "Assets/ANIMOL/Sprites/UI/Main";

        [Serializable] sealed class Manifest { public Spec[] sprites; }
        [Serializable] sealed class Spec { public string png; public int[] pixels; public int[] border; }

        [Test] public void IndependentUiSpritesKeepMeasuredPixelsAndSlicedBorders()
        {
            var json = File.ReadAllText("Assets/ANIMOL/Data/UI/MainMenuSpriteManifest.json");
            var manifest = JsonUtility.FromJson<Manifest>(json);
            Assert.That(manifest.sprites.Length, Is.EqualTo(21));
            foreach (var spec in manifest.sprites)
            {
                var path = Root + "/Chrome/" + Path.GetFileName(spec.png);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(spec.pixels[0], spec.pixels[1])), path);
                Assert.That(sprite.border, Is.EqualTo(new Vector4(spec.border[0], spec.border[1], spec.border[2], spec.border[3])), path);
                Assert.That(importer.maxTextureSize, Is.GreaterThanOrEqualTo(Math.Max(spec.pixels[0], spec.pixels[1])), path);
                Assert.That(importer.alphaIsTransparency && !importer.mipmapEnabled, Is.True, path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
            }
            var title = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Branding/william-paradox-title.png");
            var titleImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(title)) as TextureImporter;
            Assert.That(title.rect.size, Is.EqualTo(new Vector2(2060, 763)));
            Assert.That(titleImporter.maxTextureSize, Is.GreaterThanOrEqualTo(4096));
        }

        [Test] public void ExistingLobbyUsesOneSkinAndEditableTmpLabels()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MainLobbyController lobby = null;
            var events = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                lobby ??= root.GetComponentInChildren<MainLobbyController>(true);
                events += root.GetComponentsInChildren<EventSystem>(true).Length;
            }
            Assert.That(lobby, Is.Not.Null);
            Assert.That(events, Is.EqualTo(1));
            Assert.That(lobby.skin, Is.Not.Null);
            Assert.That(lobby.skin.font, Is.Not.Null);
            Assert.That(lobby.catalog.themes.Length, Is.EqualTo(4));
            Assert.That(lobby.playButton.GetComponent<Image>().sprite, Is.SameAs(lobby.skin.primary));
            Assert.That(((RectTransform)lobby.playButton.transform).sizeDelta,
                Is.EqualTo(new Vector2(-50, 44)));
            Assert.That(lobby.campaignButton.GetComponent<RectTransform>().rect.height,
                Is.EqualTo(44).Within(.1f));
            Assert.That(lobby.soundButton, Is.Not.Null);
            Assert.That(lobby.themeChecks.Length, Is.EqualTo(4));
            foreach (var node in lobby.canvas.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject),
                    Is.Zero, node.name);
            var visible = 0;
            foreach (var old in lobby.canvas.GetComponentsInChildren<Text>(true))
            {
                if (!old.gameObject.activeSelf) continue;
                Assert.That(old.enabled, Is.False, old.name);
                var mirror = old.GetComponent<MainMenuTextMirror>();
                Assert.That(mirror, Is.Not.Null, old.name);
                Assert.That(mirror.visibleText, Is.Not.Null, old.name);
                Assert.That(mirror.visibleText.font, Is.SameAs(lobby.skin.font), old.name);
                visible++;
            }
            Assert.That(visible, Is.GreaterThan(15));
            var title = lobby.logoRect.Find("William Paradox Title").GetComponent<Image>();
            Assert.That(title.sprite, Is.SameAs(lobby.skin.title));
            Assert.That(title.raycastTarget, Is.False);
        }
    }
}
