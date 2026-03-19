# Currency System

A simple, struct-based currency management system for Signalia that provides efficient currency tracking, saving, and event integration.

> **Note**: This system is part of the Signalia Common Mechanics package and requires `SIGS_CMN` to be defined in your project settings.

## Features

- **Struct-Based Management**: Efficiently initializes and manages currency data using value types
- **Automatic Saving**: Integrates with Signalia's GameSaving system for persistent storage
- **Event Integration**: Triggers Signalia events when currency values change
- **UI Helper Component**: Automatically updates TextMeshProUGUI fields with currency values
- **Audio & Haptic Support**: Plays audio and haptic feedback when currencies change
- **SIGS Integration**: Access currencies through the main SIGS framework

## Requirements

- **Signalia Framework**: This system requires the Signalia framework to be installed
- **SIGS_CMN Package**: The `SIGS_CMN` define must be enabled in your project settings
- **GameSaving System**: Requires Signalia's GameSaving system for persistence
- **Radio System**: Requires Signalia's Radio system for event integration

## Quick Start

### Basic Usage

```csharp
// Get a currency and modify it
SIGS.GetCurrency("gold").Modify(100);

// Get current value
var gold = SIGS.GetCurrency("gold");
textField.SetText(gold.Value.ToString());
```

### Using the CurrencyHelper Component

1. Add the `CurrencyHelper` component to a GameObject with a TextMeshProUGUI
2. Set the currency name (e.g., "gold", "coins", "gems")
3. Configure audio and haptic feedback options
4. The component will automatically update the text when the currency changes

## API Reference

### CMN_Currencies Class

#### `CMN_Currencies.LoadCurrencyType(string name)`
Loads a currency by name, creating it if it doesn't exist.

**Parameters:**
- `name`: The name of the currency

**Returns:** `CustomCurrency` struct

### CustomCurrency Struct

#### Properties
- `name`: The currency name
- `value`: Current currency value
- `SaveKey`: Auto-generated save key for persistence
- `locKey`: Auto-generated localization key
- `updateListener`: Auto-generated event name for updates

#### Methods

##### `Modify(float amount, bool autoSave = true, bool notify = true)`
Modifies the currency value by the specified amount.

**Parameters:**
- `amount`: Amount to add/subtract
- `autoSave`: Whether to automatically save the change
- `notify`: Whether to trigger update events

##### `Save()`
Saves the current currency value to persistent storage.

##### `Load()`
Loads the currency value from persistent storage.

### SIGS Integration

#### `SIGS.GetCurrency(string currencyName)`
Gets a currency by name through the SIGS framework.

**Parameters:**
- `currencyName`: The name of the currency

**Returns:** `CustomCurrency` struct

### CurrencyHelper Component

#### Properties

**Currency Settings:**
- `currencyName`: Name of the currency to track
- `targetText`: TextMeshProUGUI component to update
- `updateOnStart`: Whether to update on component start
- `listenForUpdates`: Whether to listen for currency changes

**Display Settings:**
- `displayFormat`: Format string for display (e.g., "{0}", "Gold: {0}")
- `useCommaFormatting`: Add commas to numbers for better readability (e.g., 1,000 instead of 1000)
- `useLocalization`: Whether to use localization (Work in Progress)
- `localizationPrefix`: Prefix for localization keys

**Audio Settings:**
- `increaseAudioKey`: Audio key for increase events
- `decreaseAudioKey`: Audio key for decrease events

**Haptic Settings:**
- `playHapticsOnIncrease`: Play haptics when currency increases
- `playHapticsOnDecrease`: Play haptics when currency decreases
- `increaseHapticType`: Haptic type for increase events
- `decreaseHapticType`: Haptic type for decrease events

#### Methods

##### `Initialize()`
Manually initialize the currency helper.

##### `RefreshDisplay()`
Manually refresh the currency display.

##### `SetCurrencyName(string newCurrencyName)`
Change the currency name and refresh.

##### `GetCurrentValue()`
Get the current currency value.

##### `ModifyCurrency(float amount, bool autoSave = true, bool notify = true)`
Modify the currency value.

##### `SetCurrencyValue(float value, bool autoSave = true, bool notify = true)`
Set the currency value directly.

## Event System

The currency system automatically generates event names for each currency:

