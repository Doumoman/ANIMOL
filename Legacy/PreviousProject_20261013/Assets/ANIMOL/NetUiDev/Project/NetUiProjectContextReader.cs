#if ANIMOL_NET_UI_DEV
using System;
using System.Linq;
using System.Threading.Tasks;
using ANIMOL.AnimalMultiplayerPhase3;
using ANIMOL.AnimalUiV2;
using UnityEngine;

namespace Animol.NetUiDev.Project
{
    // Read-only port: admission/Ready cannot accidentally be called by a context lookup.
    public interface INetUiContextSource
    {
        bool BlocksNewEntry { get; }
        Task<NetUiCatalog> GetCatalogAsync();
        Task<NetUiLookupResponse> LookupRoomAsync(string code);
        Task<NetUiContextSnapshot> ReadContextAsync(string mode, string intent, string code);
    }

    public sealed class NetUiCoordinatorContextSource : INetUiContextSource
    {
        private readonly NetUiDevCoordinator service;
        public NetUiCoordinatorContextSource(NetUiDevCoordinator service) { this.service = service; }
        public bool BlocksNewEntry => service.BlocksNewEntry;
        public Task<NetUiCatalog> GetCatalogAsync() => service.GetCatalogAsync();
        public Task<NetUiLookupResponse> LookupRoomAsync(string code) => service.LookupRoomAsync(code);
        public Task<NetUiContextSnapshot> ReadContextAsync(string mode, string intent, string code) => service.ReadContextAsync(mode, intent, code);
    }

    public sealed class NetUiBoundContext
    {
        public MultiplayerSelectionRequest Request;
        public AnimalUiSnapshot Snapshot;
        public NetUiLookupResponse Lookup;
        public string GrowthPolicy;
        public string RoomCode;
        public string Route;
    }

    public sealed class NetUiProjectContextReader
    {
        private readonly INetUiContextSource source;
        private readonly NetUiProjectSettings settings;
        private readonly AnimalCatalog artCatalog;
        private readonly NetUiIdMapEntry[] map;
        private bool reading;
        public NetUiProjectContextReader(INetUiContextSource source, NetUiProjectSettings settings, AnimalCatalog artCatalog, NetUiIdMapEntry[] map)
        {
            this.source = source;
            this.settings = JsonUtility.FromJson<NetUiProjectSettings>(JsonUtility.ToJson(settings));
            this.artCatalog = artCatalog;
            this.map = map.Select(e => new NetUiIdMapEntry { AnimalId = e.AnimalId, StableArtId = e.StableArtId, Role = e.Role }).ToArray();
        }

        public static NetUiIdMapEntry[] BuildProjectMap(AnimalCatalog catalog, AnimalMultiplayerIdMap idMap)
        {
            NetUiValidation.Require(catalog != null && idMap != null && idMap.Entries != null, "PROJECT_ID_MAP_MISSING");
            return idMap.Entries.Where(e => e?.CampaignAnimal != null).Select(e =>
            {
                var art = catalog.Find(e.ArtId);
                NetUiValidation.Require(!string.IsNullOrWhiteSpace(e.CampaignAnimal.AnimalId) &&
                    !e.CampaignAnimal.AnimalId.StartsWith("TEST_", StringComparison.Ordinal) &&
                    art != null && idMap.ToArtId(e.CampaignAnimal.AnimalId) == e.ArtId &&
                    idMap.ToProjectAnimal(e.ArtId) == e.CampaignAnimal, "PROJECT_ID_MAP_AMBIGUOUS");
                return new NetUiIdMapEntry { AnimalId = e.CampaignAnimal.AnimalId, StableArtId = e.ArtId, Role = Role(art.Role) };
            }).ToArray();
        }

