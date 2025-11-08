# Threading Synchronization Fix for Multiplayer Matchmaking

**Date:** October 27, 2025

## Critical Bug: Race Condition Between Background and Main Thread

### The Problem

After implementing the "player tracking during stabilization" fix, players were STILL failing to connect and the game was falling back to AI. Analysis of the logs revealed a **critical threading synchronization bug**:

```
[NakamaManager] ✓ Detected 2 unique players - ready to start!
[NakamaManager] ✓✓ Match has 2 players - starting stabilization timer
[NakamaManager] Player left: 02054386...
[NakamaManager] Wait timer expired (13s). Starting search mode.  ← BUG!
```

The `hasSeenTwoPlayers` flag was being set on a **background thread** (in `OnMatchPresence` callback from WebSocket), but the `Update()` method was checking it on the **main thread**. Without proper memory barriers or synchronization:

1. Background thread sets `hasSeenTwoPlayers = true`
2. Main thread might not see the updated value immediately (CPU cache, instruction reordering)
3. The 13-second wait timer condition `!hasSeenTwoPlayers` continues to evaluate to `true`
4. Timer expires, game falls back to AI

### Why This Happens

In C#, when accessing variables from multiple threads without synchronization:
- The reading thread might see stale values from its CPU cache
- The compiler/JIT might reorder instructions for optimization
- There's no guarantee of memory visibility between threads

### The Solution

**Ensure all flag updates happen on the Unity main thread using the existing `pendingUIUpdate` mechanism:**

#### Before (Broken):
```csharp
private void OnMatchPresence(IMatchPresenceEvent presenceEvent)
{
    // ... runs on background thread
    
    if (seenPlayerIds.Count >= 2)
    {
        hasSeenTwoPlayers = true;  // ❌ Set on background thread!
    }
    
    // Later, Update() checks hasSeenTwoPlayers on main thread
    // Main thread might not see the updated value!
}
```

#### After (Fixed):
```csharp
private void OnMatchPresence(IMatchPresenceEvent presenceEvent)
{
    // ... runs on background thread
    
    bool justDetectedEnoughPlayers = false;
    if (seenPlayerIds.Count >= 2 && !hasSeenTwoPlayers)
    {
        justDetectedEnoughPlayers = true;
    }
    
    // Use local variable for immediate logic checks
    if (!matchStabilized && !hasSeenTwoPlayers && !justDetectedEnoughPlayers)
    {
        matchStabilizationTimer = 0f;  // Only reset if we truly don't have enough
    }
    
    // Capture values for main thread execution
    bool shouldSetFlag = justDetectedEnoughPlayers;
    
    // ✅ Set flag on main thread via pendingUIUpdate (executed in Update())
    pendingUIUpdate = () =>
    {
        if (shouldSetFlag)
        {
            hasSeenTwoPlayers = true;
            Debug.Log("[NakamaManager] ✓✓ Set hasSeenTwoPlayers flag on main thread");
        }
        // ... UI update logic ...
    };
}
```

### Key Changes

1. **Flag Setting on Main Thread:**
   - `hasSeenTwoPlayers` is now set inside the `pendingUIUpdate` lambda
   - The `Update()` method executes `pendingUIUpdate` every frame, ensuring main thread synchronization

2. **Local Variable for Immediate Logic:**
   - `justDetectedEnoughPlayers` tracks the state within the current method call
   - Prevents timer reset when we just detected enough players (even if flag isn't set yet)

3. **Captured Variables in Lambdas:**
   - `pendingUIUpdate` lambda now captures `seenPlayerIds.Count` at creation time
   - Removes dependency on `hasSeenTwoPlayers` flag in UI update logic

### Files Modified

- `Assets/Scripts/NakamaManager.cs`:
  - Modified `OnMatchPresence()` method (lines ~408-434)
  - Changed flag setting to use main thread dispatcher
  - Added `justDetectedEnoughPlayers` local variable to prevent race conditions
  - Simplified `pendingUIUpdate` lambda to use captured player count

### Testing

After this fix, the matchmaking flow should be:

1. **Match Found:** "Matchmaker matched: 2 users"
2. **Player Joins:** "Player joined: [UserID]"
3. **Enough Players:** "✓ Detected 2 unique players - ready to start!"
4. **Flag Set:** "✓✓ Set hasSeenTwoPlayers flag on main thread"
5. **Timer Stopped:** Wait timer no longer runs
6. **Stabilization:** 3-second stabilization timer completes
7. **Game Starts:** Scene loads successfully

### Additional Notes

This is a common pitfall in Unity networking code where callbacks from networking libraries (Nakama, Photon, etc.) execute on background threads, but Unity's main thread must be used for:
- Setting flags that `Update()` checks
- Modifying UI elements
- Changing scene state
- Most Unity API calls

**Always use an action queue pattern (like `pendingUIUpdate`), Unity's `SynchronizationContext`, or coroutines when bridging from background threads to Unity's main thread.**

---

## Related Fixes

This fix builds on the previous "Player Disconnect During Stabilization" fix, which prevented players from being removed from tracking too early. Together, these fixes ensure:

1. Players are tracked reliably during temporary disconnects
2. The main thread sees flag updates immediately
3. The wait timer stops correctly when enough players are detected
4. The match successfully starts with 2 players

See `PLAYER_DISCONNECT_DURING_STABILIZATION_FIX.md` for the complete context.

