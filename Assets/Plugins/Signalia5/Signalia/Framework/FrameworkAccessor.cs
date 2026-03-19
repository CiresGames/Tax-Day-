using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.Utilities.SIGInput;
using Object = UnityEngine.Object;

using AHAKuo.Signalia.GameSystems.LoadingScreens;
using AHAKuo.Signalia.GameSystems.AudioLayering;
using AHAKuo.Signalia.GameSystems.TutorialSystem;
using AHAKuo.Signalia.GameSystems.ResourceCaching;
using AHAKuo.Signalia.GameSystems.SaveSystem;
using AHAKuo.Signalia.GameSystems.PoolingSystem;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.CommonMechanics;
using AHAKuo.Signalia.GameSystems.DialogueSystem;
using AHAKuo.Signalia.GameSystems.Notifications;
using AHAKuo.Signalia.GameSystems.AchievementSystem;

namespace AHAKuo.Signalia.Framework
{
    /// <summary>
    /// SIGS (Signalia Global Shorthand) is a static gateway to commonly-used Signalia tools and systems.
    /// This class simplifies interaction with the UI system, radio system (event broadcasting), utility functions,
    /// timing sequences (via DOTween), value broadcasting, and all game systems.
    /// 
    /// This is meant to centralize all primary Signalia features into one easy-to-access location,
    /// streamlining common operations such as showing UI, sending events, running delayed tasks,
    /// and accessing shared variables or states.
    /// 
    /// This is mainly to improve readability and reduce boilerplate code as well as reduce confusion in what to use or which method is the right one.
    /// 
    /// Example Usage:
    ///     SIGS.ViewControl("Inventory", true);
    ///     SIGS.DoIn(2f, () => Debug.Log("2 seconds passed"));
    ///     SIGS.Send("GameStarted");
    /// </summary>
    public static class SIGS
    {
        //// FRAMEWORK METHODS ////

        /// <summary>
        /// Resets the Signalia runtime values, clearing all tracked values and resetting the state of the framework.
        /// This is useful for resetting the framework to a clean state, such as when reloading scenes or restarting the game.
        /// WARNING: This will destroy the active watchman and all static values, so use with caution.
        /// </summary>
        public static void ResetSignaliaRuntime() => Watchman.ResetEverything(true);

        /// <summary>
        /// Subscribes to any input down event, allowing you to perform an action when any input is detected (e.g., mouse click, touch, key press).
        /// </summary>
        /// <param name="action"></param>
        /// <param name="oneShot"></param>
        public static void OnAnyInputDown(Action action, bool oneShot = false) => RuntimeValues.InputDelegation.SubscribeToAnyInputDown(action, oneShot);

        /// <summary>
        /// Fires any input delegate. Use this if you have a custom input system, and call it on a AnyInput down method you own. Don't call it if you don't use legacy unity input.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="oneShot"></param>
        public static void FireAnyInput() => SIGS.Send(FrameworkConstants.InternalListeners.ANYINPUT_FORCUSTOMINPUT);

        //// INPUT BRIDGE METHODS ////

        /// <summary>Passes an input-down transition to Signalia.</summary>
        public static void PassDown(string actionName) => SignaliaInputBridge.PassDown(actionName);

        /// <summary>Passes an input-held transition to Signalia.</summary>
        public static void PassHeld(string actionName) => SignaliaInputBridge.PassHeld(actionName);

        /// <summary>Passes an input-up transition to Signalia.</summary>
        public static void PassUp(string actionName) => SignaliaInputBridge.PassUp(actionName);

        /// <summary>Passes a float value to Signalia, updating the current recorded value.</summary>
        public static void PassFloat(string actionName, float value) => SignaliaInputBridge.PassFloat(actionName, value);

        /// <summary>Passes a Vector2 value to Signalia, updating the current recorded value.</summary>
        public static void PassVector(string actionName, Vector2 value) => SignaliaInputBridge.PassVector(actionName, value);

        /// <summary>Returns true if the action is currently held.</summary>
        public static bool GetInput(string actionName) => SignaliaInputBridge.Get(actionName);

        /// <summary>Returns true if the action was pressed this frame.</summary>
        public static bool GetInputDown(string actionName, bool oneFrame = false) => SignaliaInputBridge.GetDown(actionName, oneFrame);

        /// <summary>Returns true if the action was released this frame.</summary>
        public static bool GetInputUp(string actionName, bool oneFrame = false) => SignaliaInputBridge.GetUp(actionName, oneFrame);

        /// <summary>Returns the latest float value for the action.</summary>
        public static float GetInputFloat(string actionName) => SignaliaInputBridge.GetFloat(actionName);

        /// <summary>Returns the latest Vector2 value for the action.</summary>
        public static Vector2 GetInputVector2(string actionName) => SignaliaInputBridge.GetVector2(actionName);

        /// <summary>Disables all input actions.</summary>
        public static void DisableInput() => SignaliaInputBridge.DisableInput();

        /// <summary>Enables all input actions.</summary>
        public static void EnableInput() => SignaliaInputBridge.EnableInput();

        /// <summary>Disables a specific action. Prefer using AddInputModifier for proper state management.</summary>
        [System.Obsolete("Use AddInputModifier or SetInputModifier instead for proper state management.")]
        public static void DisableAction(string actionName) => SignaliaInputBridge.DisableAction(actionName);

        /// <summary>Enables a specific action. Prefer using RemoveInputModifier for proper state management.</summary>
        [System.Obsolete("Use RemoveInputModifier instead for proper state management.")]
        public static void EnableAction(string actionName) => SignaliaInputBridge.EnableAction(actionName);

        /// <summary>Disables an entire action map by its name. Prefer using AddInputModifier for proper state management.</summary>
        [System.Obsolete("Use AddInputModifier or SetInputModifier instead for proper state management.")]
        public static void DisableActionMap(string mapName) => SignaliaInputBridge.DisableActionMap(mapName);

        /// <summary>Enables an entire action map by its name. Prefer using RemoveInputModifier for proper state management.</summary>
        [System.Obsolete("Use RemoveInputModifier instead for proper state management.")]
        public static void EnableActionMap(string mapName) => SignaliaInputBridge.EnableActionMap(mapName);

