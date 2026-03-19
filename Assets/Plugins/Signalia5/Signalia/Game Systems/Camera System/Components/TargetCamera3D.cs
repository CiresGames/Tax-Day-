using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.CameraSystem
{
    /// <summary>
    /// A simpler camera for 3D character. Supports first-person and third-person modes (with offshoulder offsets that auto-detect walls and adaptive zoom).
    /// Can manipulate the main camera directly or drive a transform (useful for Cinemachine virtual cameras).
    /// Target can be assigned directly or resolved from a DeadKey at runtime.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Camera/Signalia | Target Camera 3D")]
    public class TargetCamera3D : MonoBehaviour
    {
        #region Serialized Fields

        #region Drive Mode
        [Tooltip("How the camera system drives the camera. Direct Camera manipulates the main camera directly. Transform Driver updates a transform (useful for Cinemachine virtual cameras).")]
        [SerializeField] private DriveMode driveMode = DriveMode.DirectCamera;
        [Tooltip("Transform to drive when in Transform Driver mode. If null, uses this GameObject's transform.")]
        [SerializeField] private Transform driverTransform;
        [Tooltip("DeadKey name to find the driver transform. If driverTransform is null and this is set, will try to retrieve the transform from DeadKey system.")]
        [SerializeField] private string driverTransformDeadKey = "";
        #endregion

        #region Camera Mode
        [SerializeField] private CameraMode cameraMode = CameraMode.ThirdPerson;
        #endregion

        #region Target
        [SerializeField] private Transform target;
        [Tooltip("DeadKey name to find the target transform. If target is null and this is set, will try to retrieve the transform from DeadKey system at runtime.")]
        [SerializeField] private string targetDeadKey = "";
        [SerializeField] private Vector3 targetOffset = Vector3.zero;
        #endregion

        #region First Person Settings
        [SerializeField] private float firstPersonMouseSensitivity = 2f;
        [SerializeField] private float firstPersonMouseDamping = 10f;
        [SerializeField] private float firstPersonVerticalClampMin = -80f;
        [SerializeField] private float firstPersonVerticalClampMax = 80f;
        #endregion

        #region Third Person Settings
        [SerializeField] private float thirdPersonDistance = 5f;
        [SerializeField] private float thirdPersonHeight = 2f;
        [SerializeField] private Vector3 thirdPersonOffset = Vector3.zero;
        [SerializeField] private float thirdPersonMouseSensitivity = 2f;
        [SerializeField] private float thirdPersonMouseDamping = 10f;
        [SerializeField] private float thirdPersonVerticalClampMin = -80f;
        [SerializeField] private float thirdPersonVerticalClampMax = 80f;
        [SerializeField] private float thirdPersonSmoothing = 10f;
        [SerializeField] private float thirdPersonRotationSmoothing = 10f;
        [SerializeField] private bool rotateCharacterTowardMovement = true;
        [SerializeField] private float characterRotationSpeed = 10f;
        [SerializeField] private float scrollSensitivity = 1f;
        [SerializeField] private float minScrollDistance = 1f;
        [SerializeField] private float maxScrollDistance = 20f;
        #endregion

        #region Collision Detection
        [SerializeField] private LayerMask cameraCollisionLayerMask = -1;
        [SerializeField] private float cameraCollisionRadius = 0.3f;
        [SerializeField] private float minCameraDistance = 0.5f;
        #endregion

        #region Input Actions
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string scrollActionName = "Scroll";
        #endregion

        #region Debug
        [SerializeField] private bool showDebugGizmos = false;
        #endregion

        #endregion

        #region Private Fields

        #region Camera References
        private Camera cam;
        #endregion

        #region Camera State
        private float currentRotationX;
        private float currentRotationY;
        private float desiredRotationX;
        private float desiredRotationY;
        private Vector3 currentCameraPosition;
        private Vector3 desiredCameraPosition;
        private Vector3 smoothedTargetPosition;
        #endregion

        #endregion

        #region Enums
        public enum CameraMode
        {
            FirstPerson,
            ThirdPerson
        }

        public enum DriveMode
        {
            DirectCamera,
            TransformDriver
        }
        #endregion

        #region Unity Lifecycle Methods

        private void Awake()
        {
            InitializeCamera();
        }

        private void Start()
        {
            // Resolve driver transform from DeadKey if needed (DeadKey should be initialized by now)
            if (driveMode == DriveMode.TransformDriver)
            {
                // If driverTransform is still null and we have a DeadKey, try to resolve it
                if (driverTransform == null && !string.IsNullOrEmpty(driverTransformDeadKey))
                {
                    if (SIGS.DeadKeyExists(driverTransformDeadKey))
                    {
                        driverTransform = SIGS.GetDeadValue<Transform>(driverTransformDeadKey);
                        if (driverTransform != null)
                        {
                            Debug.Log($"[TargetCamera3D] Successfully found driver transform '{driverTransform.name}' using DeadKey '{driverTransformDeadKey}' on {gameObject.name}", this);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[TargetCamera3D] DeadKey '{driverTransformDeadKey}' not found. Driver transform will use this GameObject's transform.", this);
                    }
                }
                
                // Fallback to this GameObject's transform if still null (either no DeadKey specified or DeadKey lookup failed)
                if (driverTransform == null)
                {
                    driverTransform = transform;
                }
            }

            // Resolve target from DeadKey if needed
            if (target == null && !string.IsNullOrEmpty(targetDeadKey))
            {
                if (SIGS.DeadKeyExists(targetDeadKey))
                {
                    target = SIGS.GetDeadValue<Transform>(targetDeadKey);
                    if (target != null)
                    {
                        Debug.Log($"[TargetCamera3D] Successfully found target '{target.name}' using DeadKey '{targetDeadKey}' on {gameObject.name}", this);
                    }
                }
                else
                {
                    Debug.LogWarning($"[TargetCamera3D] DeadKey '{targetDeadKey}' not found. Camera will not follow a target.", this);
                }
            }
            
            InitializeTarget();
            InitializeCameraState();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            // In Transform Driver mode, we don't need a camera reference
            if (driveMode == DriveMode.DirectCamera && cam == null)
            {
                return;
            }

            // Smooth target position to avoid jitter from FixedUpdate physics
            Vector3 targetPosition = target.position + targetOffset;
            smoothedTargetPosition = Vector3.Lerp(smoothedTargetPosition, targetPosition, Time.smoothDeltaTime * 20f);

            HandleCameraInput();
            UpdateCamera();
        }

        #endregion

        #region Initialization

        private void InitializeCamera()
        {
            // Only initialize camera reference if in Direct Camera mode
            if (driveMode == DriveMode.DirectCamera)
            {
                // Find the main camera to manipulate
                cam = Camera.main;
                if (cam == null)
                {
                    // If no main camera, try to find any camera in the scene
#if UNITY_6000_0_OR_NEWER
                    cam = FindAnyObjectByType<Camera>();
#else
                    cam = FindObjectOfType<Camera>();
#endif
                    if (cam == null)
                    {
                        Debug.LogWarning($"[TargetCamera3D] No camera found in scene. Camera manipulation will not work.", this);
                    }
                }
            }

            // Initialize driver transform if in Transform Driver mode
            // Note: DeadKey lookup is deferred to Start() since DeadKey initializes in Awake()
            // and we can't guarantee initialization order. Only set fallback if no DeadKey is specified.
            if (driveMode == DriveMode.TransformDriver)
            {
                if (driverTransform == null && string.IsNullOrEmpty(driverTransformDeadKey))
                {
                    // Only set fallback if no DeadKey is specified
                    // If DeadKey is specified, it will be resolved in Start()
                    driverTransform = transform;
                }
            }
        }

        private void InitializeTarget()
        {
            // Target is resolved in Start() from targetDeadKey if null; no fallback lookup here
        }

        private void InitializeCameraState()
        {
            // Initialize camera rotation
            if (target != null)
            {
                currentRotationY = target.eulerAngles.y;
                currentRotationX = transform.eulerAngles.x;
            }
            else
            {
                currentRotationY = transform.eulerAngles.y;
                currentRotationX = transform.eulerAngles.x;
            }

            // Initialize desired rotation to match current rotation
            desiredRotationX = currentRotationX;
            desiredRotationY = currentRotationY;

            // Clamp initial rotation
            currentRotationX = ClampAngle(currentRotationX, firstPersonVerticalClampMin, firstPersonVerticalClampMax);
            desiredRotationX = ClampAngle(desiredRotationX, firstPersonVerticalClampMin, firstPersonVerticalClampMax);

            // Initialize camera position
            if (driveMode == DriveMode.DirectCamera && cam != null)
            {
                currentCameraPosition = cam.transform.position;
            }
            else if (driveMode == DriveMode.TransformDriver && driverTransform != null)
            {
                currentCameraPosition = driverTransform.position;
            }
            else
            {
                currentCameraPosition = transform.position;
            }

            // Initialize smoothed target position
            if (target != null)
            {
                smoothedTargetPosition = target.position + targetOffset;
            }
        }

        #endregion

        #region Camera Input Handling
        private void HandleCameraInput()
        {
            Vector2 lookInput = SIGS.GetInputVector2(lookActionName);

            if (cameraMode == CameraMode.FirstPerson)
            {
                // First person: update desired rotation based on input
                desiredRotationY += lookInput.x * firstPersonMouseSensitivity;
                desiredRotationX -= lookInput.y * firstPersonMouseSensitivity;
                desiredRotationX = ClampAngle(desiredRotationX, firstPersonVerticalClampMin, firstPersonVerticalClampMax);

                // Smoothly interpolate current rotation toward desired rotation
                float dampingFactor = firstPersonMouseDamping * Time.smoothDeltaTime;
                currentRotationY = Mathf.LerpAngle(currentRotationY, desiredRotationY, dampingFactor);
                currentRotationX = Mathf.LerpAngle(currentRotationX, desiredRotationX, dampingFactor);

                // Apply rotation to character (Y axis only)
                if (target != null)
                {
                    Vector3 characterRotation = target.eulerAngles;
                    characterRotation.y = currentRotationY;
                    target.rotation = Quaternion.Euler(characterRotation);
                }
            }
            else // Third Person
            {
                // Third person: update desired rotation based on input
                desiredRotationY += lookInput.x * thirdPersonMouseSensitivity;
                desiredRotationX -= lookInput.y * thirdPersonMouseSensitivity;
                desiredRotationX = ClampAngle(desiredRotationX, thirdPersonVerticalClampMin, thirdPersonVerticalClampMax);

                // Smoothly interpolate current rotation toward desired rotation
                float dampingFactor = thirdPersonMouseDamping * Time.smoothDeltaTime;
                currentRotationY = Mathf.LerpAngle(currentRotationY, desiredRotationY, dampingFactor);
                currentRotationX = Mathf.LerpAngle(currentRotationX, desiredRotationX, dampingFactor);

                // Handle scroll input for camera distance
                HandleScrollInput();
            }
        }

        private void HandleScrollInput()
        {
            // Get scroll input as float (positive for scroll up, negative for scroll down)
            float scrollValue = SIGS.GetInputFloat(scrollActionName);

            // Apply scroll input to camera distance
            if (scrollValue != 0f)
            {
                float scrollDelta = scrollValue * scrollSensitivity * Time.smoothDeltaTime;
                thirdPersonDistance += scrollDelta;
                thirdPersonDistance = Mathf.Clamp(thirdPersonDistance, minScrollDistance, maxScrollDistance);
            }
        }
        #endregion

        #region Camera Update Methods

        private void UpdateCamera()
        {
            if (cameraMode == CameraMode.FirstPerson)
            {
                UpdateFirstPersonCamera();
            }
            else
            {
                UpdateThirdPersonCamera();
            }
        }

        #endregion

        #region First Person Camera

        private void UpdateFirstPersonCamera()
        {
            Transform targetTransform = GetTargetTransform();

            // Position camera/transform at smoothed target position
            targetTransform.position = smoothedTargetPosition;

            // Rotate camera/transform based on input
            targetTransform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0f);

            // Movement uses Camera.main for direction; no additional setup needed.
        }

        #endregion

        #region Third Person Camera

        private void UpdateThirdPersonCamera()
        {
            CalculateCameraPosition();
            UpdateCameraTransform();
            UpdateCharacterController();
        }

        private void CalculateCameraPosition()
        {
            // Use smoothed target position to avoid jitter
            Vector3 offsetPosition = smoothedTargetPosition + thirdPersonOffset;

            // Calculate rotation
            Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0f);
            Vector3 direction = rotation * Vector3.back;
            
            // Calculate desired position
            desiredCameraPosition = offsetPosition + direction * thirdPersonDistance;
            desiredCameraPosition.y += thirdPersonHeight;

            // Check for camera collision (use offset position for consistency)
            Vector3 collisionAdjustedPosition = CheckCameraCollision(offsetPosition, desiredCameraPosition);

            // Smooth camera movement using smoothDeltaTime for consistent frame-rate independent movement
            currentCameraPosition = Vector3.Lerp(currentCameraPosition, collisionAdjustedPosition, thirdPersonSmoothing * Time.smoothDeltaTime);
        }

        private void UpdateCameraTransform()
        {
            Transform targetTransform = GetTargetTransform();

            // Set camera/transform position
            targetTransform.position = currentCameraPosition;

            // Make camera/transform look at offset position (consistent with position calculation)
            Vector3 offsetPosition = smoothedTargetPosition + thirdPersonOffset;
            Vector3 lookDirection = (offsetPosition - currentCameraPosition).normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                targetTransform.rotation = Quaternion.Slerp(targetTransform.rotation, lookRotation, thirdPersonRotationSmoothing * Time.smoothDeltaTime);
            }
        }

        private void UpdateCharacterController()
        {
            if (target == null)
            {
                return;
            }

            // Calculate rotation for camera forward direction
            Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0f);
            Vector3 cameraForward = rotation * Vector3.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            // Rotate character toward movement direction if enabled
            if (rotateCharacterTowardMovement)
            {
                Vector2 moveInput = SIGS.GetInputVector2(moveActionName);
                if (moveInput.sqrMagnitude > 0.01f)
                {
                    // Calculate movement direction in world space (relative to camera)
                    Vector3 camRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
                    Vector3 movementDirection = (camRight * moveInput.x + cameraForward * moveInput.y).normalized;
                    
                    if (movementDirection != Vector3.zero)
                    {
                        // Rotate character toward movement direction
                        Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
                        target.rotation = Quaternion.Slerp(target.rotation, targetRotation, characterRotationSpeed * Time.smoothDeltaTime);
                    }
                }
            }
        }

        #endregion

        #region Camera Collision Detection

        private Vector3 CheckCameraCollision(Vector3 targetPosition, Vector3 desiredPosition)
        {
            Vector3 direction = desiredPosition - targetPosition;
            float distance = direction.magnitude;
            direction.Normalize();

            // Perform sphere cast from target to desired camera position
            RaycastHit hit;
            if (Physics.SphereCast(
                targetPosition,
                cameraCollisionRadius,
                direction,
                out hit,
                distance,
                cameraCollisionLayerMask,
                QueryTriggerInteraction.Ignore))
            {
                // If we hit something, move camera closer to target
                float adjustedDistance = hit.distance - cameraCollisionRadius;
                adjustedDistance = Mathf.Max(adjustedDistance, minCameraDistance);
                return targetPosition + direction * adjustedDistance;
            }

            return desiredPosition;
        }

        #endregion

        #region Utility Methods
        private float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        /// <summary>
        /// Gets the transform to manipulate based on the current drive mode.
        /// </summary>
        private Transform GetTargetTransform()
        {
            if (driveMode == DriveMode.TransformDriver)
            {
                return driverTransform != null ? driverTransform : transform;
            }
            else
            {
                return cam != null ? cam.transform : transform;
            }
        }

        #endregion

        #region Debug/Visualization
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position + targetOffset;
            Transform targetTransform = GetTargetTransform();
            Vector3 cameraPos = Application.isPlaying ? targetTransform.position : transform.position;

            if (cameraMode == CameraMode.ThirdPerson)
            {
                // Draw camera collision sphere
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(cameraPos, cameraCollisionRadius);

                // Draw line from target to camera
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(targetPosition, cameraPos);

                // Draw desired camera position
                Gizmos.color = Color.green;
                if (Application.isPlaying)
                {
                    Gizmos.DrawWireSphere(desiredCameraPosition, cameraCollisionRadius);
                }
            }
        }
        #endregion

        #region Public Properties

        #region Mode & Target
        /// <summary>
        /// Gets or sets the camera mode.
        /// </summary>
        public CameraMode Mode
        {
            get => cameraMode;
            set => cameraMode = value;
        }

        /// <summary>
        /// Gets or sets the target transform.
        /// </summary>
        public Transform Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>
        /// Gets or sets the offset from target position for camera calculations.
        /// </summary>
        public Vector3 TargetOffset
        {
            get => targetOffset;
            set => targetOffset = value;
        }

        /// <summary>
        /// Gets or sets the drive mode.
        /// </summary>
        public DriveMode CurrentDriveMode
        {
            get => driveMode;
            set
            {
                driveMode = value;
                InitializeCamera();
            }
        }

        /// <summary>
        /// Gets or sets the driver transform (used when DriveMode is TransformDriver).
        /// </summary>
        public Transform DriverTransform
        {
            get => driverTransform;
            set => driverTransform = value;
        }
        #endregion

        #region First Person Settings
        /// <summary>
        /// Gets or sets the mouse sensitivity for first person mode.
        /// </summary>
        public float FirstPersonMouseSensitivity
        {
            get => firstPersonMouseSensitivity;
            set => firstPersonMouseSensitivity = value;
        }

        /// <summary>
        /// Gets or sets the mouse damping for first person mode.
        /// </summary>
        public float FirstPersonMouseDamping
        {
            get => firstPersonMouseDamping;
            set => firstPersonMouseDamping = value;
        }

        /// <summary>
        /// Gets or sets the minimum vertical rotation angle (looking down) in first person mode.
        /// </summary>
        public float FirstPersonVerticalClampMin
        {
            get => firstPersonVerticalClampMin;
            set => firstPersonVerticalClampMin = value;
        }

        /// <summary>
        /// Gets or sets the maximum vertical rotation angle (looking up) in first person mode.
        /// </summary>
        public float FirstPersonVerticalClampMax
        {
            get => firstPersonVerticalClampMax;
            set => firstPersonVerticalClampMax = value;
        }
        #endregion

        #region Third Person Settings
        /// <summary>
        /// Gets or sets the distance from target to camera in third person mode.
        /// </summary>
        public float ThirdPersonDistance
        {
            get => thirdPersonDistance;
            set => thirdPersonDistance = value;
        }

        /// <summary>
        /// Gets or sets the additional height offset for camera position in third person mode.
        /// </summary>
        public float ThirdPersonHeight
        {
            get => thirdPersonHeight;
            set => thirdPersonHeight = value;
        }

        /// <summary>
        /// Gets or sets the additional offset from target position in third person mode.
        /// </summary>
        public Vector3 ThirdPersonOffset
        {
            get => thirdPersonOffset;
            set => thirdPersonOffset = value;
        }

        /// <summary>
        /// Gets or sets the mouse sensitivity for third person orbit.
        /// </summary>
        public float ThirdPersonMouseSensitivity
        {
            get => thirdPersonMouseSensitivity;
            set => thirdPersonMouseSensitivity = value;
        }

        /// <summary>
        /// Gets or sets the mouse damping for third person mode.
        /// </summary>
        public float ThirdPersonMouseDamping
        {
            get => thirdPersonMouseDamping;
            set => thirdPersonMouseDamping = value;
        }

        /// <summary>
        /// Gets or sets the minimum vertical rotation angle (looking down) in third person mode.
        /// </summary>
        public float ThirdPersonVerticalClampMin
        {
            get => thirdPersonVerticalClampMin;
            set => thirdPersonVerticalClampMin = value;
        }

        /// <summary>
        /// Gets or sets the maximum vertical rotation angle (looking up) in third person mode.
        /// </summary>
        public float ThirdPersonVerticalClampMax
        {
            get => thirdPersonVerticalClampMax;
            set => thirdPersonVerticalClampMax = value;
        }

        /// <summary>
        /// Gets or sets the position smoothing speed for third person camera movement.
        /// </summary>
        public float ThirdPersonSmoothing
        {
            get => thirdPersonSmoothing;
            set => thirdPersonSmoothing = value;
        }

        /// <summary>
        /// Gets or sets the rotation smoothing speed for third person camera.
        /// </summary>
        public float ThirdPersonRotationSmoothing
        {
            get => thirdPersonRotationSmoothing;
            set => thirdPersonRotationSmoothing = value;
        }

        /// <summary>
        /// Gets or sets whether the target transform rotates toward movement direction.
        /// </summary>
        public bool RotateTowardMovement
        {
            get => rotateCharacterTowardMovement;
            set => rotateCharacterTowardMovement = value;
        }

        /// <summary>
        /// Gets or sets how fast the character rotates toward movement direction.
        /// </summary>
        public float CharacterRotationSpeed
        {
            get => characterRotationSpeed;
            set => characterRotationSpeed = value;
        }

        /// <summary>
        /// Gets or sets the scroll wheel sensitivity for zoom.
        /// </summary>
        public float ScrollSensitivity
        {
            get => scrollSensitivity;
            set => scrollSensitivity = value;
        }

        /// <summary>
        /// Gets or sets the minimum camera distance when scrolling.
        /// </summary>
        public float MinScrollDistance
        {
            get => minScrollDistance;
            set => minScrollDistance = value;
        }

        /// <summary>
        /// Gets or sets the maximum camera distance when scrolling.
        /// </summary>
        public float MaxScrollDistance
        {
            get => maxScrollDistance;
            set => maxScrollDistance = value;
        }
        #endregion

        #region Collision Settings
        /// <summary>
        /// Gets or sets the layer mask for camera collision detection.
        /// </summary>
        public LayerMask CameraCollisionLayerMask
        {
            get => cameraCollisionLayerMask;
            set => cameraCollisionLayerMask = value;
        }

        /// <summary>
        /// Gets or sets the radius of the sphere used for camera collision detection.
        /// </summary>
        public float CameraCollisionRadius
        {
            get => cameraCollisionRadius;
            set => cameraCollisionRadius = value;
        }

        /// <summary>
        /// Gets or sets the minimum distance the camera can get to the target.
        /// </summary>
        public float MinCameraDistance
        {
            get => minCameraDistance;
            set => minCameraDistance = value;
        }
        #endregion

        #region Input Action Names
        /// <summary>
        /// Gets or sets the input action name for look/rotation.
        /// </summary>
        public string LookActionName
        {
            get => lookActionName;
            set => lookActionName = value;
        }

        /// <summary>
        /// Gets or sets the input action name for movement.
        /// </summary>
        public string MoveActionName
        {
            get => moveActionName;
            set => moveActionName = value;
        }

        /// <summary>
        /// Gets or sets the input action name for scroll/zoom.
        /// </summary>
        public string ScrollActionName
        {
            get => scrollActionName;
            set => scrollActionName = value;
        }
        #endregion

        #region Debug
        /// <summary>
        /// Gets or sets whether debug gizmos are shown in the Scene view.
        /// </summary>
        public bool ShowDebugGizmos
        {
            get => showDebugGizmos;
            set => showDebugGizmos = value;
        }
        #endregion

        #endregion
    }
}

