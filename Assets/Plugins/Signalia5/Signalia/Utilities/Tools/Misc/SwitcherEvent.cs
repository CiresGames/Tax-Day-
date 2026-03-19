using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using AHAKuo.Signalia.GameSystems.SaveSystem;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.Utilities
{
    [AddComponentMenu("Signalia/Tools/Signalia | Switcher Event")]
    /// <summary>
    /// A simple switcher event that invokes different UnityEvents when switched on or off.
    /// </summary>
    public class SwitcherEvent : MonoBehaviour
    {
        [SerializeField] private UnityEvent onSwitchOn, onLoadedSwitchOn, onSwitchOff, onLoadedSwitchOff;
        [SerializeField] private string S_onSwitchOn, S_onLoadedSwitchOn, S_onSwitchOff, S_onLoadedSwitchOff;
        [SerializeField] private SwitchState startState = SwitchState.Off;
        [SerializeField] private bool consistent;
        [SerializeField] private string saveKey;
        [SerializeField] private string saveFile;

        private SwitchState currentSwitch = SwitchState.Off;

        private void Awake()
        {
            currentSwitch = startState;
            
            if (consistent)
            {
                bool state = GameSaving.Load<bool>(saveKey, GetSaveFileName(), false);
                currentSwitch = state ? SwitchState.On : SwitchState.Off;
            }
            
            if (currentSwitch == SwitchState.On)
            {
                onLoadedSwitchOn?.Invoke();
            }
            else
            {
                onLoadedSwitchOff?.Invoke();
            }
        }

        public void Switch()
        {
            if (currentSwitch == SwitchState.Off)
            {
                onSwitchOn?.Invoke();
                currentSwitch = SwitchState.On;
            }
            else
            {
                onSwitchOff?.Invoke();
                currentSwitch = SwitchState.Off;
            }

            // save
            if (consistent)
            {
                GameSaving.Save(saveKey, currentSwitch == SwitchState.On, GetSaveFileName());
            }
        }

        private string GetSaveFileName()
        {
            if (!string.IsNullOrEmpty(saveFile))
                return saveFile;
            return ConfigReader.GetConfig()?.SavingSystem?.SettingsFileName ?? "settings";
        }

        private enum SwitchState
        {
            On,
            Off
        }
    }
}