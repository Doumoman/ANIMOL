using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ANIMOL.UI.Crayon;
using ANIMOL.UI.Crayon.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ANIMOL.Tests
{
    public sealed class CrayonUIAssetTests
    {
        [Test] public void AllPrepaintedSetsMatchTheManifestAndCompileWithoutShaderMessages()
        {
            Assert.That(CrayonUIAssets.ReadManifest().entries.Length, Is.EqualTo(18));
            Assert.That(CrayonUIAssets.Validate(), Is.Empty);
        }

        [Test] public void PreviewPrefabHasCompleteDecorationsAndPreservesSeparateInputGraphics()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BattleCrayonUIInstaller.PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var motion = prefab.GetComponent<CrayonUIMotion>();
            var preview = prefab.GetComponent<BattleCrayonArtPreview>();
            Assert.That(motion.bindings.Count, Is.GreaterThanOrEqualTo(40));
            foreach (var binding in motion.bindings)
            {
                Assert.That(binding.image && binding.animated && binding.still, Is.True);
                Assert.That(binding.image.sprite.name, Does.EndWith("_A"));
                Assert.That(binding.image.raycastTarget, Is.False);
                Assert.That(binding.image.type, Is.EqualTo(binding.image.sprite.name.StartsWith("Frame_")
                    ? Image.Type.Sliced : Image.Type.Simple));
            }
            foreach (var button in prefab.GetComponentsInChildren<Button>(true))
            {
                Assert.That(button.targetGraphic, Is.Not.Null, button.name);
                Assert.That(button.targetGraphic.raycastTarget, Is.True, button.name);
                Assert.That(button.onClick.GetPersistentEventCount(), Is.EqualTo(1), button.name);
                Assert.That(button.onClick.GetPersistentTarget(0), Is.SameAs(preview));
            }
            Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true), Has.None.Null);
            Assert.That(preview.isolHealthFill.sprite.name, Is.EqualTo("Fill_Isol"));
            Assert.That(preview.isolHealthFill.material.shader.name, Is.Not.EqualTo("UI/ANIMOL/CrayonFrameBoil"));
            Assert.That(EditorBuildSettings.scenes.Select(s => s.path), Does.Not.Contain(BattleCrayonUIInstaller.ScenePath));
        }

        [Test] public void RuntimeOnlySelectsMaterialsAndNeverGeneratesVisualAssets()
        {
            foreach (var path in Directory.GetFiles("Assets/ANIMOL/Scripts/Runtime/Battle", "*.cs"))
            {
                var source = File.ReadAllText(path);
                foreach (var forbidden in new[] { "new Texture2D", "Sprite.Create", "LineRenderer", "SetPixel", "AnimationClip", "Random." })
                    Assert.That(source, Does.Not.Contain(forbidden), path);
            }
        }

        [Test] public void ShaderChoosesOnlyAThenBThenCInZeroPointSixSecondStepsAndStillAlwaysUsesA()
        {
            var animated = new Material(CrayonUIAssets.Material("Frame_ButtonSquare"));
            var still = new Material(CrayonUIAssets.Material("Frame_ButtonSquare", true));
            var frames = new[] { "A", "B", "C" }.Select(f => CrayonUIAssets.Sprite("Frame_ButtonSquare_" + f).texture).ToArray();
            var previousTime = Shader.GetGlobalVector("_Time");
            try
            {
                var expected = frames.Select(t => RenderHash(still, t, 0f)).ToArray();
                Assert.That(expected.Distinct().Count(), Is.EqualTo(3), "The supplied painted variants must actually differ.");
                var times = new[] { 0f, .59f, .61f, 1.19f, 1.21f, 1.79f, 1.81f };
                var indices = new[] { 0, 0, 1, 1, 2, 2, 0 };
                for (int i = 0; i < times.Length; i++)
                {
                    Assert.That(RenderHash(animated, frames[0], times[i]), Is.EqualTo(expected[indices[i]]), "Animated t=" + times[i]);
                    Assert.That(RenderHash(still, frames[0], times[i]), Is.EqualTo(expected[0]), "Reduced motion t=" + times[i]);
                }
            }
            finally { Shader.SetGlobalVector("_Time", previousTime); Object.DestroyImmediate(animated); Object.DestroyImmediate(still); }
        }

        // GPU readback for validation only; it renders the supplied PNG, never manufactures artwork.
        static string RenderHash(Material material, Texture2D source, float seconds)
        {
            var target = RenderTexture.GetTemporary(128, 128, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target; GL.Clear(true, true, Color.clear);
                Shader.SetGlobalVector("_Time", new Vector4(seconds / 20f, seconds, seconds * 2f, seconds * 3f));
                material.mainTexture = source;
                GL.PushMatrix();
                try
                {
                    GL.LoadOrtho(); material.SetPass(0); GL.Begin(GL.QUADS); GL.Color(Color.white);
                    GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0);
                    GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0);
                    GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 0);
                    GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 0); GL.End();
                }
                finally { GL.PopMatrix(); }
                pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); pixels.Apply();
                Assert.That(pixels.GetPixels32().Count(c => c.a > 128), Is.GreaterThan(100));
                using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(pixels.GetRawTextureData()));
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(pixels); }
        }
    }
}
