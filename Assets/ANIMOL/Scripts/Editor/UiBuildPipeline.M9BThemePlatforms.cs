using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Development;
using ANIMOL.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    public static partial class UiBuildPipeline
    {
        private const string M9BPrefix = "animol-m9b-theme-platform-";
        private const string M9BAuditPath = "Logs/animol-m9b-theme-platform-audit.txt";
        private const string M9BRoot = "Assets/ANIMOL/Data/Development/M9BThemePlatforms";
        private const string M9BTypeRoot = M9BRoot + "/Types";
        private const string M9BArtRoot = "Assets/ANIMOL/Art/MapObjects/M9B";
        private const string M9BPrefabRoot = "Assets/ANIMOL/Prefabs/MapObjects/M9B";
        private const string M9BRegistryPath = "Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset";
        private const string M9BPaletteMapPath = M9BRoot + "/DEV-M9B-PALETTE-45.asset";
        private const string M9BLabMapPath = M9BRoot + "/DEV-M9B-REPRESENTATIVE-LAB.asset";

        private sealed class M9BObjectSpec
        {
            public readonly string Id, Name, English, ThemeId, Behavior;
            public readonly StageMapObjectKind Kind;
            public readonly Vector2 Footprint;
            public readonly MapObjectImplementationLevel Level;
            public readonly bool Pair;
            public M9BObjectSpec(string id, string name, string english, string themeId, StageMapObjectKind kind,
                Vector2 footprint, MapObjectImplementationLevel level, string behavior, bool pair = false)
            { Id = id; Name = name; English = english; ThemeId = themeId; Kind = kind; Footprint = footprint; Level = level; Behavior = behavior; Pair = pair; }
        }

        [MenuItem("ANIMOL/M9B Theme Platforms/0 Audit (No Asset Mutation)")]
        public static void AuditM9BThemePlatforms()
        {
            if (!File.Exists("Docs/ANIMOL_M9_PORTRAIT_RESULT.md") || !File.Exists("Docs/ANIMOL_M9_PORTRAIT_OBJECTS_RESULT.md"))
                throw new InvalidOperationException("M9 portrait/object completion reports are missing.");
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignCatalogGenerator.CatalogPath);
            var stages = catalog == null ? Array.Empty<CampaignStageDefinition>() : catalog.EnumerateStages().ToArray();
            var lines = new List<string>
            {
                "ANIMOL M9B 5-theme/platform preflight", "mutation=false", "sourcePatchDocument=present",
                "sourceFantasyDesignDocument=BLOCKED_NOT_FOUND", "m9PortraitPrerequisite=PASS_FROM_RESULT",
                "m9CommonSixPrerequisite=PASS_FROM_RESULT", "operationalMapAutoPlacement=forbidden",
                $"stableStageIds={stages.Select(item => item.StageId).Distinct(StringComparer.Ordinal).Count()}/{stages.Length}",
                $"ready={stages.Count(item => ContentAvailabilityResolver.Resolve(item) == ContentAvailability.Ready)}",
                $"unassigned={stages.Count(item => ContentAvailabilityResolver.Resolve(item) == ContentAvailability.Unassigned)}",
                $"invalid={stages.Count(item => ContentAvailabilityResolver.Resolve(item) == ContentAvailability.Invalid)}",
                $"t01MapSha256={FileSha256(T01MapPath)}", $"t01StageSha256={FileSha256(T01StagePath)}",
                $"t01RewardSha256={FileSha256(T01RewardPath)}", "physicalDevice=BLOCKED"
            };
            foreach (var path in Directory.GetFiles("Assets/ANIMOL/Data/Campaign/Maps", "*.asset").OrderBy(value => value, StringComparer.Ordinal))
                lines.Add($"campaignMap.{Path.GetFileName(path)}={FileSha256(path.Replace('\\', '/'))}");
            WriteM9BLog("audit.txt", lines);
        }

        [MenuItem("ANIMOL/M9B Theme Platforms/1 Build Catalog and DEV Labs")]
        public static void ApplyM9BThemePlatforms()
        {
            var baseline = RequireM9BAudit();
            EnsureM9BFolder(M9BRoot); EnsureM9BFolder(M9BTypeRoot); EnsureM9BFolder(M9BArtRoot); EnsureM9BFolder(M9BPrefabRoot);
            BackupAndUpdateThemeMetadata();
            var specs = Specs();
            var definitions = BuildM9BDefinitions(specs);
            var registry = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>(M9BRegistryPath);
            if (registry == null) { registry = ScriptableObject.CreateInstance<StageMapObjectTypeRegistry>(); AssetDatabase.CreateAsset(registry, M9BRegistryPath); }
            registry.EditorConfigure(2, definitions);
            EditorUtility.SetDirty(registry);
            var paletteMap = BuildPaletteRoundTripMap(specs, definitions);
            var labMap = BuildRepresentativeLabMap(definitions);
            BuildRepresentativeLabScene(labMap, registry);
            EnsureM9BSceneInBuildSettings();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            AssertM9BProtectedMaps(baseline, "build");
            ValidateM9BThemePlatforms();
            Debug.Log("[ANIMOL][M9B] 5 themes, 45 authorable types, and representative DEV lab generated; campaign maps untouched.");
        }

        [MenuItem("ANIMOL/M9B Theme Platforms/2 Capture Portrait DEV Lab")]
        public static void CaptureM9BThemePlatformLab()
        {
            var scene = EditorSceneManager.OpenScene(DevelopmentBuildScenePolicy.ThemePlatformLabPath, OpenSceneMode.Single);
            var loader = UnityEngine.Object.FindFirstObjectByType<StageMapRuntimeLoader>();
            var camera = Camera.main;
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (!scene.IsValid() || loader == null || camera == null || canvas == null)
                throw new InvalidOperationException("M9B representative lab camera/canvas/loader is missing.");
            loader.Load(loader.Map);
            CapturePortraitFrame(camera, canvas, 1080, 1920, "Assets/ANIMOL/Screenshots/M9B_THEME_PLATFORM_LAB_1080x1920.png");
            CapturePortraitFrame(camera, canvas, 1080, 2400, "Assets/ANIMOL/Screenshots/M9B_THEME_PLATFORM_LAB_1080x2400.png");
            AssetDatabase.Refresh();
            WriteM9BLog("portrait-capture.txt", new[] { "capture1080x1920=created", "capture1080x2400=created", "horizontalVisibleCells=12", "extraTallVerticalCells=5.33" });
        }

        [MenuItem("ANIMOL/M9B Theme Platforms/3 Validate")]
        public static void ValidateM9BThemePlatforms()
        {
            var baseline = RequireM9BAudit();
            var errors = new List<string>();
            var expected = Specs();
            var registry = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>(M9BRegistryPath);
            var registryError = "Registry missing.";
            if (registry == null || !registry.IsValid(out registryError)) errors.Add(registryError ?? "Registry missing.");
            var actualIds = registry == null ? Array.Empty<string>() : registry.Types.Select(item => item.StableTypeId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var expectedIds = expected.Select(item => item.Id).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!actualIds.SequenceEqual(expectedIds)) errors.Add("The exact 45 stable type IDs do not match the M9B contract.");
            var themes = new Dictionary<string, (string ko, string en)>
            {
                ["T01"] = ("월궁", "Moon Palace"), ["T02"] = ("구름고래 목장", "Cloud Whale Ranch"),
                ["T03"] = ("별가루 도서관", "Stardust Library"), ["T04"] = ("시간유리 온실", "Hourglass Conservatory"),
                ["T05"] = ("오로라 수정광산", "Aurora Crystal Mine")
            };
            foreach (var pair in themes)
            {
                var theme = AssetDatabase.LoadAssetAtPath<ThemeDefinition>($"Assets/ANIMOL/Data/Campaign/Themes/{pair.Key}.asset");
                if (theme == null || theme.ThemeId != pair.Key || theme.DisplayName != pair.Value.ko || theme.EnglishDisplayName != pair.Value.en ||
                    theme.StageSlotCount != 20 || theme.Stages.Count != 20) errors.Add($"{pair.Key} metadata/20-slot contract mismatch.");
            }
            if (AssetDatabase.LoadAssetAtPath<ThemeDefinition>("Assets/ANIMOL/Data/Campaign/Themes/T06.asset") != null) errors.Add("T06 must not exist.");
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(M9BPaletteMapPath, ImportAssetOptions.ForceUpdate);
            var paletteMap = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(M9BPaletteMapPath);
            var placedTypeIds = paletteMap?.Objects.Select(item => item.DataKey).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
            if (!placedTypeIds.SequenceEqual(expectedIds)) errors.Add("45-type placement save/reload round-trip failed.");
            if (paletteMap == null || !StageMapValidator.ValidateStructure(paletteMap).IsValid) errors.Add("Palette map structure/link round-trip is invalid.");
            if (paletteMap == null || !StageMapValidator.ValidateForOperation(paletteMap).Errors.Any(error => error.Contains("PlaceablePrototype", StringComparison.Ordinal)))
                errors.Add("Prototype Ready blocker is missing.");
            var lab = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(M9BLabMapPath);
            if (lab == null || lab.Objects.Select(item => item.DataKey).Distinct(StringComparer.Ordinal).Count() != 9 || lab.Objects.Count != 11)
                errors.Add("Representative lab must persist nine types/eleven placements.");
            if (lab != null && !StageMapValidator.ValidateStructure(lab).IsValid) errors.Add("Representative lab structure/link validation failed.");
            var release = DevelopmentBuildScenePolicy.GetPlayerScenePaths(false);
            var development = DevelopmentBuildScenePolicy.GetPlayerScenePaths(true);
            if (release.Contains(DevelopmentBuildScenePolicy.ThemePlatformLabPath, StringComparer.Ordinal) ||
                !development.Contains(DevelopmentBuildScenePolicy.ThemePlatformLabPath, StringComparer.Ordinal)) errors.Add("DEV scene build exclusion mismatch.");
            AssertM9BProtectedMaps(baseline, "validation");
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignCatalogGenerator.CatalogPath);
            var stages = catalog.EnumerateStages().ToArray();
            if (stages.Length != 100 || stages.Select(item => item.StageId).Distinct(StringComparer.Ordinal).Count() != 100) errors.Add("Campaign stable 100-ID contract changed.");
            var devPlayable = registry?.Types.Count(item => item.ImplementationLevel == MapObjectImplementationLevel.DevPlayable) ?? 0;
            var prototypes = registry?.Types.Count(item => item.ImplementationLevel == MapObjectImplementationLevel.PlaceablePrototype) ?? 0;
            WriteM9BLog("validation.txt", new[]
            {
                $"valid={errors.Count == 0}", $"registeredTypes={actualIds.Length}", $"distinctPlacedTypes={placedTypeIds.Length}",
                $"persistedPalettePlacements={paletteMap?.Objects.Count ?? 0}", "saveReopenEditDelete=covered-by-M9B-editmode",
                $"devPlayable={devPlayable}", $"placeablePrototype={prototypes}", "releaseCandidate=0",
                "representativeTypes=9", $"representativePlacements={lab?.Objects.Count ?? 0}", "prototypeReadyBlocked=true",
                $"stableStageIds={stages.Select(item => item.StageId).Distinct(StringComparer.Ordinal).Count()}/{stages.Length}",
                $"ready={stages.Count(item => ContentAvailabilityResolver.Resolve(item) == ContentAvailability.Ready)}",
                $"t01MapSha256={FileSha256(T01MapPath)}", "operationalMapAutoPlacement=false", "t06Registered=false",
                "physicalDevice=BLOCKED", "errors=" + string.Join(" | ", errors)
            });
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static List<M9BObjectSpec> Specs()
        {
            const MapObjectImplementationLevel D = MapObjectImplementationLevel.DevPlayable;
            const MapObjectImplementationLevel P = MapObjectImplementationLevel.PlaceablePrototype;
            return new List<M9BObjectSpec>
            {
                new("OBJ_SIDE_SPRING","방향 전환 옆 스프링","Side Spring","",StageMapObjectKind.SideSpring,Vector2.one,D,"Spring"),
                new("OBJ_POUNDER","수직 왕복 절구","Pounder","",StageMapObjectKind.Pounder,new Vector2(2,3),D,"Pounder"),
                new("OBJ_RICE_SLOW","떡 감속 구역","Rice Slow Zone","",StageMapObjectKind.RiceSlow,new Vector2(2,1),D,"Rice"),
                new("TILE_HALF_BLOCK","상·하 반블럭","Half Block","",StageMapObjectKind.HalfBlock,new Vector2(1,.5f),D,"Half"),
                new("TILE_DROP_PLATFORM","하향점프 단방향 발판","Drop Platform","",StageMapObjectKind.DropPlatform,Vector2.one,D,"Drop"),
                new("OBJ_RAIL_PLATFORM","경로형 철도 발판","Rail Platform","",StageMapObjectKind.RailPlatform,new Vector2(2,.5f),D,"Rail"),
                new("MOON_LANTERN_STEP","달등불 발판","Moon Lantern Step","T01",StageMapObjectKind.MoonLanternStep,Vector2.one,D,"MoonLantern"),
                new("CLOUD_WHALE_FERRY","구름고래 나룻배","Cloud Whale Ferry","T02",StageMapObjectKind.CloudWhaleFerry,new Vector2(3,1),D,"Rail"),
                new("CLOUD_SHEEP_STEP","구름양 발판","Cloud Sheep Step","T02",StageMapObjectKind.CloudSheepStep,new Vector2(2,1),P,"Prototype"),
                new("UPDRAFT_COLUMN","상승기류 기둥","Updraft Column","T02",StageMapObjectKind.UpdraftColumn,new Vector2(2,3),P,"Prototype"),
                new("SHEPHERD_BELL","목동 방울","Shepherd Bell","T02",StageMapObjectKind.ShepherdBell,Vector2.one,P,"Prototype"),
                new("PAGE_BRIDGE","페이지 다리","Page Bridge","T03",StageMapObjectKind.PageBridge,new Vector2(3,1),P,"Prototype"),
                new("BOOKMARK_LIFT","책갈피 리프트","Bookmark Lift","T03",StageMapObjectKind.BookmarkLift,new Vector2(2,1),D,"BookmarkLift"),
                new("INK_BLOT","잉크 얼룩","Ink Blot","T03",StageMapObjectKind.InkBlot,new Vector2(2,1),D,"Rice"),
                new("DEW_SEED_STEP","이슬씨앗 발판","Dew Seed Step","T04",StageMapObjectKind.DewSeedStep,Vector2.one,P,"Prototype"),
                new("GLASS_VINE_LIFT","유리덩굴 리프트","Glass Vine Lift","T04",StageMapObjectKind.GlassVineLift,new Vector2(2,1),D,"GlassVineLift"),
                new("SAND_DRIP","모래방울","Sand Drip","T04",StageMapObjectKind.SandDrip,Vector2.one,P,"Prototype"),
                new("RESONANCE_TILE","공명 타일","Resonance Tile","T05",StageMapObjectKind.ResonanceTile,Vector2.one,P,"Prototype"),
                new("PRISM_SPRING","프리즘 스프링","Prism Spring","T05",StageMapObjectKind.PrismSpring,Vector2.one,D,"Spring"),
                new("CRYSTAL_STALACTITE","수정 종유석","Crystal Stalactite","T05",StageMapObjectKind.CrystalStalactite,new Vector2(1,2),P,"Prototype"),
                new("MOON_JADE_BALANCE","옥 저울","Jade Balance","T01",StageMapObjectKind.MoonJadeBalance,new Vector2(2,.5f),D,"Balance",true),
                new("MOON_RABBIT_BOWL","토끼 절구통","Rabbit Bowl","T01",StageMapObjectKind.MoonRabbitBowl,new Vector2(2,1),D,"MoonBowl"),
                new("MOON_JADE_PENDULUM","옥 추","Jade Pendulum","T01",StageMapObjectKind.MoonJadePendulum,new Vector2(2,.5f),D,"MoonPendulum"),
                new("MOON_SLIDING_EAVE","미끄럼 처마","Sliding Eave","T01",StageMapObjectKind.MoonSlidingEave,new Vector2(2,.5f),D,"MoonEave"),
                new("MOON_PHASE_STAIR","달 위상 계단","Moon Phase Stair","T01",StageMapObjectKind.MoonPhaseStair,new Vector2(2,1),D,"MoonPhase"),
                new("CLOUD_BALLOON_TETHER","풍선 닻 발판","Balloon Tether","T02",StageMapObjectKind.CloudBalloonTether,new Vector2(2,.5f),D,"Balloon"),
                new("CLOUD_WINDMILL_BLADE","풍차 날개","Windmill Blade","T02",StageMapObjectKind.CloudWindmillBlade,new Vector2(3,1),P,"Prototype"),
                new("CLOUD_RAINBOW_SLIDE","무지개 미끄럼틀","Rainbow Slide","T02",StageMapObjectKind.CloudRainbowSlide,new Vector2(3,1),P,"Prototype"),
                new("CLOUD_RAIN_UMBRELLA","비우산 발판","Rain Umbrella","T02",StageMapObjectKind.CloudRainUmbrella,new Vector2(2,1),P,"Prototype"),
                new("CLOUD_SAIL_STEP","돛 발판","Sail Step","T02",StageMapObjectKind.CloudSailStep,new Vector2(2,1),D,"CloudSailStep"),
                new("LIB_INDEX_DRAWER","색인 서랍","Index Drawer","T03",StageMapObjectKind.LibIndexDrawer,new Vector2(2,1),D,"Drawer"),
                new("LIB_LETTER_BELT","글자 벨트","Letter Belt","T03",StageMapObjectKind.LibLetterBelt,new Vector2(3,1),P,"Prototype"),
                new("LIB_POPUP_STAIR","팝업 계단","Pop-up Stair","T03",StageMapObjectKind.LibPopupStair,new Vector2(3,2),P,"Prototype"),
                new("LIB_SPINE_BRAKE","책등 브레이크","Spine Brake","T03",StageMapObjectKind.LibSpineBrake,new Vector2(2,1),D,"LibSpineBrake"),
                new("LIB_CHAPTER_FORK","장 분기대","Chapter Fork","T03",StageMapObjectKind.LibChapterFork,new Vector2(2,1),P,"Prototype"),
                new("GREEN_PETAL_CUP","꽃잎 잔","Petal Cup","T04",StageMapObjectKind.GreenPetalCup,new Vector2(2,1),P,"Prototype"),
                new("GREEN_DEW_GLASS","이슬 유리","Dew Glass","T04",StageMapObjectKind.GreenDewGlass,new Vector2(2,1),P,"Prototype"),
                new("GREEN_SUNDIAL_PETAL","해시계 꽃잎","Sundial Petal","T04",StageMapObjectKind.GreenSundialPetal,new Vector2(2,1),P,"Prototype"),
                new("GREEN_SAND_RETRACE","모래 되짚기 발판","Sand Retrace","T04",StageMapObjectKind.GreenSandRetrace,new Vector2(2,.5f),D,"Retrace"),
                new("GREEN_VINE_KNOT","덩굴 매듭","Vine Knot","T04",StageMapObjectKind.GreenVineKnot,new Vector2(2,1),P,"Prototype"),
                new("MINE_MAGNET_PAIR","자석 쌍 발판","Magnet Pair","T05",StageMapObjectKind.MineMagnetPair,new Vector2(2,.5f),D,"Magnet",true),
                new("MINE_SLICK_FACET","미끄럼 결정면","Slick Facet","T05",StageMapObjectKind.MineSlickFacet,new Vector2(2,1),P,"Prototype"),
                new("MINE_PRISM_BEAM","프리즘 광선","Prism Beam","T05",StageMapObjectKind.MinePrismBeam,new Vector2(3,1),P,"Prototype"),
                new("MINE_CRYSTAL_LEVER","수정 레버","Crystal Lever","T05",StageMapObjectKind.MineCrystalLever,Vector2.one,P,"Prototype"),
                new("MINE_CART_FORK","광차 분기","Mine Cart Fork","T05",StageMapObjectKind.MineCartFork,new Vector2(3,1),D,"MineCartFork")
            };
        }

        private static List<StageMapObjectTypeDefinition> BuildM9BDefinitions(IReadOnlyList<M9BObjectSpec> specs)
        {
            var definitions = new List<StageMapObjectTypeDefinition>(specs.Count);
            foreach (var spec in specs)
            {
                var commonPath = $"Assets/ANIMOL/Data/Development/M9Objects/{spec.Id}.asset";
                var path = string.IsNullOrEmpty(spec.ThemeId) ? commonPath : $"{M9BTypeRoot}/{spec.Id}.asset";
                var definition = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeDefinition>(path);
                if (definition == null) { definition = ScriptableObject.CreateInstance<StageMapObjectTypeDefinition>(); AssetDatabase.CreateAsset(definition, path); }
                var prefab = ResolveOrCreateM9BPrefab(spec);
                var sprite = prefab.GetComponentInChildren<SpriteRenderer>()?.sprite;
                var settings = MakeM9BSettings(spec, Vector2Int.zero, Array.Empty<string>());
                definition.EditorConfigure(spec.Id, spec.Name, spec.Kind, spec.Footprint,
                    spec.Kind == StageMapObjectKind.HalfBlock ? .5f : 1f, prefab, settings);
                definition.EditorConfigureCatalog(spec.English, spec.ThemeId,
                    spec.Level == MapObjectImplementationLevel.PlaceablePrototype
                        ? "PlaceablePrototype · 고유 런타임 미구현, 운영 Ready 차단"
                        : "DevPlayable · DEV 물리/초기화 검증 대상",
                    StageMapLayer.Object, sprite, sprite, spec.Level, new[] { spec.Behavior }, 2, spec.Pair);
                ApplyM9CDesignContract(definition);
                EditorUtility.SetDirty(definition); definitions.Add(definition);
            }
            return definitions;
        }

        private static GameObject ResolveOrCreateM9BPrefab(M9BObjectSpec spec)
        {
            if (string.IsNullOrEmpty(spec.ThemeId))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ANIMOL/Prefabs/MapObjects/{spec.Id}.prefab");
                if (existing != null) return existing;
            }
            var sprite = CreateM9BSprite(spec);
            var root = new GameObject(spec.Id);
            var visual = new GameObject("Visual", typeof(SpriteRenderer)); visual.transform.SetParent(root.transform, false);
            visual.GetComponent<SpriteRenderer>().sprite = sprite; visual.GetComponent<SpriteRenderer>().sortingOrder = 12;
            visual.transform.localPosition = new Vector3(spec.Footprint.x * .5f, spec.Footprint.y * .5f);
            switch (spec.Behavior)
            {
                case "Spring": root.AddComponent<BoxCollider2D>(); root.AddComponent<SideSpringObject>(); break;
                case "Rice": { var c = root.AddComponent<BoxCollider2D>(); c.isTrigger = true; root.AddComponent<RiceSlowZoneObject>(); break; }
                case "Rail": AddMovingPhysics<RailPlatformObject>(root, spec.Footprint); break;
                case "Balance": AddMovingPhysics<MoonJadeBalanceObject>(root, spec.Footprint); break;
                case "Balloon": AddMovingPhysics<CloudBalloonTetherObject>(root, spec.Footprint); break;
                case "Drawer": AddMovingPhysics<LibIndexDrawerObject>(root, spec.Footprint); break;
                case "Retrace": AddMovingPhysics<GreenSandRetraceObject>(root, spec.Footprint); break;
                case "Magnet": AddMovingPhysics<MineMagnetPairObject>(root, spec.Footprint); break;
                case "MoonLantern": root.AddComponent<BoxCollider2D>(); root.AddComponent<PlatformEffector2D>(); root.AddComponent<MoonLanternStepObject>(); break;
                case "MoonBowl": root.AddComponent<BoxCollider2D>(); root.AddComponent<MoonRabbitBowlObject>(); break;
                case "MoonPendulum": AddMovingPhysics<MoonJadePendulumObject>(root, spec.Footprint); break;
                case "MoonEave": AddMovingPhysics<MoonSlidingEaveObject>(root, spec.Footprint); break;
                case "MoonPhase": root.AddComponent<BoxCollider2D>(); root.AddComponent<MoonPhaseStairObject>(); break;
                case "MineCartFork": AddMovingPhysics<MineCartForkObject>(root, spec.Footprint); break;
                case "CloudSailStep": AddMovingPhysics<CloudSailStepObject>(root, spec.Footprint); break;
                case "BookmarkLift": AddMovingPhysics<BookmarkLiftObject>(root, spec.Footprint); break;
                case "GlassVineLift": AddMovingPhysics<GlassVineLiftObject>(root, spec.Footprint); break;
                case "LibSpineBrake": AddMovingPhysics<LibSpineBrakeObject>(root, spec.Footprint); break;
                default: root.AddComponent<PrototypeMapObject>(); break;
            }
            var collider = root.GetComponent<BoxCollider2D>();
            if (collider != null) { collider.size = spec.Footprint; collider.offset = spec.Footprint * .5f; }
            var path = $"{M9BPrefabRoot}/{spec.Id}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root); return prefab;
        }

        private static void AddMovingPhysics<T>(GameObject root, Vector2 footprint) where T : StageMapRuntimeObject
        { root.AddComponent<Rigidbody2D>(); var collider = root.AddComponent<BoxCollider2D>(); collider.size = footprint; collider.offset = footprint * .5f; root.AddComponent<T>(); }

        private static Sprite CreateM9BSprite(M9BObjectSpec spec)
        {
            var width = Mathf.Max(16, Mathf.RoundToInt(spec.Footprint.x * StageMapDefinition.SourceCellPixels));
            var height = Mathf.Max(8, Mathf.RoundToInt(spec.Footprint.y * StageMapDefinition.SourceCellPixels));
            var hash = StableM9BSeed(spec.Id);
            var color = Color.HSVToRGB((hash % 360) / 360f, .55f, spec.Level == MapObjectImplementationLevel.PlaceablePrototype ? .72f : 1f);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var border = x < 2 || y < 2 || x >= width - 2 || y >= height - 2;
                var stripe = ((x / 4) + (y / 4) + hash) % 5 == 0;
                pixels[y * width + x] = border ? new Color32(25, 30, 48, 255) :
                    (Color32)(stripe ? Color.Lerp(color, Color.white, .35f) : color);
            }
            texture.SetPixels32(pixels); texture.Apply();
            var path = $"{M9BArtRoot}/{spec.Id}.png"; File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = StageMapDefinition.SourceCellPixels; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static StageMapObjectSettings MakeM9BSettings(M9BObjectSpec spec, Vector2Int cell, IEnumerable<string> links)
        {
            var settings = new StageMapObjectSettings();
            IEnumerable<Vector2Int> path = Array.Empty<Vector2Int>();
            if (spec.Kind == StageMapObjectKind.Pounder) path = new[] { cell, cell + new Vector2Int(0, -3) };
            else if (spec.Kind is StageMapObjectKind.RailPlatform or StageMapObjectKind.CloudWhaleFerry) path = new[] { cell, cell + new Vector2Int(3, 0) };
            else if (spec.Kind == StageMapObjectKind.MoonJadeBalance) path = new[] { cell, cell + Vector2Int.down };
            else if (spec.Kind == StageMapObjectKind.CloudBalloonTether) path = new[] { cell, cell + new Vector2Int(0, -3) };
            else if (spec.Kind == StageMapObjectKind.LibIndexDrawer) path = new[] { cell, cell + new Vector2Int(2, 0) };
            else if (spec.Kind == StageMapObjectKind.GreenSandRetrace) path = new[] { cell, cell + new Vector2Int(3, 0) };
            else if (spec.Kind == StageMapObjectKind.MineMagnetPair) path = new[] { cell, cell + Vector2Int.right };
            else if (spec.Kind == StageMapObjectKind.MoonJadePendulum) path = new[] { cell + new Vector2Int(-2, 0), cell + new Vector2Int(2, 0) };
            else if (spec.Kind == StageMapObjectKind.MoonSlidingEave) path = new[] { cell, cell + Vector2Int.right };
            else if (spec.Kind == StageMapObjectKind.CloudSailStep) path = new[] { cell, cell + new Vector2Int(3, 0) };
            else if (spec.Kind is StageMapObjectKind.BookmarkLift or StageMapObjectKind.GlassVineLift) path = new[] { cell, cell + new Vector2Int(0, 4) };
            else if (spec.Kind == StageMapObjectKind.LibSpineBrake) path = new[] { cell, cell + new Vector2Int(4, 0) };
            var speed = spec.Kind == StageMapObjectKind.CloudBalloonTether ? 1.25f : spec.Kind == StageMapObjectKind.LibIndexDrawer ? 2f : 2.5f;
            settings.EditorConfigure(3, StageMapObjectDirection.Right, spec.Footprint, HalfBlockPlacement.Lower,
                SideSpringContactPolicy.FacingSideOnly, 8f, 4.5f, speed, .6f, .6f, spec.Kind == StageMapObjectKind.LibIndexDrawer ? 3f : 5f,
                .55f, .75f, true, path);
            settings.EditorConfigureAuthoring(spec.Level, links, StableM9BSeed(spec.Id), MapObjectResetPolicy.RespawnAndRetry,
                MapObjectRouteRole.Required, spec.Pair ? 1 : 0,
                spec.Level == MapObjectImplementationLevel.PlaceablePrototype ? "고유 동작 미구현: DEV 고스트/직렬화 전용. 운영 Ready 금지." : string.Empty);
            settings.EditorConfigureDesignBehavior(.45f, 1.5f, 1f,
                spec.Kind is StageMapObjectKind.MoonLanternStep or StageMapObjectKind.MoonPhaseStair
                    ? MapObjectPassengerPolicy.DeferStateChangeWhileOccupied : MapObjectPassengerPolicy.Carry,
                spec.Kind switch
                {
                    StageMapObjectKind.MoonJadePendulum => MapObjectTriggerMode.AutomaticCycle,
                    StageMapObjectKind.MoonPhaseStair => MapObjectTriggerMode.OnPatternPlate,
                    StageMapObjectKind.MoonRabbitBowl => MapObjectTriggerMode.OnNextJump,
                    _ => MapObjectTriggerMode.OnOccupancy
                }, 2f, 90f, 1f, true);
            return settings;
        }

        private static StageMapDefinition BuildPaletteRoundTripMap(IReadOnlyList<M9BObjectSpec> specs, IReadOnlyList<StageMapObjectTypeDefinition> definitions)
        {
            var map = LoadOrCreateM9BMap(M9BPaletteMapPath, "DEV-M9B-PALETTE-45", new RectInt(0, 0, 4, 3));
            var typeLookup = definitions.ToDictionary(item => item.StableTypeId, StringComparer.Ordinal);
            var ids = specs.ToDictionary(item => item.Id, item => $"M9B-{item.Id}-01", StringComparer.Ordinal);
            ids["MOON_JADE_BALANCE-B"] = "M9B-MOON_JADE_BALANCE-02";
            ids["MINE_MAGNET_PAIR-B"] = "M9B-MINE_MAGNET_PAIR-02";
            for (var i = 0; i < specs.Count; i++)
            {
                var spec = specs[i]; var cell = new Vector2Int(2 + (i % 7) * 8, 5 + (i / 7) * 5);
                var link = spec.Id == "MOON_JADE_BALANCE" ? ids["MOON_JADE_BALANCE-B"] : spec.Id == "MINE_MAGNET_PAIR" ? ids["MINE_MAGNET_PAIR-B"] : null;
                PlaceM9B(map, typeLookup[spec.Id], ids[spec.Id], cell, string.IsNullOrEmpty(link) ? Array.Empty<string>() : new[] { link }, spec);
            }
            PlaceM9B(map, typeLookup["MOON_JADE_BALANCE"], ids["MOON_JADE_BALANCE-B"], new Vector2Int(58, 5), new[] { ids["MOON_JADE_BALANCE"] }, specs.Single(item => item.Id == "MOON_JADE_BALANCE"));
            PlaceM9B(map, typeLookup["MINE_MAGNET_PAIR"], ids["MINE_MAGNET_PAIR-B"], new Vector2Int(58, 10), new[] { ids["MINE_MAGNET_PAIR"] }, specs.Single(item => item.Id == "MINE_MAGNET_PAIR"));
            map.EditorMarkCollisionDataSynchronized(); EditorUtility.SetDirty(map); return map;
        }

        private static StageMapDefinition BuildRepresentativeLabMap(IReadOnlyList<StageMapObjectTypeDefinition> definitions)
        {
            var map = LoadOrCreateM9BMap(M9BLabMapPath, "DEV-M9B-REPRESENTATIVE-LAB", new RectInt(-1, -1, 2, 2));
            var specs = Specs().ToDictionary(item => item.Id, StringComparer.Ordinal);
            var defs = definitions.ToDictionary(item => item.StableTypeId, StringComparer.Ordinal);
            PlaceM9B(map, defs["MOON_JADE_BALANCE"], "LAB-BALANCE-A", new Vector2Int(-5, 1), new[] { "LAB-BALANCE-B" }, specs["MOON_JADE_BALANCE"]);
            PlaceM9B(map, defs["MOON_JADE_BALANCE"], "LAB-BALANCE-B", new Vector2Int(-2, 1), new[] { "LAB-BALANCE-A" }, specs["MOON_JADE_BALANCE"]);
            PlaceM9B(map, defs["CLOUD_BALLOON_TETHER"], "LAB-BALLOON", new Vector2Int(0, 3), Array.Empty<string>(), specs["CLOUD_BALLOON_TETHER"]);
            PlaceM9B(map, defs["LIB_INDEX_DRAWER"], "LAB-DRAWER", new Vector2Int(3, 0), Array.Empty<string>(), specs["LIB_INDEX_DRAWER"]);
            PlaceM9B(map, defs["GREEN_SAND_RETRACE"], "LAB-RETRACE", new Vector2Int(-5, -4), Array.Empty<string>(), specs["GREEN_SAND_RETRACE"]);
            PlaceM9B(map, defs["MINE_MAGNET_PAIR"], "LAB-MAGNET-A", new Vector2Int(1, -4), new[] { "LAB-MAGNET-B" }, specs["MINE_MAGNET_PAIR"]);
            PlaceM9B(map, defs["MINE_MAGNET_PAIR"], "LAB-MAGNET-B", new Vector2Int(4, -4), new[] { "LAB-MAGNET-A" }, specs["MINE_MAGNET_PAIR"]);
            PlaceM9B(map, defs["CLOUD_SAIL_STEP"], "LAB-SAIL", new Vector2Int(-11, 3), Array.Empty<string>(), specs["CLOUD_SAIL_STEP"]);
            PlaceM9B(map, defs["BOOKMARK_LIFT"], "LAB-BOOKMARK", new Vector2Int(-11, -4), Array.Empty<string>(), specs["BOOKMARK_LIFT"]);
            PlaceM9B(map, defs["GLASS_VINE_LIFT"], "LAB-VINE", new Vector2Int(8, -4), Array.Empty<string>(), specs["GLASS_VINE_LIFT"]);
            PlaceM9B(map, defs["LIB_SPINE_BRAKE"], "LAB-SPINE", new Vector2Int(7, 3), Array.Empty<string>(), specs["LIB_SPINE_BRAKE"]);
            map.EditorMarkCollisionDataSynchronized(); EditorUtility.SetDirty(map); return map;
        }

        private static StageMapDefinition LoadOrCreateM9BMap(string path, string id, RectInt chunks)
        {
            var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(path);
            if (map == null) { map = ScriptableObject.CreateInstance<StageMapDefinition>(); AssetDatabase.CreateAsset(map, path); }
            foreach (var placement in map.Objects.ToArray()) map.EditorRemoveObject(placement.StableId);
            map.EditorInitializeIdentity(id, "DEV");
            if (map.WorldUnitsPerCell <= 0f) map.EditorInitializeVariableChunksFromAuthoredContent(1f);
            if (!map.EditorTrySetChunkBounds(chunks, true, out _)) throw new InvalidOperationException($"Cannot configure DEV chunks for {id}.");
            return map;
        }

        private static void PlaceM9B(StageMapDefinition map, StageMapObjectTypeDefinition type, string id, Vector2Int cell,
            IEnumerable<string> links, M9BObjectSpec spec)
        { map.EditorPlaceObject(id, spec.Kind, cell.x, cell.y, spec.Id, type.Prefab, MakeM9BSettings(spec, cell, links)); }

        private static void BuildRepresentativeLabScene(StageMapDefinition map, StageMapObjectTypeRegistry registry)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(PortraitWorldCameraPolicy));
            cameraObject.tag = "MainCamera"; cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 10.667f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.04f, .05f, .11f);
            var world = new GameObject("M9BThemePlatformWorld", typeof(StageMapRuntimeLoader));
            world.GetComponent<StageMapRuntimeLoader>().ConfigureDefinition(map, registry, true);
            var floor = new GameObject("Floor", typeof(BoxCollider2D), typeof(SpriteRenderer));
            floor.transform.position = new Vector3(0, -6.5f); floor.transform.localScale = new Vector3(14, 1, 1);
            floor.GetComponent<SpriteRenderer>().color = new Color(.2f, .25f, .4f);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Gameplay/DevPlayer.prefab");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab); player.name = "M9BThemePlatformPlayer"; player.transform.position = new Vector3(0, -5);
            var checkpoint = new GameObject("M9BCheckpoint").transform; checkpoint.position = player.transform.position;
            var session = new GameObject("ObjectLabSession", typeof(ObjectLabSession)).GetComponent<ObjectLabSession>();
            var serialized = new SerializedObject(session); serialized.FindProperty("checkpoint").objectReferenceValue = checkpoint; serialized.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("DevelopmentSceneAccessGuard", typeof(DevelopmentSceneAccessGuard));
            var ui = InstantiatePrefab("UI_CommonRoot.prefab"); AttachPrefab("SC15_GameplayHud.prefab", ui.transform.Find("SafeArea/ScreenHost"));
            var pause = AttachPrefab("PauseModal.prefab", ui.transform.Find("SafeArea/ModalHost")); pause.SetActive(false);
            ui.AddComponent<DevMobileInputRouter>(); ui.AddComponent<ObjectLabUiPresenter>();
            CreateLabel("M9BBanner", ui.transform.Find("SafeArea/OverlayHost"), "DEV M9B 대표 5종 · 운영/코인 미연결", 26,
                new Vector2(.05f, .5f), new Vector2(.95f, .5f), new Vector2(900, 60), new Vector2(0, 520), TextAnchor.MiddleCenter);
            CreateEventSystem(); EditorSceneManager.SaveScene(scene, DevelopmentBuildScenePolicy.ThemePlatformLabPath);
        }

        private static void EnsureM9BSceneInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(item => item.path != DevelopmentBuildScenePolicy.ThemePlatformLabPath))
                scenes.Add(new EditorBuildSettingsScene(DevelopmentBuildScenePolicy.ThemePlatformLabPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void BackupAndUpdateThemeMetadata()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            var backup = $"Logs/{M9BPrefix}theme-backup-{timestamp}"; Directory.CreateDirectory(backup);
            var names = new[]
            {
                ("T01","월궁","Moon Palace"), ("T02","구름고래 목장","Cloud Whale Ranch"),
                ("T03","별가루 도서관","Stardust Library"), ("T04","시간유리 온실","Hourglass Conservatory"),
                ("T05","오로라 수정광산","Aurora Crystal Mine")
            };
            var specs = Specs();
            foreach (var entry in names)
            {
                var path = $"Assets/ANIMOL/Data/Campaign/Themes/{entry.Item1}.asset";
                File.Copy(path, Path.Combine(backup, Path.GetFileName(path)), true);
                var theme = AssetDatabase.LoadAssetAtPath<ThemeDefinition>(path);
                if (theme == null || theme.ThemeId != entry.Item1 || theme.Stages.Count != 20) throw new InvalidOperationException($"{entry.Item1} stable theme/20 slots are missing.");
                var gimmicks = specs.Where(item => string.IsNullOrEmpty(item.ThemeId) || item.ThemeId == entry.Item1).Select(item => item.Id).ToArray();
                theme.EditorConfigureThemeCatalog(entry.Item2, entry.Item3, 20, $"DEV-{entry.Item1}-PALETTE", $"DEV-{entry.Item1}-BACKGROUND", $"DEV-{entry.Item1}-AUDIO", gimmicks);
                EditorUtility.SetDirty(theme);
            }
            WriteM9BLog("theme-migration.txt", new[] { $"backup={backup}", "metadataOnly=true", "stageArraysPreserved=20x5", "T06=false", "hiddenForest=backlog-only" });
        }

        private static Dictionary<string, string> RequireM9BAudit()
        {
            if (!File.Exists(M9BAuditPath)) throw new InvalidOperationException("Run M9B audit first.");
            return File.ReadAllLines(M9BAuditPath).Where(line => line.Contains('=')).Select(line => line.Split(new[] { '=' }, 2))
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
        }

        private static void AssertM9BProtectedMaps(IReadOnlyDictionary<string, string> baseline, string phase)
        {
            foreach (var path in Directory.GetFiles("Assets/ANIMOL/Data/Campaign/Maps", "*.asset"))
            {
                var key = $"campaignMap.{Path.GetFileName(path)}";
                if (!baseline.TryGetValue(key, out var before) || !string.Equals(before, FileSha256(path.Replace('\\','/')), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"{phase}: operational campaign map changed: {path}");
            }
            foreach (var pair in new[] { ("t01StageSha256", T01StagePath), ("t01RewardSha256", T01RewardPath) })
                if (!baseline.TryGetValue(pair.Item1, out var before) || !string.Equals(before, FileSha256(pair.Item2), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"{phase}: protected asset changed: {pair.Item2}");
        }

        private static void EnsureM9BFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\','/'); if (!string.IsNullOrWhiteSpace(parent)) EnsureM9BFolder(parent);
            AssetDatabase.CreateFolder(parent ?? "Assets", Path.GetFileName(path));
        }

        private static int StableM9BSeed(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var character in value ?? string.Empty) { hash ^= character; hash *= 16777619; }
                return (int)(hash & 0x7fffffff);
            }
        }

        private static void WriteM9BLog(string suffix, IEnumerable<string> lines)
        { Directory.CreateDirectory("Logs"); File.WriteAllLines(Path.Combine("Logs", M9BPrefix + suffix), lines); }
    }
}
