"""Actual localhost HTTP tests, exclusively TEST_* IDs; never production catalog mapping."""
import concurrent.futures
import importlib.util
import json
import socket
import subprocess
import sys
import tempfile
import threading
import time
import unittest
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parent.parent
SPEC = importlib.util.spec_from_file_location("animol_net02_server", ROOT / "Server" / "server.py")
SERVER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(SERVER)


class HttpLobbyTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.folder = Path(self.temp.name)
        self.config = json.loads((ROOT / "ServerTests" / "fixture_config.json").read_text())
        self.config_path = self.folder / "config.json"
        self.save_config()
        self.database = self.folder / "lobby.sqlite3"
        self.tokens = {}
        self.rooms = {}
        self.boot()

    def save_config(self):
        self.config_path.write_text(json.dumps(self.config))

    def boot(self):
        self.store = SERVER.LobbyStore(self.database, self.config_path)
        self.http = SERVER.LobbyHttpServer(("127.0.0.1", 0), self.store)
        self.base = f"http://127.0.0.1:{self.http.server_address[1]}"
        self.thread = threading.Thread(target=self.http.serve_forever, kwargs={"poll_interval": 0.01}, daemon=True)
        self.thread.start()

    def stop(self):
        self.http.shutdown()
        self.thread.join(2)
        self.http.server_close()
        self.store.close()

    def tearDown(self):
        self.stop()
        self.temp.cleanup()

    def api(self, path, body=None, token=None):
        if path in ("/v1/room/ready", "/v1/room/start", "/v1/room/leave") and isinstance(body, dict) and "RoomId" not in body:
            body = dict(body, RoomId=self.rooms.get(token, "TEST_UNKNOWN_ROOM"))
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = "Bearer " + token
        data = None if body is None else json.dumps(body).encode()
        req = Request(self.base + path, data=data, headers=headers, method="GET" if body is None else "POST")
        try:
            with urlopen(req, timeout=3) as response:
                parsed = json.load(response)
                if path == "/v1/room/entry" and parsed.get("Status") == "Accepted":
                    self.rooms[token] = parsed["RoomId"]
                return response.status, parsed
        except HTTPError as response:
            return response.code, json.load(response)

    def session(self, number):
        if number not in self.tokens:
            status, body = self.api("/v1/dev/session", {"AccountId": f"TEST_ACCOUNT_{number}", "DevAccessKey": f"TEST_KEY_{number}"})
            self.assertEqual(200, status)
            self.tokens[number] = body["SessionToken"]
        return self.tokens[number]

    def context(self, number=1, intent="TEST_CREATE_INTENT", code=""):
        status, body = self.api("/v1/multiplayer/context", {"ModeId": "TEST_COMPETITION", "EntryIntent": intent, "RoomCode": code}, self.session(number))
        self.assertEqual(200, status, body)
        return body

    def request(self, number=1, intent="TEST_CREATE_INTENT", code="", action=None):
        context = self.context(number, intent, code)
        return {"ActionId": action or f"TEST_ACTION_{number}", "ContextId": context["Context"]["ContextId"],
                "SnapshotRevision": context["Snapshot"]["Revision"], "PolicyRevision": context["Context"]["PolicyRevision"],
                "EntryIntent": intent, "RoomCode": code, "Loadout": context["Context"]["InitialLoadout"], "Options": []}

    def enter(self, number=1, intent="TEST_CREATE_INTENT", code="", action=None):
        request = self.request(number, intent, code, action)
        status, result = self.api("/v1/room/entry", request, self.session(number))
        self.assertEqual(200, status, result)
        self.assertEqual("Accepted", result["Status"], result)
        return request, result

    def state(self, number=1):
        status, body = self.api("/v1/room/state", token=self.session(number))
        self.assertEqual(200, status, body)
        return body

    def test_01_empty_config_denies_session_and_does_not_invent_ids(self):
        self.config = json.loads((ROOT / "Server" / "config.empty.json").read_text())
        self.save_config()
        status, body = self.api("/v1/dev/session", {"AccountId": "TEST_ACCOUNT_1", "DevAccessKey": "TEST_KEY_1"})
        self.assertEqual(401, status)
        self.assertEqual("DEV_SESSION_DENIED", body["Reason"])

    def test_02_authentication_and_catalog_never_return_access_keys(self):
        self.assertEqual(401, self.api("/v1/catalog")[0])
        status, catalog = self.api("/v1/catalog", token=self.session(1))
        self.assertEqual(200, status)
        self.assertNotIn("DevAccessKey", json.dumps(catalog))
        self.assertEqual(6, catalog["CodeFormat"]["Length"])

    def test_03_server_mints_receipt_and_distinct_room_token(self):
        request, result = self.enter()
        self.assertTrue(result["AcceptanceToken"])
        self.assertNotEqual(request["ActionId"], result["AcceptanceToken"])
        self.assertEqual(request["Loadout"], result["AcceptedLoadout"])
        self.assertEqual(request["SnapshotRevision"], result["SnapshotRevision"])
        self.assertEqual(1, len(self.state()["Participants"]))

    def test_04_idempotent_create_and_result_survive_server_restart(self):
        request, first = self.enter()
        self.stop()
        self.boot()
        status, second = self.api("/v1/room/entry", request, self.session(1))
        status2, third = self.api("/v1/room/result", request, self.session(1))
        self.assertEqual((200, 200), (status, status2))
        self.assertEqual(first, second)
        self.assertEqual(first, third)
        self.assertEqual(1, len(self.state()["Participants"]))

    def test_05_same_action_changed_payload_conflicts(self):
        request, _ = self.enter()
        request["RoomCode"] = "ABCDEF"
        for path in ("/v1/room/entry", "/v1/room/result"):
            status, body = self.api(path, request, self.session(1))
            self.assertEqual(409, status)
            self.assertEqual("ACTION_PAYLOAD_CONFLICT", body["Reason"])

    def test_06_unknown_result_creates_neither_action_nor_room(self):
        request = self.request()
        status, body = self.api("/v1/room/result", request, self.session(1))
        self.assertEqual(200, status)
        self.assertEqual("Unknown", body["Status"])
        self.assertEqual(404, self.api("/v1/room/state", token=self.session(1))[0])
        with self.store.lock:
            self.assertEqual(0, self.store.db.execute("SELECT COUNT(*) FROM actions").fetchone()[0])

    def test_07_exact_three_roles_and_real_ids_required(self):
        request = self.request()
        request["Loadout"].pop("Air")
        _, result = self.api("/v1/room/entry", request, self.session(1))
        self.assertEqual("THREE_ROLE_LOADOUT_REQUIRED", result["Reason"])
        request = self.request(action="TEST_WRONG_ROLE")
        request["Loadout"]["Ground"] = "TEST_AIR"
        _, result = self.api("/v1/room/entry", request, self.session(1))
        self.assertEqual("ANIMAL_ROLE_OR_ID_INVALID", result["Reason"])

    def test_08_unimplemented_selected_animal_blocked_unrelated_one_ignored(self):
        request = self.request()
        request["Loadout"]["Ground"] = "TEST_UNIMPLEMENTED"
        _, result = self.api("/v1/room/entry", request, self.session(1))
        self.assertEqual("Unavailable", result["Status"])
        self.assertEqual("ANIMAL_UNIMPLEMENTED", result["Reason"])
        self.enter(action="TEST_VALID_WITH_UNIMPLEMENTED_OTHER")

    def test_09_unlocked_does_not_supply_permission_but_explicit_trial_grant_can(self):
        self.config["Accounts"][0]["ModePermissions"][0]["AllowedAnimalIds"].remove("TEST_GROUND")
        self.save_config()
        status, error = self.api("/v1/multiplayer/context", {"ModeId": "TEST_COMPETITION", "EntryIntent": "TEST_CREATE_INTENT", "RoomCode": ""}, self.session(1))
        self.assertEqual(403, status)
        self.assertEqual("ANIMAL_PERMISSION_DENIED", error["Reason"])
        self.config["Accounts"][0]["ModePermissions"][0]["AllowedAnimalIds"].append("TEST_GROUND")
        self.config["Accounts"][0]["UnlockedAnimalIds"] = []
        self.save_config()
        self.enter()

    def test_10_stale_snapshot_after_permission_change_is_final_durable_rejection(self):
        request = self.request()
        self.config["Accounts"][0]["UnlockedAnimalIds"] = []
        self.save_config()
        _, first = self.api("/v1/room/entry", request, self.session(1))
        self.assertEqual("SNAPSHOT_STALE", first["Reason"])
        self.config["Accounts"][0]["UnlockedAnimalIds"] = ["TEST_GROUND", "TEST_SPECIAL", "TEST_AIR"]
        self.save_config()
        _, second = self.api("/v1/room/entry", request, self.session(1))
        self.assertEqual(first, second)

    def test_11_stale_policy_is_rejected(self):
        request = self.request()
        self.config["Modes"][0]["PolicyRevision"] = "TEST_POLICY_2"
        self.save_config()
        _, result = self.api("/v1/room/entry", request, self.session(1))
        self.assertEqual("POLICY_STALE", result["Reason"])

    def test_12_concurrent_join_reserves_only_four_total_and_duplicates_allowed(self):
        _, owner = self.enter()
        requests = [(i, self.request(i, "TEST_JOIN_INTENT", owner["RoomCode"])) for i in range(2, 7)]
        def submit(pair):
            i, request = pair
            return self.api("/v1/room/entry", request, self.session(i))[1]
        with concurrent.futures.ThreadPoolExecutor(max_workers=5) as executor:
            results = list(executor.map(submit, requests))
        self.assertEqual(3, sum(r["Status"] == "Accepted" for r in results))
        self.assertEqual(2, sum(r["Reason"] == "ROOM_FULL" for r in results))
        state = self.state()
        self.assertEqual(4, len(state["Participants"]))
        self.assertEqual(1, len({json.dumps(p["Loadout"]) for p in state["Participants"]}))

    def test_13_one_account_cannot_enter_parallel_rooms(self):
        self.enter()
        request = self.request(action="TEST_OTHER_ROOM")
        _, result = self.api("/v1/room/entry", request, self.session(1))
        self.assertEqual("ALREADY_IN_ROOM", result["Reason"])
        self.assertEqual(1, len(self.state()["Participants"]))

    def test_14_custom_start_requires_owner_two_ready_connected_members(self):
        _, owner = self.enter()
        self.assertEqual(409, self.api("/v1/room/start", {}, self.session(1))[0])
        self.enter(2, "TEST_JOIN_INTENT", owner["RoomCode"])
        self.assertEqual(403, self.api("/v1/room/start", {}, self.session(2))[0])
        self.api("/v1/room/ready", {"Ready": True}, self.session(1))
        self.assertFalse(self.state()["CanStart"])
        self.api("/v1/room/ready", {"Ready": True}, self.session(2))
        self.assertTrue(self.state()["CanStart"])
        status, started = self.api("/v1/room/start", {}, self.session(1))
        self.assertEqual(200, status)
        self.assertEqual("Started", started["Phase"])
        self.assertEqual(started, self.api("/v1/room/start", {}, self.session(1))[1])

    def test_15_ready_desired_value_is_idempotent(self):
        self.enter()
        _, first = self.api("/v1/room/ready", {"Ready": True}, self.session(1))
        _, second = self.api("/v1/room/ready", {"Ready": True}, self.session(1))
        self.assertEqual(first["Revision"], second["Revision"])
        self.assertTrue(second["Participants"][0]["Ready"])
        self.assertEqual(400, self.api("/v1/room/ready", {"Ready": "true"}, self.session(1))[0])

    def test_16_waiting_owner_leave_transfers_owner_and_leave_is_idempotent(self):
        _, owner = self.enter()
        self.enter(2, "TEST_JOIN_INTENT", owner["RoomCode"])
        self.assertEqual("Accepted", self.api("/v1/room/leave", {}, self.session(1))[1]["Status"])
        self.assertEqual("TEST_ACCOUNT_2", self.state(2)["OwnerAccountId"])
        self.assertEqual("Accepted", self.api("/v1/room/leave", {}, self.session(1))[1]["Status"])

    def test_17_public_lane_creates_second_room_after_four_and_requires_four_to_start(self):
        receipts = [self.enter(i, "TEST_PUBLIC_INTENT")[1] for i in range(1, 6)]
        self.assertEqual(1, len({r["RoomId"] for r in receipts[:4]}))
        self.assertNotEqual(receipts[0]["RoomId"], receipts[4]["RoomId"])
        for i in range(1, 4):
            self.api("/v1/room/ready", {"Ready": True}, self.session(i))
        self.assertFalse(self.state()["CanStart"])
        self.api("/v1/room/ready", {"Ready": True}, self.session(4))
        self.assertTrue(self.state()["CanStart"])

    def test_18_expired_heartbeat_retains_slot_but_blocks_start(self):
        _, owner = self.enter()
        self.enter(2, "TEST_JOIN_INTENT", owner["RoomCode"])
        for i in (1, 2):
            self.api("/v1/room/ready", {"Ready": True}, self.session(i))
        with self.store.lock:
            self.store.db.execute("UPDATE sessions SET seen=0 WHERE account='TEST_ACCOUNT_2'")
            self.store.db.commit()
        state = self.state(1)
        self.assertEqual(2, len(state["Participants"]))
        self.assertFalse(state["CanStart"])
        self.assertFalse(state["Participants"][1]["Connected"])
        self.state(2)
        self.assertTrue(self.state(1)["CanStart"])

    def test_19_codes_and_room_options_are_server_policy_only(self):
        self.assertEqual(400, self.api("/v1/room/lookup", {"RoomCode": "123"}, self.session(1))[0])
        request = self.request()
        request["Options"] = [{"OptionId": "UNKNOWN", "Value": "TEST_FAST"}]
        _, result = self.api("/v1/room/entry", request, self.session(1))
        self.assertEqual("OPTION_NOT_ALLOWED", result["Reason"])
        request = self.request(action="TEST_ALLOWED_OPTION")
        request["Options"] = [{"OptionId": "TEST_SPEED", "Value": "TEST_FAST"}]
        _, result = self.api("/v1/room/entry", request, self.session(1))
        self.assertEqual("Accepted", result["Status"])
        self.assertEqual(request["Options"], self.state()["Options"])

    def test_20_started_lobby_gate_no_gameplay_and_leave_aborts(self):
        _, owner = self.enter()
        self.enter(2, "TEST_JOIN_INTENT", owner["RoomCode"])
        for i in (1, 2):
            self.api("/v1/room/ready", {"Ready": True}, self.session(i))
        self.api("/v1/room/start", {}, self.session(1))
        self.assertFalse(self.api("/health")[1]["Gameplay"])
        self.api("/v1/room/leave", {}, self.session(1))
        self.assertEqual("Aborted", self.state(2)["Phase"])

    def test_21_revoked_permission_after_admission_blocks_ready_and_start(self):
        _, owner = self.enter()
        self.enter(2, "TEST_JOIN_INTENT", owner["RoomCode"])
        for i in (1, 2):
            self.api("/v1/room/ready", {"Ready": True}, self.session(i))
        self.config["Accounts"][1]["ModePermissions"] = []
        self.save_config()
        self.assertFalse(self.state(1)["CanStart"])
        self.assertFalse(self.state(2)["CanReady"])

    def test_22_action_results_are_bound_to_authenticated_account(self):
        request, result = self.enter()
        _, other = self.api("/v1/room/result", request, self.session(2))
        self.assertEqual("Unknown", other["Status"])
        self.assertNotIn("AcceptanceToken", other)
        self.assertEqual(404, self.api("/v1/room/state", token=self.session(2))[0])

    def test_23_cli_first_run_creates_database_parent_and_serves_health(self):
        with socket.socket() as reserved:
            reserved.bind(("127.0.0.1", 0))
            port = reserved.getsockname()[1]
        database = self.folder / "Runtime" / "nested" / "cli.sqlite3"
        process = subprocess.Popen([sys.executable, str(ROOT / "Server" / "server.py"), "--bind", "127.0.0.1", "--port", str(port),
                                    "--config", str(ROOT / "Server" / "config.empty.json"), "--database", str(database)],
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        try:
            deadline = time.monotonic() + 5
            observed = None
            while time.monotonic() < deadline:
                if process.poll() is not None:
                    self.fail("CLI server exited before health check: " + process.stderr.read().decode())
                try:
                    with urlopen(f"http://127.0.0.1:{port}/health", timeout=0.5) as response:
                        observed = json.load(response)
                    break
                except OSError:
                    time.sleep(0.02)
            self.assertIsNotNone(observed)
            self.assertEqual("DevOnly", observed["Status"])
            self.assertTrue(database.is_file())
        finally:
            process.terminate()
            process.wait(timeout=3)
            process.stdout.close()
            process.stderr.close()

    def test_24_invalid_namespace_is_unavailable_not_silent_client_mismatch(self):
        self.config["SessionNamespace"] = "UNRELATED"
        self.save_config()
        status, result = self.api("/health")
        self.assertEqual(503, status)
        self.assertEqual("SERVER_CONFIG_INVALID", result["Reason"])

    def test_25_delayed_old_leave_never_exits_a_new_room(self):
        _, old = self.enter()
        self.api("/v1/room/leave", {"RoomId": old["RoomId"]}, self.session(1))
        _, new = self.enter(action="TEST_NEW_ROOM")
        _, result = self.api("/v1/room/leave", {"RoomId": old["RoomId"]}, self.session(1))
        self.assertEqual("Accepted", result["Status"])
        self.assertEqual("ALREADY_LEFT", result["Reason"])
        self.assertEqual(new["RoomId"], self.state()["RoomId"])
        status, result = self.api("/v1/room/ready", {"RoomId": old["RoomId"], "Ready": True}, self.session(1))
        self.assertEqual(409, status)
        self.assertEqual("ROOM_TARGET_MISMATCH", result["Reason"])
        self.assertFalse(self.state()["Participants"][0]["Ready"])


if __name__ == "__main__":
    unittest.main(verbosity=2)
