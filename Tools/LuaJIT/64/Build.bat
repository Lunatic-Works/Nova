@echo off
cd /d %~dp0

if not exist jit (mkdir jit)
if not exist Out (mkdir Out)

xcopy /dqy ..\..\..\Tools\LuaJIT\jit jit
setlocal enabledelayedexpansion

for /r %%i in (*.lua) do (
    set v=%%~dpi
    call :loop
    set v=!v:%~dp0=!
    if not exist %~dp0out\!v! (mkdir %~dp0Out\!v!)
)

for /r %%i in (*.lua) do (
    set v=%%i
    set v=!v:%~dp0=!
    call :loop
    ..\..\..\Tools\LuaJIT\64\luajit64.exe -b !v! Out\!v!.bytes
)

rd /sq jit
rd /sq Out\jit
setlocal disabledelayedexpansion

:loop
if !v:~-1!==" " (set v=!v:~0,-1! & goto loop)
