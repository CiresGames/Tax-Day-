using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.UI;
using TMPro;
using System;

namespace AHAKuo.Signalia.Examples.AudioLayering
{
    public class RoomEnteringNotification : MonoBehaviour
    {
        [SerializeField] private string notificationView = "notification";
        [SerializeField] private NotificationConstants[] notifications; // not the best, but just for example
        [SerializeField] private string notificationTextField;

        [Serializable]
        private struct NotificationConstants
        {
            public string notification;
            public string listener;
        }

        private void Start()
        {
            foreach (var notification in notifications)
            {
                notification.listener.InitializeListener(() =>
                {
                    var field = SIGS.GetLiveValue<TMP_Text>(notificationTextField);
                    field.SetText($"Entered {notification.notification}");
                    notificationView.ShowPopUp(0.5f, true);
                });
            }
        }
    }
}
