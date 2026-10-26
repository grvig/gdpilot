@echo off
rem Builds GDPilot.InGame.dll, the part of GDPilot that loads into Grim Dawn.
rem Needs the Visual Studio 2022 Build Tools with the C++ workload.
setlocal
set HERE=%~dp0
call "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat" >nul 2>&1 || exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "%HERE%fetch-detours.ps1" || exit /b 1
set DETOURS=%HERE%external\Detours-e4bfd6b03e50de46b47abfbd1e46b384f0c5f833
if not exist "%HERE%bin" mkdir "%HERE%bin"
if not exist "%HERE%obj" mkdir "%HERE%obj"
cl /nologo /std:c++20 /EHsc /O2 /W4 /WX /MT /LD /DUNICODE /D_UNICODE ^
   /I "%DETOURS%\include" /Fo"%HERE%obj\\" ^
   "%HERE%InGame.cpp" ^
   /Fe"%HERE%bin\GDPilot.InGame.dll" ^
   /link /NOLOGO "%DETOURS%\lib.X64\detours.lib" || exit /b 1
