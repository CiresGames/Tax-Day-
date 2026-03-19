using UnityEngine;

namespace AHAKuo.Signalia.Examples.AudioLayering
{
    /// <summary>
    /// A camera controller that smoothly follows a target object (typically the player).
    /// Provides smooth interpolation, offset positioning, and boundary constraints.
    /// </summary>
    public class CameraFollower : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private string playerTag = "Player";
        
        [Header("Follow Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 5, -10);
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private bool smoothFollow = true;
        [SerializeField] private bool lookAtTarget = false;
        [SerializeField] private float lookAtSpeed = 2f;
        
        [Header("Boundary Constraints")]
        [SerializeField] private bool useBoundaryConstraints = false;
        [SerializeField] private Vector3 boundaryCenter = Vector3.zero;
        [SerializeField] private Vector3 boundarySize = new Vector3(30f, 30f, 0f);
        
        [Header("Advanced Settings")]
        [SerializeField] private bool followOnX = true;
        [SerializeField] private bool followOnY = true;
        [SerializeField] private bool followOnZ = false;
        [SerializeField] private float deadZone = 0.1f;
        
        [Header("Gizmo Settings")]
        [SerializeField] private Color offsetColor = Color.blue;
        [SerializeField] private Color boundaryColor = Color.cyan;
        [SerializeField] private bool showGizmos = true;
        
        private Vector3 targetPosition;
        private Vector3 velocity = Vector3.zero;
        private Camera cam;
        
        private void Start()
        {
            cam = GetComponent<Camera>();
            
            // Auto-find player if enabled and no target is set
            if (autoFindPlayer && target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null)
                {
                    target = player.transform;
                }
                else
                {
                    Debug.LogWarning($"CameraFollower: No GameObject with tag '{playerTag}' found. Please assign a target manually.");
                }
            }
            
            // Initialize camera position
            if (target != null)
            {
                UpdateTargetPosition();
                transform.position = targetPosition;
            }
        }
        
        private void LateUpdate()
        {
            if (target == null) return;
            
            UpdateTargetPosition();
            MoveCamera();
            
            if (lookAtTarget)
            {
                LookAtTarget();
            }
        }
        
        private void UpdateTargetPosition()
        {
            Vector3 desiredPosition = target.position + offset;
            
            // Apply axis constraints
            Vector3 currentPos = transform.position;
            if (!followOnX) desiredPosition.x = currentPos.x;
            if (!followOnY) desiredPosition.y = currentPos.y;
            if (!followOnZ) desiredPosition.z = currentPos.z;
            
            // Apply boundary constraints
            if (useBoundaryConstraints)
            {
                desiredPosition = ClampToBoundary(desiredPosition);
            }
            
            targetPosition = desiredPosition;
        }
        
        private void MoveCamera()
        {
            Vector3 currentPos = transform.position;
            Vector3 direction = targetPosition - currentPos;
            
            // Check if we're within dead zone
            if (direction.magnitude <= deadZone)
            {
                return;
            }
            
            if (smoothFollow)
            {
                // Smooth interpolation using SmoothDamp
                transform.position = Vector3.SmoothDamp(currentPos, targetPosition, ref velocity, 1f / followSpeed);
            }
            else
            {
                // Direct movement
                transform.position = Vector3.MoveTowards(currentPos, targetPosition, followSpeed * Time.deltaTime);
            }
        }
        
        private void LookAtTarget()
        {
            Vector3 lookDirection = target.position - transform.position;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
            }
        }
        
        private Vector3 ClampToBoundary(Vector3 position)
        {
            Vector3 halfSize = boundarySize * 0.5f;
            Vector3 minBounds = boundaryCenter - halfSize;
            Vector3 maxBounds = boundaryCenter + halfSize;
            
            position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
            position.y = Mathf.Clamp(position.y, minBounds.y, maxBounds.y);
            
            return position;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            
            if (target == null) return;
            
            // Draw offset visualization
            Gizmos.color = offsetColor;
            Vector3 offsetPosition = target.position + offset;
            Gizmos.DrawWireSphere(offsetPosition, 0.5f);
            Gizmos.DrawLine(target.position, offsetPosition);
            
            // Draw boundary if enabled
            if (useBoundaryConstraints)
            {
                Gizmos.color = boundaryColor;
                Gizmos.DrawWireCube(boundaryCenter, boundarySize);
                
                // Draw center point
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(boundaryCenter, 0.3f);
            }
            
            // Draw camera's current position and target position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
            
            // Draw line from camera to target position
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        
        /// <summary>
        /// Sets a new target to follow
        /// </summary>
        /// <param name="newTarget">The new target transform</param>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        
        /// <summary>
        /// Sets the camera offset
        /// </summary>
        /// <param name="newOffset">New offset vector</param>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }
        
        /// <summary>
        /// Sets the follow speed
        /// </summary>
        /// <param name="speed">New follow speed</param>
        public void SetFollowSpeed(float speed)
        {
            followSpeed = speed;
        }
        
        /// <summary>
        /// Enables or disables boundary constraints
        /// </summary>
        /// <param name="enabled">Whether to enable boundary constraints</param>
        /// <param name="center">Boundary center (optional)</param>
        /// <param name="size">Boundary size (optional)</param>
        public void SetBoundaryConstraints(bool enabled, Vector3? center = null, Vector3? size = null)
        {
            useBoundaryConstraints = enabled;
            if (center.HasValue) boundaryCenter = center.Value;
            if (size.HasValue) boundarySize = size.Value;
        }
        
        /// <summary>
        /// Instantly snaps the camera to the target position
        /// </summary>
        public void SnapToTarget()
        {
            if (target != null)
            {
                UpdateTargetPosition();
                transform.position = targetPosition;
            }
        }
        
        /// <summary>
        /// Gets the current target being followed
        /// </summary>
        /// <returns>The target transform, or null if none</returns>
        public Transform GetTarget()
        {
            return target;
        }
    }
}
