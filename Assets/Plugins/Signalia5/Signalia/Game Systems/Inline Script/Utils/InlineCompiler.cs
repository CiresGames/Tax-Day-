#if UNITY_EDITOR
using static AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils.InlineScriptPaths;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils
{
    /// <summary>
    /// Handles compilation of inline scripts into MonoScript assets
    /// </summary>
    public static class InlineCompiler
    {
        public readonly struct InlineScriptSourceSegments
        {
            public InlineScriptSourceSegments(string userCode, string additionalUsings, string cachedDefinitions,
                string globalUsings)
            {
                UserCode = userCode;
                AdditionalUsings = additionalUsings;
                CachedDefinitions = cachedDefinitions;
                GlobalUsings = globalUsings;
            }

            public string UserCode { get; }
            public string AdditionalUsings { get; }
            public string CachedDefinitions { get; }
            public string GlobalUsings { get; }
        }

        private const string SectionPrefix = "// @InlineScript Section:";
        private const string SectionSuffix = "// @InlineScript EndSection";

        /// <summary>
        /// Compiles an inline snippet into a MonoScript asset
        /// </summary>
        public static void CompileInlineSnippet(SerializedObject serializedObject, SerializedProperty sourceProp,
            SerializedProperty compiledScriptProp, SerializedProperty guidProp, SerializedProperty additionalUsingsProp,
            SerializedProperty cachedDefinitionsProp, InlineScriptTypeProfile profile)
        {
            if (sourceProp == null)
            {
                Debug.LogError("InlineScript: Cannot compile without source code.");
                return;
            }

            var sourceCode = sourceProp.stringValue ?? string.Empty;
            var additionalUsings = additionalUsingsProp != null ? additionalUsingsProp.stringValue ?? string.Empty : string.Empty;
            var cachedDefinitions = cachedDefinitionsProp != null ? cachedDefinitionsProp.stringValue ?? string.Empty : string.Empty;
            
            string globalUsings = string.Empty;
            try
            {
                var config = AHAKuo.Signalia.Framework.ConfigReader.GetConfig();
                globalUsings = config?.InlineScript?.GlobalUsings ?? string.Empty;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InlineScript] Failed to load global usings from Signalia config: {ex.Message}");
            }
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                if (!EditorUtility.DisplayDialog("InlineScript", "Source code is empty. Compile anyway?", "Yes", "No"))
                {
                    return;
                }
            }

            var targetScript = compiledScriptProp?.objectReferenceValue as MonoScript;
            var assetScriptPath = targetScript != null ? AssetDatabase.GetAssetPath(targetScript) : null;
            var className = !string.IsNullOrEmpty(assetScriptPath)
                ? Path.GetFileNameWithoutExtension(assetScriptPath)
                : null;

            if (string.IsNullOrEmpty(assetScriptPath))
            {
                var guid = guidProp?.stringValue;
                if (string.IsNullOrEmpty(guid))
                {
                    guid = System.Guid.NewGuid().ToString("N");
                    if (guidProp != null)
                    {
                        guidProp.stringValue = guid;
                    }
                }
                
                // Ensure GUID uniqueness by checking if it's already used
                guid = EnsureUniqueGuid(guid, guidProp);

                EnsureDirectories();

                className = $"{InlineScriptConstants.GeneratedPrefix}{guid}";
                assetScriptPath = Path.Combine(RootPath_Cache, className + ".cs").Replace("\\", "/");
            }

            if (string.IsNullOrEmpty(className))
            {
                Debug.LogError("InlineScript: Unable to determine class name for inline snippet.");
                return;
            }

            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), assetScriptPath);
            var scriptDirectory = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrEmpty(scriptDirectory))
            {
                Directory.CreateDirectory(scriptDirectory);
            }

            var scriptContents = profile.HasReturnValue
                ? CodeWriter.BoilerPlateFunction(className, sourceCode, additionalUsings, cachedDefinitions, globalUsings,
                    profile.BoilerplateBaseClassName, profile.ReturnTypeName, profile.MethodName)
                : CodeWriter.BoilerPlateVoid(className, sourceCode, additionalUsings, cachedDefinitions, globalUsings);
            File.WriteAllText(projectPath, scriptContents);

            AssetDatabase.ImportAsset(assetScriptPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(assetScriptPath);
            if (monoScript == null)
            {
                Debug.LogError($"InlineScript: Failed to load MonoScript at {assetScriptPath}.");
                return;
            }

            var scriptType = monoScript.GetClass();
            if (scriptType != null && !profile.BehaviourBaseType.IsAssignableFrom(scriptType))
            {
                Debug.LogError($"InlineScript: Generated type {scriptType.FullName} does not inherit from {profile.BehaviourBaseType.FullName}.");
                return;
            }

            if (compiledScriptProp != null)
            {
                serializedObject.Update();
                compiledScriptProp.objectReferenceValue = monoScript;
            }

            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (serializedObject.targetObject != null)
            {
                EditorUtility.SetDirty(serializedObject.targetObject);
            }

            Debug.Log($"InlineScript: Compilation complete for '{assetScriptPath}'.");
        }

        /// <summary>
        /// Normalizes source code by standardizing line endings
        /// </summary>
        public static string NormalizeSource(string source)
        {
            return (source ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd();
        }

        /// <summary>
        /// Builds a composite normalized representation of all editable segments.
        /// </summary>
        public static string NormalizeComposite(string source, string additionalUsings, string cachedDefinitions,
            string globalUsings)
        {
            return string.Join("\n", new[]
            {
                "GLOBAL:" + NormalizeSegment(globalUsings),
                "USINGS:" + NormalizeSegment(additionalUsings),
                "DEFS:" + NormalizeSegment(cachedDefinitions),
                "CODE:" + NormalizeSegment(source)
            });
        }

        /// <summary>
        /// Tries to extract the original source code from a compiled script
        /// </summary>
        public static bool TryExtractCompiledSource(SerializedProperty compiledScriptProp, out InlineScriptSourceSegments source)
        {
            source = default;
            if (compiledScriptProp == null)
            {
                return false;
            }

            if (!(compiledScriptProp.objectReferenceValue is MonoScript monoScript))
            {
                return false;
            }

            var scriptPath = AssetDatabase.GetAssetPath(monoScript);
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            {
                return false;
            }

            try
            {
                var lines = File.ReadAllLines(scriptPath);
                if (TryParseCommentSections(lines, out var sections))
                {
                    string SectionToString(string key)
                    {
                        return sections.TryGetValue(key, out var values) ? string.Join("\n", values) : string.Empty;
                    }

                    var userCode = NormalizeSource(SectionToString("User Code"));
                    var additional = NormalizeSource(SectionToString("Additional Usings"));
                    var cached = NormalizeSource(SectionToString("Cached Definitions"));
                    var global = NormalizeSource(SectionToString("Global Imports"));
                    source = new InlineScriptSourceSegments(userCode, additional, cached, global);
                    return true;
                }

                var collected = new List<string>();
                var inUserCodeComment = false;

                foreach (var line in lines)
                {
                    if (line.Trim() == "// User Code:")
                    {
                        inUserCodeComment = true;
                        continue;
                    }

                    if (inUserCodeComment)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            break;
                        }

                        if (line.StartsWith("// "))
                        {
                            collected.Add(line.Substring(3));
                        }
                        else if (line.StartsWith("//"))
                        {
                            collected.Add(line.Substring(2));
                        }
                    }
                }

                var normalized = NormalizeSource(string.Join("\n", collected));
                source = new InlineScriptSourceSegments(normalized, string.Empty, string.Empty, string.Empty);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"InlineScript: Failed to read compiled script contents. {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ensures the cache directory exists
        /// </summary>
        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(RootPath_Cache);
        }

        /// <summary>
        /// Ensures a GUID is unique by checking for existing files
        /// </summary>
        private static string EnsureUniqueGuid(string guid, SerializedProperty guidProp)
        {
            // Check if a file with this GUID already exists
            var className = $"{InlineScriptConstants.GeneratedPrefix}{guid}";
            var assetScriptPath = Path.Combine(RootPath_Cache, className + ".cs").Replace("\\", "/");

            if (File.Exists(assetScriptPath))
            {
                // Generate a new GUID if the file already exists
                var newGuid = System.Guid.NewGuid().ToString("N");
                if (guidProp != null)
                {
                    guidProp.stringValue = newGuid;
                }
                return newGuid;
            }

            return guid;
        }

        private static bool TryParseCommentSections(IReadOnlyList<string> lines,
            out Dictionary<string, List<string>> sections)
        {
            sections = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            string currentSection = null;

            foreach (var rawLine in lines)
            {
                var trimmed = rawLine.Trim();
                if (trimmed.StartsWith(SectionPrefix, StringComparison.Ordinal))
                {
                    currentSection = trimmed.Substring(SectionPrefix.Length).Trim();
                    if (!sections.ContainsKey(currentSection))
                    {
                        sections[currentSection] = new List<string>();
                    }
                    continue;
                }

                if (trimmed == SectionSuffix)
                {
                    currentSection = null;
                    continue;
                }

                if (currentSection == null)
                {
                    continue;
                }

                if (rawLine.StartsWith("//"))
                {
                    var content = rawLine.Substring(2);
                    if (content.StartsWith(" ", StringComparison.Ordinal))
                    {
                        content = content.Substring(1);
                    }
                    sections[currentSection].Add(content.TrimEnd('\r'));
                }
                else
                {
                    sections[currentSection].Add(rawLine.TrimEnd('\r'));
                }
            }

            return sections.Count > 0;
        }

        private static string NormalizeSegment(string segment)
        {
            return string.IsNullOrWhiteSpace(segment) ? string.Empty : NormalizeSource(segment);
        }
    }
}
#endif
