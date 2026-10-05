using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ANIMOL.Core
{
    public enum StageMapLayer { Background, Terrain, Decoration, Object }
    public enum StageMapObjectKind
    {
        PlayerStart, Checkpoint, BubbleCandidate, Exit, Hole, Spike, MovingPlatform,
        Door, Switch, EventTrigger, CameraZone,
        SideSpring, Pounder, RiceSlow, HalfBlock, DropPlatform, RailPlatform,
        MoonLanternStep,
        CloudWhaleFerry, CloudSheepStep, UpdraftColumn, ShepherdBell,
        PageBridge, BookmarkLift, InkBlot,
        DewSeedStep, GlassVineLift, SandDrip,
        ResonanceTile, PrismSpring, CrystalStalactite,
        MoonJadeBalance, MoonRabbitBowl, MoonJadePendulum, MoonSlidingEave, MoonPhaseStair,
        CloudBalloonTether, CloudWindmillBlade, CloudRainbowSlide, CloudRainUmbrella, CloudSailStep,
        LibIndexDrawer, LibLetterBelt, LibPopupStair, LibSpineBrake, LibChapterFork,
        GreenPetalCup, GreenDewGlass, GreenSundialPetal, GreenSandRetrace, GreenVineKnot,
        MineMagnetPair, MineSlickFacet, MinePrismBeam, MineCrystalLever, MineCartFork
    }

    public enum StageMapObjectDirection { Left = -1, Right = 1 }
    public enum HalfBlockPlacement { Lower, Upper }
    public enum SideSpringContactPolicy { FacingSideOnly, AnySide }
    public enum MapObjectImplementationLevel { DevPlayable, PlaceablePrototype, ReleaseCandidate }
    public enum MapObjectResetPolicy { RespawnAndRetry, RetryOnly, MapReload }
    public enum MapObjectRouteRole { Required, Shortcut, Bonus }
    public enum MapObjectPassengerPolicy { Carry, DeferStateChangeWhileOccupied, TriggerOnly, NoPassengerMovement }
    public enum MapObjectTriggerMode { AutomaticCycle, OnOccupancy, OnApproach, OnPatternPlate, OnNextJump, OnLinkedTrigger }

    [Serializable]
    public sealed class StageMapObjectSettings
    {
        [SerializeField] private int version = 1;
        [SerializeField] private StageMapObjectDirection direction = StageMapObjectDirection.Right;
        [SerializeField] private Vector2 footprintCells = Vector2.one;
        [SerializeField] private HalfBlockPlacement halfPlacement;
        [SerializeField] private SideSpringContactPolicy springContactPolicy = SideSpringContactPolicy.FacingSideOnly;
        [SerializeField] private float horizontalImpulse = 8f;
        [SerializeField] private float verticalImpulse = 4.5f;
        [SerializeField] private float movementSpeed = 3f;
        [SerializeField] private float upperPauseSeconds = .6f;
        [SerializeField] private float lowerPauseSeconds = .6f;
        [SerializeField] private float activationRangeCells = 5f;
        [SerializeField, Range(.05f, .99f)] private float groundSpeedMultiplier = .55f;
        [SerializeField] private float endStopSeconds = .75f;
        [SerializeField] private bool returnWhenEmpty = true;
        [SerializeField] private List<Vector2Int> pathCells = new List<Vector2Int>();
        [SerializeField] private MapObjectImplementationLevel implementationLevel = MapObjectImplementationLevel.DevPlayable;
        [SerializeField] private List<string> linkedInstanceIds = new List<string>();
        [SerializeField] private int phaseSeed;
        [SerializeField] private MapObjectResetPolicy resetPolicy = MapObjectResetPolicy.RespawnAndRetry;
        [SerializeField] private MapObjectRouteRole routeRole = MapObjectRouteRole.Required;
        [SerializeField] private int minimumLinkCount;
        [SerializeField, TextArea] private string prototypeNotice = string.Empty;
        [SerializeField] private float warningSeconds = .45f;
        [SerializeField] private float activeSeconds = 1.5f;
        [SerializeField] private float recoverSeconds = 1f;
        [SerializeField] private MapObjectPassengerPolicy passengerPolicy = MapObjectPassengerPolicy.Carry;
        [SerializeField] private MapObjectTriggerMode triggerMode = MapObjectTriggerMode.OnOccupancy;
        [SerializeField] private float arcHeightCells = 2f;
        [SerializeField] private float rotationDegrees = 90f;
        [SerializeField] private float effectStrength = 1f;
        [SerializeField] private bool deferWhileOccupied = true;

        public int Version => version;
        public StageMapObjectDirection Direction => direction;
        public Vector2 FootprintCells => footprintCells;
        public HalfBlockPlacement HalfPlacement => halfPlacement;
        public SideSpringContactPolicy SpringContactPolicy => springContactPolicy;
        public float HorizontalImpulse => horizontalImpulse;
        public float VerticalImpulse => verticalImpulse;
        public float MovementSpeed => movementSpeed;
        public float UpperPauseSeconds => upperPauseSeconds;
        public float LowerPauseSeconds => lowerPauseSeconds;
        public float ActivationRangeCells => activationRangeCells;
        public float GroundSpeedMultiplier => groundSpeedMultiplier;
        public float EndStopSeconds => endStopSeconds;
        public bool ReturnWhenEmpty => returnWhenEmpty;
        public IReadOnlyList<Vector2Int> PathCells => pathCells;
        public MapObjectImplementationLevel ImplementationLevel => implementationLevel;
        public IReadOnlyList<string> LinkedInstanceIds => linkedInstanceIds;
        public int PhaseSeed => phaseSeed;
        public MapObjectResetPolicy ResetPolicy => resetPolicy;
        public MapObjectRouteRole RouteRole => routeRole;
        public int MinimumLinkCount => minimumLinkCount;
        public string PrototypeNotice => prototypeNotice;
        public float WarningSeconds => warningSeconds;
        public float ActiveSeconds => activeSeconds;
        public float RecoverSeconds => recoverSeconds;
        public MapObjectPassengerPolicy PassengerPolicy => passengerPolicy;
        public MapObjectTriggerMode TriggerMode => triggerMode;
        public float ArcHeightCells => arcHeightCells;
        public float RotationDegrees => rotationDegrees;
        public float EffectStrength => effectStrength;
        public bool DeferWhileOccupied => deferWhileOccupied;

        public StageMapObjectSettings Clone()
        {
            var clone = new StageMapObjectSettings();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), clone);
            clone.pathCells ??= new List<Vector2Int>();
            clone.linkedInstanceIds ??= new List<string>();
            return clone;
        }

        public void EditorConfigure(int settingsVersion, StageMapObjectDirection facing, Vector2 footprint,
            HalfBlockPlacement half, SideSpringContactPolicy contactPolicy, float horizontal, float vertical,
            float speed, float upperPause, float lowerPause, float activationRange, float slowMultiplier,
            float stopSeconds, bool shouldReturn, IEnumerable<Vector2Int> path)
        {
            version = Mathf.Max(1, settingsVersion);
            direction = facing;
            footprintCells = new Vector2(Mathf.Max(.5f, footprint.x), Mathf.Max(.5f, footprint.y));
            halfPlacement = half;
            springContactPolicy = contactPolicy;
            horizontalImpulse = Mathf.Max(0f, horizontal);
            verticalImpulse = Mathf.Max(0f, vertical);
            movementSpeed = Mathf.Max(.01f, speed);
            upperPauseSeconds = Mathf.Max(0f, upperPause);
            lowerPauseSeconds = Mathf.Max(0f, lowerPause);
            activationRangeCells = Mathf.Max(0f, activationRange);
            groundSpeedMultiplier = Mathf.Clamp(slowMultiplier, .05f, .99f);
            endStopSeconds = Mathf.Max(0f, stopSeconds);
            returnWhenEmpty = shouldReturn;
            pathCells = path?.Distinct().ToList() ?? new List<Vector2Int>();
        }

        public void EditorConfigureAuthoring(MapObjectImplementationLevel level, IEnumerable<string> linkedIds,
            int deterministicPhaseSeed, MapObjectResetPolicy objectResetPolicy, MapObjectRouteRole objectRouteRole,
            int requiredLinkCount, string unimplementedNotice)
        {
            implementationLevel = level;
            linkedInstanceIds = linkedIds?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToList()
                                ?? new List<string>();
            phaseSeed = deterministicPhaseSeed;
            resetPolicy = objectResetPolicy;
            routeRole = objectRouteRole;
            minimumLinkCount = Mathf.Max(0, requiredLinkCount);
            prototypeNotice = unimplementedNotice ?? string.Empty;
        }

        public void EditorSetLinkedInstanceIds(IEnumerable<string> linkedIds) => linkedInstanceIds =
            linkedIds?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToList()
            ?? new List<string>();

        public void EditorSetPathCells(IEnumerable<Vector2Int> cells) => pathCells =
            cells?.Distinct().ToList() ?? new List<Vector2Int>();

        public void EditorConfigureDesignBehavior(float warning, float active, float recover,
            MapObjectPassengerPolicy passengers, MapObjectTriggerMode trigger, float arcHeight,
            float rotation, float strength, bool deferStateChange)
        {
            warningSeconds = Mathf.Max(0f, warning);
            activeSeconds = Mathf.Max(0f, active);
            recoverSeconds = Mathf.Max(0f, recover);
            passengerPolicy = passengers;
            triggerMode = trigger;
            arcHeightCells = Mathf.Max(0f, arcHeight);
            rotationDegrees = rotation;
            effectStrength = Mathf.Max(0f, strength);
            deferWhileOccupied = deferStateChange;
        }

        public void EditorFlip() => direction = direction == StageMapObjectDirection.Left
            ? StageMapObjectDirection.Right : StageMapObjectDirection.Left;
    }

    [Serializable]
    public sealed class StageMapCell
    {
        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private StageMapLayer layer;
        [SerializeField] private string tileId = string.Empty;
        [SerializeField] private string variantId = string.Empty;
        public int X => x;
        public int Y => y;
        public StageMapLayer Layer => layer;
        public string TileId => tileId;
        public string VariantId => variantId;

        public StageMapCell(int x, int y, StageMapLayer layer, string tileId, string variantId)
        {
            this.x = x;
            this.y = y;
            this.layer = layer;
            this.tileId = tileId ?? string.Empty;
            this.variantId = variantId ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class StageMapObjectPlacement
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private StageMapObjectKind kind;
        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private string dataKey = string.Empty;
        [SerializeField] private GameObject prefab;
        [SerializeField] private StageMapObjectSettings settings = new StageMapObjectSettings();
        public string StableId => stableId;
        public StageMapObjectKind Kind => kind;
        public int X => x;
        public int Y => y;
        public string DataKey => dataKey;
        public GameObject Prefab => prefab;
        public StageMapObjectSettings Settings => settings ??= new StageMapObjectSettings();

        public StageMapObjectPlacement(string stableId, StageMapObjectKind kind, int x, int y, string dataKey)
            : this(stableId, kind, x, y, dataKey, null, null) { }

        public StageMapObjectPlacement(string stableId, StageMapObjectKind kind, int x, int y, string dataKey,
            GameObject prefab, StageMapObjectSettings settings)
        {
            this.stableId = stableId ?? string.Empty;
            this.kind = kind;
            this.x = x;
            this.y = y;
            this.dataKey = dataKey ?? string.Empty;
            this.prefab = prefab;
            this.settings = settings?.Clone() ?? new StageMapObjectSettings();
        }

        public void EditorFlip() => Settings.EditorFlip();
    }

    [Serializable]
    public sealed class StageMapHumanCompletionEvidence
    {
        [SerializeField] private int mapVersion;
        [SerializeField] private string contentHash = string.Empty;
        [SerializeField] private int bubbleSeed = -1;
        [SerializeField] private string[] bubbleCombination = Array.Empty<string>();
        [SerializeField] private string playEnvironment = string.Empty;
        [SerializeField] private float elapsedSeconds;
        [SerializeField] private string note = string.Empty;

        public int MapVersion => mapVersion;
        public string ContentHash => contentHash;
        public int BubbleSeed => bubbleSeed;
        public IReadOnlyList<string> BubbleCombination => bubbleCombination;
        public string PlayEnvironment => playEnvironment;
        public float ElapsedSeconds => elapsedSeconds;
        public string Note => note;
        public string CombinationKey => string.Join("+", (bubbleCombination ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal));

        public StageMapHumanCompletionEvidence(int mapVersion, string contentHash, int bubbleSeed, string[] bubbleCombination,
            string playEnvironment, float elapsedSeconds, string note)
        {
            this.mapVersion = mapVersion;
            this.contentHash = contentHash ?? string.Empty;
            this.bubbleSeed = bubbleSeed;
            this.bubbleCombination = bubbleCombination ?? Array.Empty<string>();
            this.playEnvironment = playEnvironment ?? string.Empty;
            this.elapsedSeconds = elapsedSeconds;
            this.note = note ?? string.Empty;
        }

        public bool IsValidFor(int expectedMapVersion, string expectedContentHash, ISet<string> authoredCandidates) =>
            mapVersion == expectedMapVersion && string.Equals(contentHash, expectedContentHash, StringComparison.Ordinal) &&
            bubbleSeed >= 0 && elapsedSeconds > 0f && !string.IsNullOrWhiteSpace(playEnvironment) &&
            bubbleCombination != null && bubbleCombination.Length == 3 &&
            bubbleCombination.Distinct(StringComparer.Ordinal).Count() == 3 && bubbleCombination.All(authoredCandidates.Contains);
    }

    [CreateAssetMenu(menuName = "ANIMOL/Campaign/Stage Map", fileName = "StageMapDefinition")]
    public sealed partial class StageMapDefinition : ScriptableObject
    {
        public const int SourceCellPixels = 16;
        public const int TilesPerChunk = 16;

        [Header("Stable identity")]
        [SerializeField] private string stageId = string.Empty;
        [SerializeField] private string themeId = string.Empty;
        [SerializeField] private int mapVersion = 1;
        [Tooltip("0 means product world scale is still unconfigured. The editor uses a clearly marked DEV preview fallback.")]
        [SerializeField] private float worldUnitsPerCell;

        [Header("Variable chunk bounds - 16 x 16 cells per chunk")]
        [SerializeField] private int minChunkX;
        [SerializeField] private int minChunkY;
        [SerializeField] private int chunkWidth;
        [SerializeField] private int chunkHeight;
        [SerializeField] private int authoringRevision;
        [SerializeField] private int collisionDataRevision;

        [Header("Stage properties - zero/empty remains unconfigured")]
        [SerializeField] private string[] fixedAnimalIds = Array.Empty<string>();
        [SerializeField] private string initialAnimalId = string.Empty;
        [SerializeField] private float mainTimeLimitSeconds;
        [SerializeField] private float escapeTimeLimitSeconds;
        [SerializeField] private float fastClearThresholdSeconds;
        [SerializeField] private string[] optionalObjectiveIds = Array.Empty<string>();
        [SerializeField] private float exitRevealCutsceneSeconds;
        [SerializeField] private bool timersAdvanceDuringExitReveal;
        [SerializeField] private CampaignStageRewardDefinition rewardDefinition;

        [Header("Authored map")]
        [SerializeField] private List<StageMapCell> cells = new List<StageMapCell>();
        [SerializeField] private List<StageMapObjectPlacement> objects = new List<StageMapObjectPlacement>();

        [Header("Human completion gate")]
        [SerializeField] private bool manuallyCompleted;
        [SerializeField] private int reviewedMapVersion;
        [SerializeField] private string reviewedContentHash = string.Empty;
        [SerializeField] private string reviewerNote = string.Empty;
        [SerializeField] private int reviewedBubbleSeed = -1;
        [SerializeField] private string[] reviewedBubbleCombination = Array.Empty<string>();
        [SerializeField] private string reviewedPlayEnvironment = string.Empty;
        [SerializeField] private float reviewedElapsedSeconds;
        [SerializeField] private List<StageMapHumanCompletionEvidence> humanCompletionEvidence = new List<StageMapHumanCompletionEvidence>();

        public string StageId => stageId;
        public string ThemeId => themeId;
        public int MapVersion => mapVersion;
        public float WorldUnitsPerCell => worldUnitsPerCell;
        public RectInt ChunkBounds => new RectInt(minChunkX, minChunkY, chunkWidth, chunkHeight);
        public RectInt CellBounds => new RectInt(minChunkX * TilesPerChunk, minChunkY * TilesPerChunk,
            chunkWidth * TilesPerChunk, chunkHeight * TilesPerChunk);
        public bool HasValidChunkBounds => chunkWidth > 0 && chunkHeight > 0;
        public RectInt EditorPreviewChunkBounds => HasValidChunkBounds
            ? ChunkBounds
            : new RectInt(0, 0, 1, 1);
        public RectInt EditorPreviewCellBounds
        {
            get
            {
                var bounds = EditorPreviewChunkBounds;
                return new RectInt(bounds.xMin * TilesPerChunk, bounds.yMin * TilesPerChunk,
                    bounds.width * TilesPerChunk, bounds.height * TilesPerChunk);
            }
        }
        public int AuthoringRevision => authoringRevision;
        public int CollisionDataRevision => collisionDataRevision;
        public IReadOnlyList<string> FixedAnimalIds => fixedAnimalIds;
        public string InitialAnimalId => initialAnimalId;
        public float MainTimeLimitSeconds => mainTimeLimitSeconds;
        public float EscapeTimeLimitSeconds => escapeTimeLimitSeconds;
        public float FastClearThresholdSeconds => fastClearThresholdSeconds;
        public IReadOnlyList<string> OptionalObjectiveIds => optionalObjectiveIds;
        public float ExitRevealCutsceneSeconds => exitRevealCutsceneSeconds;
        public bool TimersAdvanceDuringExitReveal => timersAdvanceDuringExitReveal;
        public CampaignStageRewardDefinition RewardDefinition => rewardDefinition;
        public IReadOnlyList<StageMapCell> Cells => cells;
        public IReadOnlyList<StageMapObjectPlacement> Objects => objects;
        public bool ManuallyCompleted => manuallyCompleted;
        public string ReviewerNote => reviewerNote;
        public int ReviewedBubbleSeed => reviewedBubbleSeed;
        public IReadOnlyList<string> ReviewedBubbleCombination => reviewedBubbleCombination;
        public string ReviewedPlayEnvironment => reviewedPlayEnvironment;
        public float ReviewedElapsedSeconds => reviewedElapsedSeconds;
        public IReadOnlyList<StageMapHumanCompletionEvidence> HumanCompletionEvidence => humanCompletionEvidence;
        public string ContentHash => ComputeContentHash();
        public int CurrentHumanCompletionCombinationCount => GetCurrentHumanCompletionKeys().Count;
        public int RequiredHumanCompletionCombinationCount => BubbleLayoutSelector.EnumerateThreeCombinations(
            objects.Where(item => item.Kind == StageMapObjectKind.BubbleCandidate).Select(item => item.StableId)).Count;
        public bool HasCurrentHumanCompletionReview => manuallyCompleted && RequiredHumanCompletionCombinationCount > 0 &&
                                                        CurrentHumanCompletionCombinationCount == RequiredHumanCompletionCombinationCount;

        public StageMapCell FindCell(int x, int y, StageMapLayer layer) => cells.FirstOrDefault(cell => cell.X == x && cell.Y == y && cell.Layer == layer);

        public static Vector2Int CellToChunk(int x, int y) => new Vector2Int(FloorDiv(x, TilesPerChunk), FloorDiv(y, TilesPerChunk));
        public bool ContainsCell(int x, int y) => HasValidChunkBounds && CellBounds.Contains(new Vector2Int(x, y));
        public bool CanAuthorCell(int x, int y) => ContainsCell(x, y) ||
                                                   (!HasValidChunkBounds && EditorPreviewCellBounds.Contains(new Vector2Int(x, y)));
        public bool ContainsChunk(Vector2Int chunk) => HasValidChunkBounds && ChunkBounds.Contains(chunk);
        public Rect GetWorldBounds()
        {
            var bounds = CellBounds;
            return new Rect(bounds.xMin * worldUnitsPerCell, bounds.yMin * worldUnitsPerCell,
                bounds.width * worldUnitsPerCell, bounds.height * worldUnitsPerCell);
        }

        public StageMapChunkOccupancy GetChunkOccupancy(Vector2Int chunk)
        {
            var cellCount = cells.Count(cell => CellToChunk(cell.X, cell.Y) == chunk);
            var objectIds = objects.Where(item => CellToChunk(item.X, item.Y) == chunk)
                .Select(item => item.StableId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (HasTerrainStructures)
            {
                var resolved = ResolveTerrain(ANIMOL.Gameplay.StageTerrainStructureRegistry.Load());
                cellCount += resolved.solids.Values.Count(c => !string.IsNullOrEmpty(c.ownerId) && CellToChunk(c.cell.x, c.cell.y) == chunk);
                objectIds = objectIds.Concat(terrainPlacements.placements.Where(p =>
                    Animol.TerrainStructure.AnimolTerrainPlacementEngine.GetCoveredChunks(resolved.entriesById[p.catalogId], new Vector2Int(p.x, p.y)).Contains(chunk))
                    .Select(p => p.instanceId)).ToArray();
            }
            return new StageMapChunkOccupancy(chunk, cellCount, objectIds);
        }

        public void EditorInitializeIdentity(string stableStageId, string stableThemeId)
        {
            if (!string.IsNullOrWhiteSpace(stageId) && stageId != stableStageId)
                throw new InvalidOperationException("A map stable stage ID cannot be replaced.");
            stageId = stableStageId ?? string.Empty;
            themeId = stableThemeId ?? string.Empty;
            InvalidateHumanReview();
        }

        public void EditorSetCell(int x, int y, StageMapLayer layer, string tileId, string variantId = "")
        {
            if (layer == StageMapLayer.Terrain && HasTerrainStructures)
            { EditStructuredBaseCell(x, y, tileId, variantId, string.IsNullOrWhiteSpace(tileId)); return; }
            EnsureInitialChunkContains(x, y);
            if (!ContainsCell(x, y)) throw new InvalidOperationException($"Cell ({x},{y}) is outside the authored chunk bounds {ChunkBounds}.");
            EditorEraseCell(x, y, layer);
            if (!string.IsNullOrWhiteSpace(tileId)) cells.Add(new StageMapCell(x, y, layer, tileId, variantId));
            NotifyAuthoredChange();
        }

        public bool EditorEraseCell(int x, int y, StageMapLayer layer)
        {
            if (layer == StageMapLayer.Terrain && HasTerrainStructures) return EditStructuredBaseCell(x, y, null, null, true);
            var removed = cells.RemoveAll(cell => cell.X == x && cell.Y == y && cell.Layer == layer) > 0;
            if (removed) NotifyAuthoredChange();
            return removed;
        }

        public void EditorPlaceObject(string stableId, StageMapObjectKind kind, int x, int y, string dataKey = "")
        {
            EditorPlaceObject(stableId, kind, x, y, dataKey, null, null);
        }

        public void EditorPlaceObject(string stableId, StageMapObjectKind kind, int x, int y, string dataKey,
            GameObject prefab, StageMapObjectSettings settings)
        {
            if (string.IsNullOrWhiteSpace(stableId)) throw new ArgumentException("Object stable ID is required.", nameof(stableId));
            if (HasTerrainStructures)
            {
                var resolved = ResolveTerrain(ANIMOL.Gameplay.StageTerrainStructureRegistry.Load());
                var candidate = new StageMapObjectPlacement(stableId, kind, x, y, dataKey, prefab, settings);
                if (EnumeratePlacementCells(candidate).Any(c => resolved.solids.TryGetValue(c, out var solid) && !string.IsNullOrEmpty(solid.ownerId)))
                    throw new InvalidOperationException("Object footprint/path overlaps a structure-owned # cell.");
            }
            EnsureInitialChunkContains(x, y);
            if (!ContainsCell(x, y)) throw new InvalidOperationException($"Object {stableId} at ({x},{y}) is outside the authored chunk bounds {ChunkBounds}.");
            objects.RemoveAll(item => item.StableId == stableId);
            objects.Add(new StageMapObjectPlacement(stableId, kind, x, y, dataKey, prefab, settings));
            NotifyAuthoredChange();
        }

        public bool EditorRemoveObject(string stableId)
        {
            var removed = objects.RemoveAll(item => item.StableId == stableId) > 0;
            if (removed) NotifyAuthoredChange();
            return removed;
        }

        public void EditorInitializeVariableChunksFromAuthoredContent(float unitsPerCell)
        {
            if (unitsPerCell <= 0f) throw new ArgumentOutOfRangeException(nameof(unitsPerCell));
            var positions = cells.Select(cell => new Vector2Int(cell.X, cell.Y))
                .Concat(objects.SelectMany(EnumeratePlacementCells))
                .Concat(Animol.TerrainStructure.FreeShapeTopology.Index(freeShapeTerrain).Keys).ToArray();
            if (positions.Length == 0)
            {
                minChunkX = 0; minChunkY = 0; chunkWidth = 1; chunkHeight = 1;
            }
            else
            {
                var chunks = positions.Select(position => CellToChunk(position.x, position.y)).ToArray();
                minChunkX = chunks.Min(chunk => chunk.x);
                minChunkY = chunks.Min(chunk => chunk.y);
                chunkWidth = chunks.Max(chunk => chunk.x) - minChunkX + 1;
                chunkHeight = chunks.Max(chunk => chunk.y) - minChunkY + 1;
            }
            worldUnitsPerCell = unitsPerCell;
            mapVersion = Math.Max(2, mapVersion);
            NotifyAuthoredChange();
            EditorMarkCollisionDataSynchronized();
        }

        public bool EditorTrySetChunkBounds(RectInt requested, bool deleteOutsideData, out StageMapChunkRemovalImpact impact)
        {
            impact = AnalyzeChunkBoundsChange(requested);
            if (HasFreeShape && freeShapeTerrain.cells.Any(c => !requested.Contains(CellToChunk(c.x,c.y))))
                throw new InvalidOperationException("자유형 지형을 이동/삭제한 뒤 청크를 축소하세요.");
            if (HasTerrainStructures)
            {
                var resolved = ResolveTerrain(ANIMOL.Gameplay.StageTerrainStructureRegistry.Load());
                foreach (var placement in terrainPlacements.placements)
                    if (Animol.TerrainStructure.AnimolTerrainPlacementEngine.GetCoveredChunks(resolved.entriesById[placement.catalogId],
                        new Vector2Int(placement.x, placement.y)).Any(chunk => !requested.Contains(chunk)))
                        throw new InvalidOperationException("Move/delete complete structure instances before removing their covered chunks.");
            }
            if (requested.width <= 0 || requested.height <= 0) return false;
            if (impact.HasOccupiedData && !deleteOutsideData) return false;
            if (deleteOutsideData)
            {
                cells.RemoveAll(cell => !CellInsideChunkBounds(cell.X, cell.Y, requested));
                objects.RemoveAll(item => EnumeratePlacementCells(item).Any(cell => !CellInsideChunkBounds(cell.x, cell.y, requested)));
            }
            minChunkX = requested.xMin;
            minChunkY = requested.yMin;
            chunkWidth = requested.width;
            chunkHeight = requested.height;
            NotifyAuthoredChange();
            return true;
        }

        public StageMapChunkRemovalImpact AnalyzeChunkBoundsChange(RectInt requested)
        {
            var removedCells = cells.Where(cell => !CellInsideChunkBounds(cell.X, cell.Y, requested)).
                Select(cell => $"{cell.Layer}@({cell.X},{cell.Y})={cell.TileId}").OrderBy(value => value, StringComparer.Ordinal).ToArray();
            removedCells=removedCells.Concat(Animol.TerrainStructure.FreeShapeTopology.Index(freeShapeTerrain).Keys.Where(p=>!requested.Contains(CellToChunk(p.x,p.y))).Select(p=>"FreeShape@"+p)).ToArray();
            var removedObjects = objects.Where(item => EnumeratePlacementCells(item).Any(cell => !CellInsideChunkBounds(cell.x, cell.y, requested))).
                Select(item => $"{item.StableId}:{item.Kind}@({item.X},{item.Y})").OrderBy(value => value, StringComparer.Ordinal).ToArray();
            return new StageMapChunkRemovalImpact(requested, removedCells, removedObjects);
        }

        public void EditorNotifyAuthoredPropertiesChanged()
        {
            NotifyAuthoredChange();
        }

        public void EditorMarkCollisionDataSynchronized()
        {
            collisionDataRevision = authoringRevision;
        }

        public void EditorMarkHumanCompletion(string note)
        {
            manuallyCompleted = true;
            reviewedMapVersion = mapVersion;
            reviewedContentHash = ContentHash;
            reviewerNote = note ?? string.Empty;
            reviewedBubbleSeed = -1;
            reviewedBubbleCombination = Array.Empty<string>();
            reviewedPlayEnvironment = string.Empty;
            reviewedElapsedSeconds = 0f;
        }

        public void EditorMarkHumanCompletion(string note, int bubbleSeed, IEnumerable<string> bubbleCombination,
            string playEnvironment, float elapsedSeconds)
        {
            var combination = (bubbleCombination ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (bubbleSeed < 0) throw new ArgumentOutOfRangeException(nameof(bubbleSeed));
            if (combination.Length != 3) throw new ArgumentException("Exactly three unique bubble IDs are required.", nameof(bubbleCombination));
            if (string.IsNullOrWhiteSpace(playEnvironment)) throw new ArgumentException("A real play environment is required.", nameof(playEnvironment));
            if (elapsedSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            var authoredCandidates = new HashSet<string>(objects.Where(item => item.Kind == StageMapObjectKind.BubbleCandidate)
                .Select(item => item.StableId), StringComparer.Ordinal);
            if (combination.Any(id => !authoredCandidates.Contains(id)))
                throw new ArgumentException("The completion combination must use authored bubble candidates.", nameof(bubbleCombination));

            manuallyCompleted = true;
            reviewedMapVersion = mapVersion;
            reviewedContentHash = ContentHash;
            reviewerNote = note ?? string.Empty;
            reviewedBubbleSeed = bubbleSeed;
            reviewedBubbleCombination = combination;
            reviewedPlayEnvironment = playEnvironment.Trim();
            reviewedElapsedSeconds = elapsedSeconds;
            humanCompletionEvidence ??= new List<StageMapHumanCompletionEvidence>();
            var key = string.Join("+", combination);
            humanCompletionEvidence.RemoveAll(item => item != null && item.MapVersion == mapVersion &&
                string.Equals(item.ContentHash, reviewedContentHash, StringComparison.Ordinal) && item.CombinationKey == key);
            humanCompletionEvidence.Add(new StageMapHumanCompletionEvidence(mapVersion, reviewedContentHash, bubbleSeed,
                combination, reviewedPlayEnvironment, elapsedSeconds, reviewerNote));
        }

        public void InvalidateHumanReview()
        {
            manuallyCompleted = false;
            reviewedContentHash = string.Empty;
            reviewedBubbleSeed = -1;
            reviewedBubbleCombination = Array.Empty<string>();
            reviewedPlayEnvironment = string.Empty;
            reviewedElapsedSeconds = 0f;
            humanCompletionEvidence?.Clear();
        }

        private HashSet<string> GetCurrentHumanCompletionKeys()
        {
            var hash = ContentHash;
            var authored = new HashSet<string>(objects.Where(item => item.Kind == StageMapObjectKind.BubbleCandidate)
                .Select(item => item.StableId), StringComparer.Ordinal);
            return new HashSet<string>((humanCompletionEvidence ?? new List<StageMapHumanCompletionEvidence>())
                .Where(item => item != null && item.IsValidFor(mapVersion, hash, authored)).Select(item => item.CombinationKey), StringComparer.Ordinal);
        }

        public float GetEditorPreviewUnitsPerCell() => worldUnitsPerCell > 0f ? worldUnitsPerCell : 1f;

        private string ComputeContentHash()
        {
            var builder = new StringBuilder();
            builder.Append(stageId).Append('|').Append(themeId).Append('|').Append(mapVersion).Append('|').Append(worldUnitsPerCell)
                .Append('|').Append(minChunkX).Append(':').Append(minChunkY).Append(':').Append(chunkWidth).Append(':').Append(chunkHeight)
                .Append('|').Append(mainTimeLimitSeconds).Append('|').Append(escapeTimeLimitSeconds).Append('|').Append(fastClearThresholdSeconds)
                .Append('|').Append(exitRevealCutsceneSeconds).Append('|').Append(timersAdvanceDuringExitReveal)
                .Append('|').Append(rewardDefinition == null ? string.Empty :
                    $"{rewardDefinition.Version}:{rewardDefinition.FirstClearBaseCoin}:{rewardDefinition.FastClearBonusConfigured}:{rewardDefinition.FastClearBonusCoin}:" +
                    $"{rewardDefinition.OptionalObjectiveBonusConfigured}:{rewardDefinition.OptionalObjectiveBonusMaximumCoin}:" +
                    $"{rewardDefinition.ReplayRewardConfigured}:{rewardDefinition.ReplayBaseCoin}")
                .Append('|').Append(initialAnimalId).Append('|').Append(string.Join(",", fixedAnimalIds ?? Array.Empty<string>()))
                .Append('|').Append(string.Join(",", optionalObjectiveIds ?? Array.Empty<string>()));
            foreach (var cell in cells.OrderBy(x => x.Layer).ThenBy(x => x.X).ThenBy(x => x.Y).ThenBy(x => x.TileId))
                builder.Append("|C:").Append((int)cell.Layer).Append(':').Append(cell.X).Append(':').Append(cell.Y).Append(':').Append(cell.TileId).Append(':').Append(cell.VariantId);
            if (HasTerrainStructures) builder.Append("|TerrainStructures:").Append(JsonUtility.ToJson(terrainPlacements));
            if (HasFreeShape) builder.Append("|FreeShapeTerrain:").Append(JsonUtility.ToJson(freeShapeTerrain));
            foreach (var item in objects.OrderBy(x => x.StableId, StringComparer.Ordinal))
            {
                var settings = item.Settings;
                builder.Append("|O:").Append(item.StableId).Append(':').Append((int)item.Kind).Append(':').Append(item.X).Append(':').Append(item.Y).Append(':').Append(item.DataKey)
                    .Append(':').Append(settings.Version).Append(':').Append((int)settings.Direction).Append(':').Append(settings.FootprintCells.x).Append('x').Append(settings.FootprintCells.y)
                    .Append(':').Append((int)settings.HalfPlacement).Append(':').Append((int)settings.SpringContactPolicy)
                    .Append(':').Append(settings.HorizontalImpulse).Append(':').Append(settings.VerticalImpulse).Append(':').Append(settings.MovementSpeed)
                    .Append(':').Append(settings.UpperPauseSeconds).Append(':').Append(settings.LowerPauseSeconds).Append(':').Append(settings.ActivationRangeCells)
                    .Append(':').Append(settings.GroundSpeedMultiplier).Append(':').Append(settings.EndStopSeconds).Append(':').Append(settings.ReturnWhenEmpty)
                    .Append(':').Append(item.Prefab == null ? string.Empty : item.Prefab.name);
                foreach (var node in settings.PathCells) builder.Append('@').Append(node.x).Append(',').Append(node.y);
            }
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()))).Replace("-", string.Empty).ToLowerInvariant();
        }

        private void EnsureInitialChunkContains(int x, int y)
        {
            if (HasValidChunkBounds) return;
            var chunk = CellToChunk(x, y);
            minChunkX = chunk.x; minChunkY = chunk.y; chunkWidth = 1; chunkHeight = 1;
        }

        private void NotifyAuthoredChange()
        {
            if (editorBatchDepth > 0) { editorBatchChanged = true; return; }
            authoringRevision = Math.Max(1, authoringRevision + 1);
            InvalidateHumanReview();
        }

        [NonSerialized] private int editorBatchDepth;
        [NonSerialized] private bool editorBatchChanged;
        public void EditorBatchAuthoredChanges(Action action)
        {
            editorBatchDepth++;
            bool completed = false;
            try { action(); completed = true; }
            finally
            {
                editorBatchDepth--;
                if (editorBatchDepth == 0)
                {
                    var changed = editorBatchChanged; editorBatchChanged = false;
                    if (completed && changed) NotifyAuthoredChange();
                }
            }
        }

        private static bool CellInsideChunkBounds(int x, int y, RectInt chunkBounds) => chunkBounds.width > 0 && chunkBounds.height > 0 &&
            chunkBounds.Contains(CellToChunk(x, y));

        public static IEnumerable<Vector2Int> EnumeratePlacementCells(StageMapObjectPlacement placement)
        {
            if (placement == null) yield break;
            var footprint = placement.Settings.FootprintCells;
            var width = Mathf.Max(1, Mathf.CeilToInt(footprint.x));
            var height = Mathf.Max(1, Mathf.CeilToInt(footprint.y));
            foreach (var origin in EnumeratePathOrigins(placement))
            for (var dx = 0; dx < width; dx++)
            for (var dy = 0; dy < height; dy++)
                yield return origin + new Vector2Int(dx, dy);
        }

        private static IEnumerable<Vector2Int> EnumeratePathOrigins(StageMapObjectPlacement placement)
        {
            var nodes = placement.Settings.PathCells;
            if (nodes.Count == 0) { yield return new Vector2Int(placement.X, placement.Y); yield break; }
            yield return nodes[0];
            for (var index = 1; index < nodes.Count; index++)
            {
                var from = nodes[index - 1]; var to = nodes[index];
                var x = from.x; var y = from.y;
                var dx = Mathf.Abs(to.x - from.x); var sx = from.x < to.x ? 1 : -1;
                var dy = -Mathf.Abs(to.y - from.y); var sy = from.y < to.y ? 1 : -1;
                var error = dx + dy;
                while (x != to.x || y != to.y)
                {
                    var twice = error * 2;
                    if (twice >= dy) { error += dy; x += sx; }
                    if (twice <= dx) { error += dx; y += sy; }
                    yield return new Vector2Int(x, y);
                }
            }
        }

        private static int FloorDiv(int value, int divisor)
        {
            var quotient = value / divisor;
            var remainder = value % divisor;
            return remainder < 0 ? quotient - 1 : quotient;
        }
    }

    public sealed class StageMapChunkOccupancy
    {
        public Vector2Int Chunk { get; }
        public int CellCount { get; }
        public IReadOnlyList<string> ObjectIds { get; }
        public int ObjectCount => ObjectIds.Count;
        public bool IsEmpty => CellCount == 0 && ObjectCount == 0;
        public StageMapChunkOccupancy(Vector2Int chunk, int cellCount, IReadOnlyList<string> objectIds)
        {
            Chunk = chunk;
            CellCount = cellCount;
            ObjectIds = objectIds ?? Array.Empty<string>();
        }
    }

    public sealed class StageMapChunkRemovalImpact
    {
        public RectInt RequestedBounds { get; }
        public IReadOnlyList<string> RemovedCells { get; }
        public IReadOnlyList<string> RemovedObjects { get; }
        public bool HasOccupiedData => RemovedCells.Count > 0 || RemovedObjects.Count > 0;
        public StageMapChunkRemovalImpact(RectInt requestedBounds, IReadOnlyList<string> removedCells, IReadOnlyList<string> removedObjects)
        {
            RequestedBounds = requestedBounds;
            RemovedCells = removedCells ?? Array.Empty<string>();
            RemovedObjects = removedObjects ?? Array.Empty<string>();
        }
    }

    public sealed class StageMapValidationReport
    {
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    public static class StageMapValidator
    {
        public static StageMapValidationReport ValidateStructure(StageMapDefinition map)
        {
            var report = new StageMapValidationReport();
            if (map == null) { report.Errors.Add("Map asset is missing."); return report; }
            if (string.IsNullOrWhiteSpace(map.StageId) || string.IsNullOrWhiteSpace(map.ThemeId)) report.Errors.Add("Stable stage/theme identity is missing.");
            if (map.MapVersion < 1) report.Errors.Add("Map version must be positive.");
            if (!map.HasValidChunkBounds) report.Errors.Add("Chunk bounds must have positive width and height.");
            if (map.WorldUnitsPerCell <= 0f) report.Errors.Add("ANIMOL product world cell size is unconfigured.");
            if (map.Cells.Any(cell => !map.ContainsCell(cell.X, cell.Y))) report.Errors.Add("One or more authored cells are outside the declared chunk bounds.");
            if (map.Objects.SelectMany(StageMapDefinition.EnumeratePlacementCells).Any(cell => !map.ContainsCell(cell.x, cell.y)))
                report.Errors.Add("One or more map object footprints/paths are outside the declared chunk bounds.");
            if (map.Cells.GroupBy(cell => (cell.X, cell.Y, cell.Layer)).Any(group => group.Count() > 1)) report.Errors.Add("Duplicate map cell/layer entries exist.");
            if (map.Objects.Any(item => string.IsNullOrWhiteSpace(item.StableId)) || map.Objects.GroupBy(item => item.StableId, StringComparer.Ordinal).Any(group => group.Count() > 1))
                report.Errors.Add("Map object stable IDs are missing or duplicated.");
            if (map.Objects.Any(item => item.Settings.Version < 1 || item.Settings.FootprintCells.x <= 0f || item.Settings.FootprintCells.y <= 0f))
                report.Errors.Add("Map object settings version/footprint is invalid.");
            var objectIds = new HashSet<string>(map.Objects.Select(item => item.StableId), StringComparer.Ordinal);
            if (map.Objects.Any(item => item.Settings.LinkedInstanceIds.Any(linked => !objectIds.Contains(linked))))
                report.Errors.Add("One or more map object links are dangling.");
            if (map.Objects.Any(item => item.Settings.LinkedInstanceIds.Count < item.Settings.MinimumLinkCount))
                report.Errors.Add("One or more map objects are missing required stable-ID links.");
            if (map.Objects.Any(item => item.Settings.ImplementationLevel == MapObjectImplementationLevel.PlaceablePrototype &&
                                        item.Settings.RouteRole == MapObjectRouteRole.Required))
                report.Errors.Add("PlaceablePrototype objects cannot be assigned to a required route or required reward access path.");
            if (map.HasTerrainStructures || map.CollisionDataRevision == -1)
            {
                try { map.ResolveTerrain(ANIMOL.Gameplay.StageTerrainStructureRegistry.Load()); }
                catch (Exception ex) { report.Errors.Add(ex.Message); }
            }
            else if (map.CollisionDataRevision != map.AuthoringRevision) report.Errors.Add("Derived collision data revision is stale.");
            return report;
        }

        public static StageMapValidationReport ValidateForOperation(StageMapDefinition map)
        {
            var report = ValidateStructure(map);
            if (map == null) return report;
            if (map.FixedAnimalIds == null || map.FixedAnimalIds.Count == 0 || !map.FixedAnimalIds.Contains(map.InitialAnimalId)) report.Errors.Add("Fixed/initial animal data is incomplete.");
            if (map.MainTimeLimitSeconds <= 0f || map.EscapeTimeLimitSeconds <= 0f || map.FastClearThresholdSeconds < 0f) report.Errors.Add("Main/escape timing is unconfigured or fast-clear timing is invalid.");
            if (map.RewardDefinition == null || !map.RewardDefinition.IsValid) report.Errors.Add("Versioned stage reward data is missing or invalid.");
            else
            {
                if (!map.RewardDefinition.FastClearBonusConfigured) report.Errors.Add("Fast-clear reward policy is not approved.");
                if (!map.RewardDefinition.ReplayRewardConfigured) report.Errors.Add("Replay reward policy is not approved.");
            }
            if (map.Objects.Count(x => x.Kind == StageMapObjectKind.PlayerStart) != 1) report.Errors.Add("Exactly one player start is required.");
            if (map.Objects.Count(x => x.Kind == StageMapObjectKind.Checkpoint) < 1) report.Errors.Add("At least one checkpoint is required.");
            if (map.Objects.Count(x => x.Kind == StageMapObjectKind.BubbleCandidate) < 3) report.Errors.Add("At least three bubble candidates are required.");
            if (map.Objects.Count(x => x.Kind == StageMapObjectKind.Exit) != 1) report.Errors.Add("Exactly one exit is required.");
            if (map.Objects.Any(item => item.Settings.ImplementationLevel == MapObjectImplementationLevel.PlaceablePrototype))
                report.Errors.Add("Operational Ready is blocked while PlaceablePrototype map objects are present.");
            if (!map.HasCurrentHumanCompletionReview) report.Errors.Add("Human completion review for this exact version/hash is missing.");
            return report;
        }
    }
}
