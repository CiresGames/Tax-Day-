using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities.SIGInput;
using System;
using UnityEngine;
using UnityEngine.Serialization;

using AHAKuo.Signalia.GameSystems.AchievementSystem;
using AHAKuo.Signalia.GameSystems.AudioLayering;
using AHAKuo.Signalia.GameSystems.ResourceCaching;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using AHAKuo.Signalia.GameSystems.DialogueSystem;

namespace AHAKuo.Signalia.Framework
{
    public class SignaliaConfigAsset : ScriptableObject
    {
        public float DefaultButtonsCooldown = 0.1f;
        public bool PreventButtonsClickingWhenViewAnimating = false;
        public bool DisableEventSystemWhenViewAnimating = false;
        public bool PreventButtonsClickingWhenAnimatableAnimating = false;
        public bool DisableEventSystemWhenAnimatableAnimating = false;
        public string ClickBackAudio;
        public bool AlwaysClickBackAudio = false;
        public bool KeepManagerAlive = false;

        public string UIViewAnimatingIn = "";
        public string UIViewAnimatingOut = "";
        public string UIButtonClicked = "";

        public string UIButtonsDisabler = "";
        public string UIButtonsEnabler = "";
        public string UnityEventSystemOff = "";
        public string UnityEventSystemOn = "";
         
        public bool ConvertAllButtonsToUIButtons = false;
        public bool OverrideUiViewAnimateOnce = false;
        public bool UIViewsAnimateOnce = false;
        public bool AutoAddEffector = true;
        public bool AutoAddBackButton = true;
        public bool DisableButtonBlockers_TMPText = false;

        public AudioMixerAsset AudioMixerAsset;
        public bool DisableAudioMixerLoading = false;
        public bool TwoSideListeners = false;
        
        [Header("Audio Assets")]
        [Tooltip("Direct references to AudioAsset files. If empty, ResourceHandler will attempt to find and assign them automatically.")]
        public AudioAsset[] AudioAssets = new AudioAsset[0];

        [Header("Input")]
        [Tooltip("Action maps used by Signalia's input bridge. These only define action names and types.")]
        public SignaliaActionMap[] InputActionMaps = new SignaliaActionMap[0];

        [Tooltip("UIBackButton will trigger SIGS.Clickback() when any of these input actions are pressed (edge-detected from SIGS.GetInput()).")]
        public string[] BackButtonActionNames = new string[] { "Back", "Cancel" };

        [Header("Resource Caching")]
        [Tooltip("Direct references to ResourceAsset files. These contain cached resources accessible by string keys.")]
        public ResourceAsset[] ResourceAssets = new ResourceAsset[0];

        // Debugging
        public bool EnableDebugging = false;
        public bool UseIntrospection = true;
        public bool LogListenerCreation = false;
        public bool LogListenerDisposal = false;
        public bool LogEventSend = false;
        public bool LogEventReceive = false;
        public bool LogLiveKeyCreation = false;
        public bool LogLiveKeyRead = false;
        public bool LogLiveKeyDisposal = false;
        public bool LogDeadKeyCreation = false;
        public bool LogDeadKeyRead = false;
        public bool LogDeadKeyDisposal = false;
        public bool LogHaptics = false;

        // ComplexRadio debugging options
        public bool LogComplexListenerCreation = false;
        public bool LogComplexListenerDisposal = false;
        public bool LogChannelCreation = false;
        public bool LogChannelDisposal = false;
        public bool LogChannelSend = false;
        public bool LogChannelReceive = false;
        
        // Haptics settings
        public bool EnableHaptics = true; // it's a setting unlike most other features as haptics may not be wanted by players.
        public bool AutoSaveHapticSetting = true;
        public string HapticsSaveKey = "haptic_enable";
        public float Haptics_GlobalIntensityMultiplier = 0.5f;
        public float Haptics_GlobalDurationMultiplier = 0.5f;

