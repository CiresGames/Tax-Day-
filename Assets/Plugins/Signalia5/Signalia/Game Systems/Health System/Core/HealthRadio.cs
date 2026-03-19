using System;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Health
{
    /// <summary>
    /// Dedicated radio system for Health System events. Isolated from generic Signalia radio for predictable performance.
    /// Only ObjectHealth instances may subscribe to damage events.
    /// </summary>
    public static class HealthRadio
    {
        /// <summary>
        /// Damage event payload containing all information needed for damage resolution.
        /// </summary>
        [Serializable]
        public struct DamageEvent
        {
            public float damageAmount;
            public Vector3 hitPosition;
            public float damageRadius;
            public LayerMask sourceTargetLayers;
            
            public DamageEvent(float damage, Vector3 position, LayerMask targets, float radius = 0f)
            {
                this.damageAmount = damage;
                this.hitPosition = position;
                this.sourceTargetLayers = targets;
                this.damageRadius = radius;
            }
        }

        /// <summary>
        /// Delegate for damage event listeners
        /// </summary>
        public delegate void DamageEventListener(DamageEvent damageEvent);

        /// <summary>
        /// Event fired when damage is broadcast. Only ObjectHealth components should subscribe.
        /// </summary>
        private static event DamageEventListener OnDamageEvent;

        // Listener tracking for debugging
        private static readonly Dictionary<string, ListenerInfo> trackedListeners = new();
        private static readonly List<string> listenersByEvent = new();

        /// <summary>
        /// Broadcast a damage event through the Health Radio.
        /// </summary>
        /// <param name="damageEvent">The damage event payload</param>
        public static void BroadcastDamage(DamageEvent damageEvent)
        {
            if (RuntimeValues.Debugging.IsIntrospectionEnabled)
            {
                RuntimeValues.Debugging.LogEventSend(null, $"HealthRadio.Damage({damageEvent.damageAmount})");
            }
            
            OnDamageEvent?.Invoke(damageEvent);
        }

        /// <summary>
        /// Subscribe to damage events. Only ObjectHealth components should use this.
        /// </summary>
        /// <param name="listener">The damage event listener</param>
        /// <param name="context">The GameObject context for debugging</param>
        /// <returns>Unique listener ID for unsubscription</returns>
        public static string Subscribe(DamageEventListener listener, GameObject context = null)
        {
            if (listener == null)
            {
                Debug.LogWarning("[HealthRadio] Attempted to subscribe with null listener.");
                return null;
            }

            OnDamageEvent += listener;
            
            string uniqueId = Guid.NewGuid().ToString("N")[..8];
            var info = new ListenerInfo(
                "HealthRadio.Damage",
                listener.Method?.Name ?? "Unknown",
                listener.Method?.DeclaringType?.Name ?? "Unknown",
                context != null ? context.name : "Static",
                false,
                ListenerInfo.ListenerType.SimpleEvent,
                listener
            );
            
            trackedListeners[uniqueId] = info;
            listenersByEvent.Add(uniqueId);
            
            if (RuntimeValues.Debugging.IsIntrospectionEnabled)
            {
                RuntimeValues.Debugging.LogEventCreation(context, "HealthRadio.Damage");
            }
            
            return uniqueId;
        }

        /// <summary>
        /// Unsubscribe from damage events using the unique listener ID.
        /// </summary>
        /// <param name="listenerId">The unique listener ID returned from Subscribe</param>
        /// <param name="listener">The listener to remove</param>
        public static void Unsubscribe(string listenerId, DamageEventListener listener)
        {
            if (string.IsNullOrEmpty(listenerId) || listener == null)
                return;

            OnDamageEvent -= listener;
            
            if (trackedListeners.ContainsKey(listenerId))
            {
                trackedListeners.Remove(listenerId);
                listenersByEvent.Remove(listenerId);
            }
        }

        /// <summary>
        /// Get all tracked listeners for debugging
        /// </summary>
        public static List<ListenerInfo> GetAllTrackedListeners()
        {
            return new List<ListenerInfo>(trackedListeners.Values);
        }

        /// <summary>
        /// Get the count of active listeners
        /// </summary>
        public static int ListenerCount()
        {
            return OnDamageEvent?.GetInvocationList().Length ?? 0;
        }

        /// <summary>
        /// Clean up all listeners (for system reset)
        /// </summary>
        public static void CleanUp()
        {
            OnDamageEvent = null;
            trackedListeners.Clear();
            listenersByEvent.Clear();
        }
    }
}