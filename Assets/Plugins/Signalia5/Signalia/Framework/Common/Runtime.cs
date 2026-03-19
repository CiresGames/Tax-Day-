using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.Utilities.SIGInput;
using System;

namespace AHAKuo.Signalia.Framework
{
    public static class RuntimeValues
    {
        public static SignaliaConfigAsset Config => ConfigReader.GetConfig();

        /// <summary>
        /// Called by the config booter. Resets all runtime values to default.
        /// Can be called at any time to reset the runtime values, careful as it can break the system if called at the wrong time.
        /// </summary>
        public static void ResetRuntimeValues()
        {
            UIConfig.Reset();
            TrackedValues.Reset();
            InputDelegation.ResetInputs();
            SignaliaInputBridge.Reset();
            SignaliaInputBridge.RefreshActionMaps();
        }

        public static class TrackedValues
        {
            public static void Reset()
            {
                animatingViews.Clear();
                animatingAnimatables.Clear();
                travelHistory.Clear();
                viewRegistry.Clear();
                buttonRegistry.Clear();
                elementRegistry.Clear();
                fragments.Clear();
                finiteMovers.Clear();
            }

            /// <summary>
            /// Add a view to the list of animating views. And then remove it after the animation is done. Used for tracking views that are currently moving.
            /// </summary>
            /// <param name="uIView"></param>
            /// <param name="AnimationAsset"></param>
            public static void LogMovingViewLength(UIView uIView, UIAnimationAsset AnimationAsset)
            {
                if (Config == null) { return; }

                Watchman.Watch();

                animatingViews.Add(uIView);
                DOVirtual.DelayedCall(AnimationAsset.FullEndTime(), () => animatingViews.Remove(uIView), AnimationAsset.UnscaledTime);
            }

            /// <summary>
            /// Add an animatable to the list of animating animatables. And then remove it after the animation is done. Used for tracking animatables that are currently moving.
            /// </summary>
            /// <param name="uIView"></param>
            /// <param name="AnimationAsset"></param>
            public static void LogMovingAnimatableLength(UIAnimatableArrayable animatable, UIAnimationAsset AnimationAsset)
            {
                if (Config == null) { return; }

                Watchman.Watch();

                if (animatable == null || !animatable.BlocksClicks)
                {
                    return;
                }

                animatingAnimatables.Add(animatable);
                DOVirtual.DelayedCall(AnimationAsset.FullEndTime(), () => animatingAnimatables.Remove(animatable), AnimationAsset.UnscaledTime);
            }

            public static void LogTravelHistory(UIView traveler)
            {
                Watchman.Watch();
                travelHistory.Add(traveler);
            }

            public static void LogRemoveTravelHistory(UIView traveler)
            {
                //Watchman.Watch(); it was messing with ondestroy object instantiation, leading to unity screaming about it.
                travelHistory.Remove(traveler);
            }

            public static void LogViewRegistry(UIView view)
            {
                Watchman.Watch();
                viewRegistry.Add(view);

                // log a warning if this view has a name that is not empty and matches another view in the registry.
                if (view.MenuName.HasValue()
                    && viewRegistry.Any(v => v != view && v.MenuName == view.MenuName))
                {
                    Debug.LogWarning($"[Signalia] View with name {view.MenuName} already exists in the registry. This may cause issues.", view);
                }
            }

            public static void LogRemoveViewRegistry(UIView view)
            {
                //Watchman.Watch(); it was messing with ondestroy object instantiation, leading to unity screaming about it.
                viewRegistry.Remove(view);
            }

            public static void LogButtonRegistry(UIButton button)
            {
                Watchman.Watch();
                buttonRegistry.Add(button);

                // log a warning if this button has a name that is not empty and matches another button in the registry.
                if (button.ButtonName.HasValue()
                    && buttonRegistry.Any(b => b != button && b.ButtonName == button.ButtonName))
                {
                    Debug.LogWarning($"[Signalia] Button with name {button.ButtonName} already exists in the registry. This may cause issues.", button);
                }
            }

            public static void LogRemoveButtonRegistry(UIButton button)
            {
                buttonRegistry.Remove(button);
            }

