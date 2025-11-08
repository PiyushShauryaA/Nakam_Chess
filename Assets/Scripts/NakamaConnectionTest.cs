using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nakama;

namespace CanvasChess
{
    /// <summary>
    /// Simple test script to verify Nakama connection is working
    /// </summary>
    public class NakamaConnectionTest : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text statusText = null!;
        [SerializeField] private TMP_Text serverInfoText = null!;
        
        [Header("Test Settings")]
        [SerializeField] private string testUsername = "TestPlayer";
        [SerializeField] private string serverUrl = "http://localhost:7350";
        [SerializeField] private string serverKey = "defaultkey";
        
        private IClient client = null!;
        private ISession session = null!;
        private ISocket socket = null!;
        
        private void Start()
        {
            TestNakamaConnection();
        }
        
        /// <summary>
        /// Tests the Nakama connection
        /// </summary>
        private async void TestNakamaConnection()
        {
            try
            {
                UpdateStatus("Testing Nakama connection...");
                
                // Create client
                client = new Client(serverKey, serverUrl, 7350, "http");
                UpdateStatus("Client created successfully");
                
                // Authenticate
                session = await client.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier, testUsername);
                UpdateStatus($"Authenticated as: {session.Username}");
                
                // Create socket
                socket = client.NewSocket();
                await socket.ConnectAsync(session);
                UpdateStatus("Socket connected successfully");
                
                // Test matchmaking
                await TestMatchmaking();
                
                UpdateStatus("✅ Nakama connection test PASSED!");
                UpdateServerInfo($"Server: {serverUrl}\nUsername: {session.Username}\nSession ID: {session.AuthToken}");
            }
            catch (System.Exception ex)
            {
                UpdateStatus($"❌ Nakama connection test FAILED: {ex.Message}");
                Debug.LogError($"[NakamaConnectionTest] Error: {ex}");
            }
        }
        
        /// <summary>
        /// Tests matchmaking functionality
        /// </summary>
        private async Task TestMatchmaking()
        {
            try
            {
                UpdateStatus("Testing matchmaking...");
                
                // Create matchmaking ticket
                var matchmakerTicket = await socket.AddMatchmakerAsync("*", 2, 2, new Dictionary<string, string>());
                UpdateStatus($"Matchmaking ticket created: {matchmakerTicket.Ticket}");
                
                // Wait a bit for potential matches
                await Task.Delay(2000);
                
                // Cancel matchmaking
                await socket.RemoveMatchmakerAsync(matchmakerTicket);
                UpdateStatus("Matchmaking test completed");
            }
            catch (System.Exception ex)
            {
                UpdateStatus($"Matchmaking test failed: {ex.Message}");
                Debug.LogError($"[NakamaConnectionTest] Matchmaking error: {ex}");
            }
        }
        
        /// <summary>
        /// Updates the status text
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[NakamaConnectionTest] {message}");
        }
        
        /// <summary>
        /// Updates the server info text
        /// </summary>
        private void UpdateServerInfo(string info)
        {
            if (serverInfoText != null)
            {
                serverInfoText.text = info;
            }
        }
        
        private void OnDestroy()
        {
            // Clean up connections
            if (socket != null)
            {
                socket.CloseAsync();
            }
        }
    }
}
