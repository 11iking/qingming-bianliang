#!/bin/bash
set -euo pipefail
root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [[ "$(uname -s)" != "Darwin" ]]; then echo "This launcher is for macOS." >&2; exit 1; fi
editor_path="${UNITY_EDITOR_PATH:-}"
if [[ -z "$editor_path" ]]; then
  editors=()
  for candidate in /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity /Applications/Unity/Unity.app/Contents/MacOS/Unity; do
    [[ -x "$candidate" ]] && editors+=("$candidate")
  done
  if [[ ${#editors[@]} -eq 0 ]]; then echo "请先在 Unity Hub 安装 Apple silicon 编辑器，或设置 UNITY_EDITOR_PATH。"; exit 1; fi
  for ((i=0;i<${#editors[@]};i++)); do printf '%d) %s\n' "$((i+1))" "${editors[$i]}"; done
  echo "首次恢复桌面基线建议选 2022.3 LTS；升级 Unity 6 请在工程副本进行。"
  read -r -p "输入编辑器序号：" choice
  [[ "$choice" =~ ^[0-9]+$ ]] || exit 1
  [[ "$choice" -ge 1 && "$choice" -le "${#editors[@]}" ]] || exit 1
  editor_path="${editors[$((choice-1))]}"
fi
[[ -x "$editor_path" ]] || { echo "找不到所选编辑器。" >&2; exit 1; }
exec "$editor_path" -projectPath "$root_dir/UnityProject" "$@"
