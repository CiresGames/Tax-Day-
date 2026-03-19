using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems
{
    public static class GameSystemsHandler
    {
        public static void InitializeGameSystems()
        {
            // prepare loading screen
            var config = ConfigReader.GetConfig();

            if (config.LoadingScreen.preloadLoadingScreen)
                SIGS.PrepareLoadingScreen();

            // initialize resource caching system
            SIGS.InitializeResourceCaching();

            // initialize save system preloaded files
            var files = ConfigReader.GetConfig().SavingSystem.CachedSaveFiles;
            foreach (var item in files)
            {
                SIGS.InitializeSaveCache(item);
            }

            SIGS.InitializeInventorySystem();

            // initialize localization system
            SIGS.InitializeLocalization();

            SIGS.InitializeDialogueSystem();
        }
        public static void CleanupGameSystems()
        {
            SIGS.CleanseAudioLayers();

            SIGS.ResetTutorials();

            SIGS.CleanLoadingScreens();

            SIGS.PoolingClear();

            SIGS.ClearResourceCache();

            SIGS.ClearSaveCaches();

            SIGS.ClearInventoryCache();

            SIGS.ClearDialogueSystem();

            AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.Clear();
        }

        /// <summary>
        /// Method called to shutdown game systems when the application is quitting.
        /// </summary>
        public static void ShutdownProcesses()
        {
            SIGS.ShutdownSaveSystem();
        }
    }
}
