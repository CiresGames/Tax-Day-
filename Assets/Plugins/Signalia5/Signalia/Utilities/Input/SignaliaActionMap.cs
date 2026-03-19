using System;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.Utilities.SIGInput
{
    [CreateAssetMenu(menuName = "Signalia/Input/Action Map", fileName = "SignaliaActionMap")]
    public class SignaliaActionMap : ScriptableObject
    {
        public string MapName;

        [Tooltip("Defines whether this action map should start enabled or disabled when Signalia initializes runtime values.")]
        public SignaliaActionMapInitialState InitialState = SignaliaActionMapInitialState.Enabled;
        
        [Tooltip("If this action map is enabled, input from these action maps will be suppressed (even if those maps are enabled). Use this to author action map priority.")]
        public List<SignaliaActionMap> BlockedActionMaps = new();
        public List<SignaliaActionDefinition> Actions = new();

        public bool TryGetAction(string actionName, out SignaliaActionDefinition action)
        {
            action = null;

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return false;
            }

            foreach (var entry in Actions)
            {
                if (entry != null && entry.ActionName == actionName)
                {
                    action = entry;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class SignaliaActionDefinition
    {
        public string ActionName;
        public SignaliaActionType ActionType = SignaliaActionType.Bool;
    }

    public enum SignaliaActionType
    {
        Bool,
        Float,
        Vector2
    }

    public enum SignaliaActionMapInitialState
    {
        Enabled,
        Disabled
    }
}
