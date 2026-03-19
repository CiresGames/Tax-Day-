using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.Radio
{
    /// <summary>
    /// Sends an event using the event system.
    /// </summary>
    [AddComponentMenu("Signalia/Radio/Signalia | Game Event Sender")]
    public class SimpleEventSender : MonoBehaviour
    {
        [SerializeField] private string[] sentEvents;

        public void SendEventInScript()
        {
            foreach (var sentEvent in sentEvents)
            {
                SimpleRadio.SendEventByContext(sentEvent, gameObject);
            }
        }

        public void SendEvent(string sentEvent)
        {
            SimpleRadio.SendEventByContext(sentEvent, gameObject);
        }
    }
}