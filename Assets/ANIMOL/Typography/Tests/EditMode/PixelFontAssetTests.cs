using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace ANIMOL.Typography.Tests
{
    public sealed class PixelFontAssetTests
    {
        private TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ANIMOL/Typography/Pixelroborobo_UI.asset");
        [Test] public void PrimaryIsSmallStaticRasterWithPointAndNoMipmaps()
        {
            Assert.That(Font.atlasPopulationMode,Is.EqualTo(AtlasPopulationMode.Static));
            Assert.That(Font.atlasRenderMode,Is.EqualTo(GlyphRenderMode.RASTER_HINTED));
            Assert.That(Font.faceInfo.pointSize,Is.EqualTo(16)); Assert.That(Font.characterTable.Count,Is.LessThan(1000));
            Assert.That(Font.atlasTextureCount,Is.EqualTo(1)); Assert.That(Font.atlasWidth,Is.EqualTo(1024));
            foreach(var asset in new[]{Font}.Concat(Font.fallbackFontAssetTable)) foreach(var atlas in asset.atlasTextures) {
                Assert.That(atlas.filterMode,Is.EqualTo(FilterMode.Point),asset.name); Assert.That(atlas.mipmapCount,Is.EqualTo(1),asset.name);
            }
        }
        [Test] public void CurrentCorpusAndEveryPrefabLabelHaveNoMissingGlyphs()
        {
            Assert.That(File.ReadAllText("Docs/PixelTypography/ui-corpus.txt").Where(c=>!Font.HasCharacter(c,true,true)),Is.Empty);
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ANIMOL/Prefabs/UI"})) {
                var path=AssetDatabase.GUIDToAssetPath(guid);
                foreach(var label in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Text>(true))
                    Assert.That(label.text.Where(c=>!char.IsControl(c)&&!Font.HasCharacter(c,true,true)),Is.Empty,path+"/"+label.name);
            }
        }
        [Test] public void UnlistedHangulUsesDynamicPixelFallbackWithoutGrowingStaticAtlas()
        {
            int before=Font.characterTable.Count;
            char missing=Enumerable.Range(0xAC00,11172).Select(c=>(char)c).First(c=>!Font.HasCharacter(c));
            Assert.That(Font.HasCharacter(missing,true,true),Is.True);
            Assert.That(Font.characterTable.Count,Is.EqualTo(before));
            Assert.That(Font.fallbackFontAssetTable[0].HasCharacter(missing),Is.True);
            Assert.That(Font.fallbackFontAssetTable[0].atlasPopulationMode,Is.EqualTo(AtlasPopulationMode.Dynamic));
            Assert.That(Font.fallbackFontAssetTable[0].isMultiAtlasTexturesEnabled,Is.True);
        }
    }
}
