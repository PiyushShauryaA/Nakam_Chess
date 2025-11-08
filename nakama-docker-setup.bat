@echo off
echo 🚀 Setting up Nakama server for Chess Game...

REM Check if Docker is running
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Docker is not running. Please start Docker and try again.
    pause
    exit /b 1
)

REM Pull the latest Nakama image
echo 📥 Pulling Nakama Docker image...
docker pull heroiclabs/nakama:3.22.0

REM Create a directory for Nakama data
echo 📁 Creating data directory...
if not exist "nakama-data" mkdir nakama-data

REM Start Nakama server
echo 🎮 Starting Nakama server...
docker run -p 7350:7350 -p 7351:7351 -p 8080:8080 -v "%cd%/nakama-data:/nakama/data" heroiclabs/nakama:3.22.0

echo ✅ Nakama server is running!
echo 🌐 Server URL: http://localhost:7350
echo 📊 Dashboard: http://localhost:7351
echo 🔧 Console: http://localhost:8080
echo.
echo Press Ctrl+C to stop the server
pause
