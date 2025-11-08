using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Nakama;
using Nakama.TinyJson;

namespace CanvasChess
{
    /// <summary>
    /// Manages Nakama connection, authentication, and multiplayer functionality
    /// </summary>
    public class NakamaManager : MonoBehaviour
    {
        public static NakamaManager Instance;
        #region Serialized Fields
        [Header("Nakama Configuration")]
        [SerializeField] private NakamaConfig nakamaConfig = null!;
        
        [Header("UI References")]
        [SerializeField] private Button connectButton = null!;
        [SerializeField] private TMP_Text statusText = null!;
        [SerializeField] private TMP_InputField usernameInput = null!;
        
        [Header("Timer Selection")]
        [SerializeField] private Toggle rapidToggle = null!;
        [SerializeField] private Toggle blitzToggle = null!;
        [SerializeField] private Toggle bulletToggle = null!;
        [SerializeField] private TMP_Text selectedTimerText = null!;
        [SerializeField] private ToggleGroup timerToggleGroup = null!;
        #endregion

        #region Private Fields
        private IClient client = null!;
        private ISession session = null!;
        private ISocket socket = null!;
        private IMatch currentMatch = null!;
        
        private string desiredUserId = "";
        private bool isConnecting = false;
        private bool isSearchingForMatch = false;
        private bool autoSwitchEnabled = true;
        
        private float matchWaitTimer = 0f;
        private float searchTimer = 0f;
        private System.Action pendingUIUpdate = null;
        private float matchSyncDelay = 3f; // Give extra time for match synchronization
        private float matchStabilizationTimer = 0f; // Timer to track when match is stable
        private bool matchStabilized = false; // Flag to track if match is stable
        private bool hasSeenTwoPlayers = false; // Flag to track if we've seen 2 players at some point
        private bool sceneLoadingTriggered = false; // Prevent multiple scene loads
        private System.Collections.Generic.HashSet<string> seenPlayerIds = new System.Collections.Generic.HashSet<string>(); // Track unique players we've seen
        private float playerListLogTimer = 0f; // Timer for periodic player debug logs
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            Debug.Log("[NakamaManager] Start() called");
            statusText.text = "Enter username and press Connect.";
            connectButton.onClick.AddListener(OnConnectClicked);
            
            SetupTimerSelection();
            
            // Reset connection state when returning to menu
            isConnecting = false;
            isSearchingForMatch = false;
            matchWaitTimer = 0f;
            searchTimer = 0f;
            
            Debug.Log("[NakamaManager] Initialized");
        }

