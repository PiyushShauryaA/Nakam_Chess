using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net;

namespace CanvasChess.Editor
{
    /// <summary>
    /// Editor utility to install Nakama Unity SDK
    /// </summary>
    public class NakamaInstaller : EditorWindow
    {
        [MenuItem("Tools/Chess/Install Nakama SDK")]
        public static void ShowWindow()
        {
            GetWindow<NakamaInstaller>("Nakama SDK Installer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Nakama Unity SDK Installer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("This tool will help you install the Nakama Unity SDK:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("1. Download Nakama SDK"))
            {
                DownloadNakamaSDK();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("2. Install via Package Manager"))
            {
                InstallViaPackageManager();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("3. Manual Installation Instructions"))
            {
                ShowManualInstructions();
            }

            GUILayout.Space(10);
            GUILayout.Label("Alternative Installation Methods:", EditorStyles.boldLabel);
            GUILayout.Label("1. Download from GitHub: https://github.com/heroiclabs/nakama-unity/releases", EditorStyles.wordWrappedLabel);
            GUILayout.Label("2. Use OpenUPM CLI: openupm add com.heroiclabs.nakama-unity", EditorStyles.wordWrappedLabel);
            GUILayout.Label("3. Add to manifest.json manually", EditorStyles.wordWrappedLabel);
        }

        private void DownloadNakamaSDK()
        {
            string downloadUrl = "https://github.com/heroiclabs/nakama-unity/archive/refs/tags/v3.22.0.zip";
            string downloadPath = Path.Combine(Application.dataPath, "..", "Nakama-Unity.zip");
            
            Debug.Log($"Downloading Nakama SDK from: {downloadUrl}");
            Debug.Log($"Save to: {downloadPath}");
            
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(downloadUrl, downloadPath);
                }
                
                Debug.Log("Download completed! Please extract the zip file to your project's Packages folder.");
                Debug.Log("Then add this line to your manifest.json:");
                Debug.Log("\"com.heroiclabs.nakama-unity\": \"file:../Nakama-Unity\",");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Download failed: {ex.Message}");
            }
        }

        private void InstallViaPackageManager()
        {
            Debug.Log("To install via Package Manager:");
            Debug.Log("1. Open Window > Package Manager");
            Debug.Log("2. Click the '+' button");
            Debug.Log("3. Select 'Add package from git URL'");
            Debug.Log("4. Enter: https://github.com/heroiclabs/nakama-unity.git?path=/Nakama");
        }

        private void ShowManualInstructions()
        {
            Debug.Log("Manual Installation Instructions:");
            Debug.Log("1. Download Nakama Unity SDK from: https://github.com/heroiclabs/nakama-unity/releases");
            Debug.Log("2. Extract the downloaded package");
            Debug.Log("3. Copy the 'Nakama' folder to your project's 'Assets/Plugins/' directory");
            Debug.Log("4. Refresh Unity (Ctrl+R)");
            Debug.Log("5. The Nakama namespace should now be available");
        }
    }
}
