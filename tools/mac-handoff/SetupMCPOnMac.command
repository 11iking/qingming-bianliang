#!/bin/bash
set -euo pipefail
root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
[[ "$(uname -s)" == "Darwin" ]] || { echo "This setup is for macOS." >&2; exit 1; }
command -v uv >/dev/null 2>&1 || { echo "请先安装 uv 和 Python 3.10+，然后重新运行。" >&2; exit 1; }
cd "$root_dir/tools/unity-mcp/Server"
uv sync --frozen
echo "Mac MCP 环境已安装。客户端命令为 /bin/bash，参数使用本包 StartMCPOnMac.command 的绝对路径。"
