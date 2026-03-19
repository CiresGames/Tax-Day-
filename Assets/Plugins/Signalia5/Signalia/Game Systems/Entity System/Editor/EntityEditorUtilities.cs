#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace AHAKuo.Signalia.GameSystems.Entities.Editors
{
    /// <summary>
    /// Utility methods for Entity System editors.
    /// </summary>
    public static class EntityEditorUtilities
    {
        /// <summary>
        /// Loads the EntityLogicGraphView stylesheet for grid visibility.
        /// </summary>
        /// <returns>The loaded StyleSheet, or null if not found.</returns>
        public static StyleSheet LoadGraphViewStyleSheet()
        {
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/AHAKuo Creations/Signalia/Game Systems/Entity System/Editor/EntityNodalEditor/EntityLogicGraphView.uss");
        }
    }
}
#endif
