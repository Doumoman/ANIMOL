using System.IO;
using System.Linq;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.Tests.EditMode
{
    public sealed class MoonGraphicsQaTests
    {
        [Test]
        public void SpecimenIsIsolatedAndPreservesCampaignMapOnLoad()
        {
            const string map="Assets/ANIMOL/Data/Campaign/Maps/T01-S01.asset";
            var before=File.ReadAllBytes(map);
            Assert.That(EditorBuildSettings.scenes.Any(s=>s.path==MoonGraphicsQaBuilder.ScenePath),Is.False);
            var scene=EditorSceneManager.OpenPreviewScene(MoonGraphicsQaBuilder.ScenePath);
            try
            {
                var roots=scene.GetRootGameObjects();
                Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<MoonGraphicsQaScene>(true)).Count(),Is.EqualTo(1));
                Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<DevPlayerController>(true)).Count(),Is.EqualTo(1));
                Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<SafeAreaLayout>(true)).Count(),Is.GreaterThan(0));
                Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<DevMobileInputRouter>(true)).Count(),Is.EqualTo(1));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Assert.That(File.ReadAllBytes(map),Is.EqualTo(before));
        }
        [Test]
        public void RefinedTerrainUsesAllNineDirectionsOnExact32PixelGeometry()
        {
            var scene=EditorSceneManager.OpenPreviewScene(MoonGraphicsQaBuilder.ScenePath);
            try
            {
                var objects=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true)).ToArray();
                foreach(var direction in new[]{"NW","N","NE","W","C","E","SW","S","SE"})
                    Assert.That(objects.Any(o=>o.name.EndsWith("QA_"+direction)),Is.True,direction);
                foreach(var mesh in objects.Select(o=>o.sharedMesh).Distinct())
                    foreach(var v in mesh.vertices)
                    { Assert.That(v.x*32,Is.EqualTo(Mathf.Round(v.x*32)).Within(.001f));Assert.That(v.y*32,Is.EqualTo(Mathf.Round(v.y*32)).Within(.001f)); }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
