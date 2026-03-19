using AHAKuo.Signalia.GameSystems.AudioLayering;
using AHAKuo.Signalia.GameSystems.ResourceCaching;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities.SIGInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AHAKuo.Signalia.Framework
{
    public static class ResourceHandler
    {
        private static AudioAsset[] _audioAssets; // cache
        private static double _lastAssetRefreshTime; // editor cache refresh tracking
        private static readonly Dictionary<AudioClip, AudioDataLoadState> _preloadedClips = new();
        private static bool _isPreloadingAudio;

        private static SignaliaActionMap[] _inputActionMaps; // cache
        private static double _lastInputActionMapsRefreshTime; // editor cache refresh tracking

        private static string[] _layerNames; // cache for layer names
        private static double _lastLayerRefreshTime; // editor cache refresh tracking

        public static AudioAsset[] GetAudioAssets()
        {
            // First, try to get audio assets from config
            var config = ConfigReader.GetConfig();
            if (config != null && config.AudioAssets != null && config.AudioAssets.Length > 0)
            {
                // Filter out null references
                var validAssets = config.AudioAssets.Where(asset => asset != null).ToArray();
                if (validAssets.Length > 0)
                {
                    _audioAssets = validAssets;
                    return validAssets;
                }
            }

            // If config has no audio assets, try to auto-populate them
            if (config != null && (config.AudioAssets == null || config.AudioAssets.Length == 0))
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Signalia] No AudioAssets assigned in config. Attempting to find and assign them automatically. " +
                               "Use 'Tools > Signalia > Load Audio Assets' to populate them manually.");
                LoadAudioAssets();
                
                // Try again after auto-population
                if (config.AudioAssets != null && config.AudioAssets.Length > 0)
                {
                    var validAssets = config.AudioAssets.Where(asset => asset != null).ToArray();
                    if (validAssets.Length > 0)
                    {
                        _audioAssets = validAssets;
                        return validAssets;
                    }
                }
#else
                Debug.LogWarning("[Signalia] No AudioAssets assigned in config. " +
                               "Use 'Tools > Signalia > Load Audio Assets' in the editor to populate them.");
#endif
            }

#if UNITY_EDITOR
            // In editor, use smart caching that refreshes periodically
            double currentTime = EditorApplication.timeSinceStartup;
            bool shouldRefresh = _audioAssets == null || 
                                (currentTime - _lastAssetRefreshTime) > 5.0; // Refresh every 5 seconds in editor
            
            if (_audioAssets != null && !shouldRefresh)
            {
                return _audioAssets;
            }
            
            // Fallback to Resources.LoadAll
            AudioAsset[] assets = Resources.LoadAll<AudioAsset>(FrameworkConstants.PATH_RESOURCE);
            _lastAssetRefreshTime = currentTime;
#else
            // In build, use simple cache
            if (_audioAssets != null)
            {
                return _audioAssets;
            }
            
            // Fallback to Resources.LoadAll
            AudioAsset[] assets = Resources.LoadAll<AudioAsset>(FrameworkConstants.PATH_RESOURCE);