        private void Update()
        {
            // Process pending UI updates from background threads
            if (pendingUIUpdate != null)
            {
                pendingUIUpdate();
                pendingUIUpdate = null;
            }

            // Check if UI elements are still valid (scene might have changed)
            if (statusText == null || connectButton == null)
            {
                Debug.LogWarning("[NakamaManager] UI elements are null, likely scene changed. Disabling Update.");
                enabled = false;
                return;
            }

            if (isConnecting && currentMatch != null)
            {
                int totalPlayersInMatch = GetCurrentMatchPlayerCount();

                if (nakamaConfig.enableDebugLogging)
                {
                    playerListLogTimer += Time.deltaTime;
                    if (playerListLogTimer >= 1f)
                    {
                        playerListLogTimer = 0f;
                        LogCurrentPlayers("[NakamaManager] Player heartbeat");
                    }
                }

                if (totalPlayersInMatch < nakamaConfig.maxPlayers)
                {
                    matchWaitTimer += Time.deltaTime;
                    float totalWaitTime = nakamaConfig.matchWaitTimeout + matchSyncDelay;
                    float remainingTime = totalWaitTime - matchWaitTimer;

                    if (remainingTime > 0)
                    {
                        statusText.text = $"Waiting for players... ({totalPlayersInMatch}/{nakamaConfig.maxPlayers}) ({remainingTime:F1}s remaining)";
                    }
                    else if (!isSearchingForMatch)
                    {
                        Debug.Log($"[NakamaManager] Wait timer expired ({totalWaitTime}s). Starting search mode.");
                        StartSearchMode();
                    }
                }
                else
                {
                    // Enough players present, reset wait timer so it doesn't trigger search mode
                    matchWaitTimer = 0f;

                    if (!matchStabilized && hasSeenTwoPlayers && seenPlayerIds.Count >= nakamaConfig.maxPlayers)
                    {
                        matchStabilizationTimer += Time.deltaTime;
                        if (matchStabilizationTimer % 0.5f < Time.deltaTime || matchStabilizationTimer < Time.deltaTime)
                        {
                            Debug.Log($"[NakamaManager] Match stabilization: {seenPlayerIds.Count} players tracked, timer: {matchStabilizationTimer:F1}s / 2.0s");
                        }

                        if (matchStabilizationTimer >= 2f && !sceneLoadingTriggered)
                        {
                            matchStabilized = true;
                            sceneLoadingTriggered = true;
                            Debug.Log($"[NakamaManager] ✓✓✓ Match stabilized! Starting game with {seenPlayerIds.Count} players");
                            statusText.text = $"Match ready! Starting game...";
                            StartCoroutine(LoadGameSceneAfterDelay(1f));
                        }
                    }
                }
            }
            else if (isConnecting && currentMatch == null)
            {
                matchWaitTimer += Time.deltaTime;
                float remainingTime = nakamaConfig.matchWaitTimeout - matchWaitTimer;
                
                if (remainingTime > 0)
                {
                    statusText.text = $"Looking for players... ({remainingTime:F1}s remaining)";
                }
                else if (!isSearchingForMatch)
                {
                    // Time's up, start searching for matches
                    Debug.Log($"[NakamaManager] Matchmaking timeout expired ({nakamaConfig.matchWaitTimeout}s). Starting search mode.");
                    StartSearchMode();
                }
            }
            
            // Handle search timeout
            if (isSearchingForMatch)
            {
                searchTimer += Time.deltaTime;
                if (searchTimer >= nakamaConfig.matchmakingTimeout)
                {
                    // Search timeout, go to AI game
                    Debug.Log($"[NakamaManager] Search timeout reached ({nakamaConfig.matchmakingTimeout}s). Starting AI game");
                    if (statusText != null)
                    {
                        statusText.text = "Search timeout. Starting AI game...";
                    }
                    StartAIGame();
                }
                else if (searchTimer % 5f < Time.deltaTime) // Log every 5 seconds
                {
                    Debug.Log($"[NakamaManager] Search in progress... {searchTimer:F1}s elapsed, {nakamaConfig.matchmakingTimeout - searchTimer:F1}s remaining");
                }
            }
        }

        private void OnDestroy()
        {
            DisconnectFromNakama();
        }
        #endregion

        #region Connection Management
        /// <summary>
        /// Initializes the Nakama client
        /// </summary>
        private void InitializeNakamaClient()
        {
            if (nakamaConfig == null)
            {
                Debug.LogError("[NakamaManager] NakamaConfig is not assigned!");
                return;
            }
            
            client = new Client(nakamaConfig.scheme, nakamaConfig.host, nakamaConfig.port, nakamaConfig.serverKey, UnityWebRequestAdapter.Instance);
            Debug.Log($"[NakamaManager] Client initialized: {nakamaConfig.GetServerUrl()}");
        }

