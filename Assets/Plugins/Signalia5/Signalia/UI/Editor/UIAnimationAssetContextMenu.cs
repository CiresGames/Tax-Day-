using System.Collections.Generic;
using System.IO;
using System.Linq;
using AHAKuo.Signalia.UI;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.UI.Editors
{
    public static class UIAnimationAssetContextMenu
    {
        private const string MenuPath = "Assets/Create Negative Duplicate";

        [MenuItem(MenuPath, true)]
        private static bool ValidateCreateNegativeDuplicate()
        {
            return Selection.objects.OfType<UIAnimationAsset>().Any();
        }

        [MenuItem(MenuPath, false, 2100)]
        private static void CreateNegativeDuplicate()
        {
            var selectedAssets = Selection.objects.OfType<UIAnimationAsset>().ToArray();
            if (selectedAssets.Length == 0)
            {
                return;
            }

            var createdAssets = new List<Object>();

            foreach (var animationAsset in selectedAssets)
            {
                var sourcePath = AssetDatabase.GetAssetPath(animationAsset);
                var directory = Path.GetDirectoryName(sourcePath);
                var baseName = Path.GetFileNameWithoutExtension(sourcePath);
                var newAsset = animationAsset.CreateNegativeDuplicate();
                newAsset.name = $"{baseName} Negative";

                var targetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, $"{newAsset.name}.asset"));
                AssetDatabase.CreateAsset(newAsset, targetPath);
                createdAssets.Add(newAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (createdAssets.Count > 0)
            {
                Selection.objects = createdAssets.ToArray();
            }
        }
    }
}
