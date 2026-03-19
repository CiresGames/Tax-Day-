using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using System;
using System.Linq;
using UnityEngine;

namespace AHAKuo.Signalia.UI
{
    public static class UIEventSystem
    {
        public static event Action BackButtonEvent;

        public static class UIEvents
        {
            /// <summary>
            /// Performs a back button event.
            /// </summary>
            public static void Clickback()
            {
                if (!RuntimeValues.UIConfig.ButtonsCanBeClicked) { return; } // since back is a button, it should use the same cooldowns as UIButtons.

                RuntimeValues.UIConfig.CoolDownButtons();
                BackButtonEvent?.Invoke(); // subscribers invoke

                var travelHistory = RuntimeValues.TrackedValues.TravelHistory;
                var backAudio = ConfigReader.GetConfig().ClickBackAudio;
                var shouldPlayBackAudio = (travelHistory.Empty() && ConfigReader.GetConfig().AlwaysClickBackAudio)
                || (travelHistory.HasValue());

                if (shouldPlayBackAudio)
                    backAudio.PlayAudio();

                if (travelHistory.Count > 0)
                {
                    travelHistory.LastOrDefault().HideByBackward();
                }
            }

            public static void InvokeUIView(string menu, bool show)
            {
                if (string.IsNullOrEmpty(menu)) { return; }
                var ui = MenuByName(menu);
                if (ui == null) { return; }

                if (show)
                {
                    ui.Show();
                }
                else
                {
                    ui.Hide();
                }
            }

            public static void InvokeUIViewAsPopUp(string menu, float time, bool unscaled)
            {
                if (string.IsNullOrEmpty(menu)) { return; }
                var ui = MenuByName(menu);
                if (ui == null) { return; }

                ui.ShowAsPopUp(time, unscaled);
            }

            public static bool IsUIViewVisible(string menu)
            {
                if (string.IsNullOrEmpty(menu)) { return false; }
                return RuntimeValues.TrackedValues.ViewRegistry.Any(view => view.MenuName == menu && view.IsShown);
            }

            /// <summary>
            /// Subscribe to the click event of any UI button.
            /// </summary>
            /// <param name="action"></param>
            /// <param name="oneShot"></param>
            public static void OnClickAnywhere(Action action, bool oneShot = false)
            {
                RuntimeValues.UIConfig.SubscribeToClickAnywhere(action, oneShot);
            }
        }

        public static UIView MenuByName(string menu)
        {
            if (string.IsNullOrEmpty(menu)) { return null; }
            return RuntimeValues.TrackedValues.ViewRegistry.FirstOrDefault(view => view.MenuName == menu);
        }

        public static UIButton ButtonByName(string button)
        {
            if (string.IsNullOrEmpty(button)) { return null; }
            return RuntimeValues.TrackedValues.ButtonRegistry.FirstOrDefault(btn => btn.ButtonName == button);
        }

        public static UIElement ElementByName(string element)
        {
            if (string.IsNullOrEmpty(element)) { return null; }
            return RuntimeValues.TrackedValues.ElementRegistry.FirstOrDefault(el => el.ElementName == element);
        }

        public static void Reset()
        {
            BackButtonEvent = null;
        }

        public static void DisableButtons()
        {
            RuntimeValues.UIConfig.SetButtonsDisabled(true);
        }

        public static void EnableButtons()
        {
            RuntimeValues.UIConfig.SetButtonsDisabled(false);
        }
    }
}