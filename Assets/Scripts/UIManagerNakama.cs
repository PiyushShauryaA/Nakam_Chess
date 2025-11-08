using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Nakama;

namespace CanvasChess
{
    /// <summary>
    /// UIManager with Nakama multiplayer integration
    /// Handles UI updates and player management for Nakama-based multiplayer
    /// </summary>
    public class UIManagerNakama : UIManager
    {
        #region Serialized Fields
        [Header("Nakama Integration")]
        [SerializeField] private NakamaManager nakamaManager = null!;
        #endregion

        #region Private Fields
        private bool isNakamaConnected = false;
        #endregion

        #region Unity Lifecycle
        private new void Start()
        {
            // Call base Start method
            base.Start();
            
            // Initialize Nakama integration
            InitializeNakamaIntegration();
        }

        private new void OnDestroy()
        {
            // Unsubscribe from Nakama events
            if (nakamaManager != null && nakamaManager.GetSocket() != null)
            {
                nakamaManager.GetSocket().ReceivedMatchPresence -= OnMatchPresenceChanged;
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
                // Subscribe to match presence events
                nakamaManager.GetSocket().ReceivedMatchPresence += OnMatchPresenceChanged;
                isNakamaConnected = true;
                
                Debug.Log("[UIManagerNakama] Nakama integration initialized");
            }
            else
            {
                Debug.LogWarning("[UIManagerNakama] NakamaManager not found or socket not connected");
                isNakamaConnected = false;
            }
        }

