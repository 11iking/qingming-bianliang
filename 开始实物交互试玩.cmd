@echo off
chcp 65001 >nul
if not exist "%~dp0build-unity\BianliangUnity.exe" (
  echo 未找到游戏，请使用“开始体验Unity版.cmd”检查安装。
  pause
  exit /b 1
)
start "" "%~dp0build-unity\BianliangUnity.exe" -hands-preview
