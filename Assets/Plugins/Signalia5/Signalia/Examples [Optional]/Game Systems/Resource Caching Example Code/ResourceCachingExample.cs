using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.PackageHandlers;
using UnityEngine.UI;

namespace AHAKuo.Signalia.GameSystems.ResourceCaching.Examples
{
    /// <summary>
    /// Example script demonstrating how to use the Resource Caching system.
    /// This shows various ways to access cached resources.
    /// </summary>
    public class ResourceCachingExample : MonoBehaviour
    {
        [Header("Example Usage")]
        [SerializeField] private string prefabKey = "player_prefab";
        [SerializeField] private string audioKey = "jump_sound";
        [SerializeField] private string spriteKey = "coin_icon";

        [SerializeField] private Transform placementPosition;
        [SerializeField] private Image coinIconDisplay;

        private void Start()
        {
            SIGS.Listener("load_assets_example", DemonstrateResourceLoading);
        }

        private void DemonstrateResourceLoading()
        {
            Debug.Log("=== Resource Caching Example ===");

            // Method 1: Using SIGS.GetResource<T>(key)
            GameObject playerPrefab = SIGS.GetResource<GameObject>(prefabKey);
            if (playerPrefab != null)
            {
                Debug.Log($"Loaded player prefab: {playerPrefab.name}");
                var _ = Instantiate(playerPrefab, placementPosition.position, Quaternion.identity);
                _.transform.SetParent(placementPosition.transform, false);
                _.transform.localPosition = Vector3.zero;
            }

            AudioClip jumpSound = SIGS.GetResource<AudioClip>(audioKey);
            if (jumpSound != null)
            {
                Debug.Log($"Loaded jump sound: {jumpSound.name}");
                // play the audio
                AudioSource.PlayClipAtPoint(jumpSound, placementPosition.position);
            }

            // Method 2: Using string extension methods
            Sprite coinIcon = spriteKey.GetResource<Sprite>();
            if (coinIcon != null)
            {
                Debug.Log($"Loaded coin icon: {coinIcon.name}");
                coinIconDisplay.sprite = coinIcon;
                coinIconDisplay.gameObject.SetActive(true);
            }

            // Method 3: Using GameObject extension method
            GameObject somePrefab = gameObject.LoadAsResource<GameObject>("enemy_prefab");
            if (somePrefab != null)
            {
                Debug.Log($"Loaded enemy prefab: {somePrefab.name}");
            }

            // Method 4: Check if resource exists
            if (SIGS.HasResource("health_potion"))
            {
                Debug.Log("Health potion resource is available!");
            }

            // Method 5: Get all available keys
            string[] allKeys = SIGS.GetAllResourceKeys();
            Debug.Log($"Total cached resources: {allKeys.Length}");
            Debug.Log($"Available keys: {string.Join(", ", allKeys)}");

            // Method 6: Get cache size
            int cacheSize = SIGS.GetResourceCacheSize();
            Debug.Log($"Resource cache size: {cacheSize}");
        }

        [ContextMenu("Test Resource Loading")]
        private void TestResourceLoading()
        {
            DemonstrateResourceLoading();
        }

        private void Update()
        {
            // Example: Load a resource dynamically based on input
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GameObject projectilePrefab = "projectile".GetResource<GameObject>();
                if (projectilePrefab != null)
                {
                    // Instantiate the cached prefab
                    Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                    Debug.Log("Fired projectile from cached resource!");
                }
                else
                {
                    Debug.LogWarning("Projectile resource not found in cache!");
                }
            }
        }
    }
}