        /// <summary>
        /// Handles match presence changes (players joining/leaving)
        /// </summary>
        private void OnMatchPresenceChanged(IMatchPresenceEvent presenceEvent)
        {
            Debug.Log($"[UIManagerNakama] Match presence changed");
            
            if (nakamaManager != null && nakamaManager.GetCurrentMatch() != null)
            {
                var match = nakamaManager.GetCurrentMatch();
                Debug.Log($"[UIManagerNakama] Players in match: {match.Presences.Count()}");
                
                // Check if a player left
                if (presenceEvent.Leaves != null && presenceEvent.Leaves.Count() > 0)
                {
                    foreach (var player in presenceEvent.Leaves)
                    {
                        HandlePlayerDisconnected(player.Username);
                    }
                }
            }
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Updates the turn indicator GameObject based on whose turn it is
        /// </summary>
        public override void UpdateMyTurnIndicator()
        {
            if (PlayerEventSystem == null || gameManager == null)
                return;
                
            bool shouldBeActive = false;
            
            if (isNakamaConnected)
            {
                // In Nakama multiplayer, determine player role and turn
                var gameManagerNakama = gameManager as GameManagerNakama;
                if (gameManagerNakama != null)
                {
                    bool isMyTurn = false;
                    
                    if (gameManagerNakama.IsMasterClient())
                    {
                        // First player (white)
                        isMyTurn = gameManager.CurrentTurn == PieceColor.White;
                    }
                    else
                    {
                        // Second player (black)
                        isMyTurn = gameManager.CurrentTurn == PieceColor.Black;
                    }
                    
                    shouldBeActive = isMyTurn;
                }
            }
            else
            {
                // Single player mode - use original logic
                if (gameManager.CurrentTurn == PieceColor.White)
                    shouldBeActive = true;
            }
            
            PlayerEventSystem.SetActive(shouldBeActive);
            
            // Debug log to track event system state
            Debug.Log($"Event System Update - IsNakamaConnected: {isNakamaConnected}, CurrentTurn: {gameManager.CurrentTurn}, ShouldBeActive: {shouldBeActive}");
        }

        /// <summary>
        /// Handles checkmate events with Nakama multiplayer support
        /// </summary>
        protected override void HandleCheckmate(PieceColor winnerColor)
        {
            Debug.Log($"[UIManagerNakama] === NAKAMA CHECKMATE DEBUG ===");
            Debug.Log($"[UIManagerNakama] Winner Color: {winnerColor}");
            
            // Get player info from Nakama
            if (isNakamaConnected && nakamaManager != null)
            {
                var session = nakamaManager.GetSession();
                var match = nakamaManager.GetCurrentMatch();
                Debug.Log($"[UIManagerNakama] Local Player: {session?.Username} ({session?.UserId})");
                Debug.Log($"[UIManagerNakama] Local Player Color: {PlayerSettings.PlayerColor}");
                Debug.Log($"[UIManagerNakama] Match ID: {match?.Id}");
            }
            
            // Call base HandleCheckmate first
            base.HandleCheckmate(winnerColor);
            
            // Add Nakama-specific handling if needed
            if (isNakamaConnected)
            {
                Debug.Log($"[UIManagerNakama] Checkmate in Nakama multiplayer: {winnerColor} wins");
                // Could send match result to Nakama here if needed
            }
            
            Debug.Log($"[UIManagerNakama] ===============================");
        }

        /// <summary>
        /// Handles back to menu button click with Nakama cleanup
        /// </summary>
        protected override void OnBackToMenuButtonClicked()
        {
            Debug.Log("Back to menu button clicked");
            
            // Disconnect from Nakama if connected
            if (isNakamaConnected && nakamaManager != null)
            {
                nakamaManager.DisconnectFromNakama();
                StartCoroutine(LoadMenuAfterDelay(0.2f));
            }
            else
            {
                SceneManager.LoadScene("MainMenuMulti");
            }
        }

        /// <summary>
        /// Handles player disconnection in Nakama multiplayer
        /// </summary>
        public override void HandlePlayerDisconnected(string playerName)
        {
            Debug.Log($"[UIManagerNakama] Player {playerName} disconnected from Nakama match");
            
            // Check if the game has already ended
            if (gameManager != null && gameManager.GameEnded)
            {
                Debug.Log($"Player {playerName} disconnected after game ended, not showing disconnection UI");
                return;
            }

            disconnectedPlayerName = playerName;
            if (disconnectPanel != null) disconnectPanel.SetActive(true);
            if (disconnectCoroutine != null) StopCoroutine(disconnectCoroutine);
            disconnectCoroutine = StartCoroutine(DisconnectCountdownCoroutine());
        }

        /// <summary>
        /// Ends game on disconnect with Nakama cleanup
        /// </summary>
        private new void EndGameOnDisconnect()
        {
            // Check if the game has already ended
            if (gameManager != null && gameManager.GameEnded)
            {
                Debug.Log("Game already ended, not overriding win/lose state due to disconnection");
                // Hide disconnect panel
                if (disconnectPanel != null) disconnectPanel.SetActive(false);
                // Disconnect from Nakama and return to menu
                if (isNakamaConnected && nakamaManager != null)
                {
                    nakamaManager.DisconnectFromNakama();
                    StartCoroutine(LoadMenuAfterDelay(0.2f));
                }
                else
                {
                    SceneManager.LoadScene("MainMenuMulti");
                }
                return;
            }

            // Determine winner/loser only if game hasn't already ended
            bool isLocalPlayerWinner = false;
            if (isNakamaConnected)
            {
                // If local player is still connected, they win
                isLocalPlayerWinner = true;
            }
            
            if (isLocalPlayerWinner)
                ShowWinMessage();
            else
                ShowLoseMessage();
                
            // Hide disconnect panel
            if (disconnectPanel != null) disconnectPanel.SetActive(false);
            
            // Disconnect from Nakama and return to menu
            if (isNakamaConnected && nakamaManager != null)
            {
                nakamaManager.DisconnectFromNakama();
                StartCoroutine(LoadMenuAfterDelay(0.2f));
            }
            else
            {
                SceneManager.LoadScene("MainMenuMulti");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets whether Nakama is connected
        /// </summary>
        public bool IsNakamaConnected()
        {
            return isNakamaConnected;
        }

        /// <summary>
        /// Gets the current match information
        /// </summary>
        public IMatch GetCurrentMatch()
        {
            return nakamaManager?.GetCurrentMatch();
        }

        /// <summary>
        /// Gets the current session information
        /// </summary>
        public ISession GetCurrentSession()
        {
            return nakamaManager?.GetSession();
        }
        #endregion
    }
}
