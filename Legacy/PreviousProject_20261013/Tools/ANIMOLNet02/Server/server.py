#!/usr/bin/env python3
"""ANIMOL NET02 DEV lobby; standard library only, NEVER a production account/game server."""
import argparse
import hashlib
import json
import os
import secrets
import sqlite3
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit

ROLES = ("Ground", "Special", "Air")
CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"
CAPACITY = 4
CONNECTED_SECONDS = 30
MAX_BODY = 65536


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False)


class ApiError(Exception):
    def __init__(self, reason, status=400, outcome="Rejected"):
        super().__init__(reason)
        self.reason, self.status, self.outcome = reason, status, outcome


def need_string(value, reason, maximum=256):
    if not isinstance(value, str) or not value or len(value) > maximum:
        raise ApiError(reason)
    return value


def object_body(value):
    if not isinstance(value, dict):
        raise ApiError("OBJECT_BODY_REQUIRED")
    return value


class LobbyStore:
    def __init__(self, database_path, config_path):
        self.config_path = Path(config_path)
        self.lock = threading.RLock()
        self.db = sqlite3.connect(str(database_path), check_same_thread=False)
        self.db.row_factory = sqlite3.Row
        self.db.execute("PRAGMA journal_mode=WAL")
        self.db.execute("PRAGMA foreign_keys=ON")
        self.db.executescript("""
        CREATE TABLE IF NOT EXISTS sessions (
          token TEXT PRIMARY KEY, account TEXT NOT NULL, namespace TEXT NOT NULL, seen REAL NOT NULL);
        CREATE TABLE IF NOT EXISTS rooms (
          id TEXT PRIMARY KEY, code TEXT UNIQUE NOT NULL, mode TEXT NOT NULL, policy TEXT NOT NULL,
          kind TEXT NOT NULL, phase TEXT NOT NULL, owner TEXT NOT NULL, options TEXT NOT NULL,
          revision INTEGER NOT NULL DEFAULT 1, created REAL NOT NULL);
        CREATE TABLE IF NOT EXISTS members (
          account TEXT PRIMARY KEY, room TEXT NOT NULL REFERENCES rooms(id),
          loadout TEXT NOT NULL, intent TEXT NOT NULL, ready INTEGER NOT NULL DEFAULT 0, joined REAL NOT NULL);
        CREATE INDEX IF NOT EXISTS members_room ON members(room);
        CREATE TABLE IF NOT EXISTS actions (
          account TEXT NOT NULL, action TEXT NOT NULL, payload TEXT NOT NULL, result TEXT NOT NULL,
          PRIMARY KEY(account, action));
        """)
        self.db.commit()
        self.read_config()  # Fail fast on invalid config; empty config is valid and blocks use.

    def close(self):
        with self.lock:
            self.db.close()

    def read_config(self):
        try:
            c = json.loads(self.config_path.read_text(encoding="utf-8"))
            if c.get("SchemaVersion") != 1:
                raise ValueError("SchemaVersion")
            namespace = c["SessionNamespace"]
            if namespace != "ANIMOL_NET02_DEV":
                raise ValueError("SessionNamespace")
            for name, key in (("Animals", "AnimalId"), ("Modes", "ModeId"), ("Accounts", "AccountId")):
                rows = c[name]
                if not isinstance(rows, list) or len({r[key] for r in rows}) != len(rows):
                    raise ValueError(name)
                if any(not isinstance(r[key], str) or not r[key] for r in rows):
                    raise ValueError(key)
            animal_ids = {a["AnimalId"] for a in c["Animals"]}
            if len({a["StableArtId"] for a in c["Animals"]}) != len(c["Animals"]):
                raise ValueError("StableArtId")
            for a in c["Animals"]:
                if a["Role"] not in ROLES or not isinstance(a["StableArtId"], str) or not a["StableArtId"] or type(a["Implemented"]) is not bool:
                    raise ValueError("Animal map")
            for m in c["Modes"]:
                if not isinstance(m["DisplayName"], str) or not m["DisplayName"] or not isinstance(m["PolicyRevision"], str) or not m["PolicyRevision"] or m["RepresentativeRole"] not in ROLES:
                    raise ValueError("Mode policy")
                if not isinstance(m["AllowedAnimalIds"], list) or len(set(m["AllowedAnimalIds"])) != len(m["AllowedAnimalIds"]) or not set(m["AllowedAnimalIds"]) <= animal_ids:
                    raise ValueError("AllowedAnimalIds")
                if not isinstance(m["EntryIntents"], list) or not m["EntryIntents"]:
                    raise ValueError("EntryIntents")
                if len({i["IntentId"] for i in m["EntryIntents"]}) != len(m["EntryIntents"]):
                    raise ValueError("IntentId")
                for i in m["EntryIntents"]:
                    if not isinstance(i["IntentId"], str) or not i["IntentId"] or i["Kind"] not in ("create", "join", "public"):
                        raise ValueError("Intent capability")
                if type(m.get("PublicStartCount", 4)) is not int or m.get("PublicStartCount", 4) != 4 or type(m.get("CustomStartMinimum", 2)) is not int or not 2 <= m.get("CustomStartMinimum", 2) <= 4:
                    raise ValueError("Capacity")
                options = m["Options"]
                if not isinstance(options, list) or len({o["OptionId"] for o in options}) != len(options):
                    raise ValueError("Options")
                for o in options:
                    if not isinstance(o["OptionId"], str) or not o["OptionId"] or not isinstance(o["AllowedValues"], list) or not o["AllowedValues"] or any(not isinstance(v, str) or not v for v in o["AllowedValues"]) or o["DefaultValue"] not in o["AllowedValues"]:
                        raise ValueError("Option values")
            for a in c["Accounts"]:
                if not isinstance(a["DevAccessKey"], str) or not a["DevAccessKey"]:
                    raise ValueError("DevAccessKey")
                if not isinstance(a["UnlockedAnimalIds"], list) or not set(a["UnlockedAnimalIds"]) <= animal_ids:
                    raise ValueError("UnlockedAnimalIds")
                if not isinstance(a["InitialLoadout"], dict) or not isinstance(a["ModePermissions"], list):
                    raise ValueError("Account configuration")
                seen_grants = set()
                for p in a["ModePermissions"]:
                    if not p["ModeId"] or not isinstance(p["IntentIds"], list) or not isinstance(p["AllowedAnimalIds"], list):
                        raise ValueError("ModePermissions")
                    if not set(p["AllowedAnimalIds"]) <= animal_ids:
                        raise ValueError("Permission animal IDs")
                    mode = next((m for m in c["Modes"] if m["ModeId"] == p["ModeId"]), None)
                    if mode is None or not set(p["IntentIds"]) <= {i["IntentId"] for i in mode["EntryIntents"]}:
                        raise ValueError("Permission mode or intent")
                    for intent_id in p["IntentIds"]:
                        grant_key = (p["ModeId"], intent_id)
                        if grant_key in seen_grants:
                            raise ValueError("Duplicate permission grant")
                        seen_grants.add(grant_key)
            return c
        except (OSError, ValueError, KeyError, TypeError) as e:
            raise ApiError("SERVER_CONFIG_INVALID", 503, "Unavailable") from e

    def request(self, method, path, token, body):
        with self.lock:
            self.db.execute("BEGIN IMMEDIATE")
            try:
                c = self.read_config()
                result = self.dispatch(method, path, token, object_body(body), c)
                self.db.commit()
                return 200, result
            except ApiError as e:
                self.db.rollback()
                return e.status, {"Status": e.outcome, "Reason": e.reason}
            except Exception:
                self.db.rollback()
                raise

    def dispatch(self, method, path, token, body, c):
        if method == "GET" and path == "/health":
            return {"Status": "DevOnly", "Protocol": "ANIMOL_NET02", "Gameplay": False}
        if method == "POST" and path == "/v1/dev/session":
            account_id = need_string(body.get("AccountId"), "ACCOUNT_ID_REQUIRED")
            key = need_string(body.get("DevAccessKey"), "DEV_ACCESS_KEY_REQUIRED", 1024)
            a = next((a for a in c["Accounts"] if a["AccountId"] == account_id), None)
            if a is None or not secrets.compare_digest(a["DevAccessKey"], key):
                raise ApiError("DEV_SESSION_DENIED", 401, "Unavailable")
            token = secrets.token_urlsafe(32)
            self.db.execute("INSERT INTO sessions VALUES(?,?,?,?)", (token, account_id, c["SessionNamespace"], time.time()))
            return {"SessionToken": token, "AccountId": account_id, "SessionNamespace": c["SessionNamespace"]}
        session = self.db.execute("SELECT * FROM sessions WHERE token=?", (token,)).fetchone()
        if session is None or session["namespace"] != c["SessionNamespace"]:
            raise ApiError("AUTH_REQUIRED", 401, "Unavailable")
        account = next((a for a in c["Accounts"] if a["AccountId"] == session["account"]), None)
        if account is None:
            raise ApiError("DEV_ACCOUNT_REVOKED", 401, "Unavailable")
        self.db.execute("UPDATE sessions SET seen=? WHERE token=?", (time.time(), token))
        routes = {
            ("GET", "/v1/catalog"): lambda: {"Modes": c["Modes"], "Animals": c["Animals"], "CodeFormat": {"Length": 6, "Alphabet": CODE_ALPHABET}},
            ("POST", "/v1/multiplayer/context"): lambda: self.context(body, account, c),
            ("POST", "/v1/room/lookup"): lambda: self.lookup(body),
            ("POST", "/v1/room/entry"): lambda: self.entry(body, account, c),
            ("POST", "/v1/room/result"): lambda: self.entry_result(body, account),
            ("GET", "/v1/room/state"): lambda: self.state(account["AccountId"], c),
            ("POST", "/v1/room/ready"): lambda: self.ready(body, account, c),
            ("POST", "/v1/room/start"): lambda: self.start(body, account, c),
            ("POST", "/v1/room/leave"): lambda: self.leave(body, account),
        }
        route = routes.get((method, path))
        if route is None:
            raise ApiError("ENDPOINT_NOT_FOUND", 404)
        return route()

    def mode_intent(self, mode_id, intent_id, c):
        mode = next((m for m in c["Modes"] if m["ModeId"] == mode_id), None)
        if mode is None:
            raise ApiError("MODE_UNCONFIGURED", 409, "Unavailable")
        intent = next((i for i in mode["EntryIntents"] if i["IntentId"] == intent_id), None)
        if intent is None:
            raise ApiError("INTENT_UNCONFIGURED", 409, "Unavailable")
        return mode, intent

    def snapshot(self, account, mode, intent, c):
        permission = next((p for p in account["ModePermissions"] if p["ModeId"] == mode["ModeId"] and intent["IntentId"] in p["IntentIds"]), None)
        granted = set(permission["AllowedAnimalIds"]) if permission else set()
        allowed = set(mode["AllowedAnimalIds"])
        revision = "sha256:" + hashlib.sha256(canonical({"Account": account, "Mode": mode, "Intent": intent, "Animals": c["Animals"]}).encode()).hexdigest()
        return {"Revision": revision, "Animals": [{"AnimalId": a["AnimalId"], "Implemented": a["Implemented"],
                  "Unlocked": a["AnimalId"] in account["UnlockedAnimalIds"], "HasContextPermission": a["AnimalId"] in granted,
                  "CanUseInContext": a["Implemented"] and a["AnimalId"] in granted and a["AnimalId"] in allowed} for a in c["Animals"]]}

    def normalized_code(self, value):
        if not isinstance(value, str):
            raise ApiError("ROOM_CODE_INVALID")
        code = value.strip().upper()
        if len(code) != 6 or any(ch not in CODE_ALPHABET for ch in code):
            raise ApiError("ROOM_CODE_INVALID")
        return code

    def lookup(self, body):
        code = self.normalized_code(body.get("RoomCode"))
        room = self.db.execute("SELECT * FROM rooms WHERE code=?", (code,)).fetchone()
        if room is None or room["phase"] == "Closed":
            raise ApiError("ROOM_NOT_FOUND", 404)
        return {"RoomId": room["id"], "RoomCode": room["code"], "ModeId": room["mode"], "PolicyRevision": room["policy"],
                "Phase": room["phase"], "ParticipantCount": self.member_count(room["id"]), "Capacity": CAPACITY}

    def context(self, body, account, c):
        mode, intent = self.mode_intent(body.get("ModeId"), body.get("EntryIntent"), c)
        code = ""
        if intent["Kind"] == "join":
            summary = self.lookup(body)
            if summary["ModeId"] != mode["ModeId"] or summary["PolicyRevision"] != mode["PolicyRevision"]:
                raise ApiError("ROOM_POLICY_MISMATCH", 409)
            if summary["Phase"] != "Waiting":
                raise ApiError("ROOM_NOT_WAITING", 409)
            code = summary["RoomCode"]
        self.validate_loadout(account["InitialLoadout"], account, mode, intent, c)
        return {"Context": {"ContextId": mode["ModeId"], "ModeId": mode["ModeId"], "EntryIntent": intent["IntentId"],
                "PolicyRevision": mode["PolicyRevision"], "AllowedAnimalIds": mode["AllowedAnimalIds"],
                "InitialLoadout": account["InitialLoadout"], "RepresentativeRole": mode["RepresentativeRole"], "RoomCode": code},
                "Snapshot": self.snapshot(account, mode, intent, c)}

    def validate_loadout(self, loadout, account, mode, intent, c):
        if not isinstance(loadout, dict) or set(loadout) != set(ROLES):
            raise ApiError("THREE_ROLE_LOADOUT_REQUIRED")
        by_id = {a["AnimalId"]: a for a in c["Animals"]}
        allowed = {a["AnimalId"]: a for a in self.snapshot(account, mode, intent, c)["Animals"]}
        for role in ROLES:
            animal_id = loadout[role]
            if not isinstance(animal_id, str) or animal_id not in by_id or by_id[animal_id]["Role"] != role:
                raise ApiError("ANIMAL_ROLE_OR_ID_INVALID")
            if not by_id[animal_id]["Implemented"]:
                raise ApiError("ANIMAL_UNIMPLEMENTED", 409, "Unavailable")
            if not allowed[animal_id]["HasContextPermission"]:
                raise ApiError("ANIMAL_PERMISSION_DENIED", 403)
            if not allowed[animal_id]["CanUseInContext"]:
                raise ApiError("ANIMAL_NOT_ALLOWED", 403)

    def options(self, requested, mode, kind):
        if not isinstance(requested, list):
            raise ApiError("OPTIONS_ARRAY_REQUIRED")
        if kind != "create" and requested:
            raise ApiError("OPTIONS_ONLY_ON_CREATE")
        if any(not isinstance(o, dict) or set(o) != {"OptionId", "Value"} for o in requested):
            raise ApiError("OPTION_INVALID")
        if len({o["OptionId"] for o in requested}) != len(requested):
            raise ApiError("OPTION_DUPLICATE")
        policy = {o["OptionId"]: o for o in mode["Options"]}
        actual = {key: o["DefaultValue"] for key, o in policy.items()}
        for option in requested:
            key, value = option["OptionId"], option["Value"]
            if key not in policy or value not in policy[key]["AllowedValues"]:
                raise ApiError("OPTION_NOT_ALLOWED")
            actual[key] = value
        return [{"OptionId": key, "Value": actual[key]} for key in sorted(actual)]

    def entry_result(self, body, account):
        action_id = need_string(body.get("ActionId"), "ACTION_ID_REQUIRED", 128)
        record = self.db.execute("SELECT * FROM actions WHERE account=? AND action=?", (account["AccountId"], action_id)).fetchone()
        if record is None:
            return {"Status": "Unknown", "Reason": "ACTION_NOT_FOUND", "ActionId": action_id}
        if record["payload"] != canonical(body):
            raise ApiError("ACTION_PAYLOAD_CONFLICT", 409)
        return json.loads(record["result"])

    def entry(self, body, account, c):
        action_id = need_string(body.get("ActionId"), "ACTION_ID_REQUIRED", 128)
        previous = self.db.execute("SELECT * FROM actions WHERE account=? AND action=?", (account["AccountId"], action_id)).fetchone()
        if previous is not None:
            return self.entry_result(body, account)
        try:
            required = {"ActionId", "ContextId", "SnapshotRevision", "PolicyRevision", "EntryIntent", "RoomCode", "Loadout", "Options"}
            if set(body) != required:
                raise ApiError("ENTRY_FIELDS_INVALID")
            mode, intent = self.mode_intent(body["ContextId"], body["EntryIntent"], c)
            snapshot = self.snapshot(account, mode, intent, c)
            if body["PolicyRevision"] != mode["PolicyRevision"]:
                raise ApiError("POLICY_STALE", 409)
            if body["SnapshotRevision"] != snapshot["Revision"]:
                raise ApiError("SNAPSHOT_STALE", 409)
            self.validate_loadout(body["Loadout"], account, mode, intent, c)
            options = self.options(body["Options"], mode, intent["Kind"])
            if self.db.execute("SELECT 1 FROM members WHERE account=?", (account["AccountId"],)).fetchone():
                raise ApiError("ALREADY_IN_ROOM", 409)
            room = self.find_or_create_room(body, account, mode, intent, options)
            if room["phase"] != "Waiting":
                raise ApiError("ROOM_NOT_WAITING", 409)
            if self.member_count(room["id"]) >= CAPACITY:
                raise ApiError("ROOM_FULL", 409)
            self.db.execute("INSERT INTO members(account,room,loadout,intent,joined) VALUES(?,?,?,?,?)",
                            (account["AccountId"], room["id"], canonical(body["Loadout"]), intent["IntentId"], time.time()))
            self.db.execute("UPDATE rooms SET revision=revision+1 WHERE id=?", (room["id"],))
            result = {"Status": "Accepted", "Reason": "", "ActionId": action_id, "SnapshotRevision": snapshot["Revision"],
                      "AcceptanceToken": secrets.token_urlsafe(32), "AcceptedContextId": room["mode"],
                      "AcceptedPolicyRevision": room["policy"], "AcceptedEntryIntent": intent["IntentId"],
                      "AcceptedLoadout": json.loads(canonical(body["Loadout"])), "RoomId": room["id"], "RoomCode": room["code"]}
        except ApiError as e:
            result = {"Status": e.outcome, "Reason": e.reason, "ActionId": action_id}
        self.db.execute("INSERT INTO actions VALUES(?,?,?,?)", (account["AccountId"], action_id, canonical(body), canonical(result)))
        return result

    def member_count(self, room_id):
        return self.db.execute("SELECT COUNT(*) FROM members WHERE room=?", (room_id,)).fetchone()[0]

    def find_or_create_room(self, body, account, mode, intent, options):
        kind = intent["Kind"]
        if kind == "join":
            code = self.normalized_code(body["RoomCode"])
            room = self.db.execute("SELECT * FROM rooms WHERE code=? AND kind='custom'", (code,)).fetchone()
            if room is None or room["phase"] == "Closed":
                raise ApiError("ROOM_NOT_FOUND", 404)
            if room["mode"] != mode["ModeId"] or room["policy"] != mode["PolicyRevision"]:
                raise ApiError("ROOM_POLICY_MISMATCH", 409)
            return room
        if body["RoomCode"] != "":
            raise ApiError("ROOM_CODE_UNEXPECTED")
        if kind == "public":
            room = self.db.execute("""SELECT r.* FROM rooms r WHERE mode=? AND policy=? AND kind='public' AND phase='Waiting'
                     AND (SELECT COUNT(*) FROM members m WHERE m.room=r.id)<4 ORDER BY created LIMIT 1""",
                                   (mode["ModeId"], mode["PolicyRevision"])).fetchone()
            if room is not None:
                return room
        room_id = "devroom_" + secrets.token_hex(16)
        while True:
            code = "".join(secrets.choice(CODE_ALPHABET) for _ in range(6))
            if not self.db.execute("SELECT 1 FROM rooms WHERE code=?", (code,)).fetchone():
                break
        self.db.execute("INSERT INTO rooms(id,code,mode,policy,kind,phase,owner,options,created) VALUES(?,?,?,?,?,'Waiting',?,?,?)",
                        (room_id, code, mode["ModeId"], mode["PolicyRevision"], "public" if kind == "public" else "custom",
                         account["AccountId"], canonical(options), time.time()))
        return self.db.execute("SELECT * FROM rooms WHERE id=?", (room_id,)).fetchone()

    def member_room(self, account_id):
        room = self.db.execute("SELECT r.* FROM rooms r JOIN members m ON r.id=m.room WHERE m.account=?", (account_id,)).fetchone()
        if room is None:
            raise ApiError("NOT_IN_ROOM", 404)
        return room

    def state(self, account_id, c):
        room = self.member_room(account_id)
        rows = self.db.execute("""SELECT m.*,COALESCE((SELECT MAX(s.seen) FROM sessions s WHERE s.account=m.account),0) seen
                      FROM members m WHERE room=? ORDER BY joined,account""", (room["id"],)).fetchall()
        participants = [{"AccountId": r["account"], "Ready": bool(r["ready"]), "Connected": time.time() - r["seen"] <= CONNECTED_SECONDS,
                         "IsOwner": r["account"] == room["owner"], "Loadout": json.loads(r["loadout"])} for r in rows]
        mode = next((m for m in c["Modes"] if m["ModeId"] == room["mode"]), None)
        policy_current = mode is not None and mode["PolicyRevision"] == room["policy"]
        eligible = {}
        for r in rows:
            try:
                a = next((a for a in c["Accounts"] if a["AccountId"] == r["account"]), None)
                if a is None or not policy_current:
                    raise ApiError("PARTICIPANT_PERMISSION_STALE")
                current_mode, intent = self.mode_intent(room["mode"], r["intent"], c)
                self.validate_loadout(json.loads(r["loadout"]), a, current_mode, intent, c)
                eligible[r["account"]] = True
            except ApiError:
                eligible[r["account"]] = False
        minimum = 4 if room["kind"] == "public" else mode.get("CustomStartMinimum", 2) if mode else 4
        can_start = room["phase"] == "Waiting" and policy_current and account_id == room["owner"] and len(rows) >= minimum and all(p["Ready"] and p["Connected"] and eligible[p["AccountId"]] for p in participants)
        return {"RoomId": room["id"], "RoomCode": room["code"], "ModeId": room["mode"], "PolicyRevision": room["policy"],
                "Phase": room["phase"], "Revision": str(room["revision"]), "OwnerAccountId": room["owner"],
                "Participants": participants, "Capacity": CAPACITY, "Options": json.loads(room["options"]),
                "CanReady": room["phase"] == "Waiting" and policy_current and eligible.get(account_id, False), "CanStart": can_start, "CanLeave": True}

    def ready(self, body, account, c):
        if set(body) != {"RoomId", "Ready"} or type(body["Ready"]) is not bool:
            raise ApiError("READY_BOOLEAN_REQUIRED")
        room_id = need_string(body["RoomId"], "ROOM_ID_REQUIRED")
        state = self.state(account["AccountId"], c)
        if state["RoomId"] != room_id:
            raise ApiError("ROOM_TARGET_MISMATCH", 409)
        if not state["CanReady"]:
            raise ApiError("ROOM_NOT_READYABLE", 409)
        row = self.db.execute("SELECT ready FROM members WHERE account=?", (account["AccountId"],)).fetchone()
        if bool(row["ready"]) != body["Ready"]:
            self.db.execute("UPDATE members SET ready=? WHERE account=?", (int(body["Ready"]), account["AccountId"]))
            self.db.execute("UPDATE rooms SET revision=revision+1 WHERE id=?", (state["RoomId"],))
        return self.state(account["AccountId"], c)

    def start(self, body, account, c):
        if set(body) != {"RoomId"}:
            raise ApiError("ROOM_ID_REQUIRED")
        room_id = need_string(body["RoomId"], "ROOM_ID_REQUIRED")
        state = self.state(account["AccountId"], c)
        if state["RoomId"] != room_id:
            raise ApiError("ROOM_TARGET_MISMATCH", 409)
        if state["OwnerAccountId"] != account["AccountId"]:
            raise ApiError("OWNER_REQUIRED", 403)
        if state["Phase"] == "Started":
            return state
        if not state["CanStart"]:
            raise ApiError("START_CONDITIONS_NOT_MET", 409)
        self.db.execute("UPDATE rooms SET phase='Started',revision=revision+1 WHERE id=?", (state["RoomId"],))
        return self.state(account["AccountId"], c)

    def leave(self, body, account):
        if set(body) != {"RoomId"}:
            raise ApiError("ROOM_ID_REQUIRED")
        room_id = need_string(body["RoomId"], "ROOM_ID_REQUIRED")
        account_id = account["AccountId"]
        try:
            room = self.member_room(account_id)
        except ApiError:
            return {"Status": "Accepted", "Reason": "ALREADY_LEFT"}
        if room["id"] != room_id:
            return {"Status": "Accepted", "Reason": "ALREADY_LEFT"}
        self.db.execute("DELETE FROM members WHERE account=?", (account_id,))
        remaining = self.db.execute("SELECT account FROM members WHERE room=? ORDER BY joined,account", (room["id"],)).fetchall()
        phase = "Closed" if not remaining else "Aborted" if room["phase"] in ("Started", "Aborted") else room["phase"]
        owner = remaining[0]["account"] if remaining and room["owner"] == account_id else room["owner"]
        self.db.execute("UPDATE rooms SET owner=?,phase=?,revision=revision+1 WHERE id=?", (owner, phase, room["id"]))
        return {"Status": "Accepted", "Reason": ""}


