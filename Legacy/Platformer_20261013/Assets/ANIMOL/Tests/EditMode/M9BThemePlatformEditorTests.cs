using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ANIMOL.Core;
using ANIMOL.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class M9BThemePlatformEditorTests
    {
        private const string RegistryPath = "Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset";
        private const string PaletteMapPath = "Assets/ANIMOL/Data/Development/M9BThemePlatforms/DEV-M9B-PALETTE-45.asset";

        [Test]
        public void Catalog_HasExact45StableIdsAndExplicitImplementationLevels()
        {
            var registry = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(registry.IsValid(out var error), Is.True, error);
            Assert.That(registry.Types.Count, Is.EqualTo(45));
            Assert.That(registry.Types.Select(item => item.StableTypeId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(45));
            foreach (var id in new[] { "OBJ_SIDE_SPRING", "OBJ_POUNDER", "OBJ_RICE_SLOW", "TILE_HALF_BLOCK", "TILE_DROP_PLATFORM", "OBJ_RAIL_PLATFORM",
                         "MOON_JADE_BALANCE", "CLOUD_BALLOON_TETHER", "LIB_INDEX_DRAWER", "GREEN_SAND_RETRACE", "MINE_MAGNET_PAIR" })
                Assert.That(registry.Find(id), Is.Not.Null, id);
            Assert.That(registry.Types.Count(item => item.ImplementationLevel == MapObjectImplementationLevel.DevPlayable), Is.EqualTo(28));
            Assert.That(registry.Types.Count(item => item.ImplementationLevel == MapObjectImplementationLevel.PlaceablePrototype), Is.EqualTo(17));
            Assert.That(registry.Types.All(item => item.Prefab != null && item.GhostSprite != null && item.Icon != null), Is.True);
        }

        [Test]
        public void Themes_AreFixedFiveByTwenty_AndHiddenForestIsNotT06()
        {
            var expected = new[]
            {
                ("T01", "월궁", "Moon Palace"), ("T02", "구름고래 목장", "Cloud Whale Ranch"),
                ("T03", "별가루 도서관", "Stardust Library"), ("T04", "시간유리 온실", "Hourglass Conservatory"),
                ("T05", "오로라 수정광산", "Aurora Crystal Mine")
            };
            foreach (var entry in expected)
            {
                var theme = AssetDatabase.LoadAssetAtPath<ThemeDefinition>($"Assets/ANIMOL/Data/Campaign/Themes/{entry.Item1}.asset");
                Assert.That(theme, Is.Not.Null); Assert.That(theme.ThemeId, Is.EqualTo(entry.Item1));
                Assert.That(theme.DisplayName, Is.EqualTo(entry.Item2)); Assert.That(theme.EnglishDisplayName, Is.EqualTo(entry.Item3));
                Assert.That(theme.StageSlotCount, Is.EqualTo(20)); Assert.That(theme.Stages.Count, Is.EqualTo(20));
            }
            Assert.That(AssetDatabase.LoadAssetAtPath<ThemeDefinition>("Assets/ANIMOL/Data/Campaign/Themes/T06.asset"), Is.Null);
        }

        [Test]
        public void PaletteMap_SaveReload_Preserves45TypesSettingsPathsLinksAndPrototypeReadyBlock()
        {
            AssetDatabase.ImportAsset(PaletteMapPath, ImportAssetOptions.ForceUpdate);
            var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(PaletteMapPath);
            Assert.That(StageMapValidator.ValidateStructure(map).IsValid, Is.True, string.Join(" | ", StageMapValidator.ValidateStructure(map).Errors));
            Assert.That(map.Objects.Select(item => item.DataKey).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(45));
            Assert.That(map.Objects.Count, Is.EqualTo(47));
            Assert.That(map.Objects.All(item => item.Settings.Version is >= 2 and <= 3 && item.Prefab != null), Is.True);
            foreach (var pairId in new[] { "MOON_JADE_BALANCE", "MINE_MAGNET_PAIR" })
            {
                var pair = map.Objects.Where(item => item.DataKey == pairId).ToArray();
                Assert.That(pair.Length, Is.EqualTo(2));
                Assert.That(pair[0].Settings.LinkedInstanceIds.Single(), Is.EqualTo(pair[1].StableId));
                Assert.That(pair[1].Settings.LinkedInstanceIds.Single(), Is.EqualTo(pair[0].StableId));
            }
            Assert.That(StageMapValidator.ValidateForOperation(map).Errors.Any(item => item.Contains("PlaceablePrototype", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void Authoring_DuplicateEditReloadDeleteRoundTrip_UsesUndoAutosaveAndStableTypeId()
        {
            const string tempPath = "Assets/ANIMOL/Data/Development/M9BThemePlatforms/DEV-M9B-AUTHORING-TEST.asset";
            AssetDatabase.DeleteAsset(tempPath);
            Assert.That(AssetDatabase.CopyAsset(PaletteMapPath, tempPath), Is.True);
            try
            {
                var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(tempPath);
                var source = map.Objects.Single(item => item.DataKey == "MOON_LANTERN_STEP");
                Assert.That(StageMapObjectAuthoringOperations.Duplicate(map, source.StableId, new Vector2Int(0, 2), out var duplicateId, out var validation), Is.True,
                    string.Join(" | ", validation.Errors));
                AssetDatabase.ImportAsset(tempPath, ImportAssetOptions.ForceUpdate);
                map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(tempPath);
                var duplicate = map.Objects.Single(item => item.StableId == duplicateId);
                Assert.That(duplicate.DataKey, Is.EqualTo("MOON_LANTERN_STEP"));
                var oldDirection = duplicate.Settings.Direction;
                Assert.That(StageMapObjectAuthoringOperations.Flip(map, duplicateId), Is.True);
                AssetDatabase.ImportAsset(tempPath, ImportAssetOptions.ForceUpdate);
                map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(tempPath);
                Assert.That(map.Objects.Single(item => item.StableId == duplicateId).Settings.Direction, Is.Not.EqualTo(oldDirection));
                Assert.That(StageMapObjectAuthoringOperations.Remove(map, duplicateId), Is.True);
                AssetDatabase.ImportAsset(tempPath, ImportAssetOptions.ForceUpdate);
                Assert.That(AssetDatabase.LoadAssetAtPath<StageMapDefinition>(tempPath).Objects.Any(item => item.StableId == duplicateId), Is.False);
            }
            finally { AssetDatabase.DeleteAsset(tempPath); }
        }

        [Test]
        public void DevSceneIsExcludedFromRelease_AndCampaignStableIdentityRemainsProtected()
        {
            CollectionAssert.DoesNotContain(DevelopmentBuildScenePolicy.GetPlayerScenePaths(false), DevelopmentBuildScenePolicy.ThemePlatformLabPath);
            CollectionAssert.Contains(DevelopmentBuildScenePolicy.GetPlayerScenePaths(true), DevelopmentBuildScenePolicy.ThemePlatformLabPath);
            var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(T01S01ManualMapWorkflow.MapPath);
            Assert.That((map.StageId, map.ThemeId, map.MainTimeLimitSeconds, map.EscapeTimeLimitSeconds),
                Is.EqualTo(("T01-S01", "T01", 60f, 30f)));
            Assert.That(Sha("Assets/ANIMOL/Data/Campaign/Stages/T01-S01.asset"), Is.EqualTo("fb5891f13793608126922bef5171eca388032a255ebc77a482847cb63f87598e"));
            Assert.That(Sha("Assets/ANIMOL/Data/Campaign/Rewards/T01-S01.asset"), Is.EqualTo("f713e78480a6c7b99f4d7820f56d026a2815ae32a1cc24693f036e695d3f9c9b"));
        }

        private static string Sha(string path)
        {
            using var sha = SHA256.Create(); using var stream = File.OpenRead(path);
            return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
        }
    }
}
