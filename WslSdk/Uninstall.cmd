@echo off
pushd "%~dp0"

%windir%\system32\net.exe session >nul 2>&1
if not %errorLevel% == 0 (
  echo Failure: Current permissions inadequate
  pause
  exit 1
)

if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
  set regasm=%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe
) else (
  echo Failure: Unsupported platform
  pause
  exit 2
)

echo Register the component.
%regasm% /unregister WslSdk.exe
pause

:exit
popd
@echo on
