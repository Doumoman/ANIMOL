using System.Collections;
using System.Linq;
using ANIMOL.Puzzle;
using Animol.EchoTheater.V03;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class EchoTheaterV02PlayTests
    {
        [UnityTest]
        public IEnumerator ExistingMemoryTheaterPrototypeUsesV03WithoutChangingOccupancy()
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
            var rig = Object.FindFirstObjectByType<EchoTheaterV03BackgroundRig>();
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
#if UNITY_EDITOR
            Assert.IsTrue(floorImages.Where(r => r.name.StartsWith("Floor ")).All(r =>
                UnityEditor.AssetDatabase.GetAssetPath(r.sprite).Contains("ANIMOL_ECHO_THEATER_CARTOON_V03")));
#endif
            Assert.IsEmpty(floor.GetComponentsInChildren<Collider2D>());
            var skyImages = background.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(131, skyImages.Length);
            Assert.AreEqual(120, skyImages.Count(r => r.name.Contains("_dot_") || r.name.Contains("_star_")));
            Assert.IsEmpty(background.GetComponentsInChildren<Collider2D>());
            Assert.Greater(Camera.main.farClipPlane, 120);
            var expectedRoles = new[] { PrototypeRole.Shutter, PrototypeRole.MovingStage,
                PrototypeRole.Conveyor, PrototypeRole.Bookshelf, PrototypeRole.OneWayGate,
                PrototypeRole.Domino, PrototypeRole.Statue, PrototypeRole.Bounce,
                PrototypeRole.Brake, PrototypeRole.Dice, PrototypeRole.CowardDoll };
            foreach (var role in expectedRoles)
            {
                var device = game.Session.Devices.FirstOrDefault(d => d.role == role &&
                    d.variant != "plate" && d.variant != "target");
                Assert.NotNull(device, role.ToString());
                var actor = game.Devices.Actor(device.id);
                Assert.NotNull(actor, device.id);
                var visual = actor.GetComponent<DeviceVisualView>();
                Assert.NotNull(visual, device.id);
                Assert.IsTrue(visual.IsV03Active, device.id);
                Assert.NotNull(visual.visual.sprite, device.id);
#if UNITY_EDITOR
                Assert.That(UnityEditor.AssetDatabase.GetAssetPath(visual.visual.sprite),
                    Does.Contain("ANIMOL_ECHO_THEATER_CARTOON_V03"), device.id);
#endif
            }
            Assert.Zero(game.Devices.MissingBindings);
        }
    }
}
