using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Linq;
using Nakama;
using Nakama.TinyJson;

namespace CanvasChess
{
    /// <summary>
    /// GameManager with Nakama multiplayer integration
    /// Extends the base GameManager functionality with Nakama-specific networking
    /// </summary>
    public class GameManagerNakama : GameManager
    {
        #region Serialized Fields
        [Header("Nakama Integration")]
        [SerializeField] private NakamaManager nakamaManager = null!;
        #endregion

    #region Private Fields
    private bool isSyncingMove = false;
    private bool isNakamaConnected = false;
    private System.Collections.Generic.Queue<System.Action> pendingMatchDataActions = new System.Collections.Generic.Queue<System.Action>();
    #endregion

    #region Unity Lifecycle
    private new void Start()
    {
        // Call base Start method
        base.Start();
        
        // Initialize Nakama integration
        InitializeNakamaIntegration();
    }

    private void Update()
    {
        base.Update();
        // Process any pending match data actions on the main thread
        while (pendingMatchDataActions.Count > 0)
        {
            var action = pendingMatchDataActions.Dequeue();
            action?.Invoke();
        }
    }

    private new void OnDestroy()
    {
        // Unsubscribe from Nakama events
        if (nakamaManager != null && nakamaManager.GetSocket() != null)
        {
            nakamaManager.GetSocket().ReceivedMatchState -= OnMatchDataReceived;
        }
        
        base.OnDestroy();
    }
    #endregion

        #region Nakama Integration
        /// <summary>
        /// Initializes Nakama integration
        /// </summary>
        private void InitializeNakamaIntegration()
        {
            if (nakamaManager == null)
                nakamaManager = FindObjectOfType<NakamaManager>();
            
            if (nakamaManager != null && nakamaManager.GetSocket() != null)
            {
                // Subscribe to match data events
                nakamaManager.GetSocket().ReceivedMatchState += OnMatchDataReceived;
                isNakamaConnected = true;
                
                Debug.Log("[GameManagerNakama] Nakama integration initialized");
            }
            else
            {
                Debug.LogWarning("[GameManagerNakama] NakamaManager not found or socket not connected");
                isNakamaConnected = false;
            }
        }

