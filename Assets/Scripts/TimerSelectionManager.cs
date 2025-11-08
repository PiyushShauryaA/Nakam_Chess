using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CanvasChess
{
    /// <summary>
    /// Manages timer selection UI and settings
    /// </summary>
    public class TimerSelectionManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Timer Toggle Buttons")]
        [SerializeField] private Toggle rapidToggle = null!;
        [SerializeField] private Toggle blitzToggle = null!;
        [SerializeField] private Toggle bulletToggle = null!;
        
        [Header("Timer Labels")]
        [SerializeField] private TextMeshProUGUI rapidLabel = null!;
        [SerializeField] private TextMeshProUGUI blitzLabel = null!;
        [SerializeField] private TextMeshProUGUI bulletLabel = null!;
        
        [Header("Timer Display")]
        [SerializeField] private TextMeshProUGUI selectedTimerDisplay = null!;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            SetupTimerToggles();
            UpdateTimerDisplay();
        }
        #endregion

        #region Timer Setup
        /// <summary>
        /// Sets up the timer toggle buttons and their event listeners
        /// </summary>
        private void SetupTimerToggles()
        {
            // Set up toggle listeners
            if (rapidToggle != null)
            {
                rapidToggle.onValueChanged.AddListener((isOn) => OnTimerToggleChanged(PlayerSettings.TimerType.Rapid, isOn));
                rapidToggle.isOn = PlayerSettings.SelectedTimerType == PlayerSettings.TimerType.Rapid;
            }
            
            if (blitzToggle != null)
            {
                blitzToggle.onValueChanged.AddListener((isOn) => OnTimerToggleChanged(PlayerSettings.TimerType.Blitz, isOn));
                blitzToggle.isOn = PlayerSettings.SelectedTimerType == PlayerSettings.TimerType.Blitz;
            }
            
            if (bulletToggle != null)
            {
                bulletToggle.onValueChanged.AddListener((isOn) => OnTimerToggleChanged(PlayerSettings.TimerType.Bullet, isOn));
                bulletToggle.isOn = PlayerSettings.SelectedTimerType == PlayerSettings.TimerType.Bullet;
            }
            
            // Set up labels
            if (rapidLabel != null)
                rapidLabel.text = "Rapid (10 min)";
            
            if (blitzLabel != null)
                blitzLabel.text = "Blitz (5 min)";
            
            if (bulletLabel != null)
                bulletLabel.text = "Bullet (3 min)";
        }

        /// <summary>
        /// Called when a timer toggle is changed
        /// </summary>
        /// <param name="timerType">The timer type that was selected</param>
        /// <param name="isOn">Whether the toggle is now on</param>
        private void OnTimerToggleChanged(PlayerSettings.TimerType timerType, bool isOn)
        {
            if (isOn)
            {
                PlayerSettings.SelectedTimerType = timerType;
                UpdateTimerDisplay();
                Debug.Log($"Timer type changed to: {timerType}");
            }
        }

        /// <summary>
        /// Updates the timer display to show the currently selected timer
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (selectedTimerDisplay != null)
            {
                string timerText = PlayerSettings.SelectedTimerType switch
                {
                    PlayerSettings.TimerType.Rapid => "Rapid (10 minutes)",
                    PlayerSettings.TimerType.Blitz => "Blitz (5 minutes)",
                    PlayerSettings.TimerType.Bullet => "Bullet (3 minutes)",
                    _ => "Rapid (10 minutes)"
                };
                
                selectedTimerDisplay.text = $"Selected: {timerText}";
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the currently selected timer duration in seconds
        /// </summary>
        /// <returns>Timer duration in seconds</returns>
        public float GetSelectedTimerDuration()
        {
            return PlayerSettings.GetTimerDuration();
        }

        /// <summary>
        /// Gets the currently selected timer type
        /// </summary>
        /// <returns>The selected timer type</returns>
        public PlayerSettings.TimerType GetSelectedTimerType()
        {
            return PlayerSettings.SelectedTimerType;
        }

        /// <summary>
        /// Sets the timer type programmatically
        /// </summary>
        /// <param name="timerType">The timer type to set</param>
        public void SetTimerType(PlayerSettings.TimerType timerType)
        {
            PlayerSettings.SelectedTimerType = timerType;
            
            // Update toggle states
            if (rapidToggle != null)
                rapidToggle.isOn = timerType == PlayerSettings.TimerType.Rapid;
            
            if (blitzToggle != null)
                blitzToggle.isOn = timerType == PlayerSettings.TimerType.Blitz;
            
            if (bulletToggle != null)
                bulletToggle.isOn = timerType == PlayerSettings.TimerType.Bullet;
            
            UpdateTimerDisplay();
        }
        #endregion
    }
} 