        /// <summary>Clears and releases a specific action.</summary>
        public static void KillAction(string actionName) => SignaliaInputBridge.KillAction(actionName);

        /// <summary>Clears and releases all actions.</summary>
        public static void KillAllActions() => SignaliaInputBridge.KillAllActions();

        #region Input State Modifiers

        /// <summary>
        /// Adds an input state modifier. Returns false if a modifier with the same ID already exists.
        /// Use this for proper input blocking and cursor visibility management.
        /// </summary>
        public static bool AddInputModifier(InputStateModifier modifier) => SignaliaInputBridge.AddModifier(modifier);

        /// <summary>
        /// Adds or updates an input state modifier. If a modifier with the same ID exists, it will be replaced.
        /// </summary>
        public static void SetInputModifier(InputStateModifier modifier) => SignaliaInputBridge.SetModifier(modifier);

        /// <summary>
        /// Removes an input state modifier by its ID.
        /// </summary>
        public static bool RemoveInputModifier(string modifierId) => SignaliaInputBridge.RemoveModifier(modifierId);

        /// <summary>
        /// Removes all input state modifiers from a specific source.
        /// </summary>
        public static int RemoveInputModifiersBySource(string source) => SignaliaInputBridge.RemoveModifiersBySource(source);

        /// <summary>
        /// Checks if an input state modifier with the given ID exists.
        /// </summary>
        public static bool HasInputModifier(string modifierId) => SignaliaInputBridge.HasModifier(modifierId);

        /// <summary>
        /// Gets an input state modifier by its ID.
        /// </summary>
        public static InputStateModifier? GetInputModifier(string modifierId) => SignaliaInputBridge.GetModifier(modifierId);

        /// <summary>
        /// Gets the number of active input state modifiers.
        /// </summary>
        public static int InputModifierCount => SignaliaInputBridge.ModifierCount;

        #endregion

        /// <summary>Sets a cooldown gate for an action.</summary>
        public static void SetInputCooldown(string actionName, float duration, bool unscaled = false) => SignaliaInputBridge.SetCooldown(actionName, duration, unscaled);

        /// <summary>Returns true if the action is on cooldown.</summary>
        public static bool IsInputOnCooldown(string actionName) => SignaliaInputBridge.IsOnCooldown(actionName);

        /// <summary>Buffers an input action for later execution. Use ConsumeBufferedInput() to check and consume buffered inputs.</summary>
        public static void BufferInput(string actionName, float? duration = null, bool? unscaled = null) => SignaliaInputBridge.BufferInput(actionName, duration, unscaled);

        /// <summary>Checks if a buffered input exists and is still valid. If valid, consumes it (removes it from the buffer).</summary>
        public static bool ConsumeBufferedInput(string actionName) => SignaliaInputBridge.ConsumeBufferedInput(actionName);

        /// <summary>Checks if a buffered input exists and is still valid without consuming it.</summary>
        public static bool HasBufferedInput(string actionName) => SignaliaInputBridge.HasBufferedInput(actionName);

        /// <summary>Clears a specific buffered input, or all buffered inputs if actionName is null/empty.</summary>
        public static void ClearBufferedInput(string actionName = null) => SignaliaInputBridge.ClearBufferedInput(actionName);

        /// <summary>Returns true if the input action is currently enabled (not disabled, and in at least one enabled action map).</summary>
        public static bool IsInputActionEnabled(string actionName) => SignaliaInputBridge.IsActionEnabled(actionName);

        /// <summary>Returns true if the input action map is currently enabled (not disabled, and not suppressed by another enabled action map).</summary>
        public static bool IsInputActionMapEnabled(string mapName) => SignaliaInputBridge.IsActionMapEnabled(mapName);

        //// UI SYSTEM METHODS ////

        /// <summary>
        /// Disables all UI buttons, preventing user interaction.
        /// </summary>
        public static void DisableButtons() => UIEventSystem.DisableButtons();

        /// <summary>
        /// Enables all UI buttons, allowing user interaction again.
        /// </summary>
        public static void EnableButtons() => UIEventSystem.EnableButtons();

        /// <summary>Triggers a UI clickback event.</summary>
        public static void Clickback() => UIEventSystem.UIEvents.Clickback();

        /// <summary>Shows or hides a UI view by name.</summary>
        public static void UIViewControl(string s, bool show) => UIEventSystem.UIEvents.InvokeUIView(s, show);

        /// <summary>Displays a popup view for a specified time, with optional unscaled time.</summary>
        public static void ShowPopUp(string menuName, float time, bool unscaled) => UIEventSystem.UIEvents.InvokeUIViewAsPopUp(menuName, time, unscaled);

        /// <summary>Checks if a specific UI view is currently visible.</summary>
        public static bool IsUIViewVisible(string menu) => UIEventSystem.UIEvents.IsUIViewVisible(menu);

        /// <summary>
        /// Retrieves a UIView by its name.
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        public static UIView GetView(string viewName) => UIEventSystem.MenuByName(viewName);

        /// <summary>Retrieves a UIButton by its name.</summary>
        public static UIButton GetButton(string buttonName) => UIEventSystem.ButtonByName(buttonName);

        /// <summary>Retrieves a UIElement by its name.</summary>
        public static UIElement GetElement(string elementName) => UIEventSystem.ElementByName(elementName);

        /// <summary>
        /// Subscribes to the click event of any UI button, allowing you to perform an action when the user clicks any button in the UI.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="oneShot"></param>
        public static void OnClickAnywhere(Action action, bool oneShot = false) => UIEventSystem.UIEvents.OnClickAnywhere(action, oneShot);

        /// <summary>Disables the back button, preventing back navigation.</summary>
        public static void DisableBackButton() => UIBackButton.DisableBackButton();

        /// <summary>Enables the back button, allowing back navigation.</summary>
        public static void EnableBackButton() => UIBackButton.EnableBackButton();

        //// RADIO SYSTEM METHODS ////