        /// <summary>
        /// Handles match data received from Nakama (called from background thread)
        /// </summary>
        private void OnMatchDataReceived(IMatchState matchState)
        {
            // This is called from a background thread, so we need to queue the processing for the main thread
            if (isSyncingMove) return; // Prevent processing our own moves
            
            try
            {
                // Parse the received data on the background thread (this is safe)
                byte[] stateData = matchState.State;
                
                // Queue the processing for the main thread
                pendingMatchDataActions.Enqueue(() => {
                    try
                    {
                        string jsonData = System.Text.Encoding.UTF8.GetString(stateData);
                        var moveData = JsonUtility.FromJson<NakamaMoveData>(jsonData);
                        
                        Debug.Log($"[GameManagerNakama] Received move data: {moveData.moveType}");
                        
                        // Process the move based on type
                        switch (moveData.moveType)
                        {
                            case "move":
                                ProcessReceivedMove(moveData);
                                break;
                            case "castling":
                                ProcessReceivedCastling(moveData);
                                break;
                            case "promotion":
                                ProcessReceivedPromotion(moveData);
                                break;
                            default:
                                Debug.LogWarning($"[GameManagerNakama] Unknown move type: {moveData.moveType}");
                                break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[GameManagerNakama] Error processing match data: {ex.Message}");
                    }
                });
            }
            catch (System.Exception ex)
            {
                // This error logging won't work from background thread, but at least it won't crash
                // The error will be caught when the queued action executes
            }
        }

        /// <summary>
        /// Processes a received move
        /// </summary>
        private void ProcessReceivedMove(NakamaMoveData moveData)
        {
            isSyncingMove = true;
            Debug.Log($"[GameManagerNakama] Processing received move: {moveData.fromX}, {moveData.fromY} -> {moveData.toX}, {moveData.toY}");
            
            try
            {
                Tile fromTile = boardManager.GetTile(moveData.fromX, moveData.fromY);
                Tile toTile = boardManager.GetTile(moveData.toX, moveData.toY);
                Debug.Log($"[GameManagerNakama] From tile: {fromTile}, To tile: {toTile}");
                
                if (fromTile != null && toTile != null)
                {
                    Piece piece = fromTile.GetPiece();
                    if (piece != null)
                    {
                        Debug.Log($"[GameManagerNakama] Piece found: Type={piece.Type}, Color={piece.Color}, CurrentTile={piece.CurrentTile}");
                        Debug.Log($"[GameManagerNakama] About to call MakeMove...");
                        MakeMove(piece, toTile);
                        Debug.Log($"[GameManagerNakama] MakeMove completed successfully");
                    }
                    else
                    {
                        Debug.LogError($"[GameManagerNakama] No piece found on fromTile at ({moveData.fromX}, {moveData.fromY})");
                    }
                }
                else
                {
                    Debug.LogError($"[GameManagerNakama] Invalid tiles - fromTile: {fromTile}, toTile: {toTile}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameManagerNakama] Exception in ProcessReceivedMove: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                isSyncingMove = false;
            }
        }

        /// <summary>
        /// Processes a received castling move
        /// </summary>
        private void ProcessReceivedCastling(NakamaMoveData moveData)
        {
            isSyncingMove = true;
            
            PieceColor color = moveData.isWhite ? PieceColor.White : PieceColor.Black;
            bool kingside = moveData.isKingside;
            
            ExecuteCastling(color, kingside);
            DeselectPiece();
            CheckGameEndConditions();
            
            if (!gameEnded)
            {
                SwitchTurn();
            }
            
            isSyncingMove = false;
        }

        /// <summary>
        /// Processes a received promotion move
        /// </summary>
        private void ProcessReceivedPromotion(NakamaMoveData moveData)
        {
            isSyncingMove = true;
            
            Tile fromTile = boardManager.GetTile(moveData.fromX, moveData.fromY);
            Tile toTile = boardManager.GetTile(moveData.toX, moveData.toY);
            
            if (fromTile != null && toTile != null)
            {
                Piece piece = fromTile.GetPiece();
                if (piece != null && piece.Type == PieceType.Pawn)
                {
                    // Move the pawn to destination
                    fromTile.RemovePiece();
                    toTile.SetPiece(piece);
                    piece.RecordMove(turnNumber);
                    
                    // Promote to the specified piece type
                    PieceType promotionType = (PieceType)moveData.promotionType;
                    piece.PromoteToPiece(promotionType);
                    
                    // Complete the promotion move
                    CompletePromotionMove();
                }
            }
            
            isSyncingMove = false;
        }
        #endregion

        #region Move Synchronization
        /// <summary>
        /// Sends move data to other players via Nakama
        /// </summary>
        private async Task SendMoveData(NakamaMoveData moveData)
        {
            if (nakamaManager != null && isNakamaConnected)
            {
                try
                {
                    string jsonData = JsonUtility.ToJson(moveData);
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonData);
                    
                    await nakamaManager.SendMatchData("move", data);
                    Debug.Log($"[GameManagerNakama] Sent move data: {moveData.moveType}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameManagerNakama] Failed to send move data: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Override MakeMove to include Nakama synchronization
        /// </summary>
        protected override void MakeMove(Piece piece, Tile targetTile)
        {
            try
            {
                // Store original position before the move
                int fromX = piece.CurrentTile?.X ?? -1;
                int fromY = piece.CurrentTile?.Y ?? -1;
                
                Debug.Log($"[GameManagerNakama] MakeMove called: ({fromX}, {fromY}) -> ({targetTile.X}, {targetTile.Y}), isSyncingMove={isSyncingMove}");
                
                // Call base MakeMove first
                base.MakeMove(piece, targetTile);
                
                Debug.Log($"[GameManagerNakama] Base MakeMove completed");
                
                // Send move data to other players if not syncing
                if (!isSyncingMove && isNakamaConnected)
                {
                    var moveData = new NakamaMoveData
                    {
                        moveType = "move",
                        fromX = fromX,
                        fromY = fromY,
                        toX = targetTile.X,
                        toY = targetTile.Y,
                        isWhite = piece.Color == PieceColor.White
                    };
                    
                    Debug.Log($"[GameManagerNakama] Sending move data to network...");
                    _ = SendMoveData(moveData); // Fire and forget
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameManagerNakama] Exception in MakeMove: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Override ExecuteCastling to include Nakama synchronization
        /// </summary>
        protected override void ExecuteCastling(PieceColor color, bool kingside)
        {
            // Call base ExecuteCastling first
            base.ExecuteCastling(color, kingside);
            
            // Send castling data to other players if not syncing
            if (!isSyncingMove && isNakamaConnected)
            {
                var moveData = new NakamaMoveData
                {
                    moveType = "castling",
                    isWhite = color == PieceColor.White,
                    isKingside = kingside
                };
                
                _ = SendMoveData(moveData); // Fire and forget
            }
        }

        /// <summary>
        /// Override PromoteAndCompleteMove to include Nakama synchronization
        /// </summary>
        public override void PromoteAndCompleteMove(PieceType pieceType)
        {
            // Call base PromoteAndCompleteMove first
            base.PromoteAndCompleteMove(pieceType);
            
            // Send promotion data to other players if not syncing
            if (!isSyncingMove && isNakamaConnected && pendingPromotionPiece != null && pendingPromotionTile != null)
            {
                var moveData = new NakamaMoveData
                {
                    moveType = "promotion",
                    fromX = pendingPromotionTile.X,
                    fromY = pendingPromotionTile.Y,
                    toX = pendingPromotionTile.X,
                    toY = pendingPromotionTile.Y,
                    isWhite = pendingPromotionPiece.Color == PieceColor.White,
                    promotionType = (int)pieceType
                };
                
                _ = SendMoveData(moveData); // Fire and forget
            }
        }
        #endregion

        #region Player Management
        /// <summary>
        /// Gets the current player's role in the match
        /// Uses PlayerSettings.PlayerColor which was assigned during scene load
        /// </summary>
        public bool IsMasterClient()
        {
            if (nakamaManager == null || nakamaManager.GetCurrentMatch() == null)
                return true; // Default to master if not in multiplayer
            
            // Use PlayerSettings.PlayerColor which was reliably set during AssignPlayerColor()
            // White player = master (index 0), Black player = not master (index 1)
            return PlayerSettings.PlayerColor == PieceColor.White;
        }

        /// <summary>
        /// Updates board rotation based on player role in multiplayer
        /// </summary>
        public override void UpdateBoardRotation()
        {
            if (boardManager != null)
            {
                // For Nakama multiplayer, determine rotation based on player role
                bool shouldRotate = false;
                
                if (isNakamaConnected)
                {
                    // In multiplayer, rotate board if not the first player (white)
                    shouldRotate = !IsMasterClient();
                }
                else
                {
                    // In single player, use PlayerSettings
                    shouldRotate = PlayerSettings.PlayerColor == PieceColor.Black;
                }
                
                if (shouldRotate)
                {
                    boardManager.transform.localRotation = Quaternion.Euler(0, 0, 180);
                }
                else
                {
                    boardManager.transform.localRotation = Quaternion.identity;
                }
            }
        }
        #endregion

        #region Input Handling Override
        /// <summary>
        /// Override OnTileClicked to handle Nakama multiplayer
        /// </summary>
        public override void OnTileClicked(Tile tile, bool isAI = false)
        {
            if (gameEnded) return;
            
            // If waiting for promotion, ignore tile clicks
            if (isWaitingForPromotion) return;
            
            // Restrict input to the correct player, unless this is an AI move
            PieceColor allowedColor = currentTurn;
            if (!isAI)
            {
                // Check if this is multiplayer
                if (isNakamaConnected)
                {
                    // In multiplayer, determine player color based on role
                    PieceColor playerColor = IsMasterClient() ? PieceColor.White : PieceColor.Black;
                    if (currentTurn != playerColor)
                        return; // Not player's turn
                }
                else
                {
                    // Single player mode - check for AI
                    var gameManagerAI = FindObjectOfType<GameManagerAI>();
                    if (gameManagerAI != null)
                    {
                        allowedColor = gameManagerAI.PlayerColor;
                        if (currentTurn != allowedColor)
                            return; // Not player's turn
                    }
                }
            }
            
            // Call base OnTileClicked with the filtered logic
            base.OnTileClicked(tile, isAI);
        }
        #endregion
    }

    /// <summary>
    /// Data structure for Nakama move synchronization
    /// </summary>
    [System.Serializable]
    public class NakamaMoveData
    {
        public string moveType; // "move", "castling", "promotion"
        public int fromX, fromY;
        public int toX, toY;
        public bool isWhite;
        public bool isKingside; // For castling
        public int promotionType; // For promotion
    }
}
