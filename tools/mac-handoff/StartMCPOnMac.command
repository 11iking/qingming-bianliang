#!/bin/bash
set -euo pipefail
root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
server_dir="$root_dir/tools/unity-mcp/Server"
[[ -x "$server_dir/.venv/bin/mcp-for-unity" ]] || { echo "请先运行 SetupMCPOnMac.command。" >&2; exit 1; }
export PYTHONUTF8=1
cd "$server_dir"
exec "$server_dir/.venv/bin/mcp-for-unity" --transport stdio
