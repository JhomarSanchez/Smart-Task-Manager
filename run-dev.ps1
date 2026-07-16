Write-Host "=====================================================================" -ForegroundColor Green
Write-Host "          Smart Task Manager - Developer Services Launcher" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host ""

# Determine correct paths relative to the script location
$ScriptDir = $PSScriptRoot
if (!$ScriptDir) {
    $ScriptDir = Get-Location
}

$TaskDir = Join-Path $ScriptDir "task"
if (!(Test-Path (Join-Path $TaskDir "SmartTask.slnx"))) {
    # If the subfolder 'task' doesn't contain the solution, we are already inside the 'task' folder
    $TaskDir = $ScriptDir
}

$BackendProj = Join-Path $TaskDir "src/Backend/SmartTask.Api"
$FrontendDir = Join-Path $TaskDir "src/Frontend"

# 1. Verify and Start PostgreSQL Docker Container
Write-Host "[1/3] Verifying PostgreSQL Docker container..." -ForegroundColor Cyan
docker start smarttask-postgres 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "[INFO] PostgreSQL container 'smarttask-postgres' not found. Attempting new run..." -ForegroundColor Yellow
    docker run --name smarttask-postgres -e POSTGRES_DB=smarttask -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -v smarttask_data:/var/lib/postgresql/data -d postgres:16-alpine
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[WARNING] Failed to start Docker container. Verify Docker Desktop status." -ForegroundColor Red
    }
} else {
    Write-Host "[OK] PostgreSQL container started successfully." -ForegroundColor Green
}
Write-Host ""

# 2. Launch Backend API in a separate CMD window
Write-Host "[2/3] Starting ASP.NET Core Web API..." -ForegroundColor Cyan
Start-Process cmd -ArgumentList "/k dotnet run" -WorkingDirectory $BackendProj
Write-Host "[OK] Backend window triggered." -ForegroundColor Green

# 3. Launch Frontend Web App in a separate CMD window (disabling interactive analytics prompt)
Write-Host "[3/3] Starting Angular Web Application..." -ForegroundColor Cyan
Start-Process cmd -ArgumentList "/k set NG_CLI_ANALYTICS=false&& npm start" -WorkingDirectory $FrontendDir
Write-Host "[OK] Frontend window triggered." -ForegroundColor Green
Write-Host ""

Write-Host "=====================================================================" -ForegroundColor Green
Write-Host " All services triggered in separate windows." -ForegroundColor Green
Write-Host " - Backend API:  http://localhost:5100" -ForegroundColor Cyan
Write-Host " - Frontend Web: http://localhost:4200" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host ""
