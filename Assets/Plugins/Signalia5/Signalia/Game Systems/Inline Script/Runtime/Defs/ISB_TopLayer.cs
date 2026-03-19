using System;
using System.Linq;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Shared runtime implementation for inline script types.
    /// Should not be used externally or outside scope of asset.
    /// </summary>
    [Serializable]
    public abstract class ISB_TopLayer
    {
#if UNITY_EDITOR
        [SerializeField]
        [UnityEditorOnly]
        private string _sourceCode = string.Empty;

        [SerializeField]
        [UnityEditorOnly]
        private string _additionalUsings = string.Empty;

        [SerializeField]
        [UnityEditorOnly]
        private string _cachedDefinitions = string.Empty;
#endif
        [SerializeField]
        private UnityEngine.Object _compiledScriptAsset;

        [SerializeField]
        private string _guid = string.Empty;

        public string Guid => _guid;

        protected abstract InlineScriptTypeProfile GetProfile();

        protected void ExecuteBehaviour(Action<ISB_BASE> action, string operationName)
        {
            var behaviour = InstantiateBehaviourInternal();
            if (behaviour == null)
            {
                return;
            }

            try
            {
                action?.Invoke(behaviour);
            }
            catch (Exception ex)
            {
                Debug.LogError($"InlineScript: Failed to execute behaviour '{behaviour.GetType().FullName}' during {GetType().Name}.{operationName}. {ex.Message}");
            }
        }

        protected TResult ExecuteBehaviour<TResult>(Func<ISB_BASE, TResult> func, string operationName, TResult defaultValue = default)
        {
            var behaviour = InstantiateBehaviourInternal();
            if (behaviour == null)
            {
                return defaultValue;
            }

            try
            {
                return func != null ? func(behaviour) : defaultValue;
            }
            catch (Exception ex)
            {
                Debug.LogError($"InlineScript: Failed to execute behaviour '{behaviour.GetType().FullName}' during {GetType().Name}.{operationName}. {ex.Message}");
                return defaultValue;
            }
        }

        public ISB_BASE GetPrefabBehaviour()
        {
            return InstantiateBehaviourInternal();
        }

#if UNITY_EDITOR
        public string SourceCode
        {
            get => _sourceCode;
            set => _sourceCode = value;
        }

        public string AdditionalUsings
        {
            get => _additionalUsings;
            set => _additionalUsings = value;
        }

        public string CachedDefinitions
        {
            get => _cachedDefinitions;
            set => _cachedDefinitions = value;
        }

        public void SetGuid(string guid)
        {
            _guid = guid;
        }

        public void SetCompiledScriptAsset(UnityEngine.Object scriptAsset)
        {
            _compiledScriptAsset = scriptAsset;
        }
#endif

        private ISB_BASE InstantiateBehaviourInternal()
        {
            if (_compiledScriptAsset == null)
            {
                return null;
            }

            var className = GetGeneratedClassName();
            var expectedTypeName = GetExpectedTypeName(className);
            var behaviourType = ResolveBehaviourType(expectedTypeName);
            if (behaviourType == null)
            {
                return null;
            }

            var profile = GetProfile();
            if (!profile.BehaviourBaseType.IsAssignableFrom(behaviourType))
            {
                Debug.LogError($"InlineScript: Compiled behaviour type '{behaviourType.FullName}' does not inherit from {profile.BehaviourBaseType.FullName}.");
                return null;
            }

            try
            {
                var behaviour = Activator.CreateInstance(behaviourType) as ISB_BASE;
                if (behaviour == null)
                {
                    Debug.LogError($"InlineScript: Compiled behaviour type '{behaviourType.FullName}' does not inherit from InlineScript boilerplate base.");
                    return null;
                }

                return behaviour;
            }
            catch (Exception ex)
            {
                Debug.LogError($"InlineScript: Failed to instantiate behaviour '{behaviourType.FullName}'. {ex.Message}");
                return null;
            }
        }

        private string GetGeneratedClassName()
        {
            if (!string.IsNullOrEmpty(_guid))
            {
                return $"{InlineScriptConstants.GeneratedPrefix}{_guid}";
            }

            return _compiledScriptAsset != null ? _compiledScriptAsset.name : string.Empty;
        }

        private static string GetExpectedTypeName(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return string.Empty;
            }

            return $"AHAKuo.Signalia.GameSystems.InlineScript.External.Generated.{className}";
        }

        private static Type ResolveBehaviourType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(t => t != null);
        }
    }
}
