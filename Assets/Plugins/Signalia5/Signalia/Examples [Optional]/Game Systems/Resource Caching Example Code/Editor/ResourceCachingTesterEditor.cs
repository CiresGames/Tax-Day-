using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.ResourceCaching.Examples;
using AHAKuo.Signalia.Framework.Editors;

namespace AHAKuo.Signalia.Examples.ResourceCaching
{
    /// <summary>
    /// Custom editor for ResourceCachingTester providing testing buttons for all Resource Caching methods.
    /// </summary>
    [CustomEditor(typeof(ResourceCachingTester))]
    public class ResourceCachingTesterEditor : UnityEditor.Editor
    {
        private ResourceCachingTester tester;
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            tester = (ResourceCachingTester)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Resource Caching Tester", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This component provides comprehensive testing for all Resource Caching methods. Use the buttons below to test different functionality.", MessageType.Info);
            EditorGUILayout.Space(10);

            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(20);

            // Testing Buttons Section
            EditorGUILayout.LabelField("Testing Controls", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // SIGS Methods Testing
            EditorGUILayout.LabelField("SIGS Methods Testing", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Test SIGS.GetResource<T>()", GUILayout.Height(25)))
            {
                tester.TestSIGSGetResource();
            }
            
            if (GUILayout.Button("Test SIGS.HasResource()", GUILayout.Height(25)))
            {
                tester.TestSIGSHasResource();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Test SIGS.GetAllKeys()", GUILayout.Height(25)))
            {
                tester.TestSIGSGetAllResourceKeys();
            }
            
            if (GUILayout.Button("Test SIGS.GetCacheSize()", GUILayout.Height(25)))
            {
                tester.TestSIGSGetResourceCacheSize();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Extension Methods Testing
            EditorGUILayout.LabelField("Extension Methods Testing", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Test string.GetResource<T>()", GUILayout.Height(25)))
            {
                tester.TestStringGetResource();
            }
            
            if (GUILayout.Button("Test string.HasResource()", GUILayout.Height(25)))
            {
                tester.TestStringHasResource();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Test GameObject.LoadAsResource<T>()", GUILayout.Height(25)))
            {
                tester.TestGameObjectLoadAsResource();
            }

            EditorGUILayout.Space(10);

            // Advanced Testing
            EditorGUILayout.LabelField("Advanced Testing", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Test Resource Instantiation", GUILayout.Height(25)))
            {
                tester.TestResourceInstantiation();
            }
            
            if (GUILayout.Button("Test Audio Playback", GUILayout.Height(25)))
            {
                tester.TestAudioPlayback();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Comprehensive Testing
            EditorGUILayout.LabelField("Comprehensive Testing", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            
            GUIStyle runAllStyle = new GUIStyle(GUI.skin.button);
            runAllStyle.fontSize = 14;
            runAllStyle.fontStyle = FontStyle.Bold;
            runAllStyle.normal.textColor = Color.green;
            
            if (GUILayout.Button("🚀 Run All Tests", runAllStyle, GUILayout.Height(35)))
            {
                tester.RunAllTests();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Test Results Display
            EditorGUILayout.LabelField("Test Results", EditorStyles.boldLabel);
            
            // Create a scrollable area for test results
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            
            string testResult = tester.GetLastTestResult();
            EditorGUILayout.TextArea(testResult, GUILayout.ExpandHeight(true));
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Clear Results", GUILayout.Height(25)))
            {
                tester.ClearTestResults();
            }
            
            if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(25)))
            {
                EditorGUIUtility.systemCopyBuffer = testResult;
                Debug.Log("Test results copied to clipboard.");
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Help Section
            EditorGUILayout.LabelField("Help & Information", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Set the test keys above to match your ResourceAsset entries\n" +
                "• Use 'Load Resource Assets' in Signalia Settings to populate the cache\n" +
                "• Check the Console for detailed test output\n" +
                "• Test results are displayed above and logged to Console\n" +
                "• Resource instantiation creates temporary objects that auto-destroy\n" +
                "• Audio playback requires an AudioSource component",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Open Signalia Settings", GUILayout.Height(25)))
            {
                FrameworkSettings.ShowWindow();
            }
            
            if (GUILayout.Button("Open Package Manager", GUILayout.Height(25)))
            {
                PackageManagerWindow.ShowWindow();
            }
            
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