        /// <summary>Plays an audio clip through the ComplexRadio system.</summary>
        public static void PlayAudio(string audioName, params IAudioPlayingSettings[] settings) => ComplexRadio.PlayAudio(audioName, settings);

        /// <summary>Plays an audio clip if it is not already playing, using the ComplexRadio system.</summary>
        public static void PlayAudioIfNotPlaying(string audioName, params IAudioPlayingSettings[] settings) => ComplexRadio.PlayAudioIfNotPlaying(audioName, settings);

        /// <summary>Stops an audio clip with optional fade-out behavior.</summary>
        public static void StopAudio(string audioName, bool fadeOut, float fadeTime) => ComplexRadio.StopAudio(audioName, fadeOut, fadeTime);

        /// <summary>Creates a listener for an event by string key, invoking an action once or repeatedly.</summary>
        public static Listener Listener(string s, Action a, bool oneShot = false, GameObject context = null) => new(s, a, oneShot, context);

        /// <summary>Creates a listener for an event with parameters, using object array.</summary>
        public static Listener Listener(string s, Action<object[]> a, bool oneShot = false, GameObject context = null) => new(s, a, oneShot, context);

        /// <summary>Creates a live (non-instance) variable provider using a key and value-fetching function. Useful for values or objects that might change later. Example: Health, XP, AbilityStatus, Money, etc...</summary>
        public static LiveKey LiveKey(string s, Func<object> v, GameObject context = null) => new(s, v, context);

        /// <summary>Creates a dead (instance) variable provider using a key and a value. Useful to remember instanced values that do not follow origin. Example: PlayerLastPosition, PlayerTransformObject, LastRespawnPoint, etc...</summary>
        public static DeadKey DeadKey(string s, object v, GameObject context = null) => new(s, v, context);

        /// <summary>Sends a basic event through the SimpleRadio system.</summary>
        public static void Send(string s) => SimpleRadio.SendEvent(s);

        /// <summary>Sends an event with parameters through the SimpleRadio system.</summary>
        public static void Send(string s, params object[] args) => SimpleRadio.SendEvent(s, args);

        /// <summary>Receives a live value of a specified type from the LiveKey system.</summary>
        public static T GetLiveValue<T>(string s) => SimpleRadio.ReceiveLiveKeyValue<T>(s);

        /// <summary>Receives a dead value of a specified type from the DeadKey system.</summary>
        public static T GetDeadValue<T>(string s) => SimpleRadio.ReceiveDeadKeyValue<T>(s);

        /// <summary>Receives a list of live values of a specified type from the LiveKey system.</summary>
        public static List<T> GetLiveValues<T>(string s) where T : UnityEngine.Object => SimpleRadio.ReceiveLiveKeyValue<List<UnityEngine.Object>>(s).OfType<T>().ToList();

        /// <summary>Receives a list of dead values of a specified type from the DeadKey system.</summary>
        public static List<T> GetDeadValues<T>(string s) where T : UnityEngine.Object => SimpleRadio.ReceiveDeadKeyValue<List<UnityEngine.Object>>(s).OfType<T>().ToList();

        /// <summary>Checks if a live key exists on the radio event bus.</summary>
        public static bool LiveKeyExists(string s) => SimpleRadio.DoesLiveKeyExist(s);

        /// <summary>Checks if a dead key exists on the radio event bus.</summary>
        public static bool DeadKeyExists(string s) => SimpleRadio.DoesDeadKeyExist(s);

        /// <summary>Retrieves a resonance channel by name for complex radio setup.</summary>
        public static ResonanceChannel Channel(string channelName) => ComplexRadio.Channel(channelName);

        /// <summary>Checks if a resonance channel is currently live or initialized.</summary>
        public static bool IsChannelLive(string channelName) => ComplexRadio.IsChannelLive(channelName);

        //// UTILITY METHODS ////

        /// <summary>Executes an action once a condition becomes true. Returns the tween sequence.</summary>
        public static Tween DoWhen(Func<bool> c, Action a) => SSUtility.DoWhen(c, a);

        /// <summary>Continuously checks a condition and performs one of two actions depending on the result.</summary>
        public static Tween DoWhenever(Func<bool> c, Action a, Action alt = null) => SSUtility.DoWhenever(c, a, alt);

        /// <summary>Delays an action by a specific time duration. Optionally unscaled.</summary>
        public static Tween DoIn(float time, Action methodToSequence, bool unscaled = true) => SSUtility.DoIn(time, methodToSequence, unscaled);

        /// <summary>Repeats an action while a condition is true, with delay and optional lock step.</summary>
        public static Tween DoWhile(Func<bool> condition, Action action, float waitTimeAfterLock, Func<bool> locker, bool debugSteps = false) => SSUtility.DoWhile(condition, action, waitTimeAfterLock, locker, debugSteps);

        /// <summary>Performs an action repeatedly until a condition becomes true.</summary>
        public static Tween DoUntil(Func<bool> condition, Action action) => SSUtility.DoUntil(condition, action);

        /// <summary>Loops an action for a given duration and interval.</summary>
        public static Tween DoLoop(float duration, float frequency, Action methodToLoop) => SSUtility.DoEveryIntervalFor(duration, frequency, methodToLoop);

        /// <summary>Calls an action at regular intervals indefinitely. Optionally unscaled time.</summary>
        public static Tween DoEveryInterval(float frequency, Action callback, bool unscaled = true) => SSUtility.DoEveryInterval(frequency, callback, unscaled);

        /// <summary>Executes an action every frame using frame time.</summary>
        public static Tween DoFrameUpdate(Action callback, bool unscaled = false) => SSUtility.DoFrameUpdate(callback, unscaled);

        /// <summary>Repeats an action at random intervals between min and max values.</summary>
        public static Tween DoRandomly(float min, float max, Action methodToLoop) => SSUtility.DoRandomly(min, max, methodToLoop);

        /// <summary>Loops an action for a duration at fixed intervals. Optionally unscaled time.</summary>
        public static Tween DoEveryIntervalFor(float duration, float frequency, Action methodToLoop, bool unscaled = true) => SSUtility.DoEveryIntervalFor(duration, frequency, methodToLoop, unscaled);

