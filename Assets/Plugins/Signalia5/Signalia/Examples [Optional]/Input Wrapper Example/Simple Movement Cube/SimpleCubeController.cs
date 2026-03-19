using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace Signalia.Examples
{
    /// <summary>
    /// Bare bones controller for moving a cube using the Move input action.
    /// </summary>
    public class SimpleCubeController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private void Update()
        {
            Vector2 moveInput = SIGS.GetInputVector2("Move");
            
            Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed * Time.deltaTime;
            transform.position += movement;
        }
    }
}

