using System;
using System.Collections.Generic;

namespace Animol.NetUiDev
{
    public static class NetUiValidation
    {
        public static string NormalizeServerUrl(string value, bool mobile)
        {
            value = (value ?? "").Trim();
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https") ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new ArgumentException("Use an absolute http(s) server URL without credentials, query, or fragment.");
            if (mobile && (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("A phone must use the PC LAN address or the server address, not loopback.");
            System.Net.IPAddress address;
            if (System.Net.IPAddress.TryParse(uri.DnsSafeHost, out address) &&
                (address.Equals(System.Net.IPAddress.Any) || address.Equals(System.Net.IPAddress.IPv6Any) || address.Equals(System.Net.IPAddress.Broadcast)))
                throw new ArgumentException("Use a reachable server address, not an unspecified or broadcast address.");
            return value.TrimEnd('/');
        }

        public static void Require(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException(reason); }

        public static bool SameLoadout(NetUiLoadout a, NetUiLoadout b)
        { return a != null && b != null && a.Ground == b.Ground && a.Special == b.Special && a.Air == b.Air; }

        public static void ValidateCatalog(NetUiCatalog catalog)
        {
            Require(catalog != null && catalog.Animals != null && catalog.Modes != null, "CATALOG_MISSING");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var arts = new HashSet<string>(StringComparer.Ordinal);
            foreach (var animal in catalog.Animals)
            {
                Require(animal != null && !string.IsNullOrWhiteSpace(animal.AnimalId) && !string.IsNullOrWhiteSpace(animal.StableArtId), "ANIMAL_ID_MISSING");
                Require(IsRole(animal.Role), "ANIMAL_ROLE_INVALID");
                Require(ids.Add(animal.AnimalId) && arts.Add(animal.StableArtId), "ANIMAL_ID_AMBIGUOUS");
            }
            var modes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mode in catalog.Modes)
            {
                Require(mode != null && !string.IsNullOrWhiteSpace(mode.ModeId) && modes.Add(mode.ModeId), "MODE_ID_INVALID");
                Require(!string.IsNullOrWhiteSpace(mode.PolicyRevision) && mode.EntryIntents != null && IsRole(mode.RepresentativeRole), "MODE_POLICY_MISSING");
                var intents = new HashSet<string>(StringComparer.Ordinal);
                foreach (var intent in mode.EntryIntents)
                    Require(intent != null && !string.IsNullOrWhiteSpace(intent.IntentId) && intents.Add(intent.IntentId) &&
                        (intent.Kind == "create" || intent.Kind == "join" || intent.Kind == "public"), "ENTRY_INTENT_INVALID");
            }
            Require(catalog.CodeFormat != null && catalog.CodeFormat.Length > 0 && !string.IsNullOrWhiteSpace(catalog.CodeFormat.Alphabet), "CODE_FORMAT_MISSING");
        }

        public static void ValidateContext(NetUiContextSnapshot reply, NetUiContextRequest request, NetUiCatalog catalog)
        {
            Require(reply != null && reply.Context != null && reply.Snapshot != null && reply.Snapshot.Animals != null, "CONTEXT_MISSING");
            var context = reply.Context;
            var mode = FindMode(catalog, request.ModeId);
            Require(mode != null && context.ContextId == request.ModeId && context.ModeId == request.ModeId &&
                context.EntryIntent == request.EntryIntent && context.PolicyRevision == mode.PolicyRevision &&
                context.RoomCode == request.RoomCode, "CONTEXT_MISMATCH");
            Require(!string.IsNullOrWhiteSpace(reply.Snapshot.Revision) && IsRole(context.RepresentativeRole), "SNAPSHOT_MISSING");
            bool foundIntent = false;
            foreach (var intent in mode.EntryIntents) if (intent.IntentId == request.EntryIntent) foundIntent = true;
            Require(foundIntent, "ENTRY_INTENT_NOT_IN_CATALOG");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var permission in reply.Snapshot.Animals)
                Require(permission != null && ids.Add(permission.AnimalId) && FindAnimal(catalog, permission.AnimalId) != null, "PERMISSION_ID_INVALID");
        }

