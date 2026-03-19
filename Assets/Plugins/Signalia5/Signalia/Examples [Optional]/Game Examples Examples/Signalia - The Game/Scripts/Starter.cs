using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.Examples.SignaliaTheGame
{
    /// <summary>
    /// This class is used to start the game.
    /// </summary>
    public class Starter : MonoBehaviour
    {
        private void Awake()
        {
            Watchman.Watch();
        }

        /// <summary>
        /// This method is used to start the game.
        /// </summary>
        /// <remarks>
        /// This method is used to start the game. It is used to load the main menu scene.
        /// </remarks>
        private void Start()
        {
            // Load the main menu scene
            SIGS.Listener("MainMenu", LoadMainMenu, true);
            SIGS.Listener("Game1", LoadGame1, true);
            SIGS.Listener("Game2", LoadGame2, true);

            SIGS.DoIn(1, () => SIGS.UIViewControl("StarterMenu", true));
        }

        private void LoadMainMenu()
        {
            // Load the main menu scene
            SIGS.LoadSceneAsync("Signalia - Main Menu");
        }

        private void LoadGame1()
        {
            // Load the game scene
            SIGS.LoadSceneAsync("Signalia - Game 1");
        }

        private void LoadGame2()
        {
            // Load the game scene
            SIGS.LoadSceneAsync("Signalia - Game 2");
        }
    }
}