        // Game System settings
        public PoolingSettings PoolingSystem = new();
        public LoadingScreenSettings LoadingScreen = new();
        public SavingSystemSettings SavingSystem = new();
        public AudioLayeringSettings AudioLayering = new();
        public CurrencySystemSettings CurrencySystem = new();
        public CommonMechanicsSettings CommonMechanics = new();
        public InventorySystemSettings InventorySystem = new();
        public InlineScriptSettings InlineScript = new();
        public LocalizationSystemSettings LocalizationSystem = new();
        public DialogueSystemSettings DialogueSystem = new();
        public AchievementSystemSettings AchievementSystem = new();
        
        // Signalia Time settings
        public SignaliaTimeSettings SignaliaTime = new();
        
        // Input System settings
        public InputSystemSettings InputSystem = new();
    }

    ////// Setting Classes for Game Systems other than basic Signalia features //////
    [Serializable]
    public class SavingSystemSettings
    {
        [Tooltip("The default file name for preferences/settings")]
        public string SettingsFileName = "settings";
        
        [Tooltip("The file extension to use for save files")]
        public string SaveFileExtension = ".sav";
        
        [Tooltip("The directory path where save files are stored (relative to persistentDataPath). Leave empty to save to root.")]
        public string SaveDirectoryPath = "SaveData";
        
        [Tooltip("Enable logging for save operations")]
        public bool LogSaving = false;
        
        [Tooltip("Files to cache on startup for faster access")]
        public string[] CachedSaveFiles = new string[0];
        
        [Tooltip("Per-file encryption rules. Configure which files should be encrypted and their passwords.")]
        public EncryptionEntry[] EncryptionRules = new EncryptionEntry[0];
    }
    
    [Serializable]
    public class EncryptionEntry
    {
        [Tooltip("The file name to encrypt (without extension)")]
        public string fileName = "";
        
        [Tooltip("Enable encryption for this file")]
        public bool encrypt = false;
        
        [Tooltip("The encryption password for this file")]
        public string password = "";
    }

    [Serializable]
    public class PoolingSettings
    {
        public bool SmartPoolLifetimeKill = true;
        [Tooltip("Maximum number of objects allowed in each pool. Set to 0 for unlimited.")]
        public int CeilingLimit = 0;
        [Tooltip("When ceiling is reached, recycle oldest objects instead of creating new ones.")]
        public bool EnableRecycling = false;
    }

    [Serializable]
    public class LoadingScreenSettings
    {
        public UIView LoadingScreenPrefab;
        public string EventOnLoadInitialComplete;
        public string EventOnLoadFullyComplete;
        public bool SimulateFakeLoading = false;
        public float FakeLoadingTime = 2f;
        public bool ClickToProgress = false;
        public string ProgressionEvent;
        [Tooltip("When ClickToProgress is enabled, any of these Signalia input actions will also trigger progression (in addition to clicking). These must exist in your SignaliaActionMap assets.")]
        public string[] ClickToProgressActionNames = new string[] { "Confirm", "Submit", "Interact" };
        public bool preloadLoadingScreen = false;
    }

    [Serializable]
    public class AudioLayeringSettings
    {
        public AudioLayeringLayerData LayerData;
        public float FadeDuration = 1f;
        public bool ContinuousPlaying = false;
    }

    [Serializable]
    public class CurrencySystemSettings
    {
        public string SaveFileName = "gd_lcl";
        [Tooltip("Define limits for different currencies. Each currency can have custom min/max values or infinite limits.")]
        public CurrencyLimit[] CurrencyLimits = new CurrencyLimit[0];
    }

    [Serializable]
    public class CurrencyLimit
    {
        [Tooltip("The name of the currency this limit applies to")]
        public string CurrencyName = "gold";
        
        [Tooltip("Type of minimum limit for this currency")]
        public CurrencyLimitType MinLimitType = CurrencyLimitType.Infinite;
        
        [Tooltip("Custom minimum value (only used when MinLimitType is Custom)")]
        public float CustomMinValue = 0f;
        
        [Tooltip("Type of maximum limit for this currency")]
        public CurrencyLimitType MaxLimitType = CurrencyLimitType.Infinite;
        
