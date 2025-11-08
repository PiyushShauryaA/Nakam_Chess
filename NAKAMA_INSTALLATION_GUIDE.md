# 🚀 Nakama Unity SDK Installation Guide

## ⚠️ Current Status

Your project is currently using **placeholder Nakama types** to prevent compilation errors. You need to install the actual Nakama Unity SDK to enable full functionality.

## 📦 Installation Methods

### Method 1: Package Manager (Recommended)

1. **Open Unity Package Manager:**
   - Go to `Window > Package Manager`

2. **Add Package from Git URL:**
   - Click the `+` button in the top-left
   - Select `Add package from git URL...`
   - Enter: `https://github.com/heroiclabs/nakama-unity.git?path=/Nakama`
   - Click `Add`

3. **Wait for Installation:**
   - Unity will download and install the package
   - This may take a few minutes

### Method 2: Manual Download

1. **Download the SDK:**
   - Go to: https://github.com/heroiclabs/nakama-unity/releases
   - Download the latest release (v3.22.0)

2. **Extract to Project:**
   - Extract the downloaded zip file
   - Copy the `Nakama` folder to `Assets/Plugins/` in your project
   - If the `Plugins` folder doesn't exist, create it

3. **Refresh Unity:**
   - Press `Ctrl+R` (or `Cmd+R` on Mac) to refresh
   - Unity should recognize the new files

### Method 3: Using OpenUPM CLI

1. **Install OpenUPM CLI:**
   ```bash
   npm install -g openupm-cli
   ```

2. **Add Nakama Package:**
   ```bash
   openupm add com.heroiclabs.nakama-unity
   ```

## 🔧 After Installation

### Step 1: Remove Placeholder Files

Once the Nakama SDK is installed, delete these placeholder files:
- `Assets/Scripts/NakamaPlaceholder.cs`

### Step 2: Update Using Statements

The scripts will automatically use the real Nakama types instead of placeholders.

### Step 3: Test Installation

1. **Check for Compilation Errors:**
   - Open Unity Console (`Window > General > Console`)
   - Look for any red error messages
   - All Nakama-related errors should be resolved

2. **Verify Namespace:**
   - The `Nakama` namespace should now be available
   - You should see `IMatchData`, `IClient`, `ISocket`, etc. types

## 🎮 Next Steps

After successful installation:

1. **Create NakamaConfig:**
   - Right-click in Project window
   - `Create > Chess > Nakama Config`
   - Configure your server settings

2. **Set Up Nakama Server:**
   - Run `nakama-docker-setup.bat` to start local server
   - Or configure your own Nakama server

3. **Update Scene References:**
   - Use `Tools > Chess > Migrate Photon to Nakama`
   - Follow the migration steps

## 🐛 Troubleshooting

### Package Manager Issues

**Error: "Package cannot be found"**
- Try Method 2 (Manual Download) instead
- Ensure you have internet connection
- Check Unity version compatibility

### Compilation Errors

**Error: "IMatchData could not be found"**
- Ensure Nakama SDK is properly installed
- Check that `NakamaPlaceholder.cs` is still present (should be deleted after real SDK install)
- Try refreshing Unity (`Ctrl+R`)

**Error: "UpdateBoardRotation cannot override"**
- This should be fixed - the base method is now virtual
- If still occurring, check GameManager.cs line 1416

### Missing Dependencies

**Error: "Assembly reference missing"**
- Try Method 1 (Package Manager) for automatic dependency resolution
- Ensure Unity version is compatible (2022.3 LTS or newer recommended)

## 📚 Verification Checklist

After installation, verify:

- [ ] No compilation errors in Unity Console
- [ ] `Nakama` namespace is available
- [ ] `NakamaPlaceholder.cs` is deleted
- [ ] Can create `NakamaConfig` asset
- [ ] NakamaManager script compiles without errors
- [ ] GameManagerNakama script compiles without errors

## 🆘 Still Having Issues?

If you're still experiencing problems:

1. **Check Unity Version:**
   - Nakama Unity SDK requires Unity 2021.3 or newer
   - Unity 2022.3 LTS is recommended

2. **Clean Installation:**
   - Delete `Library` folder in your project
   - Reopen Unity project
   - Try installation again

3. **Alternative Approach:**
   - Use the placeholder system for now
   - The game will work in single-player mode
   - Install Nakama SDK later when ready for multiplayer

## 📞 Support Resources

- [Nakama Unity SDK Documentation](https://heroiclabs.com/docs/nakama/client-libraries/unity/)
- [Nakama GitHub Repository](https://github.com/heroiclabs/nakama-unity)
- [Unity Package Manager Guide](https://docs.unity3d.com/Manual/upm-ui.html)

---

**Note:** The placeholder system allows your project to compile and run in single-player mode while you set up the full Nakama SDK for multiplayer functionality.
