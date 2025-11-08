# 🚀 Nakama Unity SDK Installation Guide

## 🎯 Problem
Your "Connect and Join" button isn't working because the project is using placeholder Nakama types instead of the real Nakama Unity SDK.

## 🔧 Solution
We need to install the real Nakama Unity SDK to enable actual multiplayer functionality.

## 📦 Installation Methods

### **Method 1: Unity Package Manager (Recommended)**

1. **Open Unity Package Manager:**
   - Window → Package Manager
   - Click the "+" button in top-left
   - Select "Add package from git URL..."

2. **Add Nakama Package:**
   ```
   https://github.com/heroiclabs/nakama-unity.git?path=/Nakama
   ```

3. **Wait for Installation:**
   - Unity will download and install the package
   - This may take a few minutes

### **Method 2: Manual Installation**

1. **Download Nakama Unity SDK:**
   - Go to: https://github.com/heroiclabs/nakama-unity/releases
   - Download the latest release (3.22.0)
   - Extract the zip file

2. **Copy to Unity Project:**
   - Copy the `Nakama` folder from the extracted files
   - Paste it into your `Assets/` folder
   - Unity will automatically import the scripts

### **Method 3: OpenUPM (Alternative)**

1. **Install OpenUPM CLI:**
   ```bash
   npm install -g @openupm/cli
   ```

2. **Add Nakama Package:**
   ```bash
   openupm add com.heroiclabs.nakama-unity
   ```

## 🔄 After Installation

### **Step 1: Update NakamaManager.cs**
Replace the placeholder imports with real Nakama imports:

```csharp
// Remove this line:
// Note: Nakama types are provided by NakamaPlaceholder.cs until SDK is installed

// Add these imports:
using Nakama;
using Nakama.TinyJson;
```

### **Step 2: Remove Placeholder**
Delete the `NakamaPlaceholder.cs` file after SDK installation.

### **Step 3: Test Connection**
1. Open both Unity instances
2. Click "Connect and Join" on both
3. Players should find each other through Nakama server

## 🎯 Expected Behavior After Installation

### **Before (Current Issue):**
- ❌ Players can't find each other
- ❌ Using placeholder types
- ❌ No real server connection

### **After (Fixed):**
- ✅ Players find each other instantly
- ✅ Real Nakama server connection
- ✅ Actual multiplayer functionality

## 🚀 Quick Test

1. **Install Nakama SDK** using Method 1 (Package Manager)
2. **Update NakamaManager.cs** with real imports
3. **Delete NakamaPlaceholder.cs**
4. **Test with two Unity instances:**
   - Instance 1: Enter username "Player1", click "Connect and Join"
   - Instance 2: Enter username "Player2", click "Connect and Join"
   - Both should find each other and start multiplayer game

## 🔧 Troubleshooting

### **If Package Manager Method Fails:**
- Try Method 2 (Manual Installation)
- Ensure you have internet connection
- Check Unity console for errors

### **If Players Still Don't Connect:**
- Verify Nakama server is running (`http://localhost:7350`)
- Check Unity console for connection errors
- Ensure both instances are using the same server settings

## 📝 Next Steps

After successful installation:
1. **Test multiplayer connection**
2. **Verify matchmaking works**
3. **Test game synchronization**
4. **Remove placeholder files**

Your multiplayer chess game will then work perfectly! 🎮
