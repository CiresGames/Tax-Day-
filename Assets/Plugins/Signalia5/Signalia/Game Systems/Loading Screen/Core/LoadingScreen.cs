using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AHAKuo.Signalia.GameSystems.LoadingScreens
{
    /// <summary>
    /// This class holds reference to utilities that help with loading screens.
    /// </summary>
    public static class LoadingScreen
    {
        private static Dictionary<string, UIView> loadingScreenInstances = new();

        public static bool OnALoadingScreen => loadingScreenInstances.Any(x => x.Value.IsShown);

        /// <summary>
        /// True when a loading operation has reached the activation gate and is waiting for a "click to continue" progression trigger.
        /// </summary>
        public static bool WaitingForClickToProgress { get; private set; } = false;

        /// <summary>
        /// Prepare the config default loading screen and also any custom loading screens you want to use.
        /// </summary>
        /// <param name="loadingScreenPrefab"></param>
        public static void PrepareLoadingScreen(UIView loadingScreenPrefab = null)
        {
            var config = ConfigReader.GetConfig();
            if (loadingScreenPrefab != null)
            {
                if (!loadingScreenInstances.ContainsKey(loadingScreenPrefab.MenuName))
                {
                    var loadingScreen = GameObject.Instantiate(loadingScreenPrefab);
                    loadingScreenInstances.Add(loadingScreen.MenuName, loadingScreen);
                    GameObject.DontDestroyOnLoad(loadingScreen.gameObject);
                }
            }
            if (config.LoadingScreen.LoadingScreenPrefab != null)
            {
                if (!loadingScreenInstances.ContainsKey(config.LoadingScreen.LoadingScreenPrefab.MenuName))
                {
                    var loadingScreen = GameObject.Instantiate(config.LoadingScreen.LoadingScreenPrefab);
                    loadingScreenInstances.Add(loadingScreen.MenuName, loadingScreen);
                    GameObject.DontDestroyOnLoad(loadingScreen.gameObject);
                }
            }
        }

        /// <summary>
        /// This method loads a scene asynchronously and displays a loading screen. You can pass in a custom loading screen prefab, otherwise, it will use from the settings.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="loadingScreen"></param>
        public static void LoadSceneAsync(string sceneName, UIView loadingScreen = null)
        {
            var config = ConfigReader.GetConfig().LoadingScreen;
            var loadingScreenPrefab = loadingScreen ?? config.LoadingScreenPrefab;

            if (loadingScreenPrefab == null || loadingScreenPrefab.MenuName.IsNullOrEmpty())
            {
                Debug.LogWarning("Loading screen prefab is not set in the config or missing a MenuName.");
                return;
            }

            if (!loadingScreenInstances.TryGetValue(loadingScreenPrefab.MenuName, out var screen))
            {
                screen = GameObject.Instantiate(loadingScreenPrefab);
                SIGS.DoIn(0.1f, () => screen.Show());
                loadingScreenInstances.Add(screen.MenuName, screen);
                GameObject.DontDestroyOnLoad(screen.gameObject);
            }
            else
            {
                screen.Show();
            }

            void onShowEndHandler()
            {
                screen.OnShowEnd -= onShowEndHandler;

                var asyncOp = SceneManager.LoadSceneAsync(sceneName);
                asyncOp.allowSceneActivation = false;

                float delay = config.SimulateFakeLoading ? config.FakeLoadingTime : 0f;

                SIGS.DoIn(delay, () =>
                {
                    SIGS.DoWhen(() => asyncOp.progress >= 0.9f, () =>
                    {
                        SIGS.Send(config.EventOnLoadInitialComplete);

                        void proceedToActivate()
                        {
                            WaitingForClickToProgress = false;
                            asyncOp.allowSceneActivation = true;

                            SIGS.DoIn(0.25f, () =>
                            {
                                screen.Hide();
                                SIGS.Send(config.EventOnLoadFullyComplete);
                            });
                        }

                        if (config.ClickToProgress)
                        {
                            WaitingForClickToProgress = true;
                            SIGS.Listener(config.ProgressionEvent, proceedToActivate, oneShot: true);
                        }
                        else
                        {
                            WaitingForClickToProgress = false;
                            proceedToActivate();
                        }
                    });
                });
            }

            screen.OnShowEnd += onShowEndHandler;
        }

        public static void Clean()
        {
            WaitingForClickToProgress = false;
            loadingScreenInstances.Clear();
        }
    }
}
