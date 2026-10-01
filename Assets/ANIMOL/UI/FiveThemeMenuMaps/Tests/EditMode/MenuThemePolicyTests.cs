using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.FiveThemeMenu.Tests
{
    public class MenuThemePolicyTests
    {
        [Test] public void ImportedSpritesMatchManifestAndPixelSettings()
        {
            var m=JObject.Parse(File.ReadAllText("Docs/FiveThemeMenu/Source/manifest.json"));
            foreach(var p in ((JObject)m["assets"]).Properties()) {
                string path="Assets/ANIMOL/UI/FiveThemeMenuMaps/Sprites/"+p.Name;
                using(var sha=SHA256.Create()) Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant(),Is.EqualTo((string)p.Value["sha256"]),path);
                var i=(TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(i.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(i.spritePixelsPerUnit,Is.EqualTo(32));Assert.That(i.mipmapEnabled,Is.False);Assert.That(i.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
            }
            var c=Resources.Load<MenuThemeCatalog>("ANIMOLFiveThemeMenu");
            Assert.That(c.themes.Select(t=>t.id),Is.EqualTo(m["themeOrder"].Values<string>()));
            Assert.That(c.themes.Select(t=>t.cropTop),Is.EqualTo(new[]{52,52,48,39,33}));
            Assert.That(c.themes.All(t=>(t.footY-t.cropTop)*4==1488),Is.True);
            Assert.That(c.rabbitFrames.Length,Is.EqualTo(8));Assert.That(c.rabbitFrames.All(s=>s.rect.size==new Vector2(24,32)),Is.True);
        }
        [Test] public void FiveSecondSlotsAndHalfSecondFadeIncludeLoop()
        {
            for(int segment=0;segment<30;segment++) {
                Assert.That(MenuThemeCycle.IndexAt(segment*5),Is.EqualTo(segment%5));
                Assert.That(MenuThemeCycle.FadeAt(segment*5+4.5),Is.Zero);
                Assert.That(MenuThemeCycle.FadeAt(segment*5+4.75),Is.EqualTo(.5f).Within(.0001));
                Assert.That(MenuThemeCycle.FadeAt(segment*5+4.999),Is.GreaterThan(.997));
                Assert.That(MenuThemeCycle.FadeAt((segment+1)*5),Is.Zero);
            }
        }
        [Test] public void IncomingPanContinuesAtBoundaryAndNeverExposesEitherPortraitViewport()
        {
            var c=Resources.Load<MenuThemeCatalog>("ANIMOLFiveThemeMenu");
            for(int segment=0;segment<20;segment++) {
                Assert.That(MenuThemeCycle.PanAt(segment*5-.0001,segment),Is.EqualTo(MenuThemeCycle.PanAt(segment*5,segment)));
                for(double time=segment*5-.5;time<segment*5+5;time+=.025) {
                    var pan=MenuThemeCycle.PanAt(time,segment);int crop=c.themes[segment%5].cropTop;
                    Assert.That(Math.Abs(pan.x),Is.LessThanOrEqualTo(6));Assert.That(Math.Abs(pan.y),Is.LessThanOrEqualTo(4));
                    Assert.That(41-pan.x,Is.InRange(0,352-270));Assert.That(crop-pan.y,Is.InRange(0,704-600));
                }
            }
        }
    }
}