#endif

            if (assets.Length > 0)
            {
                // cache
                _audioAssets = assets;
                return assets;
            }
            else
            {
#if UNITY_EDITOR
                // Check if the folder structure exists
                string resourcePath = "Assets/Resources/" + FrameworkConstants.PATH_RESOURCE;
                if (!System.IO.Directory.Exists(resourcePath))
                {
                    Debug.LogWarning($"Signalia audio folder not found at {resourcePath}. " +
                                   "Please ensure the Signalia folder structure exists in Resources.");
                }
                else
                {
                    Debug.LogWarning($"No AudioAsset files found in {resourcePath}. " +
                                   "Please add AudioAsset files to this folder.");
                }
#endif
                return null;
            }
        }

        public static AudioAsset.AudioData GetAudio(string key)
        {
            if (key.Equals(FrameworkConstants.StringConstants.NOAUDIO))
                return null;

            AudioAsset[] assets = ResourceHandler.GetAudioAssets();

            if (assets == null)
            {
                return null;
            }

            foreach (var asset in assets)
            {
                if (asset.GetListKeys.Contains(key))
                {
                    return asset.GetAudioData(key);
                }
            }

            return null;
        }

        public static AudioMixerAsset LoadAudioAsset()
        {
            var config = ConfigReader.GetConfig();

            if (config == null)
            {
                Debug.LogError("SignaliaConfigAsset is null! This usually means the Signalia folder structure is missing. " +
                             "Please ensure Assets/Resources/Signalia/ folder exists and contains SigConfig.asset");
                return null;
            }

            if (config.AudioMixerAsset == null)
            {
                Debug.LogWarning("AudioMixerAsset has not been set in the ConfigAsset. Please set it first from Tools > Signalia > Settings > Assets");
                return null;
            }

            return config.AudioMixerAsset;
        }

        public static string[] GetAudioLayeringNames()
        {
#if UNITY_EDITOR
            // In editor, use smart caching that refreshes periodically
            double currentTime = EditorApplication.timeSinceStartup;
            bool shouldRefresh = _layerNames == null || 
                                (currentTime - _lastLayerRefreshTime) > 5.0; // Refresh every 5 seconds in editor
            
            if (_layerNames != null && !shouldRefresh)
            {
                return _layerNames;
            }
            
            // Refresh cache
            var layerData = LoadAudioLayeringLayerData();
            if (layerData == null)
            {
                _layerNames = new string[0];
                _lastLayerRefreshTime = currentTime;
                return _layerNames;
            }

            var layers = layerData.GetLayersForLoading();
            _layerNames = layers.Select(layer => layer.id).ToArray();
            _lastLayerRefreshTime = currentTime;
            return _layerNames;
#else
            // In build, use simple cache
            if (_layerNames != null)
            {
                return _layerNames;
            }

            var layerData = LoadAudioLayeringLayerData();
            if (layerData == null)
            {
                return new string[0];
            }

            var layers = layerData.GetLayersForLoading();
            _layerNames = layers.Select(layer => layer.id).ToArray();
            return _layerNames;
#endif
        }

        public static AudioLayeringLayerData LoadAudioLayeringLayerData()
        {
            var config = ConfigReader.GetConfig();
            if (config == null)
            {
                Debug.LogError("SignaliaConfigAsset is null! This usually means the Signalia folder structure is missing. " +
                             "Please ensure Assets/Resources/Signalia/ folder exists and contains SigConfig.asset");
                return null;
            }
            if (config.AudioLayering.LayerData == null)
            {
                Debug.LogWarning("AudioLayeringLayerData has not been set in the ConfigAsset. Please set it first from Tools > Signalia > Settings > Assets");
                return null;
            }
            return config.AudioLayering.LayerData;
        }

        public static SignaliaActionMap[] GetInputActionMaps()
        {
            // First, try to get input action maps from config
            var config = ConfigReader.GetConfig();
            if (config != null && !config.InputSystem.EnableSignaliaInputSystem)
            {
                _inputActionMaps = Array.Empty<SignaliaActionMap>();
                return _inputActionMaps;
            }

            if (config != null && config.InputActionMaps != null && config.InputActionMaps.Length > 0)
            {
                // Filter out null references
                var validMaps = config.InputActionMaps.Where(map => map != null).ToArray();
                if (validMaps.Length > 0)
                {
                    _inputActionMaps = validMaps;
                    return validMaps;
                }
            }

            // If config has no input action maps, try to auto-populate them
            if (config != null && (config.InputActionMaps == null || config.InputActionMaps.Length == 0))
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Signalia] No InputActionMaps assigned in config. Attempting to find and assign them automatically. " +
                               "Use 'Tools > Signalia > Load Input Action Maps' to populate them manually.");
                LoadInputActionMaps();
                
                // Try again after auto-population
                if (config.InputActionMaps != null && config.InputActionMaps.Length > 0)
                {
                    var validMaps = config.InputActionMaps.Where(map => map != null).ToArray();
                    if (validMaps.Length > 0)
                    {
                        _inputActionMaps = validMaps;
                        return validMaps;
                    }
                }
#else
                Debug.LogWarning("[Signalia] No InputActionMaps assigned in config. " +
                               "Use 'Tools > Signalia > Load Input Action Maps' in the editor to populate them.");
#endif
            }

#if UNITY_EDITOR
            // In editor, use smart caching that refreshes periodically
            double currentTime = EditorApplication.timeSinceStartup;
            bool shouldRefresh = _inputActionMaps == null || 
                                (currentTime - _lastInputActionMapsRefreshTime) > 5.0; // Refresh every 5 seconds in editor
            
            if (_inputActionMaps != null && !shouldRefresh)
            {
                return _inputActionMaps;
            }
            
            // Fallback: return empty array if no maps found
            _lastInputActionMapsRefreshTime = currentTime;
