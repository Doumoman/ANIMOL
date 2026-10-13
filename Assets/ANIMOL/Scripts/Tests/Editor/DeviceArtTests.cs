using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using ANIMOL.Presentation;
using ANIMOL.Presentation.Editor;
namespace ANIMOL.Presentation.Tests
{
    public class DeviceArtTests
    {
        [Test] public void AllImportsAndExactClipTimesAreValid()=>Assert.DoesNotThrow(()=>DeviceArtBuilder.Validate());
        static DeviceVisualView View(string id)
        {
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Devices/"+id+".prefab"));var view=go.GetComponent<DeviceVisualView>();view.ResetVisual();return view;
        }
        [Test] public void GateOpenHoldsUntilExplicitClearAndPausePreservesPose()
        {
            var v=View("d09_one_way_gate");try
            {
                var position=v.transform.position;Assert.True(v.Notify("allowed_passage_begin","open_e"));v.Advance(.2f);var open=v.visual.sprite;
                v.Advance(20);Assert.AreEqual(open,v.visual.sprite);Assert.AreEqual("open_e",v.CurrentState);
                v.Notify("allowed_passage_clear","close_e");v.Advance(.05f);var before=v.Elapsed;v.paused=true;v.Advance(1);Assert.AreEqual(before,v.Elapsed);
                v.paused=false;v.Advance(1);Assert.AreEqual(position,v.transform.position);Assert.AreEqual(v.states[0].sprite,v.visual.sprite);Assert.False(v.BlocksInput);
                v.ResetVisual();Assert.AreEqual(v.initialState,v.CurrentState);Assert.AreEqual(0,v.Elapsed);
            }finally{Object.DestroyImmediate(v.gameObject);}
        }
        [Test] public void DirectionMappingAndRejectedEventsDoNotChangePresentation()
        {
            Assert.AreEqual(Vector2Int.up,DeviceVisualView.Direction("n"));Assert.AreEqual(Vector2Int.down,DeviceVisualView.Direction("s"));Assert.AreEqual(Vector2Int.right,DeviceVisualView.Direction("e"));Assert.AreEqual(Vector2Int.left,DeviceVisualView.Direction("w"));
            var v=View("d01_star_socket");try{Assert.False(v.Notify("uncommitted_push","stone_push_e"));Assert.AreEqual("stone",v.CurrentState);Assert.True(v.Notify("matching_star_socket_committed","socket_insert"));v.Advance(1);var terminal=v.visual.sprite;v.Advance(100);Assert.AreEqual(terminal,v.visual.sprite);Assert.AreEqual(0,v.GetComponentsInChildren<Collider2D>().Length);}finally{Object.DestroyImmediate(v.gameObject);}
        }
        [Test] public void BridgeStateIsHeldAndLoopSelectionIsIdempotent()
        {
            var v=View("d02_pressure_vine_bridge");try{v.Notify("existing_bridge_enabled","bridge_extend_n");v.Advance(1);var pose=v.visual.sprite;v.Advance(20);Assert.AreEqual(pose,v.visual.sprite);v.Present("bridge_idle_vertical");v.Advance(.5f);v.Present("bridge_idle_vertical");Assert.AreEqual(.5f,v.Elapsed);v.ResetVisual();Assert.AreEqual("plate_idle",v.CurrentState);}finally{Object.DestroyImmediate(v.gameObject);}
        }
    }
}
