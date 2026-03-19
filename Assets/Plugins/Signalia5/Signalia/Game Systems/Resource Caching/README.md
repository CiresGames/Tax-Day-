# Signalia Resource Caching System

A powerful resource caching system for Unity that allows you to preload and cache resources for instant access during runtime, improving performance and providing better organization for your project assets.

## Features

- **String-based Resource Access**: Load resources using simple string keys instead of file paths
- **Preloaded Resources**: Resources are loaded at startup, eliminating runtime loading delays
- **Flexible Asset Types**: Support for prefabs, audio clips, ScriptableObjects, sprites, materials, and more
- **Drag & Drop Support**: Easily populate ResourceAssets by dragging assets directly into the inspector
- **Auto-population**: Automatically discover and assign ResourceAssets to the Signalia config
- **Performance Optimized**: Built-in dictionary for O(1) resource lookup
- **Signalia Integration**: Seamlessly integrates with the Signalia framework ecosystem
- **Visual Inspector**: Beautiful header graphics and intuitive drag & drop interface

## Quick Start

### 1. Create a Resource Asset

1. Right-click in your Project window
2. Navigate to `Create > Signalia > Game Systems > Resource Asset`
3. Name your asset (e.g., "GameResources")

### 2. Populate Your Resource Asset

**Method 1: Drag & Drop (Recommended)**
1. Select your ResourceAsset in the Project window
2. In the Inspector, you'll see a grey drag & drop area with "📁 Drag & Drop Assets Here"
3. Drag assets from your Project window directly into this area
4. Assets will be automatically added with generated keys based on their names
5. Keys are automatically made unique if duplicates exist

**Method 2: Manual Entry**
1. Click the "+" button to add new entries
2. Enter a key name (e.g., "player_prefab")
3. Assign the resource object
4. Repeat for all resources you want to cache

### 3. Configure in Signalia Settings

1. Open `Tools > Signalia > Settings`
2. Go to the "Game Systems" tab
3. Select "Resource Caching"
4. Click "Load Resource Assets" to automatically assign your ResourceAssets
5. Save your settings

### 4. Use in Code

```csharp
// Method 1: Using SIGS (Recommended)
GameObject playerPrefab = SIGS.GetResource<GameObject>("player_prefab");
AudioClip jumpSound = SIGS.GetResource<AudioClip>("jump_sound");
Sprite playerIcon = SIGS.GetResource<Sprite>("player_icon");

// Method 2: Using string extensions
GameObject enemyPrefab = "enemy_prefab".GetResource<GameObject>();
bool hasResource = "health_potion".HasResource();

// Method 3: Using GameObject extensions
ScriptableObject gameSettings = gameObject.LoadAsResource<ScriptableObject>("game_settings");

// Check if resources exist
if (SIGS.HasResource("special_item"))
{
    // Resource exists, safe to use
}

// Get all available resource keys
string[] allKeys = SIGS.GetAllResourceKeys();
Debug.Log($"Total cached resources: {SIGS.GetResourceCacheSize()}");
```

## API Reference

### SIGS Methods

#### `SIGS.GetResource<T>(string key)`
Retrieves a cached resource by its key.

**Parameters:**
- `T`: The type of resource to retrieve
- `key`: The string key identifying the resource

**Returns:** The cached resource or null if not found

**Example:**
```csharp
GameObject prefab = SIGS.GetResource<GameObject>("my_prefab");
```

#### `SIGS.HasResource(string key)`
Checks if a resource exists for the given key.

**Parameters:**
- `key`: The string key to check

**Returns:** True if the resource exists

**Example:**
```csharp
if (SIGS.HasResource("player_model"))
{
    // Resource is available
}
```

#### `SIGS.GetAllResourceKeys()`
Gets all available resource keys.

**Returns:** Array of all resource keys

**Example:**
```csharp
string[] keys = SIGS.GetAllResourceKeys();
foreach (string key in keys)
{
    Debug.Log($"Available resource: {key}");
}
```

#### `SIGS.GetResourceCacheSize()`
Gets the number of cached resources.

**Returns:** Number of cached resources

**Example:**
```csharp
int count = SIGS.GetResourceCacheSize();
Debug.Log($"Total resources cached: {count}");
```

### Extension Methods

#### `string.GetResource<T>(this string resourceKey)`
Extension method for string to load resources.

**Example:**
```csharp
Sprite icon = "player_icon".GetResource<Sprite>();
```