- Event Name Format: `{currencyName}sigs_u_pickedup`
- Example: For "gold" currency, the event name is "goldsigs_u_pickedup"

### Listening to Currency Updates

```csharp
public class CurrencyManager : MonoBehaviour
{
    private Listener goldUpdateListener;
    
    private void Start()
    {
        // Listen for gold currency updates
        string eventName = "gold" + "sigs_u_pickedup";
        goldUpdateListener = SIGS.Listener(eventName, OnGoldUpdated);
    }
    
    private void OnDestroy()
    {
        // Clean up listener
        goldUpdateListener?.Dispose();
    }
    
    private void OnGoldUpdated(object newValue)
    {
        if (newValue is float value)
        {
            Debug.Log($"Gold updated! New value: {value}");
            // Update UI, play sounds, etc.
        }
    }
}
```

## Examples

### Basic Currency Management

```csharp
// Add currency
SIGS.GetCurrency("gold").Modify(100);

// Remove currency
SIGS.GetCurrency("gold").Modify(-50);

// Get current value
float currentGold = SIGS.GetCurrency("gold").value;

// Set specific value
var gold = SIGS.GetCurrency("gold");
float difference = 1000f - gold.value;
gold.Modify(difference);
```

### Using CurrencyHelper

1. Add `CurrencyHelper` component to a GameObject
2. Assign a TextMeshProUGUI component
3. Set currency name to "gold"
4. Configure display format (e.g., "Gold: {0}" for "Gold: 1,000")
5. Enable comma formatting for better readability
6. Configure audio keys for increase/decrease
7. The text will automatically update when gold changes

### Event Integration

```csharp
public class CurrencyManager : MonoBehaviour
{
    private Listener goldUpdateListener;
    private Listener coinsUpdateListener;
    
    private void Start()
    {
        // Listen for multiple currencies
        goldUpdateListener = SIGS.Listener("goldsigs_u_pickedup", OnGoldChanged);
        coinsUpdateListener = SIGS.Listener("coinssigs_u_pickedup", OnCoinsChanged);
    }
    
    private void OnDestroy()
    {
        // Clean up listeners
        goldUpdateListener?.Dispose();
        coinsUpdateListener?.Dispose();
    }
    
    private void OnGoldChanged(object value)
    {
        if (value is float goldValue)
        {
            // Update gold UI
            goldText.text = $"Gold: {goldValue}";
            
            // Play achievement sound if reached milestone
            if (goldValue >= 1000)
            {
                SIGS.PlayAudio("achievement_unlocked");
            }
        }
    }
    
    private void OnCoinsChanged(object value)
    {
        if (value is float coinValue)
        {
            // Update coins UI
            coinsText.text = $"Coins: {coinValue}";
        }
    }
}
```

## Integration with Other Systems

### Audio System
The CurrencyHelper can play audio when currencies change:

```csharp
// Configure in inspector or code
currencyHelper.increaseAudioKey = "coin_pickup";
currencyHelper.decreaseAudioKey = "coin_spend";
```

### Haptic System
The CurrencyHelper can trigger haptic feedback:

```csharp
// Configure in inspector or code
currencyHelper.playHapticsOnIncrease = true;
currencyHelper.increaseHapticType = HapticType.Light;
```

### UI System
The CurrencyHelper automatically updates TextMeshProUGUI components:

```csharp
// The helper will automatically update the text
// when the currency value changes
```

## Best Practices

1. **Use Descriptive Names**: Use clear currency names like "gold", "coins", "gems"
2. **Consistent Naming**: Keep currency names consistent across your project
3. **Event Listening**: Always listen to currency update events for UI updates
4. **Audio Feedback**: Provide audio feedback for currency changes to improve player experience
5. **Save Management**: Use auto-save for important currencies, manual save for temporary ones

## Troubleshooting

### Currency Not Updating
- Check that the currency name matches exactly
- Ensure the CurrencyHelper is listening for updates
- Verify the TextMeshProUGUI component is assigned

### Events Not Firing
- Check the event name format: `{currencyName}sigs_u_pickedup`
- Ensure the `notify` parameter is true when modifying currency
- Verify SIGS.Listener is properly set up

### Save Issues
- Check that GameSaving system is properly configured
- Verify the save file name matches the expected format
- Ensure auto-save is enabled or manually call Save()
