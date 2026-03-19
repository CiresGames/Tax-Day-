using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.Radio
{
    [AddComponentMenu("Signalia/Tools/Signalia | Haptics")]
    /// <summary>
    /// Standalone component for triggering haptic feedback. Can be used independently of audio or attached to UI elements.
    /// </summary>
    public class HapticScript : MonoBehaviour
    {
        [SerializeField] private HapticSettings hapticSettings = new HapticSettings();
        [SerializeField] private bool enableHaptics = true;   
        [SerializeField] private bool triggerOnStart = false;
        [SerializeField] private bool triggerOnEnable = false;
        [SerializeField] private bool triggerOnDisable = false;
        [SerializeField] private string[] eventNamesToListen = new string[0];
        [SerializeField] private bool listenToEvents = false;

        private void Start()
        {
            if (triggerOnStart)
            {
                TriggerHaptic();
            }

            if (listenToEvents && eventNamesToListen.Length > 0)
            {
                SetupEventListeners();
            }
        }

        private void OnEnable()
        {
            if (triggerOnEnable)
            {
                TriggerHaptic();
            }
        }

        private void OnDisable()
        {
            if (triggerOnDisable)
            {
                TriggerHaptic();
            }
        }

        /// <summary>
        /// Sets up event listeners for automatic haptic triggering
        /// </summary>
        private void SetupEventListeners()
        {
            foreach (var eventName in eventNamesToListen)
            {
                if (!string.IsNullOrEmpty(eventName))
                {
                    SIGS.Listener(eventName, () => TriggerHaptic(), false, gameObject);
                }
            }
        }

        /// <summary>
        /// Triggers haptic feedback using the component's settings
        /// </summary>
        public void TriggerHaptic()
        {
            if (enableHaptics)
            {
                HapticsManager.TriggerHaptic(hapticSettings);
            }
        }

        /// <summary>
        /// Triggers haptic feedback with custom settings
        /// </summary>
        /// <param name="hapticType">Type of haptic to trigger</param>
        /// <param name="intensity">Intensity of the haptic (0-1)</param>
        /// <param name="duration">Duration of the haptic in seconds</param>
        public void TriggerHaptic(HapticType hapticType, float intensity = 1f, float duration = 0.1f)
        {
            if (enableHaptics)
            {
                HapticsManager.TriggerHaptic(hapticType, intensity, duration);
            }
        }

        /// <summary>
        /// Triggers haptic feedback using custom settings
        /// </summary>
        /// <param name="settings">Haptic settings to use</param>
        public void TriggerHaptic(HapticSettings settings)
        {
            if (enableHaptics)
            {
                HapticsManager.TriggerHaptic(settings);
            }
        }

        /// <summary>
        /// Triggers a preset haptic type
        /// </summary>
        /// <param name="type">Preset haptic type</param>
        public void TriggerHapticPreset(HapticType type)
        {
            if (enableHaptics)
            {
                HapticsManager.TriggerHapticPreset(type);
            }
        }

        /// <summary>
        /// Sets the haptic settings for this component
        /// </summary>
        /// <param name="settings">New haptic settings</param>
        public void SetHapticSettings(HapticSettings settings)
        {
            hapticSettings = settings;
        }

        /// <summary>
        /// Enables or disables haptics for this component
        /// </summary>
        /// <param name="enabled">Whether haptics should be enabled</param>
        public void SetHapticsEnabled(bool enabled)
        {
            enableHaptics = enabled;
        }

        /// <summary>
        /// Gets the current haptic settings
        /// </summary>
        /// <returns>Current haptic settings</returns>
        public HapticSettings GetHapticSettings()
        {
            return hapticSettings;
        }

        /// <summary>
        /// Checks if haptics are enabled for this component
        /// </summary>
        /// <returns>True if haptics are enabled</returns>
        public bool IsHapticsEnabled()
        {
            return enableHaptics;
        }

        /// <summary>
        /// Triggers haptic feedback with a delay
        /// </summary>
        /// <param name="delay">Delay in seconds before triggering</param>
        public void TriggerHapticDelayed(float delay)
        {
            if (enableHaptics)
            {
                SIGS.DoIn(delay, () => TriggerHaptic());
            }
        }

        /// <summary>
        /// Triggers haptic feedback repeatedly
        /// </summary>
        /// <param name="interval">Interval between haptic triggers in seconds</param>
        /// <param name="count">Number of times to trigger (0 = infinite)</param>
        public void TriggerHapticRepeated(float interval, int count = 0)
        {
            if (enableHaptics)
            {
                if (count <= 0)
                {
                    // Infinite repetition
                    SIGS.DoEveryInterval(interval, () => TriggerHaptic());
                }
                else
                {
                    // Limited repetition
                    for (int i = 0; i < count; i++)
                    {
                        SIGS.DoIn(i * interval, () => TriggerHaptic());
                    }
                }
            }
        }

        /// <summary>
        /// Stops all haptic feedback
        /// </summary>
        public void StopAllHaptics()
        {
            HapticsManager.StopAllHaptics();
        }

        /// <summary>
        /// Gets information about connected haptic devices
        /// </summary>
        /// <returns>String containing device information</returns>
        public string GetHapticDeviceInfo()
        {
            return HapticsManager.GetHapticDeviceInfo();
        }

        /// <summary>
        /// Public method for UI button events
        /// </summary>
        public void OnButtonClick()
        {
            TriggerHaptic();
        }

        /// <summary>
        /// Public method for UI button events with custom haptic type
        /// </summary>
        /// <param name="hapticType">Type of haptic to trigger</param>
        public void OnButtonClick(HapticType hapticType)
        {
            TriggerHapticPreset(hapticType);
        }

        /// <summary>
        /// Public method for UI button events with custom settings
        /// </summary>
        /// <param name="intensity">Intensity of the haptic</param>
        /// <param name="duration">Duration of the haptic</param>
        public void OnButtonClick(float intensity, float duration)
        {
            TriggerHaptic(hapticSettings.HapticType, intensity, duration);
        }
    }
}
