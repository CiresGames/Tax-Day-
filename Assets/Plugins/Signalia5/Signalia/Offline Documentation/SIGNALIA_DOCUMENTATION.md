# Signalia Framework - Complete System Documentation

**Version:** 4.0.0  
**Author:** AHAKuo Creations  
**Framework Type:** Unity UI & Game Systems Framework

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Core Architecture](#2-core-architecture)
3. [UI System (COREA)](#3-ui-system-corea)
4. [Radio System (RESONANCE)](#4-radio-system-resonance)
5. [Game Systems](#5-game-systems)
6. [Utilities](#6-utilities)
7. [Integration Guide](#7-integration-guide)
8. [Best Practices](#8-best-practices)
9. [API Reference](#9-api-reference)

---

## 1. Introduction

### 1.1 What is Signalia?

Signalia is a comprehensive Unity framework that provides a unified, integrated ecosystem for building modern game UIs and managing complex game systems. Instead of isolated tools, Signalia offers a cohesive architecture where all components work together seamlessly.

### 1.2 Core Philosophy

- **Unified Ecosystem**: All features integrate through a central framework
- **Event-Driven Architecture**: Radio system enables decoupled communication
- **Static API Access**: SIGS (Signalia Global Shorthand) provides centralized access
- **Runtime Management**: Watchman handles lifecycle and cleanup
- **Modular Game Systems**: Optional systems that plug into the core framework

### 1.3 Key Features


**Core Framework:**
- Watchman singleton for runtime management
- SIGS static API for unified access
- Runtime Values for state management
- Config system for centralized settings

**UI System (COREA):**
- UIView - Screen/panel management with animations
- UIButton - Enhanced button with animations, toggles, and events
- UIElement - Named UI component registry
- UIAnimatable - Reusable animation components
- UIViewGroup - Grouped view management
- UIToggleGroup - Radio button-style toggles
- UIFill - Progress bars and fill animations

**Radio System (RESONANCE):**
- SimpleRadio - Event broadcasting and listening
- ComplexRadio - Audio channel management
- LiveKey/DeadKey - Value sharing and state management
- Listener - Event subscription system

**Game Systems** (Modular, optional):
- Localization System - Multi-language support
- Save System - Persistent data management
- Pooling System - Object pooling for performance
- Inventory System - Item management framework
- Audio Layering - Dynamic audio mixing
- Resource Caching - Asset pre-loading
- Loading Screen - Scene transition management
- Tutorial System (Deprecated) - Interactive tutorials
- Common Mechanics - Reusable game mechanics
- Inline Script - Runtime code execution

---

## 2. Core Architecture

### 2.1 The Watchman

The **Watchman** is Signalia's singleton manager that oversees the entire framework lifecycle.

**Key Responsibilities:**
- Framework initialization
- Runtime value management
- Scene transition handling
- Cleanup and disposal
- Game system coordination

**Lifecycle:**

```csharp
// Automatic initialization
void Awake()
{
    // Watchman is created automatically when any Signalia feature is used
    // Initializes: UI config, game systems, DOTween capacity
    // Subscribes to: scene loaded events
    // Sets up: DontDestroyOnLoad if configured
}

void Update()
{
    // Updates input delegation for AnyInputDown events
}

void OnApplicationQuit()
{
    // Shuts down game systems
    // Triggers cleanup processes
}

void OnDestroy()
{
    // Resets all static values
    // Cleans up UI event system
    // Disposes radio systems
    // Clears resource handlers
}
```

**Manual Control:**

```csharp
// Reset Signalia runtime (destroys Watchman)
SIGS.ResetSignaliaRuntime();

// Access Watchman instance
Watchman.Instance;

// Check if application is quitting
bool isQuitting = Watchman.IsQuitting;
```

**Auto-Setup Features:**
- Automatic Effector creation (if configured)
- Automatic BackButton creation (if configured)
- Audio asset preloading
- Scene management

### 2.2 Runtime Values

The **RuntimeValues** class manages all runtime state for Signalia.

**Structure:**

```csharp
RuntimeValues
├── Config           // Access to SignaliaConfigAsset
├── TrackedValues    // UI component tracking
├── UIConfig         // Button states and animation locks
├── RadioConfig      // Haptics and audio settings
├── InputDelegation  // Input event management
└── Debugging        // Debug logging configuration
```

**TrackedValues - Component Registry:**

```csharp
// Track active UI components
List<UIView> ViewRegistry        // All registered views
List<UIButton> ButtonRegistry    // All registered buttons
List<UIElement> ElementRegistry  // All registered elements
List<UIView> TravelHistory      // Navigation history for back button

// Track animations
bool AViewIsAnimating            // Any view currently animating
bool AnAnimatableIsAnimating     // Any animatable animating

// Current state
UIView CurrentFocusedView        // Last opened view
```

**UIConfig - Button Control:**

```csharp
// Global button state
bool ButtonsCanBeClicked  // Combines all button lock conditions

// Button locking
SIGS.DisableButtons();    // Disable all buttons
SIGS.EnableButtons();     // Enable all buttons

// Click-anywhere subscription
SIGS.OnClickAnywhere(() => Debug.Log("Clicked!"), oneShot: false);
```

**InputDelegation - Input Management:**

```csharp
// Subscribe to any input
SIGS.OnAnyInputDown(() => Debug.Log("Input detected!"));

// For custom input systems
SIGS.FireAnyInput();  // Manually trigger input event
```

### 2.3 Config System

The **SignaliaConfigAsset** centralizes all framework settings.

**Location:** `Resources/Signalia/SigConfig.asset`

**Key Settings:**

```csharp
// Framework Behavior
bool KeepManagerAlive              // DontDestroyOnLoad for Watchman
bool AutoAddEffector               // Auto-create Effector
bool AutoAddBackButton             // Auto-create back button
float DefaultButtonsCooldown       // Global button cooldown time

// UI Settings
bool PreventButtonsClickingWhenViewAnimating
bool PreventButtonsClickingWhenAnimatableAnimating
bool ConvertAllButtonsToUIButtons  // Auto-upgrade Unity buttons

// Radio System
bool EnableHaptics
bool AutoSaveHapticSetting
string HapticsSaveKey

// Game Systems
LocalizationSystemSettings LocalizationSystem
SavingSystemSettings SavingSystem
PoolingSystemSettings PoolingSystem
// ... other game system settings

// Debugging
bool EnableDebugging
bool UseIntrospection
bool LogListenerCreation
bool LogEventSend
// ... other debug flags
```

**Access:**

```csharp
var config = ConfigReader.GetConfig();

// In Editor: auto-creates if missing
// At Runtime: must exist or logs error
```

### 2.4 SIGS - Signalia Global Shorthand

**SIGS** is the central API gateway to all Signalia features. It provides a unified, static interface.

**Design Benefits:**
- Single point of access
- Consistent naming
- Reduced boilerplate
- Improved readability
- IntelliSense-friendly

**Organization:**

```csharp
SIGS
├── Framework Methods      // Runtime control
├── UI System Methods      // View/button control
├── Radio System Methods   // Events and audio
├── Utility Methods        // Timing, sequences
└── Game System Methods    // All game systems
```

**Common Usage:**

```csharp
// UI Control
SIGS.UIViewControl("MainMenu", true);
SIGS.DisableButtons();

// Events
SIGS.Send("GameStart");
SIGS.Listener("PlayerDied", OnPlayerDeath);

// Audio
SIGS.PlayAudio("bgm_main");

// Utilities
SIGS.DoIn(2f, () => Debug.Log("After 2 seconds"));
SIGS.DoWhen(() => playerHealth <= 0, OnPlayerDeath);

// Game Systems
SIGS.SaveData("playerName", "Hero", "saveFile");
string name = SIGS.LoadData<string>("playerName", "saveFile");
GameObject pooled = SIGS.PoolingGet(prefab, lifetime: 5f);
```

---

## 3. UI System (COREA)

The UI System provides a complete framework for building animated, event-driven user interfaces.

### 3.1 UIView

**UIView** represents a screen, panel, or any UI container that can be shown/hidden with animations.

**Core Features:**
- Show/hide with custom animations
- Automatic navigation history (for back button)
- First-selected element support
- Faint background overlay
- Parent view hiding
- Graphics raycaster control

**Inspector Settings:**

```csharp
// Behavior
UIViewStatus startingStatus          // Hidden/Shown
bool backButtonHides                  // Can back button hide this?
bool majorMenu                        // Is this a major navigation point?
string menuToBackTo                   // Manual back navigation target
bool hideAllOtherMenusOnShow         // Exclusive view
bool disableGameObject               // SetActive on hide
bool disableGraphicRaycaster         // Raycast control

// Selection
Selectable firstSelectedOnShow       // Auto-select on show
bool deselectAllOnHide              // Clear selection on hide

// Background
bool useFaintBackground              // Dark overlay
Color faintBackgroundColor           // Overlay color
bool faintBackgroundHideOnTap       // Tap overlay to hide

// Animations
UIAnimationAsset showAnimation       // Show animation
UIAnimationAsset hideAnimation       // Hide animation
bool playOnlyWhenChangingStatus     // Skip redundant animations

// Identification
string menuName                      // Unique identifier
```

**Code Usage:**

```csharp
// Show/Hide via SIGS
SIGS.UIViewControl("MainMenu", show: true);
SIGS.UIViewControl("Settings", show: false);

// Show as popup (auto-hides after time)
SIGS.ShowPopUp("Notification", time: 3f, unscaled: true);

// Direct component access
UIView view = GetComponent<UIView>();
view.Show();
view.Hide();
view.ShowAsPopUp(3f, unscaled: true);

// Check state
bool isVisible = SIGS.IsUIViewVisible("MainMenu");
bool isShowing = view.IsShowing;
bool isHidden = view.IsHidden;

// Get reference
UIView mainMenu = SIGS.GetView("MainMenu");

// Subscribe to events
view.SubscribeToHideByBackward(() => Debug.Log("Closed by back button"));

// Manual trigger
view.Clickback();  // Manually trigger back button behavior
```

**Animation System:**

UIView uses `UIAnimationAsset` for show/hide transitions. Each asset can contain:
- Position animation
- Scale animation
- Rotation animation
- Fade animation
- Delay and duration
- Easing curves
- Unscaled time option

**Navigation System:**

```csharp
// Travel history is automatically tracked
// Back button uses TravelHistory to navigate backwards

// Access navigation
List<UIView> history = RuntimeValues.TrackedValues.TravelHistory;
UIView currentView = RuntimeValues.TrackedValues.CurrentFocusedView;
```

### 3.2 UIButton

**UIButton** extends Unity's Button with animations, toggles, and integrated Signalia features.

**Core Features:**
- Click/hover/select animations
- Toggle functionality with save/load
- Menu control (show/hide views)
- Event integration (Radio system)
- Cooldown management
- Smart dimming (non-interactable state)

**Inspector Settings:**

```csharp
// Identification
string buttonName                    // Unique identifier

// Animations
UIAnimationAsset clickAnimation
UIAnimationAsset hoverAnimation
UIAnimationAsset unhoverAnimation
UIAnimationAsset selectAnimation
UIAnimationAsset deselectAnimation

// Behavior
bool disableWhileHovering           // Lock during hover
bool disableWhileAnimating          // Lock during animation
bool disableWhileSelecting          // Lock during selection
bool disableWithTime                // Cooldown
float disableTime                   // Cooldown duration
bool actionsAfterAnimation          // Execute after animation

// View Control
bool hideParentView                 // Hide parent on click
string[] menusToShow               // Views to show
string[] menusToShowAsPopUp        // Views as popups
string[] menusToHide               // Views to hide
float popUpHideDelay               // Popup duration

// Toggle System
bool useToggling                    // Enable toggle mode
string savingKey                    // Save key
bool autoSaveToggle                // Auto-save state
bool autoLoadToggle                // Auto-load state
string eventOnLoadToggle           // Event on load
string toggleEventSender           // Event on toggle
Image toggleCheckImage             // Toggle indicator
bool changeToggleSprite            // Sprite toggle
Sprite toggleOnSprite
Sprite toggleOffSprite
UnityEvent<bool> toggleEvent       // Unity event
UnityEvent toggleEventOn
UnityEvent toggleEventOff

// Binding
string interactivityBinding        // Bind to LiveKey for enabled state
```

**Code Usage:**

```csharp
// Get reference
UIButton button = SIGS.GetButton("PlayButton");

// Manual actions
button.SimulateClick();
button.PlayClickAnimation();
button.PlayHoverAnimation();

// Toggle control
button.SetToggleState(true);
bool isToggled = button.GetToggle();

// View control
button.SetHideParentView(true);
button.ShowMenu("Settings");
button.HideMenu("MainMenu");
button.ShowPopUp("Notification", 3f);

// State
bool isInteractable = button.IsInteractable;
```

**Toggle Groups:**

```csharp
// Create a toggle group (radio buttons)
UIToggleGroup group = GetComponent<UIToggleGroup>();

// Buttons auto-register if child of toggle group
// Only one button can be toggled at a time
```

**Smart Dimming:**

When a button becomes non-interactable:
- Button graphic dims
- All child graphics dim
- CanvasGroup alpha reduced
- Visual feedback is automatic

### 3.3 UIAnimatable

**UIAnimatable** provides reusable animation capabilities for any UI element.

**Features:**
- Multiple simultaneous animations
- Loop support
- Blocking (prevents clicks during animation)
- Trigger on enable
- Manual control

**Inspector Settings:**

```csharp
bool blocksClicks                   // Block UI during animation
bool playOnEnable                   // Auto-play when enabled
UIAnimationAsset[] animations       // Array of animations
```

**Code Usage:**

```csharp
UIAnimatable animatable = GetComponent<UIAnimatable>();

// Control
animatable.Play();
animatable.Stop();
animatable.ForceComplete();

// Check state
bool isPlaying = animatable.IsPerforming;
```

### 3.4 UIElement

**UIElement** provides a name-based registry for UI components.

**Purpose:**
- Identify UI objects by string names
- Tutorial system integration
- Runtime UI queries

**Usage:**

```csharp
// On component
[SerializeField] string elementName = "HealthBar";

// Get reference
UIElement healthBar = SIGS.GetElement("HealthBar");
GameObject obj = healthBar.gameObject;
```

### 3.5 UIFill

**UIFill** manages progress bars, health bars, and any fill-based UI.

**Features:**
- Smooth fill transitions
- Direction control
- Event integration
- Easy value binding

**Code Usage:**

```csharp
UIFill healthBar = GetComponent<UIFill>();

// Set value
healthBar.SetFill(0.75f);  // 75%

// Animate
healthBar.SetFill(0.5f, duration: 0.3f);

// Bind to value
SIGS.LiveKey("PlayerHealth", () => player.health / player.maxHealth);
healthBar.BindToLiveKey("PlayerHealth");
```

### 3.6 UIViewGroup

**UIViewGroup** manages collections of views with coordinated behavior.

**Usage:**

```csharp
// All child UIViews are part of the group
// Coordinated show/hide
// Group-level control
```

### 3.7 UIBackButton

**UIBackButton** provides automatic back navigation.

**Features:**
- Auto-created by Watchman
- Uses view travel history
- Respects view settings
- Keyboard support (Escape key)

**Code Usage:**

```csharp
SIGS.DisableBackButton();
SIGS.EnableBackButton();

// Manual trigger
UIBackButton.Instance.Clickback();
```

---

## 4. Radio System (RESONANCE)

The Radio System provides event broadcasting, audio management, and value sharing.

### 4.1 SimpleRadio - Event System

**SimpleRadio** enables decoupled communication through events.

**Key Features:**
- String-based event broadcasting
- Parameter passing
- One-shot listeners
- Context tracking

**Usage:**

```csharp
// Send events
SIGS.Send("GameStarted");
SIGS.Send("PlayerScored", 100, "Player1");

// Create listeners
SIGS.Listener("GameStarted", OnGameStart);
SIGS.Listener("GameStarted", OnGameStart, oneShot: true);  // Fires once

// With parameters
SIGS.Listener("PlayerScored", (args) => {
    int score = (int)args[0];
    string player = (string)args[1];
    Debug.Log($"{player} scored {score}!");
});

// With context (auto-cleanup on destroy)
SIGS.Listener("EnemySpawned", OnEnemySpawn, context: gameObject);

// Manual listener management
Listener listener = new Listener("PlayerDied", OnPlayerDeath);
listener.Dispose();  // Unsubscribe
```

**Listener Tracking:**

Signalia tracks all listeners for debugging:

```csharp
// View active listeners in System Vitals window
// Tools > Signalia > System Vitals
```

### 4.2 ComplexRadio - Audio System

**ComplexRadio** manages audio playback through channels.

**Key Concepts:**

- **AudioAsset**: ScriptableObject containing audio clips and settings
- **Resonance Channel**: Named audio channel for playback
- **Audio Mixing**: Integration with Unity's Audio Mixer

**Usage:**

```csharp
// Play audio
SIGS.PlayAudio("bgm_main");
SIGS.PlayAudioIfNotPlaying("bgm_main");

// With settings
SIGS.PlayAudio("sfx_explosion", 
    new VolumeModifier(0.8f),
    new PitchModifier(1.2f),
    new Looping(true)
);

// Stop audio
SIGS.StopAudio("bgm_main", fadeOut: true, fadeTime: 2f);

// Channel management
ResonanceChannel channel = SIGS.Channel("Music");
bool isLive = SIGS.IsChannelLive("Music");
```

**AudioAsset Creation:**

1. Create: `Create > Signalia > Radio > Audio Asset`
2. Assign audio clips
3. Configure: volume, pitch, mixer group, spatial settings
4. Add to Signalia config

**3D Audio:**

```csharp
// AudioAsset supports:
- Spatial blend (2D to 3D)
- Min/max distance
- Doppler level
- Spread
```

### 4.3 LiveKey & DeadKey - Value Sharing

**LiveKey** and **DeadKey** enable value sharing across systems without coupling.

**LiveKey - Dynamic Values:**

Values that change and always return current state.

```csharp
// Create LiveKey
SIGS.LiveKey("PlayerHealth", () => player.currentHealth);

// Multiple providers can exist
SIGS.LiveKey("Enemies", () => enemyManager.GetEnemies());

// Read values
int health = SIGS.GetLiveValue<int>("PlayerHealth");
List<Enemy> enemies = SIGS.GetLiveValues<Enemy>("Enemies");

// Check existence
bool exists = SIGS.LiveKeyExists("PlayerHealth");
```

**DeadKey - Static Values:**

Values captured at a point in time (instances).

```csharp
// Create DeadKey
SIGS.DeadKey("LastCheckpoint", checkpointPosition);
SIGS.DeadKey("BossInstance", bossEnemy);

// Read values
Vector3 checkpoint = SIGS.GetDeadValue<Vector3>("LastCheckpoint");

// Check existence
bool exists = SIGS.DeadKeyExists("LastCheckpoint");
```

**Use Cases:**

**LiveKey:**
- Player stats (health, mana, score)
- System states (is paused, enemy count)
- Dynamic queries (active quests, inventory items)

**DeadKey:**
- Last known positions
- Object references
- Cached calculations
- Temporary data

**Auto-Cleanup:**

Both LiveKey and DeadKey support context objects:

```csharp
// Cleanup when GameObject is destroyed
SIGS.LiveKey("EnemyHealth", () => health, context: gameObject);
SIGS.DeadKey("SpawnPoint", transform.position, context: gameObject);
```

---

## 5. Game Systems

Signalia's game systems are modular addons that integrate with the core framework.

### 5.1 Localization System

**Purpose:** Multi-language support for text, audio, sprites, and assets.

**Key Features:**
- LocBook-based workflow
- String extraction system (ILocbookExtraction interface)
- Paragraph styles for typography
- Hybrid key mode
- External tool integration (Lingramia)
- TextMeshPro integration
- Asset localization (audio, sprites, objects)

**Setup:**

1. Create LocBook: `Create > Signalia > Game Systems > Localization > LocBook`
2. Reference `.locbook` file
3. Add to Signalia config
4. Initialize: `SIGS.InitializeLocalization()`

**Text Localization:**

```csharp
// Get localized string
string text = SIGS.GetLocalizedString("menu_play");

// Change language
SIGS.ChangeLanguage("es", save: true);

// Get current language
string lang = SIGS.GetCurrentLanguage();

// Check if key exists
bool exists = SIGS.HasLocalizationKey("menu_play");

// Get available languages
List<string> languages = SIGS.GetAvailableLanguages();
```

**Component-Based:**

```csharp
// LocalizedText component
LocalizedText text = GetComponent<LocalizedText>();
text.localizationKey = "menu_play";
text.SetParagraphStyle("Header");  // Use string-based paragraph style
text.UpdateText();

// SimpleLocalizedText (lightweight)
SimpleLocalizedText simpleText = GetComponent<SimpleLocalizedText>();
simpleText.key = "description";
simpleText.SetParagraphStyle("Body");
```

**Extension Methods:**

```csharp
using AHAKuo.Signalia.GameSystems.Localization.Internal;

// Direct TMP_Text extension
tmpText.SetLocalizedText("menu_play");
tmpText.SetLocalizedText("menu_play", paragraphStyle: "Header");

// With formatting
tmpText.SetLocalizedTextFormat("score_display", null, "", 
    "Body", playerScore, playerName);
```

**Paragraph Styles:**

Paragraph styles allow different formatting for different text types. **Paragraph style is a string field** - you can use any custom string value:

```csharp
// Paragraph style is a string, not an enum
// Common examples:
"Header"       // Large headers
"Description"  // Descriptive text
"Body"         // Body text
"Caption"      // Small captions
"Subtitle"     // Subtitles
"Title"        // Title text

// You can also use custom values:
"MyCustomStyle"
"DialogText"
"MenuLabel"

// Leave empty string "" for default style
```

**How It Works:**

1. Create TextStyle assets with matching `paragraphStyle` string values
2. When you specify a paragraph style (e.g., `"Header"`), the system looks for a TextStyle asset with:
   - Matching language code
   - Matching paragraph style string
3. If no matching paragraph style is found, it falls back to a TextStyle with empty paragraph style for that language
4. If still not found, it uses any TextStyle with matching language code

**TextStyle Assets:**

Create different fonts/formatting per language and style:

```
Create > Signalia > Game Systems > Localization > Text Style
```

In each TextStyle asset:
- Set `Language Code` (e.g., "en", "es", "fr")
- Set `Paragraph Style` to a string value (e.g., "Header", "Body", "Caption", or any custom string)
- Leave `Paragraph Style` empty for the default style for that language

Example TextStyle assets:
- `EN_Header` - Language: "en", Paragraph Style: "Header"
- `EN_Body` - Language: "en", Paragraph Style: "Body"
- `EN_Default` - Language: "en", Paragraph Style: "" (empty = default)
- `ES_Header` - Language: "es", Paragraph Style: "Header"

**Asset Localization:**

```csharp
// Audio clips
AudioClip clip = SIGS.GetLocalizedAudioClip("voiceover_intro");
AudioClip clipES = SIGS.GetLocalizedAudioClip("voiceover_intro", "es");

// Sprites
Sprite sprite = SIGS.GetLocalizedSprite("ui_button_play");

// Generic assets
Texture texture = SIGS.GetLocalizedAsset<Texture>("texture_background");

// Check existence
bool hasAudio = SIGS.HasLocalizedAudioClip("voiceover_intro");
bool hasSprite = SIGS.HasLocalizedSprite("ui_button_play");
```

**String Extraction System:**

The Locbook Extraction system provides a way to extract hardcoded strings from your game and automatically generate LocBooks. This is essential for retrofitting existing projects with localization.

**How It Works:**

1. Implement `ILocbookExtraction` interface on your MonoBehaviours or ScriptableObjects
2. Define what text to extract in `GetExtractionData()`
3. Run the extractor tool: `Tools > Signalia > Game Systems > Localization > Extract Locbook`
4. The tool scans your project and generates a LocBook asset with a .locbook file
5. Update your code to use localization methods instead of direct text assignment
6. Add translations in Lingramia

**Implementing ILocbookExtraction:**

For a **ScriptableObject**:

```csharp
using AHAKuo.Signalia.GameSystems.Localization.External;

public class MyDialogue : ScriptableObject, ILocbookExtraction
{
    public string characterName = "Hero";
    public string dialogueText = "Hello, world!";
    
    public ExtractionData GetExtractionData()
    {
        var data = new ExtractionData();
        var page = new ExtractionPage
        {
            pageName = $"Dialogue: {name}",
            about = "Dialogue content",
            fields = new List<ExtractionPageField>()
        };
        
        page.fields.Add(new ExtractionPageField(characterName));
        page.fields.Add(new ExtractionPageField(dialogueText));
        
        data.pages.Add(page);
        return data;
    }
}
```

For a **MonoBehaviour**:

```csharp
using AHAKuo.Signalia.GameSystems.Localization.External;

public class UIController : MonoBehaviour, ILocbookExtraction
{
    public string titleText = "Main Menu";
    public List<string> buttonLabels = new List<string> { "Play", "Options", "Quit" };
    
    public ExtractionData GetExtractionData()
    {
        var data = new ExtractionData();
        var page = new ExtractionPage
        {
            pageName = $"UI: {gameObject.name}",
            about = "UI text elements",
            fields = new List<ExtractionPageField>()
        };
        
        page.fields.Add(new ExtractionPageField(titleText));
        
        foreach (var label in buttonLabels)
        {
            page.fields.Add(new ExtractionPageField(label));
        }
        
        data.pages.Add(page);
        return data;
    }
}
```

**Requirements:**

- **ScriptableObjects**: Must be placed in folders named `Resources` (e.g., `Assets/Resources/` or `Assets/MyGame/Resources/`)
- **MonoBehaviours**: Must be attached to GameObjects in **open scenes** (scenes must be open when running the extractor)

**Using the Extractor:**

1. Open: `Tools > Signalia > Game Systems > Localization > Extract Locbook`
2. Review the instructions in the window
3. Click **"Start Extraction"**
4. Choose where to save the LocBook asset
5. Review the extraction results

**After Extraction - Update Your Code:**

**Before:**
```csharp
tmpText.text = titleText;
```

**After (with Hybrid Key mode enabled):**
```csharp
tmpText.SetLocalizedText(titleText);
```

**After (with keys):**
```csharp
tmpText.SetLocalizedText("ui_mainmenu_title");
```

**Key Concepts:**

- **Pages**: Logical groupings of localization entries (e.g., one page per asset, or multiple pages for UI/dialogue/errors)
- **Fields**: Individual text entries that need localization
- **Keys**: Optional unique identifiers. Leave empty to use Hybrid Key mode (uses original text as key)

**Lingramia Integration:**

Lingramia is the external editor for LocBooks - a powerful standalone application designed specifically for editing localization data. It provides a user-friendly interface for managing translations, adding languages, and utilizing AI-powered translation features. Signalia includes an integrated auto-download feature to make setup seamless.

**What is Lingramia?**

Lingramia is a desktop application that allows you to:
- Edit localization entries with a clean, intuitive interface
- Add and manage multiple languages
- Use AI translation features to speed up localization
- Work with .locbook files exported from Signalia's LocBook system
- Import/export translations efficiently

**Installation Methods:**

Signalia provides two ways to install Lingramia:

**Method 1: Automatic Installation (Recommended)**

The easiest way to install Lingramia is through Unity's integrated downloader:

1. **From LocBook Inspector:**
   - Select any LocBook asset in your project
   - In the Inspector, find the "Lingramia Integration" section
   - Click the "⬇️ Download & Install Lingramia" button
   - The download window will open automatically

2. **From Unity Menu:**
   - Navigate to `Tools > Signalia > Game Systems > Localization > Download Lingramia`
   - A download window will appear with installation options

3. **Installation Process:**
   - The installer automatically fetches the latest release from GitHub (repository: `AHAKuo/Lingramia`)
   - Downloads the appropriate version for your platform (Windows, macOS, or Linux)
   - Extracts the application to the installation directory
   - Verifies the installation is complete

4. **Verification:**
   - Once installed, the LocBook Inspector will show "Lingramia is installed"
   - The installation path will be displayed in the download window
   - You can click "Open Installation Directory" to navigate to the folder

**Installation Locations:**

Lingramia is installed to platform-specific application data folders:

- **Windows:** `%LocalAppData%\AHAKuo Creations\Lingramia\`
  - Example: `C:\Users\YourName\AppData\Local\AHAKuo Creations\Lingramia\`
- **macOS:** `~/Library/Application Support/AHAKuo Creations/Lingramia/`
- **Linux:** `~/.local/share/AHAKuo Creations/Lingramia/`
- **Fallback:** If platform detection fails, uses Unity's temporary cache directory

**Method 2: Manual Installation**

If you prefer to download manually or the automatic installer encounters issues:

1. **Download from GitHub:**
   - Visit: `https://github.com/AHAKuo/Lingramia/releases`
   - Download the latest release ZIP file for your platform
   - Extract the ZIP file to the installation directory (see locations above)
   - Ensure `Lingramia.exe` (Windows) or `Lingramia.app` (macOS) is in the root of the installation folder

2. **Other Sources:**
   - Discord community channels
   - Unity Asset Store (if available)

**Troubleshooting Installation:**

If Lingramia fails to install or launch:

1. **Check Installation Status:**
   - Open `Tools > Signalia > Game Systems > Localization > Download Lingramia`
   - The window will show if Lingramia is detected

2. **Verify Installation Directory:**
   - Click "Open Installation Directory" to verify files are present
   - Ensure `Lingramia.exe` (or equivalent for your platform) exists

3. **Common Issues:**
   - **Network Issues:** Ensure you have internet access to download from GitHub
   - **Permissions:** Check that you have write permissions to the installation directory
   - **Antivirus:** Some antivirus software may block the download/extraction
   - **Corrupted Download:** Try downloading again or use manual installation

4. **Re-installation:**
   - Delete the installation directory manually
   - Run the downloader again to perform a fresh installation

**Using Lingramia:**

Once installed, you can use Lingramia to edit your LocBooks:

1. **Opening a LocBook:**
   - Select a LocBook asset in Unity
   - In the Inspector, click "🚀 Open Locbook Text" button
   - Lingramia will launch automatically with your .locbook file loaded

2. **Editing in Lingramia:**
   - Edit entries, add languages, and use AI translation features
   - Save your changes in Lingramia (Ctrl+S / Cmd+S)

3. **Importing Changes:**
   - Return to Unity
   - Click "🔄 Update Asset from .locbook File" in the LocBook Inspector
   - Your changes will be imported into the Unity asset

**Note:** LocBook text pages are read-only in Unity. All text editing must be done in Lingramia to maintain data integrity and enable advanced features like AI translation.

**Language Change Events:**

```csharp
// Listen for language changes
SIGS.Listener("LanguageChanged", OnLanguageChanged);

// Manual fire (after batch operations)
SIGS.FireLanguageChangeEvent();
```

### 5.2 Save System

**Purpose:** Persistent data storage with async support.

**Key Features:**
- Async/sync operations
- Auto-save support
- File-based organization
- Cache management
- Type-safe loading
- Preference system

**Initialize:**

```csharp
// Automatic initialization on first use
// Manual init:
SIGS.InitializeSaveCache("playerData");
```

**Saving:**

```csharp
// Synchronous save
SIGS.SaveData("playerName", "Hero", "playerData");
SIGS.SaveData("playerLevel", 5, "playerData");
SIGS.SaveData("playerPosition", transform.position, "playerData");

// Asynchronous save (batched)
await SIGS.SaveAsync("score", 1000, "gameData");

// Preferences (uses settings file)
SIGS.SavePreference("volume", 0.8f);
SIGS.SavePreference("quality", QualitySettings.GetQualityLevel());

// Force pending saves
await SIGS.ForceSaveAllAsync();
```

**Loading:**

```csharp
// With default value
string name = SIGS.LoadData("playerName", "playerData", "Unknown");
int level = SIGS.LoadData("playerLevel", "playerData", 1);
Vector3 pos = SIGS.LoadData("playerPosition", "playerData", Vector3.zero);

// Without default (returns default(T))
int score = SIGS.LoadData<int>("score", "gameData");

// Preferences
float volume = SIGS.LoadPreference("volume", 0.5f);
int quality = SIGS.LoadPreference("quality", 2);

// Load all data from file
Dictionary<string, string> allData = SIGS.LoadAllSaveData("playerData");

// String-specific load
string nameStr = SIGS.LoadString("playerName", "playerData");
```

**File Management:**

```csharp
// Check existence
bool fileExists = SIGS.SaveFileExists("playerData");
bool keyExists = SIGS.SaveKeyExists("playerName", "playerData");

// Delete
SIGS.DeleteSaveKey("oldKey", "playerData");
SIGS.DeleteSaveFile("oldSave");
SIGS.WipeAllSaveData();  // Nuclear option

// Check pending saves
bool hasPending = SIGS.HasPendingSaves("playerData");
bool anyPending = SIGS.HasAnyPendingSaves();
int pendingCount = SIGS.GetPendingSaveCount();
```

**Cache Management:**

```csharp
// Initialize cache for faster I/O
SIGS.InitializeSaveCache("playerData");

// Clear cache
SIGS.ClearSaveCache("playerData");
SIGS.ClearSaveCaches();  // All files

// Shutdown (saves pending data)
SIGS.ShutdownSaveSystem();
```

**Configuration:**

In SignaliaConfigAsset:
- `SettingsFileName` - Default preferences file
- `LogSaving` - Debug logging
- `AutoSaveInterval` - Async save frequency

**Best Practices:**

1. Use sync for critical data (player progress)
2. Use async for non-critical data (statistics)
3. Force save before quit/scene changes
4. Use preferences for settings
5. Organize data into logical files

### 5.3 Pooling System

**Purpose:** Object pooling for performance optimization.

**Key Features:**
- On-demand pooling
- Automatic lifetime management
- Component caching
- Warmup support
- Active count queries

**Basic Usage:**

```csharp
// Get pooled object
GameObject bullet = SIGS.PoolingGet(bulletPrefab);
bullet.transform.position = firePoint.position;

// With lifetime (auto-deactivate after time)
GameObject explosion = SIGS.PoolingGet(explosionPrefab, lifetime: 2f);

// Disabled initially
GameObject enemy = SIGS.PoolingGet(enemyPrefab, lifetime: -1f, enabled: false);
enemy.SetActive(true);  // Manual activation

// Get multiple
List<GameObject> coins = SIGS.PoolingGet(coinPrefab, count: 10, lifetime: 5f);
```

**Component Caching:**

Avoid `GetComponent` calls by caching:

```csharp
// Cache specific components
var (bullet, cache) = SIGS.PoolingGet(bulletPrefab, 5f, true,
    (typeof(Rigidbody), false),
    (typeof(TrailRenderer), true)  // from children
);

// Access cached components
Rigidbody rb = SIGS.PoolingTryGetCached<Rigidbody>(cache);
TrailRenderer trail = SIGS.PoolingTryGetCached<TrailRenderer>(cache);

// Use them
rb.AddForce(transform.forward * speed);
trail.enabled = true;
```

**Batch Operations:**

```csharp
// Get multiple with caching
var batch = SIGS.PoolingGet(enemyPrefab, count: 5, lifetime: 10f, enabled: true,
    (typeof(EnemyAI), false),
    (typeof(Animator), false)
);

foreach (var (obj, cache) in batch)
{
    var ai = SIGS.PoolingTryGetCached<EnemyAI>(cache);
    var anim = SIGS.PoolingTryGetCached<Animator>(cache);
    
    ai.Initialize();
    anim.SetTrigger("Spawn");
}
```

**Warmup:**

Pre-allocate pools at game start:

```csharp
void Start()
{
    // Create 20 inactive bullets
    SIGS.PoolingWarmup(bulletPrefab, 20);
    
    // Create 10 inactive enemies
    SIGS.PoolingWarmup(enemyPrefab, 10);
}
```

**Active Count Queries:**

```csharp
// Check if at least N instances are active
bool hasEnemies = SIGS.PoolingActiveCount(enemyPrefab, count: 5);

if (SIGS.PoolingActiveCount(bulletPrefab, 10))
{
    Debug.Log("10+ bullets active");
}
```

**Cleanup:**

```csharp
// Clear all pools (releases memory)
SIGS.PoolingClear();
```

**Integration with PoolingSpawner:**

```csharp
// Component for quick spawning
PoolingSpawner spawner = GetComponent<PoolingSpawner>();
spawner.spawnPrefab = enemyPrefab;
spawner.spawnCount = 5;
spawner.lifetime = 10f;
spawner.Spawn();  // Spawns at spawner position
```

### 5.4 Inventory System

**Purpose:** Item management framework with persistence and UI integration.

**Key Features:**
- Item definitions
- Grid-based display
- Drag-and-drop
- Persistence
- Stack management
- Quantity limits

**Core Classes:**

- **ItemSO**: ScriptableObject defining an item
- **ItemDefinition**: Runtime item instance (ItemSO + quantity)
- **InventoryDefinition**: Container of items
- **ItemSlot**: UI slot component
- **ItemGrid**: UI grid component
- **GameInventory**: Game-level inventory management

**ItemSO Creation:**

```csharp
// Create: Create > Signalia > Game Systems > Inventory > Item
[CreateAssetMenu]
public class ItemSO : ScriptableObject
{
    public string ItemName;
    public string ItemID;
    public Sprite ItemIcon;
    public int MaxQuantity;
    public bool IsStackable;
    // ... custom properties
}
```

**InventoryDefinition Usage:**

```csharp
// Create inventory
var inventory = InventoryDefinition.GetOrCreateInventory("PlayerBackpack", persistent: true);

// Add items
inventory.AddItem(swordItem, quantity: 1);
inventory.AddItem(potionItem, quantity: 5);

// Remove items
bool removed = inventory.RemoveItem(swordItem, quantity: 1);

// Query
bool hasItem = inventory.HasItem(swordItem);
int count = inventory.GetItemCount(swordItem);
ItemDefinition def = inventory.GetItemDefinition(swordItem);

// Clear
inventory.ClearAll();

// Listen for updates
SIGS.Listener(inventory.UpdateListenerString, OnInventoryChanged);
```

**Persistence:**

```csharp
// Persistent inventory (auto-saves)
var inventory = InventoryDefinition.GetOrCreateInventory("Player", persistent: true);

// Manual save/load
inventory.SaveToDisk();
inventory.LoadFromSave();
```

**UI Integration:**

```csharp
// GameItemSlot - represents one slot
GameItemSlot slot = GetComponent<GameItemSlot>();
slot.inventoryDefinition = inventory;
slot.slotIndex = 0;

// GameItemGrid - represents grid of slots
GameItemGrid grid = GetComponent<GameItemGrid>();
grid.inventoryDefinition = inventory;
grid.rows = 5;
grid.columns = 6;

// GameItemDisplayer - displays item details
GameItemDisplayer displayer = GetComponent<GameItemDisplayer>();
displayer.DisplayItem(itemDefinition);
```

**Initialization:**

```csharp
// Initialize at game start
SIGS.InitializeInventorySystem();

// Shutdown (saves all)
SIGS.ShutdownInventorySystem();

// Clear cache
SIGS.ClearInventoryCache();
```

### 5.5 Audio Layering

**Purpose:** Dynamic audio mixing with layered tracks.

**Key Concepts:**

- **Layer**: Collection of tracks (e.g., "Music", "Ambience")
- **Track**: Single audio channel with priority
- **Room Audio**: Location-based audio
- **Ambient Audio**: Background atmosphere

**Key Features:**
- Smooth crossfading
- Priority-based track selection
- Volume/pitch modulation
- Loop management
- Spatial audio support

**Setup:**

1. Create AudioLayeringLayerData asset
2. Define layers and tracks
3. Assign to Signalia config
4. Place Room/Ambient components in scenes

**Layer Usage:**

```csharp
// Get layer
Layer musicLayer = SIGS.AudioLayer("Music");

// Play track (fades in, fades out current)
musicLayer.PlayTrack("bgm_combat");
musicLayer.PlayTrack("bgm_exploration");

// Priority system
// Higher priority tracks automatically override lower priority

// Stop track
musicLayer.StopTrack("bgm_combat", fadeOut: true);

// Volume control
musicLayer.SetVolume(0.7f);
musicLayer.FadeVolume(0.3f, duration: 2f);
```

**Room Audio Component:**

```csharp
AudioLayeringRoom room = GetComponent<AudioLayeringRoom>();
room.layerID = "Music";
room.trackID = "bgm_dungeon";

// Plays when player enters trigger
// Fades when player exits
```

**Ambient Audio Component:**

```csharp
AudioLayeringAmbient ambient = GetComponent<AudioLayeringAmbient>();
ambient.layerID = "Ambience";
ambient.trackID = "amb_forest";

// Always playing in background
// Crossfades with other ambient tracks
```

**Cleanup:**

```csharp
SIGS.CleanseAudioLayers();
```

### 5.6 Resource Caching

**Purpose:** Pre-cache assets for instant access.

**Key Features:**
- String-based resource lookup
- Editor-time configuration
- Automatic caching
- Type-safe retrieval

**Setup:**

1. Create ResourceAsset: `Create > Signalia > Game Systems > Resource Caching > Resource Asset`
2. Add key-value pairs
3. Add to Signalia config

**Usage:**

```csharp
// Initialize (automatic on first use)
SIGS.InitializeResourceCaching();

// Get resource
Sprite icon = SIGS.GetResource<Sprite>("icon_health");
AudioClip sfx = SIGS.GetResource<AudioClip>("sfx_jump");
GameObject prefab = SIGS.GetResource<GameObject>("prefab_enemy");

// Check existence
bool exists = SIGS.HasResource("icon_health");

// Get all keys
string[] keys = SIGS.GetAllResourceKeys();

// Get cache size
int count = SIGS.GetResourceCacheSize();

// Clear
SIGS.ClearResourceCache();
```

**Use Cases:**
- UI icons
- Sound effects
- VFX prefabs
- Configuration data
- Localization assets

### 5.7 Loading Screen

**Purpose:** Scene transition management with loading UI.

**Key Features:**
- Async scene loading
- Progress tracking
- Custom loading screens
- Click-to-skip support

**Setup:**

1. Create loading screen UIView
2. Configure in Signalia settings
3. Optionally prepare: `SIGS.PrepareLoadingScreen(loadingPrefab)`

**Usage:**

```csharp
// Load scene
SIGS.LoadSceneAsync("GameLevel");

// Check if on loading screen
bool onLoading = SIGS.OnLoadingScreen;

// Cleanup
SIGS.CleanLoadingScreens();
```

**Custom Loading Screen:**

```csharp
// UIView with loading elements
// Automatically shown during load
// Progress bar updates automatically
```

### 5.8 Tutorial System (Deprecated)

**Purpose:** Deprecated. This system will be removed in a future release; avoid new integrations.

**Key Features:**
- Element highlighting
- Mask overlay
- Click detection
- Tutorial state management

**Usage:**

```csharp
// Highlight UI element
SIGS.HighlightElement("PlayButton", new ElementHighlightProperties
{
    blockClicks = true,
    showMask = true,
    maskColor = new Color(0, 0, 0, 0.8f),
    pulseEffect = true
});

// Reset tutorials
SIGS.ResetTutorials();
```

**Tutorial Management:**

```csharp
// Track tutorial completion
bool completed = SIGS.LoadPreference("tutorial_combat_completed", false);
if (!completed)
{
    StartCombatTutorial();
    SIGS.SavePreference("tutorial_combat_completed", true);
}
```

### 5.9 Common Mechanics

**Purpose:** Reusable gameplay mechanics.

**Includes:**

**Trigger Boxes:**

```csharp
Triggerbox trigger = GetComponent<Triggerbox>();

// Events
trigger.onEnter.AddListener(OnPlayerEnter);
trigger.onExit.AddListener(OnPlayerExit);
trigger.onStay.AddListener(OnPlayerStay);

// Filters
trigger.filterByTag = true;
trigger.requiredTag = "Player";
```

**Interactive Zones:**

```csharp
InteractiveZone zone = GetComponent<InteractiveZone>();

// Interaction
zone.onInteract.AddListener(OnInteract);

// Visual feedback
zone.showPrompt = true;
zone.interactPrompt = "Press E to interact";
```

**Currency System:**

```csharp
// Define currency in config
var gold = SIGS.GetCurrency("Gold");

// Operations
gold.Add(100);
gold.Subtract(50);
int amount = gold.Value();

// Persistence (automatic if configured)
```

### 5.10 Inline Script

**Purpose:** Runtime code execution and quick scripting.

**Key Features:**
- Compile and run C# at edit-time
- Quick prototyping
- Event-driven execution
- Auto-generated classes

**Usage:**

Place inline script components and write code directly in inspector.
Compiled automatically and executed on trigger.

---

## 6. Utilities

### 6.1 SSUtility - Sequencing & Timing

Signalia includes powerful timing utilities through SIGS:

**Delayed Execution:**

```csharp
// Execute after delay
SIGS.DoIn(2f, () => Debug.Log("2 seconds passed"));
SIGS.DoIn(1f, SpawnEnemy, unscaled: true);  // Ignores Time.timeScale

// Next frame
SIGS.DoNext(() => Debug.Log("Next frame"));

// After N frames
SIGS.DoAfterFrames(10, () => Debug.Log("After 10 frames"));
```

**Conditional Execution:**

```csharp
// Execute when condition true
SIGS.DoWhen(() => playerHealth <= 0, OnPlayerDeath);

// Continuous check (runs action based on condition)
SIGS.DoWhenever(
    () => enemyCount > 0,
    () => Debug.Log("Enemies alive"),
    () => Debug.Log("No enemies")  // Alternative when false
);

// Execute while condition true
SIGS.DoWhile(
    () => isCharging,
    ChargeAttack,
    waitTime: 0.1f,
    () => !isStunned  // Lock condition
);

// Execute until condition true
SIGS.DoUntil(() => gameOver, UpdateGame);
```

**Repeating Actions:**

```csharp
// Every interval
SIGS.DoEveryInterval(1f, SpawnEnemy);
SIGS.DoEveryInterval(0.5f, CheckInput, unscaled: false);

// For duration
SIGS.DoLoop(10f, 0.5f, DamageOverTime);  // 10 seconds, every 0.5s
SIGS.DoEveryIntervalFor(5f, 0.1f, UpdateShield);

// Every frame
SIGS.DoFrameUpdate(() => transform.Rotate(0, 1, 0));

// Random intervals
SIGS.DoRandomly(1f, 3f, SpawnPowerUp);  // Random between 1-3 seconds
```

**Retry Logic:**

```csharp
SIGS.DoRetries(
    () => TryConnect(),  // Returns bool
    maxRetries: 5,
    delayBetweenAttempts: 1f,
    onSuccess: () => Debug.Log("Connected!"),
    onFailure: () => Debug.Log("Failed to connect")
);
```

**Cooldowns:**

```csharp
// Check cooldown
if (SIGS.IsOnCooldown(5f, "PlayerAttack"))
{
    Debug.Log("Attack on cooldown");
    return;
}

// Cooldown gate (checks and sets)
if (SIGS.CooldownGate("PlayerAttack", 2f))
{
    // Attack allowed
    Attack();
}

// Action with cooldown
SIGS.DoActionWithCooldown(Attack, 2f, "PlayerAttack");

// Kill cooldown
SIGS.KillCooldownGate("PlayerAttack");
```

**Probability:**

```csharp
// Random chance (0-1)
if (SIGS.ThrowDice(0.25f))  // 25% chance
{
    DropRareItem();
}
```

**Tween Control:**

All timing methods return `Tween` objects:

```csharp
Tween tween = SIGS.DoIn(5f, ExplodeBomb);

// Control
tween.Kill();
tween.Pause();
tween.Play();
tween.Complete();
```

### 6.2 Promise System

Custom promise-based execution flow for sequential execution:

**PromiseFlow - Manual Steps:**

```csharp
var promise = SIGS.BeginPromiseFlow();
promise
    .NQ(finished => {
        Debug.Log("Step 1");
        finished();
    })
    .NQ(finished => {
        Debug.Log("Step 2");
        finished();
    })
    .NQNow(() => Debug.Log("Instant step"))
    .NQWait(1f)  // Wait 1 second
    .NQListen("EventName");  // Wait for event

// Steps execute sequentially as each step calls finished()
// You can also step around manually:
promise.StepPrevious();
promise.StepNext();
promise.StepTo(0);
```

**TimePromise - Timed Steps:**

```csharp
var promise = SIGS.BeginTimePromise(1f);  // 1 second between steps
promise
    .NQ(() => Debug.Log("Step 1"))
    .NQ(() => Debug.Log("Step 2"))
    .NQ(() => Debug.Log("Step 3"));

// Auto-executes every second
```

### 6.3 Extensions

Signalia adds many extension methods:

**String Extensions:**

```csharp
bool hasValue = str.HasValue();  // !string.IsNullOrEmpty
```

**Transform Extensions:**

```csharp
transform.DestroyAllChildren();
transform.SetX(10f);
transform.SetY(20f);
transform.SetZ(30f);
```

**Component Extensions:**

```csharp
T component = gameObject.GetOrAddComponent<T>();
```

---

## 7. Integration Guide

### 7.1 Project Setup

**Initial Setup:**

1. Import Signalia package
2. Config auto-creates at `Resources/Signalia/SigConfig.asset`
3. Configure settings in config inspector
4. Done! Framework is ready

**Recommended Settings:**

```csharp
// SignaliaConfigAsset recommended settings
KeepManagerAlive = true
AutoAddEffector = true
AutoAddBackButton = true
DefaultButtonsCooldown = 0.1f
PreventButtonsClickingWhenViewAnimating = true
EnableDebugging = false (production) / true (development)
```

### 7.2 Building a UI Flow

**Step-by-Step Example:**

**1. Create Views:**

```
MainMenu (UIView)
├── PlayButton (UIButton)
├── SettingsButton (UIButton)
└── QuitButton (UIButton)

Settings (UIView)
├── VolumeSlider
├── QualityDropdown
└── BackButton (UIButton)
```

**2. Configure Views:**

```csharp
// MainMenu UIView
menuName = "MainMenu"
startingStatus = Shown
majorMenu = true
hideAllOtherMenusOnShow = true

// Settings UIView
menuName = "Settings"
startingStatus = Hidden
backButtonHides = true
menuToBackTo = "MainMenu"
```

**3. Configure Buttons:**

```csharp
// PlayButton
buttonName = "PlayButton"
menusToHide = ["MainMenu"]
clickAnimation = ClickAnimation (UIAnimationAsset)

// SettingsButton
buttonName = "SettingsButton"
menusToShow = ["Settings"]
menusToHide = ["MainMenu"]
clickAnimation = ClickAnimation

// BackButton
buttonName = "BackButton"
hideParentView = true
clickAnimation = ClickAnimation
```

**4. Code Integration:**

```csharp
public class GameManager : MonoBehaviour
{
    void Start()
    {
        // Initialize systems
        SIGS.InitializeLocalization();
        
        // Setup listeners
        SIGS.Listener("GameStarted", OnGameStart);
        
        // Show main menu
        SIGS.UIViewControl("MainMenu", true);
    }
    
    void OnGameStart()
    {
        // Load game scene
        SIGS.LoadSceneAsync("GameLevel");
    }
}
```

### 7.3 Implementing Game Systems

**Example: Player Profile with Save System:**

```csharp
public class PlayerProfile
{
    private const string SAVE_FILE = "player_profile";
    
    public string playerName;
    public int level;
    public int experience;
    public Vector3 lastPosition;
    
    public void Save()
    {
        SIGS.SaveData("playerName", playerName, SAVE_FILE);
        SIGS.SaveData("level", level, SAVE_FILE);
        SIGS.SaveData("experience", experience, SAVE_FILE);
        SIGS.SaveData("position", lastPosition, SAVE_FILE);
    }
    
    public void Load()
    {
        playerName = SIGS.LoadData("playerName", SAVE_FILE, "NewPlayer");
        level = SIGS.LoadData("level", SAVE_FILE, 1);
        experience = SIGS.LoadData("experience", SAVE_FILE, 0);
        lastPosition = SIGS.LoadData("position", SAVE_FILE, Vector3.zero);
    }
}
```

**Example: Health System with Events:**

```csharp
public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    
    void Start()
    {
        currentHealth = maxHealth;
        
        // Provide health to other systems
        SIGS.LiveKey("PlayerHealth", () => currentHealth, context: gameObject);
        SIGS.LiveKey("PlayerMaxHealth", () => maxHealth, context: gameObject);
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        SIGS.Send("PlayerDamaged", currentHealth, damage);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        SIGS.Send("PlayerDied");
    }
}

// UI can bind to LiveKey
// healthBar.BindToLiveKey("PlayerHealth");
```

**Example: Enemy Spawner with Pooling:**

```csharp
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public float spawnInterval = 3f;
    
    void Start()
    {
        // Warmup pool
        SIGS.PoolingWarmup(enemyPrefab, 10);
        
        // Start spawning
        SIGS.DoEveryInterval(spawnInterval, SpawnEnemy);
        
        // Listen for game over
        SIGS.Listener("GameOver", StopSpawning);
    }
    
    void SpawnEnemy()
    {
        var (enemy, cache) = SIGS.PoolingGet(enemyPrefab, lifetime: 30f, enabled: true,
            (typeof(EnemyAI), false),
            (typeof(NavMeshAgent), false)
        );
        
        // Setup
        enemy.transform.position = GetRandomSpawnPoint();
        
        var ai = SIGS.PoolingTryGetCached<EnemyAI>(cache);
        ai?.Initialize();
    }
    
    void StopSpawning()
    {
        // Timer automatically stops (no reference needed)
        enabled = false;
    }
}
```

### 7.4 Localization Workflow

**1. Setup:**

```csharp
// Create LocBook asset
// Add languages: en, es, fr, etc.
// Create TextStyle assets for each language
```

**2. Create Entries:**

Use Lingramia to create and edit localization entries. 

**Quick Setup:**
Download Lingramia directly from Unity: `Tools > Signalia > Game Systems > Localization > Download Lingramia`

- Click "🚀 Open Locbook Text" button on LocBook inspector
- When prompted, choose Lingramia to open the .locbook file
- Add entries with keys and translations in Lingramia
- Save in Lingramia
- Click "🔄 Update Asset from .locbook File" in Unity to import changes

**3. Apply to UI:**

```csharp
// Option A: Component-based
LocalizedText text = GetComponent<LocalizedText>();
text.localizationKey = "ui_play_button";

// Option B: Code-based
tmpText.SetLocalizedText("ui_play_button");

// Option C: Direct
string text = SIGS.GetLocalizedString("ui_play_button");
```

**4. Language Switching:**

```csharp
// Dropdown for language selection
public void OnLanguageChanged(int index)
{
    string[] languages = { "en", "es", "fr" };
    SIGS.ChangeLanguage(languages[index], save: true);
}
```

### 7.5 Audio Integration

**Setup Audio Assets:**

```csharp
// Create AudioAsset for each sound
// Assign to Signalia config
```

**Play Sounds:**

```csharp
public class PlayerController : MonoBehaviour
{
    void Jump()
    {
        // Play jump sound
        SIGS.PlayAudio("sfx_jump");
        
        // Apply force
        rb.AddForce(Vector3.up * jumpForce);
    }
    
    void Shoot()
    {
        // Play with modifiers
        SIGS.PlayAudio("sfx_shoot",
            new VolumeModifier(0.8f),
            new PitchModifier(Random.Range(0.9f, 1.1f))
        );
        
        // Spawn bullet
        var bullet = SIGS.PoolingGet(bulletPrefab, 3f);
    }
}
```

**Background Music:**

```csharp
public class MusicManager : MonoBehaviour
{
    void Start()
    {
        SIGS.PlayAudio("bgm_menu");
        
        SIGS.Listener("GameStarted", () => {
            SIGS.StopAudio("bgm_menu", fadeOut: true, fadeTime: 1f);
            SIGS.DoIn(1f, () => SIGS.PlayAudio("bgm_game"));
        });
    }
}
```

---

## 8. Best Practices

### 8.1 Event System Best Practices

**DO:**

✅ Use meaningful event names:

```csharp
// Good
SIGS.Send("PlayerLeveledUp");
SIGS.Send("EnemyDefeated");
SIGS.Send("QuestCompleted");

// Bad
SIGS.Send("Event1");
SIGS.Send("Update");
```

✅ Use context for auto-cleanup:

```csharp
// Automatically disposed when gameObject is destroyed
SIGS.Listener("GameOver", OnGameOver, context: gameObject);
```

✅ Use one-shot for temporary listeners:

```csharp
SIGS.Listener("FirstTimeSetup", Initialize, oneShot: true);
```

✅ Document event parameters:

```csharp
// PlayerDamaged event: (float currentHealth, float damageAmount)
SIGS.Send("PlayerDamaged", health, damage);

SIGS.Listener("PlayerDamaged", (args) => {
    float health = (float)args[0];
    float damage = (float)args[1];
});
```

**DON'T:**

❌ Create circular dependencies:

```csharp
// Bad: A listens to B, B listens to A
```

❌ Overuse events for direct communication:

```csharp
// Bad
SIGS.Send("GetPlayerPosition");  // Request
SIGS.Send("PlayerPosition", pos);  // Response

// Good
Vector3 pos = SIGS.GetLiveValue<Vector3>("PlayerPosition");
```

### 8.2 UI Best Practices

**DO:**

✅ Use UIView for all major screens:

```csharp
MainMenu (UIView)
Settings (UIView)
Inventory (UIView)
Pause (UIView)
```

✅ Name everything:

```csharp
menuName = "MainMenu"
buttonName = "PlayButton"
elementName = "HealthBar"
```

✅ Use animations for polish:

```csharp
showAnimation = FadeInSlideUp
hideAnimation = FadeOutSlideDown
clickAnimation = ScaleBounce
```

✅ Configure back button behavior:

```csharp
backButtonHides = true
menuToBackTo = "MainMenu"
```

**DON'T:**

❌ Mix direct and event-based view control:

```csharp
// Bad
uiView.Show();
SIGS.UIViewControl("Settings", true);

// Good - pick one approach
SIGS.UIViewControl("MainMenu", true);
SIGS.UIViewControl("Settings", true);
```

❌ Forget to set majorMenu on main navigation points:

```csharp
// Main screens should be major menus
majorMenu = true
```

### 8.3 Performance Best Practices

**DO:**

✅ Use pooling for frequently spawned objects:

```csharp
bullets, enemies, particles, VFX
```

✅ Warmup pools at start:

```csharp
void Start()
{
    SIGS.PoolingWarmup(bulletPrefab, 50);
    SIGS.PoolingWarmup(enemyPrefab, 20);
}
```

✅ Cache components with pooling:

```csharp
var (obj, cache) = SIGS.PoolingGet(prefab, 5f, true,
    (typeof(Rigidbody), false),
    (typeof(Renderer), false)
);
```

✅ Use async saves for non-critical data:

```csharp
await SIGS.SaveAsync("statistics", stats, "gameData");
```

✅ Use LiveKey for frequently accessed values:

```csharp
SIGS.LiveKey("PlayerHealth", () => currentHealth);
// Instead of constant event sends
```

**DON'T:**

❌ Create/destroy frequently:

```csharp
// Bad
Instantiate(bulletPrefab);
Destroy(bullet, 3f);

// Good
SIGS.PoolingGet(bulletPrefab, lifetime: 3f);
```

❌ Synchronous save in Update/FixedUpdate:

```csharp
// Bad
void Update()
{
    SIGS.SaveData("position", transform.position, "player");
}

// Good
void OnApplicationQuit()
{
    SIGS.SaveData("position", transform.position, "player");
}
```

### 8.4 Organization Best Practices

**Project Structure:**

```
Assets/
├── Game/
│   ├── Scripts/
│   │   ├── Managers/
│   │   ├── Player/
│   │   ├── Enemies/
│   │   └── UI/
│   ├── Prefabs/
│   └── Scenes/
├── Resources/
│   └── Signalia/
│       ├── SigConfig.asset
│       ├── AudioAssets/
│       ├── ResourceAssets/
│       └── Localization/
└── Art/
    ├── UI/
    ├── Sprites/
    └── Audio/
```

**Naming Conventions:**

```csharp
// Audio Assets
bgm_menu, bgm_game, bgm_boss
sfx_jump, sfx_shoot, sfx_explosion

// Localization Keys
ui_play_button, ui_settings_title
game_objective_text, game_victory_message

// Save Keys
player_name, player_level, player_position
settings_volume, settings_quality

// Event Names
PlayerDied, EnemySpawned, QuestCompleted
GameStarted, GamePaused, GameResumed
```

### 8.5 Debugging Best Practices

**Enable Debug Logging:**

```csharp
// In SignaliaConfigAsset
EnableDebugging = true
LogEventSend = true
LogListenerCreation = true
```

**Use System Vitals:**

```
Tools > Signalia > System Vitals
```

View:
- Active listeners
- Live/Dead keys
- UI component registry
- Memory usage

**Console Logging:**

```csharp
// Check listener creation
[Simple Radio] Listener Initialization: [PlayerDied]

// Check event sends
[Simple Radio] Event Sent: [GameStarted] | Args: 

// Check audio playback
[Complex Radio] Channel Send: [Music]
```

---

## 9. API Reference

### 9.1 SIGS API Quick Reference

**Framework:**

```csharp
SIGS.ResetSignaliaRuntime()
SIGS.OnAnyInputDown(Action, bool oneShot)
SIGS.FireAnyInput()
```

**UI System:**

```csharp
// Button Control
SIGS.DisableButtons()
SIGS.EnableButtons()

// View Control
SIGS.UIViewControl(string name, bool show)
SIGS.ShowPopUp(string name, float time, bool unscaled)
SIGS.IsUIViewVisible(string name)

// References
SIGS.GetView(string name)
SIGS.GetButton(string name)
SIGS.GetElement(string name)

// Events
SIGS.OnClickAnywhere(Action, bool oneShot)

// Back Button
SIGS.DisableBackButton()
SIGS.EnableBackButton()
```

**Radio System:**

```csharp
// Events
SIGS.Send(string eventName)
SIGS.Send(string eventName, params object[] args)
SIGS.Listener(string eventName, Action callback, bool oneShot, GameObject context)

// Audio
SIGS.PlayAudio(string name, params IAudioPlayingSettings[] settings)
SIGS.PlayAudioIfNotPlaying(string name)
SIGS.StopAudio(string name, bool fadeOut, float fadeTime)

// Values
SIGS.LiveKey(string key, Func<object> value, GameObject context)
SIGS.DeadKey(string key, object value, GameObject context)
SIGS.GetLiveValue<T>(string key)
SIGS.GetDeadValue<T>(string key)
SIGS.LiveKeyExists(string key)
SIGS.DeadKeyExists(string key)

// Channels
SIGS.Channel(string name)
SIGS.IsChannelLive(string name)
```

**Utilities:**

```csharp
// Timing
SIGS.DoIn(float time, Action callback, bool unscaled)
SIGS.DoNext(Action callback)
SIGS.DoAfterFrames(int frames, Action callback)

// Conditional
SIGS.DoWhen(Func<bool> condition, Action callback)
SIGS.DoWhenever(Func<bool> condition, Action ifTrue, Action ifFalse)
SIGS.DoWhile(Func<bool> condition, Action action, float wait, Func<bool> locker)
SIGS.DoUntil(Func<bool> condition, Action action)

// Repeating
SIGS.DoEveryInterval(float interval, Action callback, bool unscaled)
SIGS.DoLoop(float duration, float frequency, Action callback)
SIGS.DoFrameUpdate(Action callback, bool unscaled)
SIGS.DoRandomly(float min, float max, Action callback)

// Retry
SIGS.DoRetries(Func<bool> tryAction, int maxRetries, float delay, Action onSuccess, Action onFailure)

// Cooldown
SIGS.IsOnCooldown(float time, string key)
SIGS.CooldownGate(string key, float time, bool unscaled)
SIGS.KillCooldownGate(string key)
SIGS.DoActionWithCooldown(Action action, float time, string key, bool unscaled)

// Probability
SIGS.ThrowDice(float chance)

// Promise System
SIGS.BeginPromiseFlow()
SIGS.BeginTimePromise(float interval)
```

**Game Systems:**

```csharp
// Loading Screen
SIGS.LoadSceneAsync(string sceneName)
SIGS.PrepareLoadingScreen(UIView prefab)
SIGS.CleanLoadingScreens()
SIGS.OnLoadingScreen

// Tutorial
SIGS.HighlightElement(string name, ElementHighlightProperties props)
SIGS.ResetTutorials()

// Audio Layering
SIGS.AudioLayer(string id)
SIGS.CleanseAudioLayers()

// Pooling
SIGS.PoolingGet(GameObject prefab, float lifetime, bool enabled)
SIGS.PoolingGet(GameObject prefab, int count, float lifetime, bool enabled)
SIGS.PoolingWarmup(GameObject prefab, int count)
SIGS.PoolingActiveCount(GameObject prefab, int count)
SIGS.PoolingTryGetCached<T>(Dictionary<Type, Component> cache)
SIGS.PoolingClear()

// Resource Caching
SIGS.InitializeResourceCaching()
SIGS.GetResource<T>(string key)
SIGS.HasResource(string key)
SIGS.GetAllResourceKeys()
SIGS.GetResourceCacheSize()
SIGS.ClearResourceCache()

// Save System
SIGS.InitializeSaveCache(string fileName)
SIGS.SaveData(string key, object value, string fileName)
SIGS.SavePreference(string key, object value)
SIGS.ForceSaveAllAsync()
SIGS.LoadData<T>(string key, string fileName, T defaultValue)
SIGS.LoadPreference<T>(string key, T defaultValue)
SIGS.LoadString(string key, string fileName)
SIGS.LoadAllSaveData(string fileName)
SIGS.DeleteSaveFile(string fileName)
SIGS.DeleteSaveKey(string key, string fileName)
SIGS.SaveKeyExists(string key, string fileName)
SIGS.SaveFileExists(string fileName)
SIGS.WipeAllSaveData()
SIGS.HasPendingSaves(string fileName)
SIGS.HasAnyPendingSaves()
SIGS.GetPendingSaveCount()
SIGS.ShutdownSaveSystem()

// Inventory System
SIGS.InitializeInventorySystem()
SIGS.ShutdownInventorySystem()
SIGS.ClearInventoryCache()

// Currency System
SIGS.GetCurrency(string name)

// Localization System
SIGS.InitializeLocalization()
SIGS.GetLocalizedString(string key)
SIGS.GetTextStyle(string languageCode, string paragraphStyle)
SIGS.ChangeLanguage(string code, bool save)
SIGS.GetCurrentLanguage()
SIGS.FireLanguageChangeEvent()
SIGS.HasLocalizationKey(string key)
SIGS.GetAvailableLanguages()
SIGS.GetLocalizedAudioClip(string key)
SIGS.GetLocalizedSprite(string key)
SIGS.GetLocalizedAsset<T>(string key)
SIGS.HasLocalizedAudioClip(string key)
SIGS.HasLocalizedSprite(string key)
SIGS.HasLocalizedAsset(string key)

// Haptics System
SIGS.TriggerHaptic(HapticSettings settings)
SIGS.TriggerHaptic(HapticType type, float intensity, float duration)
SIGS.TriggerHapticPreset(HapticType type)
SIGS.StopAllHaptics()
SIGS.GetHapticDeviceInfo()
SIGS.IsHapticsEnabled()
SIGS.SetHapticsEnabled(bool enabled)
```

### 9.2 Component API Quick Reference

**UIView:**

```csharp
void Show()
void Hide()
void ShowAsPopUp(float time, bool unscaled)
void Clickback()
bool IsShowing
bool IsHidden
bool IsShown
```

**UIButton:**

```csharp
void SimulateClick()
void PlayClickAnimation()
void SetToggleState(bool state)
bool GetToggle()
void ShowMenu(string name)
void HideMenu(string name)
bool IsInteractable
```

**UIAnimatable:**

```csharp
void Play()
void Stop()
void ForceComplete()
bool IsPerforming
```

**UIFill:**

```csharp
void SetFill(float value)
void SetFill(float value, float duration)
void BindToLiveKey(string key)
```

**LocalizedText:**

```csharp
string localizationKey
string paragraphStyle  // String-based paragraph style (e.g., "Header", "Body")
void UpdateText()
void SetParagraphStyle(string style)  // Set paragraph style as string
string ParagraphStyle  // Get current paragraph style
```

**Listener:**

```csharp
Listener(string eventName, Action callback, bool oneShot, GameObject context)
void Dispose()
```

**LiveKey:**

```csharp
LiveKey(string key, Func<object> valueGetter, GameObject context)
void Dispose()
```

**DeadKey:**

```csharp
DeadKey(string key, object value, GameObject context)
void Dispose()
```

### 9.3 Extension Methods

**TMP_Text Extensions:**

```csharp
void SetLocalizedText(string key)
void SetLocalizedText(string key, TextStyle style)
void SetLocalizedText(string key, TextStyle style, string languageCode, string paragraphStyle)
void SetLocalizedTextFormat(string key, TextStyle style, string languageCode, string paragraphStyle, params object[] args)
```

**String Extensions:**

```csharp
bool HasValue()  // !string.IsNullOrEmpty
```

**Transform Extensions:**

```csharp
void DestroyAllChildren()
void SetX(float value)
void SetY(float value)
void SetZ(float value)
```

**GameObject Extensions:**

```csharp
T GetOrAddComponent<T>()
```

---

## 10. Troubleshooting

### 10.1 Common Issues

**Issue: "SignaliaConfigAsset not found"**

```
Solution: Config auto-creates at Resources/Signalia/SigConfig.asset
If missing:
1. Create Resources/Signalia folder
2. Restart Unity
3. Config will auto-generate
```

**Issue: "Buttons not responding"**

```
Check:
1. RuntimeValues.UIConfig.ButtonsCanBeClicked
2. DisableButtons() was called?
3. View animation lock enabled?
4. Animatable blocking clicks?
5. EventSystem present in scene?
```

**Issue: "Events not firing"**

```
Check:
1. Event name spelling (case-sensitive)
2. Listener created before Send()?
3. OneShot listener already fired?
4. Context GameObject destroyed?
5. Enable debug logging to verify
```

**Issue: "Pooled objects not spawning"**

```
Check:
1. Prefab reference assigned?
2. Pooling system enabled in config?
3. Check console for pooling errors
4. Verify prefab is not null
```

**Issue: "Localization not working"**

```
Check:
1. InitializeLocalization() called?
2. LocBook assigned to config?
3. Key exists in LocBook?
4. Current language has translation?
5. TextStyle exists for language?
```

**Issue: "Save data not persisting"**

```
Check:
1. Save system initialized?
2. ForceSaveAllAsync() before quit?
3. Correct file name used?
4. Permissions on save folder?
5. Check Application.persistentDataPath
```

### 10.2 Debug Tools

**System Vitals Window:**

```
Tools > Signalia > System Vitals
```

Shows:
- Active listeners
- Live/Dead keys
- UI registry
- Pooling status
- Memory usage

**Enable Logging:**

```csharp
// In SignaliaConfigAsset
EnableDebugging = true
LogEventSend = true
LogListenerCreation = true
LogEventReceive = true
```

**Manual Checks:**

```csharp
// Check Watchman
Debug.Log(Watchman.Instance != null);
Debug.Log(Watchman.IsQuitting);

// Check config
Debug.Log(ConfigReader.GetConfig() != null);

// Check registries
Debug.Log(RuntimeValues.TrackedValues.ViewRegistry.Count);
Debug.Log(RuntimeValues.TrackedValues.ButtonRegistry.Count);
```

---

## 11. Advanced Topics

### 11.1 Custom Game Systems

Create custom systems that integrate with Signalia:

```csharp
public static class MyCustomSystem
{
    private static bool initialized = false;
    
    public static void Initialize()
    {
        if (initialized) return;
        
        Watchman.Watch();  // Ensure Watchman exists
        
        // Subscribe to termination
        Watchman.OnTermination += Cleanup;
        
        // Setup your system
        // ...
        
        initialized = true;
    }
    
    private static void Cleanup()
    {
        // Cleanup logic
        initialized = false;
    }
}

// Add to SIGS via extension
public static class SIGSExtensions
{
    public static void InitializeCustomSystem(this SIGS _)
    {
        MyCustomSystem.Initialize();
    }
}

// Usage
SIGS.InitializeCustomSystem();
```

### 11.2 Custom Animations

Create custom UIAnimationAssets:

```csharp
// Create animation asset
var anim = ScriptableObject.CreateInstance<UIAnimationAsset>();
anim.animationType = AnimationType.Scale;
anim.duration = 0.3f;
anim.easeType = Ease.OutBack;
anim.endScale = Vector3.one * 1.2f;

// Assign to components
button.clickAnimation = anim;
```

### 11.3 Multi-Scene Architecture

```csharp
public class SceneManager : MonoBehaviour
{
    void Start()
    {
        // Persistent scene (has Watchman)
        DontDestroyOnLoad(gameObject);
        
        // Listen for scene transitions
        SIGS.Listener("LoadScene", (args) => {
            string sceneName = (string)args[0];
            SIGS.LoadSceneAsync(sceneName);
        });
        
        // Cleanup per scene
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnload;
    }
    
    void OnSceneUnload(Scene scene)
    {
        // Scene-specific cleanup
        // Context-based listeners auto-cleanup
    }
}
```

---

## 12. Changelog

**Version 4.0.0:**
- **Localization System Overhaul:**
  - Complete refactor to standalone namespace
  - String extraction system with ILocbookExtraction interface
  - Paragraph style support (string-based, custom values)
  - Material override support for TextStyle
  - Arabic text formatting and shaping support
  - Arabic TMP font creator editor window
  - Japanese and Chinese font support
  - Smart symbol mirroring for RTL text
  - Support for audio, image, and asset localization pages
  - Lingramia integration improvements
  - Auto refresh cache and auto update LocBooks options
  - Multiple LocBooks support
  - Enhanced LocBook workflow and UI
  - Comprehensive localization documentation

- **Inventory System:**
  - Complete inventory management framework
  - Grid-based display system
  - ItemSlot, ItemGrid, and GameInventory components
  - Custom properties system for items
  - Item audio support for inventory actions
  - Usage pipelines for items
  - Integration with Game Saving system
  - Persistent inventory support

- **Game Saving System:**
  - Architecture refactoring for improved performance
  - Async delete operations
  - Better error handling and logging
  - Encryption settings warnings

- **Pooling System:**
  - Enhanced spawn options and modes
  - ParentedEmitter for flexible audio positioning
  - Spawn enabled/disabled options
  - Active count query methods
  - Performance improvements

- **UI System:**
  - Notes component with hierarchy icon support
  - UIButton improvements (TreatHoverAsSelection)
  - Animation preview reset fixes
  - Back button enable/disable methods
  - Non-blocking animation option for UIAnimatable
  - Toggle event sender for UIButton
  - Animatable fragmentation with composite keying (stores initial transform state for smooth animations)

- **Utilities:**
  - SIGS.HoldOn utility for delayed execution control
  - SIGS.DoFrameUpdate utility method
  - Thread-safe delayed execution improvements
  - Cooldown gate enhancements

- **Common Mechanics:**
  - Trigger Box component
  - Interactive Zone component
  - Currency management system with UI helper

- **Audio System:**
  - Audio asset preloading method
  - Search and pagination for audio assets
  - Audio Player "Remember Me" option
  - Preloaded audio templates

- **Resource Caching:**
  - Complete resource caching system
  - String-based resource lookup
  - Type-safe resource retrieval

- **Framework Improvements:**
  - Comprehensive Signalia Framework documentation
  - Unified game system access through framework accessors
  - Watchman logic improvements for application state handling
  - Streamlined initialization process
  - Improved namespace organization

- **Editor Tools:**
  - Enhanced atlas texture naming in SignaliaTMPFontFactory
  - Improved editor windows and inspectors
  - Better visual coherence across editor UI
  - Simplified toolbar and context menu localization access

- **Documentation:**
  - Complete system documentation rewrite
  - Localization extraction guide
  - Updated API references
  - Enhanced troubleshooting guides

- **Bug Fixes:**
  - Paragraph style fallback logic improved to treat it as a preference
  - Namespace reference for paragraph style in GetTextStyle method
  - Compilation errors and namespacing issues resolved
  - Arabic formatting symbol alignment issues
  - LocBook structure - proper Pages -> Entries hierarchy implementation
  - Create LocBook button error handling and logging
  - Non-Arabic text reversal issues in RTL formatting
  - Complex Radio content loading issues resolved
  - Clickback search bar in settings window

**Version 3.1.0:**
- Preloaded audio templates
- InputSystem compatibility improvements
- AudioLayer dropdown fixes
- Unity 6 support
- Enhanced search functionality

**Version 3.0.0:**
- Audio Layering system
- Haptics system
- 3D Audio support
- System Vitals window
- Promise system enhancements
- UIFill component
- Major namespace reorganization

**Version 2.2.0:**
- Button registry
- Promise system
- Unity 6 support
- Cooldown gates
- Smart button dimming

---

## 13. Support & Resources

**Documentation:**
- Offline PDF: `Assets/AHAKuo Creations/Signalia/Offline Documentation/StartHere.pdf`
- This file: `SIGNALIA_DOCUMENTATION.md`

**Support Channels:**
- **Discord Server**: Join the AHAKuo Discord server for community support, discussions, and updates
- **YouTube Channel**: Visit the AHAKuo YouTube channel for tutorials, showcases, and feature demonstrations
- **Contact Page**: For specific requests or business inquiries, visit ahakuo.com/contact-page

**Menu Items:**
- `Tools > Signalia > System Vitals` - Debug window
- `Tools > Signalia > Packages` - Package management
- `Tools > Signalia > Game Systems` - System-specific tools

**Asset Store:**
- Publisher: AHAKuo Creations

**Example Scenes:**
- `Assets/AHAKuo Creations/Signalia/Examples [Optional]/`

---

## 14. Conclusion

Signalia provides a complete, integrated framework for Unity game development. By following this documentation and the provided examples, you can build sophisticated UI systems and game features with minimal boilerplate code.

**Key Takeaways:**

1. **Use SIGS for everything** - Unified API access
2. **Leverage the event system** - Decouple your code
3. **Use game systems modularly** - Only enable what you need
4. **Follow naming conventions** - Keep your project organized
5. **Enable debugging during development** - Catch issues early

**Next Steps:**

1. Review example scenes
2. Configure SignaliaConfigAsset for your project
3. Build your UI with UIView and UIButton
4. Integrate game systems as needed
5. Reference this documentation when needed

Thank you for using Signalia Framework!

---

**Document Version:** 1.0  
**Framework Version:** 4.0.0  
**Last Updated:** 2025-11-02  
**Author:** AHAKuo Creations  
**Task ID:** 86evc2tj9
