#if UNITY_EDITOR
using AHAKuo.Signalia.GameSystems.InlineScript;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.InlineScript.Internal.Editor
{
    public static class InlineScriptEditorHelper
    {
        private static readonly Dictionary<string, string> LastCompiledSources = new Dictionary<string, string>();

        private const string ClipboardPrefix = "InlineScriptClipboard|";
        private static InlineScriptClipboardData? _clipboardCache;
        
        private static readonly string[] ControlKeywords =
        {
            "if", "else", "for", "foreach", "while", "switch", "case", "default", "try", "catch", "finally", "do", "using"
        };

        public enum CompileStatus
        {
            Unknown,
            Pending,
            Dirty,
            Compiled
        }

        [Serializable]
        private struct InlineScriptClipboardData
        {
            public string SourceCode;
            public string AdditionalUsings;
            public string CachedDefinitions;
            public string Guid;
            public string ScriptAssetGuid;

            public bool IsValid => !string.IsNullOrEmpty(SourceCode) ||
                                    !string.IsNullOrEmpty(AdditionalUsings) ||
                                    !string.IsNullOrEmpty(CachedDefinitions) ||
                                    !string.IsNullOrEmpty(Guid) ||
                                    !string.IsNullOrEmpty(ScriptAssetGuid);
        }

        /// <summary>
        /// Compiles an inline snippet into a MonoScript asset
        /// </summary>
        public static void CompileInlineSnippet(SerializedObject serializedObject, SerializedProperty sourceProp,
            SerializedProperty compiledScriptProp, SerializedProperty guidProp,
            SerializedProperty additionalUsingsProp, SerializedProperty cachedDefinitionsProp, InlineScriptTypeProfile profile)
        {
            InlineCompiler.CompileInlineSnippet(serializedObject, sourceProp, compiledScriptProp, guidProp,
                additionalUsingsProp, cachedDefinitionsProp, profile);
        }

        /// <summary>
        /// Builds a unique property key for tracking compilation state
        /// </summary>
        public static string BuildPropertyKey(SerializedProperty property)
        {
            var targetId = property.serializedObject?.targetObject != null
                ? property.serializedObject.targetObject.GetInstanceID().ToString()
                : "global";
            
            // Include the GUID to ensure uniqueness per inline script instance
            var guidProp = property.FindPropertyRelative("_guid");
            var guid = guidProp?.stringValue ?? "noguid";
            
            return $"{targetId}:{property.propertyPath}:{guid}";
        }

        /// <summary>
        /// Determines the compilation status of a property
        /// </summary>
        public static CompileStatus DetermineCompileStatus(string key, SerializedProperty sourceProp,
            SerializedProperty compiledScriptProp, SerializedProperty additionalUsingsProp,
            SerializedProperty cachedDefinitionsProp)
        {
            if (sourceProp == null)
            {
                return CompileStatus.Unknown;
            }

            var hasScript = compiledScriptProp != null && compiledScriptProp.objectReferenceValue != null;
            if (!hasScript)
            {
                return CompileStatus.Pending;
            }

            var sourceCode = GetPropertyString(sourceProp);
            var additionalUsings = GetPropertyString(additionalUsingsProp);
            var cachedDefinitions = GetPropertyString(cachedDefinitionsProp);
            
            string globalUsings = string.Empty;
            try
            {
                var config = AHAKuo.Signalia.Framework.ConfigReader.GetConfig();
                globalUsings = config?.InlineScript?.GlobalUsings ?? string.Empty;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[InlineScript] Failed to load global usings from Signalia config: {ex.Message}");
            }
            
            var normalizedSource = InlineCompiler.NormalizeComposite(sourceCode, additionalUsings, cachedDefinitions, globalUsings);

            if (LastCompiledSources.TryGetValue(key, out var cachedSource))
            {
                if (string.Equals(cachedSource, normalizedSource, StringComparison.Ordinal))
                {
                    return CompileStatus.Compiled;
                }

                return CompileStatus.Dirty;
            }

            if (InlineCompiler.TryExtractCompiledSource(compiledScriptProp, out var compiledSourceFromFile))
            {
                var compiledNormalized = InlineCompiler.NormalizeComposite(compiledSourceFromFile.UserCode,
                    compiledSourceFromFile.AdditionalUsings, compiledSourceFromFile.CachedDefinitions,
                    compiledSourceFromFile.GlobalUsings);
                LastCompiledSources[key] = compiledNormalized;
                if (string.Equals(compiledNormalized, normalizedSource, StringComparison.Ordinal))
                {
                    return CompileStatus.Compiled;
                }

                return CompileStatus.Dirty;
            }

            return CompileStatus.Dirty;
        }

        /// <summary>
        /// Updates the cached source for a property key
        /// </summary>
        public static void UpdateCachedSource(string propertyKey, string source, string additionalUsings,
            string cachedDefinitions, string globalUsings = null)
        {
            if (string.IsNullOrEmpty(globalUsings))
            {
                try
                {
                    var config = AHAKuo.Signalia.Framework.ConfigReader.GetConfig();
                    globalUsings = config?.InlineScript?.GlobalUsings ?? string.Empty;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[InlineScript] Failed to load global usings from Signalia config: {ex.Message}");
                    globalUsings = string.Empty;
                }
            }
            var composite = InlineCompiler.NormalizeComposite(source ?? string.Empty, additionalUsings ?? string.Empty,
                cachedDefinitions ?? string.Empty, globalUsings);
            LastCompiledSources[propertyKey] = composite;
        }

        public static void ClearCachedSource(string propertyKey)
        {
            if (string.IsNullOrEmpty(propertyKey))
            {
                return;
            }

            LastCompiledSources.Remove(propertyKey);
        }

        /// <summary>
        /// Restores the serialized properties back to the contents of the compiled script asset.
        /// </summary>
        public static bool TryRevertToCompiledSource(string propertyKey, SerializedProperty sourceProp,
            SerializedProperty additionalUsingsProp, SerializedProperty cachedDefinitionsProp,
            SerializedProperty compiledScriptProp)
        {
            if (!InlineCompiler.TryExtractCompiledSource(compiledScriptProp, out var source))
            {
                return false;
            }

            if (sourceProp != null)
            {
                sourceProp.stringValue = source.UserCode ?? string.Empty;
            }

            if (additionalUsingsProp != null)
            {
                additionalUsingsProp.stringValue = source.AdditionalUsings ?? string.Empty;
            }

            if (cachedDefinitionsProp != null)
            {
                cachedDefinitionsProp.stringValue = source.CachedDefinitions ?? string.Empty;
            }

            UpdateCachedSource(propertyKey, source.UserCode, source.AdditionalUsings, source.CachedDefinitions,
                source.GlobalUsings);
            return true;
        }

        public static void CopyToClipboard(SerializedProperty sourceProp, SerializedProperty additionalUsingsProp,
            SerializedProperty cachedDefinitionsProp, SerializedProperty compiledScriptProp, SerializedProperty guidProp)
        {
            var data = new InlineScriptClipboardData
            {
                SourceCode = sourceProp?.stringValue ?? string.Empty,
                AdditionalUsings = additionalUsingsProp?.stringValue ?? string.Empty,
                CachedDefinitions = cachedDefinitionsProp?.stringValue ?? string.Empty,
                Guid = guidProp?.stringValue ?? string.Empty,
                ScriptAssetGuid = GetScriptAssetGuid(compiledScriptProp)
            };

            if (string.IsNullOrEmpty(data.Guid) && compiledScriptProp?.objectReferenceValue is MonoScript scriptAsset)
            {
                var generatedPrefix = InlineScriptConstants.GeneratedPrefix;
                var scriptName = scriptAsset != null ? scriptAsset.name : string.Empty;
                if (!string.IsNullOrEmpty(generatedPrefix) && !string.IsNullOrEmpty(scriptName) &&
                    scriptName.StartsWith(generatedPrefix, StringComparison.Ordinal))
                {
                    data.Guid = scriptName.Substring(generatedPrefix.Length);
                }
            }

            _clipboardCache = data;
            EditorGUIUtility.systemCopyBuffer = ClipboardPrefix + JsonUtility.ToJson(data);
        }

        public static bool TryPasteFromClipboard(SerializedProperty sourceProp, SerializedProperty additionalUsingsProp,
            SerializedProperty cachedDefinitionsProp, SerializedProperty compiledScriptProp, SerializedProperty guidProp)
        {
            if (!TryGetClipboard(out var data))
            {
                return false;
            }

            if (sourceProp != null)
            {
                sourceProp.stringValue = data.SourceCode ?? string.Empty;
            }

            if (additionalUsingsProp != null)
            {
                additionalUsingsProp.stringValue = data.AdditionalUsings ?? string.Empty;
            }

            if (cachedDefinitionsProp != null)
            {
                cachedDefinitionsProp.stringValue = data.CachedDefinitions ?? string.Empty;
            }

            if (guidProp != null)
            {
                guidProp.stringValue = data.Guid ?? string.Empty;
            }

            if (compiledScriptProp != null)
            {
                MonoScript script = null;
                if (!string.IsNullOrEmpty(data.ScriptAssetGuid))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(data.ScriptAssetGuid);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                    }
                }

                compiledScriptProp.objectReferenceValue = script;
            }

            return true;
        }

        public static bool HasClipboardData()
        {
            return TryGetClipboard(out _);
        }

        public static bool HasCopyableContent(SerializedProperty sourceProp, SerializedProperty additionalUsingsProp,
            SerializedProperty cachedDefinitionsProp, SerializedProperty compiledScriptProp, SerializedProperty guidProp)
        {
            if (sourceProp != null && !string.IsNullOrEmpty(sourceProp.stringValue))
            {
                return true;
            }

            if (additionalUsingsProp != null && !string.IsNullOrEmpty(additionalUsingsProp.stringValue))
            {
                return true;
            }

            if (cachedDefinitionsProp != null && !string.IsNullOrEmpty(cachedDefinitionsProp.stringValue))
            {
                return true;
            }

            if (compiledScriptProp != null && compiledScriptProp.objectReferenceValue != null)
            {
                return true;
            }

            if (guidProp != null && !string.IsNullOrEmpty(guidProp.stringValue))
            {
                return true;
            }

            return false;
        }

        private static bool TryGetClipboard(out InlineScriptClipboardData data)
        {
            if (_clipboardCache.HasValue && _clipboardCache.Value.IsValid)
            {
                data = _clipboardCache.Value;
                return true;
            }

            var buffer = EditorGUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(buffer) && buffer.StartsWith(ClipboardPrefix, StringComparison.Ordinal))
            {
                var json = buffer.Substring(ClipboardPrefix.Length);
                try
                {
                    var parsed = JsonUtility.FromJson<InlineScriptClipboardData>(json);
                    if (parsed.IsValid)
                    {
                        _clipboardCache = parsed;
                        data = parsed;
                        return true;
                    }
                }
                catch (ArgumentException)
                {
                    // Ignore invalid clipboard contents
                }
            }

            data = default;
            return false;
        }

        private static string GetScriptAssetGuid(SerializedProperty compiledScriptProp)
        {
            if (compiledScriptProp?.objectReferenceValue is MonoScript monoScript)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(monoScript, out var assetGuid, out long _))
                {
                    return assetGuid;
                }

                var assetPath = AssetDatabase.GetAssetPath(monoScript);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    return AssetDatabase.AssetPathToGUID(assetPath);
                }
            }

            return string.Empty;
        }

        private static string GetPropertyString(SerializedProperty property)
        {
            return property != null ? property.stringValue ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Validates source code and returns validation results
        /// </summary>
        public static List<(string message, MessageType messageType)> ValidateSource(string source)
        {
            var results = new List<(string, MessageType)>();
            if (string.IsNullOrWhiteSpace(source))
            {
                return results;
            }

            var normalizedSource = InlineCompiler.NormalizeSource(source);
            var lines = normalizedSource.Split(new[] { '\n' }, StringSplitOptions.None);
            var braceBalance = 0;
            var parenBalance = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                var trimmed = rawLine.Trim();

                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith("/*"))
                {
                    braceBalance += CountOccurrences(trimmed, '{') - CountOccurrences(trimmed, '}');
                    parenBalance += CountOccurrences(trimmed, '(') - CountOccurrences(trimmed, ')');
                    continue;
                }

                braceBalance += CountOccurrences(trimmed, '{') - CountOccurrences(trimmed, '}');
                parenBalance += CountOccurrences(trimmed, '(') - CountOccurrences(trimmed, ')');

                if (RequiresSemicolon(trimmed) && !trimmed.EndsWith(";"))
                {
                    results.Add(($"Line {i + 1} might be missing a semicolon.", MessageType.Warning));
                }
            }

            if (braceBalance != 0)
            {
                results.Add(("Mismatched curly braces detected.", MessageType.Error));
            }

            if (parenBalance != 0)
            {
                results.Add(("Mismatched parentheses detected.", MessageType.Warning));
            }

            return results;
        }

        /// <summary>
        /// Checks if a line requires a semicolon
        /// </summary>
        private static bool RequiresSemicolon(string trimmedLine)
        {
            if (trimmedLine.EndsWith("{") || trimmedLine.EndsWith("}") || trimmedLine.EndsWith(":") ||
                trimmedLine.EndsWith(",") || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("["))
            {
                return false;
            }

            foreach (var keyword in ControlKeywords)
            {
                if (trimmedLine.StartsWith(keyword + " ", StringComparison.Ordinal) ||
                    trimmedLine.StartsWith(keyword + "(", StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Counts occurrences of a character in a string
        /// </summary>
        private static int CountOccurrences(string text, char character)
        {
            var count = 0;
            foreach (var c in text)
            {
                if (c == character)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
#endif