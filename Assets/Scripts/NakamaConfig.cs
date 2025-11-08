using UnityEngine;

namespace CanvasChess
{
    /// <summary>
    /// Configuration settings for Nakama server connection
    /// </summary>
    [CreateAssetMenu(fileName = "NakamaConfig", menuName = "Chess/Nakama Config")]
    public class NakamaConfig : ScriptableObject
    {
        [Header("Nakama Server Settings")]
        [Tooltip("Protocol scheme (http or https)")]
        public string scheme = "http";
        
        [Tooltip("Server host address")]
        public string host = "localhost";
        
        [Tooltip("Server port number")]
        public int port = 7350;
        
        [Tooltip("Server key for authentication")]
        public string serverKey = "defaultkey";
        
        [Header("Match Settings")]
        [Tooltip("Minimum players required for a match")]
        public int minPlayers = 2;
        
        [Tooltip("Maximum players allowed in a match")]
        public int maxPlayers = 2;
        
        [Tooltip("Timeout for matchmaking search (seconds)")]
        public float matchmakingTimeout = 15f;
        
        [Tooltip("Timeout for waiting for players in a match (seconds)")]
        public float matchWaitTimeout = 10f;
        
        [Header("Connection Settings")]
        [Tooltip("Connection timeout (seconds)")]
        public float connectionTimeout = 10f;
        
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = true;
        
        /// <summary>
        /// Gets the full server URL
        /// </summary>
        public string GetServerUrl()
        {
            return $"{scheme}://{host}:{port}";
        }
        
        /// <summary>
        /// Validates the configuration
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(host) && 
                   port > 0 && port < 65536 && 
                   !string.IsNullOrEmpty(serverKey);
        }
        
        private void OnValidate()
        {
            // Ensure port is within valid range
            port = Mathf.Clamp(port, 1, 65535);
            
            // Ensure timeouts are positive
            matchmakingTimeout = Mathf.Max(1f, matchmakingTimeout);
            matchWaitTimeout = Mathf.Max(1f, matchWaitTimeout);
            connectionTimeout = Mathf.Max(1f, connectionTimeout);
            
            // Ensure player counts are valid
            minPlayers = Mathf.Clamp(minPlayers, 1, maxPlayers);
            maxPlayers = Mathf.Clamp(maxPlayers, minPlayers, 8);
        }
    }
}
