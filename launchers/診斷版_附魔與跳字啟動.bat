@echo off
chcp 65001 >nul
title MapleTMS - enchant and damage trace
cd /d "%~dp0"

if not exist "Launcher.exe" (
  echo [ERROR] Launcher.exe not found.
  pause
  exit /b 1
)
if not exist "Hook.dll" (
  echo [ERROR] Hook.dll not found.
  pause
  exit /b 1
)
if not exist "enchant.dll" (
  echo [ERROR] enchant.dll not found.
  pause
  exit /b 1
)

echo Starting diagnostic client...
echo Tooltip hook is loaded by Launcher; attack and canvas traces are read-only.
start "" "Launcher.exe"
