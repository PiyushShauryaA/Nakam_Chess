# Timer Selection System Implementation

## Overview
This document describes the implementation of a multi-timer selection system for the chess game, allowing players to choose between different time controls.

## Timer Options

### 1. Rapid (10 minutes)
- **Duration**: 600 seconds per player
- **Description**: Standard rapid chess time control
- **Default**: Yes (selected by default)

### 2. Blitz (5 minutes)
- **Duration**: 300 seconds per player
- **Description**: Fast-paced blitz chess
- **Default**: No

### 3. Bullet (3 minutes)
- **Duration**: 180 seconds per player
- **Description**: Ultra-fast bullet chess
- **Default**: No

## Implementation Details

### Files Modified

#### 1. `Assets/Scripts/PlayerSettings.cs`
- Added `TimerType` enum with Rapid, Blitz, and Bullet options
- Added `SelectedTimerType` static property to store current selection
- Added `GetTimerDuration()` method to return duration based on selected type

#### 2. `Assets/Scripts/TimerSelectionManager.cs` (New)
- Manages timer selection UI
- Handles toggle button events
- Updates timer display
- Provides public methods for timer type management

#### 3. `Assets/Scripts/GameManager.cs`
- Removed hardcoded `initialTimeSeconds` field
- Modified `InitializeTimerSystem()` to use `PlayerSettings.GetTimerDuration()`
- Now dynamically sets timer duration based on player selection

#### 4. `Assets/Scripts/GameManagerAI.cs`
- Removed hardcoded `initialTimeSeconds` field
- Modified timer initialization to use `PlayerSettings.GetTimerDuration()`
- Consistent with multiplayer timer system

#### 5. `Assets/Scripts/PhotonSimpleConnect.cs`
- Added timer selection UI fields
- Added `SetupTimerSelection()` method
- Added timer toggle event handlers
- Added timer display update functionality

#### 6. `Assets/Scripts/Editor/MainMenuMultiSceneBuilder.cs`
- Added timer selection UI creation
- Added `CreateTimerToggle()` helper method
- Updated scene layout to accommodate timer selection
- Added timer selection wiring to PhotonSimpleConnect

### UI Components

#### Timer Selection Section
- **Title**: "Select Timer:"
- **Toggle Buttons**: 
  - Rapid (10 min)
  - Blitz (5 min) 
  - Bullet (3 min)
- **Display**: Shows currently selected timer type
- **Layout**: Vertical layout with proper spacing

#### Toggle Button Design
- **Background**: Dark blue-gray color
- **Checkmark**: Green color when selected
- **Label**: White text with proper alignment
- **Interaction**: Radio button behavior (only one selected at a time)

## Usage

### For Players
1. **In Menu Scene**: Select desired timer type using toggle buttons
2. **Visual Feedback**: Selected timer is highlighted and displayed
3. **Game Start**: Timer duration is automatically applied when game begins
4. **Consistency**: Same timer applies to both multiplayer and AI games

### For Developers
1. **Adding New Timer Types**: 
   - Add to `PlayerSettings.TimerType` enum
   - Update `GetTimerDuration()` method
   - Add UI toggle in scene builder
2. **Modifying Timer Durations**: Update values in `PlayerSettings.GetTimerDuration()`
3. **Custom Timer Logic**: Extend `TimerSelectionManager` for additional features

## Technical Features

### State Management
- **Persistent Selection**: Timer choice persists across game sessions
- **Default Fallback**: Rapid timer selected by default
- **Validation**: Invalid selections fall back to Rapid

### UI Integration
- **Responsive Design**: Adapts to different screen sizes
- **Visual Feedback**: Clear indication of selected timer
- **Accessibility**: Proper button sizing and contrast

### Multiplayer Compatibility
- **Synchronized**: Both players see same timer duration
- **Consistent**: Same timer logic across all game modes
- **Reliable**: Timer settings preserved during connection

## Testing

### Manual Testing Checklist
- [ ] Timer selection works in menu scene
- [ ] Selected timer displays correctly
- [ ] Timer duration applies correctly in game
- [ ] Works in both multiplayer and AI modes
- [ ] Timer persists across scene changes
- [ ] Default selection works properly
- [ ] UI updates when selection changes

### Automated Testing
- Unit tests for `PlayerSettings.GetTimerDuration()`
- Integration tests for timer initialization
- UI tests for toggle button interactions

## Future Enhancements

### Potential Additions
1. **Custom Timer**: Allow players to set custom duration
2. **Increment Options**: Add time increment per move
3. **Timer Presets**: Save favorite timer combinations
4. **Tournament Mode**: Special timer settings for tournaments
5. **Analytics**: Track which timers are most popular

### Code Improvements
1. **ScriptableObject**: Move timer settings to ScriptableObject for easier editing
2. **Localization**: Support for multiple languages
3. **Animation**: Smooth transitions between timer selections
4. **Sound Effects**: Audio feedback for timer selection

## Troubleshooting

### Common Issues
1. **Timer Not Updating**: Check if `PlayerSettings.SelectedTimerType` is being set correctly
2. **UI Not Responding**: Verify toggle button event listeners are properly wired
3. **Wrong Duration**: Ensure `GetTimerDuration()` method is being called
4. **Scene Builder Errors**: Check if all required components are present

### Debug Information
- Timer selection is logged to console
- Current timer type can be queried via `PlayerSettings.SelectedTimerType`
- Timer duration can be verified via `PlayerSettings.GetTimerDuration()`

## Conclusion

The timer selection system provides a flexible and user-friendly way for players to choose their preferred time control. The implementation is robust, maintainable, and easily extensible for future enhancements. 