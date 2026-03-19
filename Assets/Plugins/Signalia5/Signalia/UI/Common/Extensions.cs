using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities;
using System;
using UnityEngine;

namespace AHAKuo.Signalia.UI
{
    public static class Extensions
    {
        public static void ShowMenu(this string menuName)
        {
            UIEventSystem.UIEvents.InvokeUIView(menuName, true);
        }

        public static void HideMenu(this string menuName)
        {
            UIEventSystem.UIEvents.InvokeUIView(menuName, false);
        }

        public static void ShowPopUp(this string menuName, float time, bool unscaled)
        {
            UIEventSystem.UIEvents.InvokeUIViewAsPopUp(menuName, time, unscaled);
        }     
    }
}