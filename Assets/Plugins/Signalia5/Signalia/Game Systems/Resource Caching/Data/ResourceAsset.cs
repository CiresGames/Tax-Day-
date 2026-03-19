using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.ResourceCaching
{
    /// <summary>
    /// A ScriptableObject that contains a dictionary mapping string keys to Unity Objects.
    /// This allows for efficient resource loading by pre-caching assets with string identifiers.
    /// </summary>
    [CreateAssetMenu(fileName = "New Resource Asset", menuName = "Signalia/Game Systems/Resource Asset", order = 1)]
    public class ResourceAsset : ScriptableObject
    {
        [System.Serializable]
        public class ResourceEntry
        {
            [Tooltip("The key used to identify this resource")]
            public string key;
            
            [Tooltip("The Unity Object to cache")]
            public Object resource;
            
            public ResourceEntry(string key, Object resource)
            {
                this.key = key;
                this.resource = resource;
            }
        }

        [Tooltip("List of resource entries with string keys")]
        [SerializeField] private List<ResourceEntry> resources = new List<ResourceEntry>();

        private Dictionary<string, Object> _resourceDictionary;

        /// <summary>
        /// Gets the cached resource dictionary, building it if necessary
        /// </summary>
        public Dictionary<string, Object> ResourceDictionary
        {
            get
            {
                if (_resourceDictionary == null)
                {
                    BuildDictionary();
                }
                return _resourceDictionary;
            }
        }

        /// <summary>
        /// Gets a resource by its key
        /// </summary>
        /// <typeparam name="T">The type of resource to retrieve</typeparam>
        /// <param name="key">The key identifying the resource</param>
        /// <returns>The cached resource or null if not found</returns>
        public T GetResource<T>(string key) where T : Object
        {
            if (ResourceDictionary.TryGetValue(key, out var resource))
            {
                if (resource is T typedResource)
                {
                    return typedResource;
                }
                else
                {
                    Debug.LogWarning($"[Signalia Resource Caching] Resource with key '{key}' exists but is of type {resource.GetType().Name}, not {typeof(T).Name}.");
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a resource exists for the given key
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the resource exists</returns>
        public bool HasResource(string key)
        {
            return ResourceDictionary.ContainsKey(key);
        }

        /// <summary>
        /// Gets all available resource keys
        /// </summary>
        /// <returns>Array of all resource keys</returns>
        public string[] GetAllKeys()
        {
            var keys = new string[ResourceDictionary.Count];
            ResourceDictionary.Keys.CopyTo(keys, 0);
            return keys;
        }

        /// <summary>
        /// Builds the internal dictionary from the serialized list
        /// </summary>
        private void BuildDictionary()
        {
            _resourceDictionary = new Dictionary<string, Object>();
            
            foreach (var entry in resources)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.resource != null)
                {
                    _resourceDictionary[entry.key] = entry.resource;
                }
            }
        }

        /// <summary>
        /// Adds or updates a resource entry
        /// </summary>
        /// <param name="key">The key for the resource</param>
        /// <param name="resource">The resource to cache</param>
        public void AddOrUpdateResource(string key, Object resource)
        {
            if (string.IsNullOrEmpty(key) || resource == null) return;

            // Update dictionary if it exists
            if (_resourceDictionary != null)
            {
                _resourceDictionary[key] = resource;
            }

            // Update serialized list
            var existingEntry = resources.Find(r => r.key == key);
            if (existingEntry != null)
            {
                existingEntry.resource = resource;
            }
            else
            {
                resources.Add(new ResourceEntry(key, resource));
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Removes a resource entry
        /// </summary>
        /// <param name="key">The key to remove</param>
        public void RemoveResource(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            // Update dictionary if it exists
            if (_resourceDictionary != null)
            {
                _resourceDictionary.Remove(key);
            }

            // Update serialized list
            resources.RemoveAll(r => r.key == key);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void OnValidate()
        {
            // Rebuild dictionary when values change in editor
            if (_resourceDictionary != null)
            {
                BuildDictionary();
            }
        }

        public int GetCacheSize()
        {
            return resources.Count;
        }
    }
}
