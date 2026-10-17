using System.Collections;
using System.Linq;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
namespace ANIMOL.Presentation.Tests
{
    public sealed class TwilightCoastPlayTests
    {
        [UnityTest] public IEnumerator TerrainAndDecorativeDeckNeverChangeLogicalOccupancy()
        {
            var art = UnityEditor.AssetDatabase.LoadAssetAtPath<TwilightCoastArt>("Assets/ANIMOL/Data/Art/TwilightCoast/EnvironmentArt.asset");
            var map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(3,3);
            for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++) map.SetTerrain(new Vector2Int(x,y), TerrainMaterial.Grass);
            map.SetDevice(new DeviceCell { position = Vector2Int.one, kind = DeviceKind.Conveyor, direction = Vector2Int.right });
            var before = JsonUtility.ToJson(map); var root = new GameObject("Clock renderer isolation"); var renderer = root.AddComponent<TwilightCoastRenderer>(); renderer.Bind(map,art); yield return null;
            Assert.AreEqual(before,JsonUtility.ToJson(map)); Assert.AreEqual("o00_i00", renderer.Floor.GetSprite(new Vector3Int(1,1,0)).name);
            Assert.AreEqual(-.1f, renderer.Underside.transform.localPosition.y); Assert.AreEqual(Vector3.one,renderer.Grid.cellSize);
            Assert.Less(renderer.Underside.GetComponent<TilemapRenderer>().sortingOrder,renderer.Floor.GetComponent<TilemapRenderer>().sortingOrder);
            foreach (var c in map.terrain) Assert.IsFalse(renderer.GetComponentsInChildren<TwilightCoastPalm>().First().visual.bounds.Contains((Vector3)(Vector2)c.position));
            var sunCenter = renderer.Horizon.transform.TransformPoint(new Vector3(6,-2.25f,0));
            Assert.IsFalse(renderer.Deck.GetComponent<TilemapRenderer>().bounds.Contains(sunCenter) && renderer.Deck.HasTile(renderer.Grid.WorldToCell(sunCenter)));
            var count = renderer.RefreshedCells; map.RemoveTerrain(Vector2Int.one); Assert.AreEqual(9,renderer.RefreshedCells-count); Assert.IsNull(renderer.Floor.GetTile(new Vector3Int(1,1,0)));
            map.SetTerrain(Vector2Int.one,TerrainMaterial.Stone); Assert.AreEqual("o00_i00", renderer.Floor.GetSprite(new Vector3Int(1,1,0)).name);
            map.Resize(2,2); yield return null; Assert.AreEqual(9,map.terrain.Count); Assert.AreEqual(1,map.devices.Count); Assert.IsNull(renderer.Floor.GetTile(new Vector3Int(2,2,0))); Assert.GreaterOrEqual(renderer.Horizon.leftRepeats,2);
            Assert.IsEmpty(root.GetComponentsInChildren<Collider2D>()); Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
            Object.Destroy(root); Object.Destroy(map); yield return null;
        }
        [UnityTest] public IEnumerator AnimatedNearReplacesOneLayerWithoutChangingFollowCameraOrTerrain()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/QA/ANIMOL_TwilightCoast_Puzzle_QA.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)); yield return null; yield return null;
            var player=Object.FindFirstObjectByType<PuzzleController>(); var backdrop=Object.FindFirstObjectByType<SkyIslandBackdrop>(); var camera=player.FollowCamera.GetComponent<Camera>(); var position=camera.transform.position; var rows=camera.orthographicSize; var rect=camera.rect;
            backdrop.animateNear=false; var time=backdrop.NearElapsed; backdrop.AdvanceNear(.7f); var sprite=backdrop.NearSequence.At(time+.7f);
            Assert.IsTrue(backdrop.transform.Find("Near").GetComponentsInChildren<SpriteRenderer>().All(r=>r.sprite==sprite)); Assert.AreEqual(1,backdrop.transform.Cast<Transform>().Count(t=>t.name=="Near"));
            backdrop.SetNearAnimation(null); Assert.IsTrue(backdrop.transform.Find("Near").GetComponentsInChildren<SpriteRenderer>().All(r=>r.sprite==player.RuntimeMap.theme.twilightCoast.near));
            var palm=player.GetComponentInChildren<TwilightCoastPalm>(); var origin=palm.transform.position; palm.Advance(.9f); yield return null;
            Assert.AreEqual(origin,palm.transform.position); Assert.AreEqual(Quaternion.identity,palm.transform.rotation); Assert.AreEqual(position,camera.transform.position); Assert.AreEqual(rows,camera.orthographicSize); Assert.AreEqual(rect,camera.rect); Assert.IsFalse(player.BlocksInput); Assert.AreEqual(1,Time.timeScale);
        }
        [UnityTest] public IEnumerator CoastPreservesExistingEightDeviceWritersFerryAndResolvedPreview()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/QA/ANIMOL_TwilightSea_Art_QA.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)); yield return null; yield return null;
            var qa=Object.FindFirstObjectByType<DeviceArtQaController>(); var environment=Object.FindFirstObjectByType<TwilightCoastInspection>(); Assert.AreEqual(307,qa.views.Length); Assert.AreEqual(8,qa.masters.Length); Assert.AreEqual(1,qa.previews.Length);
            Assert.AreEqual(1,Object.FindObjectsByType<FerryVisualView>(FindObjectsSortMode.None).Length); Assert.AreEqual(4,environment.GetComponentsInChildren<Tilemap>().Length);
            foreach(var master in qa.masters) { Assert.IsFalse(master.BlocksInput); Assert.AreEqual(1,master.GetComponentsInChildren<DeviceVisualView>().Length); }
            Assert.AreEqual(new Vector3(.12f,.30f,.50f),environment.GetComponent<SkyIslandBackdrop>().ParallaxFactors);
            Assert.AreEqual("COST --",Object.FindFirstObjectByType<PuzzleHudView>().cost.text); Assert.IsEmpty(environment.GetComponentsInChildren<Collider2D>()); Assert.IsEmpty(environment.GetComponentsInChildren<Collider>());
        }
    }
}