        /// <summary>Retries an action multiple times until successful or a max retry count is reached.</summary>
        public static Tween DoRetries(Func<bool> tryAction, int maxRetries, float delayBetweenAttempts, Action onSuccess = null, Action onFailure = null) => SSUtility.DoRetries(tryAction, maxRetries, delayBetweenAttempts, onSuccess, onFailure);

        /// <summary>Executes an action in the next frame. This is a convenience wrapper around DoAfterFrames(1, action).</summary>
        public static Tween DoNext(Action action) => SSUtility.DoNext(action);

        /// <summary>Executes an action after a certain number of frames.</summary>
        public static Tween DoAfterFrames(int frames, Action action) => SSUtility.DoAfterFrames(frames, action);

        /// <summary>Attempts an action only if it is not currently on cooldown (unique key-based).</summary>
        public static void DoActionWithCooldown(Action action, float t, string key, bool unscaled = false) => SSUtility.DoActionWithCooldown(action, t, key, unscaled);

        /// <summary>Returns true with a given chance (0 to 1 float). Like throwing a probability dice.</summary>
        public static bool ThrowDice(float chance01) => SSUtility.ThrowDice(chance01);

        /// <summary>Checks whether a cooldown is active for a given key and time duration.</summary>
        public static bool IsOnCooldown(float t, string k) => SSUtility.IsOnCooldown(t, k);

        /// <summary>
        /// Checks if a cooldown gate is open for a specific key and cooldown time. This method is different from `IsOnCooldown` in that it will also set the cooldown if the gate is open.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cooldownTime"></param>
        /// <param name="unscaled"></param>
        /// <returns></returns>
        public static bool CooldownGate(string key, float cooldownTime, bool unscaled = false) => SSUtility.CooldownGate(key, cooldownTime, unscaled);

        /// <summary>
        /// Kills a cooling cooldown gate. Basically resets any active gate on that key.
        /// </summary>
        /// <param name="v"></param>
        public static void KillCooldownGate(string v) => SSUtility.KillCooldownGate(v);

        /// <summary>
        /// Temporarily pauses execution flow by returning true during a delay period, then returning false once complete.
        /// This is like a reversed CooldownGate - it starts closed (returns true) and then opens (returns false).
        /// Use this in a method that needs to wait before executing its logic.
        /// </summary>
        /// <param name="delayTime">Duration to pause execution in seconds</param>
        /// <param name="key">Unique identifier for this hold operation</param>
        /// <param name="unscaled">Use unscaled time (default: false)</param>
        /// <returns>True if still waiting, false if delay has passed</returns>
        public static bool HoldOn(float delayTime, string key, bool unscaled = false) => SSUtility.HoldOn(delayTime, key, unscaled);

        /// <summary>
        /// Kills an active HoldOn delay, allowing execution to proceed immediately on the next call.
        /// </summary>
        /// <param name="key">The unique key identifying the HoldOn to kill</param>
        public static void KillHoldOn(string key) => SSUtility.KillHoldOn(key);

        /// <summary>
        /// Begins a new promise flow for executing actions step by step. Progresses manually through calling of a `Step` method.
        /// </summary>
        /// <returns></returns>
        public static Promise.PromiseFlow BeginPromiseFlow() => Promise.PromiseFlow.Begin();

        /// <summary>
        /// Begins a new time-based promise that automatically progresses through steps after a fixed delay.
        /// </summary>
        /// <returns></returns>
        public static Promise.TimePromise BeginTimePromise(float time) => Promise.TimePromise.Begin(time);

        //// GAME SYSTEM METHODS: Loading Screen ////

        /// <summary>Triggers a scene load using the loading screen system.</summary>
        public static void LoadSceneAsync(string s) => LoadingScreen.LoadSceneAsync(s);

        /// <summary>Prepares loading screen instances configured for the project.</summary>
        public static void PrepareLoadingScreen(UIView loadingScreenPrefab = null) => LoadingScreen.PrepareLoadingScreen(loadingScreenPrefab);

        /// <summary>Cleans up cached loading screen references.</summary>
        public static void CleanLoadingScreens() => LoadingScreen.Clean();

        /// <summary>Checks whether a loading screen is currently active.</summary>
        public static bool OnLoadingScreen => LoadingScreen.OnALoadingScreen;

        //// GAME SYSTEM METHODS: Tutorial System ////

        /// <summary>
        /// Highlights a UI element by its name with specified properties.
        /// </summary>
        /// <param name="elementName"></param>
        /// <param name="elementHighlightProperties"></param>
        [Obsolete("Tutorial System is deprecated and will be removed in a future release.")]
        public static void HighlightElement(string elementName, ElementHighlightProperties elementHighlightProperties) => TutorialMethods.HighlightElement(elementName, elementHighlightProperties);

        /// <summary>Resets all tutorials and clears cached tutorial state.</summary>
        [Obsolete("Tutorial System is deprecated and will be removed in a future release.")]
        public static void ResetTutorials() => TutorialManager.ResetAll();

        //// GAME SYSTEM METHODS: Audio Layering ////

        /// <summary>
        /// Retrieves a Layer by its ID from the AudioLayering system. This is set in the audio layering asset assigned in the Signalia settings.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Layer AudioLayer(string id) => AudioLayering.Layer(id);

        /// <summary>Clears audio layering state and disposes of managed layers.</summary>
        public static void CleanseAudioLayers() => AudioLayeringManager.Cleanse();

        //// GAME SYSTEM METHODS: Pooling ////

        /// <summary>Retrieves a pooled GameObject with optional lifetime and enable state.</summary>
        public static GameObject PoolingGet(GameObject sourcePrefab, float lifetime = -1f, bool enabled = true) => Pooling.Get(sourcePrefab, lifetime, enabled);

        /// <summary>Retrieves a batch of pooled GameObjects.</summary>
        public static List<GameObject> PoolingGet(GameObject sourcePrefab, int count, float lifetime = -1f, bool enabled = true) => Pooling.Get(sourcePrefab, count, lifetime, enabled);

