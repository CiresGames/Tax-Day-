using System;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.Framework.Editors
{
    public class AboutWindow : EditorWindow
    {
        #region Window State

        private VersionInfo versionInfo;

        #endregion

        #region Menu Items

        [MenuItem("Tools/Signalia/Getting Started")]
        public static void OpenGettingStarted()
        {
            string relativePath = FrameworkConstants.MiscPaths.GETTING_STARTED;
            string fullPath = System.IO.Path.Combine(Application.dataPath, relativePath.Substring("Assets/".Length));

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to open Getting Started file: " + e.Message);
                EditorUtility.DisplayDialog("Error", "Failed to open Getting Started documentation. Please check if the file exists at: " + fullPath, "OK");
            }
        }

        [MenuItem("Tools/Signalia/About")]
        public static void ShowWindow()
        {
            GetWindow<AboutWindow>("About Signalia").minSize = new Vector2(500, 250);
            GetWindow<AboutWindow>("About Signalia").maxSize = new Vector2(500, 250);
        }

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            LoadVersionInfo();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(GraphicLoader.Signalia, GUILayout.Height(120), GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            // Description
            GUIStyle descriptionStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUILayout.Label(
                "Signalia is a powerful, all-in-one game framework for Unity designed to accelerate development and empower creativity. " +
                "It offers advanced tools for crafting dynamic user interfaces, an event-driven C# radio system, and a suite of modular game systems and utilities—" +
                "built to help you prototype, polish, and ship your next game faster.",
                descriptionStyle
            );

            GUILayout.Space(15);

            // Version and Company
            GUIStyle versionStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label($"{versionInfo.package} | {versionInfo.version}", versionStyle);

            GUIStyle companyStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label($"by {versionInfo.company}", companyStyle);
        }

        #endregion

        #region Internals

        private void LoadVersionInfo()
        {
            string versionFilePath = "Assets/AHAKuo Creations/Signalia/Framework/version.info";
            if (System.IO.File.Exists(versionFilePath))
            {
                try
                {
                    string jsonContent = System.IO.File.ReadAllText(versionFilePath);
                    versionInfo = JsonUtility.FromJson<VersionInfo>(jsonContent);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse version.info: {e.Message}");
                    versionInfo = new VersionInfo
                    {
                        package = "Signalia Engine",
                        version = "Version information not found",
                        date = "",
                        company = "AHAKuo Creations"
                    };
                }
            }
            else
            {
                versionInfo = new VersionInfo
                {
                    package = "Signalia Engine",
                    version = "Version information not found",
                    date = "",
                    company = "AHAKuo Creations"
                };
            }
        }

        #endregion
    }
}

