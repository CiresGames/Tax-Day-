#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AHAKuo.Signalia.GameSystems.Localization.Internal.ThirdParty;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal.Editors
{
    /// <summary>
    /// Factory for creating TextMeshPro font assets from Unity Font assets with specialized character set support.
    /// </summary>
    public class SignaliaTMPFontFactory : EditorWindow
    {
        private const string WindowTitle = "Signalia TMP Font Factory";

        [SerializeField]
        private Font sourceFont;

        [SerializeField]
        private TMP_FontAsset fallbackFont;

        private SerializedObject serialized;

        #region Unicode Character Sets

        private static readonly (uint start, uint end)[] BasicLatinRanges =
        {
            (0x0020, 0x007F)
        };

        private static readonly (uint start, uint end)[] ExtendedLatinRanges =
        {
            (0x0080, 0x00FF),
            (0x0100, 0x017F),
            (0x0180, 0x024F)
        };

        private static readonly (uint start, uint end)[] ArabicRanges =
        {
            (0x0600, 0x06FF),
            (0x0750, 0x077F),
            (0x08A0, 0x08FF)
        };

        private static readonly (uint start, uint end)[] ArabicPresentationRanges =
        {
            (0xFB50, 0xFDFF),
            (0xFE70, 0xFEFF),
            (0x1EE00, 0x1EEFF)
        };

        private static readonly uint[] EssentialArabicPresentationForms =
        {
            0xFE80, 0xFE81, 0xFE82, 0xFE83, 0xFE84, 0xFE85, 0xFE86, 0xFE87, 0xFE88,
            0xFE89, 0xFE8A, 0xFE8B, 0xFE8C, 0xFE8D, 0xFE8E, 0xFE8F, 0xFE90, 0xFE91,
            0xFE92, 0xFE93, 0xFE94, 0xFE95, 0xFE96, 0xFE97, 0xFE98, 0xFE99, 0xFE9A,
            0xFE9B, 0xFE9C, 0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0, 0xFEA1, 0xFEA2, 0xFEA3,
            0xFEA4, 0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8, 0xFEA9, 0xFEAA, 0xFEAB, 0xFEAC,
            0xFEAD, 0xFEAE, 0xFEAF, 0xFEB0, 0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4, 0xFEB5,
            0xFEB6, 0xFEB7, 0xFEB8, 0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC, 0xFEBD, 0xFEBE,
            0xFEBF, 0xFEC0, 0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4, 0xFEC5, 0xFEC6, 0xFEC7,
            0xFEC8, 0xFEC9, 0xFECA, 0xFECB, 0xFECC, 0xFECD, 0xFECE, 0xFECF, 0xFED0,
            0xFED1, 0xFED2, 0xFED3, 0xFED4, 0xFED5, 0xFED6, 0xFED7, 0xFED8, 0xFED9,
            0xFEDA, 0xFEDB, 0xFEDC, 0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0, 0xFEE1, 0xFEE2,
            0xFEE3, 0xFEE4, 0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8, 0xFEE9, 0xFEEA, 0xFEEB,
            0xFEEC, 0xFEED, 0xFEEE, 0xFEEF, 0xFEF0, 0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4,
            0xFEF5, 0xFEF6, 0xFEF7, 0xFEF8, 0xFEF9, 0xFEFA, 0xFEFB, 0xFEFC
        };

        #endregion

        [MenuItem("Tools/Signalia/Game Systems/Localization/Font Factory", false, 100)]
        public static void Open()
        {
            var window = GetWindow<SignaliaTMPFontFactory>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(320f, 200f);
            window.Show();
        }

        [MenuItem("Assets/Create/Signalia/Game Systems/Localization/Create TMP Font Asset", false, 1000)]
        private static void GenerateTMPFontFromSelected()
        {
            Font font = null;
            var activeObject = Selection.activeObject;
            
            if (activeObject is Font)
            {
                font = activeObject as Font;
            }
            else
            {
                // Try to get font from the selected asset path
                var path = AssetDatabase.GetAssetPath(activeObject);
                if (!string.IsNullOrEmpty(path) && (path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)))
                {
                    font = AssetDatabase.LoadAssetAtPath<Font>(path);
                }
            }
            
            if (font != null)
            {
                var fontPath = AssetDatabase.GetAssetPath(font);
                var directory = Path.GetDirectoryName(fontPath);
                var fileName = Path.GetFileNameWithoutExtension(fontPath);
                var savePath = Path.Combine(directory, fileName + "_SDF.asset").Replace('\\', '/');
                
                var generatedFont = GenerateTMPFont(font, savePath);
                if (generatedFont != null)
                {
                    Selection.activeObject = generatedFont;
                }
            }
        }

        [MenuItem("Assets/Create/Signalia/Game Systems/Localization/Create TMP Font Asset", true)]
        private static bool ValidateGenerateTMPFontFromSelected()
        {
            var activeObject = Selection.activeObject;
            
            // Check if it's directly a Font
            if (activeObject is Font)
            {
                return true;
            }
            
            // Check if the asset path ends with font extensions
            var path = AssetDatabase.GetAssetPath(activeObject);
            if (!string.IsNullOrEmpty(path))
            {
                if (path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                {
                    // Verify we can load it as a Font
                    var font = AssetDatabase.LoadAssetAtPath<Font>(path);
                    return font != null;
                }
            }
            
            return false;
        }

        private void OnEnable()
        {
            serialized = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serialized.Update();

            DrawFontSettings();
            EditorGUILayout.Space();
            DrawGenerateButton();

            serialized.ApplyModifiedProperties();
        }

        private void DrawFontSettings()
        {
            EditorGUILayout.LabelField("Font Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(sourceFont)), new GUIContent("Source Font"));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(fallbackFont)), new GUIContent("Fallback Font"));
        }

        private void DrawGenerateButton()
        {
            using (new EditorGUI.DisabledScope(serialized.FindProperty(nameof(sourceFont)).objectReferenceValue == null))
            {
                if (GUILayout.Button("Generate TMP Font Asset", GUILayout.Height(32f)))
                {
                    if (sourceFont == null)
                    {
                        EditorUtility.DisplayDialog(WindowTitle, "A source font must be assigned before generating a TMP Font Asset.", "OK");
                        return;
                    }

                    var path = EditorUtility.SaveFilePanelInProject(
                        "Save TMP Font Asset",
                        sourceFont.name + "_ArabicSDF",
                        "asset",
                        "Choose where to save the generated TMP Font Asset");

                    if (string.IsNullOrEmpty(path))
                    {
                        return;
                    }
            
                    var fontAsset = GenerateTMPFont(
                        sourceFont,
                        savePath: path,
                        fallbackFont: fallbackFont,
                        showDialog: true);

                    if (fontAsset != null)
                    {
                        Selection.activeObject = fontAsset;
                    }
                }
            }
        }

        private static void SaveFontAsset(string path, TMP_FontAsset fontAsset)
        {
            AssetDatabase.CreateAsset(fontAsset, path);

            var fontName = fontAsset.name;
            var atlasIndex = 0;

            foreach (var texture in fontAsset.atlasTextures)
            {
                if (texture != null)
                {
                    if (fontAsset.atlasTextures.Length > 1)
                    {
                        texture.name = $"{fontName} Atlas {atlasIndex}";
                    }
                    else
                    {
                        texture.name = $"{fontName} Atlas";
                    }
                    AssetDatabase.AddObjectToAsset(texture, fontAsset);
                    atlasIndex++;
                }
            }

            if (fontAsset.atlasTexture != null && Array.IndexOf(fontAsset.atlasTextures, fontAsset.atlasTexture) < 0)
            {
                fontAsset.atlasTexture.name = $"{fontName} Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.mainTexture = fontAsset.atlasTexture;
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            fontAsset.ReadFontAssetDefinition();
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AppendRange(ISet<uint> characters, (uint start, uint end)[] ranges)
        {
            foreach (var (start, end) in ranges)
            {
                for (uint codePoint = start; codePoint <= end; codePoint++)
                {
                    characters.Add(codePoint);
                }
            }
        }

        private static void AppendCharacters(ISet<uint> characters, IEnumerable<uint> values)
        {
            foreach (uint codePoint in values)
            {
                characters.Add(codePoint);
            }
        }

        /// <summary>
        /// Generates a TMP font asset from a Unity Font with all character sets included by default.
        /// </summary>
        /// <param name="sourceFont">The Unity Font to convert</param>
        /// <param name="savePath">Optional: The path to save the font asset. If null, will prompt user.</param>
        /// <param name="fallbackFont">Fallback font to assign. Default: null</param>
        /// <param name="showDialog">Whether to show success/error dialogs. Default: false</param>
        /// <returns>The created TMP_FontAsset, or null if generation failed</returns>
        public static TMP_FontAsset GenerateTMPFont(
            Font sourceFont,
            string savePath = null,
            TMP_FontAsset fallbackFont = null,
            bool showDialog = false)
        {
            var fontToUse = sourceFont;
            
            // check if user wants to fix Arabic mapping
            //var fixArabicMapping = false || EditorUtility.DisplayDialog("Arabic Mapping Fix", "The Arabic glyphs in the source font are not mapped correctly. Would you like to preprocess the font to fix this?", "Yes", "No");
            EditorUtility.DisplayDialog("Arabic Mapping Fix",
                "If your font has problems with arabic glyphs, you can try preprocessing it with the Lingramia utility. In Lingramia: Fonts > Preprocess Font [Unity].",
                "Okay");
            
            // // Preprocess font if Arabic mapping fix is requested
            // if (fixArabicMapping)
            // {
            //     var originalFontPath = AssetDatabase.GetAssetPath(sourceFont);
            //     var preprocessedPath = PreProcessedFont(originalFontPath);
            //     
            //     if (!string.IsNullOrEmpty(preprocessedPath))
            //     {
            //         // Refresh AssetDatabase to ensure Unity recognizes the newly created font file
            //         AssetDatabase.Refresh();
            //         
            //         fontToUse = AssetDatabase.LoadAssetAtPath<Font>(preprocessedPath);
            //         if (fontToUse == null)
            //         {
            //             Debug.LogWarning($"[{WindowTitle}] Failed to load preprocessed font at {preprocessedPath}. Using original font instead.");
            //             fontToUse = sourceFont;
            //         }
            //     }
            //     else
            //     {
            //         Debug.LogWarning($"[{WindowTitle}] Font preprocessing failed. Using original font instead.");
            //         fontToUse = sourceFont;
            //     }
            // }
            
            if (fontToUse == null)
            {
                var errorMsg = $"[{WindowTitle}] Source font is null.";
                Debug.LogError(errorMsg);
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(WindowTitle, "A source font must be assigned before generating a TMP Font Asset.", "OK");
                }
                return null;
            }

            if (string.IsNullOrEmpty(savePath))
            {
                savePath = EditorUtility.SaveFilePanelInProject(
                    "Save TMP Font Asset",
                    sourceFont.name + "_SDF",
                    "asset",
                    "Choose where to save the generated TMP Font Asset");

                if (string.IsNullOrEmpty(savePath))
                {
                    return null;
                }
            }

            try
            {
                var fontAsset = TMP_FontAsset.CreateFontAsset(fontToUse);

                if (fontAsset == null)
                {
                    throw new InvalidOperationException("TMP was unable to create the font asset from the provided font.");
                }

                fontAsset.name = Path.GetFileNameWithoutExtension(savePath);

                // Add character ranges based on settings
                var charactersToAdd = new HashSet<uint>();

                // Essential Arabic presentation forms are always included
                AppendCharacters(charactersToAdd, EssentialArabicPresentationForms);

                if (charactersToAdd.Count > 0)
                {
                    uint[] charactersArray = charactersToAdd.ToArray();
                    Array.Sort(charactersArray);
                    fontAsset.TryAddCharacters(charactersArray, out uint[] missingCharacters, includeFontFeatures: true);

                    if (missingCharacters != null && missingCharacters.Length > 0)
                    {
                        var missingSet = new HashSet<uint>(missingCharacters);
                        var missingEssential = EssentialArabicPresentationForms.Where(missingSet.Contains).ToArray();

                        if (missingEssential.Length > 0)
                        {
                            var missingEssentialDisplay = string.Join(", ", missingEssential.Select(codePoint => $"U+{codePoint:X4}"));
                            Debug.LogWarning($"[{WindowTitle}] Missing essential Arabic presentation glyphs: {missingEssentialDisplay}. Characters may appear disconnected unless a fallback font is assigned.");
                        }

                        var missingOther = missingCharacters.Except(missingEssential).ToArray();

                        if (missingOther.Length > 0)
                        {
                            var missingDisplay = string.Join(", ", missingOther.Select(codePoint => $"U+{codePoint:X4}"));
                            Debug.LogWarning($"[{WindowTitle}] Missing glyphs for: {missingDisplay}");
                        }
                    }
                }

                // Assign fallback font if provided
                if (fallbackFont != null)
                {
                    if (fontAsset.fallbackFontAssetTable == null)
                    {
                        fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    }

                    if (!fontAsset.fallbackFontAssetTable.Contains(fallbackFont))
                    {
                        fontAsset.fallbackFontAssetTable.Add(fallbackFont);
                    }
                }

                SaveFontAsset(savePath, fontAsset);

                if (showDialog)
                {
                    EditorUtility.DisplayDialog(WindowTitle, "TMP Font Asset generated successfully.", "OK");
                }
                else
                {
                    Debug.Log($"[{WindowTitle}] Successfully generated TMP font asset: {savePath}");
                }

                return fontAsset;
            }
            catch (Exception ex)
            {
                var errorMsg = $"[{WindowTitle}] Failed to generate TMP Font Asset. {ex.Message}\n{ex.StackTrace}";
                Debug.LogError(errorMsg);
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(WindowTitle, "Failed to generate the TMP Font Asset. See the console for details.", "OK");
                }
                return null;
            }
        }

        /// <summary>
        /// Returns a tmp path to the preprocessed font.
        /// </summary>
        /// <param name="pathToOriginal"></param>
        /// <param name="deletedAfter">Delete the preprocessed font after the generated tmp font is created.</param>
        /// <returns></returns>
        [Obsolete("Unity doesn't allow exe files, so this has been deprecated. Use it from Lingramia through Engines > Unity Font Fixing")]
        private static string PreProcessedFont(string pathToOriginal, bool deletedAfter = true)
        {
            return FontFactoryPyBridge.PreprocessFontPath(pathToOriginal);
        }
    }
}
#endif
