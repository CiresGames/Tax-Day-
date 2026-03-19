using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;

namespace AHAKuo.Signalia.GameSystems.Notifications
{
    /// <summary>
    /// Static methods for the notification system accessible through SIGS API.
    /// </summary>
    public static class NotificationMethods
    {
        /// <summary>
        /// Shows a notification on a SystemMessage component by name.
        /// </summary>
        /// <param name="systemMessageName">The name of the SystemMessage component</param>
        /// <param name="notificationString">The message text to display</param>
        public static void ShowNotification(string systemMessageName, string notificationString)
        {
            Watchman.Watch();

            string deadKeyName = $"SystemMessage_{systemMessageName}";
            
            if (!SimpleRadio.DoesDeadKeyExist(deadKeyName))
            {
                UnityEngine.Debug.LogWarning($"[SIGS.ShowNotification] SystemMessage '{systemMessageName}' not found. Make sure it's registered as a DeadKey.");
                return;
            }

            SystemMessage systemMessage = SimpleRadio.ReceiveDeadKeyValue<SystemMessage>(deadKeyName);
            
            if (systemMessage == null)
            {
                UnityEngine.Debug.LogWarning($"[SIGS.ShowNotification] SystemMessage '{systemMessageName}' is null.");
                return;
            }

            systemMessage.ShowNotification(notificationString);
        }

        /// <summary>
        /// Shows a burner notification at a BurnerSpot by name.
        /// </summary>
        /// <param name="burnerSpotName">The name of the BurnerSpot component</param>
        /// <param name="message">Optional message text to display on the burner</param>
        public static void ShowBurner(string burnerSpotName, string message = null)
        {
            Watchman.Watch();

            string deadKeyName = $"BurnerSpot_{burnerSpotName}";
            
            if (!SimpleRadio.DoesDeadKeyExist(deadKeyName))
            {
                UnityEngine.Debug.LogWarning($"[SIGS.ShowBurner] BurnerSpot '{burnerSpotName}' not found. Make sure it's registered as a DeadKey.");
                return;
            }

            BurnerSpot burnerSpot = SimpleRadio.ReceiveDeadKeyValue<BurnerSpot>(deadKeyName);
            
            if (burnerSpot == null)
            {
                UnityEngine.Debug.LogWarning($"[SIGS.ShowBurner] BurnerSpot '{burnerSpotName}' is null.");
                return;
            }

            burnerSpot.ShowBurner(message);
        }
    }
}