        [Tooltip("Custom maximum value (only used when MaxLimitType is Custom)")]
        public float CustomMaxValue = 1000f;
    }

    public enum CurrencyLimitType
    {
        Infinite,
        Custom
    }

    [Serializable]
    public class CommonMechanicsSettings
    {
        public InteractiveZoneSettings InteractiveZone = new();
    }

    public enum InteractiveZoneInvokeType
    {
        SignaliaInputAction,
        SignaliaRadioEvent
    }

    public enum InteractiveZoneInputTriggerMode
    {
        Down,
        Held,
        Up
    }

    [Serializable]
    public class InteractiveZoneSettings
    {
        [Tooltip("How Interactive Zones should detect interaction input.")]
        public InteractiveZoneInvokeType InvokeType = InteractiveZoneInvokeType.SignaliaInputAction;

        [Tooltip("Signalia input action name used when InvokeType is SignaliaInputAction. Must exist in your Signalia Action Maps.")]
        public string InputActionName = "Interact";

        [Tooltip("How the input action should be evaluated when InvokeType is SignaliaInputAction.")]
        public InteractiveZoneInputTriggerMode ActionTrigger = InteractiveZoneInputTriggerMode.Down;

        [Tooltip("For Down/Up checks: if true, consumes the edge so only one consumer can react per frame.")]
        public bool OneFrameConsume = true;

        [Tooltip("If true, only triggers when the action is enabled (input not disabled, action not disabled, and in an enabled action map).")]
        public bool RequireActionEnabled = true;

        [Tooltip("The Signalia event name sent when interaction input is pressed. Interactive zones listen for this event when InvokeType is SignaliaRadioEvent.")]
        public string InputEventName = "cmn_interact";

        [Tooltip("Fallback to Unity's legacy input system and check for a specific key when listening for interactions.")]
        public bool UseLegacyInputFallback = true;

        [Tooltip("Legacy input key to check when UseLegacyInputFallback is enabled.")]
        public KeyCode LegacyFallbackKey = KeyCode.E;

        [Tooltip("Default display name used for input prompts when no custom binding information is provided.")]
        public string BindingDisplayName = "Interact";

        [Tooltip("Save file name used when Interactive Zones persist their completion state using the Save System.")]
        public string SaveFileName = "cmn_interactions";

