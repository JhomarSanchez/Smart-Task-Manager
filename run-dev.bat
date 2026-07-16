@echo off
title Smart Task Manager Launcher
echo =====================================================================
echo           Smart Task Manager - Developer Services Launcher
echo =====================================================================
echo.

:: Determine correct paths relative to the script location
set "SCRIPT_DIR=%~dp0"
set "TASK_DIR=%SCRIPT_DIR%task"

if not exist "%TASK_DIR%\SmartTask.slnx" (
    set "TASK_DIR=%SCRIPT_DIR%"
)

set "BACKEND_DIR=%TASK_DIR%\src\Backend\SmartTask.Api"
set "FRONTEND_DIR=%TASK_DIR%\src\Frontend"

:: 1. Verify and Start PostgreSQL Docker Container
echo [1/3] Verifying PostgreSQL Docker container...
docker start smarttask-postgres >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [INFO] PostgreSQL container 'smarttask-postgres' not found or stopped.
    echo [INFO] Attempting to launch a new container...
    docker run --name smarttask-postgres -e POSTGRES_DB=smarttask -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -v smarttask_data:/var/lib/postgresql/data -d postgres:16-alpine
    if %ERRORLEVEL% NEQ 0 (
        echo [WARNING] Failed to start Docker container. Ensure Docker Desktop is active.
    )
) else (
    echo [OK] PostgreSQL container started successfully.
)
echo.

:: 2. Launch Backend API in a separate CMD window
echo [2/3] Starting ASP.NET Core Web API...
start "SmartTask Backend API (Port 5100)" /D "%BACKEND_DIR%" cmd /k "dotnet run"
echo [OK] Backend initialization window triggered.

:: 3. Launch Frontend Angular App in a separate CMD window (disabling interactive analytics prompt)
echo [3/3] Starting Angular Web Application...
start "SmartTask Frontend Web (Port 4200)" /D "%FRONTEND_DIR%" cmd /k "set NG_CLI_ANALYTICS=false&& npm start"
echo [OK] Frontend compilation window triggered.
echo.

echo =====================================================================
echo  All services have been initiated.
echo  - Backend API:  http://localhost:5100
echo  - Frontend Web: http://localhost:4200
echo =====================================================================
echo.
pause
