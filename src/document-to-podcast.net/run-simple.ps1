# Document to Podcast .NET - Quick Start Script
param(
    [Parameter(Mandatory=$true)]
    [string]$InputFile,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputFolder,
    
    [string]$ModelEndpoint = "http://localhost:11434"
)

Write-Host "Document to Podcast .NET - Quick Start" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green

# Check if input file exists
if (-not (Test-Path $InputFile)) {
    Write-Host "Input file not found: $InputFile" -ForegroundColor Red
    exit 1
}

Write-Host "Input file found: $InputFile" -ForegroundColor Green

# Create output folder if it doesn't exist
if (-not (Test-Path $OutputFolder)) {
    New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
    Write-Host "Created output folder: $OutputFolder" -ForegroundColor Green
}

# Build and run the project
Write-Host "Building and running project..." -ForegroundColor Cyan

try {
    dotnet run --input-file $InputFile --output-folder $OutputFolder --model-endpoint $ModelEndpoint
    Write-Host "Podcast generation completed!" -ForegroundColor Green
    Write-Host "Output files are in: $OutputFolder" -ForegroundColor Green
}
catch {
    Write-Host "Failed to generate podcast" -ForegroundColor Red
    exit 1
}

# List output files
Write-Host "Generated files:" -ForegroundColor Cyan
Get-ChildItem -Path $OutputFolder | ForEach-Object {
    Write-Host "  * $($_.Name)" -ForegroundColor White
}
