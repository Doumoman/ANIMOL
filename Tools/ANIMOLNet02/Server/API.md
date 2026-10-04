# ANIMOL NET02 DEV lobby API

This Python standard-library service implements a **DEV HTTP lobby**, not Google authentication, account progress, purchases, matchmaking infrastructure, an authoritative gameplay simulation, or a deployed cloud service. It supplies no production animal/mode IDs. The default configuration is empty and denies sessions and admission. ExternalServiceConfiguration flags stay false.

## Run and test

Python 3.10+ is sufficient; there is no pip dependency. From the package root:

```bash
python3 Server/server.py --bind 127.0.0.1 --port 8080 --config Server/config.empty.json --database /tmp/animol-private-dev.sqlite3
python3 -m unittest discover -s ServerTests -v
```

Windows PowerShell:

```powershell
.\Server\run-server.ps1 -BindAddress 127.0.0.1 -Port 8080 -ConfigPath .\Server\config.empty.json
python -m unittest discover -s ServerTests -v
```

For a deliberately isolated protocol smoke test, use `--config ServerTests/fixture_config.json` and a separate disposable database. The eight fixture accounts are `TEST_ACCOUNT_1` through `TEST_ACCOUNT_8` with matching `TEST_KEY_1` through `TEST_KEY_8`; these are public fixture values. Its animals, art IDs, mode, options, intents, and policy all use TEST prefixes. **They cannot be installed into the production Phase3 IdMap or used to claim the 15 species are implemented.** Actual HTTP unit tests use temporary copies/databases and an ephemeral localhost port.

LAN phone tests require explicit `--bind 0.0.0.0` and the PC's private IPv4 address, for example `http://192.168.1.10:8080`. Restrict the OS TCP firewall to the private test network. This plaintext DEV service must not be exposed publicly. The shell wrapper accepts the same flags; environment defaults are documented in `server.env.example`. PowerShell passes explicit parameters rather than loading that env file.

## Configuration and trust

`config.empty.json` is the safe starting config. `config.schema.example.json` describes fields and is **not** a runtime config. Populate runtime configuration only with verified actual IDs and deliberate DEV account grants. No UI art implies Implemented, unlocked status, or permission. `Implemented:false`, no grant, or an empty allowed pool blocks entry. A deliberately granted trial animal may have `Unlocked:false` and `CanUseInContext:true`; unlocked status alone never grants permission.

| Object | Required fields |
|---|---|
| Root | SchemaVersion=1, SessionNamespace=`ANIMOL_NET02_DEV`, Animals, Modes, Accounts |
| Animal | AnimalId (actual opaque ID), StableArtId (explicit reverse mapping), Role=Ground/Special/Air, Implemented (explicit boolean) |
| Mode | ModeId, DisplayName, PolicyRevision (opaque string), AllowedAnimalIds (explicit array), RepresentativeRole, EntryIntents, Options |
| Entry intent | IntentId (opaque configured ID), Kind=create/join/public (server capability classification) |
| Option | OptionId, AllowedValues (strings), DefaultValue; no hardcoded name, password, visibility, or player-count option |
| Account | AccountId, DevAccessKey, UnlockedAnimalIds, InitialLoadout, ModePermissions |
| Permission | ModeId, IntentIds (explicit array), AllowedAnimalIds (explicit granted array) |
| Loadout | Ground, Special, Air, exactly one role-correct actual ID each |

Optional PublicStartCount is always 4. CustomStartMinimum defaults to 2 and may be 2–4 in explicit DEV policy; capacity is always 4. Config is re-read under the request transaction lock. Invalid config returns `Unavailable/SERVER_CONFIG_INVALID` or prevents startup. Changing mode policy invalidates existing room's readiness/start capability. Permission/implementation revocation prevents ready/start for that member. No option or animal implementation is inferred.

DEV access keys are plaintext secrets in an operator-local config, separate from bearer sessions. Do not publish real keys or databases. A bearer token authorizes its configured DEV account; it is not a Google token or an ownership/progress proof. Session tokens are random and persist in the SQLite database until that database/token is removed or account/namespace revoked. Session expiry, rate limiting, HTTPS termination, credential hashing/rotation, service accounts, and public Internet abuse controls are not implemented. They belong in a production account/server design.

SQLite persists rooms, memberships, bearer sessions, and ActionId immutable request/results. One `BEGIN IMMEDIATE` transaction and application lock cover validation/capacity checks/membership insertion/result storage. Multiple HTTP request threads cannot admit a fifth member. A single service process owns the database; horizontal scaling/multiple processes are outside this package. Stop the server before copying its SQLite database and WAL/SHM files, or use SQLite's backup facilities. Database deletion is a DEV reset, never an in-flight action recovery method.

## Wire format

All fields use exact PascalCase. POST uses `Content-Type: application/json`; authenticated routes use `Authorization: Bearer <SessionToken>`. Tokens never belong in URL/query parameters. JSON body limit is 65536 bytes. Query parameters and chunked request bodies are unsupported. The server suppresses request logging and never prints request bodies or bearer tokens.

Transport/business errors use `{ "Status": "Rejected"|"Unavailable", "Reason": "..." }`. Malformed requests/auth/lookup failures use appropriate HTTP 4xx/503. A syntactically identified entry ActionId receives HTTP 200 with its durably stored final business outcome. Body `Status`, not HTTP 200 alone, determines success. A conflicting ActionId payload uses HTTP 409. Client network failure/timeout is **Unknown**, including an error after the commit before the response reaches the client.

