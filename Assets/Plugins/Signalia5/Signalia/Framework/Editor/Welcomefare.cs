using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

namespace AHAKuo.Signalia.Framework
{
    [InitializeOnLoad]
    public class Welcomefare : EditorWindow
    {
        private static Texture2D signaliaLogo;
        public const string discordUrl = "https://discord.gg/QcFnVfQj5K";
        public const string reviewUrl = "https://assetstore.unity.com/packages/slug/311320";
        public const string support = "https://ahakuo.com/contact-page";

        static Welcomefare()
        {
            EditorApplication.delayCall += OpenWindow;
        }

        private static void OpenWindow()
        {
            if (EditorPrefs.GetBool(FrameworkConstants.EditorPrefsKeys.WELCOME_FARE, false))
            {
                return;
            }

            Welcomefare window = GetWindow<Welcomefare>(true, "Signalia: Getting Started", true);
            window.minSize = new Vector2(410, 500);
            window.maxSize = new Vector2(410, 500);
            window.Show();
        }

        [MenuItem("Tools/Signalia/Thank You Page")]
        private static void ShowWindow()
        {
            Welcomefare window = GetWindow<Welcomefare>(true, "Signalia: Getting Started", true);
            window.minSize = new Vector2(410, 500);
            window.maxSize = new Vector2(410, 500);
            window.Show();
        }

        private void OnEnable()
        {
            signaliaLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(FrameworkConstants.GraphicPaths.HEADER_SIGNALIA_THANKYOU);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // Centered Signalia Logo
            if (signaliaLogo)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(signaliaLogo, GUILayout.Width(256), GUILayout.Height(256));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // Centered Thank You Text
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Thank you for purchasing Signalia!", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Your support keeps development going.", EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Leaving a review helps Signalia grow!", EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // getting started local doc opener
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Getting Started", GUILayout.Width(120)))
            {
                string relativePath = FrameworkConstants.MiscPaths.GETTING_STARTED;
                string fullPath = Path.Combine(Application.dataPath, relativePath.Substring("Assets/".Length));

                try
                {
                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError("Failed to open Getting Started file: " + e.Message);
                    EditorUtility.DisplayDialog("Error", "Failed to open Getting Started documentation. Please check if the file exists at: " + fullPath, "OK");
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // review button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Leave a Review", GUILayout.Width(120)))
            {
                Application.OpenURL(reviewUrl);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // centered support section
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Need help or have a feature request?", EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Join Discord", GUILayout.Width(120)))
            {
                Application.OpenURL(discordUrl);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Support", GUILayout.Width(120)))
            {
                Application.OpenURL(support);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Centered Close Button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close and Don't Show Again", GUILayout.Width(256)))
            {
                Close();
                EditorPrefs.SetBool(FrameworkConstants.EditorPrefsKeys.WELCOME_FARE, true);
                EditorUtility.DisplayDialog("Thank you!", "Thank you for purchasing Signalia! You can find the documentation under Tools > Signalia > Documentations. To show this page again, use Signalia > Thank You Page", "OK");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}