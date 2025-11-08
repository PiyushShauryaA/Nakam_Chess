using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace CanvasChess
{
    /// <summary>
    /// Manages matchmaking with AI fallback functionality
    /// </summary>
    public class MatchmakingManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI References")]
        [SerializeField] private Button connectAndJoinButton = null!;
        [SerializeField] private TextMeshProUGUI statusText = null!;
        [SerializeField] private TextMeshProUGUI countdownText = null!;
        [SerializeField] private GameObject matchmakingPanel = null!;
        [SerializeField] private Button cancelButton = null!;
        
        [Header("Matchmaking Settings")]
        [SerializeField] private float matchmakingTimeout = 15f;
        [SerializeField] private float statusUpdateInterval = 1f;
        [SerializeField] private string multiplayerSceneName = "ChessGameMulti";
        [SerializeField] private string aiSceneName = "ChessGameVS_AI";
        #endregion

        #region Private Fields
        private NakamaManager nakamaManager = null!;
        private bool isMatchmaking = false;
        private float searchStartTime = 0f;
        private Coroutine matchmakingCoroutine = null!;
        private Coroutine statusUpdateCoroutine = null!;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Find NakamaManager in the scene
            nakamaManager = FindObjectOfType<NakamaManager>();
            if (nakamaManager == null)
            {
                Debug.LogError("[MatchmakingManager] NakamaManager not found in scene!");
                return;
            }
            
            SetupUI();
        }

        private void OnDestroy()
        {
            StopMatchmaking();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Sets up UI elements and button listeners
        /// </summary>
        private void SetupUI()
        {
            if (connectAndJoinButton != null)
            {
                connectAndJoinButton.onClick.AddListener(OnConnectAndJoinClicked);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelMatchmakingClicked);
            }
            
            if (matchmakingPanel != null)
            {
                matchmakingPanel.SetActive(false);
            }
            
            UpdateStatusText("Ready to search for players");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts the matchmaking process with AI fallback
        /// </summary>
        public void StartMatchmaking()
        {
            if (isMatchmaking)
            {
                Debug.LogWarning("[MatchmakingManager] Matchmaking already in progress");
                return;
            }
            
            Debug.Log("[MatchmakingManager] Starting matchmaking process");
            isMatchmaking = true;
            searchStartTime = Time.time;
            
            // Show matchmaking UI
            if (matchmakingPanel != null)
            {
                matchmakingPanel.SetActive(true);
            }
            
            // Start matchmaking coroutine
            matchmakingCoroutine = StartCoroutine(MatchmakingProcess());
            
            // Start status update coroutine
            statusUpdateCoroutine = StartCoroutine(UpdateMatchmakingStatus());
        }
        
        /// <summary>
        /// Stops the current matchmaking process
        /// </summary>
        public void StopMatchmaking()
        {
            if (!isMatchmaking)
            {
                return;
            }
            
            Debug.Log("[MatchmakingManager] Stopping matchmaking process");
            isMatchmaking = false;
            
            // Stop coroutines
            if (matchmakingCoroutine != null)
            {
                StopCoroutine(matchmakingCoroutine);
                matchmakingCoroutine = null!;
            }
            
            if (statusUpdateCoroutine != null)
            {
                StopCoroutine(statusUpdateCoroutine);
                statusUpdateCoroutine = null!;
            }
            
            // Hide matchmaking UI
            if (matchmakingPanel != null)
            {
                matchmakingPanel.SetActive(false);
            }
            
            UpdateStatusText("Matchmaking cancelled");
        }
        #endregion

        #region Button Handlers
        /// <summary>
        /// Handles connect and join button click
        /// </summary>
        private void OnConnectAndJoinClicked()
        {
            Debug.Log("[MatchmakingManager] Connect and Join button clicked");
            StartMatchmaking();
        }
        
        /// <summary>
        /// Handles cancel matchmaking button click
        /// </summary>
        private void OnCancelMatchmakingClicked()
        {
            Debug.Log("[MatchmakingManager] Cancel matchmaking button clicked");
            StopMatchmaking();
        }
        #endregion

        #region Matchmaking Process
        /// <summary>
        /// Main matchmaking coroutine
        /// </summary>
        private IEnumerator MatchmakingProcess()
        {
            // Step 1: Connect to Nakama
            yield return StartCoroutine(ConnectToNakama());
            
            // Step 2: Start matchmaking search
            yield return StartCoroutine(SearchForMatch());
            
            // Step 3: Handle result
            if (isMatchmaking) // If still matchmaking (no match found)
            {
                yield return StartCoroutine(FallbackToAI());
            }
        }
        
        /// <summary>
        /// Connects to Nakama server
        /// </summary>
        private IEnumerator ConnectToNakama()
        {
            UpdateStatusText("Connecting to server...");
            
            // Simulate connection time
            yield return new WaitForSeconds(1f);
            
            // In real implementation, this would call NakamaManager.ConnectAndAuthenticate()
            // For now, we'll simulate a successful connection
            UpdateStatusText("Connected to server");
            yield return new WaitForSeconds(0.5f);
        }
        
        /// <summary>
        /// Searches for a match with timeout
        /// </summary>
        private IEnumerator SearchForMatch()
        {
            UpdateStatusText("Searching for players...");
            
            float searchTime = 0f;
            bool matchFound = false;
            
            while (searchTime < matchmakingTimeout && isMatchmaking && !matchFound)
            {
                searchTime += Time.deltaTime;
                
                // In real implementation, this would check NakamaManager for match results
                // For now, we'll simulate the search
                yield return null;
            }
            
            if (matchFound)
            {
                Debug.Log("[MatchmakingManager] Match found! Loading multiplayer scene");
                LoadMultiplayerScene();
            }
        }
        
        /// <summary>
        /// Falls back to AI game if no match found
        /// </summary>
        private IEnumerator FallbackToAI()
        {
            UpdateStatusText("No players found. Starting AI game...");
            yield return new WaitForSeconds(2f);
            
            Debug.Log("[MatchmakingManager] Loading AI scene as fallback");
            LoadAIScene();
        }
        
        /// <summary>
        /// Updates matchmaking status display
        /// </summary>
        private IEnumerator UpdateMatchmakingStatus()
        {
            while (isMatchmaking)
            {
                float elapsedTime = Time.time - searchStartTime;
                float remainingTime = matchmakingTimeout - elapsedTime;
                
                if (remainingTime > 0)
                {
                    int seconds = Mathf.CeilToInt(remainingTime);
                    UpdateCountdownText($"Searching... {seconds}s");
                }
                else
                {
                    UpdateCountdownText("Timeout reached");
                }
                
                yield return new WaitForSeconds(statusUpdateInterval);
            }
        }
        #endregion

        #region Scene Management
        /// <summary>
        /// Loads the multiplayer scene
        /// </summary>
        private void LoadMultiplayerScene()
        {
            Debug.Log($"[MatchmakingManager] Loading multiplayer scene: {multiplayerSceneName}");
            SceneManager.LoadScene(multiplayerSceneName);
        }
        
        /// <summary>
        /// Loads the AI scene
        /// </summary>
        private void LoadAIScene()
        {
            Debug.Log($"[MatchmakingManager] Loading AI scene: {aiSceneName}");
            SceneManager.LoadScene(aiSceneName);
        }
        #endregion

        #region UI Updates
        /// <summary>
        /// Updates the status text
        /// </summary>
        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[MatchmakingManager] Status: {message}");
        }
        
        /// <summary>
        /// Updates the countdown text
        /// </summary>
        private void UpdateCountdownText(string message)
        {
            if (countdownText != null)
            {
                countdownText.text = message;
            }
        }
        #endregion
    }
}
