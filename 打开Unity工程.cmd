@echo off
chcp 65001 >nul
set "QINGMING_EDITOR=C:\Users\lty06\Downloads\Editor\Tuanjie.exe"
if not exist "%QINGMING_EDITOR%" (
  echo 未找到团结引擎编辑器，请先完成安装。
  pause
  exit /b 1
)
start "" "%QINGMING_EDITOR%" -projectPath "%~dp0UnityProject" -logFile "%~dp0tools\unity-port\Editor.log"
