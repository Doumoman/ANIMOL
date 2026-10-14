using System.Collections;
using System.Linq;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace ANIMOL.Presentation.Tests
{
    public sealed class SkyIslandPlayTests
    {
        [UnityTest] public IEnumerator TerrainEditResizeAndThemeSwitchKeepVisualLayersOutOfPhysics()
        {
            var map = ScriptableObject.CreateInstance<PuzzleMap>(); map.Resize(3, 3);
            var theme = ScriptableObject.CreateInstance<PuzzleTheme>(); map.theme = theme;
            theme.terrain = new TerrainArt[0];
            #if UNITY_EDITOR
            theme.skyIsland = UnityEditor.AssetDatabase.LoadAssetAtPath<SkyIslandArt>("Assets/ANIMOL/Data/Art/StarwayIsland/IslandArt.asset");
            #endif
            Assert.IsNotNull(theme.skyIsland);
            for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++) map.SetTerrain(new Vector2Int(x, y), TerrainMaterial.Grass);
            var host = new GameObject("Island view test"); var view = host.AddComponent<PuzzleMapView>(); view.Bind(map); yield return null;
            var island = host.GetComponent<SkyIslandMapRenderer>(); var p = Vector2Int.one; var c = new Vector3Int(1, 1, 0);
            Assert.AreEqual(SkyIslandArt.UndersideY, island.Underside.transform.localPosition.y);
            Assert.Less(island.Underside.GetComponent<TilemapRenderer>().sortingOrder, island.Grass.GetComponent<TilemapRenderer>().sortingOrder);
            Assert.AreEqual(Vector3.one, island.Grass.layoutGrid.cellSize); Assert.IsEmpty(host.GetComponentsInChildren<Collider2D>());
            Assert.IsEmpty(host.GetComponentsInChildren<Collider>());
            var before = view.RefreshedCells; map.RemoveTerrain(new Vector2Int(2, 2));
            Assert.AreEqual(9, view.RefreshedCells - before); Assert.AreEqual("o00_i02", island.Grass.GetSprite(c).name);
            Assert.IsTrue(map.Walkable(p)); Assert.IsFalse(map.Walkable(new Vector2Int(2, 2)));
            map.Resize(2, 2); yield return null; Assert.AreEqual("o03_i00", island.Grass.GetSprite(c).name); Assert.AreEqual(8, map.terrain.Count);
            map.Resize(3, 3); yield return null; Assert.AreEqual("o00_i02", island.Grass.GetSprite(c).name);
            theme.skyIsland = null; view.Rebuild(); yield return null; Assert.IsEmpty(host.GetComponentsInChildren<Tilemap>()); Assert.IsTrue(map.Walkable(p));
            Object.Destroy(host); Object.Destroy(map); Object.Destroy(theme); yield return null;
        }
    }
}
