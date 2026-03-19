using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.SaveSystem.Examples
{
    /// <summary>
    /// Move around in WASD without gravity or physics. Just for testing.
    /// </summary>
    public class SimplePlayerController : MonoBehaviour
    {
        public Vector2 xRange;
        public Vector2 zRange;
        public float maxY;
        public float speed = 5f;

        void Update()
        {
            Vector3 move = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
            {
                move += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S))
            {
                move += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A))
            {
                move += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D))
            {
                move += Vector3.right;
            }

            var calc = transform.localPosition + move.normalized * speed * Time.deltaTime;

            // Clamp position within specified ranges
            calc.x = Mathf.Clamp(calc.x, xRange.x, xRange.y);
            calc.z = Mathf.Clamp(calc.z, zRange.x, zRange.y);

            // Set the Y position to maxY
            calc.y = maxY;

            transform.localPosition = calc;
        }
    }
}