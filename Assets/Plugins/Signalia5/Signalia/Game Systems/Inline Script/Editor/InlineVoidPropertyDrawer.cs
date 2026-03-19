#if UNITY_EDITOR
using AHAKuo.Signalia.GameSystems.InlineScript;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.InlineScript.Internal.Editor
{
    [CustomPropertyDrawer(typeof(ISB_TopLayer), true)]
    internal class InlineScriptPropertyDrawer : PropertyDrawer
    {
        private const float MinimumCodeLines = 6f;
        private const float MinimumUsingLines = 2f;
        private const float MinimumDefinitionLines = 3f;
        private const float MaximumVisibleCodeLines = 18f;
        private const float CodeScrollbarWidth = 16f;
        private static readonly RectOffset CodeContentPadding = new RectOffset(26, 14, 10, 12);
        private const float SectionLabelSpacing = 2f;
        private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        private const float CompileButtonMinWidth = 80f;
        private const float CompileButtonMaxWidth = 140f;
        private const float CompileButtonHorizontalPadding = 8f;
        private const float CompileButtonVerticalPadding = 12f;
        private const float ActionButtonSpacing = 6f;
        private const float UtilityButtonMinWidth = 52f;
        private const float UtilityButtonMaxWidth = 96f;
        private const string AdditionalUsingsInfo = "Optional namespace imports added above global inline-script usings. Enter one 'using' per line.";
        private const string CachedDefinitionsInfo = "Cached fields or properties created outside the generated Execute method. Use for references that should be initialized once.";

        private static readonly GUIContent AdditionalUsingsInfoContent = new GUIContent(AdditionalUsingsInfo);
        private static readonly GUIContent CachedDefinitionsInfoContent = new GUIContent(CachedDefinitionsInfo);

        private static readonly Dictionary<string, bool> AdditionalFoldoutStates = new Dictionary<string, bool>();
        private static readonly Dictionary<string, bool> DefinitionFoldoutStates = new Dictionary<string, bool>();
        private static readonly Dictionary<string, Vector2> CodeScrollPositions = new Dictionary<string, Vector2>();
        private static readonly Dictionary<string, TransientStatusMessage> TransientStatusMessages = new Dictionary<string, TransientStatusMessage>();

        private static readonly GUIContent CopyButtonContent = new GUIContent("Copy", "Copy the inline script, including GUID and compiled script reference.");
        private static readonly GUIContent PasteButtonContent = new GUIContent("Paste", "Paste the copied inline script, reusing the same compiled script asset.");
        private static readonly GUIContent DisconnectButtonContent = new GUIContent("Disconnect", "Clear the inline script and unlink it from the generated script asset.");

        private const string DisconnectConfirmTitle = "Disconnect Inline Script";
        private const string DisconnectConfirmMessage = "This will clear the inline script fields and disconnect from the generated script asset. The inline script will no longer reference the previous compiled file. Proceed?";
        private const string DisconnectConfirmOk = "Disconnect";
        private const string DisconnectConfirmCancel = "Cancel";

        private const string DeleteScriptConfirmTitle = "Delete Source Script";
        private const string DeleteScriptConfirmMessage = "Do you also want to delete the source script? Note that any other field connected via the same guid will lose reference.";
        private const string DeleteScriptConfirmYes = "Yes";
        private const string DeleteScriptConfirmNo = "No";

        private const string CopyConfirmTitle = "Copy Inline Script";
        private const string CopyConfirmMessage = "Copy the inline script contents, GUID, and compiled asset reference to the clipboard?";
        private const string CopyConfirmOk = "Copy";
        private const string CopyConfirmCancel = "Cancel";

        private const string PasteConfirmTitle = "Paste Inline Script";
        private const string PasteConfirmMessage = "Paste the inline script from the clipboard, replacing the current contents and linking to the copied compiled asset?";
        private const string PasteConfirmOk = "Paste";
        private const string PasteConfirmCancel = "Cancel";

        private const double TransientStatusDuration = 3.5d;
        private static readonly Color TransientPasteColor = new Color(0.55f, 0.85f, 1f, 1f);
        private static readonly Color TransientDisconnectColor = new Color(0.95f, 0.65f, 0.25f, 1f);

        private struct TransientStatusMessage
        {
            public string Text;
            public Color Color;
            public double ExpireTime;
        }

        private static GUIStyle _codeStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _statusStyle;
        private static GUIStyle _highlightStyle;
        private static GUIStyle _braceStyle;
        private static GUIStyle _metricsStyle;
        private static GUIStyle _supplementaryTextStyle;
        private static Texture2D _transparentTexture;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            EnsureStyles();

            var height = LineHeight; // header / foldout
            height += EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                var propertyKey = InlineScriptEditorHelper.BuildPropertyKey(property);
                var additionalKey = BuildAdditionalKey(propertyKey);
                var definitionKey = BuildDefinitionKey(propertyKey);
                var sourceProp = property.FindPropertyRelative("_sourceCode");
                var additionalUsingsProp = property.FindPropertyRelative("_additionalUsings");
                var cachedDefinitionsProp = property.FindPropertyRelative("_cachedDefinitions");
                var codeHeight = CalculateCodeAreaHeight(sourceProp?.stringValue);
                var additionalExpanded = GetSectionExpanded(AdditionalFoldoutStates, additionalKey);
                var definitionExpanded = GetSectionExpanded(DefinitionFoldoutStates, definitionKey);

                height += LineHeight;
                if (additionalExpanded)
                {
                    height += SectionLabelSpacing;
                    height += CalculateHelpBoxHeight(AdditionalUsingsInfoContent);
                    height += SectionLabelSpacing;
                    height += CalculateTextAreaHeight(additionalUsingsProp?.stringValue, MinimumUsingLines);
                }
                height += EditorGUIUtility.standardVerticalSpacing;

                height += LineHeight;
                if (definitionExpanded)
                {
                    height += SectionLabelSpacing;
                    height += CalculateHelpBoxHeight(CachedDefinitionsInfoContent);
                    height += SectionLabelSpacing;
                    height += CalculateTextAreaHeight(cachedDefinitionsProp?.stringValue, MinimumDefinitionLines);
                }
                height += EditorGUIUtility.standardVerticalSpacing;
                height += codeHeight;
                height += LineHeight + CompileButtonVerticalPadding;
                height += EditorGUIUtility.standardVerticalSpacing;

                var validation = InlineScriptEditorHelper.ValidateSource(sourceProp?.stringValue ?? string.Empty);
                if (validation.Count > 0)
                {
                    var viewWidth = EditorGUIUtility.currentViewWidth - 40f;
                    foreach (var result in validation)
                    {
                        var content = new GUIContent(result.message);
                        height += EditorStyles.helpBox.CalcHeight(content, viewWidth);
                        height += EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var propertyKey = InlineScriptEditorHelper.BuildPropertyKey(property);
            var additionalKey = BuildAdditionalKey(propertyKey);
            var definitionKey = BuildDefinitionKey(propertyKey);

            var sourceProp = property.FindPropertyRelative("_sourceCode");
            var additionalUsingsProp = property.FindPropertyRelative("_additionalUsings");
            var cachedDefinitionsProp = property.FindPropertyRelative("_cachedDefinitions");
            var compiledScriptProp = property.FindPropertyRelative("_compiledScriptAsset");
            var guidProp = property.FindPropertyRelative("_guid");

            var profile = InlineScriptTypeProfileRegistry.GetProfile(fieldInfo?.FieldType);

            var hasCopyContent = InlineScriptEditorHelper.HasCopyableContent(sourceProp, additionalUsingsProp,
                cachedDefinitionsProp, compiledScriptProp, guidProp);
            var isConnected = HasConnection(compiledScriptProp, guidProp);

            EnsureStyles();

            var validation = InlineScriptEditorHelper.ValidateSource(sourceProp?.stringValue ?? string.Empty);
            var compileStatus = InlineScriptEditorHelper.DetermineCompileStatus(propertyKey, sourceProp, compiledScriptProp,
                additionalUsingsProp, cachedDefinitionsProp);

            var buttonLabel = compiledScriptProp?.objectReferenceValue != null ? "Compile Update" : "Compile";

            var availableWidth = Mathf.Max(0f, position.width - (CompileButtonHorizontalPadding * 2f));
            var minWidth = Mathf.Min(CompileButtonMinWidth, availableWidth);
            var maxWidth = Mathf.Min(CompileButtonMaxWidth, availableWidth);
            if (maxWidth < minWidth)
            {
                maxWidth = minWidth;
            }
            var buttonWidth = Mathf.Clamp(position.width * 0.4f, minWidth, maxWidth);
            var utilityMax = Mathf.Min(UtilityButtonMaxWidth, buttonWidth);
            var utilityMin = Mathf.Min(UtilityButtonMinWidth, utilityMax);
            var utilityButtonWidth = Mathf.Clamp(buttonWidth * 0.6f, utilityMin, utilityMax);
            var showDiscard = compileStatus == InlineScriptEditorHelper.CompileStatus.Dirty &&
                              compiledScriptProp != null && compiledScriptProp.objectReferenceValue != null;
            var showCopy = hasCopyContent;
            var showDisconnect = isConnected;
            var discardButtonWidth = showDiscard ? buttonWidth : 0f;
            var buttonHeight = LineHeight;
            var headerRect = new Rect(position.x, position.y, position.width, LineHeight);
            const float collapsedPadding = 0f;

            DrawHeader(headerRect, property, label, compileStatus, collapsedPadding, propertyKey);

            var contentY = headerRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                var additionalExpanded = GetSectionExpanded(AdditionalFoldoutStates, additionalKey);
                var additionalHeaderRect = new Rect(position.x, contentY, position.width, LineHeight);
                EditorGUI.BeginChangeCheck();
                var newAdditionalExpanded = EditorGUI.Foldout(additionalHeaderRect, additionalExpanded,
                    new GUIContent("Additional Usings"), true);
                if (EditorGUI.EndChangeCheck())
                {
                    SetSectionExpanded(AdditionalFoldoutStates, additionalKey, newAdditionalExpanded);
                    additionalExpanded = newAdditionalExpanded;
                }

                contentY = additionalHeaderRect.yMax + SectionLabelSpacing;

                if (additionalExpanded)
                {
                    var additionalInfoHeight = CalculateHelpBoxHeight(AdditionalUsingsInfoContent);
                    var additionalInfoRect = BuildContentRect(position, contentY, additionalInfoHeight);
                    EditorGUI.HelpBox(additionalInfoRect, AdditionalUsingsInfoContent.text, MessageType.Info);
                    contentY = additionalInfoRect.yMax + SectionLabelSpacing;

                    var additionalHeight = CalculateTextAreaHeight(additionalUsingsProp?.stringValue, MinimumUsingLines);
                    var additionalRect = BuildContentRect(position,
                        contentY,
                        additionalHeight);

                    if (additionalUsingsProp != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        var newAdditional = EditorGUI.TextArea(additionalRect, additionalUsingsProp.stringValue, _supplementaryTextStyle);
                        if (EditorGUI.EndChangeCheck())
                        {
                            additionalUsingsProp.stringValue = newAdditional;
                            compileStatus = InlineScriptEditorHelper.DetermineCompileStatus(propertyKey, sourceProp,
                                compiledScriptProp, additionalUsingsProp, cachedDefinitionsProp);
                        }
                    }
                    else
                    {
                        EditorGUI.HelpBox(additionalRect, "Additional usings are not available outside of the Unity Editor.",
                            MessageType.Info);
                    }

                    contentY = additionalRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                }
                else
                {
                    contentY = additionalHeaderRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                }

                var definitionExpanded = GetSectionExpanded(DefinitionFoldoutStates, definitionKey);
                var definitionHeaderRect = new Rect(position.x, contentY, position.width, LineHeight);
                EditorGUI.BeginChangeCheck();
                var newDefinitionExpanded = EditorGUI.Foldout(definitionHeaderRect, definitionExpanded,
                    new GUIContent("Cached Definitions"), true);
                if (EditorGUI.EndChangeCheck())
                {
                    SetSectionExpanded(DefinitionFoldoutStates, definitionKey, newDefinitionExpanded);
                    definitionExpanded = newDefinitionExpanded;
                }

                contentY = definitionHeaderRect.yMax + SectionLabelSpacing;

                if (definitionExpanded)
                {
                    var definitionInfoHeight = CalculateHelpBoxHeight(CachedDefinitionsInfoContent);
                    var definitionInfoRect = BuildContentRect(position, contentY, definitionInfoHeight);
                    EditorGUI.HelpBox(definitionInfoRect, CachedDefinitionsInfoContent.text, MessageType.Info);
                    contentY = definitionInfoRect.yMax + SectionLabelSpacing;

                    var definitionHeight = CalculateTextAreaHeight(cachedDefinitionsProp?.stringValue, MinimumDefinitionLines);
                    var definitionRect = BuildContentRect(position,
                        contentY,
                        definitionHeight);

                    if (cachedDefinitionsProp != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        var newDefinitions = EditorGUI.TextArea(definitionRect, cachedDefinitionsProp.stringValue, _supplementaryTextStyle);
                        if (EditorGUI.EndChangeCheck())
                        {
                            cachedDefinitionsProp.stringValue = newDefinitions;
                            compileStatus = InlineScriptEditorHelper.DetermineCompileStatus(propertyKey, sourceProp,
                                compiledScriptProp, additionalUsingsProp, cachedDefinitionsProp);
                        }
                    }
                    else
                    {
                        EditorGUI.HelpBox(definitionRect,
                            "Cached definitions are not available outside of the Unity Editor.", MessageType.Info);
                    }

                    contentY = definitionRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                }
                else
                {
                    contentY = definitionHeaderRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                }

                var codeContentHeight = CalculateCodeContentHeight(sourceProp?.stringValue);
                var codeViewHeight = CalculateCodeViewHeight(sourceProp?.stringValue);
                var codeAreaHeight = codeViewHeight + CodeContentPadding.vertical;
                var codeRect = BuildContentRect(position,
                    contentY,
                    codeAreaHeight);

                DrawCodeBackground(codeRect);

                var braceTopRect = new Rect(codeRect.x + 6f + 4f,
                    codeRect.y + 4f,
                    Mathf.Max(0f, CodeContentPadding.left - 12f - 8f),
                    LineHeight * 1.2f);
                var braceBottomRect = new Rect(codeRect.x + 6f + 4f,
                    codeRect.yMax - CodeContentPadding.bottom - LineHeight * 1.2f,
                    Mathf.Max(0f, CodeContentPadding.left - 12f - 8f),
                    LineHeight * 1.2f);
                GUI.Label(braceTopRect, "{", _braceStyle);
                GUI.Label(braceBottomRect, "}", _braceStyle);

                var metricsRect = new Rect(codeRect.xMax - CodeContentPadding.right - 96f,
                    codeRect.y + 2f,
                    96f,
                    LineHeight);
                var lineCount = CalculateLineCount(sourceProp?.stringValue);
                GUI.Label(metricsRect, $"Lines: {lineCount}", _metricsStyle);

                var scrollRect = new Rect(
                    codeRect.x + CodeContentPadding.left,
                    codeRect.y + CodeContentPadding.top + LineHeight * 1.2f + 1f,
                    Mathf.Max(0f, codeRect.width - CodeContentPadding.horizontal),
                    Mathf.Max(0f, codeViewHeight - LineHeight * 1.2f * 2 - 2f));

                if (sourceProp != null)
                {
                    var scrollPosition = GetCodeScrollPosition(propertyKey);
                    var contentWidth = Mathf.Max(0f, scrollRect.width - CodeScrollbarWidth);
                    var currentValue = sourceProp.stringValue ?? string.Empty;
                    var horizontalWidth = Mathf.Max(contentWidth,
                        CalculateLongestLineWidth(currentValue) + _codeStyle.padding.horizontal + 4f);
                    var contentRect = new Rect(0f, 0f, horizontalWidth, Mathf.Max(codeContentHeight, 1f));

                    var newScrollPosition = GUI.BeginScrollView(scrollRect, scrollPosition, contentRect, false, true);

                    // Draw syntax highlight first so the caret renders on top
                    var textRect = new Rect(0f, 0f, contentRect.width, contentRect.height);
                    if (Event.current.type == EventType.Repaint)
                    {
                        var highlighted = SyntaxHighlightingUtility.ToRichText(currentValue);
                        GUI.Label(textRect, highlighted, _highlightStyle);
                    }

                    EditorGUI.BeginChangeCheck();

                    var cursorColor = GUI.skin.settings.cursorColor;
                    var selectionColor = GUI.skin.settings.selectionColor;
                    
                    string newValue;
                    try
                    {
                        GUI.skin.settings.cursorColor = new Color(0.85f, 0.95f, 1f, 1f);
                        // Restore a subtle, non-intrusive selection highlight
                        GUI.skin.settings.selectionColor = new Color(0.55f, 0.8f, 1f, 0.22f);

                        newValue = EditorGUI.TextArea(textRect, currentValue, _codeStyle);
                    }
                    finally
                    {
                        GUI.skin.settings.cursorColor = cursorColor;
                        GUI.skin.settings.selectionColor = selectionColor;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        sourceProp.stringValue = newValue;
                        currentValue = sourceProp.stringValue ?? string.Empty;
                        validation = InlineScriptEditorHelper.ValidateSource(currentValue);
                        compileStatus = InlineScriptEditorHelper.DetermineCompileStatus(propertyKey, sourceProp,
                            compiledScriptProp, additionalUsingsProp, cachedDefinitionsProp);
                    }

                    GUI.EndScrollView();

                    SetCodeScrollPosition(propertyKey, newScrollPosition);
                }
                else
                {
                    var infoRect = new Rect(scrollRect.x, scrollRect.y, scrollRect.width, scrollRect.height);
                    EditorGUI.HelpBox(infoRect, "Source code is not available outside of the Unity Editor.", MessageType.Info);
                }

                var expandedButtonRect = new Rect(
                    codeRect.xMax - buttonWidth - CompileButtonHorizontalPadding,
                    codeRect.yMax + (CompileButtonVerticalPadding * 0.5f),
                    buttonWidth,
                    buttonHeight);

                showCopy = InlineScriptEditorHelper.HasCopyableContent(sourceProp, additionalUsingsProp,
                    cachedDefinitionsProp, compiledScriptProp, guidProp);
                showDisconnect = HasConnection(compiledScriptProp, guidProp);

                var expandedNextX = expandedButtonRect.x - ActionButtonSpacing;
                var expandedPasteRect = new Rect(expandedNextX - utilityButtonWidth, expandedButtonRect.y,
                    utilityButtonWidth, buttonHeight);
                expandedNextX = expandedPasteRect.x - ActionButtonSpacing;

                Rect expandedCopyRect = default;
                if (showCopy)
                {
                    expandedCopyRect = new Rect(expandedNextX - utilityButtonWidth, expandedButtonRect.y,
                        utilityButtonWidth, buttonHeight);
                    expandedNextX = expandedCopyRect.x - ActionButtonSpacing;
                }

                Rect expandedDisconnectRect = default;
                if (showDisconnect)
                {
                    expandedDisconnectRect = new Rect(expandedNextX - utilityButtonWidth, expandedButtonRect.y,
                        utilityButtonWidth, buttonHeight);
                    expandedNextX = expandedDisconnectRect.x - ActionButtonSpacing;
                }

                Rect expandedDiscardRect = default;
                if (showDiscard)
                {
                    expandedDiscardRect = new Rect(expandedNextX - discardButtonWidth, expandedButtonRect.y,
                        discardButtonWidth, buttonHeight);
                }

                using (new EditorGUI.DisabledScope(sourceProp == null))
                {
                    if (showDiscard && GUI.Button(expandedDiscardRect, "Discard Changes"))
                    {
                        if (InlineScriptEditorHelper.TryRevertToCompiledSource(propertyKey, sourceProp, additionalUsingsProp,
                                cachedDefinitionsProp, compiledScriptProp))
                        {
                            property.serializedObject.ApplyModifiedProperties();
                            property.serializedObject.Update();
                            validation = InlineScriptEditorHelper.ValidateSource(sourceProp?.stringValue ?? string.Empty);
                            compileStatus = InlineScriptEditorHelper.DetermineCompileStatus(propertyKey, sourceProp,
                                compiledScriptProp, additionalUsingsProp, cachedDefinitionsProp);
                            buttonLabel = compiledScriptProp?.objectReferenceValue != null ? "Compile Update" : "Compile";
                        }
                    }

                    if (showDisconnect && GUI.Button(expandedDisconnectRect, DisconnectButtonContent))
                    {
                        if (ConfirmDisconnect())
                        {
                            var previousKey = propertyKey;
                            var shouldDeleteScript = false;
                            
                            // Show second dialog if there's a compiled script to potentially delete
                            if (compiledScriptProp?.objectReferenceValue != null)
                            {
                                shouldDeleteScript = ConfirmDeleteScript();
                            }

                            if (sourceProp != null)
                            {
                                sourceProp.stringValue = string.Empty;
                            }

                            if (additionalUsingsProp != null)
                            {
                                additionalUsingsProp.stringValue = string.Empty;
                            }

                            if (cachedDefinitionsProp != null)
                            {
                                cachedDefinitionsProp.stringValue = string.Empty;
                            }

                            if (compiledScriptProp != null)
                            {
                                // Delete the source script if confirmed
                                if (shouldDeleteScript)
                                {
                                    DeleteSourceScript(compiledScriptProp);
                                }
                                compiledScriptProp.objectReferenceValue = null;
                            }

                            if (guidProp != null)
                            {
                                guidProp.stringValue = string.Empty;
                            }

                            InlineScriptEditorHelper.ClearCachedSource(previousKey);
                            ClearTransientStatus(previousKey);

                            property.serializedObject.ApplyModifiedProperties();
                            property.serializedObject.Update();

                            propertyKey = InlineScriptEditorHelper.BuildPropertyKey(property);
                            additionalKey = BuildAdditionalKey(propertyKey);
                            definitionKey = BuildDefinitionKey(propertyKey);
                            compileStatus = InlineScriptEditorHelper.DetermineCompileStatus(propertyKey, sourceProp,
                                compiledScriptProp, additionalUsingsProp, cachedDefinitionsProp);
                            buttonLabel = compiledScriptProp?.objectReferenceValue != null ? "Compile Update" : "Compile";

                            var statusMessage = shouldDeleteScript ? "Inline script disconnected and source deleted" : "Inline script disconnected";
                            SetTransientStatus(propertyKey, statusMessage, TransientDisconnectColor);
                            showCopy = InlineScriptEditorHelper.HasCopyableContent(sourceProp, additionalUsingsProp,
                                cachedDefinitionsProp, compiledScriptProp, guidProp);
                            showDisconnect = HasConnection(compiledScriptProp, guidProp);
                        }
                    }

                    if (showCopy && GUI.Button(expandedCopyRect, CopyButtonContent))
                    {
                        if (ConfirmCopy())
                        {
                            InlineScriptEditorHelper.CopyToClipboard(sourceProp, additionalUsingsProp, cachedDefinitionsProp,
                                compiledScriptProp, guidProp);
                        }
                    }
                }

                var expandedHasClipboard = InlineScriptEditorHelper.HasClipboardData();
                using (new EditorGUI.DisabledScope(sourceProp == null || !expandedHasClipboard))
                {
                    if (GUI.Button(expandedPasteRect, PasteButtonContent))
                    {
                        if (ConfirmPaste() && InlineScriptEditorHelper.TryPasteFromClipboard(sourceProp, additionalUsingsProp,
                                cachedDefinitionsProp, compiledScriptProp, guidProp))
                        {
                            var previousKey = propertyKey;
                            property.serializedObject.ApplyModifiedProperties();
                            property.serializedObject.Update();
                            propertyKey = InlineScriptEditorHelper.BuildPropertyKey(property);
                            additionalKey = BuildAdditionalKey(propertyKey);
                            definitionKey = BuildDefinitionKey(propertyKey);
                            InlineScriptEditorHelper.UpdateCachedSource(propertyKey, sourceProp?.stringValue,
                                additionalUsingsProp?.stringValue, cachedDefinitionsProp?.stringValue);
                            validation = InlineScriptEditorHelper.ValidateSource(sourceProp?.stringValue ?? string.Empty);
                            compileStatus = InlineScriptEditorHelper.DetermineCompileStatus(propertyKey, sourceProp,
                                compiledScriptProp, additionalUsingsProp, cachedDefinitionsProp);
                            buttonLabel = compiledScriptProp?.objectReferenceValue != null ? "Compile Update" : "Compile";
                            ClearTransientStatus(previousKey);
                            SetTransientStatus(propertyKey, "Pasted inline script", TransientPasteColor);
                            showCopy = InlineScriptEditorHelper.HasCopyableContent(sourceProp, additionalUsingsProp,
                                cachedDefinitionsProp, compiledScriptProp, guidProp);
                            showDisconnect = HasConnection(compiledScriptProp, guidProp);
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(sourceProp == null))
                {
                    if (GUI.Button(expandedButtonRect, buttonLabel))
                    {
                        InlineScriptEditorHelper.CompileInlineSnippet(property.serializedObject, sourceProp, compiledScriptProp,
                            guidProp, additionalUsingsProp, cachedDefinitionsProp, profile);
                        if (sourceProp != null)
                        {
                            InlineScriptEditorHelper.UpdateCachedSource(propertyKey, sourceProp.stringValue,
                                additionalUsingsProp?.stringValue, cachedDefinitionsProp?.stringValue);
                        }
                        compileStatus = InlineScriptEditorHelper.DetermineCompileStatus(propertyKey, sourceProp,
                            compiledScriptProp, additionalUsingsProp, cachedDefinitionsProp);
                        buttonLabel = compiledScriptProp?.objectReferenceValue != null ? "Compile Update" : "Compile";
                    }
                }

                contentY = expandedButtonRect.yMax + (CompileButtonVerticalPadding * 0.5f);

                foreach (var result in validation)
                {
                    var helpRect = new Rect(position.x,
                        contentY,
                        position.width,
                        EditorStyles.helpBox.CalcHeight(new GUIContent(result.message), position.width));
                    EditorGUI.HelpBox(helpRect, result.message, result.messageType);
                    contentY = helpRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            EditorGUI.EndProperty();
        }


        private static void EnsureStyles()
        {
            if (_codeStyle != null)
            {
                return;
            }

            if (_transparentTexture == null)
            {
                _transparentTexture = new Texture2D(1, 1)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Repeat
                };
                _transparentTexture.SetPixel(0, 0, Color.clear);
                _transparentTexture.Apply();
            }

            _codeStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = false,
                font = EditorStyles.standardFont ?? EditorStyles.textArea.font,
                padding = new RectOffset(8, 8, 6, 6),
                clipping = TextClipping.Clip
            };
            _codeStyle.normal.background = _transparentTexture;
            _codeStyle.focused.background = _transparentTexture;
            _codeStyle.hover.background = _transparentTexture;
            _codeStyle.active.background = _transparentTexture;
            var ghostColor = Color.clear;
            _codeStyle.normal.textColor = ghostColor;
            _codeStyle.focused.textColor = ghostColor;
            _codeStyle.hover.textColor = ghostColor;
            _codeStyle.active.textColor = ghostColor;

            _supplementaryTextStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = false,
                font = EditorStyles.standardFont ?? EditorStyles.textArea.font,
                padding = new RectOffset(6, 6, 4, 4)
            };

            _highlightStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                font = _codeStyle.font,
                fontSize = _codeStyle.fontSize,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
                padding = _codeStyle.padding,
                clipping = TextClipping.Clip
            };
            _highlightStyle.normal.textColor = new Color(0.86f, 0.92f, 1f, 1f);

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };
            _headerStyle.normal.textColor = new Color(0.88f, 0.92f, 1f, 1f);

            _statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = EditorStyles.miniLabel.fontSize + 1
            };
            _statusStyle.normal.textColor = new Color(0.74f, 0.84f, 1f, 1f);

            _braceStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = EditorStyles.boldLabel.fontSize + 6,
                alignment = TextAnchor.UpperLeft
            };
            _braceStyle.normal.textColor = new Color(0.6f, 0.78f, 1f, 1f);

            _metricsStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperRight,
                fontSize = EditorStyles.miniLabel.fontSize + 1
            };
            _metricsStyle.normal.textColor = new Color(0.76f, 0.86f, 0.97f, 0.95f);
        }

        private static float CalculateCodeAreaHeight(string source)
        {
            var viewHeight = CalculateCodeViewHeight(source);
            return viewHeight + CodeContentPadding.vertical;
        }

        private static float CalculateCodeContentHeight(string source)
        {
            var width = Mathf.Max(0f, GetContentWidth() - CodeContentPadding.horizontal - CodeScrollbarWidth);
            if (width <= 0f)
            {
                return LineHeight * MinimumCodeLines;
            }

            var content = new GUIContent(source ?? string.Empty);
            var height = _codeStyle.CalcHeight(content, width);
            return Mathf.Max(height, LineHeight * MinimumCodeLines);
        }

        private static float CalculateCodeViewHeight(string source)
        {
            var contentHeight = CalculateCodeContentHeight(source);
            var maxHeight = LineHeight * MaximumVisibleCodeLines;
            return Mathf.Min(contentHeight, maxHeight);
        }

        private static float CalculateTextAreaHeight(string source, float minimumLines)
        {
            if (string.IsNullOrEmpty(source))
            {
                return LineHeight * minimumLines;
            }

            var contentWidth = GetContentWidth();
            var content = new GUIContent(source);
            var height = _supplementaryTextStyle.CalcHeight(content, contentWidth);
            return Mathf.Max(height, LineHeight * minimumLines);
        }

        private static float CalculateHelpBoxHeight(GUIContent content)
        {
            var contentWidth = GetContentWidth();
            return EditorStyles.helpBox.CalcHeight(content, contentWidth);
        }

        private static Rect BuildContentRect(Rect position, float y, float height)
        {
            var rect = new Rect(position.x, y, position.width, height);
            return EditorGUI.IndentedRect(rect);
        }

        private static float GetContentWidth()
        {
            var indent = EditorGUI.indentLevel * 15f;
            return Mathf.Max(0f, EditorGUIUtility.currentViewWidth - 40f - indent);
        }

        private static Vector2 GetCodeScrollPosition(string propertyKey)
        {
            if (string.IsNullOrEmpty(propertyKey))
            {
                return Vector2.zero;
            }

            if (CodeScrollPositions.TryGetValue(propertyKey, out var value))
            {
                return value;
            }

            CodeScrollPositions[propertyKey] = Vector2.zero;
            return Vector2.zero;
        }

        private static void SetCodeScrollPosition(string propertyKey, Vector2 position)
        {
            if (string.IsNullOrEmpty(propertyKey))
            {
                return;
            }

            CodeScrollPositions[propertyKey] = position;
        }

        private static bool HasConnection(SerializedProperty compiledScriptProp, SerializedProperty guidProp)
        {
            if (compiledScriptProp != null && compiledScriptProp.objectReferenceValue != null)
            {
                return true;
            }

            if (guidProp != null && !string.IsNullOrEmpty(guidProp.stringValue))
            {
                return true;
            }

            return false;
        }

        private static bool ConfirmDisconnect()
        {
            return EditorUtility.DisplayDialog(DisconnectConfirmTitle, DisconnectConfirmMessage,
                DisconnectConfirmOk, DisconnectConfirmCancel);
        }

        private static bool ConfirmCopy()
        {
            return EditorUtility.DisplayDialog(CopyConfirmTitle, CopyConfirmMessage, CopyConfirmOk, CopyConfirmCancel);
        }

        private static bool ConfirmPaste()
        {
            return EditorUtility.DisplayDialog(PasteConfirmTitle, PasteConfirmMessage, PasteConfirmOk, PasteConfirmCancel);
        }

        private static bool ConfirmDeleteScript()
        {
            return EditorUtility.DisplayDialog(DeleteScriptConfirmTitle, DeleteScriptConfirmMessage, DeleteScriptConfirmYes, DeleteScriptConfirmNo);
        }

        private static void DeleteSourceScript(SerializedProperty compiledScriptProp)
        {
            if (compiledScriptProp?.objectReferenceValue == null)
            {
                return;
            }

            var scriptPath = AssetDatabase.GetAssetPath(compiledScriptProp.objectReferenceValue);
            if (!string.IsNullOrEmpty(scriptPath))
            {
                AssetDatabase.DeleteAsset(scriptPath);
                AssetDatabase.Refresh();
            }
        }

        private static void SetTransientStatus(string propertyKey, string message, Color color,
            double durationSeconds = TransientStatusDuration)
        {
            if (string.IsNullOrEmpty(propertyKey) || string.IsNullOrEmpty(message))
            {
                return;
            }

            var duration = Math.Max(0.25d, durationSeconds);
            TransientStatusMessages[propertyKey] = new TransientStatusMessage
            {
                Text = message,
                Color = color,
                ExpireTime = EditorApplication.timeSinceStartup + duration
            };
        }

        private static bool TryGetTransientStatus(string propertyKey, out string message, out Color color)
        {
            if (!string.IsNullOrEmpty(propertyKey) &&
                TransientStatusMessages.TryGetValue(propertyKey, out var status))
            {
                if (EditorApplication.timeSinceStartup <= status.ExpireTime)
                {
                    message = status.Text;
                    color = status.Color;
                    return true;
                }

                TransientStatusMessages.Remove(propertyKey);
            }

            message = null;
            color = default;
            return false;
        }

        private static void ClearTransientStatus(string propertyKey)
        {
            if (string.IsNullOrEmpty(propertyKey))
            {
                return;
            }

            TransientStatusMessages.Remove(propertyKey);
        }

        private static int CalculateLineCount(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return 1;
            }

            var lines = 1;
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] == '\n')
                {
                    lines++;
                }
            }

            return lines;
        }

        private static float CalculateLongestLineWidth(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return 0f;
            }

            var longest = 0f;
            var content = new GUIContent();
            var length = source.Length;
            var lineStart = 0;

            for (var i = 0; i <= length; i++)
            {
                if (i == length || source[i] == '\n')
                {
                    var segmentLength = i - lineStart;
                    if (segmentLength > 0 && source[i - 1] == '\r')
                    {
                        segmentLength--;
                    }

                    if (segmentLength > 0)
                    {
                        content.text = source.Substring(lineStart, segmentLength);
                    }
                    else
                    {
                        content.text = " ";
                    }

                    var size = _highlightStyle.CalcSize(content);
                    if (size.x > longest)
                    {
                        longest = size.x;
                    }

                    lineStart = i + 1;
                }
            }

            return longest;
        }

        private static void DrawHeader(Rect rect, SerializedProperty property, GUIContent label,
            InlineScriptEditorHelper.CompileStatus status, float collapsedRightPadding, string propertyKey)
        {
            var sourceProp = property.FindPropertyRelative("_sourceCode");
            var sourceCode = sourceProp?.stringValue ?? string.Empty;
            var additionalProp = property.FindPropertyRelative("_additionalUsings");
            var additionalText = additionalProp?.stringValue ?? string.Empty;
            var definitionProp = property.FindPropertyRelative("_cachedDefinitions");
            var cachedText = definitionProp?.stringValue ?? string.Empty;
            
            // Only draw rects during Repaint to prevent flickering
            if (Event.current.type == EventType.Repaint)
            {
                var headerColor = new Color(0.13f, 0.18f, 0.28f, 0.65f);
                EditorGUI.DrawRect(rect, headerColor);
                var borderColor = new Color(0.08f, 0.1f, 0.16f, 1f);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), borderColor);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), borderColor);
            }

            var foldoutRect = rect;
            var previousColor = GUI.color;
            GUI.color = Color.white;
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);
            GUI.color = previousColor;

            var rightPadding = property.isExpanded ? 0f : collapsedRightPadding;

            var labelWidth = Mathf.Max(0f, rect.width - (18f + rightPadding));
            var labelRect = new Rect(rect.x + 18f, rect.y, labelWidth, rect.height);
            EditorGUI.LabelField(labelRect, label, _headerStyle);

            // Draw status text
            var statusWidth = Mathf.Max(0f, rect.width - (6f + rightPadding));
            var statusRect = new Rect(rect.x, rect.y, statusWidth, rect.height);
            var statusText = GetStatusText(status);
            var statusColor = GetStatusColor(status);
            if (TryGetTransientStatus(propertyKey, out var transientText, out var transientColor))
            {
                statusText = transientText;
                statusColor = transientColor;
            }

            var statusColorScope = GUI.color;
            GUI.color = statusColor;
            GUI.Label(statusRect, statusText, _statusStyle);
            GUI.color = statusColorScope;

            // Draw text indicator when collapsed and has text but not compiled
            if (!property.isExpanded)
            {
                var textIndicatorText = GetTextIndicatorText(sourceCode, additionalText, cachedText, status);
                if (!string.IsNullOrEmpty(textIndicatorText))
                {
                    var textIndicatorColor = GetTextIndicatorColor(sourceCode, additionalText, cachedText, status);
                    var textIndicatorStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = EditorStyles.miniLabel.fontSize
                    };
                    
                    var textIndicatorRect = new Rect(
                        rect.x + rect.width * 0.5f - 30f, 
                        rect.y, 
                        60f, 
                        rect.height);
                    
                    var textIndicatorColorScope = GUI.color;
                    GUI.color = textIndicatorColor;
                    GUI.Label(textIndicatorRect, textIndicatorText, textIndicatorStyle);
                    GUI.color = textIndicatorColorScope;
                }
            }
        }

        private static void DrawCodeBackground(Rect rect)
        {
            // Only draw rects during Repaint to prevent flickering
            if (Event.current.type == EventType.Repaint)
            {
                var backgroundColor = new Color(0.12f, 0.15f, 0.2f, 0.9f);
                var borderColor = new Color(0.18f, 0.22f, 0.3f, 0.8f);
                
                // Draw background fill
                EditorGUI.DrawRect(rect, backgroundColor);
                
                // Draw borders
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), borderColor);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), borderColor);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), borderColor);
                EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), borderColor);
            }
        }

        private static string BuildAdditionalKey(string propertyKey)
        {
            return $"{propertyKey}:additional";
        }

        private static string BuildDefinitionKey(string propertyKey)
        {
            return $"{propertyKey}:definitions";
        }

        private static bool GetSectionExpanded(Dictionary<string, bool> dictionary, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (dictionary.TryGetValue(key, out var expanded))
            {
                return expanded;
            }

            dictionary[key] = false;
            return false;
        }

        private static void SetSectionExpanded(Dictionary<string, bool> dictionary, string key, bool expanded)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            dictionary[key] = expanded;
        }

        private static string GetStatusText(InlineScriptEditorHelper.CompileStatus status)
        {
            switch (status)
            {
                case InlineScriptEditorHelper.CompileStatus.Compiled:
                    return "Compiled";
                case InlineScriptEditorHelper.CompileStatus.Dirty:
                    return "Needs Update";
                case InlineScriptEditorHelper.CompileStatus.Pending:
                    return "Not Compiled";
                default:
                    return string.Empty;
            }
        }

        private static Color GetStatusColor(InlineScriptEditorHelper.CompileStatus status)
        {
            switch (status)
            {
                case InlineScriptEditorHelper.CompileStatus.Compiled:
                    return new Color(0.35f, 0.78f, 0.45f, 1f);
                case InlineScriptEditorHelper.CompileStatus.Dirty:
                    return new Color(0.95f, 0.65f, 0.25f, 1f);
                case InlineScriptEditorHelper.CompileStatus.Pending:
                    return new Color(0.88f, 0.34f, 0.34f, 1f);
                default:
                    return GUI.color;
            }
        }

        private static bool HasTextWritten(params string[] sections)
        {
            if (sections == null)
            {
                return false;
            }

            foreach (var section in sections)
            {
                if (!string.IsNullOrWhiteSpace(section))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetTextIndicatorText(string sourceCode, string additionalUsings, string cachedDefinitions,
            InlineScriptEditorHelper.CompileStatus status)
        {
            if (HasTextWritten(sourceCode, additionalUsings, cachedDefinitions) &&
                status == InlineScriptEditorHelper.CompileStatus.Pending)
            {
                return "Has Text";
            }
            return string.Empty;
        }

        private static Color GetTextIndicatorColor(string sourceCode, string additionalUsings, string cachedDefinitions,
            InlineScriptEditorHelper.CompileStatus status)
        {
            if (HasTextWritten(sourceCode, additionalUsings, cachedDefinitions) &&
                status == InlineScriptEditorHelper.CompileStatus.Pending)
            {
                return new Color(0.7f, 0.7f, 0.7f, 1f); // Light gray for text indicator
            }
            return Color.clear;
        }

    }
}
#endif