        /// <summary>
        /// Connects to Nakama and authenticates the user
        /// </summary>
        private async Task ConnectAndAuthenticate()
        {
            try
            {
                InitializeNakamaClient();
                
                // Authenticate with unique device ID or custom username
                // Generate unique device ID to avoid conflicts when running multiple instances
                string deviceId = SystemInfo.deviceUniqueIdentifier + "_" + System.Guid.NewGuid().ToString("N")[..8];
                session = await client.AuthenticateDeviceAsync(deviceId, usernameInput.text);
                
                Debug.Log($"[NakamaManager] Authenticated as: {session.Username} (UserID: {session.UserId})");
                Debug.Log($"[NakamaManager] Device ID used: {deviceId}");
                
                // Create socket connection
                socket = client.NewSocket();
                await socket.ConnectAsync(session);
                
                // Set up socket event handlers
                socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;
                socket.ReceivedMatchPresence += OnMatchPresence;
                socket.ReceivedMatchState += OnMatchData;
                
                Debug.Log("[NakamaManager] Connected to Nakama successfully");
                statusText.text = "Connected to Nakama!";
                
                // Start matchmaking
                await StartMatchmaking();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NakamaManager] Connection failed: {ex.Message}");
                statusText.text = $"Connection failed: {ex.Message}";
                isConnecting = false;
                connectButton.interactable = true;
            }
        }

        /// <summary>
        /// Disconnects from Nakama
        /// </summary>
        public async void DisconnectFromNakama()
        {
            try
            {
                var activeSocket = socket;

                if (currentMatch != null && activeSocket != null)
                {
                    await activeSocket.LeaveMatchAsync(currentMatch);
                    currentMatch = null;
                }

                if (activeSocket != null)
                {
                    await activeSocket.CloseAsync();
                }

                socket = null;
                
                Debug.Log("[NakamaManager] Disconnected from Nakama");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NakamaManager] Error during disconnect: {ex.Message}");
            }
        }
        #endregion

        #region Matchmaking
        /// <summary>
        /// Starts the matchmaking process
        /// </summary>
        private async Task StartMatchmaking()
        {
            try
            {
                string timerType = PlayerSettings.SelectedTimerType.ToString();
                
                Debug.Log($"[NakamaManager] Starting matchmaking with query: *, minPlayers: {nakamaConfig.minPlayers}, maxPlayers: {nakamaConfig.maxPlayers}");
                
                // Create matchmaker ticket
                var matchmakerTicket = await socket.AddMatchmakerAsync(
                    "*", // Match any players
                    nakamaConfig.minPlayers, nakamaConfig.maxPlayers, // Min/Max players
                    new Dictionary<string, string> { { "timerType", timerType } }
                );
                
                Debug.Log($"[NakamaManager] Matchmaker ticket created: {matchmakerTicket.Ticket}");
                
                statusText.text = $"Looking for a {timerType} game...";
                Debug.Log($"[NakamaManager] Matchmaking started for {timerType} game");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NakamaManager] Matchmaking failed: {ex.Message}");
                Debug.LogError($"[NakamaManager] Exception details: {ex}");
                statusText.text = $"Matchmaking failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Handles when matchmaker finds a match
        /// </summary>
        private async void OnMatchmakerMatched(IMatchmakerMatched matched)
        {
            Debug.Log($"[NakamaManager] Matchmaker matched: {matched.MatchId}");
            Debug.Log($"[NakamaManager] Matchmaker token: {matched.Token}");
            Debug.Log($"[NakamaManager] Matchmaker users: {(matched.Users != null ? matched.Users.Count() : 0)}");
            
            try
            {
                // Join the match
                currentMatch = await socket.JoinMatchAsync(matched);
                Debug.Log($"[NakamaManager] Successfully joined match: {currentMatch.Id}");
                Debug.Log($"[NakamaManager] Initial match presences: {currentMatch.Presences.Count()}");
                if (nakamaConfig.enableDebugLogging)
                {
                    var joinedPlayers = currentMatch.Presences
                        .Select(p => $"{p.Username} ({p.UserId})")
                        .ToArray();
                    Debug.Log($"[NakamaManager] Players in match on join: {string.Join(", ", joinedPlayers)}");
                }
                
                // Reset stabilization flags for new match
                matchStabilizationTimer = 0f;
                matchStabilized = false;
                sceneLoadingTriggered = false;
                seenPlayerIds.Clear();
                hasSeenTwoPlayers = false;
                playerListLogTimer = 0f;
                
                // CRITICAL: Stop search mode since we found a match!
                isSearchingForMatch = false;
                searchTimer = 0f;
                Debug.Log($"[NakamaManager] ✓ Stopped search mode - match found!");
                
                Debug.Log($"[NakamaManager] Reset stabilization flags for new match");
                
                // Ensure our own player is tracked
                if (session != null)
                {
                    seenPlayerIds.Add(session.UserId);
                }

                // Track all users provided by matchmaker response (can include opponents not yet present)
                if (matched.Users != null)
                {
                    foreach (var user in matched.Users)
                    {
                        var presence = user.Presence;
                        if (presence != null && !string.IsNullOrEmpty(presence.UserId))
                        {
                            seenPlayerIds.Add(presence.UserId);
                            if (nakamaConfig.enableDebugLogging)
                            {
                                Debug.Log($"[NakamaManager] Matchmaker user tracked: {presence.Username} ({presence.UserId})");
                            }
                        }
                    }
                }

                // Add all current presences to seenPlayerIds (including ourselves)
                foreach (var presence in currentMatch.Presences)
                {
                    seenPlayerIds.Add(presence.UserId);
                    Debug.Log($"[NakamaManager] Initial presence added: {presence.UserId} (Session: {presence.SessionId})");
                }
                
                Debug.Log($"[NakamaManager] Total players tracked: {seenPlayerIds.Count}");
                
                // Check if we already have enough players (rare but possible)
                if (seenPlayerIds.Count >= nakamaConfig.maxPlayers)
                {
                    hasSeenTwoPlayers = true;
                    Debug.Log($"[NakamaManager] ✓ Already have {seenPlayerIds.Count} players on join!");
                }
                
                // Store UI update for main thread processing
                pendingUIUpdate = () =>
                {
                    statusText.text = $"Joined match. Players: {seenPlayerIds.Count}/{nakamaConfig.maxPlayers}";
                    Debug.Log($"[NakamaManager] Joined match with {seenPlayerIds.Count} players tracked - waiting for more or stabilization");
                    
                    // Don't load scene immediately - wait for proper match stabilization
                    matchWaitTimer = 0f;
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NakamaManager] Failed to join match: {ex.Message}");
                Debug.LogError($"[NakamaManager] Exception details: {ex}");
                
                // Store UI update for main thread processing
                pendingUIUpdate = () =>
                {
                    statusText.text = $"Failed to join match: {ex.Message}";
                };
            }
        }

        /// <summary>
        /// Handles match presence changes (players joining/leaving)
        /// </summary>
        private void OnMatchPresence(IMatchPresenceEvent presenceEvent)
        {
            // Track unique players we've seen FIRST (before logging)
            if (presenceEvent.Joins != null)
            {
                foreach (var join in presenceEvent.Joins)
                {
                    seenPlayerIds.Add(join.UserId);
                    Debug.Log($"[NakamaManager] Player joined: {join.UserId} (Session: {join.SessionId})");
                    Debug.Log($"[NakamaManager] Added to seen list. Total unique players seen: {seenPlayerIds.Count}");
                }
            }
            
            if (presenceEvent.Leaves != null)
            {
                foreach (var leave in presenceEvent.Leaves)
                {
                    Debug.Log($"[NakamaManager] Player left: {leave.UserId} (Session: {leave.SessionId})");
                    
                    // Only remove from tracking if we haven't detected enough players yet
                    // Once we've seen enough players, keep them in tracking even if they temporarily disconnect
                    // This prevents temporary network issues during stabilization from breaking the match
                    if (!hasSeenTwoPlayers && !matchStabilized)
                    {
                        seenPlayerIds.Remove(leave.UserId);
                        Debug.Log($"[NakamaManager] Removed from tracking (pre-stabilization). Total: {seenPlayerIds.Count}");
                    }
                    else
                    {
                        Debug.Log($"[NakamaManager] Player left during/after stabilization - keeping in tracking for reliability. Total: {seenPlayerIds.Count}");
                    }
                }
            }
            
            // Calculate actual player count from presences list
            int opponentCount = currentMatch.Presences.Count();
            int totalPlayerCount = GetCurrentMatchPlayerCount();
            Debug.Log($"[NakamaManager] Match presence changed. Opponents in match: {opponentCount}, total players (including self): {totalPlayerCount}");
            Debug.Log($"[NakamaManager] Joins: {(presenceEvent.Joins != null ? presenceEvent.Joins.Count() : 0)}, Leaves: {(presenceEvent.Leaves != null ? presenceEvent.Leaves.Count() : 0)}");
            Debug.Log($"[NakamaManager] Unique players seen total: {seenPlayerIds.Count}");
            
            // Log all current presences after the event
            Debug.Log($"[NakamaManager] Current match presences after event:");
            foreach (var presence in currentMatch.Presences)
            {
                Debug.Log($"[NakamaManager]   → {presence.Username} ({presence.UserId}) Session: {presence.SessionId}");
            }

            if (nakamaConfig.enableDebugLogging)
            {
                var playerNames = currentMatch.Presences
                    .Select(p => $"{p.Username} ({p.UserId})")
                    .ToArray();
                Debug.Log($"[NakamaManager] Current players in match: {string.Join(", ", playerNames)}");
            }
            
            // Check if we have seen enough unique players (before resetting timer)
            // CRITICAL: Set hasSeenTwoPlayers on main thread to ensure Update() sees it immediately
            bool justDetectedEnoughPlayers = false;
            if (seenPlayerIds.Count >= nakamaConfig.maxPlayers && !hasSeenTwoPlayers)
            {
                Debug.Log($"[NakamaManager] ✓ Detected {seenPlayerIds.Count} unique players - ready to start!");
                justDetectedEnoughPlayers = true;
            }
            
            // Only reset timer if we haven't started the game yet AND we don't have enough players yet
            // Once we have enough players, keep the timer running even if someone leaves temporarily
            if (!matchStabilized && !hasSeenTwoPlayers && !justDetectedEnoughPlayers)
            {
                matchStabilizationTimer = 0f;
                Debug.Log($"[NakamaManager] Reset stabilization timer (not enough players yet)");
            }
            else if (hasSeenTwoPlayers || justDetectedEnoughPlayers)
            {
                Debug.Log($"[NakamaManager] Keeping stabilization timer running despite presence change (have {seenPlayerIds.Count} players)");
            }
            
            // Store the UI update data for processing on main thread
            // Use seenPlayerIds.Count instead of currentMatch.Presences.Count() for more reliable tracking
            // Capture the count at this moment to avoid timing issues
            int capturedPlayerCount = seenPlayerIds.Count;
            int capturedTotalPlayerCount = totalPlayerCount;
            bool shouldSetFlag = justDetectedEnoughPlayers;
            
            pendingUIUpdate = () =>
            {
                // Set hasSeenTwoPlayers flag on main thread if we just detected enough players
                // This ensures Update() sees the change in the very next frame
                if (shouldSetFlag)
                {
                    hasSeenTwoPlayers = true;
                    Debug.Log($"[NakamaManager] ✓✓ Set hasSeenTwoPlayers flag on main thread");
                }
                
                // Check using captured count which is reliable
                if (capturedPlayerCount >= nakamaConfig.maxPlayers && !sceneLoadingTriggered)
                {
                    // Start match stabilization timer
                    if (!matchStabilized)
                    {
                        Debug.Log($"[NakamaManager] ✓✓ Match has {capturedPlayerCount} players - starting stabilization timer");
                        statusText.text = $"Match ready! Players: {capturedPlayerCount}/{nakamaConfig.maxPlayers}";
                        matchStabilizationTimer = 0f;
                    }
                }
                else
                {
                    statusText.text = $"Waiting for players... ({capturedTotalPlayerCount}/{nakamaConfig.maxPlayers} found)";
                    Debug.Log($"[NakamaManager] Still waiting: {capturedTotalPlayerCount}/{nakamaConfig.maxPlayers} players");
                }
            };
        }

        /// <summary>
        /// Handles match data received from other players
        /// </summary>
        private void OnMatchData(IMatchState matchState)
        {
            // This will be handled by the GameManager
            Debug.Log($"[NakamaManager] Received match data from {matchState.UserPresence.Username}");
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Calculates the number of players currently in the match, including this client.
        /// </summary>
        private int GetCurrentMatchPlayerCount()
        {
            if (currentMatch == null)
                return 0;

            int opponentCount = currentMatch.Presences?.Count() ?? 0;
            int totalFromMatch = opponentCount + 1; // IMatch.Presences excludes the local player.
            int trackedCount = seenPlayerIds != null ? seenPlayerIds.Count : 0;
            return Mathf.Max(totalFromMatch, trackedCount);
        }

        /// <summary>
        /// Logs the current players in the match including the local player.
        /// </summary>
        private void LogCurrentPlayers(string context)
        {
            if (!nakamaConfig.enableDebugLogging || currentMatch == null)
                return;

            var players = currentMatch.Presences
                .Select(p => $"{p.Username} ({p.UserId})")
                .ToList();

            if (session != null && !players.Any(p => p.Contains(session.UserId)))
            {
                players.Add($"{session.Username} ({session.UserId}) [local]");
            }

            Debug.Log($"{context}: {string.Join(", ", players)}");
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles connect button click
        /// </summary>
        private async void OnConnectClicked()
        {
            Debug.Log("[NakamaManager] OnConnectClicked() called");
            string username = usernameInput.text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                statusText.text = "Please enter a username.";
                return;
            }
            
            desiredUserId = username;
            connectButton.interactable = false;
            statusText.text = "Connecting...";
            isConnecting = true;

            await ConnectAndAuthenticate();
        }

        /// <summary>
        /// Starts search mode when no players are found
        /// </summary>
        private async void StartSearchMode()
        {
            Debug.Log("[NakamaManager] StartSearchMode() called");
            isSearchingForMatch = true;
            searchTimer = 0f;
            
            if (statusText != null)
            {
                statusText.text = "No players found. Searching for available matches...";
            }
            
            // Leave current match and search for available matches
            if (currentMatch != null)
            {
                try
                {
                    await socket.LeaveMatchAsync(currentMatch);
                    currentMatch = null;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[NakamaManager] Error leaving match: {ex.Message}");
                }
            }
            
            // Start new matchmaking
            await StartMatchmaking();
        }

        /// <summary>
        /// Starts an AI game when matchmaking fails
        /// </summary>
        private void StartAIGame()
        {
            Debug.Log("[NakamaManager] StartAIGame() called");
            
            // Disconnect from Nakama
            DisconnectFromNakama();
            
            Debug.Log("[NakamaManager] Loading AI game scene");
            SceneManager.LoadScene("ChessGameVS_AI");
        }
        #endregion

        #region Scene Management
        /// <summary>
        /// Loads the game scene after a delay
        /// </summary>
        private IEnumerator LoadGameSceneAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Double-check that we haven't already triggered scene loading
            if (sceneLoadingTriggered)
            {
                Debug.Log($"[NakamaManager] Loading ChessGameMulti scene - Player: {session?.UserId}");
                
                // Assign player color based on position in match
                AssignPlayerColor();
                
                SceneManager.LoadScene("ChessGameMulti");
            }
        }
        
        /// <summary>
        /// Assigns player color based on position in the match
        /// </summary>
        private void AssignPlayerColor()
        {
            if (currentMatch == null || session == null)
            {
                Debug.LogWarning("[NakamaManager] Cannot assign color: currentMatch or session is null");
                PlayerSettings.PlayerColor = PieceColor.White;
                return;
            }
    
            // Use seenPlayerIds instead of currentMatch.Presences to avoid race conditions
            if (seenPlayerIds == null || seenPlayerIds.Count == 0)
            {
            Debug.LogWarning("[NakamaManager] No players tracked in seenPlayerIds.");
            PlayerSettings.PlayerColor = PieceColor.White;
            return;
            }

            // Sort seenPlayerIds for deterministic color assignment
            var sortedPlayerIds = seenPlayerIds.OrderBy(id => id, StringComparer.Ordinal).ToList();

            // Find our position in the sorted list
            int playerIndex = sortedPlayerIds.IndexOf(session.UserId);

            if (playerIndex == -1)
            {
            Debug.LogWarning("[NakamaManager] Could not find player in seenPlayerIds");
            PlayerSettings.PlayerColor = PieceColor.White;
            return;
            }

            // Assign colors: First player (index 0) gets White, second player (index 1) gets Black
            PlayerSettings.PlayerColor = playerIndex == 0 ? PieceColor.White : PieceColor.Black;

            Debug.Log($"[NakamaManager] Assigned player color: {PlayerSettings.PlayerColor} (Player index: {playerIndex})");
            Debug.Log($"[NakamaManager] Total players tracked: {sortedPlayerIds.Count}");

            // Log all players for debugging
            for (int i = 0; i < sortedPlayerIds.Count; i++)
            {
            var color = i == 0 ? "White" : "Black";
            var isLocal = sortedPlayerIds[i] == session.UserId ? " [LOCAL]" : "";
            Debug.Log($"[NakamaManager] Player {i}: {sortedPlayerIds[i]}{isLocal} -> {color}");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the current Nakama client
        /// </summary>
        public IClient GetClient()
        {
            return client;
        }

        /// <summary>
        /// Gets the current session
        /// </summary>
        public ISession GetSession()
        {
            return session;
        }

        /// <summary>
        /// Gets the current socket
        /// </summary>
        public ISocket GetSocket()
        {
            return socket;
        }

        /// <summary>
        /// Gets the current match
        /// </summary>
        public IMatch GetCurrentMatch()
        {
            return currentMatch;
        }

        /// <summary>
        /// Sends match data to other players
        /// </summary>
        public async Task SendMatchData(string opCode, byte[] data)
        {
            if (socket != null && currentMatch != null)
            {
                try
                {
                    await socket.SendMatchStateAsync(currentMatch.Id, 1, System.Text.Encoding.UTF8.GetString(data));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[NakamaManager] Failed to send match data: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Resets connection state
        /// </summary>
        public void ResetConnectionState()
        {
            Debug.Log("[NakamaManager] ResetConnectionState() called");
            isConnecting = false;
            isSearchingForMatch = false;
            matchWaitTimer = 0f;
            searchTimer = 0f;
            
            if (statusText != null)
            {
                statusText.text = "Enter username and press Connect.";
            }
            if (connectButton != null)
            {
                connectButton.interactable = true;
            }
        }
        #endregion

        #region Timer Selection
        /// <summary>
        /// Sets up the timer selection UI and event listeners
        /// </summary>
        private void SetupTimerSelection()
        {
            // Assign toggles to group if group is set
            if (timerToggleGroup != null)
            {
                if (rapidToggle != null) rapidToggle.group = timerToggleGroup;
                if (blitzToggle != null) blitzToggle.group = timerToggleGroup;
                if (bulletToggle != null) bulletToggle.group = timerToggleGroup;
            }

            // Remove all listeners first to avoid duplicate calls
            if (rapidToggle != null) rapidToggle.onValueChanged.RemoveAllListeners();
            if (blitzToggle != null) blitzToggle.onValueChanged.RemoveAllListeners();
            if (bulletToggle != null) bulletToggle.onValueChanged.RemoveAllListeners();

            if (rapidToggle != null)
                rapidToggle.onValueChanged.AddListener((isOn) => { if (isOn) OnTimerToggleChanged(PlayerSettings.TimerType.Rapid); });
            if (blitzToggle != null)
                blitzToggle.onValueChanged.AddListener((isOn) => { if (isOn) OnTimerToggleChanged(PlayerSettings.TimerType.Blitz); });
            if (bulletToggle != null)
                bulletToggle.onValueChanged.AddListener((isOn) => { if (isOn) OnTimerToggleChanged(PlayerSettings.TimerType.Bullet); });

            // Set initial state based on PlayerSettings
            if (rapidToggle != null) rapidToggle.isOn = PlayerSettings.SelectedTimerType == PlayerSettings.TimerType.Rapid;
            if (blitzToggle != null) blitzToggle.isOn = PlayerSettings.SelectedTimerType == PlayerSettings.TimerType.Blitz;
            if (bulletToggle != null) bulletToggle.isOn = PlayerSettings.SelectedTimerType == PlayerSettings.TimerType.Bullet;

            UpdateTimerDisplay();
        }

        /// <summary>
        /// Called when a timer toggle is changed
        /// </summary>
        private void OnTimerToggleChanged(PlayerSettings.TimerType timerType)
        {
            PlayerSettings.SelectedTimerType = timerType;
            UpdateTimerDisplay();
            Debug.Log($"[NakamaManager] Timer type changed to: {timerType}");
        }

        /// <summary>
        /// Updates the timer display to show the currently selected timer
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (selectedTimerText != null)
            {
                string timerText = PlayerSettings.SelectedTimerType switch
                {
                    PlayerSettings.TimerType.Rapid => "Rapid (10 minutes)",
                    PlayerSettings.TimerType.Blitz => "Blitz (5 minutes)",
                    PlayerSettings.TimerType.Bullet => "Bullet (3 minutes)",
                    _ => "Rapid (10 minutes)"
                };
                
                selectedTimerText.text = $"Selected: {timerText}";
            }
        }
        #endregion
    }
}
