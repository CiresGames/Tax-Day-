#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal.ThirdParty
{
    /// <summary>
    /// A hook between the font factory and the python fonttools package using the py script to fix mappings in fonts for arabic unicode characters that unity might not support.
    /// </summary>
    internal static class FontFactoryPyBridge
    {
        private const string PYTHON_SCRIPT_PATH = "Assets/AHAKuo Creations/Signalia/z_3rdparty/embedded.pyfonttools/python.exe";

        private const string FONTFIXSCRIPT =
            "Assets/AHAKuo Creations/Signalia/z_3rdparty/embedded.pyfonttools/script/ttf_unicodeMapping.py";

        /// <summary>
        /// Preprocesses the font file path by invoking a Python script to fix character mappings for Arabic Unicode support in Unity.
        /// </summary>
        /// <param name="pathToOriginalTtf">The file path to the original TTF font to be processed.</param>
        /// <returns>The file path to the processed TTF font.</returns>
        [Obsolete("Unity doesn't allow exe files, so this has been deprecated. Use it from Lingramia through Engines > Unity Font Fixing")]
        internal static string PreprocessFontPath(string pathToOriginalTtf)
        {
            // this is being deprecated because unity doesn't want exe files in the project folder
            if (string.IsNullOrEmpty(pathToOriginalTtf))
            {
                UnityEngine.Debug.LogWarning("[FontFactoryPyBridge] Font path is null or empty. Returning original path.");
                return pathToOriginalTtf;
            }

            // Convert Unity asset path to full file system path
            string fullFontPath = Path.GetFullPath(pathToOriginalTtf);
            if (!File.Exists(fullFontPath))
            {
                UnityEngine.Debug.LogWarning($"[FontFactoryPyBridge] Font file does not exist: {fullFontPath}. Returning original path.");
                return pathToOriginalTtf;
            }

            // Get full paths to Python executable and script
            string pythonExePath = Path.GetFullPath(PYTHON_SCRIPT_PATH);
            string scriptPath = Path.GetFullPath(FONTFIXSCRIPT);

            if (!File.Exists(pythonExePath))
            {
                UnityEngine.Debug.LogWarning($"[FontFactoryPyBridge] Python executable not found at: {pythonExePath}. Returning original path.");
                return pathToOriginalTtf;
            }

            if (!File.Exists(scriptPath))
            {
                UnityEngine.Debug.LogWarning($"[FontFactoryPyBridge] Python script not found at: {scriptPath}. Returning original path.");
                return pathToOriginalTtf;
            }

            try
            {
                // Prepare process start info
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = pythonExePath,
                    Arguments = $"\"{scriptPath}\" \"{fullFontPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Execute the Python script
                using (Process process = Process.Start(processInfo))
                {
                    // Read output and error streams
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Wait for process to complete (with timeout)
                    bool completed = process.WaitForExit(30000); // 30 second timeout

                    if (!completed)
                    {
                        process.Kill();
                        UnityEngine.Debug.LogError("[FontFactoryPyBridge] Python script execution timed out.");
                        return pathToOriginalTtf;
                    }

                    if (process.ExitCode != 0)
                    {
                        UnityEngine.Debug.LogError($"[FontFactoryPyBridge] Python script failed with exit code {process.ExitCode}.\nOutput: {output}\nError: {error}");
                        return pathToOriginalTtf;
                    }

                    // Log script output for debugging
                    if (!string.IsNullOrEmpty(output))
                    {
                        UnityEngine.Debug.Log($"[FontFactoryPyBridge] Python script output:\n{output}");
                    }

                    // Check for the fixed font file (script adds "_fixed" before extension)
                    string fixedFontPath = fullFontPath.Replace(".ttf", "_fixed.ttf").Replace(".TTF", "_fixed.TTF");
                    
                    if (File.Exists(fixedFontPath))
                    {
                        // Convert back to Unity asset path if possible
                        string fixedAssetPath = fixedFontPath.Replace('\\', '/');
                        string projectPath = Path.GetFullPath(".").Replace('\\', '/');
                        if (fixedAssetPath.StartsWith(projectPath))
                        {
                            fixedAssetPath = fixedAssetPath.Substring(projectPath.Length).TrimStart('/');
                        }
                        
                        UnityEngine.Debug.Log($"[FontFactoryPyBridge] Successfully processed font. Fixed font saved to: {fixedAssetPath}");
                        return fixedAssetPath;
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[FontFactoryPyBridge] Python script completed but fixed font file not found at: {fixedFontPath}. Returning original path.");
                        return pathToOriginalTtf;
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[FontFactoryPyBridge] Exception while executing Python script: {ex.Message}\n{ex.StackTrace}");
                return pathToOriginalTtf;
            }
        }
    }    
}
#endif