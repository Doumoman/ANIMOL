using System.Collections;
using System.Linq;
using System.Reflection;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace ANIMOL.Tests
{
    public sealed class TerrainStructurePlayModeTests
    {
        [UnityTest]
        public IEnumerator ActualPlayerLandsOnAllTwentyMainPartsAndArchVoidRemainsOpen()
        {
            var registry = StageTerrainStructureRegistry.Load();
            var map = ScriptableObject.CreateInstance<StageMapDefinition>();
            var root = new GameObject("TerrainPlayIntegration", typeof(Grid));
            var terrain = new GameObject("OperationalTerrain", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
            terrain.transform.SetParent(root.transform, false);
            var loader = root.AddComponent<StageMapRuntimeLoader>();
            typeof(StageMapRuntimeLoader).GetField("terrain", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(loader, terrain.GetComponent<Tilemap>());
            var player = new GameObject("ActualDevPlayer", typeof(Rigidbody2D), typeof(BoxCollider2D));
            player.GetComponent<BoxCollider2D>().size = new Vector2(.5f, 1f);
            var body = player.GetComponent<Rigidbody2D>(); body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var controller = player.AddComponent<DevPlayerController>();
            try
            {
                map.EditorInitializeIdentity("TERRAIN-PLAY-QA", "T01");
                map.EditorInitializeVariableChunksFromAuthoredContent(1f);
                map.EditorTrySetChunkBounds(new RectInt(-2,-2,6,6),false,out _);
                foreach (var entry in registry.Catalog.entries.Where(e => e.role == "Platform" || (e.role == "Bridge" && e.kind == "Structure")))
                {
                    // New identity in a fresh transient map keeps real production maps unchanged.
                    Object.Destroy(map); map = ScriptableObject.CreateInstance<StageMapDefinition>();
                    map.EditorInitializeIdentity("TERRAIN-PLAY-QA",entry.themeId);
                    map.EditorInitializeVariableChunksFromAuthoredContent(1f);
                    map.EditorTrySetChunkBounds(new RectInt(-2,-2,6,6),false,out _);
                    Assert.That(AnimolTerrainPlacementEngine.TryPlace(registry.Catalog,map.ReadTerrain(registry),entry.id,Vector2Int.zero,out var candidate,out var error),Is.True,error);
                    map.EditorApplyTerrainCandidate(registry,candidate); loader.Load(map);
                    player.transform.position = new Vector3(2.5f,entry.height+1.5f,0); body.linearVelocity=Vector2.zero;
                    for (int i=0;i<65;i++) yield return new WaitForFixedUpdate();
                    Assert.That(player.GetComponent<Collider2D>().bounds.min.y,Is.EqualTo(entry.height).Within(.08f),entry.id);
                    Assert.That(controller.IsGrounded,Is.True,entry.id);
                    var runtime=root.GetComponent<StageTerrainStructureRuntime>();
                    Assert.That(runtime.LogicalGrid.solids.Count,Is.EqualTo(entry.solidCount));
                    Assert.That(runtime.Instances.Values.Single().GetComponentsInChildren<Collider2D>(),Is.Empty);
                }
            }
            finally { Object.Destroy(player); Object.Destroy(root); Object.Destroy(map); }
            yield return null;
        }
    }
}
