using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Debug component that visualizes BoxCollider boundaries using gizmos in the Scene view.
    /// Draws wireframe boxes showing the exact bounds of all BoxCollider components on this GameObject and its children.
    /// Supports filled boxes and optional text labels.
    /// </summary>
    [AddComponentMenu("Signalia/Utilities/Gizmos Debug/Signalia | Box Collider Debug")]
    [ExecuteAlways]
    public class BoxColliderDebug : MonoBehaviour
    {
        [Tooltip("Color of the gizmo wireframe. Default is yellow.")]
        [SerializeField] private Color gizmoColor = Color.yellow;

        [Tooltip("Whether to show gizmos only when the object is selected, or always visible.")]
        [SerializeField] private bool onlyWhenSelected = false;

        [Tooltip("Whether to include BoxColliders from child GameObjects.")]
        [SerializeField] private bool includeChildren = true;

        [Tooltip("Whether to draw a filled box in addition to the wireframe.")]
        [SerializeField] private bool showFill = false;

        [Tooltip("Color of the filled box. Uses gizmo color with reduced alpha if not specified.")]
        [SerializeField] private Color fillColor = new Color(1f, 1f, 0f, 0.2f);

        [Tooltip("Whether to display text labels above each BoxCollider.")]
        [SerializeField] private bool showFlyingText = false;

        [Tooltip("What information to display in the flying text.")]
        [SerializeField] private TextDisplayMode textDisplayMode = TextDisplayMode.NameAndSize;

        private BoxCollider[] boxColliders;

        private enum TextDisplayMode
        {
            NameOnly,
            SizeOnly,
            NameAndSize,
            FullInfo
        }

        private void OnEnable()
        {
            RefreshColliders();
        }

        private void OnValidate()
        {
            RefreshColliders();
        }

        private void RefreshColliders()
        {
            if (includeChildren)
            {
                boxColliders = GetComponentsInChildren<BoxCollider>();
            }
            else
            {
                boxColliders = GetComponents<BoxCollider>();
            }
        }

        private void OnDrawGizmos()
        {
            if (onlyWhenSelected) return;
            DrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (!onlyWhenSelected) return;
            DrawGizmos();
        }

        private void DrawGizmos()
        {
            if (boxColliders == null || boxColliders.Length == 0)
            {
                RefreshColliders();
            }

            if (boxColliders == null || boxColliders.Length == 0)
            {
                return;
            }

            foreach (BoxCollider boxCollider in boxColliders)
            {
                if (boxCollider == null || !boxCollider.enabled)
                {
                    continue;
                }

                Transform colliderTransform = boxCollider.transform;
                Vector3 center = colliderTransform.TransformPoint(boxCollider.center);
                Vector3 size = Vector3.Scale(boxCollider.size, colliderTransform.lossyScale);

                // Draw filled box if enabled
                if (showFill)
                {
                    Gizmos.color = fillColor;
                    Gizmos.matrix = Matrix4x4.TRS(center, colliderTransform.rotation, Vector3.one);
                    Gizmos.DrawCube(Vector3.zero, size);
                }

                // Draw wireframe box
                Gizmos.color = gizmoColor;
                Gizmos.matrix = Matrix4x4.TRS(center, colliderTransform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);

                // Draw flying text if enabled
                if (showFlyingText)
                {
#if UNITY_EDITOR
                    DrawFlyingText(boxCollider, center, size);
#endif
                }
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

#if UNITY_EDITOR
        private void DrawFlyingText(BoxCollider boxCollider, Vector3 center, Vector3 size)
        {
            string text = GetTextForCollider(boxCollider, size);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // Position text above the center of the collider
            Vector3 textPosition = center + Vector3.up * (size.y * 0.5f + 0.5f);

            // Use Handles.Label for scene view text with matching color
            Color originalColor = Handles.color;
            Handles.color = gizmoColor;
            Handles.Label(textPosition, text);
            Handles.color = originalColor;
        }

        private string GetTextForCollider(BoxCollider boxCollider, Vector3 size)
        {
            switch (textDisplayMode)
            {
                case TextDisplayMode.NameOnly:
                    return boxCollider.gameObject.name;

                case TextDisplayMode.SizeOnly:
                    return $"Size: {size.x:F2} x {size.y:F2} x {size.z:F2}";

                case TextDisplayMode.NameAndSize:
                    return $"{boxCollider.gameObject.name}\n{size.x:F2} x {size.y:F2} x {size.z:F2}";

                case TextDisplayMode.FullInfo:
                    return $"{boxCollider.gameObject.name}\nSize: {size.x:F2} x {size.y:F2} x {size.z:F2}\nCenter: {boxCollider.center}";

                default:
                    return string.Empty;
            }
        }
#endif
    }
}