        public static void ValidateEntry(NetUiEntryRequest request, NetUiContextSnapshot current, NetUiCatalog catalog, NetUiIdMapEntry[] map)
        {
            Require(request != null && current != null && current.Context != null && current.Snapshot != null, "ENTRY_CONTEXT_MISSING");
            Require(!string.IsNullOrWhiteSpace(request.ActionId) && request.ActionId.Length <= 128, "ACTION_ID_MISSING_OR_TOO_LONG");
            var context = current.Context;
            Require(request.ContextId == context.ContextId && request.EntryIntent == context.EntryIntent &&
                request.PolicyRevision == context.PolicyRevision && request.SnapshotRevision == current.Snapshot.Revision &&
                request.RoomCode == context.RoomCode, "ENTRY_CONTEXT_CHANGED");
            Require(request.Loadout != null, "LOADOUT_MISSING");
            ValidateSelected(request.Loadout.Ground, "Ground", current, catalog, map);
            ValidateSelected(request.Loadout.Special, "Special", current, catalog, map);
            ValidateSelected(request.Loadout.Air, "Air", current, catalog, map);
            var options = request.Options ?? new NetUiOptionValue[0];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var mode = FindMode(catalog, request.ContextId);
            foreach (var option in options)
            {
                Require(option != null && !string.IsNullOrWhiteSpace(option.OptionId) && seen.Add(option.OptionId), "OPTION_INVALID");
                bool allowed = false;
                if (mode.Options != null) foreach (var definition in mode.Options)
                    if (definition.OptionId == option.OptionId && Contains(definition.AllowedValues, option.Value)) allowed = true;
                Require(allowed, "OPTION_NOT_ALLOWED");
            }
        }

        private static void ValidateSelected(string id, string role, NetUiContextSnapshot current, NetUiCatalog catalog, NetUiIdMapEntry[] map)
        {
            var animal = FindAnimal(catalog, id);
            Require(animal != null && animal.Role == role && animal.Implemented, "SELECTED_ANIMAL_NOT_IMPLEMENTED");
            ValidateMapAnimal(id, role, catalog, map);
            Require(current.Context.AllowedAnimalIds == null || Contains(current.Context.AllowedAnimalIds, id), "SELECTED_ANIMAL_NOT_ALLOWED");
            var mode = FindMode(catalog, current.Context.ModeId);
            Require(mode.AllowedAnimalIds == null || Contains(mode.AllowedAnimalIds, id), "SELECTED_ANIMAL_NOT_IN_MODE");
            bool permitted = false;
            foreach (var permission in current.Snapshot.Animals)
                if (permission.AnimalId == id) permitted = permission.Implemented && permission.HasContextPermission && permission.CanUseInContext;
            Require(permitted, "SELECTED_ANIMAL_PERMISSION_MISSING");
            // Unlocked alone neither grants nor denies context permission.
        }

        public static void ValidateReceipt(NetUiEntryRequest request, NetUiEntryResult result)
        {
            Require(result != null && result.ActionId == request.ActionId, "RESULT_ACTION_MISMATCH");
            Require(result.Status == "Accepted", "RESULT_NOT_ACCEPTED");
            Require(!string.IsNullOrWhiteSpace(result.AcceptanceToken) && !string.IsNullOrWhiteSpace(result.RoomId) && !string.IsNullOrWhiteSpace(result.RoomCode), "RECEIPT_MISSING");
            Require(result.SnapshotRevision == request.SnapshotRevision && result.AcceptedContextId == request.ContextId &&
                result.AcceptedPolicyRevision == request.PolicyRevision && result.AcceptedEntryIntent == request.EntryIntent &&
                SameLoadout(request.Loadout, result.AcceptedLoadout), "RECEIPT_MISMATCH");
            Require(string.IsNullOrEmpty(request.RoomCode) || request.RoomCode == result.RoomCode, "RECEIPT_ROOM_MISMATCH");
        }

