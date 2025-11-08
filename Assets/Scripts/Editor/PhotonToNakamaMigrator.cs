using UnityEngine;
using UnityEditor;
using System.IO;

namespace CanvasChess.Editor
{
    /// <summary>
    /// Editor utility to help migrate from Photon to Nakama
    /// </summary>
    public class PhotonToNakamaMigrator : EditorWindow
    {
        [MenuItem("Tools/Chess/Migrate Photon to Nakama")]
        public static void ShowWindow()
        {
            GetWindow<PhotonToNakamaMigrator>("Photon to Nakama Migrator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Photon to Nakama Migration Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("This tool will help you migrate from Photon to Nakama:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("1. Remove Photon Dependencies"))
            {
                RemovePhotonDependencies();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("2. Update GameManager References"))
            {
                UpdateGameManagerReferences();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("3. Update Scene References"))
            {
                UpdateSceneReferences();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("4. Clean Up Photon Files"))
            {
                CleanUpPhotonFiles();
            }

            GUILayout.Space(10);
            GUILayout.Label("Migration Steps:", EditorStyles.boldLabel);
            GUILayout.Label("1. Remove Photon Dependencies from manifest.json", EditorStyles.wordWrappedLabel);
            GUILayout.Label("2. Replace PhotonSimpleConnect with NakamaManager in scenes", EditorStyles.wordWrappedLabel);
            GUILayout.Label("3. Replace GameManager with GameManagerNakama in multiplayer scenes", EditorStyles.wordWrappedLabel);
            GUILayout.Label("4. Replace UIManager with UIManagerNakama in multiplayer scenes", EditorStyles.wordWrappedLabel);
            GUILayout.Label("5. Remove Photon-related scripts and assets", EditorStyles.wordWrappedLabel);
        }

        private void RemovePhotonDependencies()
        {
            string manifestPath = "Packages/manifest.json";
            if (File.Exists(manifestPath))
            {
                string content = File.ReadAllText(manifestPath);
                
                // Remove Photon-related dependencies
                content = System.Text.RegularExpressions.Regex.Replace(content, 
                    @"""com\.photon\.pun"":\s*""[^""]*"",?\s*", "");
                content = System.Text.RegularExpressions.Regex.Replace(content, 
                    @"""com\.photon\.chchat"":\s*""[^""]*"",?\s*", "");
                content = System.Text.RegularExpressions.Regex.Replace(content, 
                    @"""com\.photon\.realtime"":\s*""[^""]*"",?\s*", "");
                
                // Clean up any trailing commas
                content = System.Text.RegularExpressions.Regex.Replace(content, 
                    @",\s*}", "}");
                content = System.Text.RegularExpressions.Regex.Replace(content, 
                    @",\s*]", "]");
                
                File.WriteAllText(manifestPath, content);
                AssetDatabase.Refresh();
                
                Debug.Log("Photon dependencies removed from manifest.json");
            }
        }

        private void UpdateGameManagerReferences()
        {
            Debug.Log("Please manually update the following in your scenes:");
            Debug.Log("- Replace PhotonSimpleConnect component with NakamaManager");
            Debug.Log("- Replace GameManager component with GameManagerNakama in ChessGameMulti scene");
            Debug.Log("- Replace UIManager component with UIManagerNakama in ChessGameMulti scene");
            Debug.Log("- Update all references to use NakamaManager instead of PhotonSimpleConnect");
        }

        private void UpdateSceneReferences()
        {
            Debug.Log("Scene references updated. Please check the following scenes:");
            Debug.Log("- MainMenuMulti: Should use NakamaManager");
            Debug.Log("- ChessGameMulti: Should use GameManagerNakama and UIManagerNakama");
            Debug.Log("- ChessGameVS_AI: Should remain unchanged (uses GameManagerAI)");
        }

        private void CleanUpPhotonFiles()
        {
            Debug.Log("Cleaning up Photon files...");
            
            // List of Photon-related files to remove
            string[] filesToRemove = {
                "Assets/Scripts/PhotonSimpleConnect.cs",
                "Assets/Scripts/PhotonPlayerInfo.cs",
                "Assets/Scripts/MultiplayerTurnDebugger.cs"
            };

            foreach (string file in filesToRemove)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    File.Delete(file + ".meta");
                    Debug.Log($"Removed: {file}");
                }
            }

            // Remove Photon folder if it exists
            string photonFolder = "Assets/Photon";
            if (Directory.Exists(photonFolder))
            {
                Directory.Delete(photonFolder, true);
                File.Delete(photonFolder + ".meta");
                Debug.Log($"Removed Photon folder: {photonFolder}");
            }

            AssetDatabase.Refresh();
            Debug.Log("Photon files cleanup completed");
        }
    }
}
