using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.FiveThemeMenu.Tests
{
    public class MenuThemePolicyTests
    {
        [Test] public void ActiveCorrectionImportsMatchSuppliedPngsAndCurrentRabbitGeometry()
        {
            var catalog=Resources.Load<FantasyBackgroundCatalog>("ANIMOLMainUiV7");
            var paths=catalog.themes.SelectMany(t=>new[]{t.far,t.mid,t.platform,t.near}).Concat(new[]{catalog.rabbitLeft,catalog.rabbitRight}).Select(AssetDatabase.GetAssetPath).ToArray();
            Assert.That(paths.Distinct().Count(),Is.EqualTo(22));
            foreach(var path in paths) {
                if(!path.Contains("rabbit")) {
                    Assert.That(path,Does.StartWith("Assets/ANIMOL/UI/OriginalDesignCorrectionV2/runtime/main-background/"));
                    Assert.That(File.ReadAllBytes(path),Is.EqualTo(File.ReadAllBytes("Tools/ArtSources/ANIMOL_Original_Design_Correction_v2/runtime/main-background/"+Path.GetFileName(path))));
                }
                var t=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                Assert.That(t.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(t.mipmapEnabled,Is.False);
                Assert.That(t.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));Assert.That(t.npotScale,Is.EqualTo(TextureImporterNPOTScale.None));
                foreach(string platform in new[]{"Standalone","Android","iPhone","WebGL"}) Assert.That(t.GetPlatformTextureSettings(platform).overridden,Is.False);
                var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(new Vector2Int(tex.width,tex.height),Is.EqualTo(path.Contains("rabbit")?new Vector2Int(512,96):new Vector2Int(352,704)));
                if(!path.Contains("rabbit")){Assert.That(t.spritePixelsPerUnit,Is.EqualTo(32));var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);Assert.That(settings.spriteMeshType,Is.EqualTo(SpriteMeshType.FullRect));}
                else {
                    var readable=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    try {
                        readable.LoadImage(File.ReadAllBytes(path));var pixels=readable.GetPixels32();
                        for(int frame=0;frame<8;frame++) {
                            int foot=-1;
                            for(int y=0;y<96;y++)for(int x=0;x<64;x++)if(pixels[(95-y)*512+frame*64+x].a>0)foot=Math.Max(foot,y);
                            Assert.That(foot,Is.EqualTo(frame==3||frame==7?86:90));
                        }
                    } finally {UnityEngine.Object.DestroyImmediate(readable);}
                }
            }
        }
        [Test] public void ExactSlotsFramesAndBinaryWipe()
        {
            for(int i=0;i<30;i++) {
                Assert.That(FantasyBackgroundPolicy.ThemeAt(i*5),Is.EqualTo(i%5));
                Assert.That(FantasyBackgroundPolicy.FrameAt(i/14.0+.000001),Is.EqualTo(i%8));
            }
            for(int y=0;y<704;y+=8) for(int x=0;x<352;x+=8) {
                Assert.That(FantasyBackgroundPolicy.NewBlock(x,y,0),Is.False);
                Assert.That(FantasyBackgroundPolicy.NewBlock(x,y,.36),Is.True);
                Assert.That(FantasyBackgroundPolicy.NewBlock(x,y,.18),Is.EqualTo(FantasyBackgroundPolicy.NewBlock(x+7,y+7,.18)));
            }
        }
        [Test] public void FixedGroundAndIndependentReproducibleDirections()
        {
            Assert.That(FantasyBackgroundPolicy.RabbitY+90,Is.EqualTo(526));
            Assert.That(FantasyBackgroundPolicy.RabbitX(0,1),Is.EqualTo(-64));Assert.That(FantasyBackgroundPolicy.RabbitX(1,1),Is.EqualTo(352));
            Assert.That(FantasyBackgroundPolicy.RabbitX(0,-1),Is.EqualTo(352));Assert.That(FantasyBackgroundPolicy.RabbitX(1,-1),Is.EqualTo(-64));
            var pairs=Enumerable.Range(0,100).Select(i=>FantasyBackgroundPolicy.Direction(i,91)+","+FantasyBackgroundPolicy.Direction(i,314)).Distinct().ToArray();
            Assert.That(pairs.Length,Is.EqualTo(4));
            for(int i=0;i<100;i++) Assert.That(FantasyBackgroundPolicy.Direction(i,91,42),Is.EqualTo(FantasyBackgroundPolicy.Direction(i,91,42)));
            Assert.That(FantasyBackgroundPolicy.Plane(1,1,1,0),Is.EqualTo(new RectInt(0,0,352,704)));
            Assert.That(FantasyBackgroundPolicy.Plane(1,1,1.6f,1.6f).y,Is.GreaterThan(FantasyBackgroundPolicy.Plane(0,1,1.6f,1.6f).y));
        }
    }
}
