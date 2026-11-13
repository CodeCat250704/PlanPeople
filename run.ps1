$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "=== 1. Build App ===" -ForegroundColor Cyan
dotnet build src/PlanPeople.App/PlanPeople.App.csproj -c Debug
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed"; exit }

Write-Host "=== 2. Publish Splash ===" -ForegroundColor Cyan
# 强杀进程并清理
Stop-Process -Name "PlanPeople.Splash" -ErrorAction SilentlyContinue
Stop-Process -Name "PlanPeople.App" -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500 # 给系统一点时间释放文件句柄

if (Test-Path "publish/Splash") { Remove-Item -Path "publish/Splash" -Recurse -Force -ErrorAction SilentlyContinue }
dotnet publish src/PlanPeople.Splash/PlanPeople.Splash.csproj -c Release -o publish/Splash
if ($LASTEXITCODE -ne 0) { Write-Host "Publish failed"; exit }

Write-Host "=== 3. Copy Splash to App Debug dir ===" -ForegroundColor Cyan
$appDebugDir = "src/PlanPeople.App/bin/Debug/net8.0"
if (-not (Test-Path $appDebugDir)) {
    New-Item -ItemType Directory -Path $appDebugDir | Out-Null
}
Copy-Item -Path "publish/Splash/*" -Destination $appDebugDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "=== 4. Run App ===" -ForegroundColor Green
dotnet run --no-build --project src/PlanPeople.App/PlanPeople.App.csproj