using UnityEngine;
using System;
using DG.Tweening;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.GameSystems;
using SigTime = AHAKuo.Signalia.Utilities.SignaliaTime;
using AHAKuo.Signalia.Utilities.SIGInput;

using AHAKuo.Signalia.GameSystems.LoadingScreens;

namespace AHAKuo.Signalia.Framework
{
    /// <summary>
    /// Watches over runtime sticky "static" Signalia values and disposing them.
    /// </summary>
    public class Watchman : MonoBehaviour
    {
        /// <summary>
        /// Only called when the game is closed, as this object is not destroyed.
        /// </summary>
        public static event Action OnTermination;
        private static Watchman instance;
        public static Watchman Instance => instance;

        public static bool IsQuitting { get; private set; } = false;

        private void OnApplicationQuit()
        {
            IsQuitting = true;

            GameSystemsHandler.ShutdownProcesses();
        }

        /// <summary>
        /// No need to call this yourself manually, as it is called automatically when any signalia tool is used.
        /// </summary>
        public static void Watch()
        {
            if (!Application.isPlaying)
                return;

            // Reset IsQuitting if no instance exists (breaks catch-22 where IsQuitting prevents Watchman creation)
            if (instance == null)
                IsQuitting = false;

            if (IsQuitting)
                return;

            // spawn if none exists
            if (instance == null)
            {
                var go = new GameObject("Signalia Watchman");
                instance = go.AddComponent<Watchman>();
            }

            // add effector to scene if null
            if (ConfigReader.GetConfig().AutoAddEffector
                && Effector.Instance == null)
            {
                Effector.AddMe();
            }

            // add back button to scene if null
            if (ConfigReader.GetConfig().AutoAddBackButton
                && UIBackButton.Instance == null)
            {
                UIBackButton.AddMe();
            }

            if (ConfigReader.GetConfig().LoadingScreen != null
                && ConfigReader.GetConfig().LoadingScreen.ClickToProgress
                && LoadingScreenClickToContinueInput.Instance == null)
            {
                LoadingScreenClickToContinueInput.AddMe();
            }

            // add SignaliaTime to scene if null and enabled in config
            if (ConfigReader.GetConfig().SignaliaTime.AutoAddSignaliaTime
                && SigTime.Instance == null)
            {
                SigTime.AddMe();
            }

            // add InputMiceVisibilityController to scene if null and enabled in config
            if (ConfigReader.GetConfig().InputSystem.EnableSignaliaInputSystem
                && ConfigReader.GetConfig().InputSystem.CursorVisibility.AutoAddCursorController
                && InputMiceVisibilityController.Instance == null)
            {
                InputMiceVisibilityController.AddMe();
            }
        }

        private void Awake()
        {
            IsQuitting = false; // reset quitting state

            Application.quitting += () => IsQuitting = true; // set quitting state

            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (ConfigReader.GetConfig().KeepManagerAlive)
                DontDestroyOnLoad(gameObject);

            // call awake methods
            RuntimeValues.UIConfig.UseConfig();

            // subscribe to scene loaded event
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneLoaded;

            // set tween capacity to high
            DOTween.SetTweensCapacity(2000, 500);

            // initialize game systems
            GameSystemsHandler.InitializeGameSystems();

            // preload any configured audio so clips are ready when requested
            StartCoroutine(ResourceHandler.PreloadConfiguredAudioAssetsAsync());
        }

        private void Update()
        {   
            InvokeSignaliaUpdate();
        }

        /// <summary>
        /// Invokes the Signalia update method, which updates all updatable systems. Usually targets non-mono systems that need to be updated every frame.
        /// </summary>
        private void InvokeSignaliaUpdate()
        {   
            if (ConfigReader.GetConfig().InputSystem.EnableSignaliaInputSystem)
            {
                RuntimeValues.InputDelegation.Update();
                
                // Process input state modifiers (input blocking + cursor visibility)
                SignaliaInputBridge.ProcessModifiers();
            }
        }

        private void SceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            SIGS.Send("SceneLoaded");
        }

        /// <summary>
        /// Most likely only called when the game is closed, as this object is not destroyed. **if keep manager alive is set to true, otherwise, this is called when the scene is unloaded.**
        /// </summary>
        private void OnDestroy()
        {
            if (instance != this
                && instance != null) return; // not the instance

            ResetEverything(false); // reset everything
        }

        /// <summary>
        /// Resets all static values and destroys the watchman instance if specified.
        /// </summary>
        /// <param name="destroyWatchman"></param>
        public static void ResetEverything(bool destroyWatchman)
        {
            if (instance == null) return;

            OnTermination?.Invoke(); // invoke termination event

            // reset all static values
            RuntimeValues.ResetRuntimeValues();
            UIEventSystem.Reset();
            ComplexRadio.CleanUp();
            SimpleRadio.CleanUp();
            Bindables.ResetAll();
            ResourceHandler.Clean();
            GameSystemsHandler.CleanupGameSystems();
            SigTime.Reset(); // reset SignaliaTime system

            if (!destroyWatchman) return;

            // destroy me
            Destroy(instance.gameObject);
        }
    }
}