#else
            // In build, use simple cache
            if (_inputActionMaps != null)
            {
                return _inputActionMaps;
            }
#endif

            // Return empty array if no maps available
            return new SignaliaActionMap[0];
        }

        public static void Clean()
        {
            _audioAssets = null;
            _lastAssetRefreshTime = 0;
            _preloadedClips.Clear();
            _isPreloadingAudio = false;

            _inputActionMaps = null;
            _lastInputActionMapsRefreshTime = 0;

            _layerNames = null;
            _lastLayerRefreshTime = 0;
        }

        public static void WarmUpAudio()
        {
            GetAudioAssets();
            GetAudioLayeringNames();
        }

        /// <summary>
        /// Preloads audio clips for every audio asset that has preloading enabled.
        /// This prevents runtime hiccups by forcing clips to load during initialization.
        /// </summary>
        public static IEnumerator PreloadConfiguredAudioAssetsAsync()
        {
            if (_isPreloadingAudio)
            {
                yield break;
            }

            _isPreloadingAudio = true;

            try
            {
                var assets = GetAudioAssets();

                if (assets == null || assets.Length == 0)
                {
                    yield break;
                }

                foreach (var asset in assets)
                {
                    if (asset == null || !asset.Preload)
                    {
                        continue;
                    }

                    foreach (var clip in asset.GetAllClips())
                    {
                        if (clip == null)
                        {
                            continue;
                        }

                        if (_preloadedClips.TryGetValue(clip, out var trackedState))
                        {
                            if (trackedState == AudioDataLoadState.Loading || trackedState == AudioDataLoadState.Loaded)
                            {
                                continue;
                            }
                        }

                        var currentState = clip.loadState;
                        if (currentState == AudioDataLoadState.Loaded)
                        {
                            _preloadedClips[clip] = AudioDataLoadState.Loaded;
                            continue;
                        }

                        if (currentState == AudioDataLoadState.Loading)
                        {
                            _preloadedClips[clip] = AudioDataLoadState.Loading;

                            while (clip.loadState == AudioDataLoadState.Loading)
                            {
                                yield return null;
                            }

                            _preloadedClips[clip] = clip.loadState;

                            if (clip.loadState != AudioDataLoadState.Loaded)
                            {
                                Debug.LogWarning($"[Signalia] Audio clip '{clip.name}' from asset '{asset.name}' finished preloading with state '{clip.loadState}'.");
                            }

                            continue;
                        }

                        if (!clip.LoadAudioData())
                        {
                            Debug.LogWarning($"[Signalia] Failed to start preloading audio clip '{clip.name}' from asset '{asset.name}'.");
                            _preloadedClips[clip] = clip.loadState;
                            continue;
                        }

                        _preloadedClips[clip] = AudioDataLoadState.Loading;

                        while (clip.loadState == AudioDataLoadState.Loading)
                        {
                            yield return null;
                        }

                        _preloadedClips[clip] = clip.loadState;

                        if (clip.loadState != AudioDataLoadState.Loaded)
                        {
                            Debug.LogWarning($"[Signalia] Audio clip '{clip.name}' from asset '{asset.name}' finished preloading with state '{clip.loadState}'.");
                        }
                    }
                }
            }
            finally
            {
                _isPreloadingAudio = false;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Finds all AudioAsset files in the Resources/Signalia folder and assigns them to the config.
        /// This method should be called when the config's AudioAssets array is empty or null.
        /// </summary>
        /// <returns>True if audio assets were found and assigned, false otherwise</returns>
        public static bool LoadAudioAssets()
        {
            var config = ConfigReader.GetConfig();
            if (config == null)
            {
                Debug.LogError("[Signalia] SignaliaConfigAsset is null! Cannot load audio assets.");
                return false;
            }

            // Find all AudioAsset files in the Resources/Signalia folder
            string resourcePath = "Assets/Resources/" + FrameworkConstants.PATH_RESOURCE;
            if (!System.IO.Directory.Exists(resourcePath))
            {
                Debug.LogWarning($"[Signalia] Signalia audio folder not found at {resourcePath}. " +
                               "Please ensure the Signalia folder structure exists in Resources.");
                return false;
            }

            // Find all AudioAsset files
            string[] guids = AssetDatabase.FindAssets("t:AudioAsset", new[] { resourcePath });
            
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[Signalia] No AudioAsset files found in {resourcePath}. " +
                               "Please add AudioAsset files to this folder.");
                return false;
            }

            // Load the AudioAsset files
            var audioAssets = new List<AudioAsset>();
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AudioAsset audioAsset = AssetDatabase.LoadAssetAtPath<AudioAsset>(assetPath);
                if (audioAsset != null)
                {
                    audioAssets.Add(audioAsset);
                }
            }

            if (audioAssets.Count == 0)
            {
                Debug.LogWarning("[Signalia] No valid AudioAsset files could be loaded.");
                return false;
            }

            // Assign to config
            config.AudioAssets = audioAssets.ToArray();
            EditorUtility.SetDirty(config);
            
            Debug.Log($"[Signalia] Successfully loaded {audioAssets.Count} AudioAsset files into config. " +
                     "This should prevent the Resources.LoadAll freeze issue.");
            
            return true;
        }
