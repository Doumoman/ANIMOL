#if UNITY_EDITOR
using System;
using System.Linq;
using ANIMOL.Art.EchoTheaterV03;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Tests
{
    public sealed class EchoTheaterV03ProjectBindingTests
    {
        const string Root = EchoTheaterV03ProjectBuilder.Root;
        const string Environment = "Assets/ANIMOL/Data/Art/MemorySkyTheater/Environment/EnvironmentArt.asset";
        const string Prefabs = "Assets/ANIMOL/Prefabs/Themes/memory_theater/";

        [Test] public void GeneratedCatalogAndExistingEnvironmentUseVerifiedV03Assets()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EchoTheaterV03VisualCatalog>(
                Root + "/_Generated/EchoTheaterV03VisualCatalog.asset");
            Assert.DoesNotThrow(() => EchoTheaterV03AnimationBuilder.ValidateBuiltCatalog(catalog));
            Assert.That(catalog.SpriteCount, Is.EqualTo(187));
            Assert.That(catalog.ClipCount, Is.EqualTo(190));
            var environment = AssetDatabase.LoadAssetAtPath<EchoTheaterV02Art>(Environment);
            Assert.That(environment.sprites.Length, Is.EqualTo(1969));
            Assert.That(environment.v03Animations, Is.SameAs(catalog));
            Assert.That(environment.v03ManifestSha256, Is.EqualTo(catalog.SourceManifestSha256));
            Assert.That(environment.v03DiceOrientationMap, Is.Not.Null);
            foreach (var row in environment.sprites)
                Assert.That(AssetDatabase.GetAssetPath(row.sprite), Is.EqualTo(Root + "/" + row.path));
        }

        [Test] public void CoreTheaterDeviceStatesUseExactGeneratedClipsAndNoVisualScaling()
        {
            var checks = new[] {
                ("d04_stage_shutter", "a_horizontal_open", "id04_shutter_horizontal_open"),
                ("d06_moving_stage", "advance_n", "id06_stage_N_move_wide"),
                ("d07_conveyor", "run_w", "conveyor_west_transport"),
                ("d08_rotating_bookshelf", "rotate_0_cw", "id08_bookcase_H_to_V"),
                ("d09_one_way_gate", "open_s", "oneway_gate_south_opening"),
                ("d13_domino_wall", "fold_n", "domino_wall.fall.north"),
                ("d17_charging_statue", "warning_enter_w", "charging_statue_W_warning"),
                ("d18_bounce_corner", "impact_sw", "bounce_corner_sw_rebound"),
                ("d19_brake_pad", "brake_vertical", "brake_pad_vertical_press_release"),
                ("d22_dice_block", "roll_t1_n5_e3_e", "dice_block.roll.o00.east"),
                ("d23_coward_doll", "retreat_n", "timid_puppet_N_retreat")
            };
            foreach (var check in checks)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + check.Item1 + ".prefab");
                Assert.That(prefab, Is.Not.Null, check.Item1);
                var view = prefab.GetComponent<DeviceVisualView>();
                Assert.That(view, Is.Not.Null, check.Item1);
                var state = Array.Find(view.states, s => s.name == check.Item2);
                Assert.That(state, Is.Not.Null, check.Item1 + "/" + check.Item2);
                Assert.That(state.v03ClipKey, Is.EqualTo(check.Item3));
                Assert.That(state.sequence, Is.Not.Null);
                Assert.That(state.sequence.frames.All(s => AssetDatabase.GetAssetPath(s).StartsWith(Root + "/", StringComparison.Ordinal)), Is.True);
                Assert.That(AssetDatabase.GetAssetPath(state.clip), Does.StartWith(Root + "/_Generated/"));
                Assert.That(view.visual.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(view.visual.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(prefab.GetComponentsInChildren<Animator>(true), Is.Empty);
            }
        }

        [Test] public void MissingLatestDirectionIsRejectedWithoutLegacyFallback()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EchoTheaterV03VisualCatalog>(
                Root + "/_Generated/EchoTheaterV03VisualCatalog.asset");
            EchoTheaterV03VisualCatalog.Clip clip;
            string error;
            Assert.That(catalog.TryGetDirectionalClip("conveyor", "diagonal", "transport",
                catalog.SourceManifestSha256, out clip, out error), Is.False);
            Assert.That(clip, Is.Null);
            StringAssert.Contains("Missing latest directional clip", error);
        }
    }
}
#endif
