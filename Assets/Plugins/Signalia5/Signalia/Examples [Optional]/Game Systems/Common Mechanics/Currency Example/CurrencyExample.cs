using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics.Examples
{
    /// <summary>
    /// Example script demonstrating how to use the Currency system.
    /// This shows the basic usage patterns for managing currencies in your game.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Signalia | Currency Example")]
    public class CurrencyExample : MonoBehaviour
    {
        [Header("Example Usage")]
        [SerializeField] private string currencyName = "gold";
        [SerializeField] private float testAmount = 100f;
        
        private Listener goldUpdateListener;

        private void OnDestroy()
        {
            // Clean up listener
            goldUpdateListener?.Dispose();
        }

        /// <summary>
        /// Callback for when currency is updated.
        /// </summary>
        /// <param name="newValue">The new currency value</param>
        private void OnGoldUpdated(object newValue)
        {
            if (newValue is float value)
            {
                Debug.Log($"{currencyName} updated! New value: {value}");
                
                // You can update UI, play sounds, trigger effects, etc.
                UpdateUI(value);
                PlayCurrencySound();
            }
        }

        /// <summary>
        /// Example UI update method.
        /// </summary>
        /// <param name="value">The currency value to display</param>
        private void UpdateUI(float value)
        {
            // This would typically update a TextMeshProUGUI component
            Debug.Log($"UI Updated: {currencyName} = {value}");
        }

        /// <summary>
        /// Example sound playing method.
        /// </summary>
        private void PlayCurrencySound()
        {
            // Play audio through Signalia's audio system
            SIGS.PlayAudio("coin_pickup");
        }

        // Public methods for testing in the custom inspector
        public void AddGold()
        {
            var gold = SIGS.GetCurrency(currencyName);
            gold.Modify(50f);
            Debug.Log($"Added 50 {currencyName}. Total: {gold.value}");
        }

        public void RemoveGold()
        {
            var gold = SIGS.GetCurrency(currencyName);
            gold.Modify(-25f);
            Debug.Log($"Removed 25 {currencyName}. Total: {gold.value}");
        }

        public void SetGoldTo1000()
        {
            var gold = SIGS.GetCurrency(currencyName);
            float difference = 1000f - gold.value;
            gold.Modify(difference);
            Debug.Log($"Set {currencyName} to 1000. Total: {gold.value}");
        }

        public void ShowCurrentGold()
        {
            var gold = SIGS.GetCurrency(currencyName);
            Debug.Log($"Current {currencyName}: {gold.value}");
        }

        public void AddCustomAmount()
        {
            var gold = SIGS.GetCurrency(currencyName);
            gold.Modify(testAmount);
            Debug.Log($"Added {testAmount} {currencyName}. Total: {gold.value}");
        }

        public void RemoveCustomAmount()
        {
            var gold = SIGS.GetCurrency(currencyName);
            gold.Modify(-testAmount);
            Debug.Log($"Removed {testAmount} {currencyName}. Total: {gold.value}");
        }

        public void AddAmount(int amn)
        {
            var gold = SIGS.GetCurrency(currencyName);
            gold.Modify(amn);
            Debug.Log($"Added {amn} {currencyName}. Total: {gold.value}");
        }
    }
}
