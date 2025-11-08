# Last Move Highlighting Feature

## Overview
This feature adds visual highlighting to show the last move made on the chess board. The starting and ending squares of the last move are highlighted with a yellow overlay.

## Implementation Details

### 1. Tile Component Updates (`Tile.cs`)
- Added `lastMoveHighlightObject` field to store the highlight GameObject
- Added `HighlightLastMove(bool highlight)` method to show/hide the last move highlight
- Added `ClearLastMoveHighlight()` method to clear the highlight

### 2. GameManager Updates (`GameManager.cs`)
- Added `lastMoveFromTile` and `lastMoveToTile` fields to track the last move
- Added `HighlightLastMove(Tile fromTile, Tile toTile)` method to highlight the last move
- Added `ClearLastMoveHighlight()` method to clear the last move highlight
- Updated `MakeMove()` method to call `HighlightLastMove()` after each move
- Updated `ExecuteCastling()` method to highlight the king's move during castling
- Updated `RestartGame()` method to clear last move highlights when restarting

### 3. BoardManager Updates (`BoardManager.cs`)
- Updated `ClearAllHighlights()` method to also clear last move highlights
- Updated `ResetBoard()` method to clear last move highlights when resetting

### 4. Editor Setup Script (`LastMoveHighlightSetup.cs`)
- Created an editor utility to automatically set up last move highlight objects on tile prefabs
- Adds a yellow semi-transparent overlay for the last move highlight

## How to Set Up

### 1. Run the Setup Script
1. Open Unity Editor
2. Go to `Tools > Chess > Setup Last Move Highlights`
3. This will automatically create the necessary highlight objects on your tile prefabs

### 2. Manual Setup (if needed)
If the automatic setup doesn't work, you can manually add the highlight objects:

1. Open your tile prefabs (`LightTile.prefab` and `DarkTile.prefab`)
2. Create a new GameObject as a child of the tile
3. Name it "LastMoveHighlightObject"
4. Add the following components:
   - RectTransform (set anchors to stretch)
   - CanvasRenderer
   - Image (set color to yellow with alpha around 0.3)
5. Set the GameObject to inactive by default
6. Assign this GameObject to the `lastMoveHighlightObject` field in the Tile component

## Visual Appearance
- The last move is highlighted with a semi-transparent yellow overlay
- Both the starting square and ending square of the last move are highlighted
- The highlight persists until the next move is made
- The highlight is cleared when the game is restarted

## Technical Notes
- The highlighting system is separate from the legal move highlighting
- Last move highlights are cleared when all highlights are cleared
- The system works for both normal moves and castling moves
- The highlighting is synchronized in multiplayer games

## Usage
Once set up, the feature works automatically:
1. Make a move (normal or castling)
2. The starting and ending squares will be highlighted in yellow
3. Make another move to see the highlight update to the new move
4. The highlight will be cleared when the game is restarted

## Troubleshooting
- If highlights don't appear, check that the `lastMoveHighlightObject` field is assigned in the Tile component
- If the highlight color is wrong, adjust the Image component's color in the prefab
- If highlights persist incorrectly, ensure the `ClearLastMoveHighlight()` method is being called properly 