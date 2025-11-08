#!/bin/bash

# Nakama Docker Setup Script for Chess Game
# This script sets up a local Nakama server for testing the chess game

echo "🚀 Setting up Nakama server for Chess Game..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Pull the latest Nakama image
echo "📥 Pulling Nakama Docker image..."
docker pull heroiclabs/nakama:3.22.0

# Create a directory for Nakama data
echo "📁 Creating data directory..."
mkdir -p ./nakama-data

# Start Nakama server
echo "🎮 Starting Nakama server..."
docker run -p 7350:7350 -p 7351:7351 -p 8080:8080 \
    -v $(pwd)/nakama-data:/nakama/data \
    heroiclabs/nakama:3.22.0

echo "✅ Nakama server is running!"
echo "🌐 Server URL: http://localhost:7350"
echo "📊 Dashboard: http://localhost:7351"
echo "🔧 Console: http://localhost:8080"
echo ""
echo "Press Ctrl+C to stop the server"