        [Tooltip("If true, Interactive Zones will not allow interaction while the Dialogue system is active (DialogueManager.InDialogueNow).")]
        public bool DisableDuringDialogue = true;
    }
}
    [Serializable]
    public class InventorySystemSettings
    {
        [Header("Auto-Save Settings")]
        [Tooltip("Enable auto-save for all inventory modifications")]
        public bool EnableAutoSave = false;
        
        [Tooltip("Auto-save for any inventory (not specific IDs). Overrides AutoSaveInventoryIds when enabled.")]
        public bool AutoSaveAnyInventory = false;
        
        [Tooltip("Specific inventory IDs to auto-save. Leave empty to use AutoSaveAnyInventory instead.")]
        public string[] AutoSaveInventoryIds = new string[0];
        
        [Tooltip("Log when auto-saving inventories")]
        public bool LogAutoSave = false;
        
        [Header("Item References")]
        [Tooltip("References to ItemSO assets that can be saved/loaded. These are cached at runtime for efficient lookups.")]
        public ItemSO[] ItemReferences = new ItemSO[0];
        
        [Header("Custom Property Display")]
        [Tooltip("Default fallback text shown when an item doesn't have a custom property that was expected to be displayed.")]
        public string CustomPropertyFallbackText = "-";
        
        [Header("Display Settings")]
        [Tooltip("When enabled, ItemDisplayerPanel shows the total quantity of the item across all stacks in the inventory. When disabled, shows only the current stack quantity.")]
        public bool ShowTotalQuantityInDisplayer = false;
        
        [Header("Failure Events")]
        [Tooltip("Event sent when items fail to be added. Parameters: ItemSO item, InventoryDefinition inventory, int quantity, string reason")]
        public string OnItemAddFailed = "Inventory_ItemAddFailed";
        
        [Tooltip("Event sent when items fail to be removed. Parameters: ItemSO item, InventoryDefinition inventory, int quantity, string reason")]
        public string OnItemRemoveFailed = "Inventory_ItemRemoveFailed";
        
        [Tooltip("Event sent when items fail to be moved. Parameters: ItemSO item, InventoryDefinition source, InventoryDefinition target, int quantity, string reason")]
        public string OnItemMoveFailed = "Inventory_ItemMoveFailed";
    }

    [Serializable]
    public class DialogueSystemSettings
    {
        [Tooltip("When exiting into another dialogue object, hide and then reshow the dialogue UI to indicate that the dialogue has progressed to a different scope. NOTE: This needs the UIView to have its 'Play Only When Changing Status' option disabled.")]
        public bool reshowOnExitObject = true; // if true, dialogue will show and hide when exiting into dialogue objects
        public float reshowDelay = 0.2f;
        public float continueButtonDelay = 0.5f; // time before you can continue again.
        [Tooltip("Signalia input actions that should act as a dialogue 'continue' trigger (in addition to clicking the continue button). These must exist in your SignaliaActionMap assets.")]
        public string[] ContinueActionNames = new string[] { "Confirm", "Submit", "Interact" };
        public bool disableContinueButtonOnChoice = true;
        public bool hideSpeakerNameOnChoice = false;
        public bool hideSpeechAreaOnChoice = false;
        
        [Header("Action Map Management")]
        [Tooltip("When enabled, automatically disables target action maps and enables GUI action maps when dialogue starts. Reverses on dialogue end.")]
        public bool EnableActionMapSwitching = false;
        
        [Tooltip("Action map names to disable when dialogue starts. If empty, defaults to 'Default'. Use the MapName from your SignaliaActionMap asset, or the asset name if MapName is empty.")]
        public string[] DisableActionMapNames = new string[] { "Default" };
        
        [Tooltip("Action map names to enable when dialogue starts. If empty, defaults to 'GUI'. Use the MapName from your SignaliaActionMap asset, or the asset name if MapName is empty.")]
        public string[] EnableActionMapNames = new string[] { "GUI" };
        
        [Tooltip("Delay in seconds before re-enabling action maps when dialogue ends. Default is 0.1 seconds.")]
        public float ActionReEnableDelay = 0.1f;

        public ChoiceOmission ChoiceOmissionMode = ChoiceOmission.OmitFully;
        public string ChoiceOmissionString = "???";
    }

    [Serializable]
    public class AchievementSystemSettings
    {
        [Header("Storage")]
        [Tooltip("Save file name used to persist unlocked achievements (without extension).")]
        public string SaveFileName = "achievements";

        [Tooltip("Prefix used for all achievement save keys (e.g., 'ach_' -> 'ach_first_blood').")]
        public string SaveKeyPrefix = "ach_";

        [Header("Notifications")]
        [Tooltip("Show a Notification System message when an achievement is unlocked.")]
        public bool ShowNotifications = true;

        [Tooltip("SystemMessage name used by NotificationMethods.ShowNotification. The SystemMessage must be registered as a DeadKey named 'SystemMessage_<Name>'.")]
        public string NotificationSystemMessageName = "System";

        [Tooltip("String.Format pattern. Argument {0} = achievement title.")]
        public string NotificationFormat = "Achievement Unlocked: {0}";

        [Header("Events")]
        [Tooltip("Radio event sent when an achievement is unlocked. Parameters: AchievementSO achievement")]
        public string OnAchievementUnlockedEvent = "Achievement_Unlocked";

        [Header("Definitions")]
        [Tooltip("All achievements available in this project.")]
        public AchievementSO[] Achievements = new AchievementSO[0];

        [Header("Display Options")]
        [Tooltip("If true, unlocked achievements will still be shown in viewers. If false, they will be hidden after unlocking.")]
        public bool ReshowUnlockedAchievements = false;

        [Header("Backend")]
        [Tooltip("Optional adapter used to forward unlock events to an external backend (Steam/Console/Custom API).")]
        public AchievementBackendAdapter BackendAdapter;
    }

    [Serializable]
    public class SignaliaTimeSettings
    {
        [Header("Auto-Add")]
        [Tooltip("Automatically add the SignaliaTime component to the scene when Watchman initializes.")]
        public bool AutoAddSignaliaTime = true;

        [Header("UIView Time Modifiers")]
        [Tooltip("Configure UIViews that modify time when visible. When a view with a matching name becomes visible, its time modifier is applied. When hidden, the modifier is removed.")]
        public UIViewTimeModifier[] UIViewTimeModifiers = new UIViewTimeModifier[0];
    }

    [Serializable]
    public class UIViewTimeModifier
    {
        [Tooltip("The name of the UIView (must match the Menu Name set on the UIView component).")]
        public string ViewName = "";

        [Tooltip("Whether this view should pause time completely (sets modifier to 0).")]
        public bool PauseTime = false;

        [Tooltip("Time modifier value (0 = pause, 1 = normal speed). Only used if PauseTime is false.")]
        [Range(0f, 1f)]
        public float TimeModifierValue = 1f;

        [Tooltip("Optional description for this modifier (for debugging).")]
        public string Description = "";

        /// <summary>
        /// Gets the effective time modifier value.
        /// </summary>
        public float EffectiveValue => PauseTime ? 0f : TimeModifierValue;
    }

    [Serializable]
    public class InlineScriptSettings
    {
        [Header("Global Imports")]
        [Tooltip("Namespaces that will be appended to every generated inline script. Add one using directive per line. These namespaces will be available in all your inline scripts.")]
        [TextArea(3, 10)]
        public string GlobalUsings = "";
        
        [Header("Cache Settings")]
        [Tooltip("Root path for user-generated script cache")]
        public string RootPath_Cache = "Assets/AHAKuo Creations/InlineScript_Cache/";
    }

    [Serializable]
    public class LocalizationSystemSettings
    {
        [Header("Hybrid Key Mode")]
        [Tooltip("When enabled, the localization system will search for strings by key, value, and aliases. Useful for projects with hardcoded strings that need localization.")]
        public bool HybridKey = false;
        
        [Header("LocBook Configuration")]
        [Tooltip("Array of LocBook assets containing localization data. Each LocBook will be loaded into the system.")]
        public AHAKuo.Signalia.GameSystems.Localization.Internal.LocBook[] LocBooks = new AHAKuo.Signalia.GameSystems.Localization.Internal.LocBook[0];
        
        [Header("Text Style Cache")]
        [Tooltip("Cache of TextStyle assets for different languages. These define font and formatting settings per language.")]
        public AHAKuo.Signalia.GameSystems.Localization.Internal.TextStyle[] TextStyleCache = new AHAKuo.Signalia.GameSystems.Localization.Internal.TextStyle[0];
        
        [Header("Default Settings")]
        [Tooltip("The default starting language code (e.g., 'en', 'es', 'fr'). This is used when no saved preference exists.")]
        public string DefaultStartingLanguageCode = "en";
        
        [Header("Save Settings")]
        [Tooltip("The key used to save/load the user's language preference using the Game Saving system.")]
        public string LanguageOptionSaveKey = "language";
        
        [Header("Events")]
        [Tooltip("Radio event sent when the language is changed. Use this to update UI elements that display localized text.")]
        public string LanguageChangedEvent = "local_updated";
        
        [Header("Internal")]
        [Tooltip("When enabled, automatically updates LocBook assets when their referenced .locbook files are imported or modified.")]
        public bool AutoUpdateLocbooks = false;
        
        [Tooltip("When enabled, automatically refreshes the localization cache in runtime when LocBook assets are updated. WARNING: This will impact editor performance while playing.")]
        public bool AutoRefreshCacheInRuntime = false;
    }

    [Serializable]
    public class InputSystemSettings
    {
        [Header("General")]
        [Tooltip("Enable Signalia's input system. When disabled, Signalia will not read input actions, apply modifiers, or auto-load action maps.")]
        public bool EnableSignaliaInputSystem = true;

        [Header("Input Blockers")]
        [Tooltip("Configure input blockers that disable specific action maps or actions when certain UIViews are shown.")]
        public InputBlocker[] InputBlockers = new InputBlocker[0];

        [Header("Cursor Visibility")]
        public InputMiceVisibilitySettings CursorVisibility = new();

        [Header("Input Buffering")]
        [Tooltip("Enable input buffering system. When enabled, inputs can be buffered (stored) for a short time and executed when conditions allow (e.g., when an animation completes).")]
        public bool EnableInputBuffering = false;

        [Tooltip("Default buffer duration in seconds. Inputs buffered without a specific duration will use this value.")]
        public float DefaultBufferDuration = 0.2f;

        [Tooltip("Use unscaled time for buffer expiration. When enabled, buffers won't be affected by time scale changes.")]
        public bool UseUnscaledTimeForBuffers = false;
    }

    [Serializable]
    public class InputBlocker
    {
        [Tooltip("The Menu Name of the UIView that triggers this blocker (must match exactly).")]
        public string UIViewName = "";

        [Tooltip("Action map names to block when this view is shown.")]
        public string[] ActionMapNames = new string[0];

        [Tooltip("Action names to block when this view is shown.")]
        public string[] ActionNames = new string[0];

        [Header("Cursor Settings")]
        [Tooltip("When enabled, this blocker will also control cursor visibility.")]
        public bool ControlCursor = false;

        [Tooltip("If true, show the cursor when this view is visible. If false, hide and lock the cursor.")]
        public bool ShowCursor = true;

        [Tooltip("Cursor lock state when cursor is hidden. Only used if ShowCursor is false.")]
        public CursorLockMode HiddenCursorLockState = CursorLockMode.Locked;

        [Tooltip("Optional description for this blocker (for debugging).")]
        public string Description = "";
    }

    [Serializable]
    public class InputMiceVisibilitySettings
    {
        [Header("Auto-Add")]
        [Tooltip("Automatically add the InputMiceVisibilityController component to the scene when Watchman initializes.")]
        public bool AutoAddCursorController = true;

        [Header("Default Cursor Behavior")]
        [Tooltip("Controls the baseline cursor visibility when no input state modifiers are active.")]
        public CursorVisibilityDefaultMode DefaultVisibilityMode = CursorVisibilityDefaultMode.VisibleUnlessModified;

        [Header("View-Based Visibility Rules")]
        [Tooltip("Configure cursor visibility rules for specific UIViews. When a view with a matching Menu Name becomes visible or hidden, the configured cursor action is applied.")]
        public UIViewCursorVisibilityRule[] ViewVisibilityRules = new UIViewCursorVisibilityRule[0];

        [Header("Dialogue System Rule")]
        [Tooltip("When enabled, automatically shows the cursor when dialogue is active and hides/locks it when dialogue ends.")]
        public bool EnableDialogueSystemRule = true;

        [Tooltip("Cursor action to apply when dialogue starts.")]
        public CursorVisibilityAction OnDialogueStart = CursorVisibilityAction.Show;

        [Tooltip("Cursor action to apply when dialogue ends.")]
        public CursorVisibilityAction OnDialogueEnd = CursorVisibilityAction.HideAndLock;
    }

    [Serializable]
    public class UIViewCursorVisibilityRule
    {
        [Tooltip("The Menu Name of the UIView (must match exactly).")]
        public string ViewName = "";

        [Tooltip("What happens to the cursor when this view becomes visible.")]
        public CursorVisibilityAction OnViewVisible = CursorVisibilityAction.Show;

        [Tooltip("What happens to the cursor when this view becomes hidden.")]
        public CursorVisibilityAction OnViewHidden = CursorVisibilityAction.HideAndLock;

        [Tooltip("Optional description for this rule (for debugging).")]
        public string Description = "";
    }

    public enum CursorVisibilityAction
    {
        Show,
        Hide,
        HideAndLock
    }

    public enum CursorVisibilityDefaultMode
    {
        VisibleUnlessModified,
        InvisibleUnlessModified,
        OnlyVisibleOnAnyMenu
    }