        /// <summary>Retrieves a pooled GameObject while caching requested component types.</summary>
        public static (GameObject gameObject, Dictionary<Type, Component> compCache) PoolingGet(GameObject sourcePrefab, float lifetime, bool enabled, params (Type type, bool fromChildren)[] types) => Pooling.Get(sourcePrefab, lifetime, enabled, types);

        /// <summary>Retrieves a batch of pooled GameObjects with cached component dictionaries.</summary>
        public static List<(GameObject gameObject, Dictionary<Type, Component> compCache)> PoolingGet(GameObject sourcePrefab, int count, float lifetime, bool enabled, params (Type type, bool fromChildren)[] types) => Pooling.Get(sourcePrefab, count, lifetime, enabled, types);

        /// <summary>Warms up a pool with the specified number of inactive instances.</summary>
        public static void PoolingWarmup(GameObject sourcePrefab, int count) => Pooling.Warmup(sourcePrefab, count);

        /// <summary>Checks if the specified number of instances are currently active for the given pooled prefab.</summary>
        public static bool PoolingActiveCount(GameObject sourcePrefab, int count = 1) => Pooling.ActiveCount(sourcePrefab, count);

        /// <summary>Attempts to retrieve a cached component instance from pooled cache data.</summary>
        public static T PoolingTryGetCached<T>(Dictionary<Type, Component> cache) where T : Component => Pooling.TryGetCached<T>(cache);

        /// <summary>Clears all cached pooling data.</summary>
        public static void PoolingClear() => Pooling.ClearPools();

        //// GAME SYSTEM METHODS: Resource Caching ////

        /// <summary>Initializes resource caching databases.</summary>
        public static void InitializeResourceCaching() => ResourceCachingManager.Initialize();

        /// <summary>
        /// Gets a cached resource by its key from the Resource Caching system.
        /// </summary>
        /// <typeparam name="T">The type of resource to retrieve</typeparam>
        /// <param name="key">The key identifying the resource</param>
        /// <returns>The cached resource or null if not found</returns>
        public static T GetResource<T>(string key) where T : Object => ResourceCachingManager.GetResource<T>(key);

        /// <summary>
        /// Checks if a resource exists for the given key in the Resource Caching system.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the resource exists</returns>
        public static bool HasResource(string key) => ResourceCachingManager.HasResource(key);

        /// <summary>Clears all cached resource entries.</summary>
        public static void ClearResourceCache() => ResourceCachingManager.Clear();

        /// <summary>
        /// Gets all available resource keys from the Resource Caching system.
        /// </summary>
        /// <returns>Array of all resource keys</returns>
        public static string[] GetAllResourceKeys() => ResourceCachingManager.GetAllKeys();

        /// <summary>
        /// Gets the number of cached resources in the Resource Caching system.
        /// </summary>
        /// <returns>Number of cached resources</returns>
        public static int GetResourceCacheSize() => ResourceCachingManager.GetCacheSize();

        //// GAME SYSTEM METHODS: Save System ////

        /// <summary>Initializes the save cache for a specific file.</summary>
        public static void InitializeSaveCache(string fileName) => GameSaving.InitializeLoadedCache(fileName);

        /// <summary>Clears the save cache for a specific file.</summary>
        public static void ClearSaveCache(string fileName) => GameSaving.ClearCache(fileName);

        /// <summary>Clears all cached save files.</summary>
        public static void ClearSaveCaches() => GameSaving.ClearAllCaches();

        /// <summary>Queues a save preference using the configured settings file.</summary>
        public static void SavePreference(string key, object value) => GameSaving.SavePreference(key, value);

        /// <summary>Saves a key/value pair to a specific save file.</summary>
        public static void SaveData(string key, object value, string fileName) => GameSaving.Save(key, value, fileName);

        /// <summary>Forces all pending save operations to complete.</summary>
        public static Task ForceSaveAllAsync() => GameSaving.ForceSaveAllAsync();

        /// <summary>Checks whether a particular file has pending save data.</summary>
        public static bool HasPendingSaves(string fileName) => GameSaving.HasPendingSaves(fileName);

        /// <summary>Checks if any pending saves exist across all files.</summary>
        public static bool HasAnyPendingSaves() => GameSaving.HasPendingSaves();

        /// <summary>Retrieves the count of pending save files.</summary>
        public static int GetPendingSaveCount() => GameSaving.GetPendingSaveCount();

        /// <summary>Executes save system shutdown processes.</summary>
        public static void ShutdownSaveSystem() => GameSaving.OnApplicationQuit();

        //// GAME SYSTEM METHODS: Achievement System ////

        /// <summary>Unlocks an achievement by ID. Returns true only if it was newly unlocked.</summary>
        public static bool UnlockAchievement(string achievementId) => AchievementManager.Unlock(achievementId);

        /// <summary>Unlocks an achievement by reference. Returns true only if it was newly unlocked.</summary>
        public static bool UnlockAchievement(AchievementSO achievement) => AchievementManager.Unlock(achievement);

        /// <summary>Locks (relocks) an achievement by ID. Returns true only if it was previously unlocked and is now locked.</summary>
        public static bool LockAchievement(string achievementId) => AchievementManager.Lock(achievementId);

        /// <summary>Locks (relocks) an achievement by reference. Returns true only if it was previously unlocked and is now locked.</summary>
        public static bool LockAchievement(AchievementSO achievement) => AchievementManager.Lock(achievement);

        /// <summary>Checks whether an achievement is unlocked.</summary>
        public static bool IsAchievementUnlocked(string achievementId) => AchievementManager.IsUnlocked(achievementId);

        /// <summary>Gets a referenced achievement definition by ID.</summary>
        public static AchievementSO GetAchievement(string achievementId) => AchievementManager.Get(achievementId);

        /// <summary>Gets all unlocked achievement definitions.</summary>
        public static List<AchievementSO> GetUnlockedAchievements() => AchievementManager.GetUnlockedAchievements();

        /// <summary>Clears the achievement runtime cache.</summary>
        public static void ResetAchievementCache() => AchievementManager.ResetCache();

        //// GAME SYSTEM METHODS: Inventory System ////
        /// <summary>
        /// Initializes the inventory system runtime cache.
        /// This should be called once at game startup to optimize I/O operations.
        /// </summary>
        public static void InitializeInventorySystem() => InventoryDefinition.InitializeCache();

