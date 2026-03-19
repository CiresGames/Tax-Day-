using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.Radio
{
    [AddComponentMenu("Signalia/Tools/Signalia | Evention Box")]
    public class EventionBox : MonoBehaviour
    {
        [System.Serializable]
        public class EventionEvent
        {
            public string Label => label;
            [SerializeField] private string label;   
            [SerializeField] private UnityEvent unityEvent;
            [SerializeField] private string[] eventsToFire;
            [SerializeField] private string[] menusToShow;
            [SerializeField] private string[] menusToHide;
            [SerializeField] private string[] audioEvents;
            [SerializeField] private EventTiming timing = EventTiming.Manual;
            [SerializeField] private float delay;
            [SerializeField] private bool useTimeScale;
            [SerializeField] private bool createListener;
            [SerializeField] private bool once = true;

            private bool hasFired = false;
            private Tween pendingTween;
            public bool CreateListener => createListener;

            public enum EventTiming
            {
                Manual,
                Awake,
                Start,
                Enable,
                Disable,
                Destroy
            }

            public void FireEvent(EventTiming eventTiming, GameObject context = null)
            {
                if (once && hasFired) return;
                if (timing != eventTiming && eventTiming != EventTiming.Manual) return;

                pendingTween?.Kill();
                pendingTween = DOVirtual.DelayedCall(timing == EventTiming.Destroy ? 0 : delay, () => ExecuteEvents(context), !useTimeScale);
                hasFired = true;
            }

            private void ExecuteEvents(GameObject context = null)
            {
                unityEvent?.Invoke();
                eventsToFire?.ToList().ForEach(e => SimpleRadio.SendEventByContext(e, context));
                audioEvents?.ToList().ForEach(e => SIGS.PlayAudio(e));
                menusToShow?.ToList().ForEach(m => m.ShowMenu());
                menusToHide?.ToList().ForEach(m => m.HideMenu());
            }
        }

        [SerializeField] private List<EventionEvent> events = new();

        private void Awake()
        {
            FireEventsByTiming(EventionEvent.EventTiming.Awake);

            foreach (var evt in events.Where(e => e.CreateListener))
            {
                if (evt.Label.IsNullOrEmpty()) continue;

                var compoundedString = $"EB#_{evt.Label}";

                compoundedString.InitializeListener(() => evt.FireEvent(EventionEvent.EventTiming.Manual, gameObject));
            }
        }
        private void Start() => FireEventsByTiming(EventionEvent.EventTiming.Start);
        private void OnEnable() => FireEventsByTiming(EventionEvent.EventTiming.Enable);
        private void OnDisable() => FireEventsByTiming(EventionEvent.EventTiming.Disable);
        private void OnDestroy() => FireEventsByTiming(EventionEvent.EventTiming.Destroy);

        private void FireEventsByTiming(EventionEvent.EventTiming timing)
        {
            foreach (var evt in events) evt.FireEvent(timing, gameObject);
        }

        public void FireEventManuallyByIndex(int index)
        {
            if (index >= 0 && index < events.Count)
                events[index].FireEvent(EventionEvent.EventTiming.Manual, gameObject);
        }

        public void FireEventManuallyByName(string name)
        {
            foreach (var evt in events.Where(e => e.Label == name))
                evt.FireEvent(EventionEvent.EventTiming.Manual, gameObject);
        }
    }
}