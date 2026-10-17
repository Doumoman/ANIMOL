using System.Collections;
using System.Linq;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace ANIMOL.Presentation.Tests
{
    public sealed class MemorySkyTheaterPlayTests
    {
        [UnityTest] public IEnumerator ExistingQaKeeps11DeviceWritersWhileReplacingOldEnvironmentAcrossReloads()
        {
            #if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/QA/ANIMOL_MemoryTheater_Art_QA.unity", new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            #endif
            yield return null; yield return null;
            var qa = Object.FindFirstObjectByType<ANIMOL.Presentation.DeviceArtQaController>(); var environment = Object.FindFirstObjectByType<MemorySkyTheaterInspection>();
            Assert.AreEqual(11, qa.masters.Length); Assert.AreEqual(369, qa.views.Length); Assert.AreEqual(2, environment.replacedEnvironment.Length);
            foreach (var view in qa.views) view.ResetVisual(); yield return null;
            Assert.IsTrue(environment.replacedEnvironment.All(r => r.forceRenderingOff));
            Assert.AreEqual(369, Object.FindObjectsByType<ANIMOL.Presentation.DeviceVisualView>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(3, environment.GetComponentsInChildren<Tilemap>().Length);
            foreach (var master in qa.masters) Assert.AreEqual(1, master.GetComponentsInChildren<ANIMOL.Presentation.DeviceVisualView>().Length);
        }
        [UnityTest] public IEnumerator ExpandingActualGroundPreservesRevealClockAndCarpetEdits()
        {
            #if UNITY_EDITOR
            var art = UnityEditor.AssetDatabase.LoadAssetAtPath<MemorySkyTheaterArt>("Assets/ANIMOL/Data/Art/MemorySkyTheater/EnvironmentArt.asset");
            #else
            MemorySkyTheaterArt art = null;
            #endif
            var map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(4, 4);
            for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            var root = new GameObject("Expanding theater border"); var view = root.AddComponent<MemorySkyTheaterRenderer>(); view.Bind(map, art);
            view.SetCarpet(Vector2Int.zero, true); yield return new WaitForSecondsRealtime(.3f);
            var reveal = root.GetComponentsInChildren<MemorySkyTheaterAnimation>().First(a => a.sequence == art.footlights); var elapsed = reveal.Elapsed;
            Assert.IsTrue(reveal.Finished); map.Resize(9, 7);
            for (var y = 0; y < 7; y++) for (var x = 0; x < 9; x++) if (!map.Walkable(new Vector2Int(x, y))) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            yield return null;
            reveal = root.GetComponentsInChildren<MemorySkyTheaterAnimation>().First(a => a.sequence == art.footlights);
            Assert.IsTrue(reveal.Finished); Assert.GreaterOrEqual(reveal.Elapsed, elapsed); Assert.IsTrue(view.Carpet(Vector2Int.zero));
            Assert.AreEqual(9, root.GetComponentInChildren<MemorySkyTheaterFrame>().GetComponentsInChildren<SpriteRenderer>().Length);
            foreach (var image in root.GetComponentsInChildren<SpriteRenderer>()) foreach (var cell in map.terrain)
                Assert.IsFalse(image.bounds.Contains(new Vector3(cell.position.x, cell.position.y, 0)), image.name);
            Assert.AreEqual(63, map.terrain.Count); Assert.AreEqual(1, Time.timeScale); Assert.IsEmpty(root.GetComponentsInChildren<Collider2D>());
            Object.Destroy(root); Object.Destroy(map); yield return null;
        }
        [UnityTest] public IEnumerator CarpetTerrainResizeAndRevealRemainPresentationOnly()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(3, 3);
            for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            map.SetDevice(new DeviceCell { position = Vector2Int.one, kind = DeviceKind.Conveyor, direction = Vector2Int.right });
            var before = map.terrain.ToArray(); var devices = map.devices.ToArray();
            #if UNITY_EDITOR
            var art = UnityEditor.AssetDatabase.LoadAssetAtPath<MemorySkyTheaterArt>("Assets/ANIMOL/Data/Art/MemorySkyTheater/EnvironmentArt.asset");
            #else
            MemorySkyTheaterArt art = null;
            #endif
            var layout = Object.Instantiate(art.defaultLayout); layout.fitBorderToMap = false; layout.velvet.Clear();
            var root = new GameObject("Theater appearance test"); var view = root.AddComponent<MemorySkyTheaterRenderer>(); view.Bind(map, art, layout);
            yield return null;
            Assert.AreEqual(-.175f, view.Underside.transform.localPosition.y); Assert.AreEqual(Vector3.one, view.Grid.cellSize);
            Assert.Less(view.Underside.GetComponent<TilemapRenderer>().sortingOrder, view.Wood.GetComponent<TilemapRenderer>().sortingOrder);
            Assert.Less(view.Wood.GetComponent<TilemapRenderer>().sortingOrder, view.Velvet.GetComponent<TilemapRenderer>().sortingOrder);
            Assert.IsEmpty(root.GetComponentsInChildren<Collider2D>()); Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
            view.SetCarpet(Vector2Int.one, true); Assert.AreEqual("o15_i00", view.Velvet.GetSprite(new Vector3Int(1, 1, 0)).name);
            CollectionAssert.AreEqual(before, map.terrain); CollectionAssert.AreEqual(devices, map.devices); Assert.IsTrue(map.Walkable(Vector2Int.one));
            var count = view.RefreshedCells; map.RemoveTerrain(Vector2Int.one); Assert.AreEqual(9, view.RefreshedCells - count);
            Assert.IsNull(view.Velvet.GetTile(new Vector3Int(1, 1, 0))); Assert.IsNull(view.Wood.GetTile(new Vector3Int(1, 1, 0)));
            map.SetTerrain(Vector2Int.one, TerrainMaterial.Grass); Assert.IsTrue(view.Carpet(Vector2Int.one));
            yield return new WaitForSecondsRealtime(.3f);
            var reveal = root.GetComponentsInChildren<MemorySkyTheaterAnimation>().First(a => a.sequence == art.footlights);
            Assert.IsTrue(reveal.Finished); var elapsed = reveal.Elapsed; var origin = reveal.transform.position;
            map.Resize(2, 2); view.Bind(map, art, layout); yield return null;
            Assert.AreEqual(9, map.terrain.Count); Assert.AreEqual(1, map.devices.Count); Assert.IsTrue(reveal.Finished); Assert.GreaterOrEqual(reveal.Elapsed, elapsed);
            Assert.AreEqual(origin, reveal.transform.position); Assert.IsTrue(map.Walkable(Vector2Int.one));
            Assert.AreSame(art.footlights.frames.Last(), reveal.visual.sprite); Assert.AreEqual(1, Time.timeScale);
            Object.Destroy(root); Object.Destroy(map); Object.Destroy(layout); yield return null;
        }
    }
}