        /// <summary>
        /// Clears the inventory system cache during gameplay cleanup.
        /// Use this when changing scenes or resetting game state.
        /// </summary>
        public static void ClearInventoryCache() => InventoryDefinition.ClearCache();
        
        //// GAME SYSTEM METHODS: Dialogue System ////
        public static void InitializeDialogueSystem() => DialogueManager.Initialize();

        public static void ClearDialogueSystem() => DialogueManager.Cleanup();

        /// <summary>Loads a string value from the save system.</summary>
        public static string LoadString(string key, string fileName) => GameSaving.StringTypeLoad(key, fileName);

        /// <summary>Loads a preference value with a default fallback.</summary>
        public static T LoadPreference<T>(string key, T defaultValue) => GameSaving.LoadPreference(key, defaultValue);

        /// <summary>Loads a typed value from a save file.</summary>
        public static T LoadData<T>(string key, string fileName) => GameSaving.Load<T>(key, fileName);

        /// <summary>Loads a typed value with a default fallback.</summary>
        public static T LoadData<T>(string key, string fileName, T defaultValue) => GameSaving.Load(key, fileName, defaultValue);

        /// <summary>Loads all key/value pairs stored in a save file.</summary>
        public static Dictionary<string, string> LoadAllSaveData(string fileName) => GameSaving.LoadAllData(fileName);

        /// <summary>Deletes a save file.</summary>
        public static void DeleteSaveFile(string fileName) => GameSaving.DeleteFile(fileName);

        /// <summary>Deletes a key from a save file.</summary>
        public static void DeleteSaveKey(string key, string fileName) => GameSaving.DeleteKey(key, fileName);

        /// <summary>Checks whether a key exists in a save file.</summary>
        public static bool SaveKeyExists(string key, string fileName) => GameSaving.KeyExists(key, fileName);

        /// <summary>Checks whether a save file exists.</summary>
        public static bool SaveFileExists(string fileName) => GameSaving.FileExists(fileName);

        /// <summary>Wipes all save data stored by the save system.</summary>
        public static void WipeAllSaveData() => GameSaving.WipeAllData();

        //// GAME SYSTEM METHODS: Currency System ////

        /// <summary>
        /// Gets a currency by its name from the runtime cache.
        /// If not cached, loads from disk once and caches it.
        /// </summary>
        /// <param name="currencyName">The name of the currency to retrieve</param>
        /// <returns>The currency struct with current value and methods</returns>
        public static CMN_Currencies.CustomCurrency GetCurrency(string currencyName) => CMN_Currencies.GetCurrency(currencyName);

        /// <summary>
        /// Modifies a currency's value in the runtime cache.
        /// This is a lightweight operation that doesn't touch disk unless autoSave is true.
        /// </summary>
        /// <param name="currencyName">The name of the currency to modify</param>
        /// <param name="amount">Amount to add (use negative for subtraction)</param>
        /// <param name="autoSave">If true, saves to disk after modification (default: false)</param>
        /// <param name="notify">If true, sends an event to listeners (default: true)</param>
        /// <returns>The modified currency</returns>
        public static CMN_Currencies.CustomCurrency ModifyCurrency(string currencyName, float amount, bool autoSave = false, bool notify = true) 
            => CMN_Currencies.ModifyCurrency(currencyName, amount, autoSave, notify);

        /// <summary>
        /// Saves a specific currency from the cache to disk.
        /// </summary>
        /// <param name="currencyName">The currency name to save</param>
        public static void SaveCurrency(string currencyName) => CMN_Currencies.SaveCurrency(currencyName);

        /// <summary>
        /// Saves all cached currencies to disk.
        /// Call this at save points (e.g., checkpoints, level transitions, game pause).
        /// </summary>
        public static void SaveAllCurrencies() => CMN_Currencies.SaveAllCurrencies();

        //// GAME SYSTEM METHODS: Localization System ////

        /// <summary>
        /// Gets a localized string by its key for the current language.
        /// This is the primary method to retrieve localized text in your code.
        /// </summary>
        /// <param name="key">The localization key or source string (if hybrid key is enabled)</param>
        /// <returns>The localized string for the current language</returns>
        public static string GetLocalizedString(string key) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.ReadKey(key);

        /// <summary>
        /// Gets a localized string by its key for a specific language.
        /// This method applies formatting (e.g., Arabic shaping) automatically.
        /// </summary>
        /// <param name="key">The localization key or source string (if hybrid key is enabled)</param>
        /// <param name="languageCode">The language code to retrieve (e.g., "en", "es", "fr")</param>
        /// <param name="paragraphStyle">Optional paragraph style to filter by (e.g., "Header", "Description")</param>
        /// <returns>The localized string for the specified language</returns>
        public static string GetLocalizedString(string key, string languageCode, string paragraphStyle = "") => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.ReadKey(key, languageCode, paragraphStyle);

        /// <summary>
        /// Gets a raw localized string by its key for the current language without applying formatting.
        /// Use this when you need to apply string formatting (e.g., string.Format) before language-specific formatting.
        /// </summary>
        /// <param name="key">The localization key or source string (if hybrid key is enabled)</param>
        /// <returns>The raw localized string without formatting, or the key itself if not found</returns>
        public static string GetRawLocalizedString(string key) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.ReadKeyRaw(key);

        /// <summary>
        /// Gets a raw localized string by its key for a specific language without applying formatting.
        /// Use this when you need to apply string formatting (e.g., string.Format) before language-specific formatting.
        /// </summary>
        /// <param name="key">The localization key or source string (if hybrid key is enabled)</param>
        /// <param name="languageCode">The language code to retrieve (e.g., "en", "es", "fr")</param>
        /// <returns>The raw localized string without formatting, or fallback value if not found</returns>
        public static string GetRawLocalizedString(string key, string languageCode) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.ReadKeyRaw(key, languageCode);

