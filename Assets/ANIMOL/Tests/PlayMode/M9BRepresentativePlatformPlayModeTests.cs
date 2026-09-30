using System.Collections;
using System.Linq;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class M9BRepresentativePlatformPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("ThemePlatformLab");
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator LabSpawnsFiveRepresentativeTypes_AndBothPortraitViewsKeepAllSevenPartsVisible()
        {
            var objects = Object.FindObjectsByType<StageMapRuntimeObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(objects.Length, Is.EqualTo(11));
            Assert.That(objects.Select(item => item.GetType()).Distinct().Count(), Is.EqualTo(9));
            var camera = Camera.main;
            foreach (var size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2400) })
            {
                var target = new RenderTexture(size.x, size.y, 16);
                camera.targetTexture = target;
                try
                {
                    camera.orthographicSize = PortraitWorldCameraPolicy.OrthographicSizeForHorizontalCells(24f, camera.aspect);
                    foreach (var item in objects)
                    {
                        var renderer = item.GetComponentInChildren<SpriteRenderer>();
                        var point = camera.WorldToViewportPoint(renderer.bounds.center);
                        Assert.That(point.x, Is.InRange(0f, 1f), $"{item.StableId} x at {size}");
                        Assert.That(point.y, Is.InRange(0f, 1f), $"{item.StableId} y at {size}");
                    }
                }
                finally { camera.targetTexture = null; target.Release(); Object.Destroy(target); }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator JadeBalance_LowersOccupiedSide_StopsWhenBothOccupied_AndReturnsWhenEmpty()
        {
            var plates = Object.FindObjectsByType<MoonJadeBalanceObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(item => item.StableId).ToArray();
            Assert.That(plates.Length, Is.EqualTo(2)); plates[0].ResolveLink(); plates[1].ResolveLink();
            var start = (Vector2)plates[0].transform.position;
            plates[0].SetOccupiedForTest(true); yield return new WaitForSeconds(.35f);
            Assert.That(plates[0].transform.position.y, Is.LessThan(start.y - .1f));
            plates[1].SetOccupiedForTest(true); var stopped = (Vector2)plates[0].transform.position; yield return new WaitForSeconds(.2f);
            Assert.That(Vector2.Distance(plates[0].transform.position, stopped), Is.LessThan(.02f));
            plates[0].SetOccupiedForTest(false); plates[1].SetOccupiedForTest(false); yield return new WaitForSeconds(.5f);
            Assert.That(plates[0].transform.position.y, Is.EqualTo(plates[0].UpperPosition.y).Within(.04f));
        }

        [UnityTest]
        public IEnumerator BalloonDrawerAndRetrace_UseOccupancyOrProximityWithoutChangingWorldTimer()
        {
            var session = Object.FindFirstObjectByType<ObjectLabSession>(); var timer = session.ElapsedSeconds;
            var balloon = Object.FindFirstObjectByType<CloudBalloonTetherObject>(); var balloonStart = (Vector2)balloon.transform.position;
            balloon.SetOccupiedForTest(true); yield return new WaitForSeconds(.35f); Assert.That(balloon.transform.position.y, Is.LessThan(balloonStart.y - .1f));
            balloon.SetOccupiedForTest(false); yield return new WaitForSeconds(.35f); Assert.That(balloon.transform.position.y, Is.GreaterThan(balloonStart.y - .5f));
            var drawer = Object.FindFirstObjectByType<LibIndexDrawerObject>(); var drawerStart = (Vector2)drawer.transform.position;
            drawer.SetPresenceForTest(true); yield return new WaitForSeconds(.35f); Assert.That(drawer.transform.position.x, Is.GreaterThan(drawerStart.x + .1f));
            drawer.SetPresenceForTest(false); yield return new WaitForSeconds(.35f); Assert.That(drawer.transform.position.x, Is.LessThan(drawer.OpenPosition.x));
            var retrace = Object.FindFirstObjectByType<GreenSandRetraceObject>(); retrace.ResetRuntimeState(); var retraceStart = (Vector2)retrace.transform.position;
            retrace.SetOccupiedForTest(false); yield return new WaitForSeconds(.3f); Assert.That(retrace.transform.position.x, Is.GreaterThan(retraceStart.x + .1f));
            retrace.SetOccupiedForTest(true); yield return new WaitForSeconds(.4f); Assert.That(retrace.transform.position.x, Is.EqualTo(retraceStart.x).Within(.08f));
            Assert.That(session.ElapsedSeconds, Is.GreaterThan(timer));
        }

        [UnityTest]
        public IEnumerator MagnetPair_MovesOnlyEmptyPartner_StopsWhenBothOccupied_AndAllResetDeterministically()
        {
            var magnets = Object.FindObjectsByType<MineMagnetPairObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(item => item.StableId).ToArray();
            Assert.That(magnets.Length, Is.EqualTo(2)); magnets[0].ResolveLink(); magnets[1].ResolveLink();
            var firstStart = (Vector2)magnets[0].transform.position; var secondStart = (Vector2)magnets[1].transform.position;
            magnets[0].SetOccupiedForTest(true); magnets[1].SetOccupiedForTest(false); yield return new WaitForSeconds(.3f);
            Assert.That(Vector2.Distance(magnets[0].transform.position, firstStart), Is.LessThan(.02f));
            Assert.That(Vector2.Distance(magnets[1].transform.position, secondStart), Is.GreaterThan(.1f));
            magnets[1].SetOccupiedForTest(true); var stopped = (Vector2)magnets[1].transform.position; yield return new WaitForSeconds(.2f);
            Assert.That(Vector2.Distance(magnets[1].transform.position, stopped), Is.LessThan(.02f));
            Object.FindFirstObjectByType<StageMapRuntimeLoader>().ResetRuntimeObjects();
            Assert.That(Vector2.Distance(magnets[0].transform.position, magnets[0].InitialPosition), Is.LessThan(.01f));
            Assert.That(Vector2.Distance(magnets[1].transform.position, magnets[1].InitialPosition), Is.LessThan(.01f));
        }
    }
}
