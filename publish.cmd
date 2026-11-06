@echo off
rem Builds the one download: dist\GDPilot, zipped as dist\GDPilot.zip. Published by hand, never by a pipeline.
setlocal
set HERE=%~dp0
call "%HERE%src\GDPilot.InGame\build.cmd" || exit /b 1
if exist "%HERE%dist" rmdir /s /q "%HERE%dist"
dotnet publish "%HERE%src\GDPilot.App" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o "%HERE%dist\GDPilot" || exit /b 1
powershell -NoProfile -Command "Compress-Archive -Path '%HERE%dist\GDPilot\*' -DestinationPath '%HERE%dist\GDPilot.zip'" || exit /b 1
echo Built %HERE%dist\GDPilot.zip
