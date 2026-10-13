using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using ANIMOL.Presentation.Editor;
namespace ANIMOL.Presentation.Tests
{
    public class MemoryTheaterArtTests
    {
        static DeviceVisualView View(string id)
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MemoryTheaterArtBuilder.Prefab(id)));var v=go.GetComponent<DeviceVisualView>();v.ResetVisual();return v;
        }
        [Test] public void AllImportsTimingsAndSingleWritersAreValid()=>Assert.DoesNotThrow(()=>MemoryTheaterArtBuilder.Validate());
        [Test] public void AllSixFacesAndNinetySixRenderTransitionsAreConsistent()
        {
            var table=AssetDatabase.LoadAssetAtPath<DiceOrientationTable>(MemoryTheaterArtBuilder.DicePath);Assert.AreEqual(24,table.orientations.Length);
            var view=View("d22_dice_block");try
            {
                int count=0;
                foreach(var o in table.orientations)
                {
                    var f=o.faces;Assert.AreEqual(7,f.top+f.bottom);Assert.AreEqual(7,f.north+f.south);Assert.AreEqual(7,f.east+f.west);Assert.AreEqual(6,new[]{f.top,f.bottom,f.north,f.south,f.east,f.west}.Distinct().Count());
                    Assert.AreSame(o,table.FindFaces(f.top,f.bottom,f.north,f.south,f.east,f.west));Assert.AreEqual(4,o.transitions.Length);
                    foreach(var t in o.transitions)
                    {
                        var next=table.Find(t.toOrientation);Assert.NotNull(next);var n=next.faces;
                        int[] expected;
                        switch(t.direction){case "e":expected=new[]{f.west,f.east,f.north,f.south,f.top,f.bottom};break;case "w":expected=new[]{f.east,f.west,f.north,f.south,f.bottom,f.top};break;case "n":expected=new[]{f.south,f.north,f.top,f.bottom,f.east,f.west};break;default:expected=new[]{f.north,f.south,f.bottom,f.top,f.east,f.west};break;}
                        CollectionAssert.AreEqual(expected,new[]{n.top,n.bottom,n.north,n.south,n.east,n.west});Assert.AreEqual(t.top,n.top);
                        string opposite=t.direction=="e"?"w":t.direction=="w"?"e":t.direction=="n"?"s":"n";
                        Assert.AreEqual(o.id,next.transitions.First(a=>a.direction==opposite).toOrientation);
                        string four=o.id;for(int i=0;i<4;i++)four=table.Find(four).transitions.First(a=>a.direction==t.direction).toOrientation;Assert.AreEqual(o.id,four);
                        Assert.True(table.PresentCommittedRoll(view,"render_"+count++,o.id,t.toOrientation,t.direction));view.Advance(.25f);
                        Assert.AreEqual(view.states.First(s=>s.name==t.toOrientation).sprite,view.visual.sprite);
                    }
                }
                Assert.AreEqual(96,count);var initial=table.Find(table.initialOrientation);
                foreach(var pair in new[]{("e",4),("w",3),("n",2),("s",5)})Assert.AreEqual(pair.Item2,initial.transitions.First(t=>t.direction==pair.Item1).top);
                Assert.False(table.PresentCommittedRoll(view,"invalid",table.initialOrientation,table.initialOrientation,"e"));
            }finally{UnityEngine.Object.DestroyImmediate(view.gameObject);}
        }
        [Test] public void CommonVariantsAndThemeSwapReuseOneWriter()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(MemoryTheaterArtBuilder.CatalogPath);
            foreach(var id in new[]{"07","09","18","19"})Assert.AreEqual(PrefabAssetType.Variant,PrefabUtility.GetPrefabAssetType(catalog.devices.First(e=>e.deviceId==id).prefab));
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Devices/d07_conveyor.prefab"));try
            {
                var v=go.GetComponent<DeviceVisualView>();v.ResetVisual();Assert.True(v.Notify("action_1","conveyor_visual_running","run_e"));v.Advance(.12f);go.transform.position=new Vector3(3,4,0);v.paused=true;
                for(int i=0;i<3;i++){Assert.True(catalog.Apply(v,"07"));Assert.AreEqual("run_e",v.CurrentState);Assert.True(v.paused);Assert.AreEqual(new Vector3(3,4,0),go.transform.position);Assert.AreEqual(1,go.GetComponents<DeviceVisualView>().Length);Assert.AreEqual(1,go.GetComponentsInChildren<SpriteRenderer>().Length);}
                Assert.AreEqual(.12f,v.Elapsed);Assert.False(v.Notify("action_1","conveyor_visual_running","run_e"));Assert.False(catalog.Apply(v,"09"));
                Assert.False(catalog.Apply(v,"missing"));
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void ActionDeduplicationResetAndWarningDoNotAdvanceGameplay()
        {
            var v=View("d17_charging_statue");try
            {
                Assert.True(v.Notify("action_1","statue_line_locked","warning_enter_e"));v.Advance(.07f);
                Assert.False(v.Notify("action_1","statue_line_locked","warning_enter_e"));Assert.AreEqual(.07f,v.Elapsed);
                Assert.True(v.Notify("action_1","wait_for_next_valid_action","warning_hold_e"));v.Advance(100);Assert.AreEqual("warning_hold_e",v.CurrentState);Assert.AreEqual(Vector3.zero,v.transform.position);
                Assert.False(v.Notify("action_2","invalid_input","charge_enter_e"));Assert.AreEqual("warning_hold_e",v.CurrentState);Assert.False(v.BlocksInput);
                v.ResetVisual();Assert.True(v.Notify("action_1","statue_line_locked","warning_enter_e"));
            }finally{UnityEngine.Object.DestroyImmediate(v.gameObject);}
        }
        [Test] public void ShutterDominoBookshelfAndTransparentBreakHoldTheirCommittedTerminal()
        {
            foreach(var row in new[]{("d04_stage_shutter","occupied_shutter_remains_open","a_horizontal_blocked_open","a_horizontal_open"),("d13_domino_wall","this_domino_piece_fold_committed","fold_e","folded_e"),("d08_rotating_bookshelf","bookshelf_rotation_committed","rotate_0_cw","vertical")})
            {
                var v=View(row.Item1);try{var position=v.transform.position;Assert.True(v.Notify("action",row.Item2,row.Item3));v.Advance(2);var source=MemoryTheaterArtBuilder.Read().assets.First(a=>a.id==row.Item1+"/"+row.Item4);var expected=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/"+source.path);Assert.AreEqual(expected,v.visual.sprite);v.Advance(100);Assert.AreEqual(expected,v.visual.sprite);Assert.AreEqual(position,v.transform.position);}finally{UnityEngine.Object.DestroyImmediate(v.gameObject);}
            }
            var wall=View("foundation_cracked_wall_break");try{wall.Advance(.3f);Assert.NotNull(wall.visual.sprite);Assert.AreEqual("cracked_wall_break",wall.CurrentState);wall.ResetVisual();Assert.AreEqual(0,wall.Elapsed);}finally{UnityEngine.Object.DestroyImmediate(wall.gameObject);}
        }
    }
}
