using UnityEngine;
using AHAKuo.Signalia.Framework; // For SIGS
using AHAKuo.Signalia.Framework.PackageHandlers; // For extensions

namespace AHAKuo.Signalia.GameSystems.ResourceCaching.Examples
{
    /// <summary>
    /// Comprehensive testing script for Resource Caching system.
    /// This script provides methods to test all Resource Caching functionality.
    /// Use the custom editor to access testing buttons.
    /// </summary>
    public class ResourceCachingTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("Test key for prefab resources")]
        public string prefabTestKey = "test_prefab";
        
        [Tooltip("Test key for audio resources")]
        public string audioTestKey = "test_audio";
        
        [Tooltip("Test key for sprite resources")]
        public string spriteTestKey = "test_sprite";
        
        [Tooltip("Test key for ScriptableObject resources")]
        public string scriptableObjectTestKey = "test_scriptable_object";
        
        [Tooltip("Test key for material resources")]
        public string materialTestKey = "test_material";
        
        [Tooltip("Test key for texture resources")]
        public string textureTestKey = "test_texture";

        [Header("Test Results")]
        [SerializeField, TextArea(3, 5)]
        private string lastTestResult = "No tests run yet.";

        #region SIGS Methods Testing

        /// <summary>
        /// Tests SIGS.GetResource<T>(string key) method
        /// </summary>
        public void TestSIGSGetResource()
        {
            string result = "=== SIGS.GetResource<T>(key) Tests ===\n";
            
            // Test GameObject
            GameObject prefab = SIGS.GetResource<GameObject>(prefabTestKey);
            result += $"GameObject '{prefabTestKey}': {(prefab != null ? $"Found - {prefab.name}" : "Not Found")}\n";
            
            // Test AudioClip
            AudioClip audio = SIGS.GetResource<AudioClip>(audioTestKey);
            result += $"AudioClip '{audioTestKey}': {(audio != null ? $"Found - {audio.name}" : "Not Found")}\n";
            
            // Test Sprite
            Sprite sprite = SIGS.GetResource<Sprite>(spriteTestKey);
            result += $"Sprite '{spriteTestKey}': {(sprite != null ? $"Found - {sprite.name}" : "Not Found")}\n";
            
            // Test ScriptableObject
            ScriptableObject scriptableObject = SIGS.GetResource<ScriptableObject>(scriptableObjectTestKey);
            result += $"ScriptableObject '{scriptableObjectTestKey}': {(scriptableObject != null ? $"Found - {scriptableObject.name}" : "Not Found")}\n";
            
            // Test Material
            Material material = SIGS.GetResource<Material>(materialTestKey);
            result += $"Material '{materialTestKey}': {(material != null ? $"Found - {material.name}" : "Not Found")}\n";
            
            // Test Texture
            Texture texture = SIGS.GetResource<Texture>(textureTestKey);
            result += $"Texture '{textureTestKey}': {(texture != null ? $"Found - {texture.name}" : "Not Found")}\n";
            
            lastTestResult = result;
            Debug.Log(result);
        }

        /// <summary>
        /// Tests SIGS.HasResource(string key) method
        /// </summary>
        public void TestSIGSHasResource()
        {
            string result = "=== SIGS.HasResource(key) Tests ===\n";
            
            string[] testKeys = { prefabTestKey, audioTestKey, spriteTestKey, scriptableObjectTestKey, materialTestKey, textureTestKey };
            
            foreach (string key in testKeys)
            {
                bool hasResource = SIGS.HasResource(key);
                result += $"Key '{key}': {(hasResource ? "EXISTS" : "NOT FOUND")}\n";
            }
            
            lastTestResult = result;
            Debug.Log(result);
        }

        /// <summary>
        /// Tests SIGS.GetAllResourceKeys() method
        /// </summary>
        public void TestSIGSGetAllResourceKeys()
        {
            string result = "=== SIGS.GetAllResourceKeys() Test ===\n";
            
            string[] allKeys = SIGS.GetAllResourceKeys();
            result += $"Total keys found: {allKeys.Length}\n";
            
            if (allKeys.Length > 0)
            {
                result += "Available keys:\n";
                for (int i = 0; i < allKeys.Length; i++)
                {
                    result += $"  {i + 1}. {allKeys[i]}\n";
                }
            }
            else
            {
                result += "No keys found in cache.\n";
            }
            
            lastTestResult = result;
            Debug.Log(result);
        }

        /// <summary>
        /// Tests SIGS.GetResourceCacheSize() method
        /// </summary>
        public void TestSIGSGetResourceCacheSize()
        {
            string result = "=== SIGS.GetResourceCacheSize() Test ===\n";
            
            int cacheSize = SIGS.GetResourceCacheSize();
            result += $"Total cached resources: {cacheSize}\n";
            
            lastTestResult = result;
            Debug.Log(result);
        }

        #endregion

        #region String Extension Methods Testing

        /// <summary>
        /// Tests string.GetResource<T>() extension method
        /// </summary>
        public void TestStringGetResource()
        {
            string result = "=== string.GetResource<T>() Extension Tests ===\n";
            
            // Test GameObject
            GameObject prefab = prefabTestKey.GetResource<GameObject>();
            result += $"GameObject '{prefabTestKey}': {(prefab != null ? $"Found - {prefab.name}" : "Not Found")}\n";
            
            // Test AudioClip
            AudioClip audio = audioTestKey.GetResource<AudioClip>();
            result += $"AudioClip '{audioTestKey}': {(audio != null ? $"Found - {audio.name}" : "Not Found")}\n";
            
            // Test Sprite
            Sprite sprite = spriteTestKey.GetResource<Sprite>();
            result += $"Sprite '{spriteTestKey}': {(sprite != null ? $"Found - {sprite.name}" : "Not Found")}\n";
            
            lastTestResult = result;
            Debug.Log(result);
        }

        /// <summary>
        /// Tests string.HasResource() extension method
        /// </summary>
        public void TestStringHasResource()
        {
            string result = "=== string.HasResource() Extension Tests ===\n";
            
            string[] testKeys = { prefabTestKey, audioTestKey, spriteTestKey, scriptableObjectTestKey };
            
            foreach (string key in testKeys)
            {
                bool hasResource = key.HasResource();
                result += $"Key '{key}': {(hasResource ? "EXISTS" : "NOT FOUND")}\n";
            }
            
            lastTestResult = result;
            Debug.Log(result);
        }

        #endregion

        #region GameObject Extension Methods Testing

        /// <summary>
        /// Tests GameObject.LoadAsResource<T>() extension method
        /// </summary>
        public void TestGameObjectLoadAsResource()
        {
            string result = "=== GameObject.LoadAsResource<T>() Extension Tests ===\n";
            
            // Test GameObject
            GameObject prefab = gameObject.LoadAsResource<GameObject>(prefabTestKey);
            result += $"GameObject '{prefabTestKey}': {(prefab != null ? $"Found - {prefab.name}" : "Not Found")}\n";
            
            // Test AudioClip
            AudioClip audio = gameObject.LoadAsResource<AudioClip>(audioTestKey);
            result += $"AudioClip '{audioTestKey}': {(audio != null ? $"Found - {audio.name}" : "Not Found")}\n";
            
            // Test ScriptableObject
            ScriptableObject scriptableObject = gameObject.LoadAsResource<ScriptableObject>(scriptableObjectTestKey);
            result += $"ScriptableObject '{scriptableObjectTestKey}': {(scriptableObject != null ? $"Found - {scriptableObject.name}" : "Not Found")}\n";
            
            lastTestResult = result;
            Debug.Log(result);
        }

        #endregion

        #region Comprehensive Testing

        /// <summary>
        /// Runs all tests in sequence
        /// </summary>
        public void RunAllTests()
        {
            string result = "=== COMPREHENSIVE RESOURCE CACHING TESTS ===\n";
            result += $"Test started at: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";
            
            lastTestResult = result;
            Debug.Log(result);
            
            // Run all individual tests
            TestSIGSGetResource();
            TestSIGSHasResource();
            TestSIGSGetAllResourceKeys();
            TestSIGSGetResourceCacheSize();
            TestStringGetResource();
            TestStringHasResource();
            TestGameObjectLoadAsResource();
            
            result = "=== ALL TESTS COMPLETED ===\n";
            result += $"Test completed at: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            result += "Check Console for detailed results.\n";
            
            lastTestResult = result;
            Debug.Log(result);
        }

        /// <summary>
        /// Tests resource instantiation (if GameObject is found)
        /// </summary>
        public void TestResourceInstantiation()
        {
            string result = "=== Resource Instantiation Test ===\n";
            
            GameObject prefab = SIGS.GetResource<GameObject>(prefabTestKey);
            if (prefab != null)
            {
                try
                {
                    GameObject instance = Instantiate(prefab, transform.position + Vector3.right * 2, Quaternion.identity);
                    instance.name = $"TestInstance_{System.DateTime.Now:HHmmss}";
                    result += $"Successfully instantiated '{prefab.name}' as '{instance.name}'\n";
                    result += $"Instance will be destroyed in 5 seconds.\n";
                    
                    // Destroy after 5 seconds
                    Destroy(instance, 5f);
                }
                catch (System.Exception ex)
                {
                    result += $"Failed to instantiate '{prefab.name}': {ex.Message}\n";
                }
            }
            else
            {
                result += $"Cannot instantiate - prefab '{prefabTestKey}' not found in cache.\n";
            }
            
            lastTestResult = result;
            Debug.Log(result);
        }

        /// <summary>
        /// Tests audio playback (if AudioClip is found)
        /// </summary>
        public void TestAudioPlayback()
        {
            string result = "=== Audio Playback Test ===\n";
            
            AudioClip audio = SIGS.GetResource<AudioClip>(audioTestKey);
            if (audio != null)
            {
                try
                {
                    AudioSource audioSource = GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = gameObject.AddComponent<AudioSource>();
                    }
                    
                    audioSource.clip = audio;
                    audioSource.Play();
                    result += $"Successfully playing audio '{audio.name}'\n";
                }
                catch (System.Exception ex)
                {
                    result += $"Failed to play audio '{audio.name}': {ex.Message}\n";
                }
            }
            else
            {
                result += $"Cannot play audio - clip '{audioTestKey}' not found in cache.\n";
            }
            
            lastTestResult = result;
            Debug.Log(result);
        }

        /// <summary>
        /// Clears the test result display
        /// </summary>
        public void ClearTestResults()
        {
            lastTestResult = "Test results cleared.";
            Debug.Log("Test results cleared.");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the current test result for display
        /// </summary>
        public string GetLastTestResult()
        {
            return lastTestResult;
        }

        /// <summary>
        /// Sets a custom test result
        /// </summary>
        public void SetTestResult(string result)
        {
            lastTestResult = result;
        }

        #endregion
    }
}
