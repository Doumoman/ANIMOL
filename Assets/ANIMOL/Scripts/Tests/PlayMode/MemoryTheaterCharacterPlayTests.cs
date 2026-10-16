using System.Collections;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace ANIMOL.Presentation.Tests
{
    public class MemoryTheaterCharacterPlayTests
    {
        const string Portrait = "Docs/Validation/MemoryTheaterCharacters160/Portrait";

        [UnityTearDown] public IEnumerator LeaveEmptySceneForOtherPlayTests()
        {
#if UNITY_EDITOR
            var previous = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Memory Theater character test cleanup");
            SceneManager.SetActiveScene(empty);
            if (previous.IsValid())
            {
                var unload = SceneManager.UnloadSceneAsync(previous);
                if (unload != null) while (!unload.isDone) yield return null;
            }
#endif
            yield return null;
        }

        static void UsePortraitGameView()
        {
            var setup = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ANIMOL.Presentation.Editor.PortraitGameView"))
                .FirstOrDefault(type => type != null);
            Assert.That(setup, Is.Not.Null);
            setup.GetMethod("SelectFixedResolution", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { 1080, 1920, "Memory Theater character art QA" });
        }

        [UnityTest] public IEnumerator FourCharactersAllDirectionsAndStatesPlayOnVisual()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/ANIMOL_MemoryTheater_Character_QA.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var qa = Object.FindFirstObjectByType<MemoryTheaterCharacterQaController>();
            Assert.That(qa, Is.Not.Null);
            var visual = qa.character;
            Assert.That(visual.transform.name, Is.EqualTo("Visual"));
            Assert.That(visual.visual, Is.SameAs(visual.GetComponent<SpriteRenderer>()));
            foreach (var entry in qa.catalog.characters)
                foreach (var direction in new[] { "front", "back", "side", "left" })
                    foreach (var state in new[] { "idle", "move", "sliding", "jump", "victory" })
                    {
                        Assert.That(qa.Preview(entry.id, direction, state), Is.True, entry.id + "/" + direction + "/" + state);
                        Assert.That(visual.CurrentClip, Is.Not.Null);
                        Assert.That(visual.visual.flipX, Is.EqualTo(direction == "left"));
                        Assert.That(visual.visual.sprite, Is.Not.Null);
                        Assert.That(visual.Illustration, Is.SameAs(entry.illustration));
                        var motion = entry.Motion(direction == "left" ? "side" : direction, state);
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
            Assert.That(qa.Preview("mion", "back", "idle"), Is.True);
            Assert.That(visual.visual.flipX, Is.False);
            Assert.That(qa.Preview("seri", "front", "idle"), Is.True);
            var first = visual.visual.sprite;
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(visual.visual.sprite, Is.Not.SameAs(first), "Animation changes the SpriteRenderer frame");
            yield return new WaitForSecondsRealtime(1.15f);
            Assert.That(visual.State, Is.EqualTo("idle"), "idle repeats");
            Assert.That(qa.Preview("mion", "side", "jump"), Is.True);
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(visual.State, Is.EqualTo("idle"), "jump returns to idle");
            Assert.That(qa.Preview("mion", "front", "victory"), Is.True);
            yield return new WaitForSecondsRealtime(.95f);
            Assert.That(visual.State, Is.EqualTo("idle"), "victory returns to idle");
            UsePortraitGameView();
            yield return null;
            yield return null;
            Assert.That(Screen.width, Is.EqualTo(1080));
            Assert.That(Screen.height, Is.EqualTo(1920));
            Directory.CreateDirectory(Portrait);
            foreach (var entry in qa.catalog.characters)
            {
                Assert.That(qa.Preview(entry.id, "front", "idle"), Is.True);
                yield return null;
                ScreenCapture.CaptureScreenshot(Portrait + "/" + entry.id + "_front_idle.png");
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(qa.Preview(entry.id, "back", "idle"), Is.True);
                yield return null;
                ScreenCapture.CaptureScreenshot(Portrait + "/" + entry.id + "_back_idle.png");
                yield return new WaitForSecondsRealtime(.2f);
            }
#else
            Assert.Ignore("Requires Unity Editor scene loader."); yield break;
#endif
        }

        [UnityTest] public IEnumerator ActualPuzzleSpawnSelectionAndSceneReloadKeepV2Art()
        {
#if UNITY_EDITOR
            UsePortraitGameView();
            yield return null;
            yield return null;
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/ANIMOL_MemoryTheater_Puzzle_QA.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var puzzle = Object.FindFirstObjectByType<PuzzleController>();
            Assert.That(puzzle, Is.Not.Null);
            Assert.That(puzzle.CharacterVisual, Is.Not.Null);
            Assert.That(puzzle.CharacterVisual.characterId, Is.EqualTo("seri"));
            Assert.That(puzzle.Player.localScale, Is.EqualTo(Vector3.one));
            Assert.That(puzzle.CharacterVisual.transform.localScale.x, Is.EqualTo(.85f).Within(.0001f));
            Directory.CreateDirectory(Portrait);
            ScreenCapture.CaptureScreenshot(Portrait + "/puzzle_seri.png");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(puzzle.SelectMemoryTheaterCharacter("mion"), Is.True);
            Assert.That(puzzle.CharacterVisual.characterId, Is.EqualTo("mion"));
            Assert.That(puzzle.CharacterVisual.visual.sprite, Is.Not.Null);
            yield return null;
            ScreenCapture.CaptureScreenshot(Portrait + "/puzzle_mion.png");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(puzzle.RequestMove(Vector2Int.right), Is.True);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(puzzle.CharacterVisual.Direction, Is.EqualTo("side"));
            Assert.That(puzzle.CharacterVisual.characterId, Is.EqualTo("mion"));
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/ANIMOL_MemoryTheater_Character_QA.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var qa = Object.FindFirstObjectByType<MemoryTheaterCharacterQaController>();
            Assert.That(qa.Preview("mion", "front", "idle"), Is.True);
            Assert.That(qa.character.Illustration, Is.SameAs(qa.catalog.Find("mion").illustration));
#else
            Assert.Ignore("Requires Unity Editor scene loader."); yield break;
#endif
        }

        [UnityTest] public IEnumerator MemoryTheaterPrototypeUsesTheSameCharacterVisual()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/ANIMOL_memory_theater_Prototype.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var prototype = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.That(prototype, Is.Not.Null);
            Assert.That(prototype.CharacterVisual, Is.Not.Null);
            Assert.That(prototype.CharacterVisual.characterId, Is.EqualTo("seri"));
            Assert.That(prototype.SelectMemoryTheaterCharacter("piko"), Is.True);
            Assert.That(prototype.CharacterVisual.visual.sprite,
                Is.SameAs(prototype.RuntimeMap.theme.memoryTheaterCharacters.Find("piko").FirstFrame("front", "idle")));
#else
            Assert.Ignore("Requires Unity Editor scene loader."); yield break;
#endif
        }
    }
}