| Method/path | Request | Result |
|---|---|---|
| GET `/health` | None; unauthenticated | Status=DevOnly, Protocol=ANIMOL_NET02, Gameplay=false |
| POST `/v1/dev/session` | AccountId, DevAccessKey | SessionToken, AccountId, SessionNamespace |
| GET `/v1/catalog` | Bearer | Modes, Animals, CodeFormat; never account keys |
| POST `/v1/multiplayer/context` | ModeId, EntryIntent, RoomCode (`""` for create/public) | Context, Snapshot |
| POST `/v1/room/lookup` | RoomCode | RoomId, RoomCode, ModeId, PolicyRevision, Phase, ParticipantCount, Capacity |
| POST `/v1/room/entry` | Full immutable entry below | Durable final result/receipt below |
| POST `/v1/room/result` | **Full exact immutable entry**, same ActionId | Same stored result, or Status=Unknown / Reason=ACTION_NOT_FOUND |
| GET `/v1/room/state` | Bearer | Full authoritative RoomState below |
| POST `/v1/room/ready` | RoomId, Ready (desired boolean) | RoomState; repeated same desired value does not toggle |
| POST `/v1/room/start` | RoomId | RoomState; owner-only, repeated Started is idempotent |
| POST `/v1/room/leave` | RoomId | Status=Accepted; repeat absent target membership gives Reason=ALREADY_LEFT |

RoomCode is a DEV protocol format: six characters from `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`. Lookup/join normalize surrounding whitespace and uppercase; the project must bind the code rule from this server catalog. This format is not a claim that production room-code policy was already decided.

Context fields: ContextId=ModeId, ModeId, EntryIntent, PolicyRevision, AllowedAnimalIds, InitialLoadout, RepresentativeRole, RoomCode. For join, lookup must establish room mode/policy and Waiting status; raw input alone is not an established room. For create/public no room is created until the entry transaction commits.

Snapshot fields: Revision (server computed opaque SHA256 string over relevant config/account policy), Animals containing AnimalId, Implemented, Unlocked, HasContextPermission, CanUseInContext. Snapshot revision, policy revision, role map, implementation, allowed pool, and context permission are rechecked on submit. The selected three are validated; an unrelated unimplemented animal does not block them.

Immutable entry fields are **exactly** ActionId, ContextId, SnapshotRevision, PolicyRevision, EntryIntent, RoomCode, Loadout={Ground,Special,Air}, Options=[{OptionId,Value}]. Only create may supply explicit Options; configured defaults apply otherwise. Confirmation fixes these fields once. After timeout, persist and query the whole original payload. The result endpoint never executes/create/joins a room. An absent result is Unknown, not permission to mint another ActionId.

Accepted result fields: Status=Accepted, Reason=`""`, ActionId, SnapshotRevision, AcceptanceToken, AcceptedContextId, AcceptedPolicyRevision, AcceptedEntryIntent, AcceptedLoadout, RoomId, RoomCode. The token is server-generated once inside the same transaction as membership and persisted result. Accepted fields follow the validated room/current policy/capability and copied validated loadout. Replays return the exact original receipt. Client must check the token, ActionId, all context/policy/intent/snapshot/loadout fields and room references against its immutable request before consuming once. It must not synthesize accepted fields by copying its request.

Rejected/Unavailable result contains Status, Reason, ActionId. Final rejection stays final for that ActionId, even if policy later changes. A manually refreshed new confirmation can create a new ActionId only after resolving the previous final result; never mutate and reuse the old ActionId. ActionIds are account-scoped: another account cannot retrieve the receipt.

RoomState fields: RoomId, RoomCode, ModeId, PolicyRevision, Phase, Revision (monotonically increasing decimal integer serialized as a string), OwnerAccountId, Participants=[{AccountId, Ready, Connected, IsOwner, Loadout}], Capacity=4, Options, CanReady, CanStart, CanLeave. Snapshot and policy revisions remain opaque strings. Membership is account-owned and only one room per account is allowed. Different participants may use identical loadouts. Public admission fills an existing same-mode/current-policy Waiting room or creates another when full; this is a minimal DEV lane, not a ranked matchmaking algorithm.

Any authenticated request refreshes the caller heartbeat. Poll state periodically (for example every 2 seconds); Connected becomes false after 30 seconds with no authenticated request. A background/offline member retains its slot and owner to allow the same saved session to resume. The server does not evict or transfer owners on disconnect. Ready/start requires current permission, all connected/ready and the configured minimum: public exactly four, custom at least two by default. Explicit owner leave while Waiting transfers ownership to the earliest remaining member. No remaining member closes the room.

**Started is a lobby gate only.** No match/map/HUD launch, player simulation, movement/stamina/bubbles/exit sync, win result, or reward exists. Leaving after Started aborts the remaining room. Actual scene launch remains blocked until an authoritative gameplay service/launch contract is implemented. Cooperation policy is not overwritten by these competition DEV rules.

Ready/start/leave require the accepted target RoomId. An old leave replay acknowledges the absent old membership and never leaves a newer room. A ready/start request for a different current room is rejected. These operations use desired-state/membership idempotency and return state; they do not use the durable entry ActionId protocol. Their timeout is resolved by polling state or repeating the same desired action against the same RoomId. Persist entry payload/receipt and account/session binding in the client journal; app scene replacement and re-entry must query/consume through the central manager once.
