@echo off
cd /d "%~dp0"

echo Compilando DevLauncher...
"%USERPROFILE%\.dotnet\dotnet.exe" publish src -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%~dp0"

if exist "DevLauncher.pdb" del /q "DevLauncher.pdb"
if exist "src\bin" rd /s /q "src\bin"
if exist "src\obj" rd /s /q "src\obj"

if %errorlevel% equ 0 (
    echo Compilacion exitosa: DevLauncher.exe
) else (
    echo Error durante la compilacion.
)
pause
