#if UNITY_EDITOR
using System;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UnityEditorOnlyAttribute : PropertyAttribute
    {
    }
}
#endif
