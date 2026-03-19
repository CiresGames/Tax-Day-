using System;
using System.Collections.Generic;
using System.Diagnostics;
using AHAKuo.Signalia.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AHAKuo.Signalia.Radio
{
    /// <summary>
    /// Metadata about a listener for tracking and debugging purposes
    /// </summary>
    [System.Serializable]
    public struct ListenerInfo
    {
        public string uniqueId;
        public string eventName;
        public string methodName;
        public string declaringType;
        public string targetObjectName;
        public bool isOneShot;
        public System.DateTime creationTime;
        public ListenerType listenerType;
        
        // Store the actual listener object for disposal
        [System.NonSerialized]
        public object listenerObject;
        
        public enum ListenerType
        {
            SimpleEvent,
            ParameterEvent,
            ComplexChannel
        }
        
        public ListenerInfo(string eventName, string methodName, string declaringType, string targetObjectName, bool isOneShot, ListenerType type, object listenerObject = null)
        {
            this.uniqueId = System.Guid.NewGuid().ToString("N")[..8]; // Short unique ID
            this.eventName = eventName;
            this.methodName = methodName;
            this.declaringType = declaringType;
            this.targetObjectName = targetObjectName ?? "Static";
            this.isOneShot = isOneShot;
            this.creationTime = System.DateTime.Now;
            this.listenerType = type;
            this.listenerObject = listenerObject;
        }
        
        public override string ToString()
        {
            return $"[{uniqueId}] {eventName} → {declaringType}.{methodName} ({targetObjectName})";
        }
    }
    /// <summary>
    /// Container for the event radio system. Simple version: Does not use channels and broadcasts, but quick listeners and responders.
    /// </summary>
    internal sealed class SimpleBucket
    {
        public readonly List<Listener> simpleListeners = new();
        public readonly List<Listener> parameterListeners = new();
    }

    public static class SimpleRadio
    {
        private static readonly Dictionary<string, SimpleBucket> simpleBuckets = new();

        // Used to store live key values, associating an event string with a reference to a value.
        public static readonly Dictionary<string, Func<object>> LiveKeyDictionary = new();

        // Used to store dead key values, associating an event string with a reference to a value that is not current.
        public static readonly Dictionary<string, object> DeadKeyDictionary = new();

        // Listener tracking for debugging and system vitals
        private static readonly Dictionary<string, ListenerInfo> trackedListeners = new();
        private static readonly Dictionary<string, List<string>> listenersByEvent = new();


        public static void CleanUp()
        {
            simpleBuckets.Clear();
            LiveKeyDictionary.Clear();
            DeadKeyDictionary.Clear();
            ClearTrackedListeners();
        }

        /// <summary>
        /// Broadcasts the event in the event radio system and performs actions if any are attached to the found listener.
        /// </summary>
        /// <param name="eventString"></param>
        public static void SendEvent(string eventString)
        {
            if (string.IsNullOrEmpty(eventString)) { return; }
            RuntimeValues.Debugging.LogEventSend(null, eventString);
            if (!simpleBuckets.TryGetValue(eventString, out var bucket)) return;

            for (int i = bucket.simpleListeners.Count - 1; i >= 0; i--)
            {
                bucket.simpleListeners[i].Invoke();
            }
        }

        /// <summary>
        /// Broadcasts the event in the event radio system and performs actions if any are attached to the found listener. Supplies gameobject as context for debugging.
        /// </summary>
        /// <param name="eventString"></param>
        /// <param name="context"></param>
        public static void SendEventByContext(string eventString, GameObject context)
        {
            if (!string.IsNullOrEmpty(eventString))
            {
                RuntimeValues.Debugging.LogEventSend(context, eventString);
                if (!simpleBuckets.TryGetValue(eventString, out var bucket)) return;

                for (int i = bucket.simpleListeners.Count - 1; i >= 0; i--)
                {
                    bucket.simpleListeners[i].Invoke();
                }
            }
        }

        /// <summary>
        /// Broadcasts the event in the event radio system and performs actions if any are attached to the found listener. Uses parameters.
        /// </summary>
        /// <param name="eventString"></param>
        public static void SendEvent(string eventString, params object[] args)
        {
            if (string.IsNullOrEmpty(eventString)) { return; }
            RuntimeValues.Debugging.LogEventSend(null, eventString, args);
            if (!simpleBuckets.TryGetValue(eventString, out var bucket)) return;

            for (int i = bucket.parameterListeners.Count - 1; i >= 0; i--)
            {
                bucket.parameterListeners[i].Invoke(args);
            }
        }

        /// <summary>
        /// Broadcasts the event in the event radio system and performs actions if any are attached to the found listener. Supplies gameobject as context for debugging.
        /// </summary>
        /// <param name="eventString"></param>
        /// <param name="context"></param>
        public static void SendEventByContext(string eventString, GameObject context, params object[] args)
        {
            if (!string.IsNullOrEmpty(eventString))
            {
                RuntimeValues.Debugging.LogEventSend(context, eventString, args);
                if (!simpleBuckets.TryGetValue(eventString, out var bucket)) return;

                for (int i = bucket.parameterListeners.Count - 1; i >= 0; i--)
                {
                    bucket.parameterListeners[i].Invoke(args);
                }
            }
        }

        /// <summary>
        /// Retrieves the live key value associated with the specified event string. Best to do a check before using this method, as this method does not return null values and will cause an exception if the string doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of the variable to be returned.</typeparam>
        /// <param name="eventString">The event string to listen for.</param>
        /// <returns>The value associated with the event string.</returns>
        public static T ReceiveLiveKeyValue<T>(string eventString)
        {
            // Check if the event string is registered in the dictionary.
            if (LiveKeyDictionary.TryGetValue(eventString, out var valueProvider))
            {
                // if the value provider is null, let the user know
                if (valueProvider == null)
                {
                    UnityEngine.Debug.LogWarning("The value provider is null for the event string: " + eventString
                    + ". This may be due to a destroyed object or a missing reference.");
                    return default;
                }
                RuntimeValues.Debugging.LogLiveKeyRead(null, eventString);
                // Return the value from the provider.
                return (T)valueProvider();
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Retrieves the dead key value associated with the specified event string. Best to do a check before using this method, as this method does not return null values and will cause an exception if the string doesn't exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventString"></param>
        /// <returns></returns>
        public static T ReceiveDeadKeyValue<T>(string eventString)
        {
            // Check if the event string is registered in the dead key dictionary.
            if (DeadKeyDictionary.TryGetValue(eventString, out var value))
            {
                RuntimeValues.Debugging.LogDeadKeyRead(null, eventString);
                // Return the value from the dead key dictionary.
                return (T)value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Checks if the specified event string exists in the live key dictionary.
        /// </summary>
        /// <param name="eventString">The event string to check for.</param>
        /// <returns>True if the event string exists, otherwise false.</returns>
        public static bool DoesLiveKeyExist(string eventString)
        {
            return LiveKeyDictionary.ContainsKey(eventString);
        }

        public static bool DoesDeadKeyExist(string eventString)
        {
            return DeadKeyDictionary.ContainsKey(eventString);
        }

        public static int TotalListenerCount()
        {
            int count = 0;
            foreach (var bucket in simpleBuckets.Values)
            {
                count += bucket.simpleListeners.Count;
                count += bucket.parameterListeners.Count;
            }
            return count;
        }

        public static int LiveKeyCount()
        {
            return LiveKeyDictionary.Count;
        }

        public static int DeadKeyCount()
        {
            return DeadKeyDictionary.Count;
        }

        public static int SimpleListenersCount()
        {
            int count = 0;
            foreach (var bucket in simpleBuckets.Values)
            {
                count += bucket.simpleListeners.Count;
            }
            return count;
        }

        public static int ParameterListenersCount()
        {
            int count = 0;
            foreach (var bucket in simpleBuckets.Values)
            {
                count += bucket.parameterListeners.Count;
            }
            return count;
        }

        internal static void RegisterListener(Listener listener)
        {
            if (!simpleBuckets.TryGetValue(listener.EventName, out var bucket))
            {
                bucket = new SimpleBucket();
                simpleBuckets[listener.EventName] = bucket;
            }

            if (listener.HasSimple)
                bucket.simpleListeners.Add(listener);
            if (listener.HasArgs)
                bucket.parameterListeners.Add(listener);
        }

        internal static void UnregisterListener(Listener listener)
        {
            if (!simpleBuckets.TryGetValue(listener.EventName, out var bucket))
                return;

            if (listener.HasSimple)
                bucket.simpleListeners.Remove(listener);
            if (listener.HasArgs)
                bucket.parameterListeners.Remove(listener);

            if (bucket.simpleListeners.Count == 0 &&
                bucket.parameterListeners.Count == 0)
            {
                simpleBuckets.Remove(listener.EventName);
            }
        }

        /// <summary>
        /// Register a listener for tracking purposes
        /// </summary>
        internal static void RegisterListener(ListenerInfo info)
        {
            trackedListeners[info.uniqueId] = info;
            
            if (!listenersByEvent.ContainsKey(info.eventName))
                listenersByEvent[info.eventName] = new List<string>();
            
            listenersByEvent[info.eventName].Add(info.uniqueId);
           
        }

        /// <summary>
        /// Unregister a listener from tracking
        /// </summary>
        internal static void UnregisterListener(string uniqueId)
        {
            if (trackedListeners.TryGetValue(uniqueId, out var info))
            {
                trackedListeners.Remove(uniqueId);
                
                if (listenersByEvent.ContainsKey(info.eventName))
                {
                    listenersByEvent[info.eventName].Remove(uniqueId);
                    if (listenersByEvent[info.eventName].Count == 0)
                        listenersByEvent.Remove(info.eventName);
                }
            }
        }

        /// <summary>
        /// Get all tracked listeners
        /// </summary>
        public static List<ListenerInfo> GetAllTrackedListeners()
        {
            return new List<ListenerInfo>(trackedListeners.Values);
        }

        /// <summary>
        /// Get listeners for a specific event
        /// </summary>
        public static List<ListenerInfo> GetListenersForEvent(string eventName)
        {
            var result = new List<ListenerInfo>();
            if (listenersByEvent.ContainsKey(eventName))
            {
                foreach (var id in listenersByEvent[eventName])
                {
                    if (trackedListeners.ContainsKey(id))
                        result.Add(trackedListeners[id]);
                }
            }
            return result;
        }

        /// <summary>
        /// Get tracked listener count
        /// </summary>
        public static int TrackedListenerCount()
        {
            return trackedListeners.Count;
        }

        /// <summary>
        /// Clear all tracked listeners (for system reset)
        /// </summary>
        public static void ClearTrackedListeners()
        {
            trackedListeners.Clear();
            listenersByEvent.Clear();
        }

        /// <summary>
        /// Dispose a specific listener by its unique ID
        /// </summary>
        /// <param name="uniqueId">The unique ID of the listener to dispose</param>
        /// <returns>True if the listener was found and disposed, false otherwise</returns>
        public static bool DisposeListener(string uniqueId)
        {
            if (trackedListeners.TryGetValue(uniqueId, out var info))
            {
                // Dispose the actual listener object
                if (info.listenerObject is Listener listener)
                {
                    listener.Dispose();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a list responder exists already and if it matches the type of the list thrown into
        /// </summary>
        /// <param name="s"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool ExistingLiveKeyListMatchCheck(string s, List<UnityEngine.Object> list, out bool exists)
        {
            exists = DoesLiveKeyExist(s);

            if (!exists) { return false; }

            var existing = ReceiveLiveKeyValue<List<UnityEngine.Object>>(s);

            // Compare the type of the first element of each list to determine if they match
            var matching = list[0].GetType() == existing[0].GetType();

            return matching;
        }

        /// <summary>
        /// Check if a dead key list exists already and if it matches the type of the list thrown into
        /// </summary>
        /// <param name="s"></param>
        /// <param name="list"></param>
        /// <param name="exists"></param>
        /// <returns></returns>
        public static bool ExistingDeadKeyListMatchCheck(string s, List<UnityEngine.Object> list, out bool exists)
        {
            exists = DoesDeadKeyExist(s);

            if (!exists) { return false; }

            var existing = ReceiveDeadKeyValue<List<UnityEngine.Object>>(s);

            // Compare the type of the first element of each list to determine if they match
            var matching = list[0].GetType() == existing[0].GetType();

            return matching;
        }

        /// <summary>
        /// Adjust a live key list value.
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="packedComponents"></param>
        public static void AdjustLiveKeyList(string keyName, List<UnityEngine.Object> packedComponents)
        {
            var existing = ReceiveLiveKeyValue<List<UnityEngine.Object>>(keyName);
            var newList = new List<UnityEngine.Object>();
            newList.AddRange(existing);
            newList.AddRange(packedComponents);
            LiveKeyDictionary[keyName] = () => newList;
        }

        /// <summary>
        /// Adjust a dead key list value.
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="packedComponents"></param>
        public static void AdjustDeadKeyList(string keyName, List<UnityEngine.Object> packedComponents)
        {
            var existing = ReceiveDeadKeyValue<List<UnityEngine.Object>>(keyName);
            var newList = new List<UnityEngine.Object>();
            newList.AddRange(existing);
            newList.AddRange(packedComponents);
            DeadKeyDictionary[keyName] = newList;
        }
    }
}