        public static string ValidateMapAnimal(string actualId, string role, NetUiCatalog catalog, NetUiIdMapEntry[] map)
        {
            var animal = FindAnimal(catalog, actualId);
            Require(animal != null && animal.Role == role && map != null, "PROJECT_ID_MAP_MISSING");
            string artId = null;
            int actualMatches = 0;
            foreach (var entry in map)
                if (entry != null && entry.AnimalId == actualId) { actualMatches++; artId = entry.StableArtId; Require(entry.Role == role, "PROJECT_ID_MAP_ROLE_MISMATCH"); }
            Require(actualMatches == 1 && artId == animal.StableArtId, "PROJECT_ID_MAP_NOT_EXPLICIT");
            int artMatches = 0;
            foreach (var entry in map) if (entry != null && entry.StableArtId == artId) artMatches++;
            Require(artMatches == 1, "PROJECT_ART_MAP_AMBIGUOUS");
            return artId;
        }

        public static NetUiLoadout ToActual(NetUiArtLoadout art, NetUiCatalog catalog, NetUiIdMapEntry[] map)
        {
            Require(art != null, "ART_LOADOUT_MISSING");
            return new NetUiLoadout { Ground = ToActualId(art.Ground, "Ground", catalog, map), Special = ToActualId(art.Special, "Special", catalog, map), Air = ToActualId(art.Air, "Air", catalog, map) };
        }

        private static string ToActualId(string art, string role, NetUiCatalog catalog, NetUiIdMapEntry[] map)
        {
            Require(map != null, "PROJECT_ID_MAP_MISSING");
            string actual = null;
            int count = 0;
            foreach (var entry in map) if (entry != null && entry.StableArtId == art && entry.Role == role) { actual = entry.AnimalId; count++; }
            Require(count == 1, "PROJECT_ART_MAP_MISSING");
            Require(ValidateMapAnimal(actual, role, catalog, map) == art, "PROJECT_ART_MAP_MISMATCH");
            return actual;
        }

        public static NetUiArtLoadout ToArt(NetUiLoadout actual, NetUiCatalog catalog, NetUiIdMapEntry[] map)
        {
            Require(actual != null, "ACTUAL_LOADOUT_MISSING");
            return new NetUiArtLoadout { Ground = ValidateMapAnimal(actual.Ground, "Ground", catalog, map), Special = ValidateMapAnimal(actual.Special, "Special", catalog, map), Air = ValidateMapAnimal(actual.Air, "Air", catalog, map) };
        }

        public static void ValidateRoom(NetUiRoomState state, NetUiCatalog catalog, NetUiIdMapEntry[] map)
        {
            Require(state != null && !string.IsNullOrWhiteSpace(state.RoomId) && state.Capacity == 4 && state.Participants != null && state.Participants.Length <= 4, "ROOM_STATE_INVALID");
            var accounts = new HashSet<string>(StringComparer.Ordinal);
            int owners = 0;
            foreach (var participant in state.Participants)
            {
                Require(participant != null && !string.IsNullOrWhiteSpace(participant.AccountId) && accounts.Add(participant.AccountId), "ROOM_PARTICIPANT_INVALID");
                ToArt(participant.Loadout, catalog, map);
                if (participant.IsOwner) { owners++; Require(participant.AccountId == state.OwnerAccountId, "ROOM_OWNER_MISMATCH"); }
            }
            Require(owners == 1, "ROOM_OWNER_MISSING");
        }

        public static bool Contains(string[] values, string value)
        { if (values == null) return false; foreach (var candidate in values) if (candidate == value) return true; return false; }
        public static bool IsRole(string role) { return role == "Ground" || role == "Special" || role == "Air"; }
        public static NetUiAnimalDefinition FindAnimal(NetUiCatalog catalog, string id)
        { if (catalog != null && catalog.Animals != null) foreach (var animal in catalog.Animals) if (animal.AnimalId == id) return animal; return null; }
        public static NetUiModeDefinition FindMode(NetUiCatalog catalog, string id)
        { if (catalog != null && catalog.Modes != null) foreach (var mode in catalog.Modes) if (mode.ModeId == id) return mode; return null; }
    }
}