            public static void LogElementRegistry(UIElement element)
            {
                Watchman.Watch();
                elementRegistry.Add(element);

                // log a warning if this element has a name that is not empty and matches another element in the registry.
                if (element.ElementName.HasValue()
                    && elementRegistry.Any(e => e != element && e.ElementName == element.ElementName))
                {
                    Debug.LogWarning($"[Signalia] Element with name {element.ElementName} already exists in the registry. This may cause issues.", element);
                }
            }

            public static void LogRemoveElementRegistry(UIElement element)
            {
                elementRegistry.Remove(element);
            }

            public static V3 GetFragment(string key, RectTransform rectTransform, V3 defaultValue)
            {
                // i made this, and i honestly have no idea how it even works. I just made it, and immediately forgot how it works, it just does, and UI elements all seem to work properly for it. It just works ~ Todd Howard
                if (!fragments.ContainsKey(key))
                {
                    // add it once
                    fragments[key] = new(rectTransform.anchoredPosition, rectTransform.localScale, rectTransform.localRotation.eulerAngles);
                    return defaultValue;
                }

                var f = fragments.GetValueOrDefault(key, default);
                return f;
            }

            public static void LogFiniteMover(S2 s2, float v, bool unscaled, AnimatedUIElement animatedUIElement) // prolly not perfect
            {
                if (finiteMovers.ContainsKey(s2))
                {
                    return;
                }

                finiteMovers[s2] = animatedUIElement;

                // remove it after end time
                SIGS.DoIn(v, () => finiteMovers.Remove(s2), unscaled);
            }

            public static void KillNonLoopingOnObjectTarget(string gameObjectId)
            {
                var keys = finiteMovers.Keys.Where(k => k.s2 == gameObjectId).ToList();
                foreach (var key in keys)
                {
                    finiteMovers[key].animationAsset.StopAnimations();
                    finiteMovers.Remove(key);
                }
            }

            private static readonly List<UIView> animatingViews = new();
            private static readonly List<UIAnimatableArrayable> animatingAnimatables = new();
            private static readonly List<UIView> viewRegistry = new(); // the views that have initialized. << should be reset
            private static readonly List<UIButton> buttonRegistry = new(); // the buttons that have initialized. << should be reset
            private static readonly List<UIElement> elementRegistry = new(); // the UIElements that have initialized. << should be reset
            private static readonly List<UIView> travelHistory = new(); // used for the back button.
            private static readonly Dictionary<string, V3> fragments = new(); // stores the very first instance of an animated element's (position, scale, rotation) for reference. called it fragment... sounds cool.
            private static readonly Dictionary<S2, AnimatedUIElement> finiteMovers = new(); // stores non-looping finite movers by id.

            public static bool AViewIsAnimating => animatingViews.Count > 0;
            public static bool AnAnimatableIsAnimating => animatingAnimatables.Any(a => a != null && a.BlocksClicks);
            public static bool AnyBlockingAnimatablesAnimating => AnAnimatableIsAnimating;
            public static List<UIView> TravelHistory => travelHistory;
            public static List<UIView> ViewRegistry => viewRegistry;
            public static List<UIButton> ButtonRegistry => buttonRegistry;
            public static List<UIElement> ElementRegistry => elementRegistry;
            public static UIView CurrentFocusedView => travelHistory.LastOrDefault();
        }

        public static class RadioConfig
        {
            private static bool _hapticActive = false;

            public static void LoadSettings()
            {
                _hapticActive = SIGS.LoadPreference(Config.HapticsSaveKey, Config.EnableHaptics);
            }

            public static bool HapticsActive
            {
                get => _hapticActive;
                set
                {
                    _hapticActive = value;
                    if (Config.AutoSaveHapticSetting)
                    {
                        SIGS.SavePreference(Config.HapticsSaveKey, value);
                    }
                }
            }
        }

        public static class UIConfig
        {
            public static void Reset()
            {
                ButtonsOnCooldown = false;
                ButtonsDisabled = false;
            }

            #region Buttons
            public static bool ButtonsCanBeClicked =>
                (!ButtonsOnCooldown) &&
                !ViewAnimationLock() &&
                !VAnimatableAnimationLock() &&
                !ButtonsDisabled;

            private static bool ButtonsOnCooldown;

            private static bool ButtonsDisabled;

