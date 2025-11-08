# 🚀 Nakama Server Start Guide

## ✅ Current Status: SERVER IS RUNNING!

Your Nakama server is already running and working perfectly! Here's the current status:

### **🟢 Server Status: ACTIVE**
- ✅ **Nakama Server** - Running on port 7350
- ✅ **Database** - CockroachDB running on port 26257
- ✅ **Monitoring** - Prometheus running on port 9090
- ✅ **Health Check** - Server responding with 200 OK

### **🌐 Server URLs:**
- **Game Server:** `http://localhost:7350` ✅
- **Dashboard:** `http://localhost:7351` ✅
- **Console:** `http://localhost:8080` ✅
- **Prometheus:** `http://localhost:9090` ✅

## 🔧 How to Start Nakama Server

### **Method 1: Using Docker Compose (Recommended)**

1. **Navigate to your project directory:**
   ```bash
   cd D:\UnityProjects\NakamaProj\Chess
   ```

2. **Start the server:**
   ```bash
   docker-compose up -d
   ```

3. **Check status:**
   ```bash
   docker ps
   ```

### **Method 2: Using the Setup Script**

1. **Run the setup script:**
   ```bash
   .\nakama-docker-setup.bat
   ```

2. **Follow the prompts** to start the server

### **Method 3: Manual Docker Commands**

1. **Start Nakama server:**
   ```bash
   docker run -p 7350:7350 -p 7351:7351 heroiclabs/nakama:3.22.0
   ```

2. **Start with database:**
   ```bash
   docker run -p 7350:7350 -p 7351:7351 -p 8080:8080 -p 26257:26257 heroiclabs/nakama:3.22.0
   ```

## 🔍 How to Check Server Status

### **Check if Server is Running:**
```bash
docker ps
```

### **Test Server Connection:**
```bash
curl http://localhost:7350
```

### **Check Server Health:**
```bash
curl http://localhost:7351
```

## 🛠️ Server Management Commands

### **Start Server:**
```bash
docker-compose up -d
```

### **Stop Server:**
```bash
docker-compose down
```

### **Restart Server:**
```bash
docker-compose restart
```

### **View Server Logs:**
```bash
docker-compose logs -f nakama
```

### **Check Server Status:**
```bash
docker-compose ps
```

## 🎮 Testing Your Server

### **1. Test Basic Connection:**
```bash
curl http://localhost:7350
```
**Expected:** `HTTP/1.1 200 OK`

### **2. Test Dashboard:**
Open browser: `http://localhost:7351`
**Expected:** Nakama dashboard interface

### **3. Test Console:**
Open browser: `http://localhost:8080`
**Expected:** CockroachDB console

### **4. Test Prometheus:**
Open browser: `http://localhost:9090`
**Expected:** Prometheus monitoring interface

## 🔧 Troubleshooting

### **If Server Won't Start:**

1. **Check Docker is running:**
   ```bash
   docker --version
   ```

2. **Check if ports are available:**
   ```bash
   netstat -an | findstr :7350
   ```

3. **Check Docker logs:**
   ```bash
   docker-compose logs nakama
   ```

### **If Server is Slow:**

1. **Check system resources:**
   ```bash
   docker stats
   ```

2. **Restart containers:**
   ```bash
   docker-compose restart
   ```

### **If Connection Fails:**

1. **Verify server is running:**
   ```bash
   docker ps
   ```

2. **Check firewall settings**
3. **Verify port 7350 is not blocked**

## 📊 Server Configuration

### **Current Settings:**
- **Host:** localhost
- **Port:** 7350
- **Server Key:** defaultkey
- **Database:** CockroachDB
- **Monitoring:** Prometheus

### **Unity Project Settings:**
- **Scheme:** http
- **Host:** localhost
- **Port:** 7350
- **Server Key:** defaultkey

## 🎯 Next Steps

### **Your Server is Ready!**

1. **✅ Server is running** - No action needed
2. **✅ Unity project is ready** - All scripts compiled
3. **✅ Configuration is set** - NakamaConfig asset ready
4. **🚀 Ready to test multiplayer** - Install Nakama Unity SDK

### **To Test Multiplayer:**
1. **Install Nakama Unity SDK** in Unity
2. **Open two Unity instances** of your project
3. **Test "Connect and Join"** functionality
4. **Verify players can find each other**

## 🏆 Summary

**Your Nakama server is already running perfectly!**

- ✅ **Server Status:** ACTIVE
- ✅ **Health Check:** PASSED
- ✅ **All Services:** RUNNING
- ✅ **Ready for Unity:** YES

**No action needed - your server is ready for multiplayer testing!** 🎮
