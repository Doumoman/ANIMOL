using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ANIMOL.Menu;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Menu.Tests
{
    public class MainLobbyArtTests
    {
        const string Art = "Assets/ANIMOL/Sprites/UI/Main";
        const string Catalog = "Assets/ANIMOL/Data/UI/MainMenuCatalog.asset";

        [Test] public void FourThemeCatalogKeepsFiveSunsetSanctumCharacters()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(Catalog);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(Array.ConvertAll(catalog.themes, t => t.id), Is.EqualTo(new[] { "starway", "theater", "observatory", "sea" }));
            var people = catalog.themes.SelectMany(t => t.people).ToArray();
            Assert.That(people.Length, Is.EqualTo(17));
            Assert.That(people.SelectMany(p => p.appearances).Count(), Is.EqualTo(18));
            Assert.That(people.Count(p => p.characterId == "eden"), Is.EqualTo(1));
            Assert.That(people.Count(p => p.characterId == "doyun"), Is.EqualTo(1));
            Assert.That(people.Any(p => p.characterId == "edenpuppy"), Is.False);
            foreach (var theme in catalog.themes)
            {
                Assert.That(theme.people.Length, Is.EqualTo(theme.id == "observatory" ? 5 : 4), theme.id);
                Assert.That(theme.scenery, Is.Not.Null, theme.id);
                Assert.That(theme.gameCharacters, Is.Not.Null, theme.id);
                foreach (var person in theme.people)
                {
                    Assert.That(theme.gameCharacters.Find(person.characterId), Is.Not.Null, person.characterId);
                    foreach (var appearance in person.appearances)
                        Assert.That(appearance.illustration, Is.Not.Null, appearance.artId);
                }
            }
            var eden = catalog.FindPerson(3, "eden");
            Assert.That(eden.appearances.Select(a => a.artId), Is.EqualTo(new[] { "eden", "edenpuppy" }));
            Assert.That(eden.appearances.Select(a => a.costumeId), Is.EqualTo(new[] { "rabbit", "puppy" }));
            Assert.That(catalog.FindPerson(2, "soyul"), Is.Not.Null);
            Assert.That(catalog.FindPerson(2, "doyun"), Is.Not.Null);
            Assert.That(catalog.themes[2].displayName, Is.EqualTo("낙일 성소"));
            Assert.That(catalog.requiredStageIds, Is.Empty, "No authoritative campaign required-stage policy is present");
        }

        [Test] public void FullResolutionMenuSpritesKeepOriginalPixelsAndAndroidEdges()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(Catalog);
            foreach (var theme in catalog.themes)
                foreach (var person in theme.people)
                    foreach (var appearance in person.appearances)
                        Check(appearance.illustration, theme.id == "observatory" && person.characterId == "doyun"
                            ? new Vector2(1199, 1312) : new Vector2(1024, 1536));
            foreach (var sprite in catalog.themes.Select(t => t.scenery))
                Check(sprite, new Vector2(1122, 1402));
            Assert.That(AssetDatabase.FindAssets("t:SpriteAtlas", new[] { Art }), Is.Empty);
        }

        static void Check(Sprite sprite, Vector2 size)
        {
            var path = AssetDatabase.GetAssetPath(sprite);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(sprite.rect.size, Is.EqualTo(size), path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.crunchedCompression, Is.False, path);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
            Assert.That(importer.maxTextureSize, Is.GreaterThanOrEqualTo(1536), path);
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
            var android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden && android.format == TextureImporterFormat.RGBA32 &&
                android.maxTextureSize >= 1536 && !android.crunchedCompression, Is.True, path);
        }

        [Test] public void SixRevisedMastersMatchTheAcceptedV7Package()
        {
            var accepted = new[] {
                ("mion", "1b1217cfe1490aa63edf41cc3892953a1df9ac047cea1349366ee8c11227e34f"),
                ("piko", "41cac9c2dc691a5229025eb0785123f359d1ebf54a5302831dab18a5a167b4b8"),
                ("lumi", "82849074e65ee556c22e04883c11ebd97033a8880c156ed076985cc2e5734e36"),
                ("soyul", "c1e7abf313774becca62a3162f21c67a315c50205c4e32aefbf2d7cc2455e1b7"),
                ("haerin", "4a4a4180d89f3cb8186be86f848c00d1eb8aa0f55b8a0ceba6888e095cabe25e"),
                ("stranger", "ca12e55175517cac6404a53a0be76c0dd726edf9e1b0baede4df2907ba6cc487")
            };
            using var sha = SHA256.Create();
            foreach (var (id, expected) in accepted)
            {
                var path = Art + "/Illustrations/" + id + ".png";
                var actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
                Assert.That(actual, Is.EqualTo(expected), id);
            }
        }

        [Test] public void LobbyIsTheFirstBuildSceneWithoutRemovingPriorQaScene()
        {
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo("Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity"));
            Assert.That(EditorBuildSettings.scenes[0].enabled, Is.True);
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == "Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Art_QA.unity"), Is.True);
        }
    }
}
