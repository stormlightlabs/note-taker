# NoteTaker Build Script for Windows
# Usage: .\build.ps1 -Platform "win-x64" -Configuration "Release"

param(
    [string]$Platform = "win-x64",
    [string]$Configuration = "Release"
)

$ProjectDir = "NoteTaker.GUI"
$OutputDir = "dist"

Write-Host "Building NoteTaker for $Platform in $Configuration configuration..." -ForegroundColor Green

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "$OutputDir\$Platform") {
    Remove-Item -Recurse -Force "$OutputDir\$Platform"
}
dotnet clean "$ProjectDir\NoteTaker.GUI.fsproj" -c $Configuration

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore "$ProjectDir\NoteTaker.GUI.fsproj"

# Build and publish
Write-Host "Publishing application..." -ForegroundColor Yellow
dotnet publish "$ProjectDir\NoteTaker.GUI.fsproj" `
    -c $Configuration `
    -r $Platform `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$OutputDir\$Platform"

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "Output directory: $OutputDir\$Platform" -ForegroundColor Cyan
