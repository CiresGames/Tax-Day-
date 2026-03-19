using UnityEngine;
using System.IO;

namespace AHAKuo.Signalia.Framework
{
    public static class ConfigReader
    {
        private static SignaliaConfigAsset cachedConfig;
        private const string ResourcePath = "Signalia/SigConfig"; // Path inside Resources folder
        private const string EditorPath = "Assets/Resources/Signalia/SigConfig.asset"; // Editor-only save location

        /// <summary>
        /// Loads the config from Resources. If in Editor and missing, creates a default one.
        /// </summary>
        public static SignaliaConfigAsset GetConfig(bool forceLoad = false)
        {
            if (forceLoad)
            {
                LoadConfig();
            }

            if (cachedConfig == null)
            {
                LoadConfig();
            }
            return cachedConfig;
        }

        private static void LoadConfig()
        {
            cachedConfig = Resources.Load<SignaliaConfigAsset>(ResourcePath);

#if UNITY_EDITOR
            if (cachedConfig == null)
            {
                if (!ShouldAutoCreateConfig())
                {
                    Debug.LogError("[Signalia] SignaliaConfigAsset not found in Resources/Signalia/SigConfig.asset. " +
                                 "Auto-creation is suppressed in batch mode or while the AssetDatabase is still initializing.");
                    return;
                }

                // Check if Signalia folder structure exists
                if (!Directory.Exists("Assets/Resources/Signalia"))
                {
                    Debug.LogWarning("Signalia folder structure not found. Creating default folder structure...");
                    
                    if (!Directory.Exists("Assets/Resources"))
                    {
                        Directory.CreateDirectory("Assets/Resources");
                        Debug.Log("Created Assets/Resources folder");
                    }
                    
                    Directory.CreateDirectory("Assets/Resources/Signalia");
                    Debug.Log("Created Assets/Resources/Signalia folder");
                }

                // Create a new config asset in the Resources folder if it doesn't exist (Editor Only)
                cachedConfig = ScriptableObject.CreateInstance<SignaliaConfigAsset>();

                UnityEditor.AssetDatabase.CreateAsset(cachedConfig, EditorPath);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                
                Debug.Log("Created new SignaliaConfigAsset at " + EditorPath);
            }
            #else
            {
                if (cachedConfig == null)
                {
                    Debug.LogError("SignaliaConfigAsset not found in Resources/Signalia/SigConfig.asset. " +
                                 "Please ensure the Signalia folder structure exists and contains the config asset.");
                }
            }
#endif
        }

#if UNITY_EDITOR
        private static bool ShouldAutoCreateConfig()
        {
            if (Application.isBatchMode)
            {
                return false;
            }

            if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
            {
                return false;
            }

#if UNITY_2020_1_OR_NEWER
            if (UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
            {
                return false;
            }
#endif

            return true;
        }
#endif

#if UNITY_EDITOR
        /// <summary>
        /// Saves changes made to the config asset in the Editor.
        /// </summary>
        public static void SaveConfig()
        {
            if (cachedConfig != null)
            {
                UnityEditor.EditorUtility.SetDirty(cachedConfig);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
        }
#endif
    }
}
