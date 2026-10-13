using System.Collections;
using System.Linq;
using ANIMOL.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ANIMOL.Tests
{
    public sealed class MoonGraphicsQaPlayModeTests
    {
        [UnitySetUp] public IEnumerator Load()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity",new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;yield return null;yield return new WaitForFixedUpdate();
        }
        [UnityTest] public IEnumerator BothPortraitSizesKeepTwelveCellsAndAllDeviceCollidersVisible()
        {
            var qa=Object.FindFirstObjectByType<MoonGraphicsQaScene>();Assert.That(qa.Devices.Count,Is.EqualTo(8));
            var camera=Camera.main;
            foreach(var height in new[]{1920,2400})
            {
                var rt=new RenderTexture(1080,height,16);camera.targetTexture=rt;camera.GetComponent<PortraitWorldCameraPolicy>().Apply();
                Assert.That(camera.orthographicSize*2*camera.aspect,Is.EqualTo(12).Within(.001f));
                foreach(var device in qa.Devices)
                {
                    var bounds=device.GetComponent<Collider2D>().bounds;
                    foreach(var point in new[]{bounds.min,bounds.max})
                    { var v=camera.WorldToViewportPoint(point);Assert.That(v.x,Is.InRange(0f,1f),device.name);Assert.That(v.y,Is.InRange(0f,1f),device.name); }
                }
                camera.targetTexture=null;rt.Release();Object.Destroy(rt);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator PresentationSwitchNeverChangesColliderOrInputState()
        {
            var qa=Object.FindFirstObjectByType<MoonGraphicsQaScene>();var p=DevPlayerController.Instance;
            var boxes=qa.Devices.Select(d=>d.GetComponent<BoxCollider2D>()).ToArray();var sizes=boxes.Select(b=>b.size).ToArray();var offsets=boxes.Select(b=>b.offset).ToArray();
            p.ApplySwipe(-701,1);var pace=p.Pace;qa.ApplyPresentation(false);qa.ApplyPresentation(true);
            Assert.That(p.Pace,Is.EqualTo(pace));Assert.That(Time.timeScale,Is.EqualTo(1));
            for(int i=0;i<boxes.Length;i++){Assert.That(boxes[i].size,Is.EqualTo(sizes[i]));Assert.That(boxes[i].offset,Is.EqualTo(offsets[i]));}
            foreach(var d in qa.Devices) foreach(var artCollider in d.GetComponent<AnimolOperationalArtBinding>().ArtRoot.GetComponentsInChildren<Collider2D>(true)) Assert.That(artCollider.enabled,Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator LanternAndStairPreserveOccupiedDeferralAndRecover()
        {
            // This is a controlled occupancy scenario, not the physical route test below.
            // Keep the real player out of the lantern's restore volume while injecting occupancy.
            var player=DevPlayerController.Instance;
            player.Body.position=new Vector2(-5f,4.62f);player.Body.linearVelocity=Vector2.zero;
            yield return new WaitForFixedUpdate();
            var lantern=Object.FindFirstObjectByType<MoonLanternStepObject>();
            lantern.NotifyOccupied(false);yield return new WaitForFixedUpdate();Assert.That(lantern.State,Is.EqualTo(LanternStepState.Warning));
            lantern.NotifyOccupied(true);yield return new WaitForFixedUpdate();Assert.That(lantern.State,Is.EqualTo(LanternStepState.Solid));
            lantern.NotifyOccupied(false);yield return new WaitForSeconds(lantern.Settings.WarningSeconds+.06f);Assert.That(lantern.GetComponent<Collider2D>().enabled,Is.False);
            yield return new WaitForSeconds(lantern.Settings.ActiveSeconds+lantern.Settings.RecoverSeconds+.12f);Assert.That(lantern.State,Is.EqualTo(LanternStepState.Solid));
            var stair=Object.FindFirstObjectByType<MoonPhaseStairObject>();stair.SetOccupiedForTest(true);stair.RequestToggle();
            Assert.That(stair.PreviewVisible,Is.True);Assert.That(stair.IsVertical,Is.False);
            stair.SetOccupiedForTest(false);yield return new WaitForFixedUpdate();Assert.That(stair.SweptSpaceBlocked,Is.False,stair.SweptSpaceBlockerName);Assert.That(stair.IsVertical,Is.True);
        }
        [UnityTest] public IEnumerator RabbitCanJumpFromFloorOntoFirstOneWayAndDropThrough()
        {
            var player=DevPlayerController.Instance;
            player.Body.position=new Vector2(-3.5f,1.62f);player.Body.linearVelocity=Vector2.zero;
            yield return new WaitForSeconds(.3f);Assert.That(player.IsGrounded,Is.True);
            player.RequestJump();yield return new WaitForSeconds(.65f);player.ReleaseJump();yield return new WaitForSeconds(.15f);
            Assert.That(player.LastGroundColliderName,Is.EqualTo("QA-OneWay-Lower"));
            Assert.That(player.RequestDropThroughOneWayPlatform(),Is.True);
            yield return new WaitForSeconds(.4f);Assert.That(player.Body.position.y,Is.LessThan(2.3f));
        }
        [UnityTest] public IEnumerator FirstOneWayConnectsToLanternWithUnmodifiedJumpPhysics()
        {
            var player=DevPlayerController.Instance;
            player.Body.position=new Vector2(-3.5f,1.62f);player.Body.linearVelocity=Vector2.zero;
            yield return new WaitForSeconds(.3f);
            player.RequestJump();yield return new WaitForSeconds(.8f);player.ReleaseJump();
            var lantern=Object.FindFirstObjectByType<MoonLanternStepObject>();
            float restoreDeadline=Time.time+5f;
            while(lantern.State!=LanternStepState.Solid && Time.time<restoreDeadline) yield return null;
            Assert.That(lantern.State,Is.EqualTo(LanternStepState.Solid),"Lantern must recover before the next jump.");
            player.RequestJump();player.ApplySwipe(-710,1);
            yield return new WaitForSeconds(.27f);player.ApplySwipe(-710,-1);
            yield return new WaitForSeconds(.55f);player.ReleaseJump();
            Assert.That(player.LastGroundColliderName,Is.EqualTo("QA-M1"));
        }
    }
}
