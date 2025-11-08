# Photon to Nakama Migration Guide

This guide will help you migrate your Unity Chess game from Photon PUN2 to Nakama for multiplayer functionality.

## 🚀 Quick Start

### 1. Install Nakama Unity SDK
The Nakama Unity SDK has been added to your `Packages/manifest.json`. Unity will automatically install it when you open the project.

### 2. Create Nakama Configuration
1. Right-click in the Project window
2. Navigate to `Create > Chess > Nakama Config`
3. Name it `NakamaConfig`
4. Configure your Nakama server settings:
   - **Scheme**: `http` or `https`
   - **Host**: `localhost` (or your Nakama server address)
   - **Port**: `7350` (default Nakama port)
   - **Server Key**: `defaultkey` (or your server key)

### 3. Set Up Nakama Server
You can run Nakama locally using Docker:

```bash
# Download and run Nakama
docker run -p 7350:7350 -p 7351:7351 -p 8080:8080 heroiclabs/nakama:3.22.0
```

## 📁 New Scripts Added

### Core Nakama Scripts
- **`NakamaManager.cs`** - Replaces `PhotonSimpleConnect.cs`
- **`GameManagerNakama.cs`** - Extends `GameManager` with Nakama multiplayer
- **`UIManagerNakama.cs`** - Extends `UIManager` with Nakama support
- **`NakamaConfig.cs`** - Configuration scriptable object for server settings

### Migration Tools
- **`PhotonToNakamaMigrator.cs`** - Editor tool to help with migration

## 🔄 Migration Steps

### Step 1: Remove Photon Dependencies
1. Open Unity Editor
2. Go to `Tools > Chess > Migrate Photon to Nakama`
3. Click "Remove Photon Dependencies"

### Step 2: Update Scene References

#### MainMenuMulti Scene:
1. Remove `PhotonSimpleConnect` component
2. Add `NakamaManager` component
3. Assign the `NakamaConfig` asset to the NakamaManager
4. Connect UI references (Connect Button, Status Text, Username Input, Timer Toggles)

#### ChessGameMulti Scene:
1. Replace `GameManager` with `GameManagerNakama`
2. Replace `UIManager` with `UIManagerNakama`
3. Assign the `NakamaManager` reference to both components

#### ChessGameVS_AI Scene:
- No changes needed (uses GameManagerAI)

### Step 3: Clean Up Photon Files
1. Use the migration tool: `Tools > Chess > Migrate Photon to Nakama`
2. Click "Clean Up Photon Files"

## 🎮 How It Works

### Matchmaking Flow
1. Player enters username and selects timer type
2. Connects to Nakama server
3. Searches for matches with same timer type
4. Joins match when opponent found
5. Game starts when 2 players are connected

### Move Synchronization
- Moves are sent as JSON data through Nakama's match system
- Each move includes: type, coordinates, piece info
- Supports normal moves, castling, and pawn promotion
- Automatic synchronization across all connected players

### Player Management
- First player in match plays White
- Second player in match plays Black
- Automatic board rotation for Black player
- Disconnection handling with forfeit countdown

## 🔧 Configuration

### NakamaConfig Settings
```csharp
// Server Settings
scheme = "http"           // Protocol
host = "localhost"        // Server address
port = 7350              // Server port
serverKey = "defaultkey"  // Authentication key

// Match Settings
minPlayers = 2           // Minimum players
maxPlayers = 2           // Maximum players
matchmakingTimeout = 15f // Search timeout
matchWaitTimeout = 10f   // Wait for players timeout
```

### Player Settings (Unchanged)
```csharp
// Timer Types
PlayerSettings.TimerType.Rapid   // 10 minutes
PlayerSettings.TimerType.Blitz   // 5 minutes
PlayerSettings.TimerType.Bullet  // 3 minutes
```

## 🐛 Troubleshooting

### Common Issues

1. **Connection Failed**
   - Check if Nakama server is running
   - Verify host/port in NakamaConfig
   - Check firewall settings

2. **Matchmaking Timeout**
   - Increase `matchmakingTimeout` in NakamaConfig
   - Check if other players are searching for same timer type

3. **Moves Not Syncing**
   - Verify GameManagerNakama is being used in ChessGameMulti scene
   - Check NakamaManager reference is assigned

4. **UI Not Updating**
   - Ensure UIManagerNakama is being used
   - Check all UI references are assigned

### Debug Logging
Enable debug logging in NakamaConfig to see detailed connection and matchmaking information.

## 📚 API Reference

### NakamaManager
- `ConnectAndAuthenticate()` - Connects to Nakama
- `StartMatchmaking()` - Begins match search
- `SendMatchData()` - Sends move data to other players
- `GetCurrentMatch()` - Gets current match info

### GameManagerNakama
- Extends base GameManager with Nakama multiplayer
- Handles move synchronization
- Manages player roles (White/Black)

### UIManagerNakama
- Extends base UIManager with Nakama support
- Handles player disconnection
- Updates turn indicators for multiplayer

## 🔗 Useful Links

- [Nakama Documentation](https://heroiclabs.com/docs/)
- [Nakama Unity SDK](https://github.com/heroiclabs/nakama-unity)
- [Nakama Server Setup](https://heroiclabs.com/docs/nakama/getting-started/install/docker/)

## ✅ Migration Checklist

- [ ] Nakama Unity SDK installed
- [ ] NakamaConfig created and configured
- [ ] Nakama server running
- [ ] Photon dependencies removed
- [ ] Scene references updated
- [ ] Photon files cleaned up
- [ ] Multiplayer functionality tested
- [ ] AI mode still working
- [ ] Timer system working
- [ ] All game features working

## 🎯 Next Steps

After migration is complete:

1. Test multiplayer functionality with multiple clients
2. Configure your production Nakama server
3. Set up proper authentication if needed
4. Consider adding player statistics and match history
5. Implement spectator mode if desired

---

**Note**: The AI mode (ChessGameVS_AI scene) remains unchanged and continues to work with the original GameManagerAI system.
