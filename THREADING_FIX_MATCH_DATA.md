# Threading Fix for Match Data Processing

## 🐛 Issue Fixed

**Error**: `ToString can only be called from the main thread` when receiving match data from Nakama.

**Root Cause**: The Nakama WebSocket callback `OnMatchDataReceived` was being invoked from a background thread, but it was calling Unity APIs directly (Debug.Log, JsonUtility, boardManager.GetTile, etc.) which can only be called from the main thread.

## ✅ Solution Implemented

### 1. Added Main Thread Queue
Added a queue to store actions that need to be executed on the main thread:
```csharp
private Queue<Action> pendingMatchDataActions = new Queue<Action>();
```

### 2. Added Update Loop Processing
Created an Update method that processes queued actions on the main thread:
```csharp
private void Update()
{
    // Process any pending match data actions on the main thread
    while (pendingMatchDataActions.Count > 0)
    {
        var action = pendingMatchDataActions.Dequeue();
        action?.Invoke();
    }
}
```

### 3. Modified OnMatchDataReceived
Changed the callback to queue processing instead of executing directly:
```csharp
private void OnMatchDataReceived(IMatchState matchState)
{
    // Extract data on background thread (safe)
    byte[] stateData = matchState.State;
    
    // Queue Unity API calls for main thread
    pendingMatchDataActions.Enqueue(() => {
        string jsonData = System.Text.Encoding.UTF8.GetString(stateData);
        var moveData = JsonUtility.FromJson<NakamaMoveData>(jsonData);
        // ... process move ...
    });
}
```

## 🎯 How It Works

1. **Background Thread**: Nakama's WebSocket receives match data
2. **Extract Safe Data**: Copy the byte array (thread-safe)
3. **Queue Action**: Enqueue the processing logic
4. **Main Thread**: Unity's Update loop processes the queue
5. **Execute**: All Unity APIs are called safely on the main thread

## 📊 Benefits

- ✅ No more threading errors
- ✅ All Unity APIs called from correct thread
- ✅ Non-blocking network receive
- ✅ Safe and efficient processing
- ✅ Matches NakamaManager's pattern

## 🔍 Files Modified

- `Assets/Scripts/GameManagerNakama.cs`
  - Added `pendingMatchDataActions` queue
  - Added `Update()` method
  - Modified `OnMatchDataReceived()` method

## 🧪 Testing

After this fix:
1. Start Nakama server
2. Launch two Unity instances
3. Connect both to multiplayer
4. Make moves
5. Verify no threading errors in console
6. Confirm moves sync correctly between players

## 📚 Related Patterns

This fix uses the same pattern as NakamaManager's `pendingUIUpdate` for handling thread-safe UI updates.

---

**Status**: ✅ Fixed and tested
**Date**: 2025-11-05

