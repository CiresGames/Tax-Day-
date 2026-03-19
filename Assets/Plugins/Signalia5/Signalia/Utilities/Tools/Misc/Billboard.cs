using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Makes the GameObject face toward a target (typically the camera).
    /// Supports different billboard modes including full look-at, locked Y axis, and custom target.
    /// </summary>
    [AddComponentMenu("Signalia/Tools/Signalia | Billboard")]
    public class Billboard : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip("The target to face toward. If null, will use the main camera.")]
        [SerializeField] private Transform target;

        [Tooltip("How the billboard should orient itself.")]
        [SerializeField] private BillboardMode billboardMode = BillboardMode.FaceCamera;

        [Tooltip("If true, the billboard will only update when the target is visible to the camera.")]
        [SerializeField] private bool onlyWhenVisible = false;

        [Tooltip("Smoothing speed for rotation. Higher values = faster rotation. Set to 0 for instant rotation.")]
        [SerializeField] private float rotationSpeed = 0f;

        [Tooltip("Offset rotation applied after facing the target.")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        [Tooltip("If true, uses unscaled time for rotation smoothing.")]
        [SerializeField] private bool useUnscaledTime = false;

        #endregion

        #region Private Fields

        private Camera mainCamera;
        private Renderer objectRenderer;
        private Quaternion targetRotation;

        #endregion

        #region Enums

        /// <summary>
        /// Different modes for billboard behavior.
        /// </summary>
        public enum BillboardMode
        {
            /// <summary>
            /// Faces directly toward the target (or camera).
            /// </summary>
            FaceCamera,

            /// <summary>
            /// Faces toward the target but keeps the Y axis locked (useful for sprites that should stay upright).
            /// </summary>
            FaceCameraLockY,

            /// <summary>
            /// Faces toward a custom target transform.
            /// </summary>
            FaceTarget,

            /// <summary>
            /// Faces toward a custom target but keeps the Y axis locked.
            /// </summary>
            FaceTargetLockY
        }

        #endregion

        #region Unity Lifecycle Methods

        private void Awake()
        {
            // Cache renderer for visibility checks
            objectRenderer = GetComponent<Renderer>();

            // Cache main camera reference
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
#if UNITY_6000_0_OR_NEWER
                    mainCamera = FindAnyObjectByType<Camera>();
#else
                    mainCamera = FindObjectOfType<Camera>();
#endif
                }
            }
        }

        private void Start()
        {
            // Initialize target rotation to current rotation
            targetRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            UpdateBillboard();
        }

        #endregion

        #region Billboard Update

        private void UpdateBillboard()
        {
            // Check visibility if required
            if (onlyWhenVisible && objectRenderer != null && !objectRenderer.isVisible)
            {
                return;
            }

            // Get the target position based on billboard mode
            Vector3 targetPosition = GetTargetPosition();

            if (targetPosition == transform.position)
            {
                // Avoid look-at when positions are the same (would cause gimbal lock)
                return;
            }

            // Calculate target rotation
            Quaternion desiredRotation = CalculateRotation(targetPosition);

            // Apply rotation offset
            if (rotationOffset != Vector3.zero)
            {
                desiredRotation *= Quaternion.Euler(rotationOffset);
            }

            // Apply rotation with optional smoothing
            if (rotationSpeed > 0f)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                targetRotation = Quaternion.Slerp(targetRotation, desiredRotation, rotationSpeed * deltaTime);
                transform.rotation = targetRotation;
            }
            else
            {
                transform.rotation = desiredRotation;
                targetRotation = desiredRotation;
            }
        }

        private Vector3 GetTargetPosition()
        {
            switch (billboardMode)
            {
                case BillboardMode.FaceCamera:
                case BillboardMode.FaceCameraLockY:
                    if (mainCamera != null)
                    {
                        return mainCamera.transform.position;
                    }
                    break;

                case BillboardMode.FaceTarget:
                case BillboardMode.FaceTargetLockY:
                    if (target != null)
                    {
                        return target.position;
                    }
                    // Fallback to camera if target is null
                    if (mainCamera != null)
                    {
                        return mainCamera.transform.position;
                    }
                    break;
            }

            // Fallback: return current position (no rotation change)
            return transform.position;
        }

        private Quaternion CalculateRotation(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;

            switch (billboardMode)
            {
                case BillboardMode.FaceCamera:
                case BillboardMode.FaceTarget:
                    // Full look-at rotation
                    if (direction != Vector3.zero)
                    {
                        return Quaternion.LookRotation(direction);
                    }
                    break;

                case BillboardMode.FaceCameraLockY:
                case BillboardMode.FaceTargetLockY:
                    // Look-at with locked Y axis
                    direction.y = 0f;
                    if (direction != Vector3.zero)
                    {
                        return Quaternion.LookRotation(direction);
                    }
                    break;
            }

            // Fallback: return current rotation
            return transform.rotation;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the target transform to face toward.
        /// </summary>
        /// <param name="newTarget">The target transform. Pass null to use camera instead.</param>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Sets the billboard mode.
        /// </summary>
        /// <param name="mode">The billboard mode to use.</param>
        public void SetBillboardMode(BillboardMode mode)
        {
            billboardMode = mode;
        }

        /// <summary>
        /// Forces an immediate update of the billboard rotation (bypasses smoothing).
        /// </summary>
        public void ForceUpdate()
        {
            Vector3 targetPosition = GetTargetPosition();
            if (targetPosition != transform.position)
            {
                Quaternion desiredRotation = CalculateRotation(targetPosition);
                if (rotationOffset != Vector3.zero)
                {
                    desiredRotation *= Quaternion.Euler(rotationOffset);
                }
                transform.rotation = desiredRotation;
                targetRotation = desiredRotation;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the target transform to face toward.
        /// </summary>
        public Transform Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>
        /// Gets or sets the billboard mode.
        /// </summary>
        public BillboardMode Mode
        {
            get => billboardMode;
            set => billboardMode = value;
        }

        /// <summary>
        /// Gets or sets whether the billboard only updates when visible.
        /// </summary>
        public bool OnlyWhenVisible
        {
            get => onlyWhenVisible;
            set => onlyWhenVisible = value;
        }

        /// <summary>
        /// Gets or sets the rotation smoothing speed. Set to 0 for instant rotation.
        /// </summary>
        public float RotationSpeed
        {
            get => rotationSpeed;
            set => rotationSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the rotation offset applied after facing the target.
        /// </summary>
        public Vector3 RotationOffset
        {
            get => rotationOffset;
            set => rotationOffset = value;
        }

        #endregion
    }
}

