using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Arranges child GameObjects into a grid layout along a specified axis.
    /// Supports single-axis linear arrangements and two-axis grid arrangements.
    /// Works in both Edit Mode and Play Mode with optional runtime behavior control.
    /// </summary>
    [AddComponentMenu("Signalia/Tools/Signalia | GameObject Grid")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class GameObjectGrid : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// The primary axis for grid arrangement.
        /// </summary>
        public enum GridAxis
        {
            /// <summary>Arrange along the X axis (left/right).</summary>
            X,
            /// <summary>Arrange along the Y axis (up/down).</summary>
            Y,
            /// <summary>Arrange along the Z axis (forward/back).</summary>
            Z
        }

        /// <summary>
        /// Alignment of the grid relative to the parent position.
        /// </summary>
        public enum GridAlignment
        {
            /// <summary>Grid starts at the parent position.</summary>
            Start,
            /// <summary>Grid is centered on the parent position.</summary>
            Center,
            /// <summary>Grid ends at the parent position.</summary>
            End
        }

        /// <summary>
        /// Which axes should be reset to zero when arranging.
        /// </summary>
        [System.Flags]
        public enum LockAxes
        {
            None = 0,
            X = 1 << 0,
            Y = 1 << 1,
            Z = 1 << 2,
            All = X | Y | Z
        }

        #endregion

        #region Serialized Fields

        [Header("Grid Settings")]
        [Tooltip("The primary axis along which children are arranged.")]
        [SerializeField] private GridAxis primaryAxis = GridAxis.X;

        [Tooltip("Spacing between each child along the primary axis.")]
        [SerializeField] private float spacing = 1f;

        [Tooltip("If true, reverses the arrangement direction.")]
        [SerializeField] private bool flipDirection = false;

        [Tooltip("Alignment of the grid relative to the parent's position.")]
        [SerializeField] private GridAlignment alignment = GridAlignment.Center;

        [Header("Secondary Axis (2D Grid)")]
        [Tooltip("Enable to arrange children in a 2D grid with rows/columns.")]
        [SerializeField] private bool use2DGrid = false;

        [Tooltip("The secondary axis for 2D grid arrangement.")]
        [SerializeField] private GridAxis secondaryAxis = GridAxis.Z;

        [Tooltip("Number of items per row before wrapping to the next row.")]
        [SerializeField] private int itemsPerRow = 3;

        [Tooltip("Spacing between rows along the secondary axis.")]
        [SerializeField] private float rowSpacing = 1f;

        [Tooltip("If true, reverses the secondary axis direction.")]
        [SerializeField] private bool flipSecondaryDirection = false;

        [Header("Axis Locking")]
        [Tooltip("Axes to reset to zero (ignores child's current position on these axes).")]
        [SerializeField] private LockAxes lockedAxes = LockAxes.All;

        [Header("Behavior")]
        [Tooltip("If true, the grid updates in Play Mode. Otherwise, only updates in Edit Mode.")]
        [SerializeField] private bool updateInPlayMode = false;

        [Tooltip("If true, automatically updates the grid name based on settings.")]
        [SerializeField] private bool autoRename = true;

        [Tooltip("Prefix for the auto-generated name.")]
        [SerializeField] private string namePrefix = "Objects";

        #endregion

        #region Private Fields

        private readonly List<Transform> cachedChildren = new List<Transform>();

        #endregion

        #region Unity Lifecycle Methods

        private void Update()
        {
            if (Application.isPlaying && !updateInPlayMode)
            {
                return;
            }

            ArrangeChildren();
        }

        private void OnValidate()
        {
            // Ensure secondary axis is different from primary
            if (use2DGrid && secondaryAxis == primaryAxis)
            {
                secondaryAxis = GetDefaultSecondaryAxis(primaryAxis);
            }

            // Ensure items per row is at least 1
            itemsPerRow = Mathf.Max(1, itemsPerRow);
        }

        #endregion

        #region Grid Arrangement

        /// <summary>
        /// Arranges all child GameObjects according to the current grid settings.
        /// </summary>
        public void ArrangeChildren()
        {
            UpdateGameObjectName();
            CacheChildren();

            if (cachedChildren.Count == 0)
            {
                return;
            }

            float primarySpacing = flipDirection ? -spacing : spacing;
            float secondarySpacing = flipSecondaryDirection ? -rowSpacing : rowSpacing;

            for (int i = 0; i < cachedChildren.Count; i++)
            {
                Vector3 newPosition;

                if (use2DGrid)
                {
                    int column = i % itemsPerRow;
                    int row = i / itemsPerRow;

                    float primaryOffset = column * primarySpacing;
                    float secondaryOffset = row * secondarySpacing;

                    primaryOffset = ApplyAlignment(primaryOffset, itemsPerRow, primarySpacing);
                    secondaryOffset = ApplySecondaryAlignment(secondaryOffset, GetRowCount(), secondarySpacing);

                    newPosition = CalculatePosition2D(primaryOffset, secondaryOffset, cachedChildren[i]);
                }
                else
                {
                    float offset = i * primarySpacing;
                    offset = ApplyAlignment(offset, cachedChildren.Count, primarySpacing);

                    newPosition = CalculatePosition1D(offset, cachedChildren[i]);
                }

                cachedChildren[i].localPosition = newPosition;
            }
        }

        private void CacheChildren()
        {
            cachedChildren.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.gameObject.activeInHierarchy || !Application.isPlaying)
                {
                    cachedChildren.Add(child);
                }
            }
        }

        private float ApplyAlignment(float offset, int itemCount, float itemSpacing)
        {
            switch (alignment)
            {
                case GridAlignment.Start:
                    return offset;

                case GridAlignment.Center:
                    float totalLength = (itemCount - 1) * Mathf.Abs(itemSpacing);
                    return offset - (totalLength / 2f) * Mathf.Sign(itemSpacing);

                case GridAlignment.End:
                    float endOffset = (itemCount - 1) * Mathf.Abs(itemSpacing);
                    return offset - endOffset * Mathf.Sign(itemSpacing);

                default:
                    return offset;
            }
        }

        private float ApplySecondaryAlignment(float offset, int rowCount, float rowSpacing)
        {
            switch (alignment)
            {
                case GridAlignment.Start:
                    return offset;

                case GridAlignment.Center:
                    float totalHeight = (rowCount - 1) * Mathf.Abs(rowSpacing);
                    return offset - (totalHeight / 2f) * Mathf.Sign(rowSpacing);

                case GridAlignment.End:
                    float endOffset = (rowCount - 1) * Mathf.Abs(rowSpacing);
                    return offset - endOffset * Mathf.Sign(rowSpacing);

                default:
                    return offset;
            }
        }

        private Vector3 CalculatePosition1D(float offset, Transform reference)
        {
            Vector3 refPos = reference.localPosition;
            float x = IsAxisLocked(LockAxes.X) ? 0f : refPos.x;
            float y = IsAxisLocked(LockAxes.Y) ? 0f : refPos.y;
            float z = IsAxisLocked(LockAxes.Z) ? 0f : refPos.z;

            switch (primaryAxis)
            {
                case GridAxis.X:
                    return new Vector3(offset, y, z);
                case GridAxis.Y:
                    return new Vector3(x, offset, z);
                case GridAxis.Z:
                    return new Vector3(x, y, offset);
                default:
                    return Vector3.zero;
            }
        }

        private Vector3 CalculatePosition2D(float primaryOffset, float secondaryOffset, Transform reference)
        {
            Vector3 refPos = reference.localPosition;
            float x = IsAxisLocked(LockAxes.X) ? 0f : refPos.x;
            float y = IsAxisLocked(LockAxes.Y) ? 0f : refPos.y;
            float z = IsAxisLocked(LockAxes.Z) ? 0f : refPos.z;

            // Apply primary axis offset
            switch (primaryAxis)
            {
                case GridAxis.X:
                    x = primaryOffset;
                    break;
                case GridAxis.Y:
                    y = primaryOffset;
                    break;
                case GridAxis.Z:
                    z = primaryOffset;
                    break;
            }

            // Apply secondary axis offset
            switch (secondaryAxis)
            {
                case GridAxis.X:
                    x = secondaryOffset;
                    break;
                case GridAxis.Y:
                    y = secondaryOffset;
                    break;
                case GridAxis.Z:
                    z = secondaryOffset;
                    break;
            }

            return new Vector3(x, y, z);
        }

        private bool IsAxisLocked(LockAxes axis)
        {
            return (lockedAxes & axis) == axis;
        }

        private int GetRowCount()
        {
            if (cachedChildren.Count == 0) return 0;
            return Mathf.CeilToInt((float)cachedChildren.Count / itemsPerRow);
        }

        private GridAxis GetDefaultSecondaryAxis(GridAxis primary)
        {
            switch (primary)
            {
                case GridAxis.X:
                    return GridAxis.Z;
                case GridAxis.Y:
                    return GridAxis.X;
                case GridAxis.Z:
                    return GridAxis.X;
                default:
                    return GridAxis.Z;
            }
        }

        private void UpdateGameObjectName()
        {
            if (!autoRename) return;

            string gridType = use2DGrid ? "2D Grid" : $"{primaryAxis} Grid";
            gameObject.name = $"{namePrefix} [{gridType}]";
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets or sets the primary axis for arrangement.
        /// </summary>
        public GridAxis PrimaryAxis
        {
            get => primaryAxis;
            set
            {
                primaryAxis = value;
                ArrangeChildren();
            }
        }

        /// <summary>
        /// Gets or sets the spacing between children.
        /// </summary>
        public float Spacing
        {
            get => spacing;
            set
            {
                spacing = value;
                ArrangeChildren();
            }
        }

        /// <summary>
        /// Gets or sets the alignment of the grid.
        /// </summary>
        public GridAlignment Alignment
        {
            get => alignment;
            set
            {
                alignment = value;
                ArrangeChildren();
            }
        }

        /// <summary>
        /// Gets or sets whether the direction is flipped.
        /// </summary>
        public bool FlipDirection
        {
            get => flipDirection;
            set
            {
                flipDirection = value;
                ArrangeChildren();
            }
        }

        /// <summary>
        /// Gets or sets whether 2D grid mode is enabled.
        /// </summary>
        public bool Use2DGrid
        {
            get => use2DGrid;
            set
            {
                use2DGrid = value;
                ArrangeChildren();
            }
        }

        /// <summary>
        /// Gets or sets the number of items per row in 2D grid mode.
        /// </summary>
        public int ItemsPerRow
        {
            get => itemsPerRow;
            set
            {
                itemsPerRow = Mathf.Max(1, value);
                ArrangeChildren();
            }
        }

        /// <summary>
        /// Gets or sets the row spacing in 2D grid mode.
        /// </summary>
        public float RowSpacing
        {
            get => rowSpacing;
            set
            {
                rowSpacing = value;
                ArrangeChildren();
            }
        }

        /// <summary>
        /// Gets or sets whether the grid updates in play mode.
        /// </summary>
        public bool UpdateInPlayMode
        {
            get => updateInPlayMode;
            set => updateInPlayMode = value;
        }

        /// <summary>
        /// Gets the current number of arranged children.
        /// </summary>
        public int ChildCount => cachedChildren.Count;

        /// <summary>
        /// Forces an immediate refresh of the grid arrangement.
        /// </summary>
        public void Refresh()
        {
            ArrangeChildren();
        }

        #endregion
    }
}
