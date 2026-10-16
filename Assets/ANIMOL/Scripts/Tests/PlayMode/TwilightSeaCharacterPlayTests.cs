using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ANIMOL.Presentation.Tests
{
    public class TwilightSeaCharacterPlayTests
    {
        const string Portrait = "Docs/Validation/TwilightSeaCharacters160_v5/Portrait";
        const string PrototypeScene = "Assets/ANIMOL/Scenes/ANIMOL_twilight_sea_Prototype.unity";
        const string CoastScene = "Assets/ANIMOL/Scenes/ANIMOL_TwilightCoast_Puzzle_QA.unity";

        [UnityTearDown] public IEnumerator LeaveEmptySceneForOtherPlayTests()
        {
            var previous = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Twilight Sea character test cleanup");
            SceneManager.SetActiveScene(empty);
            if (previous.IsValid())
            {
                var unload = SceneManager.UnloadSceneAsync(previous);
                if (unload != null) while (!unload.isDone) yield return null;
            }
            yield return null;
        }

        static void UsePortraitGameView()
        {
            var setup = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ANIMOL.Presentation.Editor.PortraitGameView"))
                .FirstOrDefault(type => type != null);
            Assert.That(setup, Is.Not.Null);
            setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { 1080, 1920, "Twilight Sea character art QA" });
        }

        [UnityTest] public IEnumerator FiveAppearancesAllDirectionsAndStatesPlayOnActualPrototypeVisual()
        {
            EditorSceneManager.LoadSceneInPlayMode(PrototypeScene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var prototype = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.That(prototype, Is.Not.Null);
            var visual = prototype.CharacterVisual;
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.transform.name, Is.EqualTo("Visual"));
            Assert.That(visual.visual, Is.SameAs(visual.GetComponent<SpriteRenderer>()));
            Assert.That(visual.GetComponents<Animator>(), Is.Empty);
            var catalog = prototype.RuntimeMap.theme.twilightSeaCharacters;
            Assert.That(catalog, Is.Not.Null);
            var appearances = new[] {
                (character: "haerin", costume: "default"),
                (character: "stranger", costume: "default"),
                (character: "danu", costume: "default"),
                (character: "eden", costume: "rabbit"),
                (character: "eden", costume: "puppy")
            };
            foreach (var appearance in appearances)
            {
                Assert.That(prototype.SelectTwilightSeaCharacter(appearance.character), Is.True);
                if (appearance.character == "eden")
                    Assert.That(prototype.SelectTwilightSeaCostume(appearance.costume), Is.True);
                var entry = catalog.Find(appearance.character);
                Assert.That(visual.costumeId, Is.EqualTo(appearance.costume));
                Assert.That(visual.Illustration, Is.SameAs(entry.IllustrationFor(appearance.costume)));
                foreach (var direction in new[] { "front", "back", "side", "left" })
                    foreach (var state in new[] { "idle", "move", "sliding", "jump", "victory" })
                    {
                        Assert.That(visual.PlayPose(direction, state), Is.True,
                            appearance.character + "/" + appearance.costume + "/" + direction + "/" + state);
                        var motion = entry.Motion(direction == "left" ? "side" : direction,
                            state, appearance.costume);
                        Assert.That(visual.CurrentClip, Is.SameAs(motion.clip));
                        Assert.That(visual.visual.flipX, Is.EqualTo(direction == "left"));
                        Assert.That(visual.visual.sprite, Is.SameAs(motion.frames[0]));
                        for (var frame = 1; frame < 6; frame++)
                        {
                            visual.Advance(1f / motion.fps + .0001f);
                            Assert.That(visual.visual.sprite, Is.SameAs(motion.frames[frame]),
                                appearance.character + "/" + appearance.costume + "/" + direction +
                                "/" + state + "/" + frame);
                        }
                        visual.Advance(1f / motion.fps + .0001f);
                        if (motion.loop) Assert.That(visual.visual.sprite, Is.SameAs(motion.frames[0]));
                        else Assert.That(visual.State, Is.EqualTo("idle"));
                        yield return null;
                    }
            }
            Assert.That(prototype.SelectTwilightSeaCharacter("eden"), Is.True);
            Assert.That(prototype.SelectTwilightSeaCostume("puppy"), Is.True);
            Assert.That(visual.PlayPose("back", "idle"), Is.True);
            Assert.That(visual.visual.flipX, Is.False);
            Assert.That(visual.PlayPose("side", "jump"), Is.True);
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(visual.State, Is.EqualTo("idle"));
            Assert.That(visual.PlayPose("front", "victory"), Is.True);
            yield return new WaitForSecondsRealtime(.95f);
            Assert.That(visual.State, Is.EqualTo("idle"));

            UsePortraitGameView();
            yield return null;
            yield return null;
            Assert.That(Screen.width, Is.EqualTo(1080));
            Assert.That(Screen.height, Is.EqualTo(1920));
            Directory.CreateDirectory(Portrait);
            foreach (var appearance in appearances)
            {
                Assert.That(prototype.SelectTwilightSeaCharacter(appearance.character), Is.True);
                if (appearance.character == "eden")
                    Assert.That(prototype.SelectTwilightSeaCostume(appearance.costume), Is.True);
                var assetSet = appearance.costume == "puppy" ? "edenpuppy" : appearance.character;
                foreach (var direction in new[] { "front", "back", "side" })
                {
                    Assert.That(visual.PlayPose(direction, "idle"), Is.True);
                    yield return null;
                    ScreenCapture.CaptureScreenshot(Portrait + "/" + assetSet + "_" + direction + "_idle.png");
                    yield return new WaitForSecondsRealtime(.2f);
                }
            }
        }

        [UnityTest] public IEnumerator CoastSpawnSelectionCostumeAndSceneReloadKeepTheSameFourIds()
        {
            UsePortraitGameView();
            yield return null;
            yield return null;
            EditorSceneManager.LoadSceneInPlayMode(CoastScene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var puzzle = Object.FindFirstObjectByType<PuzzleController>();
            Assert.That(puzzle, Is.Not.Null);
            Assert.That(puzzle.CharacterVisual, Is.Not.Null);
            Assert.That(puzzle.CharacterVisual.characterId, Is.EqualTo("haerin"));
            Assert.That(puzzle.Player.localScale, Is.EqualTo(Vector3.one));
            Assert.That(puzzle.CharacterVisual.transform.localScale.x, Is.EqualTo(.85f).Within(.0001f));
            Assert.That(puzzle.SelectTwilightSeaCharacter("stranger"), Is.True);
            var catalog = puzzle.RuntimeMap.theme.twilightSeaCharacters;
            Assert.That(puzzle.CharacterVisual.visual.sprite,
                Is.SameAs(catalog.Find("stranger").FirstFrame("front", "idle")));
            Directory.CreateDirectory(Portrait);
            ScreenCapture.CaptureScreenshot(Portrait + "/coast_stranger.png");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(puzzle.SelectTwilightSeaCharacter("eden"), Is.True);
            Assert.That(puzzle.SelectTwilightSeaCostume("puppy"), Is.True);
            Assert.That(puzzle.CharacterVisual.visual.sprite,
                Is.SameAs(catalog.Find("eden").Motion("front", "idle", "puppy").frames[0]));
            Assert.That(puzzle.CharacterVisual.Illustration,
                Is.SameAs(catalog.Find("eden").IllustrationFor("puppy")));
            ScreenCapture.CaptureScreenshot(Portrait + "/coast_edenpuppy.png");
            yield return new WaitForSecondsRealtime(.2f);
            EditorSceneManager.LoadSceneInPlayMode(PrototypeScene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var prototype = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.That(prototype.CharacterVisual.characterId, Is.EqualTo("haerin"));
            Assert.That(prototype.SelectTwilightSeaCharacter("eden"), Is.True);
            Assert.That(prototype.SelectTwilightSeaCostume("puppy"), Is.True);
            Assert.That(prototype.CharacterVisual.Illustration,
                Is.SameAs(prototype.RuntimeMap.theme.twilightSeaCharacters.Find("eden").IllustrationFor("puppy")));
            Assert.That(prototype.RuntimeMap.theme.twilightSeaCharacters.Find("edenpuppy"), Is.Null);
        }
    }
}
