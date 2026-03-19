using System;
using AHAKuo.Signalia.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.Utilities.SIGInput
{
    /// <summary>
    /// Scene-friendly helper that watches a Signalia input action and fires events.
    /// Use this when you want "scene level input" without writing custom scripts:
    /// - UnityEvents: hook directly in the inspector
    /// - Signalia events: optionally broadcast through SIGS.Send (SimpleRadio)
    /// </summary>
    [AddComponentMenu("Signalia/Utilities/Input/Signalia | Scene Input Event Helper")]
    public sealed class SceneInputEventHelper : MonoBehaviour
    {
        public enum TriggerMode
        {
            Down,
            Held,
            Up,
            FloatNonZero,
            FloatChanged,
            Vector2NonZero,
            Vector2Changed
        }

        [Serializable] public sealed class FloatEvent : UnityEvent<float> { }
        [Serializable] public sealed class Vector2Event : UnityEvent<Vector2> { }

        [Tooltip("The Signalia input action name (must exist in your Signalia Action Maps). Example: Jump, Pause, Move")]
        [SerializeField] private string actionName = "Jump";

        [Tooltip("How this helper should detect/trigger the action.")]
        [SerializeField] private TriggerMode trigger = TriggerMode.Down;

        [Tooltip("For Down/Up checks: if true, consumes the edge so only one helper can react per frame.")]
        [SerializeField] private bool oneFrameConsume = true;

        [Tooltip("If true, only triggers when the action is enabled (input not disabled, action not disabled, and in an enabled action map).")]
        [SerializeField] private bool requireActionEnabled = true;

        [Tooltip("Deadzone threshold for FloatNonZero / Vector2NonZero triggers.")]
        [SerializeField] private float deadzone = 0.01f;

        [Tooltip("Delta threshold for FloatChanged / Vector2Changed triggers.")]
        [SerializeField] private float changeThreshold = 0.01f;

        [SerializeField] private UnityEvent onTriggered = new UnityEvent();
        [SerializeField] private FloatEvent onFloat = new FloatEvent();
        [SerializeField] private Vector2Event onVector2 = new Vector2Event();

        [Tooltip("If enabled, also broadcasts through Signalia Radio using SIGS.Send.")]
        [SerializeField] private bool sendSignaliaEvent;

        [Tooltip("Event key to broadcast via SIGS.Send when triggered.")]
        [SerializeField] private string signaliaEventKey;

        [Tooltip("If true, includes the action name as the first argument in SIGS.Send.")]
        [SerializeField] private bool includeActionNameArg = true;

        private bool _hasLastAnalog;
        private float _lastFloat;
        private Vector2 _lastVector2;

        private void OnEnable()
        {
            _hasLastAnalog = false;
        }

        private void Update()
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            // Avoid warning spam if no wrapper exists (SignaliaInputBridge logs warnings when read is attempted).
            if (!SignaliaInputWrapper.Exists)
            {
                return;
            }

            if (requireActionEnabled && !SIGS.IsInputActionEnabled(actionName))
            {
                return;
            }

            switch (trigger)
            {
                case TriggerMode.Down:
                    if (SIGS.GetInputDown(actionName, oneFrameConsume))
                    {
                        FireVoid();
                    }
                    break;

                case TriggerMode.Held:
                    if (SIGS.GetInput(actionName))
                    {
                        FireVoid();
                    }
                    break;

                case TriggerMode.Up:
                    if (SIGS.GetInputUp(actionName, oneFrameConsume))
                    {
                        FireVoid();
                    }
                    break;

                case TriggerMode.FloatNonZero:
                {
                    float value = SIGS.GetInputFloat(actionName);
                    if (Mathf.Abs(value) > deadzone)
                    {
                        FireFloat(value);
                    }
                    break;
                }

                case TriggerMode.FloatChanged:
                {
                    float value = SIGS.GetInputFloat(actionName);
                    bool changed = !_hasLastAnalog || Mathf.Abs(value - _lastFloat) > changeThreshold;
                    _lastFloat = value;
                    _hasLastAnalog = true;

                    if (changed)
                    {
                        FireFloat(value);
                    }
                    break;
                }

                case TriggerMode.Vector2NonZero:
                {
                    Vector2 value = SIGS.GetInputVector2(actionName);
                    if (value.sqrMagnitude > deadzone * deadzone)
                    {
                        FireVector2(value);
                    }
                    break;
                }

                case TriggerMode.Vector2Changed:
                {
                    Vector2 value = SIGS.GetInputVector2(actionName);
                    bool changed = !_hasLastAnalog || (value - _lastVector2).sqrMagnitude > changeThreshold * changeThreshold;
                    _lastVector2 = value;
                    _hasLastAnalog = true;

                    if (changed)
                    {
                        FireVector2(value);
                    }
                    break;
                }
            }
        }

        private void FireVoid()
        {
            onTriggered?.Invoke();

            if (sendSignaliaEvent && !string.IsNullOrWhiteSpace(signaliaEventKey))
            {
                if (includeActionNameArg)
                {
                    SIGS.Send(signaliaEventKey, actionName);
                }
                else
                {
                    SIGS.Send(signaliaEventKey);
                }
            }
        }

        private void FireFloat(float value)
        {
            onTriggered?.Invoke();
            onFloat?.Invoke(value);

            if (sendSignaliaEvent && !string.IsNullOrWhiteSpace(signaliaEventKey))
            {
                if (includeActionNameArg)
                {
                    SIGS.Send(signaliaEventKey, actionName, value);
                }
                else
                {
                    SIGS.Send(signaliaEventKey, value);
                }
            }
        }

        private void FireVector2(Vector2 value)
        {
            onTriggered?.Invoke();
            onVector2?.Invoke(value);

            if (sendSignaliaEvent && !string.IsNullOrWhiteSpace(signaliaEventKey))
            {
                if (includeActionNameArg)
                {
                    SIGS.Send(signaliaEventKey, actionName, value);
                }
                else
                {
                    SIGS.Send(signaliaEventKey, value);
                }
            }
        }
    }
}

