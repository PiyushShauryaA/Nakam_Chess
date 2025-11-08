# Pawn Promotion UI Setup Guide

## Overview
The pawn promotion system has been updated to allow players to choose which piece they want to promote to (Queen, Rook, Bishop, or Knight) instead of automatically promoting to a Queen.

## Implementation Details

### 1. **Piece.cs Changes**
- Added `PromoteToPiece(PieceType newPieceType)` method
- Kept the original `PromoteToQueen()` method for backward compatibility
- The `ShouldPromote()` method remains unchanged

### 2. **GameManager.cs Changes**
- Added promotion state management variables:
  - `isWaitingForPromotion`
  - `pendingPromotionPiece`
  - `pendingPromotionTile`
- Modified `MakeMove()` to show promotion UI instead of auto-promoting
- Added `PromoteAndCompleteMove(PieceType pieceType)` method
- Modified `OnTileClicked()` to prevent moves while waiting for promotion

### 3. **UIManager.cs Changes**
- Added `PromotionUI` reference
- Simplified promotion UI methods to delegate to `PromotionUI` component

### 4. **New PromotionUI.cs Component**
- Handles the promotion selection UI
- Manages button listeners for piece selection
- Loads appropriate sprites based on pawn color

## Unity Setup Instructions

### Step 1: Create the Promotion UI Panel
1. In your Canvas, create a new Panel GameObject
2. Name it "PromotionPanel"
3. Set it to cover the center of the screen (you can make it semi-transparent)
4. Initially set it to inactive

### Step 2: Add Promotion Buttons
1. Inside the PromotionPanel, create 4 Button GameObjects:
   - QueenButton
   - RookButton
   - BishopButton
   - KnightButton

2. For each button, add an Image component as a child and name it:
   - QueenImage
   - RookImage
   - BishopImage
   - KnightImage

### Step 3: Add the PromotionUI Script
1. Add the `PromotionUI.cs` script to the PromotionPanel
2. Assign the references in the Inspector:
   - Promotion Panel: The PromotionPanel GameObject
   - Queen Button: The QueenButton
   - Rook Button: The RookButton
   - Bishop Button: The BishopButton
   - Knight Button: The KnightButton
   - Queen Image: The QueenImage
   - Rook Image: The RookImage
   - Bishop Image: The BishopImage
   - Knight Image: The KnightImage
   - Game Manager: Reference to your GameManager

### Step 4: Update UIManager
1. In your UIManager GameObject, assign the PromotionUI reference
2. The UIManager will now delegate promotion UI calls to the PromotionUI component

## How It Works

1. **Detection**: When a pawn reaches the promotion rank (rank 8 for white, rank 1 for black), the system detects it
2. **UI Display**: Instead of auto-promoting, the promotion UI is shown with 4 buttons (Queen, Rook, Bishop, Knight)
3. **Piece Selection**: Player clicks on their desired piece type
4. **Promotion**: The pawn is promoted to the selected piece type
5. **Move Completion**: The move is completed and the turn switches

## Sprite Requirements

The system expects the following sprite files in `Assets/Resources/Sprites/`:
- `w_queen.png` (white queen)
- `w_rook.png` (white rook)
- `w_bishop.png` (white bishop)
- `w_knight.png` (white knight)
- `b_queen.png` (black queen)
- `b_rook.png` (black rook)
- `b_bishop.png` (black bishop)
- `b_knight.png` (black knight)

## Testing

To test the promotion system:
1. Move a pawn to the last rank (rank 8 for white, rank 1 for black)
2. The promotion UI should appear
3. Click on any of the four piece types
4. The pawn should be promoted to the selected piece type
5. The move should be completed and the turn should switch

## Multiplayer Considerations

The promotion system works with the existing multiplayer infrastructure. The promotion selection is handled locally, and the final promoted piece state is synchronized through the existing move synchronization system. 