using System.Collections;
using System.Linq;
using ANIMOL.Puzzle;
using Animol.StarwayRanch.V10;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class StarwayRanchV10PlayTests
    {
        [UnityTest] public IEnumerator PrototypeUsesV10ArtAndMotionWithoutChangingPuzzleAuthority()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Prototype/ANIMOL_starway_Prototype.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null; yield return null;
            var puzzle = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            Assert.That(puzzle, Is.Not.Null);
            Assert.That(puzzle.Session, Is.Not.Null);
            var rig = Object.FindFirstObjectByType<StarwayRanchV10BackgroundRig>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(129));
            var camera = Camera.main;
            Assert.That(camera.farClipPlane, Is.GreaterThan(120));
            Assert.That(camera.orthographic, Is.True);
            Assert.That(StarwayRanchV10BackgroundRig.EffectiveParallaxFactor(-10, 0, 70, .03f),
                Is.EqualTo(.00375f).Within(.00001f));
            var art = puzzle.RuntimeMap.theme.starwayRanch;
            var stone = puzzle.Session.Devices.First(d => d.role == PrototypeRole.StarSocket && d.variant != "socket");
            var adapter = puzzle.Devices.Actor(stone.id).GetComponent<StarwayRanchDeviceArt>();
            Assert.That(adapter, Is.Not.Null);
            Assert.That(adapter.view.CurrentState, Is.EqualTo("stone"));
            adapter.ApplyNow();
            var stoneVisual = puzzle.Devices.Actor(stone.id).GetComponent<DeviceVisualView>().visual;
            Assert.That(stoneVisual.sprite, Is.SameAs(art.Find("devices/star_stone/idle.png")));
            var chick = puzzle.Session.Devices.First(d => d.role == PrototypeRole.Chick);
            var chickRoot = puzzle.Devices.Actor(chick.id);
            var chickVisual = chickRoot.GetComponent<DeviceVisualView>().visual.transform;
            Assert.That(chickRoot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(chickVisual.localScale.x, Is.EqualTo(.62f).Within(.00001f));
            Assert.That(puzzle.Session.Footprint(chick).Count(), Is.EqualTo(1));

            puzzle.FollowCamera.enabled = false;
            var star = rig.transform.Find("far_dot_00");
            var cloud = rig.transform.Find("far_cloud_left");
            Assert.That(star, Is.Not.Null); Assert.That(cloud, Is.Not.Null);
            var starScreen = star.position.x - camera.transform.position.x;
            var cloudScreen = cloud.position.x - camera.transform.position.x;
            camera.transform.position += Vector3.right;
            yield return null;
            var starShift = star.position.x - camera.transform.position.x - starScreen;
            var cloudShift = cloud.position.x - camera.transform.position.x - cloudScreen;
            Assert.That(starShift, Is.EqualTo(-.00375f).Within(.02f));
            Assert.That(cloudShift, Is.EqualTo(-.35714f).Within(.04f));
            try
            {
                Time.timeScale = 0;
                yield return null;
                var pausedCloud = cloud.position;
                yield return new WaitForSecondsRealtime(.12f);
                Assert.That(Vector3.Distance(cloud.position, pausedCloud), Is.LessThan(.0001f));
                rig.SetReducedMotion(true);
                var locked = cloud.position.x - camera.transform.position.x;
                camera.transform.position += Vector3.right;
                yield return null;
                Assert.That(cloud.position.x - camera.transform.position.x,
                    Is.EqualTo(locked).Within(.0001f));
            }
            finally { Time.timeScale = 1; }
            Assert.That(puzzle.BlocksInput, Is.False);
        }
    }
}
