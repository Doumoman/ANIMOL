#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Art.EchoTheaterV03.Tests
{
    // Import into the actual project's existing Editor test assembly to execute.
    // Tests~ is intentionally excluded from normal Unity import. These tests have not been run here.
    public sealed class EchoTheaterV03VisualTests
    {
        private const string Hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string Root = "Assets/ANIMOL_ECHO_THEATER_CARTOON_V03";
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

        [TearDown] public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; --i) if (owned[i] != null) UnityEngine.Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        [Test] public void NonLoopHoldsLastAndLoopWrapsAtExactDuration()
        {
            Assert.AreEqual(0, EchoTheaterV03ClipTiming.FrameIndex(4, 10, 0, false, false));
            Assert.AreEqual(1, EchoTheaterV03ClipTiming.FrameIndex(4, 10, .1, false, false));
            Assert.AreEqual(3, EchoTheaterV03ClipTiming.FrameIndex(4, 10, .4, false, false));
            Assert.AreEqual(3, EchoTheaterV03ClipTiming.FrameIndex(4, 10, 100, false, false));
            Assert.AreEqual(0, EchoTheaterV03ClipTiming.FrameIndex(4, 10, .4, true, false));
        }
        [Test] public void ReverseAndAuthoritativeDurationHaveDefinedEndpoints()
        {
            Assert.AreEqual(3, EchoTheaterV03ClipTiming.FrameIndex(4, 10, 0, false, true, 1));
            Assert.AreEqual(2, EchoTheaterV03ClipTiming.FrameIndex(4, 10, .25, false, true, 1));
            Assert.AreEqual(0, EchoTheaterV03ClipTiming.FrameIndex(4, 10, 1, false, true, 1));
            Assert.AreEqual(0, EchoTheaterV03ClipTiming.FromNormalized(4, -1, false));
            Assert.AreEqual(3, EchoTheaterV03ClipTiming.FromNormalized(4, 2, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => EchoTheaterV03ClipTiming.FrameIndex(4, 10, 0, false, false, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => EchoTheaterV03ClipTiming.FromNormalized(4, double.NaN, false));
        }

        [Test] public void DirectionLookupNeverFallsBackToNoneOrDifferentCase()
        {
            var frames = Frames();
            var catalog = Catalog(frames, new[] { Clip("idle-none", "None", frames), Clip("idle-N", "N", frames) });
            EchoTheaterV03VisualCatalog.Clip found; string error;
            Assert.IsTrue(catalog.TryGetDirectionalClip("test", "N", "idle", Hash, out found, out error));
            Assert.AreEqual("idle-N", found.Key);
            Assert.IsFalse(catalog.TryGetDirectionalClip("test", "E", "idle", Hash, out found, out error));
            Assert.IsFalse(catalog.TryGetDirectionalClip("test", "n", "idle", Hash, out found, out error));
            Assert.IsFalse(catalog.TryGetClip("does-not-exist", Hash, out found, out error));
        }

        [Test] public void WrongRootWrongHashAndDuplicateKeysAreRejected()
        {
            var frames = Frames(); var clips = new[] { Clip("idle-N", "N", frames) };
            var catalog = Catalog(frames, clips); string error;
            Assert.IsFalse(catalog.ValidateIntegrity(new string('b', 64), out error));
            Assert.IsFalse(catalog.ValidateIntegrity("", out error));
            catalog.SetEditorData("Assets/ANIMOL_ECHO_THEATER_CARTOON_V02", new string('a', 32), Hash,
                frames, clips, new EchoTheaterV03VisualCatalog.Part[0]);
            Assert.IsFalse(catalog.ValidateIntegrity(Hash, out error));
            catalog.SetEditorData(Root, new string('a', 32), Hash, frames,
                new[] { Clip("same", "N", frames), Clip("same", "E", frames) }, new EchoTheaterV03VisualCatalog.Part[0]);
            Assert.IsFalse(catalog.ValidateIntegrity(Hash, out error));
        }

        [Test] public void BlockedInputDoesNotRestartOrReplaceCurrentVisual()
        {
            var frames = Frames(); var catalog = Catalog(frames, new[] { Clip("idle-N", "N", frames) });
            SpriteRenderer renderer; var player = Player(catalog, out renderer);
            Assert.IsTrue(player.Play("idle-N"));
            Assert.IsTrue(player.ApplyNormalizedProgress(.75f));
            Sprite before = renderer.sprite;
            Assert.IsFalse(player.PlayForCommittedAction("missing-key", false));
            Assert.AreSame(before, renderer.sprite);
            Assert.AreEqual("idle-N", player.CurrentKey);
        }

        [Test] public void PauseFreezesPlaybackButExplicitAuthoritativeSnapCanRebind()
        {
            var frames = Frames(); var catalog = Catalog(frames, new[] { Clip("idle-N", "N", frames) });
            SpriteRenderer renderer; var player = Player(catalog, out renderer);
            player.Play("idle-N"); Sprite before = renderer.sprite;
            player.SetPaused(true);
            Assert.IsFalse(player.ApplyNormalizedProgress(1));
            Assert.IsFalse(player.Play("idle-N", -1, true, true));
            Assert.AreSame(before, renderer.sprite);
            Assert.IsTrue(player.SnapToState("idle-N", 1));
            Assert.AreSame(frames[3].Sprite, renderer.sprite);
        }

        [Test] public void MissingLatestKeyShowsDiagnosticInsteadOfRetainingOldSprite()
        {
            var frames = Frames(); var catalog = Catalog(frames, new[] { Clip("idle-N", "N", frames) });
            SpriteRenderer renderer; var player = Player(catalog, out renderer);
            player.Play("idle-N"); Sprite old = renderer.sprite;
            LogAssert.Expect(LogType.Error, new Regex("Missing latest clip key"));
            Assert.IsFalse(player.Play("missing-key"));
            Assert.IsNotNull(renderer.sprite);
            Assert.AreNotSame(old, renderer.sprite);
            Assert.IsNull(player.CurrentKey);
            StringAssert.Contains("missing-key", player.LastDiagnostic);
        }

        [Test] public void SchemaRejectsMissingLoopWrongTypeAndDuplicateJsonProperties()
        {
            const string valid = "{\"schemaVersion\":1,\"packageId\":\"ANIMOL_ECHO_THEATER_CARTOON_V03\",\"sprites\":[],\"clips\":[{\"key\":\"x\",\"family\":\"f\",\"heading\":\"N\",\"state\":\"idle\",\"fps\":12,\"loop\":false,\"frames\":[]}]}";
            Assert.DoesNotThrow(() => EchoTheaterV03JsonSchema.Validate(valid));
            Assert.Throws<InvalidDataException>(() => EchoTheaterV03JsonSchema.Validate(valid.Replace("\"loop\":false,", "")));
            Assert.Throws<InvalidDataException>(() => EchoTheaterV03JsonSchema.Validate(valid.Replace("\"loop\":false", "\"loop\":\"false\"")));
            Assert.Throws<InvalidDataException>(() => EchoTheaterV03JsonSchema.Validate(valid.Replace("\"loop\":false", "\"loop\":false,\"loop\":true")));
        }

        private EchoTheaterV03VisualCatalog.Frame[] Frames()
        {
            var texture = new Texture2D(32, 32); owned.Add(texture);
            var frames = new EchoTheaterV03VisualCatalog.Frame[4];
            for (int i = 0; i < 4; ++i)
            {
                var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 256);
                sprite.name = "frame-" + i; owned.Add(sprite);
                frames[i] = new EchoTheaterV03VisualCatalog.Frame(sprite, "frames/f" + i + ".png", Hash,
                    new string('a', 32), 21300000 + i, 32, 32, 256, new Vector2(.5f, .5f));
            }
            return frames;
        }
        private static EchoTheaterV03VisualCatalog.Clip Clip(string key, string heading, EchoTheaterV03VisualCatalog.Frame[] frames)
        { return new EchoTheaterV03VisualCatalog.Clip(key, "test", heading, "idle", 12, false, frames); }
        private EchoTheaterV03VisualCatalog Catalog(EchoTheaterV03VisualCatalog.Frame[] frames, EchoTheaterV03VisualCatalog.Clip[] clips)
        {
            var catalog = ScriptableObject.CreateInstance<EchoTheaterV03VisualCatalog>(); owned.Add(catalog);
            catalog.SetEditorData(Root, new string('a', 32), Hash, frames, clips, new EchoTheaterV03VisualCatalog.Part[0]);
            return catalog;
        }
        private EchoTheaterV03AnimatedVisual Player(EchoTheaterV03VisualCatalog catalog, out SpriteRenderer renderer)
        {
            var go = new GameObject("V03 visual test"); go.SetActive(false); owned.Add(go);
            renderer = go.AddComponent<SpriteRenderer>();
            var player = go.AddComponent<EchoTheaterV03AnimatedVisual>();
            Assert.IsTrue(player.Configure(catalog, renderer, Hash));
            return player;
        }
    }
}
#endif
