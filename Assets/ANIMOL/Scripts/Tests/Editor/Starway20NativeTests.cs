using System.Collections.Generic;
using ANIMOL.Puzzle.Editor;
using ANIMOL.StarwayMaps;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Puzzle.Tests
{
    public sealed class Starway20NativeTests
    {
        [Test]
        public void UnchangedSeventeenNativeMapsReplayTheirSourceSolutions()
        {
            Assert.That(Starway20ReplayValidation.RunAll(new HashSet<string> { "SW01", "SW02", "SW03" }), Is.EqualTo("17/17"));
        }

        [TestCase("SW01", 65)]
        [TestCase("SW02", 43)]
        [TestCase("SW03", 43)]
        public void RevisedNativeMapReplaysSavedVerificationRoute(string id, int expectedCost)
        {
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/MapData/starway/" + id + "_MapData.asset");
            Assert.That(map, Is.Not.Null);
            Assert.That(map.ValidateMap(), Is.Empty);
            Assert.That(map.scenario.goals, Is.Empty);
            Assert.That(map.scenario.costLimit, Is.Zero);
            var route = map.scenario.verificationRoute;
            Assert.That(route.Length, Is.EqualTo(expectedCost));
            var session = new PuzzleDeviceSession(map);
            foreach (var input in route)
            {
                var action = session.Apply(ANIMOL.Presentation.Editor.PuzzlePrototypeBuilder.Direction(input));
                Assert.That(action.accepted, Is.True, id + " input " + session.Cost + " " + input);
                Assert.That(session.Failed, Is.False);
            }
            Assert.That(session.Cost, Is.EqualTo(expectedCost));
            Assert.That(session.Collected, Is.EqualTo(3));
            Assert.That(session.Cell, Is.EqualTo(map.exit));
            Assert.That(session.Won, Is.True);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Won, Is.False);
            session.Reset();
            Assert.That(session.Cost, Is.Zero);
            Assert.That(session.Cell, Is.EqualTo(map.start));
            Assert.That(session.Collected, Is.Zero);
        }

        [TestCase("SW01")]
        [TestCase("SW02")]
        [TestCase("SW03")]
        public void RevisedMapEditorUsesTheRanchTilesForEveryFloor(string id)
        {
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>("Assets/ANIMOL/Data/Puzzle/MapData/starway/" + id + "_MapData.asset");
            var rows = ANIMOL.Presentation.Editor.PuzzleMapEditor.StarwayTerrainRows(map);
            foreach (var tile in map.terrain)
            {
                var sprite = ANIMOL.Presentation.Editor.PuzzleMapEditor.TerrainPreviewSprite(map, tile.position, rows);
                if (tile.blocked) Assert.That(sprite, Is.Null, id + " wall " + tile.position);
                else Assert.That(sprite, Is.Not.Null, id + " floor " + tile.position);
            }
            foreach (var finish in ANIMOL.Art.V12.StarwayRanchV12MaterialSelector.GetBottomFinishes(rows))
                Assert.That(map.theme.starwayRanch.Find(finish.File), Is.Not.Null, id + " underside " + finish.File);
            foreach (var piece in ANIMOL.Art.V12.StarwayRanchV12BridgeDecorSelector.Select(rows))
                Assert.That(map.theme.starwayRanch.Find(piece.File), Is.Not.Null, id + " bridge " + piece.File);
        }

        [Test]
        public void StarwayFocusUsesSevenByElevenCellsAndClampsToRegion()
        {
            var focus = StarwayCameraMath.FocusWorldSize(Vector2.one);
            Assert.That(focus, Is.EqualTo(new Vector2(7, 11)));
            var region = new[] { new StarwayCell(2, 3), new StarwayCell(17, 19) };
            var bounds = StarwayCameraMath.CellBounds(region, Vector2.zero, Vector2.one);
            var center = StarwayCameraMath.ClampCenter(new Vector2(-100, 100), bounds, focus);
            Assert.That(center.x, Is.EqualTo(5.0f).Within(.001f));
            Assert.That(center.y, Is.EqualTo(14.0f).Within(.001f));
        }

        [Test]
        public void StarwayBlankRowsControlCameraHeadroomAndFootroom()
        {
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/starway/SW02_MapData.asset");
            var map = Object.Instantiate(source);
            var cameraObject = new GameObject("Starway camera focus test", typeof(Camera), typeof(PuzzleFollowCamera));
            var playerObject = new GameObject("Starway camera target test");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            playerObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                map.terrain.RemoveAll(t => t.position.y < 5 || t.position.y > 18);
                var originalInset = map.cameraSettings.topInset;
                var originalRows = map.cameraSettings.visibleRows;
                Assert.That(map.cameraSettings.bottomBlankRows, Is.EqualTo(2));
                map.cameraSettings.topBlankRows = 2;
                var bounds = PuzzleFollowCamera.StarwayFollowBounds(map);
                var center = StarwayCameraMath.ClampCenter(map.start, bounds,
                    StarwayCameraMath.FocusWorldSize(Vector2.one));
                Assert.That(center.y, Is.EqualTo(15f).Within(.001f));
                Assert.That(ANIMOL.Presentation.Editor.PuzzleMapEditor.CameraBounds(map, map.start).center.y,
                    Is.EqualTo(15f).Within(.001f));
                playerObject.transform.position = (Vector2)map.start;
                cameraObject.GetComponent<PuzzleFollowCamera>().Configure(map, playerObject.transform);
                Assert.That(cameraObject.transform.position.y, Is.EqualTo(15f).Within(.001f));
                map.cameraSettings.topBlankRows = 4;
                cameraObject.GetComponent<PuzzleFollowCamera>().Configure(map, playerObject.transform);
                Assert.That(cameraObject.transform.position.y, Is.EqualTo(17f).Within(.001f));
                Assert.That(ANIMOL.Presentation.Editor.PuzzleMapEditor.CameraBounds(map, map.start).center.y,
                    Is.EqualTo(17f).Within(.001f));
                map.cameraSettings.topBlankRows = 0;
                cameraObject.GetComponent<PuzzleFollowCamera>().Configure(map, playerObject.transform);
                Assert.That(cameraObject.transform.position.y, Is.EqualTo(13f).Within(.001f));

                map.cameraSettings.topBlankRows = 2;
                playerObject.transform.position = new Vector2(-100, -100);
                cameraObject.GetComponent<PuzzleFollowCamera>().Configure(map, playerObject.transform);
                Assert.That(cameraObject.transform.position.y, Is.EqualTo(8f).Within(.001f));
                Assert.That(ANIMOL.Presentation.Editor.PuzzleMapEditor.CameraBounds(map, playerObject.transform.position).center.y,
                    Is.EqualTo(8f).Within(.001f));
                map.cameraSettings.bottomBlankRows = 4;
                cameraObject.GetComponent<PuzzleFollowCamera>().Configure(map, playerObject.transform);
                Assert.That(cameraObject.transform.position.y, Is.EqualTo(6f).Within(.001f));
                Assert.That(ANIMOL.Presentation.Editor.PuzzleMapEditor.CameraBounds(map, playerObject.transform.position).center.y,
                    Is.EqualTo(6f).Within(.001f));
                map.cameraSettings.bottomBlankRows = 0;
                cameraObject.GetComponent<PuzzleFollowCamera>().Configure(map, playerObject.transform);
                Assert.That(cameraObject.transform.position.y, Is.EqualTo(10f).Within(.001f));
                Assert.That(map.cameraSettings.topInset, Is.EqualTo(originalInset));
                Assert.That(map.cameraSettings.visibleRows, Is.EqualTo(originalRows));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(map);
            }
        }
    }
}
