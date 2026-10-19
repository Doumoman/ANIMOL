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
    public class LetterObservatoryCharacterPlayTests
    {
        const string Scene = "Assets/ANIMOL/Scenes/Prototype/ANIMOL_letter_observatory_Prototype.unity";
        const string Portrait = "Docs/Validation/SunsetSanctumNative/Portrait";

        [UnityTearDown] public IEnumerator LeaveEmptySceneForOtherPlayTests()
        {
            var previous = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Letter Observatory character test cleanup");
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
                .Invoke(null, new object[] { 1080, 1920, "Sunset Sanctum character art QA" });
        }

        [UnityTest] public IEnumerator FiveCharactersAllDirectionsAndStatesPlayOnPrototypeVisual()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            PuzzlePrototypeController prototype = null;
            for (var attempt = 0; attempt < 60 && !prototype; attempt++)
            {
                yield return null;
                prototype = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            }
            Assert.That(prototype, Is.Not.Null);
            var visual = prototype.CharacterVisual;
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.transform.name, Is.EqualTo("Visual"));
            Assert.That(visual.visual, Is.SameAs(visual.GetComponent<SpriteRenderer>()));
            Assert.That(visual.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(1),
                "Lumi's ferret is baked into the same sprite");
            Assert.That(visual.GetComponents<Animator>(), Is.Empty);
            Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one * .85f));
            Assert.That(visual.transform.parent.localScale, Is.EqualTo(Vector3.one));
            var catalog = prototype.RuntimeMap.theme.letterObservatoryCharacters;
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.characters.Length, Is.EqualTo(5));
            foreach (var entry in catalog.characters)
            {
                Assert.That(prototype.SelectLetterObservatoryCharacter(entry.id), Is.True);
                Assert.That(visual.Illustration, Is.SameAs(entry.illustration));
                foreach (var direction in new[] { "front", "back", "side", "left" })
                    foreach (var state in new[] { "idle", "move", "sliding", "jump", "victory" })
                    {
                        Assert.That(visual.PlayPose(direction, state), Is.True,
                            entry.id + "/" + direction + "/" + state);
                        var motion = entry.Motion(direction == "left" ? "side" : direction, state);
                        Assert.That(visual.CurrentClip, Is.SameAs(motion.clip));
                        Assert.That(visual.visual.flipX, Is.EqualTo(direction == "left"));
                        Assert.That(visual.visual.sprite, Is.SameAs(motion.frames[0]));
                        for (var frame = 1; frame < 6; frame++)
                        {
                            visual.Advance(1f / motion.fps + .0001f);
                            Assert.That(visual.visual.sprite, Is.SameAs(motion.frames[frame]),
                                entry.id + "/" + direction + "/" + state + "/" + frame);
                        }
                        visual.Advance(1f / motion.fps + .0001f);
                        if (motion.loop) Assert.That(visual.visual.sprite, Is.SameAs(motion.frames[0]));
                        else Assert.That(visual.State, Is.EqualTo("idle"));
                        yield return null;
                    }
            }
            Assert.That(prototype.SelectLetterObservatoryCharacter("lumi"), Is.True);
            Assert.That(visual.PlayPose("left", "idle"), Is.True);
            Assert.That(visual.visual.flipX, Is.True);
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
            foreach (var entry in catalog.characters)
            {
                Assert.That(prototype.SelectLetterObservatoryCharacter(entry.id), Is.True);
                foreach (var direction in new[] { "front", "back", "side" })
                {
                    Assert.That(visual.PlayPose(direction, "idle"), Is.True);
                    yield return null;
                    ScreenCapture.CaptureScreenshot(Portrait + "/" + entry.id + "_" + direction + "_idle.png");
                    yield return new WaitForSecondsRealtime(.2f);
                }
            }
        }

        [UnityTest] public IEnumerator SpawnSelectionAndSceneReloadKeepSoyulSeparateFromDoyun()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var prototype = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.That(prototype, Is.Not.Null);
            Assert.That(prototype.CharacterVisual.characterId, Is.EqualTo("sein"));
            Assert.That(prototype.SelectLetterObservatoryCharacter("soyul"), Is.True);
            Assert.That(prototype.CharacterVisual.visual.sprite,
                Is.SameAs(prototype.RuntimeMap.theme.letterObservatoryCharacters.Find("soyul")
                    .FirstFrame("front", "idle")));
            Assert.That(prototype.CharacterVisual.Illustration,
                Is.SameAs(prototype.RuntimeMap.theme.letterObservatoryCharacters.Find("soyul").illustration));
            Assert.That(prototype.SelectLetterObservatoryCharacter("doyun"), Is.True);
            Assert.That(prototype.CharacterVisual.characterId, Is.EqualTo("doyun"));
            Assert.That(prototype.CharacterVisual.Illustration,
                Is.SameAs(prototype.RuntimeMap.theme.letterObservatoryCharacters.Find("doyun").illustration));
            Assert.That(prototype.SelectLetterObservatoryCharacter("soyul"), Is.True);
            Assert.That(prototype.CharacterVisual.characterId, Is.EqualTo("soyul"));
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            prototype = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.That(prototype.CharacterVisual.characterId, Is.EqualTo("sein"));
            Assert.That(prototype.SelectLetterObservatoryCharacter("lumi"), Is.True);
            Assert.That(prototype.CharacterVisual.Illustration,
                Is.SameAs(prototype.RuntimeMap.theme.letterObservatoryCharacters.Find("lumi").illustration));
        }

        [UnityTest] public IEnumerator DoyunRepairPlaysEveryDirectionAndStateWithoutChangingCharacterScale()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var prototype = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.That(prototype, Is.Not.Null);
            Assert.That(prototype.SelectLetterObservatoryCharacter("doyun"), Is.True);
            var visual = prototype.CharacterVisual;
            var definition = prototype.RuntimeMap.theme.letterObservatoryCharacters.Find("doyun");
            Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one * .85f));
            Assert.That(visual.transform.parent.localScale, Is.EqualTo(Vector3.one));
            Assert.That(visual.GetComponents<Animator>(), Is.Empty);
            foreach (var direction in new[] { "front", "side", "left", "back" })
                foreach (var state in new[] { "idle", "move", "sliding", "jump", "victory" })
                {
                    Assert.That(visual.PlayPose(direction, state), Is.True, direction + "/" + state);
                    var motion = definition.Motion(direction == "left" ? "side" : direction, state);
                    Assert.That(visual.CurrentClip, Is.SameAs(motion.clip));
                    Assert.That(visual.visual.flipX, Is.EqualTo(direction == "left"));
                    for (var frame = 0; frame < 6; frame++)
                    {
                        if (frame > 0) visual.Advance(1f / motion.fps + .0001f);
                        Assert.That(visual.visual.sprite, Is.SameAs(motion.frames[frame]),
                            direction + "/" + state + "/" + frame);
                    }
                }
            Assert.That(prototype.SelectLetterObservatoryCharacter("soyul"), Is.True);
            Assert.That(visual.visual.sprite, Is.SameAs(
                prototype.RuntimeMap.theme.letterObservatoryCharacters.Find("soyul").FirstFrame("front", "idle")));
        }
    }
}