            public static void SubscribeToClickAnywhere(Action action, bool oneShot = false)
            {
                if (Config == null) { return; }

                Watchman.Watch();

                if (action == null) { return; }

                string key = action.Method.Name + (oneShot ? "_OneShot" : "_Persistent");
                if (OnClickAnywhere.ContainsKey(key))
                {
                    Debug.LogWarning($"[Signalia] Click Anywhere delegate with key {key} already exists. Overwriting the existing delegate.");
                }
                OnClickAnywhere[key] = (oneShot, action);
            }

            public static void InvokeClickAnywhere()
            {
                if (Config == null) { return; }

                Watchman.Watch();

                var keysToRemove = new List<string>();
                keysToRemove.AddRange(OnClickAnywhere.Keys.Where(k => OnClickAnywhere[k].oneShot));

                // invoke all actions in the OnClickAnywhere dictionary
                foreach (var kvp in OnClickAnywhere)
                {
                    OnClickAnywhere[kvp.Key].action?.Invoke();
                }

                // remove after loop to avoid modifying the collection while iterating
                keysToRemove.ForEach(k => OnClickAnywhere.Remove(k));
            }

            public static Dictionary<string, (bool oneShot, Action action)> OnClickAnywhere = new();

            public static void SetButtonsDisabled(bool disabled)
            {
                if (Config == null) { return; }

                Watchman.Watch();

                ButtonsDisabled = disabled;
            }

            private static bool ViewAnimationLock()
            {
                if (Config == null) { return false; }

                Watchman.Watch();

                // returns true if buttons cant be clicked during a view animation and a view is animating
                if (!Config.PreventButtonsClickingWhenViewAnimating) { return false; }
                return TrackedValues.AViewIsAnimating;
            }

            private static bool VAnimatableAnimationLock()
            {
                if (Config == null) { return false; }

                Watchman.Watch();

                // returns true if buttons cant be clicked during an animtable animation and a blocking animatable is animating
                if (!Config.PreventButtonsClickingWhenAnimatableAnimating) { return false; }
                return TrackedValues.AnyBlockingAnimatablesAnimating;
            }

            /// <summary>
            /// Called by the UIButton script when the button is successfully clicked once. Is a separate cooldown than the one on the button itself.
            /// </summary>
            public static void CoolDownButtons()
            {
                if (Config == null) { return; }

                Watchman.Watch();

                ButtonsOnCooldown = true;
                SIGS.DoIn(Config.DefaultButtonsCooldown, () => ButtonsOnCooldown = false);
            }

            /// <summary>
            /// Calls config usage methods. Only after config asset is set.
            /// </summary>
            public static void UseConfig()
            {
                if (Config == null) { return; }

                if (Config.ConvertAllButtonsToUIButtons)
                {
                    SIGS.Listener("SceneLoaded", ConvertButtonsToUIButton);
                    ConvertButtonsToUIButton(); // called the first time "UseConfig" is called. Then called with each scene load.
                }

                // init listeners for buttonKilled.
                Config.UIButtonsDisabler.InitializeListener((s) => ButtonsDisabled = true, false, null);
                Config.UIButtonsEnabler.InitializeListener((s) => ButtonsDisabled = false, false, null);

                // init listeners for event system
                Config.UnityEventSystemOff.InitializeListener((s) => UnityEngine.EventSystems.EventSystem.current.enabled = false, false, null);
                Config.UnityEventSystemOn.InitializeListener((s) => UnityEngine.EventSystems.EventSystem.current.enabled = true, false, null);

                // load settings
                RadioConfig.LoadSettings();

                // initialize input
                InputDelegation.Initialize();
            }

            private static void ConvertButtonsToUIButton()
            {
#if UNITY_6000_0_OR_NEWER
                UnityEngine.UI.Button[] buttons = MonoBehaviour.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                UnityEngine.UI.Button[] buttons = MonoBehaviour.FindObjectsOfType<UnityEngine.UI.Button>(true);
#endif


                foreach (UnityEngine.UI.Button button in buttons)
                {
                    if (button.gameObject.GetComponent<UIButton>() == null)
                    {
                        var btn = button.gameObject.AddComponent<UIButton>();

                        // overtake the normal button
                        btn.OvertakeDefaultButtonEvent();
                    }
                }
            }
            #endregion
        }

