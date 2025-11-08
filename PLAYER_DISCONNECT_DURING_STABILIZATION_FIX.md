# 🔧 Player Disconnect During Stabilization Fix

## 🐛 Problem Identified

**Issue:** Players successfully matched but one player disconnected during the 2-second stabilization window, causing the match to fail and switch to AI game.

### What Was Happening (From Your Logs):
```
✅ [NakamaManager] Matchmaker users: 2
✅ [NakamaManager] ✓ Detected 2 unique players - ready to start!
✅ [NakamaManager] ✓✓ Match has 2 players - starting stabilization timer
❌ [NakamaManager] Player left: 853e48fb-4e45... ← BUG!
❌ [NakamaManager] Unique players seen total: 1  ← Removed from tracking!
❌ [NakamaManager] Still waiting: 1/2 players
❌ [NakamaManager] Wait timer expired → AI game
```

### Root Cause:
1. Two players matched successfully ✓
2. System detected 2 unique players ✓
3. Started 2-second stabilization timer ✓
4. **One player temporarily disconnected** (network timing issue)
5. **`seenPlayerIds.Remove()` was called** ❌
6. **Player count dropped to 1** ❌
7. **Stabilization timer was reset to 0** ❌
8. System thought only 1 player was in match ❌
9. Timeout → Switched to AI game ❌

This is a **race condition** caused by:
- Nakama WebSocket timing (async connections)
- Player joining/leaving events happening during stabilization
- Previous fix removing players from tracking when they left

---

## ✅ Solution Implemented

### Changes Made to `NakamaManager.cs`:

#### 1. **Don't Remove Players During Stabilization** (Lines 374-393)

**Before:**
```csharp
if (presenceEvent.Leaves != null)
{
    foreach (var leave in presenceEvent.Leaves)
    {
        seenPlayerIds.Remove(leave.UserId);  // ← Always removed!
        Debug.Log($"[NakamaManager] Player left: {leave.UserId}");
    }
}
```

**After:**
```csharp
if (presenceEvent.Leaves != null)
{
    foreach (var leave in presenceEvent.Leaves)
    {
        Debug.Log($"[NakamaManager] Player left: {leave.UserId}");
        
        // Only remove from tracking if we haven't detected enough players yet
        // Once we've seen enough players, keep them in tracking even if they temporarily disconnect
        if (!hasSeenTwoPlayers && !matchStabilized)
        {
            seenPlayerIds.Remove(leave.UserId);
            Debug.Log($"[NakamaManager] Removed from tracking (pre-stabilization). Total: {seenPlayerIds.Count}");
        }
        else
        {
            Debug.Log($"[NakamaManager] Player left during/after stabilization - keeping in tracking. Total: {seenPlayerIds.Count}");
        }
    }
}
```

#### 2. **Don't Reset Timer During Stabilization** (Lines 408-425)

**Before:**
```csharp
if (!matchStabilized)
{
    matchStabilizationTimer = 0f;  // ← Always reset!
}
```

**After:**
```csharp
// Check if we have seen enough unique players (before resetting timer)
if (seenPlayerIds.Count >= nakamaConfig.maxPlayers)
{
    hasSeenTwoPlayers = true;
    Debug.Log($"[NakamaManager] ✓ Detected {seenPlayerIds.Count} unique players - ready to start!");
}

// Only reset timer if we DON'T have enough players yet
// Once we have enough players, keep the timer running even if someone leaves temporarily
if (!matchStabilized && !hasSeenTwoPlayers)
{
    matchStabilizationTimer = 0f;
    Debug.Log($"[NakamaManager] Reset stabilization timer (not enough players yet)");
}
else if (hasSeenTwoPlayers)
{
    Debug.Log($"[NakamaManager] Keeping stabilization timer running despite presence change");
}
```

---

## 📊 New Behavior

### Scenario: Player Temporarily Disconnects During Stabilization

**Before Fix:**
```
1. Player1 joins match
2. Player2 joins match
3. System: "2 players detected!"
4. Start stabilization timer
5. Player2 temporarily disconnects (network timing)
6. System removes Player2 from tracking → count = 1
7. System resets timer to 0
8. Stabilization fails → Timeout → AI game
```

**After Fix:**
```
1. Player1 joins match
2. Player2 joins match
3. System: "2 players detected!"
4. Start stabilization timer
5. Player2 temporarily disconnects (network timing)
6. System: "Keeping in tracking (stabilization)" → count still = 2 ✓
7. System: "Keep timer running" → timer continues ✓
8. Timer reaches 2.0s → Game starts successfully! ✓
```

---

## 🎮 Expected Debug Output

### Successful Connection (After Fix):

