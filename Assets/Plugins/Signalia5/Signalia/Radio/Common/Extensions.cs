using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;
using System;
using UnityEngine;

namespace AHAKuo.Signalia.Radio
{
    public static class Extensions
    {
        public static void PlayAudio(this string audioName, params IAudioPlayingSettings[] settings)
        {
            ComplexRadio.PlayAudio(audioName, settings);
        }

        public static void StopAudio(this string audioName, bool fadeOut = false, float fadeTime = 0.5f)
        {
            ComplexRadio.StopAudio(audioName, fadeOut, fadeTime);
        }

        public static Listener InitializeListener(this string eventName, Action action, bool oneShot = false, GameObject context = null)
        {
            if (eventName.IsNullOrEmpty())
            {
                return null;
            }

            return SIGS.Listener(eventName, action, oneShot, context);
        }

        public static Listener InitializeListener(this string eventName, Action<object[]> a, bool oneShot = false, GameObject context = null)
        {
            if (eventName.IsNullOrEmpty())
            {
                return null;
            }

            return SIGS.Listener(eventName, a, oneShot, context);
        }

        public static void SendEvent(this string eventName, GameObject context = null)
        {
            if (context != null)
            {
                SimpleRadio.SendEventByContext(eventName, context);
            }
            else
            {
                SIGS.Send(eventName);
            }
        }

        public static void SendEvent(this string eventName, params object[] args)
        {
            SIGS.Send(eventName, args);
        }

        public static void SendEvent(this string eventName, GameObject context, params object[] args)
        {
            if (context != null)
            {
                SimpleRadio.SendEventByContext(eventName, context, args);
            }
            else
            {
                SIGS.Send(eventName, args);
            }
        }
    }
}