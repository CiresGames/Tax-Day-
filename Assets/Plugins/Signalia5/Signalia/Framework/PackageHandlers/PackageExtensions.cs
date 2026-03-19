using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.Framework.PackageHandlers
{
    /// <summary>
    /// Centralized extension methods that route package interactions through the framework accessor.
    /// </summary>
    public static class PackageExtensions
    {
        /// <summary>
        /// Retrieves an instance from the pooling system.
        /// </summary>
        public static GameObject FromPool(this GameObject sourcePrefab, float lifetime = -1f, bool enabled = true)
            => SIGS.PoolingGet(sourcePrefab, lifetime, enabled);

        /// <summary>
        /// Retrieves multiple instances from the pooling system.
        /// </summary>
        public static List<GameObject> FromPool(this GameObject sourcePrefab, int count, float lifetime = -1f, bool enabled = true)
            => SIGS.PoolingGet(sourcePrefab, count, lifetime, enabled);

        /// <summary>
        /// Warmups the pool with a predefined number of instances.
        /// </summary>
        public static void WarmupPool(this GameObject sourcePrefab, int count)
            => SIGS.PoolingWarmup(sourcePrefab, count);

        /// <summary>
        /// Determines whether the requested number of pooled instances are currently active in the scene.
        /// </summary>
        /// <param name="sourcePrefab">Prefab that owns the pool to check.</param>
        /// <param name="count">Minimum number of active instances to validate against.</param>
        public static bool PoolActiveCount(this GameObject sourcePrefab, int count = 1)
            => SIGS.PoolingActiveCount(sourcePrefab, count);

        /// <summary>
        /// Retrieves a pooled instance along with cached component references.
        /// </summary>
        public static (GameObject gameObject, Dictionary<Type, Component> compCache) FromPool(this GameObject sourcePrefab, float lifetime, bool enabled, params (Type type, bool fromChildren)[] types)
            => SIGS.PoolingGet(sourcePrefab, lifetime, enabled, types);

        /// <summary>
        /// Retrieves multiple pooled instances with cached component references.
        /// </summary>
        public static List<(GameObject gameObject, Dictionary<Type, Component> compCache)> FromPool(this GameObject sourcePrefab, int count, float lifetime, bool enabled, params (Type type, bool fromChildren)[] types)
            => SIGS.PoolingGet(sourcePrefab, count, lifetime, enabled, types);

        /// <summary>
        /// Retrieves a cached component instance from the pooling cache helper.
        /// </summary>
        public static T GetCached<T>(this Dictionary<Type, Component> cache) where T : Component
            => SIGS.PoolingTryGetCached<T>(cache);

        /// <summary>
        /// Loads a resource from the caching system using a key while preserving the GameObject extension surface.
        /// </summary>
        public static T LoadAsResource<T>(this GameObject gameObject, string resourceKey) where T : Object
            => SIGS.GetResource<T>(resourceKey);

        /// <summary>
        /// Loads a resource from the caching system using a key.
        /// </summary>
        public static T LoadAsResource<T>(this string resourceKey) where T : Object
            => SIGS.GetResource<T>(resourceKey);

        /// <summary>
        /// Checks if a cached resource exists for a key.
        /// </summary>
        public static bool HasResource(this string resourceKey)
            => SIGS.HasResource(resourceKey);

        /// <summary>
        /// Alias for <see cref="SIGS.GetResource{T}(string)"/> for fluent string usage.
        /// </summary>
        public static T GetResource<T>(this string resourceKey) where T : Object
            => SIGS.GetResource<T>(resourceKey);

        /// <summary>
        /// Gets a localized string by its key for the current language.
        /// This is equivalent to calling SIGS.GetLocalizedString(key).
        /// </summary>
        /// <param name="key">The localization key or source string (if hybrid key is enabled)</param>
        /// <returns>The localized string for the current language</returns>
        public static string Localized(this string key)
            => SIGS.GetLocalizedString(key);

        /// <summary>
        /// Gets a localized string by its key for a specific language.
        /// This method applies formatting (e.g., Arabic shaping) automatically.
        /// </summary>
        /// <param name="key">The localization key or source string (if hybrid key is enabled)</param>
        /// <param name="languageCode">The language code to retrieve (e.g., "en", "es", "fr")</param>
        /// <returns>The localized string for the specified language</returns>
        public static string Localized(this string key, string languageCode)
            => SIGS.GetLocalizedString(key, languageCode);

        /// <summary>
        /// Gets a raw localized string by its key for the current language without applying formatting.
        /// Use this when you need to apply string formatting (e.g., string.Format) before language-specific formatting.
        /// This is equivalent to calling SIGS.GetRawLocalizedString(key).
        /// </summary>
        /// <param name="key">The localization key or source string (if hybrid key is enabled)</param>
        /// <returns>The raw localized string without formatting, or the key itself if not found</returns>
        public static string LocalizedRaw(this string key)
            => SIGS.GetRawLocalizedString(key);

        /// <summary>
        /// Gets a raw localized string by its key for a specific language without applying formatting.
        /// Use this when you need to apply string formatting (e.g., string.Format) before language-specific formatting.
        /// </summary>
        /// <param name="key">The localization key or source string (if hybrid key is enabled)</param>
        /// <param name="languageCode">The language code to retrieve (e.g., "en", "es", "fr")</param>
        /// <returns>The raw localized string without formatting, or fallback value if not found</returns>
        public static string LocalizedRaw(this string key, string languageCode)
            => SIGS.GetRawLocalizedString(key, languageCode);
    }
}
