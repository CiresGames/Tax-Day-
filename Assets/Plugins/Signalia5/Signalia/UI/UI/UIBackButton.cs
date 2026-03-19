using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities.SIGInput;
using UnityEngine;
using System.Collections.Generic;

namespace AHAKuo.Signalia.UI
{
    /// <summary>
    /// A monobehaviour that constantly checks if the player used the back button.
    /// Uses Signalia's input bridge (SIGS.GetInput) and configurable action names from SignaliaConfigAsset.
    /// A scene only needs one back button to work properly.
    /// </summary>
    [AddComponentMenu("Signalia/UI/Signalia | Back Button")]
    public class UIBackButton : MonoBehaviour
    {
        [SerializeField] private bool active = true;

        public static UIBackButton Instance { get; private set; }

        // Used to edge-detect a press from SIGS.GetInput() (held state).
        private readonly Dictionary<string, bool> _previousHeld = new Dictionary<string, bool>();

        private string[] actions;

        private void Awake()
        {
            // init the instance
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        public void SetActive(bool active)
        {
            this.active = active;
        }

        private void Update()
        {
            if (!active) { return; }

            // Avoid spamming warnings when no wrapper exists.
            if (!SignaliaInputWrapper.Exists) { return; }
            
            // if a menu is currently being animated, don't check for back button presses
            var menuAnimating = RuntimeValues.TrackedValues.AViewIsAnimating;
            if (menuAnimating) { return; }

            var config = ConfigReader.GetConfig();
            actions ??= (config != null && config.BackButtonActionNames != null && config.BackButtonActionNames.Length > 0)
                ? config.BackButtonActionNames
                : new[] { "Back", "Cancel" };

            for (int i = 0; i < actions.Length; i++)
            {
                string actionName = actions[i];
                if (string.IsNullOrWhiteSpace(actionName)) { continue; }

                bool held = SIGS.GetInput(actionName);
                bool wasHeld = _previousHeld.TryGetValue(actionName, out bool prev) && prev;

                // Rising edge = "pressed" behavior like legacy GetButtonDown / GetKeyDown.
                if (held && !wasHeld)
                {
                    SIGS.Clickback();
                    _previousHeld[actionName] = true;
                    return;
                }

                _previousHeld[actionName] = held;
            }
        }

        public static void DisableBackButton()
        {
            Instance.SetActive(false);
        }

        public static void EnableBackButton()
        {
            Instance.SetActive(true);
        }

        public static void AddMe()
        {
            var go = new GameObject("Signalia Back Button");
            Instance = go.AddComponent<UIBackButton>();
        }
    }
}