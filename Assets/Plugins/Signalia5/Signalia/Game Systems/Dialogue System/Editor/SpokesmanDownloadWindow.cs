#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using AHAKuo.Signalia.GameSystems.Localization.Internal.Editors;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem.Editors
{
    /// <summary>
    /// Editor window for downloading and installing Spokesman.
    /// Shows download progress and handles the installation process.
    /// </summary>
    public class SpokesmanDownloadWindow : EditorWindow
    {
        private float downloadProgress = 0f;
        private string statusMessage = "Ready to download Spokesman";
        private bool isDownloading = false;
        private bool downloadComplete = false;
        private bool shouldAbortDownload = false;
        private IEnumerator downloadCoroutine = null;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Signalia/Game Systems/Dialogue/Download Spokesman", false, 150)]
        public static void ShowWindow()
        {
            SpokesmanDownloadWindow window = GetWindow<SpokesmanDownloadWindow>("Download Spokesman");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        [MenuItem("Tools/Signalia/Game Systems/Dialogue/Open Spokesman", false, 1)]
        public static void OpenSpokesman()
        {
            if (SpokesmanDownloader.LaunchSpokesman())
            {
                Debug.Log("[Spokesman] Spokesman launched successfully.");
            }
            else
            {
                EditorUtility.DisplayDialog("Spokesman Not Found", 
                    "Spokesman is not installed. Please download it first using 'Download Spokesman' from the menu.", 
                    "OK");
            }
        }

        [MenuItem("Tools/Signalia/Game Systems/Dialogue/Open Spokesman", true)]
        public static bool ValidateOpenSpokesman()
        {
            return SpokesmanDownloader.IsSpokesmanDownloaded();
        }

        private void OnGUI()
        {
            // Header with Spokesman logo
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Framework.GraphicLoader.DialogueSpokesmanIcon, GUILayout.Height(128), GUILayout.Width(128));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Check if already installed
            if (SpokesmanDownloader.IsSpokesmanDownloaded())
            {
                EditorGUILayout.HelpBox("Spokesman is already installed!", MessageType.Info);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Installation Folder", GUILayout.Height(30)))
                {
                    SpokesmanDownloader.OpenInstallationDirectory();
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
                EditorGUILayout.SelectableLabel(SpokesmanDownloader.GetSpokesmanExePath(), EditorStyles.textField, GUILayout.Height(20));
            }
            else
            {
                EditorGUILayout.HelpBox("Spokesman is not installed. Click the button below to download and install the latest version.", MessageType.Info);
                
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
                    if (GUILayout.Button("Download & Install Spokesman", GUILayout.Height(40)))
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
                    EditorGUILayout.HelpBox("Installation complete! You can now use Spokesman from the DialogueBook inspector.", MessageType.Info);
                    
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
            EditorGUILayout.LabelField("Spokesman will be installed to your local application data folder.", EditorStyles.miniLabel);
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
            SpokesmanDownloader.AbortDownload();
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
            IEnumerator downloadEnumerator = SpokesmanDownloader.DownloadLatestSpokesman(
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
                downloadComplete = SpokesmanDownloader.IsSpokesmanDownloaded();

                if (!downloadComplete)
                {
                    statusMessage = "Download failed. Please check the Console for error details.";
                    EditorUtility.DisplayDialog("Download Failed", 
                        "Failed to download Spokesman. Please check the Console for detailed error messages.", 
                        "OK");
                }
            }

            Repaint();
        }
    }
}
#endif
