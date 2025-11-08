using UnityEngine;
using UnityEngine.SceneManagement;

namespace CanvasChess
{
    public class BackToMenuButton : MonoBehaviour
    {
        // Call this from the button's OnClick event
        public void OnBackToMenuClicked()
        {
            Debug.Log("[BackToMenuButton] Returning to menu");
            
            // Check if we have a NakamaManager to disconnect from
            NakamaManager nakamaManager = FindObjectOfType<NakamaManager>();
            if (nakamaManager != null)
            {
                nakamaManager.DisconnectFromNakama();
                // Wait a short time before loading the menu to allow disconnect to complete
                StartCoroutine(LoadMenuAfterDelay(0.2f));
            }
            else
            {
                // Load the main menu scene immediately if no multiplayer connection
                SceneManager.LoadScene("MainMenuMulti");
            }
        }

        private System.Collections.IEnumerator LoadMenuAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene("MainMenuMulti");
        }
    }
} 