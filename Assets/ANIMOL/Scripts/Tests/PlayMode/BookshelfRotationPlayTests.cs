#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ANIMOL.Tests
{
    public sealed class BookshelfRotationPlayTests
    {
        [UnityTest] public IEnumerator CounterclockwiseArtRunsBackwardsFreezesOnPauseAndHoldsItsRealTerminal()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/Prototypes/Device08_Bookshelf.asset");
            var shelf = source.scenario.devices.Single(d => d.role == PrototypeRole.Bookshelf && d.variant != "plate");
            var instance = Object.Instantiate(shelf.visualPrefab);
            try
            {
                var view = instance.GetComponent<DeviceVisualView>(); var art = source.theme.memorySkyTheater.v02;
                Assert.That(view.ActivateV03(art.v03Animations, art.v03ManifestSha256), Is.True);
                var binding = view.states.Single(s => s.name == "rotate_90_cw");
                Assert.That(art.v03Animations.TryGetClip(binding.v03ClipKey, art.v03ManifestSha256, out var clip, out var error), Is.True, error);
                Assert.That(view.Notify("ccw", "bookshelf_rotation_committed", binding.name, true), Is.True);
                Assert.That(view.visual.sprite, Is.SameAs(clip.GetFrame(clip.FrameCount - 1).Sprite));
                Assert.That(view.Notify("ccw", "bookshelf_rotation_committed", binding.name, true), Is.False);
                yield return new WaitForSeconds(.03f);
                view.paused = true; view.Advance(0);
                var pausedSprite = view.visual.sprite; var pausedTime = view.Elapsed;
                yield return new WaitForSeconds(.12f);
                Assert.That(view.visual.sprite, Is.SameAs(pausedSprite)); Assert.That(view.Elapsed, Is.EqualTo(pausedTime));
                view.paused = false; view.Advance(0);
                yield return new WaitForSeconds(clip.FrameCount / (float)clip.Fps + .1f);
                Assert.That(view.visual.sprite, Is.SameAs(clip.GetFrame(0).Sprite));
                Assert.That(view.BlocksInput, Is.False);
            }
            finally { Object.Destroy(instance); }
        }
    }
}
#endif