        public static class InputDelegation
        {
            public static  Dictionary<string, (bool oneShot, Action action)> AnyInputDownDelegates = new();
            public static bool CustomInputSystemAnyCall = false;

            public static void Initialize()
            {
#if !ENABLE_LEGACY_INPUT_MANAGER
                SIGS.Listener(FrameworkConstants.InternalListeners.ANYINPUT_FORCUSTOMINPUT, () => CustomInputSystemAnyCall = true); // not perfect, but will do for now.
#endif
            }

            public static void Update()
            {
                var config = ConfigReader.GetConfig();
                if (config != null && !config.InputSystem.EnableSignaliaInputSystem)
                {
                    return;
                }

                // Check if any input is down and invoke the event if so
                if (AnyInputDown())
                {
                    InvokeAnyInputDown();
                    CustomInputSystemAnyCall = false;
                }
            }

            /// <summary>
            /// Subscribes to any input down event. This will be invoked when any input is detected.
            /// </summary>
            /// <param name="action"></param>
            /// <param name="oneShot"></param>
            public static void SubscribeToAnyInputDown(Action action, bool oneShot = false)
            {
                Watchman.Watch();

                if (action == null) { return; }

                string key = action.Method.Name + (oneShot ? "_OneShot" : "_Persistent");
                if (AnyInputDownDelegates.ContainsKey(key))
                {
                    Debug.LogWarning($"[Signalia] Input delegate with key {key} already exists. Overwriting the existing delegate.");
                }
                AnyInputDownDelegates[key] = (oneShot, action);
            }

            public static void ResetInputs()
            {
                // Reset the input delegation
                AnyInputDownDelegates.Clear();

                // Reset
                CustomInputSystemAnyCall = false;
            }

            public static void InvokeAnyInputDown()
            {
                Watchman.Watch();

                var keysToRemove = new List<string>();
                keysToRemove.AddRange(AnyInputDownDelegates.Keys.Where(k => AnyInputDownDelegates[k].oneShot));

                // Invoke all actions that are subscribed to the any input down event
                foreach (var kvp in AnyInputDownDelegates)
                {
                    kvp.Value.action?.Invoke();
                }

                // remove after loop to avoid modifying the collection while iterating
                keysToRemove.ForEach(k => AnyInputDownDelegates.Remove(k));
            }

            #region Checkers
            /// <summary>
            /// Checks if any input down has happened since the last frame.
            /// </summary>
            /// <returns></returns>
            public static bool AnyInputDown()
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return UnityEngine.Input.anyKeyDown;
#else
                return CustomInputSystemAnyCall;
#endif
            }
            #endregion
        }

        public static class Debugging
        {
            public static bool IsDebugging => Config.EnableDebugging;
            public static bool IsIntrospectionEnabled => Config.UseIntrospection;

            public static void LogEventCreation(UnityEngine.Object context, string name)
            {
                if (!IsDebugging) return;
                if (!Config.LogListenerCreation) return;
                Debug.Log($"[Simple Radio] Listener Initialization: [{name}]", context);
            }

            public static void LogEventDisposal(UnityEngine.Object context, string name)
            {
                if (!IsDebugging) return;
                if (!Config.LogListenerDisposal) return;
                Debug.Log($"[Simple Radio] Listener Disposal: [{name}]", context);
            }

            public static void LogEventSend(UnityEngine.Object context, string name, params object[] args)
            {
                if (!IsDebugging) return;
                if (!Config.LogEventSend) return;
                string log = $"[Simple Radio] Event Sent: [{name}] | Args: {string.Join(", ", args)}";
                Debug.Log(log, context);
            }

            public static void LogEventReceive(UnityEngine.Object context, string name, params object[] args)
            {
                if (!IsDebugging) return;
                if (!Config.LogEventReceive) return;
                string log = $"[Simple Radio] Event Received: [{name}] | Args: {string.Join(", ", args)}";
                Debug.Log(log, context);
            }

            public static void LogLiveKeyCreation(UnityEngine.Object context, string name)
            {
                if (!IsDebugging) return;
                if (!Config.LogLiveKeyCreation) return;
                Debug.Log($"[Simple Radio] LiveKey Initialization: [{name}]", context);
            }

