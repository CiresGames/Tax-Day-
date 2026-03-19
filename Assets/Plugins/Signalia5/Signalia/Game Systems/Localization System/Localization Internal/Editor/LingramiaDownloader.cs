#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal.Editors
{
    /// <summary>
    /// Helper class for downloading and managing Lingramia installation.
    /// Downloads the latest release from S3 storage and extracts it to a local directory.
    /// </summary>
    public static class LingramiaDownloader
    {
        // S3 storage information
        private const string S3_BUCKET_BASE_URL = "https://application-exec.s3.eu-north-1.amazonaws.com/lingramia-latest/";
        private const string S3_WIN64_ZIP = "Lingramia-Windows-x64.zip";
        private const string S3_WINARM64_ZIP = "Lingramia-Windows-ARM64.zip";
        private const string S3_MACOS_ZIP = "Lingramia-macOS-x64.zip"; // intel based macs
        private const string S3_MACOS_ARM64_ZIP = "Lingramia-macOS-ARM64.zip"; // apple silicon macs

        // Local installation directory
        private static string InstallationDirectory => GetInstallationDirectory();
        
        /// <summary>
        /// Gets the path to the Lingramia executable/bundle based on the current platform.
        /// </summary>
        private static string ExePath
        {
            get
            {
#if UNITY_EDITOR_OSX
                // On macOS, check for .app bundle first, then Unix executable
                string appBundlePath = Path.Combine(InstallationDirectory, "Lingramia.app");
                if (Directory.Exists(appBundlePath))
                {
                    return appBundlePath;
                }
                // Check for Unix executable
                string unixExePath = Path.Combine(InstallationDirectory, "Lingramia");
                if (File.Exists(unixExePath))
                {
                    return unixExePath;
                }
                // Fallback to .app path for consistency
                return appBundlePath;
#else
                return Path.Combine(InstallationDirectory, "Lingramia.exe");
#endif
            }
        }

        // Active download request for cancellation support
        private static UnityEngine.Networking.UnityWebRequest activeDownloadRequest = null;

        /// <summary>
        /// Gets the path to the Lingramia executable, or null if not downloaded.
        /// </summary>
        public static string GetLingramiaExePath()
        {
            if (IsLingramiaDownloaded())
            {
                return ExePath;
            }
            return null;
        }

        /// <summary>
        /// Checks if Lingramia is already downloaded and extracted.
        /// </summary>
        public static bool IsLingramiaDownloaded()
        {
#if UNITY_EDITOR_OSX
            // On macOS, check for .app bundle (directory) or Unix executable
            string appBundlePath = Path.Combine(InstallationDirectory, "Lingramia.app");
            if (Directory.Exists(appBundlePath))
            {
                return true;
            }
            string unixExePath = Path.Combine(InstallationDirectory, "Lingramia");
            return File.Exists(unixExePath);
#else
            return File.Exists(ExePath);
#endif
        }

        /// <summary>
        /// Gets the installation directory path based on the current platform.
        /// </summary>
        private static string GetInstallationDirectory()
        {
            string baseDir;
            
#if UNITY_EDITOR_WIN
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, "AHAKuo Creations", "Lingramia");
#elif UNITY_EDITOR_OSX
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(baseDir, "AHAKuo Creations", "Lingramia");
#elif UNITY_EDITOR_LINUX
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, "AHAKuo Creations", "Lingramia");
#else
            // Fallback to Unity's temporary cache
            return Path.Combine(Application.temporaryCachePath, "AHAKuo Creations", "Lingramia");
#endif
        }

        /// <summary>
        /// Aborts the current download if one is in progress.
        /// </summary>
        public static void AbortDownload()
        {
            if (activeDownloadRequest != null)
            {
                activeDownloadRequest.Abort();
                activeDownloadRequest.Dispose();
                activeDownloadRequest = null;
                UnityEngine.Debug.Log("[Lingramia Downloader] Download aborted by user");
            }
        }

        /// <summary>
        /// Downloads the latest Lingramia release from S3 storage.
        /// Returns the path to the downloaded ZIP file, or null if download failed.
        /// </summary>
        public static IEnumerator DownloadLatestLingramia(System.Action<float> progressCallback = null, System.Action<string> statusCallback = null, System.Func<bool> shouldAbort = null)
        {
            yield return null; // Ensure coroutine doesn't complete immediately
            
            // Determine the appropriate ZIP file for the current platform
            string zipFileName = GetPlatformZipFileName();
            
            if (string.IsNullOrEmpty(zipFileName))
            {
                UnityEngine.Debug.LogError("[Lingramia Downloader] Platform not supported");
                statusCallback?.Invoke("Your platform is not currently supported");
                yield break;
            }

            // Build S3 REST API URL (GetObject via REST)
            string s3Url = S3_BUCKET_BASE_URL + zipFileName;
            
            statusCallback?.Invoke($"Downloading Lingramia from S3...");

            // Create temp directory for download
            string tempDir = Path.Combine(Application.temporaryCachePath, "LingramiaDownload");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            string zipPath = Path.Combine(tempDir, zipFileName);

            // Download the ZIP file directly from S3 using REST API (GetObject)
            activeDownloadRequest = UnityEngine.Networking.UnityWebRequest.Get(s3Url);
            
            try
            {
                // Set timeout to 0 for no timeout (download will continue as long as data is being received)
                activeDownloadRequest.timeout = 0;
                
                // Disable SSL certificate validation if needed (Unity sometimes has issues)
                #if UNITY_2017_1_OR_NEWER
                activeDownloadRequest.certificateHandler = new BypassCertificateHandler();
                #endif

                // Track download progress
                var downloadOperation = activeDownloadRequest.SendWebRequest();

                while (!downloadOperation.isDone)
                {
                    // Check if download should be aborted
                    if (shouldAbort != null && shouldAbort())
                    {
                        activeDownloadRequest.Abort();
                        statusCallback?.Invoke("Download aborted");
                        yield break;
                    }

                    float progress = activeDownloadRequest.downloadProgress;
                    progressCallback?.Invoke(progress);
                    statusCallback?.Invoke($"Downloading... {progress * 100f:F1}%");
                    yield return null;
                }

                // Check if aborted after loop
                if (shouldAbort != null && shouldAbort())
                {
                    statusCallback?.Invoke("Download aborted");
                    yield break;
                }

                if (activeDownloadRequest.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError || 
                    activeDownloadRequest.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
                {
                    string errorMsg = $"Download failed.\nURL: {s3Url}\nError: {activeDownloadRequest.error}\nResponse Code: {activeDownloadRequest.responseCode}";
                    UnityEngine.Debug.LogError($"[Lingramia Downloader] {errorMsg}");
                    statusCallback?.Invoke($"Download failed: {activeDownloadRequest.error}");
                    yield break;
                }

                // Save downloaded file
                byte[] data = activeDownloadRequest.downloadHandler.data;
                
                if (data == null || data.Length == 0)
                {
                    UnityEngine.Debug.LogError($"[Lingramia Downloader] No data received from S3");
                    statusCallback?.Invoke("No data received from server");
                    yield break;
                }
                
                File.WriteAllBytes(zipPath, data);

                if (!File.Exists(zipPath))
                {
                    UnityEngine.Debug.LogError($"[Lingramia Downloader] ZIP file was not saved properly at: {zipPath}");
                    statusCallback?.Invoke("Failed to save downloaded file");
                    yield break;
                }

                statusCallback?.Invoke("Download complete. Extracting...");

                // Extract ZIP
                try
                {
                    ExtractLingramiaZip(zipPath);
                    statusCallback?.Invoke("Installation complete!");

                    // Clean up temp file
                    if (File.Exists(zipPath))
                    {
                        File.Delete(zipPath);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[Lingramia Downloader] Extraction failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    statusCallback?.Invoke($"Extraction failed: {ex.Message}");
                    yield break;
                }
            }
            finally
            {
                // Clean up the request
                if (activeDownloadRequest != null)
                {
                    activeDownloadRequest.Dispose();
                    activeDownloadRequest = null;
                }
            }
        }

        /// <summary>
        /// Extracts the Lingramia ZIP file to the installation directory.
        /// </summary>
        private static void ExtractLingramiaZip(string zipPath)
        {
            if (!File.Exists(zipPath))
            {
                throw new FileNotFoundException($"ZIP file not found: {zipPath}");
            }

            // Ensure installation directory exists
            if (!Directory.Exists(InstallationDirectory))
            {
                Directory.CreateDirectory(InstallationDirectory);
            }

            // Clear existing installation if it exists
            if (Directory.Exists(InstallationDirectory))
            {
                string[] existingFiles = Directory.GetFiles(InstallationDirectory);
                string[] existingDirs = Directory.GetDirectories(InstallationDirectory);
                
                foreach (string file in existingFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
                
                foreach (string dir in existingDirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch { }
                }
            }

            // Extract ZIP
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, InstallationDirectory);

            // Verify extraction - platform-specific checks
#if UNITY_EDITOR_OSX
            // On macOS, check for .app bundle or Unix executable
            string appBundlePath = Path.Combine(InstallationDirectory, "Lingramia.app");
            string unixExePath = Path.Combine(InstallationDirectory, "Lingramia");
            
            if (!Directory.Exists(appBundlePath) && !File.Exists(unixExePath))
            {
                // Try to find .app bundle or executable in subdirectories
                // Search for directories ending with .app
                string[] allDirs = Directory.GetDirectories(InstallationDirectory, "*", SearchOption.AllDirectories);
                string[] appDirs = Array.FindAll(allDirs, dir => dir.EndsWith(".app", StringComparison.OrdinalIgnoreCase));
                string[] unixExes = Directory.GetFiles(InstallationDirectory, "Lingramia", SearchOption.AllDirectories);
                
                if (appDirs.Length > 0)
                {
                    // Move .app bundle to root of installation directory
                    string sourceAppPath = appDirs[0];
                    if (Directory.Exists(appBundlePath))
                    {
                        Directory.Delete(appBundlePath, true);
                    }
                    Directory.Move(sourceAppPath, appBundlePath);
                }
                else if (unixExes.Length > 0)
                {
                    // Move Unix executable to root of installation directory
                    File.Copy(unixExes[0], unixExePath, true);
                    // Make it executable
                    try
                    {
                        ProcessStartInfo chmodInfo = new ProcessStartInfo
                        {
                            FileName = "/bin/chmod",
                            Arguments = $"+x \"{unixExePath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        Process chmodProcess = Process.Start(chmodInfo);
                        chmodProcess?.WaitForExit(2000);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"[Lingramia Downloader] Error setting execute permissions during extraction: {ex.Message}");
                    }
                }
                else
                {
                    throw new FileNotFoundException("Lingramia.app or Lingramia executable not found after extraction");
                }
            }
            
            // Remove quarantine attributes immediately after extraction (macOS security feature)
            // This prevents Gatekeeper from blocking the app when it's launched
            if (Directory.Exists(appBundlePath))
            {
                try
                {
                    ProcessStartInfo removeQuarantineInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/xattr",
                        Arguments = $"-dr com.apple.quarantine \"{appBundlePath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    Process removeQuarantineProcess = Process.Start(removeQuarantineInfo);
                    removeQuarantineProcess?.WaitForExit(5000);
                    
                    if (removeQuarantineProcess.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log("[Lingramia Downloader] Successfully removed quarantine attributes after extraction");
                    }
                    else
                    {
                        string errorOutput = removeQuarantineProcess.StandardError.ReadToEnd();
                        UnityEngine.Debug.LogWarning($"[Lingramia Downloader] Warning: Could not remove quarantine attributes: {errorOutput}");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[Lingramia Downloader] Warning: Error removing quarantine attributes after extraction: {ex.Message}");
                }
                
                // Ensure the executable inside the app bundle has execute permissions
                string executablePath = Path.Combine(appBundlePath, "Contents", "MacOS", "Lingramia");
                if (!File.Exists(executablePath))
                {
                    executablePath = Path.Combine(appBundlePath, "Contents", "Resources", "Lingramia");
                    if (!File.Exists(executablePath))
                    {
                        executablePath = Path.Combine(appBundlePath, "Contents", "Lingramia");
                    }
                }
                
                if (File.Exists(executablePath))
                {
                    try
                    {
                        ProcessStartInfo chmodInfo = new ProcessStartInfo
                        {
                            FileName = "/bin/chmod",
                            Arguments = $"+x \"{executablePath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        Process chmodProcess = Process.Start(chmodInfo);
                        chmodProcess?.WaitForExit(2000);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"[Lingramia Downloader] Warning: Error setting execute permissions on app bundle executable: {ex.Message}");
                    }
                }
            }
            else if (File.Exists(unixExePath))
            {
                // Remove quarantine from Unix executable as well
                try
                {
                    ProcessStartInfo removeQuarantineInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/xattr",
                        Arguments = $"-d com.apple.quarantine \"{unixExePath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    Process removeQuarantineProcess = Process.Start(removeQuarantineInfo);
                    removeQuarantineProcess?.WaitForExit(2000);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[Lingramia Downloader] Warning: Error removing quarantine attributes from executable: {ex.Message}");
                }
            }
#else
            // On Windows, check for .exe
            if (!File.Exists(ExePath))
            {
                // Try to find exe in subdirectories
                string[] exeFiles = Directory.GetFiles(InstallationDirectory, "*.exe", SearchOption.AllDirectories);
                if (exeFiles.Length > 0)
                {
                    // Move exe to root of installation directory
                    File.Copy(exeFiles[0], ExePath, true);
                }
                else
                {
                    throw new FileNotFoundException("Lingramia.exe not found after extraction");
                }
            }
#endif
        }

        /// <summary>
        /// Launches Lingramia with an optional .locbook file.
        /// </summary>
        public static bool LaunchLingramia(string locbookFilePath = null)
        {
            string exePath = GetLingramiaExePath();
            
            if (string.IsNullOrEmpty(exePath))
            {
                return false;
            }

            try
            {
#if UNITY_EDITOR_OSX
                // On macOS, handle .app bundles and Unix executables differently
                if (exePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase) && Directory.Exists(exePath))
                {
                    // Remove quarantine attributes recursively from the app bundle and its contents
                    // This is critical for macOS Gatekeeper to allow the app to run
                    try
                    {
                        // Remove quarantine from the app bundle itself
                        ProcessStartInfo removeQuarantineInfo = new ProcessStartInfo
                        {
                            FileName = "/usr/bin/xattr",
                            Arguments = $"-dr com.apple.quarantine \"{exePath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        Process removeQuarantineProcess = Process.Start(removeQuarantineInfo);
                        removeQuarantineProcess?.WaitForExit(5000);
                        
                        // Check if the process failed
                        if (removeQuarantineProcess.ExitCode != 0)
                        {
                            string errorOutput = removeQuarantineProcess.StandardError.ReadToEnd();
                            UnityEngine.Debug.LogWarning($"[Lingramia Downloader] Failed to remove quarantine attributes: {errorOutput}");
                        }
                        else
                        {
                            UnityEngine.Debug.Log("[Lingramia Downloader] Successfully removed quarantine attributes from app bundle");
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"[Lingramia Downloader] Error removing quarantine attributes: {ex.Message}");
                    }
                    
                    // Ensure the executable inside the app bundle has execute permissions
                    string executablePath = Path.Combine(exePath, "Contents", "MacOS", "Lingramia");
                    if (!File.Exists(executablePath))
                    {
                        // Try alternative common locations
                        executablePath = Path.Combine(exePath, "Contents", "Resources", "Lingramia");
                        if (!File.Exists(executablePath))
                        {
                            // Try root of Contents
                            executablePath = Path.Combine(exePath, "Contents", "Lingramia");
                        }
                    }
                    
                    if (File.Exists(executablePath))
                    {
                        try
                        {
                            ProcessStartInfo chmodInfo = new ProcessStartInfo
                            {
                                FileName = "/bin/chmod",
                                Arguments = $"+x \"{executablePath}\"",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            };
                            Process chmodProcess = Process.Start(chmodInfo);
                            chmodProcess?.WaitForExit(2000);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning($"[Lingramia Downloader] Error setting execute permissions: {ex.Message}");
                        }
                    }
                    
                    // Use 'open' with the full path to the app bundle for more reliable launching
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/open",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    if (!string.IsNullOrEmpty(locbookFilePath) && File.Exists(locbookFilePath))
                    {
                        // Use full path to app bundle with --args for file arguments
                        startInfo.Arguments = $"\"{exePath}\" --args \"{locbookFilePath}\"";
                    }
                    else
                    {
                        // Use full path to app bundle
                        startInfo.Arguments = $"\"{exePath}\"";
                    }

                    Process startProcess = Process.Start(startInfo);
                    if (startProcess != null)
                    {
                        startProcess.WaitForExit(3000);
                        if (startProcess.ExitCode != 0)
                        {
                            string errorOutput = startProcess.StandardError.ReadToEnd();
                            UnityEngine.Debug.LogError($"[Lingramia Downloader] Failed to launch app: {errorOutput}");
                            throw new Exception($"Failed to launch Lingramia: exit code {startProcess.ExitCode}");
                        }
                    }
                }
                else
                {
                    // Launch Unix executable directly
                    // Ensure it has execute permissions
                    try
                    {
                        ProcessStartInfo chmodInfo = new ProcessStartInfo
                        {
                            FileName = "/bin/chmod",
                            Arguments = $"+x \"{exePath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        Process chmodProcess = Process.Start(chmodInfo);
                        chmodProcess?.WaitForExit(2000);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"[Lingramia Downloader] Error setting execute permissions: {ex.Message}");
                    }
                    
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = false
                    };

                    if (!string.IsNullOrEmpty(locbookFilePath) && File.Exists(locbookFilePath))
                    {
                        startInfo.Arguments = $"\"{locbookFilePath}\"";
                    }

                    Process.Start(startInfo);
                }
#else
                // On Windows, launch .exe directly
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                };

                if (!string.IsNullOrEmpty(locbookFilePath) && File.Exists(locbookFilePath))
                {
                    startInfo.Arguments = $"\"{locbookFilePath}\"";
                }

                Process.Start(startInfo);
#endif
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Lingramia Downloader] Failed to launch Lingramia: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the appropriate ZIP file name for the current platform and architecture.
        /// </summary>
        private static string GetPlatformZipFileName()
        {
#if UNITY_EDITOR_WIN
            // Detect Windows architecture
            bool isARM64 = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == 
                          System.Runtime.InteropServices.Architecture.Arm64;
            
            return isARM64 ? S3_WINARM64_ZIP : S3_WIN64_ZIP;
#elif UNITY_EDITOR_OSX
            // Detect macOS architecture
            bool isARM64 = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == 
                          System.Runtime.InteropServices.Architecture.Arm64;
            
            return isARM64 ? S3_MACOS_ARM64_ZIP : S3_MACOS_ZIP;
#elif UNITY_EDITOR_LINUX
            // Linux support not yet available
            return null;
#else
            return null;
#endif
        }

        /// <summary>
        /// Opens the installation directory in the system file explorer.
        /// </summary>
        public static void OpenInstallationDirectory()
        {
            if (Directory.Exists(InstallationDirectory))
            {
                EditorUtility.RevealInFinder(InstallationDirectory);
            }
            else
            {
                EditorUtility.DisplayDialog("Directory Not Found", 
                    $"Lingramia installation directory does not exist yet.\n\nPath: {InstallationDirectory}", 
                    "OK");
            }
        }

        #if UNITY_2017_1_OR_NEWER
        /// <summary>
        /// Certificate handler that bypasses certificate validation (for S3 REST API)
        /// </summary>
        private class BypassCertificateHandler : UnityEngine.Networking.CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true; // Accept all certificates
            }
        }
        #endif
    }
}
#endif