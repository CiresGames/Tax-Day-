using UnityEngine;
using AHAKuo.Signalia.Framework;
using System.Collections.Generic;

namespace AHAKuo.Signalia.Utilities.SIGInput
{
    /// <summary>
    /// Debug component that shows burner notifications for input events.
    /// Automatically displays input actions as burner notifications at the "Input" burner spot.
    /// Useful for debugging and visualizing input in real-time.
    /// </summary>
    [AddComponentMenu("Signalia/Utilities/Input/Signalia | Input Burner")]
    public class InputBurner : MonoBehaviour
    {
        [Tooltip("Whether to show burners for input down events")]
        [SerializeField] private bool showOnDown = true;
        
        [Tooltip("Whether to show burners for input held events (can be spammy)")]
        [SerializeField] private bool showOnHeld = false;
        
        [Tooltip("Whether to show burners for input up events")]
        [SerializeField] private bool showOnUp = false;

        [Tooltip("Cooldown between burners for the same action (in seconds). Prevents spam.")]
        [SerializeField] private float cooldownPerAction = 0.1f;

        [Tooltip("Format string for burner messages. {0} = action name, {1} = state (Down/Held/Up)")]
        [SerializeField] private string messageFormat = "{0} {1}";

        [Tooltip("List of action names to monitor. Leave empty to monitor all actions.")]
        [SerializeField] private List<string> monitoredActions = new List<string>();

        private Dictionary<string, float> lastBurnTime = new Dictionary<string, float>();
        private HashSet<string> cachedActionNames = new HashSet<string>();
        private int lastActionMapFrame = -1;

        private void Awake()
        {
            Watchman.Watch();
        }

        private void Update()
        {
            // Cache action names (refresh every frame in case action maps change)
            if (Time.frameCount != lastActionMapFrame)
            {
                RefreshActionNames();
                lastActionMapFrame = Time.frameCount;
            }

            // Check each cached action
            foreach (string actionName in cachedActionNames)
            {
                if (showOnDown && SIGS.GetInputDown(actionName, true))
                {
                    ShowBurner(actionName, "Down");
                }
                
                if (showOnHeld && SIGS.GetInput(actionName))
                {
                    ShowBurner(actionName, "Held");
                }
                
                if (showOnUp && SIGS.GetInputUp(actionName, true))
                {
                    ShowBurner(actionName, "Up");
                }
            }
        }

        private void RefreshActionNames()
        {
            cachedActionNames.Clear();
            
            // Get all action names from input maps
            var actionMaps = ResourceHandler.GetInputActionMaps();
            if (actionMaps == null || actionMaps.Length == 0)
            {
                return;
            }

            // Build list of actions to check
            foreach (var map in actionMaps)
            {
                if (map == null || map.Actions == null) continue;
                
                foreach (var action in map.Actions)
                {
                    if (action == null || string.IsNullOrWhiteSpace(action.ActionName)) continue;
                    
                    // If monitoredActions is empty, check all actions. Otherwise, only check monitored ones.
                    if (monitoredActions.Count == 0 || monitoredActions.Contains(action.ActionName))
                    {
                        cachedActionNames.Add(action.ActionName);
                    }
                }
            }
        }

        private void ShowBurner(string actionName, string state)
        {
            // Check cooldown
            string key = $"{actionName}_{state}";
            if (lastBurnTime.TryGetValue(key, out float lastTime))
            {
                if (Time.time - lastTime < cooldownPerAction)
                {
                    return; // Still on cooldown
                }
            }

            lastBurnTime[key] = Time.time;

            // Format message
            string message = string.Format(messageFormat, actionName, state);

            // Show burner at "Input" spot
            try
            {
                SIGS.ShowBurner("Input", message);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[InputBurner] Failed to show burner: {e.Message}. Make sure a BurnerSpot named 'Input' exists in the scene.", this);
            }
        }
    }
}
