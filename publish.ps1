$ErrorActionPreference = "Stop"

Write-Host "=== Cleaning dist folder ===" -ForegroundColor Cyan
if (Test-Path "dist") { Remove-Item -Path "dist" -Recurse -Force }
New-Item -ItemType Directory -Path "dist" | Out-Null

Write-Host "=== 1. Publishing Main App ===" -ForegroundColor Cyan
dotnet publish src/PlanPeople.App/PlanPeople.App.csproj -c Release -o dist
if ($LASTEXITCODE -ne 0) { Write-Host "Main App publish failed"; exit }

Write-Host "=== 2. Publishing Splash ===" -ForegroundColor Cyan
dotnet publish src/PlanPeople.Splash/PlanPeople.Splash.csproj -c Release -o publish/Splash
if ($LASTEXITCODE -ne 0) { Write-Host "Splash publish failed"; exit }

Write-Host "=== 3. Assembling final bundle ===" -ForegroundColor Cyan
Copy-Item -Path "publish/Splash/PlanPeople.Splash.exe" -Destination "dist/PlanPeople.Splash.exe" -Force

Write-Host "=== 4. Cleaning up temporary files ===" -ForegroundColor Cyan
Remove-Item -Path "dist/*.pdb" -ErrorAction SilentlyContinue
Remove-Item -Path "dist/config.json" -ErrorAction SilentlyContinue

Write-Host "=== Publish Successful! ===" -ForegroundColor Green
Write-Host "Check the 'dist' folder for your final executable." -ForegroundColor Yellow