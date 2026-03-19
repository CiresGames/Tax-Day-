using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.Framework.Packages;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.Framework.Editors
{
    public class PackageManagerWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private float maxWidthProperties => position.width - 20;

        [MenuItem(FrameworkConstants.DebugTutorials.ToolbarPath_Purchases)]
        public static void ShowWindow()
        {
            GetWindow<PackageManagerWindow>("Signalia Packages").minSize = new Vector2(800, 600);
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(GraphicLoader.SignaliaPackages, GUILayout.Height(256), GUILayout.Width(256));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Signalia game systems catalog. All of these systems are part of the toolkit and powered by its core code and utilities.", MessageType.Info);
            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            DrawPackageGrid();

            GUILayout.EndScrollView();
        }

        private void DrawPackageGrid()
        {
            var packages = PackageInfos.Packages();
            int columns = 2;
            int rows = Mathf.CeilToInt((float)packages.Length / columns);

            for (int row = 0; row < rows; row++)
            {
                GUILayout.BeginHorizontal();

                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if (index < packages.Length)
                    {
                        DrawPackageCard(packages[index]);
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                    }

                    if (col < columns - 1 && index < packages.Length - 1)
                    {
                        GUILayout.Space(10);
                    }
                }

                GUILayout.EndHorizontal();

                if (row < rows - 1)
                {
                    GUILayout.Space(15);
                }
            }
        }

        private void DrawPackageCard(PackageData package)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((maxWidthProperties - 10) / 2));

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            GUILayout.Label(package.title, titleStyle);

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            Texture2D packageImage = GraphicLoader.LoadByName(package.headerImage);
            if (packageImage != null)
            {
                GUILayout.Label(packageImage, GUILayout.Height(128), GUILayout.Width(128));
            }
            else
            {
                GUILayout.Box("", GUILayout.Height(128), GUILayout.Width(128));
            }

            GUILayout.Space(10);

            GUILayout.BeginVertical();
            GUILayout.Label(package.description, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            int? settingsTab = GameSystemTabIndex(package.package);
            if (settingsTab.HasValue)
            {
                if (GUILayout.Button("⚙ Settings", GUILayout.Height(25)))
                {
                    FrameworkSettings.OpenToTab(7, settingsTab.Value);
                }
            }

            GUILayout.EndVertical();
        }

        private static int? GameSystemTabIndex(string packageId)
        {
            return packageId switch
            {
                "SIGS_DG"   => 0,
                "SIGS_SAVE" => 1,
                "SIGS_INV"  => 2,
                "SIGS_PS"   => 3,
                "SIGS_LSS"  => 4,
                "SIGS_LS"   => 5,
                "SIGS_AL"   => 6,
                "SIGS_TUT"  => 7,
                "SIGS_RC"   => 8,
                "SIGS_CMN"  => 9,
                "SIGS_ACH"  => 10,
                _           => null
            };
        }
    }
}
