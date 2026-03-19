using System;
using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.Radio
{
    /// <summary>
    /// Contains an array of event listeners that will do a Unity Game Event when the event is heard.
    /// </summary>
    [AddComponentMenu("Signalia/Radio/Signalia | Game Event Listener")]
    public class SimpleEventListener : MonoBehaviour
    {
        [SerializeField] private ListenerAndCallback[] listeners;

        [Serializable]
        public class ListenerAndCallback
        {
            public string eventName;
            public bool oneShot;
            public UnityEngine.Events.UnityEvent callback;

            public void InitializeListener(GameObject gameObject)
            {
                eventName.InitializeListener(() => callback.Invoke(), oneShot, gameObject);
            }
        }

        private void Awake()
        {
            foreach (var listener in listeners)
            {
                listener.InitializeListener(gameObject);
            }
        }
    }
}