#endif

#if UNITY_EDITOR
        /// <summary>
        /// Finds all ResourceAsset files in the Resources/Signalia folder and assigns them to the config.
        /// This method should be called when the config's ResourceAssets array is empty or null.
        /// </summary>
        /// <returns>True if resource assets were found and assigned, false otherwise</returns>
        public static bool LoadResourceAssets()
        {
            var config = ConfigReader.GetConfig();
            if (config == null)
            {
                Debug.LogError("[Signalia] SignaliaConfigAsset is null! Cannot load resource assets.");
                return false;
            }

            // Find all ResourceAsset files in the Resources/Signalia folder
            string resourcePath = "Assets/Resources/" + FrameworkConstants.PATH_RESOURCE;
            if (!System.IO.Directory.Exists(resourcePath))
            {
                Debug.LogWarning($"[Signalia] Signalia resource folder not found at {resourcePath}. " +
                               "Please ensure the Signalia folder structure exists in Resources.");
                return false;
            }

            // Find all ResourceAsset files
            string[] guids = AssetDatabase.FindAssets("t:ResourceAsset", new[] { resourcePath });
            
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[Signalia] No ResourceAsset files found in {resourcePath}. " +
                               "Please add ResourceAsset files to this folder.");
                return false;
            }

            // Load the ResourceAsset files
            var resourceAssets = new List<ResourceAsset>();
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ResourceAsset resourceAsset = AssetDatabase.LoadAssetAtPath<ResourceAsset>(assetPath);
                if (resourceAsset != null)
                {
                    resourceAssets.Add(resourceAsset);
                }
            }

            if (resourceAssets.Count == 0)
            {
                Debug.LogWarning("[Signalia] No valid ResourceAsset files could be loaded.");
                return false;
            }

            // Assign to config
            config.ResourceAssets = resourceAssets.ToArray();
            EditorUtility.SetDirty(config);
            
            Debug.Log($"[Signalia] Successfully loaded {resourceAssets.Count} ResourceAsset files into config. " +
                     "This should prevent the Resources.LoadAll freeze issue.");
            
            return true;
        }

        /// <summary>
        /// Finds all SignaliaActionMap files in the project and assigns them to the config.
        /// This method should be called when the config's InputActionMaps array is empty or null.
        /// </summary>
        /// <returns>True if input action maps were found and assigned, false otherwise</returns>
        public static bool LoadInputActionMaps()
        {
            var config = ConfigReader.GetConfig();
            if (config == null)
            {
                Debug.LogError("[Signalia] SignaliaConfigAsset is null! Cannot load input action maps.");
                return false;
            }

            // Find all SignaliaActionMap files in the project
            string[] guids = AssetDatabase.FindAssets("t:SignaliaActionMap", new[] { "Assets" });
            
            if (guids.Length == 0)
            {
                Debug.LogWarning("[Signalia] No SignaliaActionMap files found in the project. " +
                               "Please create SignaliaActionMap assets first.");
                return false;
            }

            // Load the SignaliaActionMap files
            var actionMaps = new List<SignaliaActionMap>();
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                SignaliaActionMap actionMap = AssetDatabase.LoadAssetAtPath<SignaliaActionMap>(assetPath);
                if (actionMap != null)
                {
                    actionMaps.Add(actionMap);
                }
            }

            if (actionMaps.Count == 0)
            {
                Debug.LogWarning("[Signalia] No valid SignaliaActionMap files could be loaded.");
                return false;
            }

            // Assign to config
            config.InputActionMaps = actionMaps.ToArray();
            EditorUtility.SetDirty(config);
            
            Debug.Log($"[Signalia] Successfully loaded {actionMaps.Count} SignaliaActionMap files into config.");
            
            return true;
        }
#endif
    }
}
