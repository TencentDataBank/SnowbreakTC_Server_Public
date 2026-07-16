@echo off
setlocal
pushd "%~dp0"

echo ========================================
echo SnowbreakTC Server Clean
echo ========================================

echo Removing build caches (bin/obj)...
for /d /r . %%D in (obj) do if exist "%%D" rmdir /s /q "%%D"
for /d /r . %%D in (bin) do if exist "%%D" rmdir /s /q "%%D"

echo Done.
popd
pause
