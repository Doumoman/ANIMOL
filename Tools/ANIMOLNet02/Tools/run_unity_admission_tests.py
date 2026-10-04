#!/usr/bin/env python3
"""Isolated Unity/real HTTP test runner. No production config, art assets or credentials."""
import collections
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import threading
import uuid


def main():
    package = Path(__file__).resolve().parent.parent
    project = package.parent.parent
    spec = importlib.util.spec_from_file_location('net02_server_fixture', package / 'Server/server.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    counts = collections.Counter()
    with tempfile.TemporaryDirectory(prefix='ANIMOL-NET02-HTTP-') as directory:
        scratch = Path(directory)
        config = scratch / 'fixture.json'
        shutil.copyfile(package / 'ServerTests/fixture_config.json', config)
        fault = scratch / 'fault.txt'

        class ObservedStore(module.LobbyStore):
            def request(self, method, path, token, body):
                counts[path] += 1
                status, result = super().request(method, path, token, body)
                if path == '/v1/room/entry' and fault.exists():
                    mode = fault.read_text(encoding='utf-8-sig').strip()
                    fault.unlink()
                    # Mutate only the transmitted response AFTER the real SQLite transaction.
                    if mode == 'missing-response':
                        return 200, {}
                    if mode == 'tamper-receipt' and result.get('Status') == 'Accepted':
                        result = dict(result, AcceptedEntryIntent='TEST_WRONG_INTENT')
                return status, result

        store = ObservedStore(str(scratch / 'fixture.sqlite3'), str(config))
        server = module.LobbyHttpServer(('127.0.0.1', 0), store)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        environment = dict(os.environ, ANIMOL_NET02_TEST_URL=f'http://127.0.0.1:{server.server_port}',
                           ANIMOL_NET02_TEST_CONFIG=str(config), ANIMOL_NET02_TEST_FAULT=str(fault))
        unity = shutil.which('unity')
        if not unity:
            raise RuntimeError('Unity CLI is required')
        try:
            result = subprocess.run([unity, 'test', str(project), '--mode', 'PlayMode', '--filter',
                'NetUiAdmissionHttpTests;NetUiProjectBindingFlowTests;MissingMultiplayerFlowTests.ActualHub;'
                'MissingMultiplayerFlowTests.CodeInput;MissingMultiplayerFlowTests.FourUnknown;'
                'MissingMultiplayerFlowTests.Reentry;MissingMultiplayerFlowTests.FixturePending;'
                'MissingMultiplayerFlowTests.ExistingCoop;MultiplayerPhase3TransactionTests.RefusalReason;'
                'MultiplayerPhase3TransactionTests.InactivePending;MultiplayerPhase3TransactionTests.LateRead;'
                'MultiplayerPhase3TransactionTests.EachPermission;MultiplayerPhase3TransactionTests.MalformedContext',
                '--output', 'Logs/NET02-task04-play.xml', '--timeout', '240', '--format', 'json', '--',
                '-nographics', '-logFile', 'Logs/NET02-task04-play.log', '-animolNet02Slot', 'test_' + uuid.uuid4().hex[:20]],
                cwd=project, env=environment)
        finally:
            server.shutdown(); server.server_close(); thread.join(); store.close()
            (project / 'Logs/NET02-task04-http-counts.json').write_text(
                json.dumps({'scope': 'Isolated real HTTP/SQLite fixture; no production IDs',
                            'requests_by_endpoint': dict(counts)}, indent=2) + '\n', encoding='utf-8')
        return result.returncode


if __name__ == '__main__':
    raise SystemExit(main())
