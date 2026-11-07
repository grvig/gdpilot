@echo off
rem Builds GDPilot.Loader.dll, the drop-in loader that "GDPilot.exe --install-loader" puts in the game's folder as dinput8.dll.
setlocal
set HERE=%~dp0
call "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat" >nul 2>&1 || exit /b 1
if not exist "%HERE%bin" mkdir "%HERE%bin"
if not exist "%HERE%obj" mkdir "%HERE%obj"
cl /nologo /std:c++20 /EHsc /O2 /W4 /WX /MT /LD /DUNICODE /D_UNICODE ^
   /Fo"%HERE%obj\\" "%HERE%Loader.cpp" ^
   /Fe"%HERE%bin\GDPilot.Loader.dll" ^
   /link /NOLOGO /DEF:"%HERE%Loader.def" shell32.lib ole32.lib user32.lib || exit /b 1