        /// <summary>
        /// Gets the TextStyle asset for a specific language code.
        /// TextStyles define font and formatting settings for different languages.
        /// </summary>
        /// <param name="languageCode">The language code (e.g., "en", "es", "fr")</param>
        /// <param name="paragraphStyle">Optional paragraph style to filter by (e.g., "Header", "Description"). Leave empty or null for default style.</param>
        /// <returns>The TextStyle asset for the language, or null if not found</returns>
        public static AHAKuo.Signalia.GameSystems.Localization.Internal.TextStyle GetTextStyle(string languageCode, string paragraphStyle = "") => AHAKuo.Signalia.GameSystems.Localization.Internal.LocalizationRuntime.GetTextStyle(languageCode, paragraphStyle);

        /// <summary>
        /// Changes the current language and optionally saves the preference.
        /// This will trigger a language changed event that UI elements can respond to.
        /// </summary>
        /// <param name="code">The new language code (e.g., "en", "es", "fr")</param>
        /// <param name="save">Whether to save this language preference for future sessions</param>
        public static void ChangeLanguage(string code, bool save = true) => AHAKuo.Signalia.GameSystems.Localization.Internal.LocalizationRuntime.ChangeLanguage(code, save);

        /// <summary>
        /// Gets the current active language code.
        /// </summary>
        /// <returns>The current language code</returns>
        public static string GetCurrentLanguage() => AHAKuo.Signalia.GameSystems.Localization.Internal.LocalizationRuntime.CurrentLanguageCode;

        /// <summary>
        /// Fires the language changed event through the Radio system.
        /// Use this to manually trigger UI updates after batch language operations.
        /// </summary>
        public static void TriggerLanguageChange() => AHAKuo.Signalia.GameSystems.Localization.Internal.LocalizationRuntime.FireLanguageChangedEvent();

        /// <summary>
        /// Initializes the localization system from a LocBook asset.
        /// This should be called once at game startup before using any localization features.
        /// </summary>
        /// <param name="locBook">The LocBook asset to load localization data from</param>
        //public static void InitializeLocalization(AHAKuo.Signalia.GameSystems.Localization.Internal.LocBook locBook) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.Initialize(locBook);

        /// <summary>
        /// Initializes the localization system from the LocBook configured in Signalia settings.
        /// This is the recommended initialization method as it uses the configured LocBook.
        /// </summary>
        public static void InitializeLocalization()
        {
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            if (config != null && config.LocalizationSystem.LocBooks != null && config.LocalizationSystem.LocBooks.Length > 0)
            {
                AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.Initialize(config.LocalizationSystem.LocBooks);
            }
            else
            {
                Debug.LogWarning("[Signalia Localization] No LocBooks configured in Signalia settings.");
            }
        }

        /// <summary>
        /// Checks if a localization key exists in the loaded data.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists</returns>
        public static bool HasLocalizationKey(string key) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.HasKey(key);

        /// <summary>
        /// Gets all available language codes from the loaded localization data.
        /// </summary>
        /// <returns>List of language codes</returns>
        public static System.Collections.Generic.List<string> GetAvailableLanguages() => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.GetAvailableLanguages();
        
        /// <summary>
        /// Gets a localized audio clip by its key for the current language.
        /// Audio localization is managed exclusively through Unity in the LocBook asset.
        /// </summary>
        /// <param name="key">The audio key</param>
        /// <returns>The localized audio clip, or null if not found</returns>
        public static AudioClip GetLocalizedAudioClip(string key) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.ReadAudioClip(key);
        
        /// <summary>
        /// Gets a localized audio clip by its key for a specific language.
        /// Audio localization is managed exclusively through Unity in the LocBook asset.
        /// </summary>
        /// <param name="key">The audio key</param>
        /// <param name="languageCode">The language code (e.g., "en", "es", "fr")</param>
        /// <returns>The localized audio clip, or null if not found</returns>
        public static AudioClip GetLocalizedAudioClip(string key, string languageCode) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.ReadAudioClip(key, languageCode);
        
        /// <summary>
        /// Gets a localized sprite by its key for the current language.
        /// Sprite localization is managed exclusively through Unity in the LocBook asset.
        /// </summary>
        /// <param name="key">The sprite key</param>
        /// <returns>The localized sprite, or null if not found</returns>
        public static Sprite GetLocalizedSprite(string key) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.ReadSprite(key);
        
        /// <summary>
        /// Gets a localized sprite by its key for a specific language.
        /// Sprite localization is managed exclusively through Unity in the LocBook asset.
        /// </summary>
        /// <param name="key">The sprite key</param>
        /// <param name="languageCode">The language code (e.g., "en", "es", "fr")</param>
        /// <returns>The localized sprite, or null if not found</returns>
        public static Sprite GetLocalizedSprite(string key, string languageCode) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.ReadSprite(key, languageCode);
        
        /// <summary>
        /// Gets a localized asset by its key for the current language.
        /// Asset localization is managed exclusively through Unity in the LocBook asset.
        /// </summary>
        /// <typeparam name="T">The type of asset to retrieve (must inherit from UnityEngine.Object)</typeparam>
        /// <param name="key">The asset key</param>
        /// <returns>The localized asset, or null if not found</returns>
        public static T GetLocalizedAsset<T>(string key) where T : UnityEngine.Object => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.ReadAsset<T>(key);
        
        /// <summary>
        /// Gets a localized asset by its key for a specific language.
        /// Asset localization is managed exclusively through Unity in the LocBook asset.
        /// </summary>
        /// <typeparam name="T">The type of asset to retrieve (must inherit from UnityEngine.Object)</typeparam>
        /// <param name="key">The asset key</param>
        /// <param name="languageCode">The language code (e.g., "en", "es", "fr")</param>
        /// <returns>The localized asset, or null if not found</returns>
        public static T GetLocalizedAsset<T>(string key, string languageCode) where T : UnityEngine.Object => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.ReadAsset<T>(key, languageCode);
        
        /// <summary>
        /// Checks if a localized audio clip exists for a given key.
        /// </summary>
        /// <param name="key">The audio key to check</param>
        /// <returns>True if the audio key exists</returns>
        public static bool HasLocalizedAudioClip(string key) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.HasAudioKey(key);
        
