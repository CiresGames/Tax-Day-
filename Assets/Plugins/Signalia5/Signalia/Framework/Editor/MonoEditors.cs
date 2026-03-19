using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Radio;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System;

namespace AHAKuo.Signalia.Framework.Editors
{
    [CustomEditor(typeof(SignaliaConfigAsset)), CanEditMultipleObjects]
    public class SignaliaSettings : Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("This is the Signalia Config Asset. Click below to edit.", MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Edit Settings", GUILayout.Height(30)))
            {
                FrameworkSettings.ShowWindow();
            }
        }
    }
}
