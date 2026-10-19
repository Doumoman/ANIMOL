using System.Collections;
using System.Linq;
using ANIMOL.Puzzle;
using Animol.EchoTheater.V02;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class EchoTheaterV02PlayTests
    {
        [UnityTest]
        public IEnumerator ExistingMemoryTheaterPrototypeUsesV02WithoutChangingOccupancy()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                "Assets/ANIMOL/Scenes/Prototype/ANIMOL_memory_theater_Prototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;
            yield return null;
            var game = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            var floor = Object.FindFirstObjectByType<EchoTheaterV02MapRenderer>();
            var background = Object.FindFirstObjectByType<EchoTheaterV02Background>();
            var rig = Object.FindFirstObjectByType<EchoTheaterV02BackgroundRig>();
            Assert.NotNull(game);
            Assert.NotNull(floor);
            Assert.NotNull(background);
            Assert.NotNull(rig);
            Assert.IsEmpty(Object.FindObjectsByType<MemorySkyTheaterRenderer>(FindObjectsSortMode.None));
            Assert.AreEqual("memory_theater", game.RuntimeMap.theme.name);
            Assert.AreEqual(game.stage.terrain.Count, game.RuntimeMap.terrain.Count);
            Assert.IsTrue(game.RuntimeMap.Walkable(game.RuntimeMap.start));
            var floorImages = floor.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(game.RuntimeMap.terrain.Count, floorImages.Count(r => r.name.StartsWith("Floor ")));
            Assert.IsTrue(floorImages.Where(r => r.name.StartsWith("Floor ")).All(r =>
                r.sprite && r.sprite.rect.size == new Vector2(320, 320) &&
                Mathf.Abs(r.sprite.pixelsPerUnit - 256) < .001f && r.transform.localScale == Vector3.one));
            Assert.IsEmpty(floor.GetComponentsInChildren<Collider2D>());
            var skyImages = background.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(131, skyImages.Length);
            Assert.AreEqual(120, skyImages.Count(r => r.name.Contains("_dot_") || r.name.Contains("_star_")));
            Assert.IsEmpty(background.GetComponentsInChildren<Collider2D>());
            Assert.Greater(Camera.main.farClipPlane, 120);
        }
    }
}
