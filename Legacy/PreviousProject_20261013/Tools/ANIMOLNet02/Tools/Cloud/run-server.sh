#!/usr/bin/env bash
set -euo pipefail
task_script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
task_package_root="$(cd -- "$task_script_dir/../.." && pwd)"
task_python="${ANIMOL_NET02_PYTHON:-python3}"
task_config="${ANIMOL_NET02_CONFIG:-$task_package_root/Server/config.empty.json}"
task_database="${ANIMOL_NET02_DB:-$task_package_root/Runtime/net02.sqlite3}"
task_bind="${ANIMOL_NET02_BIND:-127.0.0.1}"
task_port="${ANIMOL_NET02_PORT:-8080}"
if [[ ! "$task_port" =~ ^[0-9]+$ ]] || (( ${#task_port} > 5 )); then
  echo "ANIMOL_NET02_PORT must be numeric 1024-65535" >&2
  exit 2
fi
task_port_decimal=$((10#$task_port))
if (( task_port_decimal < 1024 || task_port_decimal > 65535 )); then
  echo "ANIMOL_NET02_PORT must be 1024-65535" >&2
  exit 2
fi
if [[ ! -f "$task_config" || ! -f "$task_package_root/Server/server.py" ]]; then
  echo "NET02 config or server source is missing" >&2
  exit 2
fi
exec "$task_python" "$task_package_root/Server/server.py" \
  --config "$task_config" --database "$task_database" --bind "$task_bind" --port "$task_port_decimal"
