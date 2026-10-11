$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot "artifacts/portable-win-x64"
$archivePath = Join-Path $repoRoot "artifacts/Yazim-Asistani-1.0.0-portable-win-x64.zip"

if (Test-Path $outputPath) {
  Remove-Item $outputPath -Recurse -Force
}

if (Test-Path $archivePath) {
  Remove-Item $archivePath -Force
}

dotnet test (Join-Path $repoRoot "YazimAsistani.sln") -c Release
dotnet publish (Join-Path $repoRoot "src/YazimAsistani.Desktop/YazimAsistani.Desktop.csproj") `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishProfile=Portable `
  -o $outputPath

Get-ChildItem $outputPath -Filter "*.pdb" -Recurse | Remove-Item -Force
Compress-Archive -Path (Join-Path $outputPath "*") -DestinationPath $archivePath -CompressionLevel Optimal

Write-Host "Portable package: $outputPath"
Write-Host "Portable archive: $archivePath"

