@echo off
cd /d "%~dp0"

echo [1/3] Verificando DevLauncher.exe...
if not exist "DevLauncher.exe" (
    "%USERPROFILE%\.dotnet\dotnet.exe" publish src -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%~dp0"
    if errorlevel 1 (
        echo Error al compilar DevLauncher.exe
        pause
        exit /b 1
    )
)

echo [2/3] Generando payload.zip...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0installer\package_payload.ps1"
if errorlevel 1 (
    echo Error al crear el archivo comprimido.
    pause
    exit /b 1
)

echo [3/3] Compilando instalador...
"%USERPROFILE%\.dotnet\dotnet.exe" publish installer -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%~dp0"
if errorlevel 1 (
    echo Error al compilar el instalador.
    pause
    exit /b 1
)

if exist "installer\payload.zip" del /q "installer\payload.zip"
if exist "DevLauncherInstaller.pdb" del /q "DevLauncherInstaller.pdb"
if exist "installer\bin" rd /s /q "installer\bin"
if exist "installer\obj" rd /s /q "installer\obj"

echo.
echo Build completada: DevLauncherInstaller.exe
pause
