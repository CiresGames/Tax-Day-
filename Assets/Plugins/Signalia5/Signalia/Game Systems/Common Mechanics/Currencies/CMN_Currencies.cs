using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics
{
    /// <summary>
    /// Simple system to track, sync, add, or remove custom currencies.
    /// Uses a runtime cache to avoid excessive I/O operations.
    /// Currency modifications happen in-cache, and saving is a separate operation.
    /// </summary>
    public class CMN_Currencies
    {
        /// <summary>
        /// Runtime cache for currency values. All currency operations work against this cache.
        /// Use SaveCurrency or SaveAllCurrencies to persist changes to disk.
        /// </summary>
        public static Dictionary<string, CustomCurrency> _runtimeValue = new();

        public static string gameDataFile => ConfigReader.GetConfig().CurrencySystem.SaveFileName;

        /// <summary>
        /// A custom currency definition.
        /// </summary>
        [Serializable]
        public struct CustomCurrency
        {
            public string name;
            public readonly string SaveKey => name.ToLower() + "_integral";
            public readonly string LocKey => name.ToLower() + "_loc";
            public float value;
            public readonly string UpdateListener => name + "sigs_u_pickedup"; // Used by SIGS.Listener(string, Update) to update whatever

            /// <summary>
            /// Creates a new CustomCurrency struct without loading from disk.
            /// For cached currency access, use CMN_Currencies.GetCurrency() instead.
            /// </summary>
            /// <param name="name">The currency name</param>
            /// <param name="value">The initial value (defaults to 0)</param>
            public CustomCurrency(string name, float value = default)
            {
                this.name = name;
                this.value = value;
                // Note: No longer auto-loads. Use CMN_Currencies.GetCurrency() for cached access.
            }

            /// <summary>
            /// Save this currency's current value to disk.
            /// </summary>
            public readonly void Save()
            {
                SIGS.SaveData(SaveKey, value, gameDataFile);
            }

            /// <summary>
            /// Load this currency's saved local value from disk. NOT CLOUD.
            /// Note: This only updates this struct instance. For proper cache integration,
            /// use CMN_Currencies.LoadCurrency() instead.
            /// </summary>
            public void Load()
            {
                this.value = SIGS.LoadData(SaveKey, gameDataFile, 0f);
            }

            /// <summary>
            /// Modify this currency's value by the specified amount.
            /// The change is applied to the runtime cache and optionally saved to disk.
            /// </summary>
            /// <param name="amount">Amount to add (use negative for subtraction)</param>
            /// <param name="autoSave">If true, saves to disk after modification</param>
            /// <param name="notify">If true, sends an event to listeners</param>
            public void Modify(float amount, bool autoSave = true, bool notify = true)
            {
                float newValue = this.value + amount;
                
                // Apply currency limits if configured
                newValue = CMN_Currencies.ClampToLimits(this.name, newValue);
                
                this.value = newValue;
                
                // Update the cache with the new value
                CMN_Currencies.UpdateCache(this);
                
                if (autoSave)
                    Save();
                if (notify)
                    UpdateListener.SendEvent(this.value);
            }
        }

        /// <summary>
        /// Updates the runtime cache with the given currency struct.
        /// </summary>
        /// <param name="currency">The currency to cache</param>
        internal static void UpdateCache(CustomCurrency currency)
        {
            _runtimeValue[currency.name] = currency;
        }

        /// <summary>
        /// Gets a currency by name from the runtime cache.
        /// If not cached, loads from disk and caches it.
        /// This is the recommended way to access currencies.
        /// </summary>
        /// <param name="name">The currency name</param>
        /// <returns>The cached currency struct</returns>
        public static CustomCurrency GetCurrency(string name)
        {
            if (_runtimeValue.TryGetValue(name, out CustomCurrency cached))
            {
                return cached;
            }
            
            // Not in cache, load from disk and cache it
            var currency = new CustomCurrency(name);
            currency.Load();
            _runtimeValue[name] = currency;
            return currency;
        }

        /// <summary>
        /// Modifies a currency's value in the runtime cache.
        /// This is a lightweight operation that doesn't touch disk.
        /// Use SaveCurrency() or enable autoSave to persist changes.
        /// </summary>
        /// <param name="name">The currency name</param>
        /// <param name="amount">Amount to add (use negative for subtraction)</param>
        /// <param name="autoSave">If true, saves to disk after modification</param>
        /// <param name="notify">If true, sends an event to listeners</param>
        /// <returns>The modified currency</returns>
        public static CustomCurrency ModifyCurrency(string name, float amount, bool autoSave = false, bool notify = true)
        {
            var currency = GetCurrency(name);
            currency.Modify(amount, autoSave, notify);
            return _runtimeValue[name]; // Return the cached value after modification
        }

        /// <summary>
        /// Saves a specific currency from the cache to disk.
        /// </summary>
        /// <param name="name">The currency name to save</param>
        public static void SaveCurrency(string name)
        {
            if (_runtimeValue.TryGetValue(name, out CustomCurrency currency))
            {
                currency.Save();
            }
        }

        /// <summary>
        /// Saves all cached currencies to disk.
        /// Call this at save points (e.g., checkpoints, level transitions, game pause).
        /// </summary>
        public static void SaveAllCurrencies()
        {
            foreach (var kvp in _runtimeValue)
            {
                kvp.Value.Save();
            }
        }

        /// <summary>
        /// Forces a reload of a specific currency from disk into the cache.
        /// </summary>
        /// <param name="name">The currency name to reload</param>
        /// <returns>The reloaded currency</returns>
        public static CustomCurrency LoadCurrency(string name)
        {
            var currency = new CustomCurrency(name);
            currency.Load();
            _runtimeValue[name] = currency;
            return currency;
        }

        /// <summary>
        /// Clears the runtime cache.
        /// Optionally saves all currencies before clearing.
        /// </summary>
        /// <param name="saveFirst">If true, saves all currencies to disk before clearing</param>
        public static void ClearCache(bool saveFirst = true)
        {
            if (saveFirst)
            {
                SaveAllCurrencies();
            }
            _runtimeValue.Clear();
        }

        /// <summary>
        /// Load a currency by name (legacy method).
        /// Note: This method now uses the cache. For explicit disk loading, use LoadCurrency().
        /// </summary>
        /// <param name="name">The currency name</param>
        /// <returns>The cached currency</returns>
        public static CustomCurrency LoadCurrencyType(string name)
        {
            return GetCurrency(name);
        }

        /// <summary>
        /// Get the currency limits for a specific currency from the config.
        /// </summary>
        /// <param name="currencyName">The name of the currency</param>
        /// <returns>The currency limit configuration, or null if not found</returns>
        public static CurrencyLimit GetCurrencyLimit(string currencyName)
        {
            var config = ConfigReader.GetConfig();
            if (config?.CurrencySystem?.CurrencyLimits == null)
                return null;

            return config.CurrencySystem.CurrencyLimits.FirstOrDefault(limit => 
                string.Equals(limit.CurrencyName, currencyName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validate if a currency value is within the configured limits.
        /// </summary>
        /// <param name="currencyName">The name of the currency</param>
        /// <param name="value">The value to validate</param>
        /// <returns>True if the value is within limits, false otherwise</returns>
        public static bool IsValueWithinLimits(string currencyName, float value)
        {
            var limit = GetCurrencyLimit(currencyName);
            if (limit == null)
                return true; // No limits defined, allow any value

            // Check minimum limit
            if (limit.MinLimitType == CurrencyLimitType.Custom && value < limit.CustomMinValue)
                return false;

            // Check maximum limit
            if (limit.MaxLimitType == CurrencyLimitType.Custom && value > limit.CustomMaxValue)
                return false;

            return true;
        }

        /// <summary>
        /// Clamp a currency value to the configured limits.
        /// </summary>
        /// <param name="currencyName">The name of the currency</param>
        /// <param name="value">The value to clamp</param>
        /// <returns>The clamped value</returns>
        public static float ClampToLimits(string currencyName, float value)
        {
            var limit = GetCurrencyLimit(currencyName);
            if (limit == null)
                return value; // No limits defined, return original value

            float minValue = limit.MinLimitType == CurrencyLimitType.Custom ? limit.CustomMinValue : float.MinValue;
            float maxValue = limit.MaxLimitType == CurrencyLimitType.Custom ? limit.CustomMaxValue : float.MaxValue;

            return Math.Max(minValue, Math.Min(maxValue, value));
        }
    }
}