#### `string.HasResource(this string resourceKey)`
Extension method to check if a resource exists.

**Example:**
```csharp
bool exists = "special_item".HasResource();
```

#### `GameObject.LoadAsResource<T>(this GameObject gameObject, string resourceName)`
Extension method for GameObjects to load resources.

**Example:**
```csharp
ScriptableObject settings = gameObject.LoadAsResource<ScriptableObject>("game_settings");
```

## Resource Asset Inspector

The Resource Asset inspector provides several helpful features:

### Visual Design
- **Header Graphics**: Beautiful Signalia-style header image
- **Grey Color Scheme**: Professional grey drag & drop area for better visibility
- **Clear Visual Indicators**: Distinct borders and styling for the drop zone

### Drag & Drop Area
- **Visual Indicator**: Grey drop zone with "📁 Drag & Drop Assets Here" text
- **Automatic Key Generation**: Keys are generated from asset names (lowercase, underscores)
- **Duplicate Handling**: Automatically generates unique keys if duplicates exist
- **Batch Import**: Drop multiple assets at once
- **File Extension Detection**: Warns about .cs files that might be problematic

### Manual Entry
- **Add Button**: Click "+" to add new resource entries
- **Remove Button**: Click "-" to remove entries
- **Reorderable List**: Drag entries to reorder them
- **Validation**: Shows warnings for empty keys or missing references
- **Script Warnings**: Special warning for .cs files explaining potential runtime issues

### Auto-populate Feature
- **Auto-populate Button**: Automatically finds and adds resources from the Resources/Signalia folder
- **Type Filtering**: Includes all asset types (prefabs, audio, ScriptableObjects, sprites, etc.)
- **Smart Filtering**: Warns about script files while allowing all other types

## Best Practices

### Key Naming
- Use descriptive, lowercase names with underscores
- Examples: `player_prefab`, `jump_sound`, `health_potion_icon`
- Avoid spaces and special characters

### Resource Organization
- Group related resources in the same ResourceAsset
- Use multiple ResourceAssets for different categories (UI, Audio, Prefabs)
- Keep ResourceAssets in the `Resources/Signalia/` folder

### Performance Tips
- Preload all resources at startup to avoid runtime delays
- Use the Resource Caching system instead of `Resources.Load()` for better performance
- Monitor cache size with `SIGS.GetResourceCacheSize()`

### Asset Types
- **Recommended**: Prefabs, AudioClips, ScriptableObjects, Sprites, Materials
- **Use with caution**: Script files (.cs) - may cause issues at runtime
- **Supported**: Any UnityEngine.Object type

## Integration with Signalia

The Resource Caching system integrates seamlessly with the Signalia framework:

- **Automatic Initialization**: Loads during Watchman's awake call through GameSystemsHandler
- **Config Integration**: Managed through SignaliaConfigAsset with dedicated ResourceAssets array
- **Menu Integration**: Accessible via `Tools > Signalia > Game Systems > Load Resource Assets`
- **Settings Integration**: Configured in the Signalia Settings window under Game Systems tab
- **Package System**: Properly encapsulated with SIGS_RC regions and skeleton support

## Troubleshooting

### Common Issues

**Q: Resources not loading at runtime**
A: Ensure your ResourceAssets are assigned in the Signalia config and the Resource Caching system is initialized.

**Q: Drag & drop not working**
A: Make sure you're dragging assets into the grey drop zone in the ResourceAsset inspector.

**Q: Duplicate key warnings**
A: The system automatically handles duplicates by appending numbers. You can manually rename keys if needed.

**Q: Script file warnings**
A: Script files (.cs) may cause issues when loaded as resources. Consider using ScriptableObjects instead.

**Q: Package not compiling**
A: The system includes skeleton implementations for compilation without the package. Check that SIGS_RC is properly defined.

### Debug Information

Enable debugging in Signalia Settings to see detailed logs about resource loading and caching operations.

## Examples

See `ResourceCachingExample.cs` in the Examples folder for a complete working example demonstrating all features of the Resource Caching system.

## Recent Updates

- **Enhanced Visual Design**: Improved drag & drop area with grey color scheme for better visibility
- **Script File Detection**: Better detection of .cs files with file extension checking
- **Auto-population**: Automatic discovery and assignment of ResourceAssets to config
- **Menu Organization**: Moved "Load Resource Assets" to Game Systems > Load for better organization
- **Package Integration**: Full integration with Signalia's package system and skeleton support