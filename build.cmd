@echo off
setlocal
pushd "%~dp0"

echo ========================================
echo SnowbreakTC Server Build
echo ========================================

echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo Restore failed.
    popd
    pause
    exit /b %errorlevel%
)

echo Building solution (Release)...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo Build failed.
    popd
    pause
    exit /b %errorlevel%
)

echo Build succeeded!
echo Output: src\SnowbreakTC.Server\bin\Release\net8.0\SnowbreakTC.Server.exe
popd
pause
