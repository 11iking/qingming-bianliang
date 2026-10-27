#!/bin/bash
set -euo pipefail
PROJECT_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
GODOT_PATH="${GODOT_BIN:-/Applications/Godot.app/Contents/MacOS/Godot}"
if [ ! -x "$GODOT_PATH" ]; then
  printf '%s\n' "未找到 Godot。请安装 Godot.app 到 /Applications，或用 Godot 项目管理器导入本目录的 project.godot。" "按回车退出。"
  read -r _
  exit 1
fi
exec "$GODOT_PATH" --editor --path "$PROJECT_DIR"
