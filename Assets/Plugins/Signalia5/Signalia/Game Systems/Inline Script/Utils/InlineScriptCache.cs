using System.Collections.Generic;

namespace AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils
{
    /// <summary>
    /// Manages caching of compiled source code for InlineScript
    /// </summary>
    public static class InlineScriptCache
    {
        private static readonly Dictionary<string, string> LastCompiledSources = new Dictionary<string, string>();

        /// <summary>
        /// Gets the cached source for a property key
        /// </summary>
        public static bool TryGetCachedSource(string propertyKey, out string cachedSource)
        {
            return LastCompiledSources.TryGetValue(propertyKey, out cachedSource);
        }

        /// <summary>
        /// Updates the cached source for a property key
        /// </summary>
        public static void UpdateCachedSource(string propertyKey, string source)
        {
            if (!string.IsNullOrEmpty(source))
            {
                LastCompiledSources[propertyKey] = source;
            }
        }

        /// <summary>
        /// Clears all cached sources
        /// </summary>
        public static void ClearCache()
        {
            LastCompiledSources.Clear();
        }

        /// <summary>
        /// Gets the number of cached sources
        /// </summary>
        public static int CacheCount => LastCompiledSources.Count;
    }
}
