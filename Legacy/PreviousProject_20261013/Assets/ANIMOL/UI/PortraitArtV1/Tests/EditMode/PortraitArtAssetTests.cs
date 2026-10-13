using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace ANIMOL.PortraitArtV1.Tests
{
    public sealed class PortraitArtAssetTests
    {
        private const string Root="Assets/ANIMOL/UI/PortraitArtV1";
        [Test] public void SpritesMatchManifestBytesAndPixelImportSettings()
        {
            var manifest=JObject.Parse(File.ReadAllText("Docs/PortraitArtV1/Source/ui_manifest.json"));
            Assert.That(Directory.GetFiles(Root+"/Sprites","*.png").Length,Is.EqualTo(10));
            foreach(var p in ((JObject)manifest["sprites"]).Properties()) {
                string path=Root+"/Sprites/"+p.Name;
                using(var sha=SHA256.Create()) Assert.That(string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b=>b.ToString("x2"))),Is.EqualTo((string)p.Value["sha256"]));
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.Sprite));Assert.That(importer.spriteImportMode,Is.EqualTo(SpriteImportMode.Single));
                Assert.That(importer.spritePixelsPerUnit,Is.EqualTo(32));Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));
                Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);Assert.That(settings.spriteMeshType,Is.EqualTo(SpriteMeshType.FullRect));
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);Assert.That(sprite.rect.size,Is.EqualTo(V(p.Value["size_px"])));
            }
        }
        [Test] public void PrefabControlsMatchSafeAreaManifestWithoutAdditionalCanvasOrBakedText()
        {
            var manifest=JObject.Parse(File.ReadAllText("Docs/PortraitArtV1/Source/ui_manifest.json"));
            foreach(string name in new[]{"Start","ModeSelect"}) {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab");
                Assert.That(prefab.GetComponentsInChildren<Canvas>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<Text>(true),Is.Empty);
                var view=prefab.GetComponent<PortraitArtScreen>();Assert.That(view.CharacterBackdropSlot.childCount,Is.Zero);Assert.That(view.CharacterBackdropSlot.GetComponents<Graphic>(),Is.Empty);
                foreach(var p in ((JObject)manifest["screens"][name]["controls"]).Properties()) {
                    var rect=(RectTransform)prefab.transform.Find(p.Name);Assert.That(rect,Is.Not.Null);
                    Assert.That(rect.anchorMin,Is.EqualTo(V(p.Value["anchor"])));Assert.That(rect.anchorMax,Is.EqualTo(rect.anchorMin));
                    Assert.That(rect.pivot,Is.EqualTo(V(p.Value["pivot"])));Assert.That(rect.anchoredPosition,Is.EqualTo(V(p.Value["position"])));Assert.That(rect.sizeDelta,Is.EqualTo(V(p.Value["size"])));
                    Assert.That(rect.GetComponent<Image>().type,Is.EqualTo(Image.Type.Simple));Assert.That(rect.GetComponent<Image>().preserveAspect,Is.True);
                    if(p.Value["label"]!=null) Assert.That(rect.GetComponentInChildren<TMP_Text>().text,Is.EqualTo((string)p.Value["label"]));
                }
                if(name=="Start") Assert.That(prefab.GetComponentsInChildren<Button>().Length,Is.EqualTo(1));
            }
        }
        [Test] public void PackFontUsesRasterStaticCorpusAndDynamicHangulFallback()
        {
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Fonts/PortraitUI_Raster.asset");
            Assert.That(font.atlasPopulationMode,Is.EqualTo(AtlasPopulationMode.Static));Assert.That(font.atlasRenderMode,Is.EqualTo(GlyphRenderMode.RASTER_HINTED));
            Assert.That(font.faceInfo.pointSize,Is.EqualTo(16));Assert.That(font.characterTable.Count,Is.LessThan(1000));
            Assert.That(font.atlasTexture.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(font.atlasTexture.mipmapCount,Is.EqualTo(1));
            Assert.That(File.ReadAllText("Docs/PortraitArtV1/font-corpus.txt").Where(c=>!font.HasCharacter(c,true,true)),Is.Empty);
            var fallback=font.fallbackFontAssetTable[0];Assert.That(fallback.atlasPopulationMode,Is.EqualTo(AtlasPopulationMode.Dynamic));
            Assert.That(AssetDatabase.GetAssetPath(fallback.sourceFontFile),Is.EqualTo(Root+"/Fonts/pixelroborobo.otf"));
            char missing=Enumerable.Range(0xAC00,11172).Select(c=>(char)c).First(c=>!font.HasCharacter(c));
            int before=font.characterTable.Count;Assert.That(font.HasCharacter(missing,true,true),Is.True);Assert.That(font.characterTable.Count,Is.EqualTo(before));
        }
        private static Vector2 V(JToken token) => new Vector2((float)token[0],(float)token[1]);
    }
}
