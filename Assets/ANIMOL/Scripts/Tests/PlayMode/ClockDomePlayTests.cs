using System.Collections;
using System.Linq;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace ANIMOL.Presentation.Tests
{
    public sealed class ClockDomePlayTests
    {
        [UnityTest] public IEnumerator TerrainAndDecorativeDeckNeverChangeLogicalOccupancy()
        {
            var art = UnityEditor.AssetDatabase.LoadAssetAtPath<ClockDomeArt>("Assets/ANIMOL/Data/Art/ClockDome/EnvironmentArt.asset");
            var map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(3,3);
            for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++) map.SetTerrain(new Vector2Int(x,y), TerrainMaterial.Grass);
            map.SetDevice(new DeviceCell { position = Vector2Int.one, kind = DeviceKind.Conveyor, direction = Vector2Int.right });
            var before = JsonUtility.ToJson(map); var root = new GameObject("Clock renderer isolation"); var renderer = root.AddComponent<ClockDomeRenderer>(); renderer.Bind(map,art); yield return null;
            Assert.AreEqual(before,JsonUtility.ToJson(map)); Assert.AreEqual("o00_i00", renderer.Floor.GetSprite(new Vector3Int(1,1,0)).name);
            Assert.AreEqual(-.15f, renderer.Underside.transform.localPosition.y); Assert.AreEqual(Vector3.one,renderer.Grid.cellSize);
            Assert.Less(renderer.Underside.GetComponent<TilemapRenderer>().sortingOrder,renderer.Floor.GetComponent<TilemapRenderer>().sortingOrder);
            foreach (var c in map.terrain) Assert.IsFalse(renderer.Telescope.bounds.Contains((Vector3)(Vector2)c.position));
            var count = renderer.RefreshedCells; map.RemoveTerrain(Vector2Int.one); Assert.AreEqual(9,renderer.RefreshedCells-count); Assert.IsNull(renderer.Floor.GetTile(new Vector3Int(1,1,0)));
            map.SetTerrain(Vector2Int.one,TerrainMaterial.Stone); Assert.AreEqual("o00_i00", renderer.Floor.GetSprite(new Vector3Int(1,1,0)).name);
            map.Resize(2,2); yield return null; Assert.AreEqual(9,map.terrain.Count); Assert.AreEqual(1,map.devices.Count); Assert.IsNull(renderer.Floor.GetTile(new Vector3Int(2,2,0))); Assert.AreEqual(3,renderer.Room.columnRows);
            Assert.IsEmpty(root.GetComponentsInChildren<Collider2D>()); Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
            Object.Destroy(root); Object.Destroy(map); yield return null;
        }
        [UnityTest] public IEnumerator PendulumSpritePlaybackAndSplitCapKeepWorldAnchorsFixed()
        {
            var art = UnityEditor.AssetDatabase.LoadAssetAtPath<ClockDomeArt>("Assets/ANIMOL/Data/Art/ClockDome/EnvironmentArt.asset"); var root = Object.Instantiate(art.roomPrefab); var room = root.GetComponent<ClockDomeRoom>(); room.Assemble(4,true); yield return null;
            var images = root.GetComponentsInChildren<SpriteRenderer>(); Assert.AreEqual(3,images.Count(r=>r.name.Contains("segment"))); Assert.AreEqual(0,images.Count(r=>r.name=="dome_cap_960x400"));
            var animation = root.GetComponentsInChildren<ClockDomeAnimation>().First(); animation.motionEnabled=false; animation.Advance(1.6f); Assert.AreSame(art.pendulum.frames[0],animation.visual.sprite);
            var origin=animation.transform.position; animation.motionEnabled=true; animation.Advance(.4f); var expected=art.pendulum.At(animation.Elapsed); Assert.AreSame(expected,animation.visual.sprite);
            yield return new WaitForSecondsRealtime(.1f); Assert.AreEqual(origin,animation.transform.position); Assert.AreEqual(Quaternion.identity,animation.transform.rotation); Assert.AreEqual(1,Time.timeScale);
            Object.Destroy(root); yield return null;
        }
        [UnityTest] public IEnumerator ExistingLetterDeviceReferencesAndBeamSurviveNewEnvironment()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/QA/ANIMOL_LetterObservatory_Art_QA.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)); yield return null; yield return null;
            var qa = Object.FindFirstObjectByType<DeviceArtQaController>(); var environment = Object.FindFirstObjectByType<ClockDomeInspection>(); Assert.AreEqual(255,qa.views.Length); Assert.AreEqual(9,qa.masters.Length);
            Assert.AreEqual(4,environment.GetComponentsInChildren<Tilemap>().Length); Assert.IsTrue(environment.replacedEnvironment.All(r=>r.forceRenderingOff));
            foreach (var master in qa.masters)
            {
                Assert.AreEqual(1,master.GetComponentsInChildren<DeviceVisualView>().Length); Assert.IsFalse(master.BlocksInput); var position=master.transform.position;
                var state=master.states.First(s=>s.sequence && !string.IsNullOrEmpty(s.eventName)); Assert.IsTrue(master.Notify("clock-env-"+master.deviceId,state.eventName,state.name)); Assert.IsFalse(master.Notify("clock-env-"+master.deviceId,state.eventName,state.name)); master.Advance(.1f); Assert.AreEqual(position,master.transform.position);
            }
            Assert.AreEqual(6,Object.FindFirstObjectByType<BeamPathView>().ActiveCount); Assert.AreEqual("COST --",Object.FindFirstObjectByType<PuzzleHudView>().cost.text);
            Assert.IsEmpty(environment.GetComponentsInChildren<Collider2D>()); Assert.IsEmpty(environment.GetComponentsInChildren<Collider>());
            Assert.AreEqual(new Vector3(.15f,.30f,.45f),environment.GetComponent<SkyIslandBackdrop>().ParallaxFactors);
        }
    }
}
