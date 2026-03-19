using UnityEngine;

namespace AHAKuo.Signalia.Examples.AudioLayering
{
    /// <summary>
    /// A simple 2D controller that moves an object around while respecting a rectangular boundary.
    /// The boundary is defined by a center point and size, and is visualized in the Scene view.
    /// </summary>
    public class BoundaryController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private bool useWASD = true;
        [SerializeField] private bool useArrowKeys = true;
        
        [Header("Boundary Settings")]
        [SerializeField] private Vector3 boundaryCenter = Vector3.zero;
        [SerializeField] private Vector3 boundarySize = new Vector3(20f, 20f, 0f);
        [SerializeField] private bool constrainToBoundary = true;
        
        [Header("Gizmo Settings")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color boundaryColor = Color.yellow;
        [SerializeField] private Color centerColor = Color.red;
        [SerializeField] private float centerGizmoSize = 0.5f;
        
        private Vector3 lastValidPosition;
        
        private void Start()
        {
            // Initialize the last valid position to current position
            lastValidPosition = transform.position;
        }
        
        private void Update()
        {
            HandleMovement();
        }
        
        private void HandleMovement()
        {
            Vector3 inputDirection = GetInputDirection();
            
            if (inputDirection != Vector3.zero)
            {
                Vector3 movement = inputDirection * moveSpeed * Time.deltaTime;
                Vector3 newPosition = transform.position + movement;
                
                // Check if the new position is within the boundary
                if (constrainToBoundary && !IsPositionWithinBoundary(newPosition))
                {
                    // Clamp the position to the boundary edge
                    newPosition = ClampToBoundary(newPosition);
                }
                
                transform.position = newPosition;
                lastValidPosition = newPosition;
            }
        }
        
        private Vector3 GetInputDirection()
        {
            Vector3 direction = Vector3.zero;
            
            // WASD input (2D only - horizontal and vertical)
            if (useWASD)
            {
                if (Input.GetKey(KeyCode.W)) direction.y += 1f;
                if (Input.GetKey(KeyCode.S)) direction.y -= 1f;
                if (Input.GetKey(KeyCode.A)) direction.x -= 1f;
                if (Input.GetKey(KeyCode.D)) direction.x += 1f;
            }
            
            // Arrow keys input (2D only - horizontal and vertical)
            if (useArrowKeys)
            {
                if (Input.GetKey(KeyCode.UpArrow)) direction.y += 1f;
                if (Input.GetKey(KeyCode.DownArrow)) direction.y -= 1f;
                if (Input.GetKey(KeyCode.LeftArrow)) direction.x -= 1f;
                if (Input.GetKey(KeyCode.RightArrow)) direction.x += 1f;
            }
            
            return direction.normalized;
        }
        
        private bool IsPositionWithinBoundary(Vector3 position)
        {
            Vector3 halfSize = boundarySize * 0.5f;
            Vector3 minBounds = boundaryCenter - halfSize;
            Vector3 maxBounds = boundaryCenter + halfSize;
            
            return position.x >= minBounds.x && position.x <= maxBounds.x &&
                   position.y >= minBounds.y && position.y <= maxBounds.y;
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
        
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // Draw the boundary box
            Gizmos.color = boundaryColor;
            Gizmos.DrawWireCube(boundaryCenter, boundarySize);
            
            // Draw the center point
            Gizmos.color = centerColor;
            Gizmos.DrawSphere(boundaryCenter, centerGizmoSize);
            
            // Draw a line from center to current position if object is selected
            if (Application.isPlaying)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(boundaryCenter, transform.position);
                
                // Draw distance indicator
                bool withinBounds = IsPositionWithinBoundary(transform.position);
                Gizmos.color = withinBounds ? Color.green : Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }
        
        /// <summary>
        /// Sets a new boundary center and size at runtime
        /// </summary>
        /// <param name="center">New boundary center</param>
        /// <param name="size">New boundary size</param>
        public void SetBoundary(Vector3 center, Vector3 size)
        {
            boundaryCenter = center;
            boundarySize = size;
        }
        
        /// <summary>
        /// Gets the current boundary settings
        /// </summary>
        /// <returns>Tuple containing center and size</returns>
        public (Vector3 center, Vector3 size) GetBoundary()
        {
            return (boundaryCenter, boundarySize);
        }
        
        /// <summary>
        /// Teleports the object to a position within the boundary
        /// </summary>
        /// <param name="position">Target position</param>
        public void TeleportTo(Vector3 position)
        {
            if (constrainToBoundary && !IsPositionWithinBoundary(position))
            {
                position = ClampToBoundary(position);
            }
            
            transform.position = position;
            lastValidPosition = position;
        }
    }
}
