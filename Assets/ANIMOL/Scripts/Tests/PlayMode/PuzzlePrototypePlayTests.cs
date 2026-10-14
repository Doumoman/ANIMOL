#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using ANIMOL.Puzzle;
namespace ANIMOL.Tests
{
    public sealed class PuzzlePrototypePlayTests
    {
        const string Root="Assets/ANIMOL/Data/Puzzle/Prototypes/";
        GameObject host,camera;
        PuzzlePrototypeController controller;
        [UnitySetUp] public IEnumerator Setup()
        {
            camera=new GameObject("Main Camera",typeof(Camera));camera.tag="MainCamera";
            host=new GameObject("Test prototype");controller=host.AddComponent<PuzzlePrototypeController>();controller.secondsPerCell=.005f;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup(){Object.Destroy(host);Object.Destroy(camera);yield return null;}
        [UnityTest] public IEnumerator AllFourThemedStagesClearThroughSwipeInputWithExistingHudAndArt()
        {
            controller.hudPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/hud_visual.prefab");
            foreach(var name in new[]{"starway","memory_theater","letter_observatory","twilight_sea"})
            {
                var map=AssetDatabase.LoadAssetAtPath<PuzzleMap>(Root+name+".asset");controller.Load(map);yield return null;
                var input=host.GetComponent<PuzzleSwipeInput>();
                input.BeginGesture(new Vector2(200,300),true);Assert.That(input.EndGesture(new Vector2(400,300)),Is.False);
                foreach(char c in map.scenario.verificationRoute)
                {
                    var direction=c=='N'?Vector2Int.up:c=='E'?Vector2Int.right:c=='S'?Vector2Int.down:Vector2Int.left;
                    input.BeginGesture(new Vector2(200,300),false);Assert.That(input.EndGesture(new Vector2(200,300)+(Vector2)direction*Mathf.Max(Screen.width*.2f,200)),Is.True,name+" "+controller.Session.Cell);
                    yield return WaitIdle();
                    Assert.That(controller.BlocksInput,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));
                    Assert.That((Vector2)controller.Player.position,Is.EqualTo((Vector2)controller.Session.Cell));
                }
                Assert.That(controller.Session.Won,Is.True);Assert.That(controller.Devices.MissingBindings,Is.Zero);
                Assert.That(controller.GetComponentsInChildren<Collider2D>(),Is.Empty);Assert.That(map.terrain.Count,Is.EqualTo(controller.RuntimeMap.terrain.Count));
            }
        }
        [UnityTest] public IEnumerator BoatCarriesActualPlayerTransformWhileCameraTracksMotion()
        {
            var map=AssetDatabase.LoadAssetAtPath<PuzzleMap>(Root+"Device11_Boat.asset");controller.Load(map);yield return null;
            controller.RequestMove(Vector2Int.right);yield return WaitIdle();controller.secondsPerCell=.3f;
            controller.RequestMove(Vector2Int.right);yield return null;yield return null;
            var boat=controller.Devices.Actor("boat");Assert.That(Vector2.Distance(boat.position,controller.Player.position),Is.LessThan(.01f));
            yield return WaitIdle();Assert.That(controller.Session.Cell,Is.EqualTo(new Vector2Int(3,2)));
        }
        [UnityTest] public IEnumerator DistantPortalCutsCameraAndNeverInterpolatesAcrossGap()
        {
            var original=AssetDatabase.LoadAssetAtPath<PuzzleMap>(Root+"Device16_Portal.asset");var map=Object.Instantiate(original);var scenario=Object.Instantiate(map.scenario);map.scenario=scenario;
            map.Resize(45,7);for(int x=8;x<44;x++)map.SetTerrain(new Vector2Int(x,2),TerrainMaterial.Grass);
            scenario.devices.First(d=>d.id=="portalA").target=new Vector2Int(38,2);scenario.devices.First(d=>d.id=="portalB").position=new Vector2Int(38,2);
            scenario.devices.First(d=>d.id=="belt").role=PrototypeRole.Ice;map.exit=new Vector2Int(43,2);map.bubbles.Clear();map.targetBubbles=0;
            controller.Load(map);yield return null;controller.secondsPerCell=.04f;controller.RequestMove(Vector2Int.right);
            while(controller.IsMoving){var x=controller.FollowCamera.transform.position.x;Assert.That(x>8&&x<32,Is.False);yield return null;}
            Assert.That(controller.Session.PortalCuts,Is.EqualTo(1));Assert.That(controller.Session.Direction,Is.EqualTo(Vector2Int.right));Assert.That(controller.Session.Won,Is.True);
            Object.Destroy(map);Object.Destroy(scenario);
        }
        IEnumerator WaitIdle(){var deadline=Time.realtimeSinceStartup+5;while(controller.IsMoving&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(controller.IsMoving,Is.False);}
    }
}
#endif
