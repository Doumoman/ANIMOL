using System.Collections;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace ANIMOL.Tests
{
    [TestFixture(1)]
    [TestFixture(2)]
    [TestFixture(4)]
    public sealed class FreeShapePlayModeTests
    {
        private readonly int artVersion;
        public FreeShapePlayModeTests(int artVersion){this.artVersion=artVersion;}
        [UnityTest] public IEnumerator ActualPlayerLandsOnAllTwentyStylesAndNoArtOwnsPhysics()
        {
            var registry=StageTerrainStructureRegistry.Load();var root=new GameObject("FreeShape play fixture");var loader=root.AddComponent<StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureStandaloneDevelopmentOwner();
            var player=new GameObject("Actual Dev player",typeof(Rigidbody2D),typeof(BoxCollider2D));var box=player.GetComponent<BoxCollider2D>();box.size=new Vector2(.5f,1);var body=player.GetComponent<Rigidbody2D>();body.constraints=RigidbodyConstraints2D.FreezeRotation;var controller=player.AddComponent<DevPlayerController>();StageMapDefinition map=null;
            try {foreach(var style in FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,artVersion).styles)
            {
                if(map!=null)Object.Destroy(map);map=ScriptableObject.CreateInstance<StageMapDefinition>();map.EditorInitializeIdentity("FREE-PLAY",style.themeId);map.EditorInitializeVariableChunksFromAuthoredContent(1);map.EditorTrySetChunkBounds(new RectInt(-2,-2,5,5),false,out _);
                var points=Enumerable.Range(-2,10).SelectMany(x=>Enumerable.Range(0,3).Select(y=>new Vector2Int(x,y)));
                Assert.That(AnimolTerrainPlacementEngine.TryEditFreeShape(registry.Catalog,map.ReadTerrain(registry),points,false,style.styleId,out var candidate,out var error),Is.True,error);candidate.freeShape.artVersion=artVersion;map.EditorApplyTerrainCandidate(registry,candidate);loader.Load(map);
                player.transform.position=new Vector3(.5f,5,0);body.linearVelocity=Vector2.zero;
                for(int i=0;i<55;i++)yield return new WaitForFixedUpdate();
                Assert.That(box.bounds.min.y,Is.EqualTo(3).Within(.08f),style.styleId);Assert.That(controller.IsGrounded,Is.True,style.styleId);
                var renderer=root.GetComponentInChildren<FreeShapeTerrainRenderer>();Assert.That(renderer.GetComponentsInChildren<Collider2D>(),Is.Empty);Assert.That(root.GetComponent<StageTerrainStructureRuntime>().LogicalGrid.solids.Count,Is.EqualTo(30));
            }}finally{Object.Destroy(player);Object.Destroy(root);if(map!=null)Object.Destroy(map);}yield return null;
        }
        [UnityTest] public IEnumerator ExistingOneWayLetsBodyRiseThroughAndSupportsFallAlongsideFreeSolid()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();var root=new GameObject("FreeShape one-way fixture");var loader=root.AddComponent<StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureStandaloneDevelopmentOwner();var bodyObject=new GameObject("One way physical probe",typeof(Rigidbody2D),typeof(BoxCollider2D));var body=bodyObject.GetComponent<Rigidbody2D>();body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeRotation;bodyObject.GetComponent<BoxCollider2D>().size=new Vector2(.5f,1);
            try{
                map.EditorInitializeIdentity("FREE-ONEWAY","T01");map.EditorInitializeVariableChunksFromAuthoredContent(1);map.EditorTrySetChunkBounds(new RectInt(-1,-1,4,4),false,out _);
                map.EditorSetCell(12,3,StageMapLayer.Terrain,"M9_MOON_ONE_WAY_16PX_PLACEHOLDER");var registry=StageTerrainStructureRegistry.Load();AnimolTerrainPlacementEngine.TryEditFreeShape(registry.Catalog,map.ReadTerrain(registry),new[]{Vector2Int.zero},false,"T01_A",out var candidate,out _);candidate.freeShape.artVersion=artVersion;map.EditorApplyTerrainCandidate(registry,candidate);loader.Load(map);
                Assert.That(root.GetComponentsInChildren<PlatformEffector2D>().Length,Is.EqualTo(1));bodyObject.transform.position=new Vector3(12.5f,1,0);body.linearVelocity=new Vector2(0,8);
                for(int i=0;i<32;i++)yield return new WaitForFixedUpdate();var contacts=new ContactPoint2D[16];int count=body.GetContacts(contacts);Assert.That(body.position.y,Is.GreaterThan(4.5f),string.Join(";",contacts.Take(count).Select(c=>c.collider.name+" parent="+c.collider.transform.parent?.name+" normal="+c.normal+" other="+c.otherCollider.name)));body.linearVelocity=new Vector2(0,-8);
                for(int i=0;i<42;i++)yield return new WaitForFixedUpdate();Assert.That(bodyObject.GetComponent<BoxCollider2D>().bounds.min.y,Is.EqualTo(4).Within(.08f));
            }finally{Object.Destroy(bodyObject);Object.Destroy(root);Object.Destroy(map);}yield return null;
        }
    }
}