            public static void LogLiveKeyRead(UnityEngine.Object context, string name)
            {
                if (!IsDebugging) return;
                if (!Config.LogLiveKeyRead) return;
                Debug.Log($"[Simple Radio] LiveKey Read: [{name}]", context);
            }

            public static void LogLiveKeyDisposal(UnityEngine.Object context, string name)
            {
                if (!IsDebugging) return;
                if (!Config.LogLiveKeyDisposal) return;
                Debug.Log($"[Simple Radio] LiveKey Disposal: [{name}]", context);
            }

            public static void LogDeadKeyCreation(UnityEngine.Object context, string name)
            {
                if (!IsDebugging) return;
                if (!Config.LogDeadKeyCreation) return;
                Debug.Log($"[Simple Radio] DeadKey Initialization: [{name}]", context);
            }

            public static void LogDeadKeyRead(UnityEngine.Object context, string name)
            {
                if (!IsDebugging) return;
                if (!Config.LogDeadKeyRead) return;
                Debug.Log($"[Simple Radio] DeadKey Read: [{name}]", context);
            }

            public static void LogDeadKeyDisposal(UnityEngine.Object context, string name)
            {
                if (!IsDebugging) return;
                if (!Config.LogDeadKeyDisposal) return;
                Debug.Log($"[Simple Radio] DeadKey Disposal: [{name}]", context);
            }

            // ComplexRadio debugging methods
            public static void LogComplexListenerCreation(UnityEngine.Object context, string channelName, string listenerType)
            {
                if (!IsDebugging) return;
                if (!Config.LogComplexListenerCreation) return;
                Debug.Log($"[Complex Radio] Listener Creation: [{channelName}] Type: {listenerType}", context);
            }

            public static void LogComplexListenerDisposal(UnityEngine.Object context, string channelName, string listenerType)
            {
                if (!IsDebugging) return;
                if (!Config.LogComplexListenerDisposal) return;
                Debug.Log($"[Complex Radio] Listener Disposal: [{channelName}] Type: {listenerType}", context);
            }

            public static void LogChannelCreation(UnityEngine.Object context, string channelName)
            {
                if (!IsDebugging) return;
                if (!Config.LogChannelCreation) return;
                Debug.Log($"[Complex Radio] Channel Creation: [{channelName}]", context);
            }

            public static void LogChannelDisposal(UnityEngine.Object context, string channelName)
            {
                if (!IsDebugging) return;
                if (!Config.LogChannelDisposal) return;
                Debug.Log($"[Complex Radio] Channel Disposal: [{channelName}]", context);
            }

            public static void LogChannelSend(UnityEngine.Object context, string channelName, params object[] args)
            {
                if (!IsDebugging) return;
                if (!Config.LogChannelSend) return;
                string log = $"[Complex Radio] Channel Send: [{channelName}] | Args: {string.Join(", ", args)}";
                Debug.Log(log, context);
            }

            public static void LogChannelReceive(UnityEngine.Object context, string channelName, params object[] args)
            {
                if (!IsDebugging) return;
                if (!Config.LogChannelReceive) return;
                string log = $"[Complex Radio] Channel Receive: [{channelName}] | Args: {string.Join(", ", args)}";
                Debug.Log(log, context);
            }

            public static void LogHapticBeginTrigger(UnityEngine.Object context)
            {
                if (!IsDebugging) return;
                if (!Config.LogHaptics) return;
                string log = $"[Haptics] Haptic trigger active!";
                Debug.Log(log, context);
            }

            public static void LogHapticEndTrigger(UnityEngine.Object context)
            {
                if (!IsDebugging) return;
                if (!Config.LogHaptics) return;
                string log = $"[Haptics] Haptic trigger ended!";
                Debug.Log(log, context);
            }

            public static void LogHapticErroredTrigger(UnityEngine.Object context, string message)
            {
                if (!IsDebugging) return;
                if (!Config.LogHaptics) return;
                string log = $"[Haptics] Haptic faced issue triggering on connected input: " + message;
                Debug.LogWarning(log, context);
            }

            public static void LogHapticUnavailable(UnityEngine.Object context)
            {
                if (!IsDebugging) return;
                if (!Config.LogHaptics) return;
                string log = "[Haptics] Haptics unavailable on any input.";
                Debug.Log(log, context);
            }
        }
    }
}
