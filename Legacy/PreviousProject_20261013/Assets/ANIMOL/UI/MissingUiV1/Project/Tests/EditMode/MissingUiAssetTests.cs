using System.IO;
using ANIMOL.MissingUiV1.Project.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ANIMOL.MissingUiV1.Project.Tests
{
    public sealed class MissingUiAssetTests
    {
        [Test] public void All55SpritesKeepManifestDimensionsBordersAndFullRectPointImport()
        {
            var root=MissingUiProjectBuilder.Root;
            var manifest=JsonUtility.FromJson<MissingUiManifest>(File.ReadAllText(root+"/manifest.json"));
            Assert.That(manifest.assets.Length,Is.EqualTo(55));
            foreach(var a in manifest.assets)
            {
                var path=root+"/Sprites/"+a.path.Substring("sprites/".Length);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path); Assert.NotNull(importer,path);
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType,Is.EqualTo(SpriteMeshType.FullRect),path);
                Assert.That(importer.spriteImportMode,Is.EqualTo(SpriteImportMode.Single),path);
                Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point),path);
                Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed),path);
                Assert.IsFalse(importer.mipmapEnabled,path);
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture.width,Is.EqualTo(a.size[0]),path);Assert.That(texture.height,Is.EqualTo(a.size[1]),path);
                Assert.That(importer.spriteBorder,Is.EqualTo(new Vector4(a.borderLBRTop[0],a.borderLBRTop[1],a.borderLBRTop[2],a.borderLBRTop[3])),path);
            }
        }
        [Test] public void OperationalPrefabReusesArtFontAndHasNoPreviewOrInputOwner()
        {
            var store=AssetDatabase.LoadAssetAtPath<GameObject>(MissingUiProjectBuilder.ResourcesPath+"/Store.prefab");
            var view=store.GetComponent<MissingStoreView>();Assert.NotNull(view);
            Assert.IsEmpty(store.GetComponentsInChildren<Canvas>(true));Assert.IsEmpty(store.GetComponentsInChildren<EventSystem>(true));
            Assert.IsEmpty(store.GetComponentsInChildren<MissingUiScreenView>(true));
            Assert.That(AssetDatabase.GetAssetPath(view.Art.Panel),Does.StartWith("Assets/ANIMOL/UI/ProductionV1/Art/"));
            Assert.That(AssetDatabase.GetAssetPath(view.Art.Typography),Is.EqualTo("Assets/ANIMOL/Typography/PixelTypographyProfile.asset"));
        }
        [Test] public void IndependentPreviewRemainsReadOnly()
        { ANIMOL.MissingUiV1.Editor.MissingUiV1Builder.ValidateMissingUiV1(); }
    }
}
