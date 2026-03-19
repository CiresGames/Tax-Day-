using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AHAKuo.Signalia.Radio.SimpleRadio;
using AHAKuo.Signalia.Framework;
using DG.Tweening;
using AHAKuo.Signalia.Utilities;

namespace AHAKuo.Signalia.Radio
{
    #region Simple Radio
    public class Listener
    {
        private string eventToHear;
        private readonly Action actionToPerform; // can be used from within code.
        private readonly Action<object[]> actionToPerformArgs; // can be used from within code.
        public string eventName => eventToHear;
        internal string EventName;
        internal bool HasArgs;
        internal bool HasSimple;
        private bool initalized = false;
        private bool oneShot = false;
        private GameObject context;
        private string trackingId; // Unique ID for tracking this listener

        public bool IsInitialized => initalized;

        // legacy
        private object[] args;
        public object[] assignedArguments => args;

        internal void Invoke()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"Event with event name {eventName} has not been initialized. And therefore cannot be invoked.");
            }

            actionToPerform?.Invoke();
            if (oneShot)
            {
                Dispose();
            }

            RuntimeValues.Debugging.LogEventReceive(context, eventName);
        }

        internal void Invoke(params object[] args)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"Event with event name {eventName} has not been initialized. And therefore cannot be invoked.");
            }

            this.args = args;

            actionToPerformArgs?.Invoke(args);
            if (oneShot)
            {
                Dispose();
            }

            RuntimeValues.Debugging.LogEventReceive(context, eventName, args);
        }

        /// <summary>
        /// Must be used to initalize the listener. Can pass in a method or function to perform on event send.
        /// </summary>
        /// <param name="unityAction"></param>
        public void InitializeEvent()
        {
            if (IsInitialized) { return; }
            if (eventName == string.Empty)
            {
                Debug.LogError("Cannot initialize event which contains an empty string");
                return;
            }

            SimpleRadio.RegisterListener(this);
            RegisterForTracking();
            HookWatchman();

            initalized = true;

            RuntimeValues.Debugging.LogEventCreation(context, eventName);
        }

        /// <summary>
        /// Called when the gameobject containing the listener is destroyed, or when the scene restarts. Must be called manually if the listener is created within code.
        /// </summary>
        public void Dispose()
        {
            if (!IsInitialized) { return; }
            SimpleRadio.UnregisterListener(this);

            // Unregister this listener from tracking
            UnregisterFromTracking();
            UnhookWatchman();

            initalized = false;

            RuntimeValues.Debugging.LogEventDisposal(context, eventName);
        }

        private void HookWatchman()
        {
            Watchman.OnTermination -= Dispose;
            Watchman.OnTermination += Dispose;
        }

        private void UnhookWatchman()
        {
            Watchman.OnTermination -= Dispose;
        }

        /// <summary>
        /// Register this listener for tracking purposes
        /// </summary>
        private void RegisterForTracking()
        {
            // Check if introspection is enabled
            if (!RuntimeValues.Debugging.IsIntrospectionEnabled) return;

            // Determine listener type based on which action is the primary one
            // If actionToPerform is not null and actionToPerformArgs is a wrapper, it's a SimpleEvent
            // If actionToPerform is null and actionToPerformArgs is the original, it's a ParameterEvent
            var listenerType = ListenerInfo.ListenerType.SimpleEvent;
            var methodName = "Unknown";
            var declaringType = "Unknown";

            if (actionToPerform != null)
            {
                // This is a simple event (Action)
                listenerType = ListenerInfo.ListenerType.SimpleEvent;
                methodName = actionToPerform.Method?.Name ?? "Unknown";
                declaringType = actionToPerform.Method?.DeclaringType?.Name ?? "Unknown";
            }
            else if (actionToPerformArgs != null)
            {
                // This is a parameter event (Action<object[]>)
                listenerType = ListenerInfo.ListenerType.ParameterEvent;
                methodName = actionToPerformArgs.Method?.Name ?? "Unknown";
                declaringType = actionToPerformArgs.Method?.DeclaringType?.Name ?? "Unknown";
            }

            var targetObjectName = context?.name ?? "Static";

            var info = new ListenerInfo(eventName, methodName, declaringType, targetObjectName, oneShot, listenerType, this);
            trackingId = info.uniqueId;

            SimpleRadio.RegisterListener(info);
        }

        /// <summary>
        /// Unregister this listener from tracking
        /// </summary>
        private void UnregisterFromTracking()
        {
            if (!string.IsNullOrEmpty(trackingId))
            {
                SimpleRadio.UnregisterListener(trackingId);
                trackingId = null;
            }
        }

        /// <summary>
        /// Can be initialized within code like this.
        /// </summary>
        /// <param name="eventToHear"></param>
        /// <param name="actionToPerform"></param>
        /// <param name="oneShot">Destroy the listener after the first event fire.</param>
        /// <param name="context">Define the context of the listener to use it in debugging.</param>
        public Listener(string eventToHear, Action actionToPerform, bool oneShot = false, GameObject context = null)
        {
            Watchman.Watch();

            this.eventToHear = eventToHear;
            this.actionToPerform = actionToPerform;
            this.actionToPerformArgs = ConfigReader.GetConfig()?.TwoSideListeners == true ? (s) => actionToPerform?.Invoke() : null;
            EventName = eventToHear;
            HasSimple = actionToPerform != null;
            HasArgs = actionToPerformArgs != null;
            this.oneShot = oneShot;
            this.context = context;
            InitializeEvent();
        }

        /// <summary>
        /// Can be initialized within code like this.
        /// </summary>
        /// <param name="eventToHear"></param>
        /// <param name="actionToPerform"></param>
        /// <param name="oneShot">Destroy the listener after the first event fire.</param>
        /// <param name="context">Define the context of the listener to use it in debugging.</param>
        public Listener(string eventToHear, Action<object[]> actionToPerform, bool oneShot = false, GameObject context = null)
        {
            Watchman.Watch();

            this.eventToHear = eventToHear;
            this.actionToPerformArgs = actionToPerform;
            EventName = eventToHear;
            HasSimple = false;
            HasArgs = actionToPerformArgs != null;
            this.oneShot = oneShot;
            this.context = context;
            InitializeEvent();
        }
    }

    /// <summary>
    /// Acts like a listener, but returns a value instead of performing an action.
    /// Useful for decoupling code and returning values over the event radio system.
    /// </summary>
    public class LiveKey
    {
        private readonly string eventToHear;
        private Func<object> valueProvider;

        public string EventName => eventToHear;

        public bool IsInitialized => SimpleRadio.LiveKeyDictionary.ContainsKey(eventToHear);

        private GameObject context;

        public LiveKey(string eventToHear, Func<object> valueProvider, GameObject context = null)
        {
            Watchman.Watch();

            SetContext(context);

            this.eventToHear = eventToHear;
            this.valueProvider = valueProvider;

            // Check if the event is already registered in the dictionary.
            if (SimpleRadio.LiveKeyDictionary.ContainsKey(this.eventToHear))
            {
                // If it is, we can just change the value provider.
                ChangeValue(valueProvider);

                RuntimeValues.Debugging.LogLiveKeyRead(context, eventToHear);
            }
            else
            {
                // Register the live key in the dictionary with its event and value provider.
                SimpleRadio.LiveKeyDictionary[this.eventToHear] = this.valueProvider;

                // Init the disposal event if not the signalia singleton. This is because the singleton is always destroyed last by the SignaliaRadioManager.
                Watchman.OnTermination -= Dispose;
                Watchman.OnTermination += Dispose;

                RuntimeValues.Debugging.LogLiveKeyCreation(context, eventToHear);
            }
        }

        /// <summary>
        /// Changes the value provider of the live key after it has been created.
        /// </summary>
        /// <param name="valueProvider"></param>
        public void ChangeValue(Func<object> valueProvider)
        {
            this.valueProvider = valueProvider;

            // update it in the dictionary.
            SimpleRadio.LiveKeyDictionary[eventToHear] = this.valueProvider;
        }

        public void Dispose()
        {
            if (SimpleRadio.LiveKeyDictionary.ContainsKey(eventToHear))
            {
                SimpleRadio.LiveKeyDictionary.Remove(eventToHear);
            }

            RuntimeValues.Debugging.LogLiveKeyDisposal(context, eventToHear);
        }

        public object ValueAssigned => valueProvider();

        public void SetContext(GameObject gameObject)
        {
            context = gameObject;
        }
    }

    /// <summary>
    /// Acts like a listener, but returns a value instead of performing an action.
    /// Useful for decoupling code and returning values over the event radio system.
    /// This version is for non-static objects, meaning it can be used to listen to events on a per-instance basis.
    /// </summary>
    public class DeadKey
    {
        private readonly string eventToHear;
        private object valueProvider;

        public string EventName => eventToHear;

        public bool IsInitialized => SimpleRadio.DeadKeyDictionary.ContainsKey(eventToHear);

        private GameObject context;

        public DeadKey(string eventToHear, object valueProvider, GameObject context = null)
        {
            Watchman.Watch();

            SetContext(context);

            this.eventToHear = eventToHear;
            this.valueProvider = valueProvider;

            // Check if the event is already registered in the dictionary.
            if (SimpleRadio.DeadKeyDictionary.ContainsKey(this.eventToHear))
            {
                // If it is, we can just change the value provider.
                ChangeValue(valueProvider);

                RuntimeValues.Debugging.LogDeadKeyRead(context, eventToHear);
            }
            else
            {
                // Register the dead key in the dictionary with its event and value provider.
                SimpleRadio.DeadKeyDictionary[this.eventToHear] = this.valueProvider;

                // Init the disposal event if not the signalia singleton. This is because the singleton is always destroyed last by the SignaliaRadioManager.
                Watchman.OnTermination -= Dispose;
                Watchman.OnTermination += Dispose;

                RuntimeValues.Debugging.LogDeadKeyCreation(context, eventToHear);
            }
        }

        /// <summary>
        /// Changes the value provider of the dead key after it has been created.
        /// </summary>
        /// <param name="valueProvider"></param>
        public void ChangeValue(object valueProvider)
        {
            this.valueProvider = valueProvider;

            // update it in the dictionary.
            SimpleRadio.DeadKeyDictionary[eventToHear] = this.valueProvider;
        }

        public void Dispose()
        {
            if (SimpleRadio.DeadKeyDictionary.ContainsKey(eventToHear))
            {
                SimpleRadio.DeadKeyDictionary.Remove(eventToHear);
            }

            RuntimeValues.Debugging.LogDeadKeyDisposal(context, eventToHear);
        }

        public object ValueAssigned => valueProvider;

        public void SetContext(GameObject gameObject)
        {
            context = gameObject;
        }
    }
    #endregion

    #region Complex Radio
    /// <summary>
    /// An action method which can be invoked with or without arguments.
    /// </summary>
    public class ResonanceAction
    {
        private readonly Action action;
        private readonly Action<object[]> actionArgs;

        // Public properties for accessing the underlying actions
        public Action Action => action;
        public Action<object[]> ActionArgs => actionArgs;

        public ResonanceAction(Action action)
        {
            this.action = action;
        }

        public ResonanceAction(Action<object[]> actionArgs)
        {
            this.actionArgs = actionArgs;
        }

        public void Invoke(params object[] args)
        {
            action?.Invoke(); // this is ok, because the actionArgs will be null if the action is not null.
            actionArgs?.Invoke(args);
        }
    }

    /// <summary>
    /// A channel which can be listened to by multiple listeners. It feeds messages to all listeners with packed arguments.
    /// </summary>
    public class ResonanceChannel
    {
        public string channelName;
        private readonly List<BlindListener> blindListeners = new();
        private readonly Dictionary<string, List<KeyedListener>> keyedListeners = new();
        private readonly Dictionary<Type, List<TypedListener>> typedListeners = new();
        public Dictionary<string, Func<object>> channelContents = new(); // a funny way to store the contents of the channel << i.e. objects which we can re-access later.
        public Dictionary<string, object> nonStaticChannelContents = new(); // stores non-static values that can be re-accessed later

        public ResonanceChannel(string channelName)
        {
            this.channelName = channelName;
        }

        public int ListenerCount => typedListeners.Count + keyedListeners.Count;

        public ResonanceChannel AddBlindListener(Action action)
        {
            var blindListener = new BlindListener(action);
            blindListener.RegisterForTracking(channelName, "BlindListener");
            blindListeners.Add(blindListener);
            return this; // to allow chaining.
        }

        public ResonanceChannel AddBlindListener(Action<object[]> action)
        {
            var blindListener = new BlindListener(action);
            blindListener.RegisterForTracking(channelName, "BlindListener");
            blindListeners.Add(blindListener);
            return this; // to allow chaining.
        }

        public ResonanceChannel AddMessageListener(string key, Action action)
        {
            var keyedListener = new KeyedListener(key, action);
            keyedListener.RegisterForTracking(channelName, "KeyedListener");
            if (!keyedListeners.TryGetValue(key, out var list))
            {
                list = new List<KeyedListener>();
                keyedListeners[key] = list;
            }

            list.Add(keyedListener);
            return this;

        }

        public ResonanceChannel AddMessageListener(string key, Action<object[]> action)
        {
            var keyedListener = new KeyedListener(key, action);
            keyedListener.RegisterForTracking(channelName, "KeyedListener");
            if (!keyedListeners.TryGetValue(key, out var list))
            {
                list = new List<KeyedListener>();
                keyedListeners[key] = list;
            }

            list.Add(keyedListener);
            return this;
        }

        public ResonanceChannel AddTypedListener<T>(Action<object[]> action) where T : class
        {
            var typedListener = new TypedListener(typeof(T), action);
            typedListener.RegisterForTracking(channelName, "TypedListener");
            if (!typedListeners.TryGetValue(typeof(T), out var list))
            {
                list = new List<TypedListener>();
                typedListeners[typeof(T)] = list;
            }

            list.Add(typedListener);
            return this;
        }

        public void RemoveListener(ResonanceListener listener)
        {
            listener.UnregisterFromTracking();
            switch (listener)
            {
                case BlindListener blindListener:
                    blindListeners.Remove(blindListener);
                    break;
                case KeyedListener keyedListener:
                    if (keyedListeners.TryGetValue(keyedListener.Key, out var keyed))
                    {
                        keyed.Remove(keyedListener);
                        if (keyed.Count == 0)
                            keyedListeners.Remove(keyedListener.Key);
                    }
                    break;
                case TypedListener typedListener:
                    if (typedListeners.TryGetValue(typedListener.Type, out var typed))
                    {
                        typed.Remove(typedListener);
                        if (typed.Count == 0)
                            typedListeners.Remove(typedListener.Type);
                    }
                    break;
            }
        }

        public ResonanceChannel AddContent(string contentKey, Func<object> content)
        {
            channelContents[contentKey] = content;
            return this;
        }

        public T GetContent<T>(string contentKey)
        {
            if (channelContents.ContainsKey(contentKey))
            {
                return (T)channelContents[contentKey]();
            }

            return default;
        }

        public void RemoveContent(string contentKey)
        {
            if (channelContents.ContainsKey(contentKey))
            {
                channelContents.Remove(contentKey);
            }
        }

        public ResonanceChannel AddNonStaticContent(string contentKey, object content)
        {
            nonStaticChannelContents[contentKey] = content;
            return this;
        }

        public T GetNonStaticContent<T>(string contentKey)
        {
            if (nonStaticChannelContents.ContainsKey(contentKey))
            {
                return (T)nonStaticChannelContents[contentKey];
            }

            return default;
        }

        public void RemoveNonStaticContent(string contentKey)
        {
            if (nonStaticChannelContents.ContainsKey(contentKey))
            {
                nonStaticChannelContents.Remove(contentKey);
            }
        }

        public void CleanseContents()
        {
            // Unregister all listeners before clearing
            foreach (var listener in blindListeners)
            {
                listener.UnregisterFromTracking();
            }
            foreach (var keyedList in keyedListeners.Values)
            {
                foreach (var listener in keyedList)
                {
                    listener.UnregisterFromTracking();
                }
            }
            foreach (var typedList in typedListeners.Values)
            {
                foreach (var listener in typedList)
                {
                    listener.UnregisterFromTracking();
                }
            }
            blindListeners.Clear();
            keyedListeners.Clear();
            typedListeners.Clear();
            channelContents.Clear();
            nonStaticChannelContents.Clear();
        }

        public void Send(string message, params object[] args)
        {
            // Debug logging
            RuntimeValues.Debugging.LogChannelSend(null, channelName, message, args);

            for (int i = blindListeners.Count - 1; i >= 0; i--)
                blindListeners[i].ReceiveMessage(message, args);

            if (keyedListeners.TryGetValue(message, out var keyed))
            {
                for (int i = keyed.Count - 1; i >= 0; i--)
                    keyed[i].ReceiveMessage(message, args);
            }

            if (args != null)
            {
                for (int a = 0; a < args.Length; a++)
                {
                    var type = args[a]?.GetType();
                    if (type == null) continue;

                    if (typedListeners.TryGetValue(type, out var typed))
                    {
                        for (int i = typed.Count - 1; i >= 0; i--)
                            typed[i].ReceiveMessage(message, args);
                    }
                }
            }
        }
    }

    public abstract class ResonanceListener
    {
        public ResonanceAction actionPerformed;
        protected string trackingId; // Unique ID for tracking this listener
        protected string channelName; // Channel this listener belongs to

        public virtual void ReceiveMessage(string message, params object[] args)
        {
            // Debug logging
            RuntimeValues.Debugging.LogChannelReceive(null, channelName, message, args);

            actionPerformed?.Invoke(args);
        }

        /// <summary>
        /// Register this listener for tracking purposes
        /// </summary>
        public void RegisterForTracking(string channelName, string listenerType)
        {
            // Check if introspection is enabled
            if (!RuntimeValues.Debugging.IsIntrospectionEnabled) return;

            this.channelName = channelName;
            var methodName = GetMethodName();
            var declaringType = GetDeclaringType();
            var targetObjectName = "ComplexRadio";

            var info = new ListenerInfo(channelName, methodName, declaringType, targetObjectName, false, ListenerInfo.ListenerType.ComplexChannel, this);
            trackingId = info.uniqueId;

            ComplexRadio.RegisterComplexListener(info);

            // Debug logging
            RuntimeValues.Debugging.LogComplexListenerCreation(null, channelName, listenerType);
        }

        /// <summary>
        /// Get the method name from the action
        /// </summary>
        protected virtual string GetMethodName()
        {
            return actionPerformed?.Action?.Method?.Name ?? actionPerformed?.ActionArgs?.Method?.Name ?? "Unknown";
        }

        /// <summary>
        /// Get the declaring type from the action
        /// </summary>
        protected virtual string GetDeclaringType()
        {
            return actionPerformed?.Action?.Method?.DeclaringType?.Name ?? actionPerformed?.ActionArgs?.Method?.DeclaringType?.Name ?? "Unknown";
        }

        /// <summary>
        /// Unregister this listener from tracking
        /// </summary>
        public void UnregisterFromTracking()
        {
            if (!string.IsNullOrEmpty(trackingId))
            {
                // Debug logging before unregistering
                RuntimeValues.Debugging.LogComplexListenerDisposal(null, channelName, GetType().Name);

                ComplexRadio.UnregisterComplexListener(trackingId);
                trackingId = null;
            }
        }
    }

    /// <summary>
    /// If it's in the channel, it will perform the action when any message is received. Blind to which message it is.
    /// </summary>
    public class BlindListener : ResonanceListener
    {
        public BlindListener(Action actionPerformed)
        {
            this.actionPerformed = new(actionPerformed);
        }

        public BlindListener(Action<object[]> actionPerformed)
        {
            this.actionPerformed = new(actionPerformed);
        }
    }

    /// <summary>
    /// Checks for a specific message and performs the action if it matches.
    /// </summary>
    public class KeyedListener : ResonanceListener
    {
        public string key;
        public string Key => key;

        public KeyedListener(string key, Action actionPerformed)
        {
            this.key = key;
            this.actionPerformed = new(actionPerformed);
        }

        public KeyedListener(string key, Action<object[]> actionPerformed)
        {
            this.key = key;
            this.actionPerformed = new(actionPerformed);
        }

        public override void ReceiveMessage(string message, params object[] args)
        {
            if (!message.Equals(key)) return;

            base.ReceiveMessage(message, args);
        }
    }

    /// <summary>
    /// Checks for a specific type argument in the passed arguments and performs the action using that argument.
    /// </summary>
    public class TypedListener : ResonanceListener
    {
        public Type type;
        public Type Type => type;

        public TypedListener(Type type, Action<object[]> actionPerformed)
        {
            this.type = type;
            this.actionPerformed = new(actionPerformed);
        }

        public override void ReceiveMessage(string message, params object[] args)
        {
            foreach (object arg in args)
            {
                if (arg.GetType() == type)
                {
                    base.ReceiveMessage(message, args);
                    return;
                }
            }
        }
    }
    #endregion

    #region Audio
    public interface IAudioPlayingSettings
    {
        public void ApplyOnSource(AudioSource source);
    }

    public interface IRememberMe : IAudioPlayingSettings
    {
        public string GetIdentifier();
    }

    public struct ParentedEmitter : IAudioPlayingSettings
    {
        public Transform parent;
        public Vector3 offset;
        public ParentedEmitter(Transform parent, Vector3 offset)
        {
            this.parent = parent;
            this.offset = offset;
        }

        public readonly void ApplyOnSource(AudioSource source)
        {
            source.transform.SetParent(parent);
            source.transform.localPosition = offset;
        }
    }

    public struct Remembrance : IRememberMe
    {
        public string identifier;
        public Remembrance(string identifier)
        {
            this.identifier = identifier;
        }
        public string GetIdentifier()
        {
            return identifier;
        }
        public void ApplyOnSource(AudioSource source)
        {
            // do nothing, it's just a marker.
        }
    }
    public struct FadeIn : IAudioPlayingSettings
    {
        public float fadeInTime;
        public bool setStartVolume;
        public float startVolume;
        public bool unscaledTime;
        public FadeIn(float fadeInTime, bool setStartVolume = false, float startVolume = 1, bool unscaledTime = false)
        {
            this.fadeInTime = fadeInTime;
            this.setStartVolume = setStartVolume;
            this.startVolume = startVolume;
            this.unscaledTime = unscaledTime;
        }

        public void ApplyOnSource(AudioSource source)
        {
            // kill tweens on source
            source.DOKill();

            var originalVolume = source.volume;

            if (setStartVolume)
            {
                source.volume = startVolume;
            }
            else
            {
                source.volume = 0;
            }

            source.DOFade(originalVolume, fadeInTime).SetUpdate(unscaledTime);
        }
    }
    public struct FadeOut : IAudioPlayingSettings
    {
        public float fadeOutTime;
        public float endVolume;
        public bool unscaledTime;
        public FadeOut(float fadeOutTime, float endVolume = 0, bool unscaledTime = false)
        {
            this.fadeOutTime = fadeOutTime;
            this.endVolume = endVolume;
            this.unscaledTime = unscaledTime;
        }

        public void ApplyOnSource(AudioSource source)
        {
            source.DOKill();
            source.DOFade(0, fadeOutTime).SetUpdate(unscaledTime);
        }
    }
    public struct Audio3D : IAudioPlayingSettings
    {
        public Vector3 position;
        public float minDistance;
        public float maxDistance;
        public AudioRolloffMode rolloffMode;
        public float spatialBlend;
        public float dopplerLevel;
        public float spread;
        public AudioVelocityUpdateMode velocityUpdateMode;

        public Audio3D(Vector3 position, float minDistance = 1f, float maxDistance = 500f,
                      AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic,
                      float spatialBlend = 1f, float dopplerLevel = 1f, float spread = 0f,
                      AudioVelocityUpdateMode velocityUpdateMode = AudioVelocityUpdateMode.Auto)
        {
            this.position = position;
            this.minDistance = minDistance;
            this.maxDistance = maxDistance;
            this.rolloffMode = rolloffMode;
            this.spatialBlend = spatialBlend;
            this.dopplerLevel = dopplerLevel;
            this.spread = spread;
            this.velocityUpdateMode = velocityUpdateMode;
        }

        public void ApplyOnSource(AudioSource source)
        {
            source.transform.position = position;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = rolloffMode;
            source.spatialBlend = spatialBlend; // 1.0 = fully 3D, 0.0 = fully 2D
            source.dopplerLevel = dopplerLevel;
            source.spread = spread;
            source.velocityUpdateMode = velocityUpdateMode;
        }
    }

    [System.Serializable]
    public struct AudioFilters : IAudioPlayingSettings
    {
        public bool useLowPassFilter;
        public float lowPassCutoffFrequency;
        public float lowPassResonanceQ;
        public bool useHighPassFilter;
        public float highPassCutoffFrequency;

        public AudioFilters(bool useLowPassFilter = false, float lowPassCutoffFrequency = 5000f, float lowPassResonanceQ = 1f,
                             bool useHighPassFilter = false, float highPassCutoffFrequency = 10f)
        {
            this.useLowPassFilter = useLowPassFilter;
            this.lowPassCutoffFrequency = lowPassCutoffFrequency;
            this.lowPassResonanceQ = lowPassResonanceQ;
            this.useHighPassFilter = useHighPassFilter;
            this.highPassCutoffFrequency = highPassCutoffFrequency;
        }

        public readonly bool HasFilters => useLowPassFilter || useHighPassFilter;

        public readonly void ApplyOnSource(AudioSource source)
        {
            if (HasFilters)
            {
                AudioFilterUtility.ApplyFilters(source, this);
            }
            else
            {
                AudioFilterUtility.ResetFilters(source);
            }
        }
    }

    public static class AudioFilterUtility
    {
        public static void ApplyFilters(AudioSource source, AudioFilters filters)
        {
            ApplyLowPass(source, filters.useLowPassFilter, filters.lowPassCutoffFrequency, filters.lowPassResonanceQ);
            ApplyHighPass(source, filters.useHighPassFilter, filters.highPassCutoffFrequency);
        }

        public static void ResetFilters(AudioSource source)
        {
            RemoveFilter<AudioLowPassFilter>(source);
            RemoveFilter<AudioHighPassFilter>(source);
        }

        private static void ApplyLowPass(AudioSource source, bool enabled, float cutoff, float resonanceQ)
        {
            var filter = source.GetComponent<AudioLowPassFilter>();

            if (enabled)
            {
                filter ??= source.gameObject.AddComponent<AudioLowPassFilter>();
                filter.enabled = true;
                filter.cutoffFrequency = cutoff;
                filter.lowpassResonanceQ = resonanceQ;
            }
            else if (filter != null)
            {
                UnityEngine.Object.Destroy(filter);
            }
        }

        private static void ApplyHighPass(AudioSource source, bool enabled, float cutoff)
        {
            var filter = source.GetComponent<AudioHighPassFilter>();

            if (enabled)
            {
                filter ??= source.gameObject.AddComponent<AudioHighPassFilter>();
                filter.enabled = true;
                filter.cutoffFrequency = cutoff;
            }
            else if (filter != null)
            {
                UnityEngine.Object.Destroy(filter);
            }
        }

        private static void RemoveFilter<T>(AudioSource source) where T : Behaviour
        {
            var filter = source.GetComponent<T>();
            if (filter != null)
            {
                UnityEngine.Object.Destroy(filter);
            }
        }
    }
    public struct HapticOnPlay : IAudioPlayingSettings
    {
        public HapticSettings hapticSettings;
        public HapticOnPlay(HapticSettings hapticSettings)
        {
            this.hapticSettings = hapticSettings;
        }
        public void ApplyOnSource(AudioSource source)
        {
            HapticsManager.TriggerHaptic(hapticSettings);
        }
    }
    #endregion

    #region Haptics System
    /// <summary>
    /// Centralized haptics manager that handles all haptic feedback across the Signalia framework.
    /// Supports multiple input systems and provides debugging capabilities.
    /// </summary>
    public static class HapticsManager
    {
        /// <summary>
        /// Global enable/disable for all haptics
        /// </summary>
        public static bool Enabled => RuntimeValues.RadioConfig.HapticsActive;

        /// <summary>
        /// Global intensity multiplier applied to all haptics
        /// </summary>
        public static float GlobalIntensityMultiplier => RuntimeValues.Config.Haptics_GlobalIntensityMultiplier;

        /// <summary>
        /// Global duration multiplier applied to all haptics
        /// </summary>
        public static float GlobalDurationMultiplier => RuntimeValues.Config.Haptics_GlobalDurationMultiplier;

        /// <summary>
        /// Triggers haptic feedback with the specified settings
        /// </summary>
        /// <param name="settings">Haptic settings to apply</param>
        public static void TriggerHaptic(HapticSettings settings)
        {
            if (!Enabled || !settings.Enabled)
                return;

            float finalIntensity = Mathf.Clamp01(settings.Intensity * GlobalIntensityMultiplier);
            float finalDuration = Mathf.Clamp(settings.Duration * GlobalDurationMultiplier, 0.01f, 1f);

            TriggerHapticInternal(settings.HapticType, finalIntensity, finalDuration);
        }

        /// <summary>
        /// Triggers haptic feedback with individual parameters
        /// </summary>
        /// <param name="type">Type of haptic feedback</param>
        /// <param name="intensity">Intensity of the haptic (0-1)</param>
        /// <param name="duration">Duration of the haptic in seconds</param>
        public static void TriggerHaptic(HapticType type, float intensity = 1f, float duration = 0.1f)
        {
            if (!Enabled || type == HapticType.None)
                return;

            float finalIntensity = Mathf.Clamp01(intensity * GlobalIntensityMultiplier);
            float finalDuration = Mathf.Clamp(duration * GlobalDurationMultiplier, 0.01f, 1f);

            TriggerHapticInternal(type, finalIntensity, finalDuration);
        }

        /// <summary>
        /// Triggers a preset haptic type
        /// </summary>
        /// <param name="type">Preset haptic type</param>
        public static void TriggerHapticPreset(HapticType type)
        {
            var preset = HapticSettings.CreatePreset(type);
            TriggerHaptic(preset);
        }

        /// <summary>
        /// Internal method that handles the actual haptic triggering
        /// </summary>
        private static void TriggerHapticInternal(HapticType type, float intensity, float duration)
        {
            // Try different haptic systems in order of preference
            if (TryTriggerUnityHaptics(type, intensity, duration))
                return;

            if (TryTriggerXRHaptics(type, intensity, duration))
                return;

            RuntimeValues.Debugging.LogHapticUnavailable(null);
        }

        /// <summary>
        /// Try to trigger haptics using Unity's built-in haptic system
        /// </summary>
        private static bool TryTriggerUnityHaptics(HapticType type, float intensity, float duration)
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_IPHONE
            try
            {
                // Unity's Handheld.Vibrate() for mobile devices
                if (Application.isMobilePlatform)
                {
                    // Convert intensity to vibration duration (iOS/Android)
                    int vibrationDuration = Mathf.RoundToInt(intensity * 100); // 0-100ms

                    if (vibrationDuration > 0)
                    {
                        Handheld.Vibrate();
                        RuntimeValues.Debugging.LogHapticBeginTrigger(null);
                        return true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                RuntimeValues.Debugging.LogHapticErroredTrigger(null, ex.Message);
            }
#endif

            return false;
        }

        /// <summary>
        /// Try to trigger haptics using Unity's XR system (VR controllers only)
        /// </summary>
        private static bool TryTriggerXRHaptics(HapticType type, float intensity, float duration)
        {
            try
            {
                // Check if XR is available and get XR devices
                var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevices(devices);

                foreach (var device in devices)
                {
                    // Check if device supports haptics
                    if (device.TryGetHapticCapabilities(out UnityEngine.XR.HapticCapabilities capabilities) && capabilities.supportsImpulse)
                    {
                        // Send haptic impulse to the device
                        device.SendHapticImpulse(0, intensity, duration);

                        RuntimeValues.Debugging.LogHapticBeginTrigger(null);
                        return true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (ConfigReader.GetConfig().LogHaptics)
                {
                    RuntimeValues.Debugging.LogHapticErroredTrigger(null, ex.Message);
                }
            }

            return false;
        }


        /// <summary>
        /// Stop all haptic feedback
        /// </summary>
        public static void StopAllHaptics()
        {
            try
            {
                // Stop XR device haptics
                var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevices(devices);

                foreach (var device in devices)
                {
                    if (device.TryGetHapticCapabilities(out UnityEngine.XR.HapticCapabilities capabilities) && capabilities.supportsImpulse)
                    {
                        device.StopHaptics();
                    }
                }

                RuntimeValues.Debugging.LogHapticEndTrigger(null);
            }
            catch (System.Exception ex)
            {
                RuntimeValues.Debugging.LogHapticErroredTrigger(null, ex.Message);
            }
        }

        /// <summary>
        /// Get information about connected haptic devices
        /// </summary>
        public static string GetHapticDeviceInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== Haptic Devices ===");

            // Mobile devices
            if (Application.isMobilePlatform)
            {
                info.AppendLine($"Mobile Platform: {Application.platform}");
                info.AppendLine("Mobile Haptics: Handheld.Vibrate() available ✓");
            }

            // XR devices
            try
            {
                var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevices(devices);

                int xrHapticDevices = 0;
                foreach (var device in devices)
                {
                    UnityEngine.XR.HapticCapabilities capabilities;
                    if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
                    {
                        xrHapticDevices++;
                        info.AppendLine($"  XR Device: {device.name} - Haptics: ✓");
                    }
                }

                if (xrHapticDevices == 0)
                {
                    info.AppendLine("XR Haptic Devices: None detected");
                }
                else
                {
                    info.AppendLine($"XR Haptic Devices: {xrHapticDevices} found");
                }
            }
            catch (System.Exception ex)
            {
                info.AppendLine($"XR Devices: Error checking ({ex.Message})");
            }

            // Connected joysticks (no haptic support in Unity core)
            var joysticks = Input.GetJoystickNames();
            info.AppendLine($"Connected Joysticks/Gamepads: {joysticks.Length}");
            for (int i = 0; i < joysticks.Length; i++)
            {
                if (!string.IsNullOrEmpty(joysticks[i]))
                {
                    info.AppendLine($"  {i + 1}. {joysticks[i]} - Haptics: ✗ (Unity core limitation)");
                }
            }

            if (joysticks.Length == 0)
            {
                info.AppendLine("No gamepads/joysticks detected");
            }

            // System info
            info.AppendLine($"Current Platform: {Application.platform}");
            info.AppendLine($"Haptic System Status: {(Enabled ? "Enabled" : "Disabled")}");
            info.AppendLine($"Debug Mode: {(RuntimeValues.Debugging.IsDebugging ? "Enabled" : "Disabled")}");

            // Limitations note
            info.AppendLine("\nNote: Unity core only supports haptics for:");
            info.AppendLine("• Mobile devices (Handheld.Vibrate)");
            info.AppendLine("• XR/VR controllers (XR.InputDevice)");
            info.AppendLine("• Regular gamepad haptics require Input System package");

            return info.ToString();
        }
    }
    #endregion
}
