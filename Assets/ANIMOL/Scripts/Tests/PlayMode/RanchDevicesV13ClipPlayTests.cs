using System.Collections;
using System.Collections.Generic;
using ANIMOL.Art.RanchDevicesV13;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ANIMOL.Presentation.Tests
{
    public sealed class RanchDevicesV13ClipPlayTests
    {
        [UnityTest]
        public IEnumerator SuppliedDirectionalAndStateClipsAdvanceOnAnInspectionRenderer()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RanchDevicesV13Catalog>(
                "Assets/ANIMOL/Art/ANIMOL_STARWAY_RANCH_DEVICES_V13/_GeneratedV13/RanchDevicesV13Catalog.asset");
            Assert.That(catalog, Is.Not.Null);
            var keys = new List<string>();
            foreach (var direction in new[] { "south", "east", "north", "west" })
            {
                keys.Add("sheep/" + direction + "/walk");
                keys.Add("sheep/" + direction + "/eat");
                keys.Add("sheep/" + direction + "/eat_and_disappear");
                keys.Add("chick/" + direction + "/walk");
                keys.Add("chick/" + direction + "/sit");
            }
            keys.AddRange(new[] { "nest/arrive", "nest/settled", "pond_pair/a_dry_b_wet",
                "pond_pair/cross_a_to_b", "pond_pair/a_wet_b_dry", "pond_pair/cross_b_to_a",
                "pond_cell/dry", "pond_cell/wet", "pond_cell/fill", "pond_cell/drain",
                "tall_grass/idle", "tall_grass/eaten",
                "windmill/east/off", "windmill/east/3",
                "windmill/east/6", "windmill/east/9" });
            var go = new GameObject("V13 clip playback check");
            var renderer = go.AddComponent<SpriteRenderer>();
            var visual = go.AddComponent<RanchDeviceVisualV13>();
            Time.timeScale = 1f;
            try
            {
                Assert.That(visual.Configure(catalog, out var error), Is.True, error);
                foreach (var key in keys)
                {
                    Assert.That(catalog.TryClip(key, out var clip), Is.True, key);
                    Assert.That(visual.SetState(key, true), Is.True, key);
                    Assert.That(renderer.sprite, Is.SameAs(clip.frames[0]), key);
                    if (key.StartsWith("pond_pair/"))
                    {
                        Assert.That(renderer.sprite.texture.width, Is.EqualTo(576));
                        Assert.That(renderer.sprite.texture.height, Is.EqualTo(320));
                        Assert.That(go.transform.localScale, Is.EqualTo(Vector3.one));
                    }
                    yield return new WaitForSeconds(1f / clip.fps + .025f);
                    bool advanced = renderer.sprite != clip.frames[0];
                    Assert.That(advanced, Is.EqualTo(clip.frames.Length > 1),
                        key + " play=" + UnityEditor.EditorApplication.isPlaying + " delta=" + Time.deltaTime);
                }
            }
            finally { Object.Destroy(go); Time.timeScale = 1f; }
        }
    }
}