        public async Task<NetUiBoundContext> ReadAsync(string route, string inputCode = "")
        {
            NetUiValidation.Require(!reading && !source.BlocksNewEntry, "CURRENT_ACTION_OR_CONTEXT_IS_BUSY");
            reading = true;
            try
            {
                var catalog = await source.GetCatalogAsync();
                NetUiValidation.ValidateCatalog(catalog);
                NetUiLookupResponse lookup = null;
                string modeId, kind;
                switch (route)
                {
                    case "Normal": modeId = settings.NormalModeId; kind = "public"; break;
                    case "Ranked": modeId = settings.RankedModeId; kind = "public"; break;
                    case "Create": modeId = settings.CustomModeId; kind = "create"; break;
                    case "Join":
                        lookup = await source.LookupRoomAsync(inputCode);
                        NetUiValidation.Require(lookup != null && !string.IsNullOrWhiteSpace(lookup.RoomId) &&
                            !string.IsNullOrWhiteSpace(lookup.RoomCode) && lookup.Phase == "Waiting" && lookup.Capacity == 4 &&
                            lookup.ParticipantCount >= 1 && lookup.ParticipantCount < 4, "ROOM_LOOKUP_UNAVAILABLE_OR_FULL");
                        modeId = lookup.ModeId; kind = "join"; break;
                    default: throw new InvalidOperationException("ENTRY_ROUTE_INVALID");
                }
                var policy = settings.Mode(modeId);
                string intent = kind == "public" ? policy.PublicIntent : kind == "create" ? policy.CreateIntent : policy.JoinIntent;
                var mode = NetUiValidation.FindMode(catalog, modeId);
                NetUiValidation.Require(mode != null && !string.IsNullOrWhiteSpace(mode.DisplayName) &&
                    mode.EntryIntents.Any(i => i.IntentId == intent && i.Kind == kind), "MODE_OR_ENTRY_INTENT_UNCONFIGURED");
                NetUiValidation.Require(lookup == null || lookup.PolicyRevision == mode.PolicyRevision, "LOOKUP_POLICY_STALE");
                NetUiValidation.Require(!source.BlocksNewEntry, "CURRENT_ACTION_OR_CONTEXT_IS_BUSY");
                string code = lookup?.RoomCode ?? "";
                var reply = await source.ReadContextAsync(modeId, intent, code);
                NetUiValidation.Require(!source.BlocksNewEntry, "CURRENT_ACTION_OR_CONTEXT_IS_BUSY");
                NetUiValidation.ValidateContext(reply, new NetUiContextRequest { ModeId = modeId, EntryIntent = intent, RoomCode = code }, catalog);
                NetUiValidation.Require(reply.Context.RepresentativeRole == mode.RepresentativeRole, "REPRESENTATIVE_ROLE_MISMATCH");
                var context = reply.Context;
                var request = new MultiplayerSelectionRequest
                {
                    ModeId = modeId, DisplayName = mode.DisplayName, EntryIntent = intent,
                    Requirements = new SelectionRequirements { RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air },
                        RepresentativeRole = ParseRole(context.RepresentativeRole), PolicyRevision = context.PolicyRevision },
                    InitialLoadout = ToArt(context.InitialLoadout, catalog),
                    AllowedAnimalIds = AllowedArtIds(context.AllowedAnimalIds, mode.AllowedAnimalIds, catalog)
                };
                var snapshot = new AnimalUiSnapshot { Revision = reply.Snapshot.Revision, ContextId = context.ContextId,
                    EntryIntent = context.EntryIntent, PolicyRevision = context.PolicyRevision };
                foreach (var permission in reply.Snapshot.Animals)
                {
                    var mapping = map.SingleOrDefault(m => m.AnimalId == permission.AnimalId);
                    if (mapping == null) continue; // Unmapped art stays unavailable; never invent a species.
                    string art = NetUiValidation.ValidateMapAnimal(permission.AnimalId, mapping.Role, catalog, map);
                    snapshot.Animals.Add(new AnimalProgress { AnimalId = art,
                        Implemented = permission.Implemented && NetUiValidation.FindAnimal(catalog, permission.AnimalId).Implemented,
                        Unlocked = permission.Unlocked, HasContextPermission = permission.HasContextPermission,
                        CanUseInContext = permission.CanUseInContext }); // No levels, mastery, effects or balances are fabricated.
                }
                NetUiValidation.Require(AnimalUiRules.CanConfirmMultiplayer(artCatalog, snapshot, request.InitialLoadout, request, true, true, out var reason),
                    "PROJECT_LOADOUT_OR_PERMISSION_UNCONFIGURED");
                return new NetUiBoundContext { Request = request, Snapshot = snapshot, Lookup = lookup, GrowthPolicy = policy.GrowthPolicy, RoomCode = code, Route = route };
            }
            finally { reading = false; }
        }

        private AnimalLoadout ToArt(NetUiLoadout actual, NetUiCatalog catalog)
        {
            var art = NetUiValidation.ToArt(actual, catalog, map);
            return new AnimalLoadout { Ground = art.Ground, Special = art.Special, Air = art.Air };
        }
        private string[] AllowedArtIds(string[] context, string[] mode, NetUiCatalog catalog)
        {
            if (context == null && mode == null) return null;
            var ids = context ?? mode;
            NetUiValidation.Require(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length &&
                ids.All(id => NetUiValidation.FindAnimal(catalog, id) != null), "ALLOWED_POOL_INVALID");
            if (context != null && mode != null)
                NetUiValidation.Require(context.All(id => mode.Contains(id)), "CONTEXT_POOL_EXCEEDS_MODE");
            return map.Where(m => ids.Contains(m.AnimalId)).Select(m => NetUiValidation.ValidateMapAnimal(m.AnimalId, m.Role, catalog, map)).ToArray();
        }
        public static string Role(AnimalRole role)
        {
            switch (role) { case AnimalRole.Ground: return "Ground"; case AnimalRole.Special: return "Special"; case AnimalRole.Air: return "Air";
                default: throw new InvalidOperationException("PROJECT_ROLE_INVALID"); }
        }
        public static AnimalRole ParseRole(string role)
        {
            switch (role) { case "Ground": return AnimalRole.Ground; case "Special": return AnimalRole.Special; case "Air": return AnimalRole.Air;
                default: throw new InvalidOperationException("PROJECT_ROLE_INVALID"); }
        }
    }
}
#endif
