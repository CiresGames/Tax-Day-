using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.SaveSystem
{
    [AddComponentMenu("Signalia/Game Systems/Save System/Signalia | Auto Save")]
    /// <summary>
    /// MonoBehaviour that handles automatic saving when the application quits or pauses.
    /// Created automatically by the GameSaving system.
    /// </summary>
    public class GameSavingAutoSave : MonoBehaviour
    {
        private void OnApplicationQuit()
        {
            GameSaving.OnApplicationQuit();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            GameSaving.OnApplicationPause(pauseStatus);
        }
    }
}
