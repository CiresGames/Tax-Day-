using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Simple singleton without a persistent object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Instancer<T> : MonoBehaviour where T : Instancer<T>
    {
        private static T _instance;

        /// <summary>
        /// Gets the singleton instance of the manager.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Attempt to find an existing instance in the scene
#if UNITY_6000_0_OR_NEWER
                    _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
                    _instance = FindObjectOfType<T>();
#endif
                }
                return _instance;
            }
        }

        /// <summary>
        /// Indicates if the manager has been initialized.
        /// </summary>
        public static bool Initialized => _instance != null;

        /// <summary>
        /// Initializes the instance when the script is enabled.
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = (T)this;
            }

            if (Instance != null
                && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// Cleans up the instance when the script is disabled.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }

    /// <summary>
    /// Singleton with a persistent object that is not destroyed on load.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class InstancerSingleton<T> : Instancer<T> where T : InstancerSingleton<T>
    {
        protected override void Awake()
        {
            base.Awake();

            // Make this instance persistent across scenes
            if (Instance == this)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}