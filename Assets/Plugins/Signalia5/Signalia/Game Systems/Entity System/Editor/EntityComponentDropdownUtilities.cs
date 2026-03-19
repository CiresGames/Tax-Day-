#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities.Editors
{
    public static class EntityComponentDropdownUtilities
    {
        public static IReadOnlyList<T> GetComponents<T>(EntityLogic logic) where T : EntityComponent
        {
            if (logic == null)
            {
                return new List<T>();
            }

            return logic.EntityComponents.OfType<T>().ToList();
        }

        public static string GetDisplayName(EntityComponent component)
        {
            if (component == null)
            {
                return "None";
            }

            string scriptName = ObjectNames.NicifyVariableName(component.GetType().Name);
            return string.IsNullOrWhiteSpace(component.Name) ? $"Unnamed {scriptName}" : component.Name;
        }

        public static T DrawComponentPopup<T>(Rect rect, GUIContent label, IReadOnlyList<T> components, T current) where T : EntityComponent
        {
            List<GUIContent> options = new List<GUIContent> { new GUIContent("None") };

            foreach (T component in components)
            {
                options.Add(new GUIContent(GetDisplayName(component)));
            }

            int selectedIndex = 0;
            bool hasMissing = false;

            if (current != null)
            {
                int index = IndexOf(components, current);
                if (index >= 0)
                {
                    selectedIndex = index + 1;
                }
                else
                {
                    string missingName = ObjectNames.NicifyVariableName(current.GetType().Name);
                    options.Add(new GUIContent($"Missing {missingName}"));
                    selectedIndex = options.Count - 1;
                    hasMissing = true;
                }
            }

            int newIndex = label == null
                ? EditorGUI.Popup(rect, selectedIndex, options.ToArray())
                : EditorGUI.Popup(rect, label, selectedIndex, options.ToArray());

            if (newIndex == 0)
            {
                return null;
            }

            if (newIndex <= components.Count)
            {
                return components[newIndex - 1];
            }

            return hasMissing ? current : null;
        }

        private static int IndexOf<T>(IReadOnlyList<T> components, T current) where T : EntityComponent
        {
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] == current)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
#endif
