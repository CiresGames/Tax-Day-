using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities.SIGInput;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.LoadingScreens
{
    /// <summary>
    /// Allows Signalia input actions to progress a loading screen when "Click to Progress" is enabled.
    /// This mirrors clicking the "Click to Continue" prompt button by sending the configured ProgressionEvent.
    /// </summary>
    [AddComponentMenu("Signalia/Loading Screens/Signalia | Click To Continue Input")]
    public sealed class LoadingScreenClickToContinueInput : MonoBehaviour
    {
        [SerializeField] private bool active = true;

        public static LoadingScreenClickToContinueInput Instance { get; private set; }

        private static readonly string[] FallbackProgressActions = new[] { "Confirm", "Submit", "Interact" };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        public void SetActive(bool active) => this.active = active;

        private void Update()
        {
            if (!active) { return; }

            // Only relevant while a loading screen is active and waiting for progression.
            if (!LoadingScreen.OnALoadingScreen) { return; }
            if (!LoadingScreen.WaitingForClickToProgress) { return; }

            // Avoid spamming warnings when no wrapper exists.
            if (!SignaliaInputWrapper.Exists) { return; }

            var config = ConfigReader.GetConfig();
            if (config == null) { return; }
            if (!config.LoadingScreen.ClickToProgress) { return; }
            if (string.IsNullOrWhiteSpace(config.LoadingScreen.ProgressionEvent)) { return; }

            var actions = (config.LoadingScreen.ClickToProgressActionNames != null && config.LoadingScreen.ClickToProgressActionNames.Length > 0)
                ? config.LoadingScreen.ClickToProgressActionNames
                : FallbackProgressActions;

            for (int i = 0; i < actions.Length; i++)
            {
                string actionName = actions[i];
                if (string.IsNullOrWhiteSpace(actionName)) { continue; }

                if (SIGS.GetInputDown(actionName, oneFrame: true))
                {
                    SIGS.Send(config.LoadingScreen.ProgressionEvent);
                    return;
                }
            }
        }

        public static void AddMe()
        {
            var go = new GameObject("Signalia Loading ClickToContinue Input");
            Instance = go.AddComponent<LoadingScreenClickToContinueInput>();
        }
    }
}

