using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using TMPro;
using UnityEngine;
using System.Linq;
using AHAKuo.Signalia.UI;
using System;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics
{
    [AddComponentMenu("Signalia/Game Systems/Common Mechanics/Signalia | Currency Displayer")]
    /// <summary>
    /// Helper component that automatically updates TMPText fields with currency values and supports audio effects.
    /// </summary>
    public class CurrencyHelper : MonoBehaviour
    {
        [SerializeField] private string currencyName = "gold";
        [SerializeField] private TextMeshProUGUI targetText;
        [SerializeField] private bool listenForUpdates = true;
        
        [SerializeField] private string displayFormat = "{0}";
        [SerializeField] private bool useLocalization = false;
        [SerializeField] private string localizationPrefix = "";
        [SerializeField] private bool useCommaFormatting = true;
        [SerializeField] private int decimalPlaces = 0;
        [SerializeField] private bool alwaysShowDecimals = false;
        
        [SerializeField] private string increaseAudioKey = "";
        [SerializeField] private string decreaseAudioKey = "";
        
        [SerializeField] private bool playHapticsOnIncrease = false;
        [SerializeField] private bool playHapticsOnDecrease = false;
        [SerializeField] private HapticType increaseHapticType = HapticType.Light;
        [SerializeField] private HapticType decreaseHapticType = HapticType.Light;
        
        [SerializeField] private UIAnimationAsset increaseAnimation;
        [SerializeField] private UIAnimationAsset decreaseAnimation;
        
        private float lastKnownValue = 0f;
        private bool isInitialized = false;
        private Listener currencyUpdateListener;

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (isInitialized && listenForUpdates)
            {
                SubscribeToCurrencyUpdates();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromCurrencyUpdates();
        }

        /// <summary>
        /// Initialize the currency helper and set up listeners.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            // Initialize animations
            InitializeAnimations();

            // Auto-find TMPText if not assigned
            if (targetText == null)
            {
                targetText = GetComponent<TextMeshProUGUI>();
            }

            // Get currency from cache (loads from disk only if not already cached)
            var currency = CMN_Currencies.GetCurrency(currencyName);
            lastKnownValue = currency.value;
            
            // Update display
            UpdateDisplay(currency.value);
            
            // Subscribe to updates
            if (listenForUpdates)
            {
                SubscribeToCurrencyUpdates();
            }
            
            isInitialized = true;
        }

        /// <summary>
        /// Initialize animation assets by creating instances.
        /// </summary>
        private void InitializeAnimations()
        {
            if (increaseAnimation != null)
            {
                increaseAnimation = increaseAnimation.CreateInstance();
            }

            if (decreaseAnimation != null)
            {
                decreaseAnimation = decreaseAnimation.CreateInstance();
            }
        }

        /// <summary>
        /// Subscribe to currency update events.
        /// </summary>
        private void SubscribeToCurrencyUpdates()
        {
            string eventName = currencyName + "sigs_u_pickedup";
            currencyUpdateListener = SIGS.Listener(eventName, (s) => OnCurrencyUpdated((float)s[0]));
        }

        /// <summary>
        /// Unsubscribe from currency update events.
        /// </summary>
        private void UnsubscribeFromCurrencyUpdates()
        {
            currencyUpdateListener?.Dispose();
            currencyUpdateListener = null;
        }

        /// <summary>
        /// Handle currency value updates.
        /// </summary>
        /// <param name="newValue">The new currency value</param>
        private void OnCurrencyUpdated(object newValue)
        {
            if (newValue is float value)
            {
                UpdateDisplay(value);
                
                // Play audio/haptic/animation feedback based on change direction
                float difference = value - lastKnownValue;
                if (difference > 0 && HasIncreaseFeedback())
                {
                    PlayIncreaseFeedback();
                }
                else if (difference < 0 && HasDecreaseFeedback())
                {
                    PlayDecreaseFeedback();
                }
                
                lastKnownValue = value;
            }
        }

        /// <summary>
        /// Update the display with the current currency value.
        /// </summary>
        /// <param name="value">The currency value to display</param>
        public void UpdateDisplay(float value)
        {
            if (targetText == null) return;

            string displayText;
            
            if (useLocalization && !string.IsNullOrEmpty(localizationPrefix))
            {
                string formattedValue = useCommaFormatting ? FormatNumberWithCommas(value) : value.ToString();
                string localizedValue = localizationPrefix + formattedValue;
                displayText = string.Format(displayFormat, localizedValue);
            }
            else
            {
                string formattedValue = useCommaFormatting ? FormatNumberWithCommas(value) : value.ToString();
                displayText = string.Format(displayFormat, formattedValue);
            }
            
            targetText.text = displayText;
        }

        /// <summary>
        /// Format a number with commas and decimal places for better readability.
        /// </summary>
        /// <param name="value">The number to format</param>
        /// <returns>Formatted string with commas and decimal places</returns>
        private string FormatNumberWithCommas(float value)
        {
            if (decimalPlaces == 0)
            {
                // Convert to integer for comma formatting, then back to string
                long intValue = (long)value;
                return intValue.ToString("N0");
            }
            else
            {
                // Format with decimal places
                string formatString = "N" + decimalPlaces;
                string formatted = value.ToString(formatString);
                
                // If alwaysShowDecimals is false and the decimal part is all zeros, remove it
                if (!alwaysShowDecimals)
                {
                    // Check if decimal part is all zeros
                    string[] parts = formatted.Split('.');
                    if (parts.Length > 1)
                    {
                        string decimalPart = parts[1];
                        if (decimalPart.All(c => c == '0'))
                        {
                            return parts[0];
                        }
                    }
                }
                
                return formatted;
            }
        }

        /// <summary>
        /// Play audio, haptic feedback, and animation for currency increase.
        /// </summary>
        private void PlayIncreaseFeedback()
        {
            // Play audio
            if (!string.IsNullOrEmpty(increaseAudioKey))
            {
                SIGS.PlayAudio(increaseAudioKey);
            }
            
            // Play haptics
            if (playHapticsOnIncrease)
            {
                SIGS.TriggerHaptic(increaseHapticType);
            }
            
            // Play animation
            if (increaseAnimation != null)
            {
                increaseAnimation.PerformAnimation(null, this.gameObject, false);
            }
        }

        private bool HasIncreaseFeedback()
        {
            return !string.IsNullOrEmpty(increaseAudioKey)
                || playHapticsOnIncrease
                || increaseAnimation != null;
        }

        private bool HasDecreaseFeedback()
        {
            return !string.IsNullOrEmpty(decreaseAudioKey)
                || playHapticsOnDecrease
                || decreaseAnimation != null;
        }

        /// <summary>
        /// Play audio, haptic feedback, and animation for currency decrease.
        /// </summary>
        private void PlayDecreaseFeedback()
        {
            // Play audio
            if (!string.IsNullOrEmpty(decreaseAudioKey))
            {
                SIGS.PlayAudio(decreaseAudioKey);
            }
            
            // Play haptics
            if (playHapticsOnDecrease)
            {
                SIGS.TriggerHaptic(decreaseHapticType);
            }
            
            // Play animation
            if (decreaseAnimation != null)
            {
                decreaseAnimation.PerformAnimation(null, this.gameObject, false);
            }
        }

        /// <summary>
        /// Manually refresh the currency display.
        /// </summary>
        public void RefreshDisplay()
        {
            var currency = CMN_Currencies.GetCurrency(currencyName);
            UpdateDisplay(currency.value);
            lastKnownValue = currency.value;
        }

        /// <summary>
        /// Set a new currency name and refresh.
        /// </summary>
        /// <param name="newCurrencyName">The new currency name</param>
        public void SetCurrencyName(string newCurrencyName)
        {
            if (isInitialized)
            {
                UnsubscribeFromCurrencyUpdates();
            }
            
            currencyName = newCurrencyName;
            
            if (isInitialized)
            {
                RefreshDisplay();
                if (listenForUpdates)
                {
                    SubscribeToCurrencyUpdates();
                }
            }
        }

        /// <summary>
        /// Get the current currency value.
        /// </summary>
        /// <returns>The current currency value</returns>
        public float GetCurrentValue()
        {
            var currency = CMN_Currencies.GetCurrency(currencyName);
            return currency.value;
        }

        /// <summary>
        /// Modify the currency value by the specified amount.
        /// </summary>
        /// <param name="amount">Amount to add/subtract</param>
        /// <param name="autoSave">Whether to auto-save the change</param>
        /// <param name="notify">Whether to trigger update events</param>
        public void ModifyCurrency(float amount, bool autoSave = true, bool notify = true)
        {
            CMN_Currencies.ModifyCurrency(currencyName, amount, autoSave, notify);
        }

        /// <summary>
        /// Set the currency value directly.
        /// </summary>
        /// <param name="value">The new currency value</param>
        /// <param name="autoSave">Whether to auto-save the change</param>
        /// <param name="notify">Whether to trigger update events</param>
        public void SetCurrencyValue(float value, bool autoSave = true, bool notify = true)
        {
            var currency = CMN_Currencies.GetCurrency(currencyName);
            float difference = value - currency.value;
            CMN_Currencies.ModifyCurrency(currencyName, difference, autoSave, notify);
        }
    }
}
