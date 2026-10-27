@echo off
chcp 65001 >nul
if not exist "%~dp0build-unity\BianliangUnity.exe" (
  echo 未找到 Unity 版程序，请在编辑器的 Qingming 菜单中重新打包。
  pause
  exit /b 1
)
start "" "%~dp0build-unity\BianliangUnity.exe"
