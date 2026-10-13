using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using ANIMOL.Presentation.Editor;
namespace ANIMOL.Presentation.Tests
{
    public class TwilightSeaArtTests
    {
        [Test] public void AllImportsTimingsAndThemeReferencesMatchAuthoredManifest(){StringAssert.Contains("712 PNG",TwilightSeaArtBuilder.Validate());}
        [Test] public void FerryUsesOneClockAndIdenticalLayerTimesForAllTwentyPairs()
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(TwilightSeaArtBuilder.FerryPath));
            try
            {
                var f=go.GetComponent<FerryVisualView>();Assert.AreEqual(20,f.pairs.Length);Assert.IsNull(f.clock.visual);Assert.AreEqual(4,go.GetComponentsInChildren<SpriteRenderer>().Length);f.ResetVisual();Vector3 root=go.transform.position;
                foreach(var pair in f.pairs)
                {
                    CollectionAssert.AreEqual(pair.rear.durations,pair.front.durations);Assert.AreEqual(pair.rear.frames.Length,pair.front.frames.Length);
                    float elapsed=0;
                    for(int i=0;i<pair.rear.frames.Length;i++)
                    {
                        f.Present(pair.state,true);f.Advance(elapsed+.001f);Assert.AreEqual(i,f.FrameIndex,pair.state);Assert.AreSame(pair.rear.frames[i],f.rear.sprite);Assert.AreSame(pair.front.frames[i],f.front.sprite);
                        var offset=pair.passengerOffsets!=null&&i<pair.passengerOffsets.Length?pair.passengerOffsets[i]:Vector2.zero;
                        Assert.Less(Vector2.Distance(f.passenger.transform.localPosition,new Vector2(offset.x,-5-offset.y)/80),.0001f);elapsed+=pair.rear.durations[i];
                    }
                    Assert.AreEqual(root,go.transform.position);Assert.Less(f.wake.sortingOrder,f.rear.sortingOrder);Assert.Less(f.rear.sortingOrder,f.passenger.sortingOrder);Assert.Less(f.passenger.sortingOrder,f.front.sortingOrder);
                }
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void BombAndSpikesHoldTerminalSpritesWithoutWallClockRuleChanges()
        {
            foreach(var fixture in new[]{new[]{"d15_three_turn_bomb","arm","count_3"},new[]{"d15_three_turn_bomb","count_3_to_2","count_2"},new[]{"d15_three_turn_bomb","count_2_to_1","count_1"},new[]{"d15_three_turn_bomb","explode","cleared"},new[]{"d05_departure_spikes","rise","raised"}})
            {
                var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(TwilightSeaArtBuilder.Prefab(fixture[0])));
                try{var v=go.GetComponent<DeviceVisualView>();v.ResetVisual();var s=v.states.First(a=>a.name==fixture[1]);Assert.True(v.Notify("action-1",s.eventName,s.name));Assert.False(v.Notify("action-1",s.eventName,s.name));v.Advance(100);Assert.AreSame(s.terminal,v.visual.sprite);Assert.AreEqual(s.name,v.CurrentState);Assert.False(v.BlocksInput);Assert.AreEqual(Vector3.zero,v.transform.position);}
                finally{UnityEngine.Object.DestroyImmediate(go);}
            }
        }
        [Test] public void BoundariesUseSuppliedCellsAndNeverInferExplosionRadius()
        {
            var cells=new HashSet<Vector2Int>{Vector2Int.zero,Vector2Int.up,Vector2Int.right};Assert.AreEqual(12,ResolvedCellPreview.ExteriorMask(Vector2Int.zero,cells));Assert.AreEqual(11,ResolvedCellPreview.ExteriorMask(Vector2Int.up,cells));Assert.AreEqual(7,ResolvedCellPreview.ExteriorMask(Vector2Int.right,cells));Assert.AreEqual(3,ResolvedCellPreview.ShoreMask(Vector2Int.zero,cells));
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(TwilightSeaArtBuilder.PreviewPath));try{var p=go.GetComponent<ResolvedCellPreview>();p.PresentBlast(cells,new[]{Vector2Int.up,new Vector2Int(10,10)});Assert.AreEqual(3,p.boundary.ActiveCount);Assert.AreEqual(1,p.targets.ActiveCount);p.PresentStop(new Vector2Int(6,2),"s");Assert.AreEqual(new Vector3(6,2,0),p.endpoint.GetComponentInChildren<SpriteRenderer>().transform.localPosition);p.PresentStop(null,"s");Assert.AreEqual(0,p.endpoint.ActiveCount);p.Clear();Assert.AreEqual(0,p.boundary.ActiveCount);}finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void CosmeticThemeSwitchPreservesElapsedPauseAndConsumedActionId()
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Devices/d09_one_way_gate.prefab"));try{var v=go.GetComponent<DeviceVisualView>();v.ResetVisual();var s=v.states.First(a=>a.name=="open_e");Assert.True(v.Notify("one",s.eventName,s.name));v.Advance(.04f);v.paused=true;foreach(var theme in new[]{"twilight_sea","letter_observatory","memory_theater"}){Assert.True(AssetDatabase.LoadAssetAtPath<DeviceThemeCatalog>("Assets/ANIMOL/Data/Themes/"+theme+".asset").Apply(v,"09"));Assert.AreEqual(.04f,v.Elapsed);Assert.True(v.paused);Assert.False(v.Notify("one",s.eventName,s.name));}}finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
