@echo off
setlocal
cd /d "%~dp0"

taskkill /IM Poe2Rumours.exe /F >nul 2>&1

dotnet publish src\Rumours.App -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -o dist\_build || exit /b 1

copy /Y dist\_build\Poe2Rumours.exe dist\ >nul
rmdir /S /Q dist\_build

echo Done: %~dp0dist\Poe2Rumours.exe
certutil -hashfile dist\Poe2Rumours.exe SHA256 | findstr /v ":"
