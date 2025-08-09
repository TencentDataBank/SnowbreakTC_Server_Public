@echo off
echo ========================================
echo SnowbreakTC Server 构建脚本
echo ========================================

echo 正在还原 NuGet 包...
dotnet restore

if %errorlevel% neq 0 (
    echo 还原包失败！
    pause
    exit /b %errorlevel%
)

echo 正在构建解决方案...
dotnet build --configuration Release --no-restore

if %errorlevel% neq 0 (
    echo 构建失败！
    pause
    exit /b %errorlevel%
)

echo 构建成功！
echo 可执行文件位置: src\SnowbreakTC.Server\bin\Release\net8.0\SnowbreakTC.Server.exe
pause