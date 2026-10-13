using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using ANIMOL.Presentation.Editor;
namespace ANIMOL.Presentation.Tests
{
    public class LetterObservatoryArtTests
    {
        [Test] public void AllImportsTimingsAndSingleWritersAreValid()=>Assert.DoesNotThrow(()=>LetterObservatoryArtBuilder.Validate());
        [Test] public void EightReflectionMappingsUseTravellingDirectionAndFullSixteenCornerArms()
        {
            var rules=AssetDatabase.LoadAssetAtPath<OpticsPresentationRules>(LetterObservatoryArtBuilder.RulesPath);Assert.AreEqual(8,rules.reflections.Length);
            foreach(var row in new[]{("slash","e","n"),("slash","n","e"),("slash","w","s"),("slash","s","w"),("backslash","e","s"),("backslash","s","e"),("backslash","w","n"),("backslash","n","w")})
            {var rule=rules.Find(row.Item1,row.Item2);Assert.AreEqual(row.Item3,rule.outgoing);Assert.NotNull(rule.corner);Assert.NotNull(rule.corner.texture);}
            Assert.IsNull(rules.Find("unknown","e"));Assert.NotNull(rules.opticsMetadata);Assert.NotNull(rules.motionMetadata);
        }
        [Test] public void LeapStartingPivotsRetainThreeCellSpanInsteadOfCanvasWidth()
        {
            var m=LetterObservatoryArtBuilder.Read();foreach(var row in m.assets.Where(a=>a.id.Contains("trajectory_")))
            {
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/"+row.path);string d=row.id.Split('_').Last();
                var expected=d=="e"?new Vector2(8,48):d=="w"?new Vector2(248,48):d=="s"?new Vector2(48,248):new Vector2(48,8);Assert.AreEqual(expected,sprite.pivot);
                Assert.AreEqual(3,240/sprite.pixelsPerUnit);Assert.AreNotEqual(new Vector2(row.width*.5f,row.height*.5f),sprite.pivot);
            }
        }
        [Test] public void ReplacingCommittedBeamCellsReusesPoolAndClearsResidualSegments()
        {
            var go=new GameObject("beam");try
            {
                var view=go.AddComponent<BeamPathView>();var m=LetterObservatoryArtBuilder.Read();var row=m.assets.First(a=>a.id=="d10_optics/beam_horizontal");view.sprites=new[]{new BeamVisualSprite{shape="h",sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/"+row.path)}};
                view.Present(new[]{new BeamVisualCell{shape="h",position=Vector2.zero},new BeamVisualCell{shape="h",position=Vector2.right},new BeamVisualCell{shape="h",position=Vector2.right*2}});Assert.AreEqual(3,view.ActiveCount);
                view.Present(new[]{new BeamVisualCell{shape="h",position=Vector2.zero}});Assert.AreEqual(1,view.ActiveCount);Assert.AreEqual(3,go.GetComponentsInChildren<SpriteRenderer>().Length);Assert.AreEqual(1,go.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.enabled));
                view.Clear();Assert.AreEqual(0,go.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.enabled));Assert.AreEqual(Vector3.zero,go.transform.position);Assert.AreEqual(0,go.GetComponentsInChildren<Collider2D>().Length);
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void CommonThemesReuseVariantsAndOneActionDoesNotReplayPortal()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>(LetterObservatoryArtBuilder.CatalogPath);Assert.AreEqual(9,catalog.devices.Length);
            foreach(var d in new[]{"07","09","18","19"})Assert.AreEqual(PrefabAssetType.Variant,PrefabUtility.GetPrefabAssetType(catalog.devices.First(e=>e.deviceId==d).prefab));
            var go=UnityEngine.Object.Instantiate(catalog.devices.First(e=>e.deviceId=="16").prefab);try
            {
                var v=go.GetComponent<DeviceVisualView>();v.ResetVisual();var state=v.states.First(s=>s.name=="a_enter_e");Assert.True(v.Notify("turn_1",state.eventName,state.name));v.Advance(.1f);Assert.False(v.Notify("turn_1",state.eventName,state.name));Assert.AreEqual(.1f,v.Elapsed);Assert.AreEqual(Vector3.zero,go.transform.position);Assert.False(v.BlocksInput);v.ResetVisual();Assert.True(v.Notify("turn_1",state.eventName,state.name));
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
