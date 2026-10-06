using System.Collections;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace ANIMOL.Tests.PlayMode
{
    [Category("CellBoundsRegression")]
    public sealed class CellBoundsPlayModeTests
    {
        [UnityTest] public IEnumerator NonAlignedCellBoundsKeepFourPhysicalWallsAndPlayerInside()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();var root=new GameObject("Cell bounds runtime");var player=new GameObject("Boundary probe");
            try
            {
                map.EditorInitializeIdentity("TEST-CELL-BOUNDS-PLAY","T01");map.EditorInitializeBoundsFromAuthoredContent(1);
                Assert.That(map.EditorTrySetCellBounds(new RectInt(-19,-7,37,23),false,out _),Is.True);
                var loader=root.AddComponent<StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureStandaloneDevelopmentOwner();loader.Load(map);
                var boundary=root.GetComponent<StageMapWorldBoundary>();Assert.That(boundary.WorldBounds,Is.EqualTo(new Rect(-19,-7,37,23)));
                Assert.That(root.GetComponentsInChildren<BoxCollider2D>().Length,Is.EqualTo(4));
                player.transform.position=new Vector3(17,1,0);var box=player.AddComponent<BoxCollider2D>();box.size=new Vector2(.5f,.5f);
                var body=player.AddComponent<Rigidbody2D>();body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeRotation;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
                body.linearVelocity=Vector2.right*10;Physics2D.SyncTransforms();
                for(int i=0;i<25;i++)yield return new WaitForFixedUpdate();
                Assert.That(body.position.x,Is.LessThanOrEqualTo(17.76f));Assert.That(body.position.x,Is.GreaterThan(17));
                Assert.That(root.GetComponentsInChildren<FreeShapeTerrainRenderer>().SelectMany(r=>r.GetComponentsInChildren<Collider2D>()),Is.Empty);
            }
            finally{Object.Destroy(root);Object.Destroy(player);Object.Destroy(map);}
            yield return null;
        }
    }
}