class LobbyHttpServer(ThreadingHTTPServer):
    daemon_threads = True
    def __init__(self, address, store):
        self.store = store
        super().__init__(address, LobbyHandler)


class LobbyHandler(BaseHTTPRequestHandler):
    server_version = "ANIMOLNET02Dev/1"
    def log_message(self, format, *args):
        pass  # Never log keys, tokens, JSON bodies, or query strings.

    def do_GET(self):
        self.handle_api("GET")

    def do_POST(self):
        self.handle_api("POST")

    def handle_api(self, method):
        try:
            parsed = urlsplit(self.path)
            if parsed.query or parsed.fragment:
                raise ApiError("QUERY_PARAMETERS_UNSUPPORTED")
            if self.headers.get("Transfer-Encoding"):
                raise ApiError("CHUNKED_BODY_UNSUPPORTED")
            try:
                length = int(self.headers.get("Content-Length", "0"))
            except ValueError:
                raise ApiError("CONTENT_LENGTH_INVALID")
            if length < 0 or length > MAX_BODY:
                raise ApiError("BODY_TOO_LARGE", 413)
            self.connection.settimeout(10)
            if method == "POST" and self.headers.get("Content-Type", "").split(";")[0].strip().lower() != "application/json":
                raise ApiError("JSON_CONTENT_TYPE_REQUIRED", 415)
            raw = self.rfile.read(length) if length else b"{}"
            try:
                body = json.loads(raw)
            except (ValueError, UnicodeError):
                raise ApiError("JSON_INVALID")
            auth = self.headers.get("Authorization", "")
            token = auth[7:] if auth.startswith("Bearer ") else ""
            status, result = self.server.store.request(method, parsed.path, token, body)
        except ApiError as e:
            status, result = e.status, {"Status": e.outcome, "Reason": e.reason}
        except Exception:
            status, result = 500, {"Status": "Unavailable", "Reason": "SERVER_INTERNAL_ERROR"}
        data = canonical(result).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        try:
            self.wfile.write(data)
        except (BrokenPipeError, ConnectionResetError):
            pass  # Mutation/result already committed; client recovers with SAME action.


def main():
    parser = argparse.ArgumentParser(description="ANIMOL NET02 DEV HTTP lobby (no gameplay/authentication integration)")
    parser.add_argument("--bind", default=os.environ.get("ANIMOL_DEV_BIND", "127.0.0.1"))
    parser.add_argument("--port", type=int, default=int(os.environ.get("ANIMOL_DEV_PORT", "8080")))
    parser.add_argument("--config", default=os.environ.get("ANIMOL_DEV_CONFIG", str(Path(__file__).with_name("config.empty.json"))))
    parser.add_argument("--database", default=os.environ.get("ANIMOL_DEV_DATABASE", str(Path(__file__).with_name("dev-lobby.sqlite3"))))
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("port must be 1..65535")
    Path(args.database).expanduser().resolve().parent.mkdir(parents=True, exist_ok=True)
    store = LobbyStore(args.database, args.config)
    server = LobbyHttpServer((args.bind, args.port), store)
    print(f"ANIMOL_NET02_DEV listening on {args.bind}:{args.port}; HTTP lobby only; no gameplay; do not publish publicly.", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
        store.close()


if __name__ == "__main__":
    main()
