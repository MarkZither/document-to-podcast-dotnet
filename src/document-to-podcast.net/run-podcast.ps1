# Document to Podcast .NET - Quick Start Script

param(
    [Parameter(Mandatory=$true)]
    [string]$InputFile,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputFolder,
    
    [string]$ModelEndpoint = "http://localhost:11434",
    
    [string]$ConfigFile = ""
)

Write-Host "Document to Podcast .NET - Quick Start" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green

# Check if .NET is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 9.0 SDK" -ForegroundColor Red
    exit 1
}

# Check if input file exists
if (-not (Test-Path $InputFile)) {
    Write-Host "✗ Input file not found: $InputFile" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Input file found: $InputFile" -ForegroundColor Green

# Create output folder if it doesn't exist
if (-not (Test-Path $OutputFolder)) {
    New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
    Write-Host "✓ Created output folder: $OutputFolder" -ForegroundColor Green
}
else {
    Write-Host "✓ Output folder exists: $OutputFolder" -ForegroundColor Green
}

# Test if model endpoint is available
try {
    $response = Invoke-WebRequest -Uri "$ModelEndpoint/v1/models" -Method GET -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✓ Model endpoint is available: $ModelEndpoint" -ForegroundColor Green
}
catch {
    Write-Host "⚠ Model endpoint not available: $ModelEndpoint" -ForegroundColor Yellow
    Write-Host "  The application will use placeholder content instead" -ForegroundColor Yellow
}

# Build the project
Write-Host "`nBuilding project..." -ForegroundColor Cyan
try {
    dotnet build --configuration Release --verbosity quiet
    Write-Host "✓ Project built successfully" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to build project" -ForegroundColor Red
    exit 1
}

# Run the application
Write-Host "`nRunning document-to-podcast..." -ForegroundColor Cyan

$arguments = @("--input-file", $InputFile, "--output-folder", $OutputFolder, "--model-endpoint", $ModelEndpoint)

if ($ConfigFile -ne "") {
    $arguments = @("--config-file", $ConfigFile)
}

try {
    dotnet run --configuration Release -- @arguments
    Write-Host "`n✓ Podcast generation completed!" -ForegroundColor Green
    Write-Host "Output files are in: $OutputFolder" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to generate podcast" -ForegroundColor Red
    exit 1
}

# List output files
Write-Host "`nGenerated files:" -ForegroundColor Cyan
Get-ChildItem -Path $OutputFolder | ForEach-Object {
    Write-Host "  * $($_.Name)" -ForegroundColor White
}