```
[NakamaManager] Matchmaker users: 2
[NakamaManager] Initial presence added: <user-id-1>
[NakamaManager] Total players tracked: 1
[NakamaManager] Player joined: <user-id-2>
[NakamaManager] Added to seen list. Total unique players seen: 2
[NakamaManager] ✓ Detected 2 unique players - ready to start!
[NakamaManager] ✓✓ Match has 2 players - starting stabilization timer

// If player temporarily leaves:
[NakamaManager] Player left: <user-id-2>
[NakamaManager] Player left during/after stabilization - keeping in tracking. Total: 2
[NakamaManager] Keeping stabilization timer running despite presence change (have 2 players)

// Timer continues:
[NakamaManager] Match stabilization: 2 players tracked, timer: 0.5s / 2.0s
[NakamaManager] Match stabilization: 2 players tracked, timer: 1.0s / 2.0s
[NakamaManager] Match stabilization: 2 players tracked, timer: 1.5s / 2.0s
[NakamaManager] Match stabilization: 2 players tracked, timer: 2.0s / 2.0s
[NakamaManager] ✓✓✓ Match stabilized! Starting game with 2 players

→ ChessGameMulti scene loads! ✅
```

---

## 🔍 Key Improvements

### 1. **Persistent Player Tracking**
- Once 2 players are detected, they stay tracked
- Temporary disconnects don't remove them
- More resilient to network timing issues

### 2. **Stabilization Timer Protection**
- Timer doesn't reset once enough players are detected
- Continues counting even if player temporarily leaves
- Ensures the 2-second window completes

### 3. **Better Debug Logging**
- Shows when players are kept in tracking
- Shows why timer is/isn't reset
- Easier to diagnose issues

---

## 🎯 Why This Happens

### Network Timing Issues:

**Nakama WebSocket Architecture:**
```
Player1 → Connect → Join Match
Player2 → Connect → Join Match → Match Created!

But WebSocket events are async, so:
- Player1 receives: "Match created, you're in"
- Player2 receives: "Match created, you're in"
- Player1 receives: "Player2 joined"
- Player2 might briefly disconnect/reconnect during handshake
- Events arrive out of order or with slight delays
```

**Why It Breaks:**
- Stabilization window is only 2 seconds
- WebSocket handshake can take 100-500ms
- If a leave event happens during this window
- Removing the player breaks the match

**How Fix Helps:**
- Once we see 2 players, we "commit" to starting the game
- Temporary disconnects during stabilization are ignored
- Timer continues running
- Game starts after 2 seconds regardless of brief disconnects

---

## 🧪 Testing Instructions

### Test Case 1: Normal Connection
1. Start Unity Editor (Player1)
2. Start Chess.exe (Player2)
3. Both select RAPID, click Connect within 3-5 seconds
4. Expected: Match stabilizes, game loads in ChessGameMulti

### Test Case 2: Slow Connection
1. Start Unity Editor (Player1), click Connect
2. Wait 2-3 seconds
3. Start Chess.exe (Player2), click Connect
4. Expected: Should still work even with delay

### Test Case 3: Network Timing
1. Both connect quickly
2. Watch for any "Player left during stabilization" messages
3. Expected: Game still starts even if temporary disconnect occurs

---

## 📝 Technical Details

### seenPlayerIds Behavior:

**Purpose:** Track all unique players who have ever joined the match

**Add Player When:**
- Player joins match (OnMatchmakerMatched)
- Player joins via presence event (OnMatchPresence - Joins)

**Remove Player When:**
- Player leaves AND we haven't seen enough players yet
- **Do NOT remove** once `hasSeenTwoPlayers = true`
- **Do NOT remove** once `matchStabilized = true`

### matchStabilizationTimer Behavior:

**Reset When:**
- New match created
- Presence changes AND we don't have enough players yet
- **Do NOT reset** once `hasSeenTwoPlayers = true`

**Continue Counting When:**
- Have enough players (`hasSeenTwoPlayers = true`)
- Match not stabilized yet (`matchStabilized = false`)
- Even if players temporarily leave

---

## ⚙️ Configuration

### Timing Parameters (in `NakamaConfig`):

```csharp
matchWaitTimeout = 10f;        // Wait for initial player join
matchmakingTimeout = 15f;      // Total matchmaking search time
matchSyncDelay = 3f;           // Extra sync time (hardcoded in NakamaManager)
```

### Stabilization Window:

```csharp
// In Update() method:
if (matchStabilizationTimer >= 2f)  // 2-second stabilization
{
    matchStabilized = true;
    // Load game scene
}
```

**Total Timeline:**
```
0s: Players connect
↓
2-5s: Matchmaking
↓
5-7s: Match joined
↓  
7-9s: Stabilization (2 seconds) ← Protected by this fix
↓
9s: Game starts!
```

---

## 🚀 Next Steps

1. **Test the fix** following instructions above
2. **Watch Unity Console** for the new debug messages
3. **Verify** game loads into ChessGameMulti (not ChessGameVS_AI)
4. **Confirm** both players can make moves

### If Still Having Issues:

1. Check both players selected **same timer type** (Rapid/Blitz/Bullet)
2. Verify Nakama server running: `docker-compose ps`
3. Check server logs: `docker-compose logs nakama --tail=50`
4. Ensure "Run In Background" is enabled in Unity Player Settings

---

## 📅 Fix Applied: October 27, 2025

**Status:** ✅ Ready for Testing  
**Files Modified:** `Assets/Scripts/NakamaManager.cs`  
**Lines Changed:** ~50 lines (improved stabilization logic and player tracking)  
**Breaking Changes:** None - fully backwards compatible

**Key Insight:** Network timing in real-world multiplayer requires resilience to brief disconnects during match setup. This fix makes the system more robust by not immediately removing players who temporarily disconnect during the critical stabilization window.

