using System.Collections;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    [Category("CellBoundsRegression")]
    public sealed class ThemeFinishV6PlayModeTests
    {
        [UnityTest] public IEnumerator PhysicalBodyFallsThroughEmptyHoleAndLandsOnInnerFloorForEveryThemeStyle()
        {
            var terrain=StageTerrainStructureRegistry.Load();var root=new GameObject("V6 hole physics");var loader=root.AddComponent<StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureStandaloneDevelopmentOwner();
            var probe=new GameObject("Hole traversal probe",typeof(Rigidbody2D),typeof(BoxCollider2D));var body=probe.GetComponent<Rigidbody2D>();body.constraints=RigidbodyConstraints2D.FreezeRotation;var box=probe.GetComponent<BoxCollider2D>();box.size=new Vector2(.5f,1);StageMapDefinition map=null;
            try
            {
                foreach(var style in FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,6).styles)
                {
                    if(map!=null)Object.Destroy(map);map=ScriptableObject.CreateInstance<StageMapDefinition>();map.EditorInitializeIdentity("V6-HOLE-PLAY",style.themeId);map.EditorInitializeBoundsFromAuthoredContent(1);map.EditorTrySetCellBounds(new RectInt(-32,-32,64,64),false,out _);
                    var rows=new[]{"#####","#...#","#...#","#...#","#...#","#...#","#####"};
                    var points=FreeShapeTopology.FromRows(rows,new Vector2Int(-17,-17));
                    Assert.That(AnimolTerrainPlacementEngine.TryEditFreeShape(terrain.Catalog,map.ReadTerrain(terrain),points,false,style.styleId,out var dto,out var error),Is.True,error);dto.freeShape.artVersion=6;map.EditorApplyTerrainCandidate(terrain,dto);loader.Load(map);
                    probe.transform.position=new Vector3(-14.5f,-12.5f,0);body.linearVelocity=Vector2.zero;
                    for(int i=0;i<50;i++)yield return new WaitForFixedUpdate();
                    Assert.That(box.bounds.min.y,Is.EqualTo(-16).Within(.08f),style.styleId);
                    Assert.That(root.GetComponentInChildren<FreeShapeTerrainRenderer>().GetComponentsInChildren<Collider2D>(),Is.Empty);
                }
            }
            finally{Object.Destroy(probe);Object.Destroy(root);if(map!=null)Object.Destroy(map);}yield return null;
        }
    }
}
