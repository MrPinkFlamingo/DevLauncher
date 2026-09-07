param()

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$zipPath = Join-Path $PSScriptRoot "payload.zip"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

$filesToCompress = @()

$exePath = Join-Path $root "DevLauncher.exe"
if (Test-Path $exePath) {
    $filesToCompress += $exePath
}

$icoPath = Join-Path $root "icon.ico"
if (Test-Path $icoPath) {
    $filesToCompress += $icoPath
}

$configPath = Join-Path $root "launcher_config.json"
if (Test-Path $configPath) {
    $filesToCompress += $configPath
}

Write-Host "Comprimiendo archivos para el instalador:"
$filesToCompress | ForEach-Object { Write-Host " - $(Split-Path $_ -Leaf)" }

Compress-Archive -Path $filesToCompress -DestinationPath $zipPath -CompressionLevel Optimal

if (Test-Path $zipPath) {
    $sizeMb = [math]::Round(((Get-Item $zipPath).Length / 1MB), 2)
    Write-Host "Paquete comprimido exitosamente: payload.zip ($sizeMb MB)"
    exit 0
} else {
    Write-Error "No se pudo generar payload.zip"
    exit 1
}
