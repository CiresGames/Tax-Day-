#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal.Editors
{
    /// <summary>
    /// Editor window for downloading and installing Lingramia.
    /// Shows download progress and handles the installation process.
    /// </summary>
    public class LingramiaDownloadWindow : EditorWindow
    {
        private float downloadProgress = 0f;
        private string statusMessage = "Ready to download Lingramia";
        private bool isDownloading = false;
        private bool downloadComplete = false;
        private bool shouldAbortDownload = false;
        private IEnumerator downloadCoroutine = null;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Signalia/Game Systems/Localization/Download Lingramia", false, 150)]
        public static void ShowWindow()
        {
            LingramiaDownloadWindow window = GetWindow<LingramiaDownloadWindow>("Download Lingramia");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        [MenuItem("Tools/Signalia/Game Systems/Localization/Open Lingramia", false, 1)]
        public static void OpenLingramia()
        {
            if (LingramiaDownloader.LaunchLingramia())
            {
                Debug.Log("[Lingramia] Lingramia launched successfully.");
            }
            else
            {
                EditorUtility.DisplayDialog("Lingramia Not Found", 
                    "Lingramia is not installed. Please download it first using 'Download Lingramia' from the menu.", 
                    "OK");
            }
        }

        [MenuItem("Tools/Signalia/Game Systems/Localization//Open Lingramia", true)]
        public static bool ValidateOpenLingramia()
        {
            return LingramiaDownloader.IsLingramiaDownloaded();
        }

        private void OnGUI()
        {
            // Header with Lingramia logo
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Framework.GraphicLoader.LocalizationLingramiaIcon, GUILayout.Height(128), GUILayout.Width(128));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Check if already installed
            if (LingramiaDownloader.IsLingramiaDownloaded())
            {
                EditorGUILayout.HelpBox("Lingramia is already installed!", MessageType.Info);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Installation Folder", GUILayout.Height(30)))
                {
                    LingramiaDownloader.OpenInstallationDirectory();
                }
                
                if (isDownloading)
                {
                    if (GUILayout.Button("Abort Download", GUILayout.Height(30)))
                    {
                        AbortDownload();
                    }
                }
                else
                {
                    if (GUILayout.Button("Re-download (Update)", GUILayout.Height(30)))
                    {
                        StartDownload();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                EditorGUILayout.LabelField("Installed at:", EditorStyles.miniLabel);
                EditorGUILayout.SelectableLabel(LingramiaDownloader.GetLingramiaExePath(), EditorStyles.textField, GUILayout.Height(20));
            }
            else
            {
                EditorGUILayout.HelpBox("Lingramia is not installed. Click the button below to download and install the latest version.", MessageType.Info);
                
                GUILayout.Space(10);
                
                if (isDownloading)
                {
                    if (GUILayout.Button("Abort Download", GUILayout.Height(40)))
                    {
                        AbortDownload();
                    }
                }
                else
                {
                    if (GUILayout.Button("Download & Install Lingramia", GUILayout.Height(40)))
                    {
                        StartDownload();
                    }
                }
            }

            // Download progress section
            if (isDownloading || downloadComplete)
            {
                GUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(statusMessage, EditorStyles.wordWrappedLabel);

                if (isDownloading)
                {
                    GUILayout.Space(10);
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), downloadProgress, $"{downloadProgress * 100f:F1}%");
                }

                if (downloadComplete)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.HelpBox("Installation complete! You can now use Lingramia from the LocBook inspector.", MessageType.Info);
                    
                    if (GUILayout.Button("Close", GUILayout.Height(30)))
                    {
                        Close();
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            GUILayout.FlexibleSpace();

            // Footer information
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Lingramia will be installed to your local application data folder.", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
        }

        private void StartDownload()
        {
            if (isDownloading)
            {
                return;
            }

            isDownloading = true;
            downloadComplete = false;
            shouldAbortDownload = false;
            downloadProgress = 0f;
            statusMessage = "Initializing download...";

            // Start download coroutine
            downloadCoroutine = DownloadCoroutine();
            EditorCoroutineUtility.StartCoroutine(downloadCoroutine, this);
        }

        private void AbortDownload()
        {
            if (!isDownloading)
            {
                return;
            }

            shouldAbortDownload = true;
            LingramiaDownloader.AbortDownload();
            statusMessage = "Aborting download...";
            Repaint();
        }

        private void OnDestroy()
        {
            // Abort download if window is closed during download
            if (isDownloading)
            {
                AbortDownload();
            }
        }

        private IEnumerator DownloadCoroutine()
        {
            // Get the download enumerator with abort check
            IEnumerator downloadEnumerator = LingramiaDownloader.DownloadLatestLingramia(
                progress => {
                    downloadProgress = progress;
                    Repaint();
                },
                status => {
                    statusMessage = status;
                    Repaint();
                },
                () => shouldAbortDownload // Abort check function
            );

            // Manually iterate through the enumerator
            while (downloadEnumerator.MoveNext())
            {
                // Check if we should abort
                if (shouldAbortDownload)
                {
                    break;
                }
                yield return downloadEnumerator.Current;
            }

            isDownloading = false;
            downloadCoroutine = null;

            if (shouldAbortDownload)
            {
                downloadComplete = false;
                statusMessage = "Download aborted by user";
                shouldAbortDownload = false;
            }
            else
            {
                downloadComplete = LingramiaDownloader.IsLingramiaDownloaded();

                if (!downloadComplete)
                {
                    statusMessage = "Download failed. Please check the Console for error details.";
                    EditorUtility.DisplayDialog("Download Failed", 
                        "Failed to download Lingramia. Please check the Console for detailed error messages.\n\nYou can try downloading manually from GitHub.", 
                        "OK");
                }
            }

            Repaint();
        }
    }

    /// <summary>
    /// Helper class to run coroutines in editor context.
    /// </summary>
    public static class EditorCoroutineUtility
    {
        private static System.Collections.Generic.List<CoroutineData> activeCoroutines = new System.Collections.Generic.List<CoroutineData>();

        private class CoroutineData
        {
            public IEnumerator coroutine;
            public EditorWindow window;
            public EditorApplication.CallbackFunction updateAction;
        }

        public static void StartCoroutine(IEnumerator coroutine, EditorWindow window)
        {
            CoroutineData data = new CoroutineData
            {
                coroutine = coroutine,
                window = window,
                updateAction = () => UpdateCoroutines()
            };

            activeCoroutines.Add(data);
            EditorApplication.update += data.updateAction;
        }

        public static void StopCoroutine(IEnumerator coroutine, EditorWindow window)
        {
            for (int i = activeCoroutines.Count - 1; i >= 0; i--)
            {
                CoroutineData data = activeCoroutines[i];
                if (data.coroutine == coroutine && data.window == window)
                {
                    activeCoroutines.RemoveAt(i);
                    if (data.updateAction != null)
                    {
                        EditorApplication.update -= data.updateAction;
                    }
                    break;
                }
            }
        }

        private static void UpdateCoroutines()
        {
            for (int i = activeCoroutines.Count - 1; i >= 0; i--)
            {
                CoroutineData data = activeCoroutines[i];
                try
                {
                    bool hasMore = data.coroutine.MoveNext();
                    
                    if (data.window != null)
                    {
                        data.window.Repaint();
                    }
                    
                    if (!hasMore)
                    {
                        activeCoroutines.RemoveAt(i);
                        if (data.updateAction != null)
                        {
                            EditorApplication.update -= data.updateAction;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[EditorCoroutineUtility] Exception in coroutine: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    activeCoroutines.RemoveAt(i);
                    if (data.updateAction != null)
                    {
                        EditorApplication.update -= data.updateAction;
                    }
                }
            }
        }
    }
}
#endif