        /// <summary>
        /// Checks if a localized sprite exists for a given key.
        /// </summary>
        /// <param name="key">The sprite key to check</param>
        /// <returns>True if the sprite key exists</returns>
        public static bool HasLocalizedSprite(string key) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.HasSpriteKey(key);
        
        /// <summary>
        /// Checks if a localized asset exists for a given key.
        /// </summary>
        /// <param name="key">The asset key to check</param>
        /// <returns>True if the asset key exists</returns>
        public static bool HasLocalizedAsset(string key) => AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.HasAssetKey(key);

        //// SIGNALIA TIME SYSTEM METHODS ////

        /// <summary>Gets the current time modifier aggregate (product of all modifiers).</summary>
        public static float TimeAggregate => SignaliaTime.ModifiersAggregate;

        /// <summary>Gets the scaled delta time (deltaTime * modifiersAggregate).</summary>
        public static float ScaledDeltaTime => SignaliaTime.ScaledDeltaTime;

        /// <summary>Gets the total scaled time elapsed since SignaliaTime was initialized.</summary>
        public static float TotalScaledTime => SignaliaTime.TotalScaledTime;

        /// <summary>Checks if time is currently paused.</summary>
        public static bool IsTimePaused => SignaliaTime.IsPaused;

        /// <summary>Checks if time is currently slowed (but not paused).</summary>
        public static bool IsTimeSlowed => SignaliaTime.IsSlowed;

        /// <summary>Adds a time modifier to the system.</summary>
        /// <param name="modifier">The time modifier to add</param>
        /// <returns>True if added, false if a modifier with the same ID already exists</returns>
        public static bool AddTimeModifier(TimeModifier modifier) => SignaliaTime.AddModifier(modifier);

        /// <summary>Adds or updates a time modifier. If a modifier with the same ID exists, it will be updated.</summary>
        /// <param name="modifier">The time modifier to add or update</param>
        public static void SetTimeModifier(TimeModifier modifier) => SignaliaTime.SetModifier(modifier);

        /// <summary>Removes a time modifier by its ID.</summary>
        /// <param name="id">The ID of the modifier to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public static bool RemoveTimeModifier(string id) => SignaliaTime.RemoveModifier(id);

        /// <summary>Removes all time modifiers from a specific source.</summary>
        /// <param name="source">The source to remove modifiers from</param>
        /// <returns>Number of modifiers removed</returns>
        public static int RemoveTimeModifiersBySource(string source) => SignaliaTime.RemoveModifiersBySource(source);

        /// <summary>Checks if a time modifier with the given ID exists.</summary>
        /// <param name="id">The ID to check</param>
        /// <returns>True if exists</returns>
        public static bool HasTimeModifier(string id) => SignaliaTime.HasModifier(id);

        /// <summary>Pauses time with the specified pause ID.</summary>
        /// <param name="pauseId">Unique ID for this pause (defaults to "GlobalPause")</param>
        /// <param name="source">Optional source identifier for debugging</param>
        public static void PauseTime(string pauseId = "GlobalPause", string source = null) => SignaliaTime.Pause(pauseId, source);

        /// <summary>Resumes time by removing a specific pause modifier.</summary>
        /// <param name="pauseId">The ID of the pause to remove (defaults to "GlobalPause")</param>
        public static void ResumeTime(string pauseId = "GlobalPause") => SignaliaTime.Resume(pauseId);

        /// <summary>Resumes time by clearing all modifiers.</summary>
        public static void ResumeTimeAll() => SignaliaTime.ResumeAll();

        /// <summary>Gets the number of active time modifiers.</summary>
        public static int TimeModifierCount => SignaliaTime.ModifierCount;

        //// HAPTICS SYSTEM METHODS ////

        /// <summary>Triggers haptic feedback with the specified settings.</summary>
        /// <param name="settings">Haptic settings to apply</param>
        public static void TriggerHaptic(HapticSettings settings) => HapticsManager.TriggerHaptic(settings);

        /// <summary>Triggers haptic feedback with individual parameters.</summary>
        /// <param name="type">Type of haptic feedback</param>
        /// <param name="intensity">Intensity of the haptic (0-1)</param>
        /// <param name="duration">Duration of the haptic in seconds</param>
        public static void TriggerHaptic(HapticType type, float intensity = 1f, float duration = 0.1f) => HapticsManager.TriggerHaptic(type, intensity, duration);

        /// <summary>Triggers a preset haptic type.</summary>
        /// <param name="type">Preset haptic type</param>
        public static void TriggerHapticPreset(HapticType type) => HapticsManager.TriggerHapticPreset(type);

        /// <summary>Stops all haptic feedback.</summary>
        public static void StopAllHaptics() => HapticsManager.StopAllHaptics();

        /// <summary>Gets information about connected haptic devices.</summary>
        /// <returns>String containing device information</returns>
        public static string GetHapticDeviceInfo() => HapticsManager.GetHapticDeviceInfo();

        /// <summary>Checks if haptics are currently enabled.</summary>
        /// <returns>True if haptics are enabled</returns>
        public static bool IsHapticsEnabled() => HapticsManager.Enabled;

        /// <summary>Enables or disables the global haptics system.</summary>
        /// <param name="enabled">Whether haptics should be enabled</param>
        public static void SetHapticsEnabled(bool enabled) => RuntimeValues.RadioConfig.HapticsActive = enabled;

        //// GAME SYSTEM METHODS: Notification System ////

        /// <summary>
        /// Shows a notification on a SystemMessage component by name.
        /// </summary>
        /// <param name="systemMessageName">The name of the SystemMessage component</param>
        /// <param name="notificationString">The message text to display</param>
        public static void ShowNotification(string systemMessageName, string notificationString) => NotificationMethods.ShowNotification(systemMessageName, notificationString);

        /// <summary>
        /// Shows a burner notification at a BurnerSpot by name.
        /// </summary>
        /// <param name="burnerSpotName">The name of the BurnerSpot component</param>
        /// <param name="message">Optional message text to display on the burner</param>
        public static void ShowBurner(string burnerSpotName, string message = null) => NotificationMethods.ShowBurner(burnerSpotName, message);
    }
}
