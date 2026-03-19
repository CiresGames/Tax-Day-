namespace AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils
{
    /// <summary>
    /// Contains path constants used throughout the InlineScript system.
    /// Paths are now read from Signalia config, with fallback to default values.
    /// </summary>
    public static class InlineScriptPaths
    {
        /// <summary>
        /// Default root path for user-generated script cache
        /// </summary>
        private const string DefaultRootPath_Cache = "Assets/AHAKuo Creations/InlineScript_Cache/";

        /// <summary>
        /// Root path for user-generated script cache (reads from Signalia config)
        /// </summary>
        public static string RootPath_Cache
        {
            get
            {
#if UNITY_EDITOR
                try
                {
                    var config = AHAKuo.Signalia.Framework.ConfigReader.GetConfig();
                    if (config != null && config.InlineScript != null && !string.IsNullOrEmpty(config.InlineScript.RootPath_Cache))
                    {
                        return config.InlineScript.RootPath_Cache;
                    }
                }
                catch
                {
                    // Fall through to default
                }
#endif
                return DefaultRootPath_Cache;
            }
        }
    }
}
