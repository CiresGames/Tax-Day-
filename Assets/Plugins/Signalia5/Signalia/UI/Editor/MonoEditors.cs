using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.Utilities;
using DG.DOTweenEditor;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace AHAKuo.Signalia.UI.Editors
{
    [CustomEditor(typeof(UIView)), CanEditMultipleObjects]
    public class UIViewEditor : Editor
    {
        private SerializedProperty startingStatus, backButtonHides, majorMenu, menuToBackTo, hideAllOtherMenusOnShow;
        private SerializedProperty disableGameObject, disableGraphicRaycaster, firstSelectedOnShow, deselectAllOnHide, reselectPreviousMenuOnHide;
        private SerializedProperty useFaintBackground, faintBackgroundHideOnTap, faintBackgroundColor;
        private SerializedProperty showAnimation, hideAnimation, playOnlyWhenChangingStatus, cancelOpposites;
        private SerializedProperty menuName;
        private SerializedProperty enableInputHandling, inputActionName, inputBehavior, useCooldown, cooldownDuration;
        private SerializedProperty showStartEvents, hideStartEvents, showEndEvents, hideEndEvents, hideByBackwardEvents;
        private SerializedProperty showUnityEvent, hideUnityEvent, showEndUnityEvent, hideEndUnityEvent;
        private SerializedProperty showStartAudio, showEndAudio, hideStartAudio, hideEndAudio;
        private SerializedProperty showStartHaptics, showEndHaptics, hideStartHaptics, hideEndHaptics;
        private SerializedProperty cascades, childrenMenus;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Main", "Animation", "Events", "Audio", "Haptics", "Cascades" };

        private ReorderableList showStart, showEnd, hideStart, hideEnd, hideByBackward, childrenMenusList;

        private void OnEnable()
        {
            startingStatus = serializedObject.FindProperty("startingStatus");
            backButtonHides = serializedObject.FindProperty("backButtonHides");
            majorMenu = serializedObject.FindProperty("majorMenu");
            menuToBackTo = serializedObject.FindProperty("menuToBackTo");
            hideAllOtherMenusOnShow = serializedObject.FindProperty("hideAllOtherMenusOnShow");

            disableGameObject = serializedObject.FindProperty("disableGameObject");
            disableGraphicRaycaster = serializedObject.FindProperty("disableGraphicRaycaster");
            firstSelectedOnShow = serializedObject.FindProperty("firstSelectedOnShow");
            deselectAllOnHide = serializedObject.FindProperty("deselectAllOnHide");
            reselectPreviousMenuOnHide = serializedObject.FindProperty("reselectPreviousMenuOnHide");

            useFaintBackground = serializedObject.FindProperty("useFaintBackground");
            faintBackgroundHideOnTap = serializedObject.FindProperty("faintBackgroundHideOnTap");
            faintBackgroundColor = serializedObject.FindProperty("faintBackgroundColor");

            showAnimation = serializedObject.FindProperty("showAnimation");
            hideAnimation = serializedObject.FindProperty("hideAnimation");
            playOnlyWhenChangingStatus = serializedObject.FindProperty("playOnlyWhenChangingStatus");
            cancelOpposites = serializedObject.FindProperty("cancelOpposites");

            menuName = serializedObject.FindProperty("menuName");

            enableInputHandling = serializedObject.FindProperty("enableInputHandling");
            inputActionName = serializedObject.FindProperty("inputActionName");
            inputBehavior = serializedObject.FindProperty("inputBehavior");
            useCooldown = serializedObject.FindProperty("useCooldown");
            cooldownDuration = serializedObject.FindProperty("cooldownDuration");

            showStartEvents = serializedObject.FindProperty("showStartEvents");
            hideStartEvents = serializedObject.FindProperty("hideStartEvents");
            showEndEvents = serializedObject.FindProperty("showEndEvents");
            hideEndEvents = serializedObject.FindProperty("hideEndEvents");
            hideByBackwardEvents = serializedObject.FindProperty("hideByBackwardEvents");

            showUnityEvent = serializedObject.FindProperty("showUnityEvent");
            hideUnityEvent = serializedObject.FindProperty("hideUnityEvent");
            showEndUnityEvent = serializedObject.FindProperty("showEndUnityEvent");
            hideEndUnityEvent = serializedObject.FindProperty("hideEndUnityEvent");

            showStartAudio = serializedObject.FindProperty("showStartAudio");
            showEndAudio = serializedObject.FindProperty("showEndAudio");
            hideStartAudio = serializedObject.FindProperty("hideStartAudio");
            hideEndAudio = serializedObject.FindProperty("hideEndAudio");
            
            showStartHaptics = serializedObject.FindProperty("showStartHaptics");
            showEndHaptics = serializedObject.FindProperty("showEndHaptics");
            hideStartHaptics = serializedObject.FindProperty("hideStartHaptics");
            hideEndHaptics = serializedObject.FindProperty("hideEndHaptics");

            cascades = serializedObject.FindProperty("cascades");
            childrenMenus = serializedObject.FindProperty("childrenMenus");

            SetupReorderableLists();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical();

            // Title
            GUIStyle titleStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = SSColors.HotPink },
                alignment = TextAnchor.MiddleCenter
            };

            // **Play Mode Controls**
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("🎮 Runtime Controls", titleStyle);

                UIView view = (UIView)target;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("▶ Show UI", GUILayout.Height(25)))
                {
                    view.Show();
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("⛔ Hide UI", GUILayout.Height(25)))
                {
                    view.Hide();
                }
                GUI.backgroundColor = Color.white;
            }

            // **EDITOR MODE CONTROLS**
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("🛠️ Editor Controls", titleStyle);

                UIView view = (UIView)target;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("▶ Test Show UI", GUILayout.Height(25)))
                {
                    EditorPreviewInjector.Start(view.ShowAnimation, view.gameObject);
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("⛔ Test Hide UI", GUILayout.Height(25)))
                {
                    EditorPreviewInjector.Start(view.HideAnimation, view.gameObject);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.HelpBox("Customize how this UI View behaves, animates, and interacts with menus.", MessageType.Info);
            GUILayout.Space(5);

            // **Tab Selection**
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);

            // **Tab Grouping Logic**
            switch (selectedTab)
            {
                case 0: DrawMainSettings(); break;
                case 1: DrawAnimationSettings(); break;
                case 2: DrawEventSettings(); break;
                case 3: DrawAudioSettings(); break;
                case 4: DrawHapticsSettings(); break;
                case 5: DrawCascadeSettings(); break;
            }

            GUILayout.Space(10);

            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        // **Main Settings**
        private void DrawMainSettings()
        {
            // helpbox to explain the purpose of filling out the menu name
            EditorGUILayout.HelpBox("The Menu Name is used to identify this menu and invoke it by name. Should be unique.", MessageType.Info);

            if (menuName.stringValue.IsNullOrEmpty())
            {
                // a button to generate a unique menu name
                if (GUILayout.Button("Generate Unique Menu Name"))
                {
                    foreach (var obje in targets)
                    {
                        if (obje is UIView target)
                        {
                            SerializedObject serializedTarget = new SerializedObject(target);
                            SerializedProperty menuNameProp = serializedTarget.FindProperty("menuName");
                            menuNameProp.stringValue = target.GenerateMenuName();
                            serializedTarget.ApplyModifiedProperties();
                        }
                    }
                }
            }

            GUILayout.Space(10);
            EditorGUILayout.PropertyField(menuName, new GUIContent("Menu Name"));
            // space
            GUILayout.Space(5);
            EditorGUILayout.PropertyField(startingStatus, new GUIContent("Starting Status"));

            // if back button hides, and menuName is not defined, show a warning
            // if (backButtonHides.boolValue && menuName.stringValue.IsNullOrEmpty())
            // {
            //     EditorGUILayout.HelpBox("Menu Name is not defined. Please define a unique menu name to use the back button.", MessageType.Warning);
            // }
            EditorGUILayout.PropertyField(backButtonHides, new GUIContent("Back Button Hides"));

            if (backButtonHides.boolValue)
            {
                EditorGUILayout.HelpBox("[OPTIONAL] The Menu to Back To is the menu that will be shown when the back button is pressed.", MessageType.Info);
                EditorHelpers.DrawMenuDropdownForProperty("Menu to Back To", menuToBackTo, serializedObject);
            }

            // info box to explain the purpose of the major menu
            EditorGUILayout.HelpBox("A major menu will hide when a menu that hides other menus is shown.", MessageType.Info);
            EditorGUILayout.PropertyField(majorMenu, new GUIContent("Major Menu"));
            // space
            GUILayout.Space(5);

            //helpbox to explain the purpose of the children menus
            EditorGUILayout.HelpBox("Children Menus are the menus that are hidden when this menu is hidden.", MessageType.Info);
            EditorHelpers.DrawMenuDropdown("Quick Add Children Menus", childrenMenusList, serializedObject);
            childrenMenusList.DoLayoutList();

            GUILayout.Space(5);

            // infobox explaining the purpose of the next settings
            EditorGUILayout.HelpBox("These settings are used to control the behavior of this menu when shown, hidden, or while hidden.", MessageType.Info);

            // begin vertical with helpbox style
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // **When Shown**
            EditorGUILayout.LabelField("When Shown", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(firstSelectedOnShow, new GUIContent("First Selected on Show"));
            // helpbox to tell user that this will hide other menus whose major menu option is checked
            EditorGUILayout.HelpBox("This will hide other menus whose Major Menu option is checked.", MessageType.Info);
            EditorGUILayout.PropertyField(hideAllOtherMenusOnShow, new GUIContent("Hide Other Menus on Show"));

            // **While Hidden**
            EditorGUILayout.LabelField("While Hidden", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(disableGameObject, new GUIContent("Disable GameObject"));

            GameObject obj = (serializedObject.targetObject as Component)?.gameObject;
            if (obj != null && disableGraphicRaycaster.boolValue && obj.GetComponent<GraphicRaycaster>() == null)
            {
                EditorGUILayout.HelpBox("This view does not have a Graphic Raycaster component. This option will do nothing. Add it?", MessageType.Warning);

                if (GUILayout.Button("Add Graphic Raycaster"))
                {
                    Undo.RegisterCompleteObjectUndo(obj, "Add Graphic Raycaster"); // Register for Undo
                    GraphicRaycaster raycaster = Undo.AddComponent<GraphicRaycaster>(obj); // Proper undoable addition
                    EditorUtility.SetDirty(obj); // Mark as modified

                    EditorUtility.DisplayDialog("Graphic Raycaster Added", "A Graphic Raycaster component has been added to this view.", "OK");
                }
            }

            EditorGUILayout.PropertyField(disableGraphicRaycaster, new GUIContent("Disable Graphic Raycaster"));

            // **When Hidden**
            EditorGUILayout.LabelField("When Hidden", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(deselectAllOnHide, new GUIContent("Deselect All on Hide"));
            EditorGUILayout.PropertyField(reselectPreviousMenuOnHide, new GUIContent("Reselect Previous Menu on Hide", "When this view hides, automatically reselect the firstSelectedOnShow of the previous menu in travel history."));
            EditorGUILayout.EndVertical();

            // **Faint Background**
            EditorGUILayout.LabelField("Faint Background", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("The faint background is a background that appears when this view is shown. It can be used to dim the background and focus on the menu.", MessageType.Info);
            EditorGUILayout.PropertyField(useFaintBackground, new GUIContent("Use Faint Background"));

            if (useFaintBackground.boolValue)
            {
                EditorGUILayout.PropertyField(faintBackgroundColor, new GUIContent("Faint Background Color"));
                // helpbox
                EditorGUILayout.HelpBox("You can also make the faint background hide this view when tapped..", MessageType.Info);
                EditorGUILayout.PropertyField(faintBackgroundHideOnTap, new GUIContent("Hide on Tap"));
            }

            // **Input Handling**
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Input Handling", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure an input action to show, hide, or toggle this view when pressed. Uses Signalia's input system.", MessageType.Info);
            EditorGUILayout.PropertyField(enableInputHandling, new GUIContent("Enable Input Handling"));

            if (enableInputHandling.boolValue)
            {
                EditorGUILayout.PropertyField(inputActionName, new GUIContent("Input Action Name", "The name of the input action to listen for (e.g., 'Menu', 'Inventory', 'Pause'). Must match an action defined in your Signalia config."));
                EditorGUILayout.PropertyField(inputBehavior, new GUIContent("Input Behavior", "What happens when the input action is pressed: Show (always shows), Hide (always hides), or Toggle (switches between show/hide)."));
                
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Input Cooldown: When enabled, prevents the input from being processed too frequently. Useful for preventing spam or accidental double-taps.", MessageType.Info);
                EditorGUILayout.PropertyField(useCooldown, new GUIContent("Use Cooldown", "Enable cooldown to prevent rapid repeated input."));

                if (useCooldown.boolValue)
                {
                    EditorGUILayout.PropertyField(cooldownDuration, new GUIContent("Cooldown Duration", "Time in seconds before the input can be processed again."));
                    if (cooldownDuration.floatValue < 0f)
                    {
                        EditorGUILayout.HelpBox("Cooldown duration cannot be negative.", MessageType.Warning);
                    }
                }

                if (string.IsNullOrWhiteSpace(inputActionName.stringValue))
                {
                    EditorGUILayout.HelpBox("Input Action Name is empty. Please specify an input action name from your Signalia config.", MessageType.Warning);
                }
            }
        }

        // **Animation Settings**
        private void DrawAnimationSettings()
        {
            // if either are null, tell the user that they are required in views
            if (showAnimation.objectReferenceValue == null || hideAnimation.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Show and Hide animations are required for this view to work.", MessageType.Error);
            }

            EditorGUILayout.PropertyField(showAnimation, new GUIContent("Show Animation"));
            EditorGUILayout.PropertyField(hideAnimation, new GUIContent("Hide Animation"));
            EditorGUILayout.PropertyField(playOnlyWhenChangingStatus, new GUIContent("Play Only When Changing Status", "Prevents re-iterating animations if the view is already in the target state. If enabled, calling Show() on an already-shown view will do nothing."));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(cancelOpposites, new GUIContent("Cancel Opposites", "When enabled, forcefully cancels the opposite animation when switching states. For example, if hiding is requested while showing, the show animation will be cancelled and hide will begin immediately. This allows smooth transitions when rapidly switching between show/hide states."));
        }

        // **Event Settings**
        private void DrawEventSettings()
        {
            // begin vertical with helpbox style
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showStart.DoLayoutList();
            hideStart.DoLayoutList();
            showEnd.DoLayoutList();
            hideEnd.DoLayoutList();
            // help box to explain the purpose of the hide by backward events
            EditorGUILayout.HelpBox("Hide By Backward Events are events that are sent when this view is hidden using back button.", MessageType.Info);
            hideByBackward.DoLayoutList();
            EditorGUILayout.EndVertical();

            // horizontal line divider
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(5);

            // begin vertical with helpbox style
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(showUnityEvent, new GUIContent("Show Unity Event"));
            EditorGUILayout.PropertyField(hideUnityEvent, new GUIContent("Hide Unity Event"));
            EditorGUILayout.PropertyField(showEndUnityEvent, new GUIContent("Show End Unity Event"));
            EditorGUILayout.PropertyField(hideEndUnityEvent, new GUIContent("Hide End Unity Event"));
            EditorGUILayout.EndVertical();
        }

        // **Cascade Settings**
        private void DrawCascadeSettings()
        {
            // button to get all animatatables in immediate children
            if (GUILayout.Button("Add Cascades from Children"))
            {
                var view = (UIView)target;
                view.CreateCascadesFromChildren();
            }

            // button to get all animatatables in immediate children, but as domino effect
            if (GUILayout.Button("Add Domino Cascades from Children"))
            {
                var view = (UIView)target;
                view.CreateCascadesWithShowDominoEffect(true, true);
            }

            // infobox explaining that first index of animatable should be the what's called when showing the view, and the second index should be what's called when hiding the view.
            EditorGUILayout.HelpBox("Cascades are used to play animations on other UI elements when this view is shown or hidden. If referencing an animatable target, the first index animation on the animatable is played when this view is shown, and the second index animation is played when this view is hidden.", MessageType.Info);
            EditorGUILayout.PropertyField(cascades, new GUIContent("Cascades"), true);
        }

        // **Audio Settings**
        private void DrawAudioSettings()
        {
            EditorHelpers.DrawAudioDropdown("Show Start Audio", showStartAudio, serializedObject);
            EditorHelpers.DrawAudioDropdown("Show End Audio", showEndAudio, serializedObject);
            EditorHelpers.DrawAudioDropdown("Hide Start Audio", hideStartAudio, serializedObject);
            EditorHelpers.DrawAudioDropdown("Hide End Audio", hideEndAudio, serializedObject);
        }

        // **Haptics Settings**
        private void DrawHapticsSettings()
        {
            EditorGUILayout.LabelField("⚡ Haptic Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Haptic feedback that plays alongside audio events.", MessageType.Info);
            
            EditorGUILayout.PropertyField(showStartHaptics, new GUIContent("Show Start Haptics"), true);
            EditorGUILayout.PropertyField(showEndHaptics, new GUIContent("Show End Haptics"), true);
            EditorGUILayout.PropertyField(hideStartHaptics, new GUIContent("Hide Start Haptics"), true);
            EditorGUILayout.PropertyField(hideEndHaptics, new GUIContent("Hide End Haptics"), true);
        }

        private void SetupReorderableLists()
        {
            childrenMenusList = new ReorderableList(serializedObject, childrenMenus, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Children Menus", EditorStyles.boldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty item = childrenMenus.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, item, GUIContent.none);
                }
            };

            showStart = new ReorderableList(serializedObject, showStartEvents, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Show Start Events", EditorStyles.boldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty item = showStartEvents.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, item, GUIContent.none);
                }
            };

            hideStart = new ReorderableList(serializedObject, hideStartEvents, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Hide Start Events", EditorStyles.boldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty item = hideStartEvents.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, item, GUIContent.none);
                }
            };

            showEnd = new ReorderableList(serializedObject, showEndEvents, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Show End Events", EditorStyles.boldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty item = showEndEvents.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, item, GUIContent.none);
                }
            };

            hideEnd = new ReorderableList(serializedObject, hideEndEvents, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Hide End Events", EditorStyles.boldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty item = hideEndEvents.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, item, GUIContent.none);
                }
            };

            hideByBackward = new ReorderableList(serializedObject, hideByBackwardEvents, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Hide By Backward Events", EditorStyles.boldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty item = hideByBackwardEvents.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, item, GUIContent.none);
                }
            };
        }
    }

    [CustomEditor(typeof(UIButton)), CanEditMultipleObjects]
    public class UIButtonEditor : Editor
    {
        private SerializedProperty buttonName;
        private SerializedProperty clickAnimation, hoverAnimation, unhoverAnimation, selectAnimation, deselectAnimation;
        private SerializedProperty disableWhileHovering, disableWhileAnimating, disableWhileSelecting, disableWithTime, disableTime;
        private SerializedProperty hideParentView;
        private SerializedProperty menusToShow, menusToHide, menusToShowAsPopUp;
        private SerializedProperty interactiblityBinding, selectionObject, selectionAffectedByHover;
        private SerializedProperty useToggling, savingKey, autoSaveToggle, autoLoadToggle, eventOnLoadToggle, toggleEventSender, toggleCheckImage, changeToggleSprite, toggleOnSprite, toggleOffSprite, toggleEvent, toggleEventOn, toggleEventOff, isOn;
        private SerializedProperty eventSenders;
        private SerializedProperty clickAudio, selectAudio, hoverAudio, toggleOnAudio, toggleOffAudio;
        private SerializedProperty clickHaptics, selectHaptics, hoverHaptics, toggleOnHaptics, toggleOffHaptics;
        private SerializedProperty invokeBackButton, canvasGroupFade, popUpHideDelay;
        private SerializedProperty unityEventOnClick, unityEventAfterClickAnimation;
        private SerializedProperty unityEventOnHover, unityEventOnUnhover;
        private SerializedProperty unityEventOnSelect, unityEventOnUnselect;
        private SerializedProperty treatHoverAsSelection;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Animations", "Disabling", "Menus", "Toggle", "Events", "Audio", "Haptics", "Misc" };

        private ReorderableList eventList, menusShow, menusHide, menusShowAsPopUp;

        bool clickNotNull = false;
        bool hoverNotNull = false;
        bool selectNotNull = false;

        private void OnEnable()
        {
            clickAnimation = serializedObject.FindProperty("clickAnimation");
            hoverAnimation = serializedObject.FindProperty("hoverAnimation");
            unhoverAnimation = serializedObject.FindProperty("unhoverAnimation");
            selectAnimation = serializedObject.FindProperty("selectAnimation");
            deselectAnimation = serializedObject.FindProperty("deselectAnimation");

            disableWhileHovering = serializedObject.FindProperty("disableWhileHovering");
            disableWhileAnimating = serializedObject.FindProperty("disableWhileAnimating");
            disableWhileSelecting = serializedObject.FindProperty("disableWhileSelecting");
            disableWithTime = serializedObject.FindProperty("disableWithTime");
            disableTime = serializedObject.FindProperty("disableTime");

            hideParentView = serializedObject.FindProperty("hideParentView");

            menusToShow = serializedObject.FindProperty("menusToShow");
            menusToHide = serializedObject.FindProperty("menusToHide");
            menusToShowAsPopUp = serializedObject.FindProperty("menusToShowAsPopUp");
            interactiblityBinding = serializedObject.FindProperty("interactiblityBinding");
            selectionObject = serializedObject.FindProperty("selectionObject");
            selectionAffectedByHover = serializedObject.FindProperty("selectionAffectedByHover");

            useToggling = serializedObject.FindProperty("useToggling");
            savingKey = serializedObject.FindProperty("savingKey");
            autoSaveToggle = serializedObject.FindProperty("autoSaveToggle");
            autoLoadToggle = serializedObject.FindProperty("autoLoadToggle");
            eventOnLoadToggle = serializedObject.FindProperty("eventOnLoadToggle");
            toggleEventSender = serializedObject.FindProperty("toggleEventSender");
            toggleCheckImage = serializedObject.FindProperty("toggleCheckImage");
            changeToggleSprite = serializedObject.FindProperty("changeToggleSprite");
            toggleOnSprite = serializedObject.FindProperty("toggleOnSprite");
            toggleOffSprite = serializedObject.FindProperty("toggleOffSprite");
            toggleEvent = serializedObject.FindProperty("toggleEvent");
            toggleEventOn = serializedObject.FindProperty("toggleEventOn");
            toggleEventOff = serializedObject.FindProperty("toggleEventOff");
            isOn = serializedObject.FindProperty("isOn");

            eventSenders = serializedObject.FindProperty("eventSenders");

            clickAudio = serializedObject.FindProperty("clickAudio");
            selectAudio = serializedObject.FindProperty("selectAudio");
            hoverAudio = serializedObject.FindProperty("hoverAudio");
            toggleOnAudio = serializedObject.FindProperty("toggleOnAudio");
            toggleOffAudio = serializedObject.FindProperty("toggleOffAudio");
            
            clickHaptics = serializedObject.FindProperty("clickHaptics");
            selectHaptics = serializedObject.FindProperty("selectHaptics");
            hoverHaptics = serializedObject.FindProperty("hoverHaptics");
            toggleOnHaptics = serializedObject.FindProperty("toggleOnHaptics");
            toggleOffHaptics = serializedObject.FindProperty("toggleOffHaptics");

            invokeBackButton = serializedObject.FindProperty("invokeBackButton");
            canvasGroupFade = serializedObject.FindProperty("canvasGroupFade");
            popUpHideDelay = serializedObject.FindProperty("popUpHideDelay");

            unityEventOnClick = serializedObject.FindProperty("unityEventOnClick");
            unityEventAfterClickAnimation = serializedObject.FindProperty("unityEventAfterClickAnimation");

            unityEventOnHover = serializedObject.FindProperty("unityEventOnHover");
            unityEventOnUnhover = serializedObject.FindProperty("unityEventOnUnhover");

            unityEventOnSelect = serializedObject.FindProperty("unityEventOnSelect");
            unityEventOnUnselect = serializedObject.FindProperty("unityEventOnUnselect");

            treatHoverAsSelection = serializedObject.FindProperty("treatHoverAsSelection");


            SetupReorderableLists();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            clickNotNull = clickAnimation.objectReferenceValue != null;
            hoverNotNull = hoverAnimation.objectReferenceValue != null;
            selectNotNull = selectAnimation.objectReferenceValue != null;

            EditorGUILayout.BeginVertical();
            GUILayout.Space(5);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = SSColors.MediumPurple },
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.HelpBox("Customize how this UIButton behaves, animates, and interacts with menus.", MessageType.Info);
            GUILayout.Space(5);

            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;

            GUILayout.Space(1);

            // Draw name field
            buttonName = serializedObject.FindProperty("buttonName");
            EditorGUILayout.PropertyField(buttonName, new GUIContent("Button Name"));
            if (buttonName.stringValue.HasValue())
            {
                // tell the user they can use this name to control the button in code
                EditorGUILayout.HelpBox("You can use this name to control the button in code, such as invoking it by name.", MessageType.Info);
            }

            // if name empty, add generate button name button
            if (string.IsNullOrEmpty(buttonName.stringValue))
            {
                if (GUILayout.Button("Generate Unique Button Name"))
                {
                    foreach (var obj in targets)
                    {
                        if (obj is UIButton button)
                        {
                            SerializedObject serializedButton = new SerializedObject(button);
                            SerializedProperty nameProp = serializedButton.FindProperty("buttonName");
                            nameProp.stringValue = button.GenerateButtonName();
                            serializedButton.ApplyModifiedProperties();
                        }
                    }
                }
            }

            // space
            GUILayout.Space(5);

            switch (selectedTab)
            {
                case 0: DrawAnimationSettings(); break;
                case 1: DrawDisablingSettings(); break;
                case 2: DrawMenuSettings(); break;
                case 3: DrawToggleSettings(); break;
                case 4: DrawEventSettings(); break;
                case 5: DrawAudioSettings(); break;
                case 6: DrawHapticsSettings(); break;
                case 7: DrawMiscSettings(); break;
            }

            GUILayout.Space(10);

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("🎮 Runtime Controls", titleStyle);

                UIButton button = (UIButton)target;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("▶ Perform Click", GUILayout.Height(25)))
                {
                    button.PerformClick();
                }
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("🔄 Perform Hover", GUILayout.Height(25)))
                {
                    button.PerformHover();
                }
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("✅ Perform Select", GUILayout.Height(25)))
                {
                    button.PerformSelect();
                }
                GUI.backgroundColor = Color.white;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("🛠️ Editor Controls", titleStyle);
                UIButton button = (UIButton)target;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("▶ Test Click Animation", GUILayout.Height(25)))
                {
                    EditorPreviewInjector.Start(button.ClickAnimationAsset, button.gameObject);
                }
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("🔄 Test Hover Animation", GUILayout.Height(25)))
                {
                    EditorPreviewInjector.Start(button.HoverAnimationAsset, button.gameObject);
                }
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("✅ Test Select Animation", GUILayout.Height(25)))
                {
                    EditorPreviewInjector.Start(button.SelectAnimationAsset, button.gameObject);
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("⛔ Test Deselect Animation", GUILayout.Height(25)))
                {
                    EditorPreviewInjector.Start(button.DeselectAnimationAsset, button.gameObject);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAnimationSettings()
        {
            EditorGUILayout.LabelField("🎭 Animation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(clickAnimation, new GUIContent("Click Animation"));
            
            // Conditionally show/hide hover animations based on treatHoverAsSelection
            if (treatHoverAsSelection.boolValue)
            {
                EditorGUILayout.HelpBox("Hover animations are disabled when 'Treat Hover as Selection' is enabled. Hovering will trigger select animations instead.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(hoverAnimation, new GUIContent("Hover Animation"));
                EditorGUILayout.PropertyField(unhoverAnimation, new GUIContent("Unhover Animation"));
            }
            
            EditorGUILayout.PropertyField(selectAnimation, new GUIContent("Select Animation"));
            EditorGUILayout.PropertyField(deselectAnimation, new GUIContent("Deselect Animation"));

            if (!clickNotNull && !hoverNotNull && !selectNotNull)
            {
                EditorGUILayout.HelpBox("No animations are set. The button will not animate.", MessageType.Warning);
            }
        }

        private void DrawDisablingSettings()
        {
            EditorGUILayout.PropertyField(disableWithTime, new GUIContent("Disable With Time"));

            if (disableWithTime.boolValue)
            {
                EditorGUILayout.PropertyField(disableTime, new GUIContent("Disable Time"));
            }

            if (hoverNotNull)
                EditorGUILayout.PropertyField(disableWhileHovering, new GUIContent("Disable While Animating [Hover]"));

            if (clickNotNull)
                EditorGUILayout.PropertyField(disableWhileAnimating, new GUIContent("Disable While Animating [Click]"));

            if (selectNotNull)
                EditorGUILayout.PropertyField(disableWhileSelecting, new GUIContent("Disable While Animating [Select]"));
        }

        private void DrawMenuSettings()
        {
            EditorGUILayout.PropertyField(hideParentView, new GUIContent("Hide Parent View"));

            EditorHelpers.DrawMenuDropdown("Quick Add to Show", menusShow, serializedObject);
            menusShow.DoLayoutList();

            EditorHelpers.DrawMenuDropdown("Quick Add to Show as Pop-Up", menusShowAsPopUp, serializedObject);
            // helpbox to tell that they use default hide, if you want to change that, go to the last tab for extras, but is recommended to code it and invoke a method instead
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox("To change the default after-show hide behavior, it is recommended to code it and invoke a method instead. To change the delay time, go to [Misc] tab.", MessageType.Info);
            menusShowAsPopUp.DoLayoutList();

            EditorHelpers.DrawMenuDropdown("Quick Add to Hide", menusHide, serializedObject);
            menusHide.DoLayoutList();
        }

        private void DrawToggleSettings()
        {
            EditorGUILayout.PropertyField(useToggling, new GUIContent("Use Toggling"));
            if (useToggling.boolValue)
            {
                EditorGUILayout.HelpBox("The saving key is used to save the toggle state. It is recommended to use a unique key for each toggle.", MessageType.Info);
                EditorGUILayout.PropertyField(savingKey, new GUIContent("Saving Key"));
                EditorGUILayout.PropertyField(autoSaveToggle, new GUIContent("Auto Save Toggle"));
                EditorGUILayout.PropertyField(autoLoadToggle, new GUIContent("Auto Load Toggle"));
                // helpbox to tell the user that the event on load toggle is used to invoke the toggle event when the toggle is loaded
                EditorGUILayout.HelpBox("The Event On Load Toggle is used to invoke the toggle event when the toggle is loaded. It is sent along with the bool argument so you can use it.", MessageType.Info);
                EditorGUILayout.PropertyField(eventOnLoadToggle, new GUIContent("Event On Load Toggle"));
                EditorGUILayout.PropertyField(toggleCheckImage, new GUIContent("Toggle Check Image"));
                EditorGUILayout.HelpBox("Changing the toggle image will switch out sprites instead of enabling/disabling the toggle check image.", MessageType.Info);
                EditorGUILayout.PropertyField(changeToggleSprite, new GUIContent("Change Toggle Sprite"));
                // infobox to tell the dev that if sprites are set, the toggle check image will just be enabled/disabled
                if (changeToggleSprite.boolValue)
                {
                    EditorGUILayout.PropertyField(toggleOnSprite, new GUIContent("Toggle On Sprite"));
                    EditorGUILayout.PropertyField(toggleOffSprite, new GUIContent("Toggle Off Sprite"));

                    // vertical box showing an error if either sprites are null when this check is active
                    if (toggleOnSprite.objectReferenceValue == null || toggleOffSprite.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox("Toggle On and Off sprites must be set to change the toggle sprite.", MessageType.Error);
                    }
                }
                EditorGUILayout.PropertyField(toggleEvent, new GUIContent("Toggle Event"));
                EditorGUILayout.PropertyField(toggleEventOn, new GUIContent("Toggle Event On"));
                EditorGUILayout.PropertyField(toggleEventOff, new GUIContent("Toggle Event Off"));
                EditorGUILayout.PropertyField(isOn, new GUIContent("Is On"));
            }
        }

        private void DrawEventSettings()
        {
            // Event List Group
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            eventList.DoLayoutList();
            EditorGUILayout.EndVertical();

            // Horizontal Line Divider
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(5);

            if (useToggling.boolValue)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Toggle Events", EditorStyles.boldLabel);

                string eventNamePreview = toggleEventSender.stringValue;
                if (string.IsNullOrWhiteSpace(eventNamePreview))
                {
                    eventNamePreview = "someEvent";
                }

                EditorGUILayout.HelpBox(
                    $"Dispatches the toggle state as the first argument of the signal payload. Listen for it and unpack the bool like so:\nSIGS.Listener(\"{eventNamePreview}\", (s) => {{ var isOn = (bool)s[0]; /* ... */ }});",
                    MessageType.Info);
                EditorGUILayout.PropertyField(toggleEventSender, new GUIContent("Toggle Event Sender"));
                EditorGUILayout.EndVertical();

                GUILayout.Space(5);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUILayout.Space(5);
            }

            // Unity Events Group
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Unity Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(unityEventOnClick, new GUIContent("On Click Event"));
            
            if (treatHoverAsSelection.boolValue)
            {
                EditorGUILayout.HelpBox("Hover events are disabled when 'Treat Hover as Selection' is enabled. Hovering will trigger select events instead.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(unityEventOnHover, new GUIContent("On Hover Event"));
                EditorGUILayout.PropertyField(unityEventOnUnhover, new GUIContent("On Unhover Event"));
            }
            
            EditorGUILayout.PropertyField(unityEventOnSelect, new GUIContent("On Select Event"));
            EditorGUILayout.PropertyField(unityEventOnUnselect, new GUIContent("On Unselect Event"));
            EditorGUILayout.EndVertical();

            // Horizontal Line Divider
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(5);

            EditorGUILayout.LabelField("Animation Timing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Control when Unity Events are invoked relative to their animations.", MessageType.Info);
            
            EditorGUILayout.PropertyField(unityEventAfterClickAnimation, new GUIContent("Wait for Click Animation"));
        }

        private void DrawAudioSettings()
        {
            // start vertical helpbox
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorHelpers.DrawAudioDropdown("Click Audio", clickAudio, serializedObject);
            EditorHelpers.DrawAudioDropdown("Select Audio", selectAudio, serializedObject);
            EditorHelpers.DrawAudioDropdown("Hover Audio", hoverAudio, serializedObject);
            EditorGUILayout.EndVertical();

            if (useToggling.boolValue)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorHelpers.DrawAudioDropdown("Toggle On Audio", toggleOnAudio, serializedObject);
                EditorHelpers.DrawAudioDropdown("Toggle Off Audio", toggleOffAudio, serializedObject);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawHapticsSettings()
        {
            EditorGUILayout.LabelField("⚡ Haptic Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Haptic feedback that plays alongside audio events.", MessageType.Info);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(clickHaptics, new GUIContent("Click Haptics"), true);
            EditorGUILayout.PropertyField(selectHaptics, new GUIContent("Select Haptics"), true);
            EditorGUILayout.PropertyField(hoverHaptics, new GUIContent("Hover Haptics"), true);
            EditorGUILayout.EndVertical();

            if (useToggling.boolValue)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(toggleOnHaptics, new GUIContent("Toggle On Haptics"), true);
                EditorGUILayout.PropertyField(toggleOffHaptics, new GUIContent("Toggle Off Haptics"), true);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawMiscSettings()
        {
            EditorGUILayout.PropertyField(interactiblityBinding, new GUIContent("Interactivity Binding"));
            EditorGUILayout.PropertyField(invokeBackButton, new GUIContent("Invoke Back Button"));
            EditorGUILayout.PropertyField(canvasGroupFade, new GUIContent("Canvas Group Fade"));
            EditorGUILayout.PropertyField(popUpHideDelay, new GUIContent("Pop Up Delay"));
            
            // Treat Hover as Selection
            GUILayout.Space(5);
            EditorGUILayout.HelpBox("When enabled, mouse hover will trigger select/deselect animations and events instead of hover/unhover. Perfect for PC-focused experiences.", MessageType.Info);
            EditorGUILayout.PropertyField(treatHoverAsSelection, new GUIContent("Treat Hover as Selection"));
            
            // selection object
            GUILayout.Space(5);
            EditorGUILayout.HelpBox("This object enables when the button is selected. It is disabled when the button is not selected.", MessageType.Info);
            EditorGUILayout.PropertyField(selectionObject, new GUIContent("Selection Object"));
            if (selectionObject.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(selectionAffectedByHover, new GUIContent("Also on Hover"));
            }
        }

        private void SetupReorderableLists()
        {
            eventList = new ReorderableList(serializedObject, eventSenders, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Event Senders", EditorStyles.boldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty item = eventSenders.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, item, GUIContent.none);
                }
            };

            menusShow = new ReorderableList(serializedObject, menusToShow, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Menus to Show", EditorStyles.boldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty item = menusToShow.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, item, GUIContent.none);
                }
            };

            menusShowAsPopUp = new ReorderableList(serializedObject, menusToShowAsPopUp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Menus to Show as Pop-Up", EditorStyles.boldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty item = menusToShowAsPopUp.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, item, GUIContent.none);
                }
            };

            menusHide = new ReorderableList(serializedObject, menusToHide, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Menus to Hide", EditorStyles.boldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty item = menusToHide.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, item, GUIContent.none);
                }
            };
        }
    }

    [CustomEditor(typeof(UIAnimatable)), CanEditMultipleObjects]
    public class UIAnimatableEditor : Editor
    {
        private SerializedProperty animationArray;
        private SerializedProperty dontBlockClicks;
        private int animationIndex = 0;
        private string animationLabel = "";

        private GUIContent playIcon;
        private GUIContent cancelIcon;

        private void OnEnable()
        {
            animationArray = serializedObject.FindProperty("animationArray");
            dontBlockClicks = serializedObject.FindProperty("dontBlockClicks");

            // Load icons (Unity built-in icons)
            playIcon = EditorGUIUtility.IconContent("d_PlayButton");
            cancelIcon = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Customize how animations behave and interact with this element.", MessageType.Info);

            if (Application.isPlaying)
                DrawActionsSectionPlayMode(); // Add Play/Cancel buttons

            if (!Application.isPlaying)
                DrawActionsSectionEditorMode();

            DrawMainSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawActionsSectionPlayMode()
        {
            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.9f, 1f, 0.9f); // light green box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            GUILayout.Label("▶ Actions [Play Mode]", EditorStyles.boldLabel);
            GUILayout.Space(5);

            DrawActionRow("Play First Animation", () => PlayFirstAnimation(), playIcon, Color.green);

            GUILayout.Space(5);
            animationIndex = DrawActionRowWithInt("Play Animation by Index", animationIndex, idx => PlayAnimationFromIndex(idx), playIcon, Color.green);
            animationLabel = DrawActionRowWithText("Play Animation by Label", animationLabel, lbl => PlayAnimation(lbl), playIcon, Color.green);

            GUILayout.Space(5);
            animationIndex = DrawActionRowWithInt("Cancel Animation by Index", animationIndex, idx => CancelAnimation(idx), cancelIcon, Color.red);
            animationLabel = DrawActionRowWithText("Cancel Animation by Label", animationLabel, lbl => CancelAnimation(lbl), cancelIcon, Color.red);

            EditorGUILayout.EndVertical();
        }

        private void DrawActionsSectionEditorMode()
        {
            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.9f, 0.95f, 1f); // light blue box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            GUILayout.Label("🧪 Actions [Editor Preview]", EditorStyles.boldLabel);
            GUILayout.Space(5);

            DrawActionRow("Play First Animation", () => PreviewFirstAnimation(), playIcon, Color.green);

            GUILayout.Space(5);
            animationIndex = DrawActionRowWithInt("Play Animation by Index", animationIndex, idx => PreviewAnimationFromIndex(idx), playIcon, Color.green);
            animationLabel = DrawActionRowWithText("Play Animation by Label", animationLabel, lbl => PreviewAnimation(lbl), playIcon, Color.green);

            GUILayout.Space(5);
            animationIndex = DrawActionRowWithInt("Cancel Animation by Index", animationIndex, idx => PreviewCancelAnimation(idx), cancelIcon, Color.red);
            animationLabel = DrawActionRowWithText("Cancel Animation by Label", animationLabel, lbl => PreviewCancelAnimation(lbl), cancelIcon, Color.red);

            EditorGUILayout.EndVertical();
        }

        private void DrawActionRow(string label, Action callback, GUIContent icon, Color buttonColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(170));

            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(icon, GUILayout.Width(30), GUILayout.Height(20)))
                callback.Invoke();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private int DrawActionRowWithInt(string label, int currentValue, Action<int> callback, GUIContent icon, Color buttonColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(170));
            int value = EditorGUILayout.IntField(currentValue, GUILayout.Width(50));

            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(icon, GUILayout.Width(30), GUILayout.Height(20)))
                callback.Invoke(value);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            return value;
        }

        private string DrawActionRowWithText(string label, string currentValue, Action<string> callback, GUIContent icon, Color buttonColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(170));
            string value = EditorGUILayout.TextField(currentValue);

            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(icon, GUILayout.Width(30), GUILayout.Height(20)))
                callback.Invoke(value);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            return value;
        }


        private void DrawMainSettings()
        {
            EditorGUILayout.PropertyField(animationArray, new GUIContent("Animation List"), true);
            EditorGUILayout.PropertyField(dontBlockClicks, new GUIContent("Don't Block Clicks", "Allow this animatable to play without disabling button interactions."));
        }

        private void PlayFirstAnimation()
        {
            UIAnimatable animatable = (UIAnimatable)target;
            animatable.PlayFirstAnimation();
            Debug.Log($"🎬 Played First Animation!");
        }

        private void PlayAnimationFromIndex(int index)
        {
            UIAnimatable animatable = (UIAnimatable)target;

            if (index >= 0 && index < animatable.AnimationArray.Length)
            {
                animatable.PlayAnimationFromIndex(index);
                Debug.Log($"🎬 Played Animation at Index {index}");
            }
            else
            {
                Debug.LogWarning($"⚠ Invalid Animation Index {index}!");
            }
        }

        private void PlayAnimation(string label)
        {
            UIAnimatable animatable = (UIAnimatable)target;

            if (!string.IsNullOrEmpty(label))
            {
                animatable.PlayAnimation(label);
                Debug.Log($"🎬 Played Animation: {label}");
            }
            else
            {
                Debug.LogWarning($"⚠ Animation Label is empty!");
            }
        }

        private void CancelAnimation(int index)
        {
            UIAnimatable animatable = (UIAnimatable)target;

            if (index >= 0 && index < animatable.AnimationArray.Length)
            {
                animatable.CancelAnimation(index);
                Debug.Log($"⛔ Canceled Animation at Index {index}");
            }
            else
            {
                Debug.LogWarning($"⚠ Invalid Animation Index {index}!");
            }
        }

        private void CancelAnimation(string label)
        {
            UIAnimatable animatable = (UIAnimatable)target;

            if (!string.IsNullOrEmpty(label))
            {
                animatable.CancelAnimation(label);
                Debug.Log($"⛔ Canceled Animation: {label}");
            }
            else
            {
                Debug.LogWarning($"⚠ Animation Label is empty!");
            }
        }

        // PREVIEW METHODS
        private void PreviewFirstAnimation()
        {
            UIAnimatable animatable = (UIAnimatable)target;
            EditorPreviewInjector.Start(animatable.AnimationArray[0].AnimationAsset, animatable.gameObject);
        }

        private void PreviewAnimationFromIndex(int index)
        {
            UIAnimatable animatable = (UIAnimatable)target;
            if (index >= 0 && index < animatable.AnimationArray.Length)
            {
                EditorPreviewInjector.Start(animatable.AnimationArray[index].AnimationAsset, animatable.gameObject);
            }
            else
            {
                Debug.LogWarning($"⚠ Invalid Animation Index {index}!");
            }
        }

        private void PreviewAnimation(string label)
        {
            UIAnimatable animatable = (UIAnimatable)target;
            foreach (var animation in animatable.AnimationArray)
            {
                if (animation.Label == label)
                {
                    EditorPreviewInjector.Start(animation.AnimationAsset, animatable.gameObject);
                    return;
                }
            }
            Debug.LogWarning($"⚠ Animation Label '{label}' not found!");
        }

        private void PreviewCancelAnimation(int index)
        {
            UIAnimatable animatable = (UIAnimatable)target;
            if (index >= 0 && index < animatable.AnimationArray.Length)
            {
                var targetGo = animatable.GetAnimatableTarget(index);
                DG.DOTweenEditor.DOTweenEditorPreview.Stop(true, true);
                animatable.AnimationArray[index].AnimationAsset.StopPreview(targetGo);
                animatable.AnimationArray[index].AnimationAsset.StopAnimations();
            }
            else
            {
                Debug.LogWarning($"⚠ Invalid Animation Index {index}!");
            }
        }

        private void PreviewCancelAnimation(string label)
        {
            UIAnimatable animatable = (UIAnimatable)target;
            var targetGo = animatable.GetAnimatableTarget(label);
            var animation = animatable.AnimationArray.FirstOrDefault(x => x.Label == label);
            if (animation != null)
            {
                DG.DOTweenEditor.DOTweenEditorPreview.Stop(true, true);
                animation.AnimationAsset.StopPreview(targetGo);
                animation.AnimationAsset.StopAnimations();
            }
            else
            {
                Debug.LogWarning($"⚠ Animation Label '{label}' not found!");
            }
        }
    }

    [CustomEditor(typeof(UIToggleGroup)), CanEditMultipleObjects]
    public class UIToggleGroupEditor : Editor
    {
        private static readonly Dictionary<string, int> tabIndices = new();

        private SerializedProperty buttonsProp;
        private SerializedProperty maxSelectedProp;
        private SerializedProperty allChildrenProp;
        private SerializedProperty alwaysOneSelectedProp;
        private SerializedProperty initialSelectionProp;
        private SerializedProperty indexesProp;

        private void OnEnable()
        {
            buttonsProp = serializedObject.FindProperty("buttons");
            maxSelectedProp = serializedObject.FindProperty("maxSelected");
            allChildrenProp = serializedObject.FindProperty("allChildren");
            alwaysOneSelectedProp = serializedObject.FindProperty("alwaysOneSelected");
            initialSelectionProp = serializedObject.FindProperty("initialSelection");
            indexesProp = serializedObject.FindProperty("indexes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            UIToggleGroup toggleGroup = (UIToggleGroup)target;

            string propertyKey = target.GetInstanceID().ToString();
            if (!tabIndices.ContainsKey(propertyKey))
                tabIndices[propertyKey] = 0; // Default to "Main"

            GUILayout.Space(5);

            // **Infobox**
            EditorGUILayout.HelpBox("A Toggle Group is a collection of UIToggleButtons. Place this on the parent of some buttons with toggling active and only a certain amount can be selected at a time.", MessageType.Info);

            // **Toolbar with Tabs**
            string[] toolbarOptions = { "Main", "Selection" };
            GUI.backgroundColor = Color.gray;
            tabIndices[propertyKey] = GUILayout.Toolbar(tabIndices[propertyKey], toolbarOptions, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(5);

            // **Switch Tabs**
            switch (tabIndices[propertyKey])
            {
                case 0: // **Main Tab**
                    DrawMainTab(toggleGroup);
                    break;

                case 1: // **Selection Tab**
                    DrawSelectionTab();
                    break;
            }

            DrawActionsTab(toggleGroup);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMainTab(UIToggleGroup toggleGroup)
        {
            EditorGUILayout.PropertyField(buttonsProp, new GUIContent("Buttons"), true);
            EditorGUILayout.PropertyField(maxSelectedProp, new GUIContent("Max Selected"));
            EditorGUILayout.PropertyField(allChildrenProp, new GUIContent("Include All Children"));

            // Show "Always One Selected" only when maxSelected == 1
            if (maxSelectedProp.intValue == 1)
            {
                EditorGUILayout.PropertyField(alwaysOneSelectedProp, new GUIContent("Always One Selected"));
            }
        }

        private void DrawSelectionTab()
        {
            EditorGUILayout.PropertyField(initialSelectionProp, new GUIContent("Enable Initial Selection"));

            if (initialSelectionProp.boolValue)
            {
                EditorGUILayout.PropertyField(indexesProp, new GUIContent("Selected Indexes"), true);
            }
        }

        private void DrawActionsTab(UIToggleGroup toggleGroup)
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Find Children", GUILayout.Height(30)))
            {
                Undo.RecordObject(toggleGroup, "Find Children");
                toggleGroup.RefreshGroup();
                EditorUtility.SetDirty(toggleGroup);
            }

            if (GUILayout.Button("Clear Buttons List", GUILayout.Height(30)))
            {
                Undo.RecordObject(toggleGroup, "Clear Buttons");
                toggleGroup.Buttons.Clear();
                EditorUtility.SetDirty(toggleGroup);
            }
        }
    }

    [CustomEditor(typeof(UIViewGroup)), CanEditMultipleObjects]
    public class UIViewGroupEditor : Editor
    {
        private static readonly Dictionary<string, int> tabIndices = new();

        private SerializedProperty viewsProp;

        private void OnEnable()
        {
            viewsProp = serializedObject.FindProperty("views");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            UIViewGroup viewGroup = (UIViewGroup)target;

            string propertyKey = target.GetInstanceID().ToString();
            if (!tabIndices.ContainsKey(propertyKey))
                tabIndices[propertyKey] = 0; // Default to "Main"

            GUILayout.Space(5);

            GUILayout.Space(5);

            // infobox describing how it works
            EditorGUILayout.HelpBox("A View Group is a collection of UIViews. Place this on the parent of some views and only one of them can be visible at a time.", MessageType.Info);

            DrawMainTab();

            DrawActionsTab(viewGroup);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMainTab()
        {
            EditorGUILayout.LabelField("View Group", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(viewsProp, new GUIContent("Views"), true);
        }

        private void DrawActionsTab(UIViewGroup viewGroup)
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Find Children", GUILayout.Height(30)))
            {
                Undo.RecordObject(viewGroup, "Find Children");
                viewGroup.Views.Clear();
                foreach (Transform child in viewGroup.transform)
                {
                    UIView view = child.GetComponent<UIView>();
                    if (view != null)
                    {
                        viewGroup.Views.Add(view);
                    }
                }
                EditorUtility.SetDirty(viewGroup);
            }

            if (GUILayout.Button("Clear Views", GUILayout.Height(30)))
            {
                Undo.RecordObject(viewGroup, "Clear Views");
                viewGroup.Views.Clear();
                EditorUtility.SetDirty(viewGroup);
            }
        }
    }
    
    [CustomEditor(typeof(UIElement)), CanEditMultipleObjects]
    public class UIElementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            UIElement uiElement = (UIElement)target;

            GUILayout.Space(5);

            // draw name field
            EditorGUILayout.PropertyField(serializedObject.FindProperty("elementName"), new GUIContent("Element Name"));
            if (string.IsNullOrEmpty(uiElement.ElementName))
            {
                EditorGUILayout.HelpBox("Element Name is empty. It is recommended to set a unique name for this UI Element.", MessageType.Warning);

                if (GUILayout.Button("Generate Unique Element Name"))
                {
                    uiElement.GenerateUniqueElementName();
                    EditorUtility.SetDirty(uiElement);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(UIAnimationAsset)), CanEditMultipleObjects]
    public class UIAnimationAssetEditor : Editor
    {
        private GameObject previewTarget; // Editor-only, non-persistent

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            UIAnimationAsset animationAsset = (UIAnimationAsset)target;

            GUILayout.Space(5);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Preview animations in the editor. Assign a target GameObject below.", MessageType.Info);

                previewTarget = (GameObject)EditorGUILayout.ObjectField("Preview Target", previewTarget, typeof(GameObject), true);

                GUIContent playIcon = EditorGUIUtility.IconContent("d_PlayButton");
                if (GUILayout.Button(new GUIContent(" Preview Animations", playIcon.image), GUILayout.Height(30)))
                {
                    if (previewTarget != null)
                        EditorPreviewInjector.Start(animationAsset, previewTarget);
                    else
                        Debug.LogWarning("Please assign a target GameObject to preview.");
                }

                GUIContent stopIcon = EditorGUIUtility.IconContent("d_PreMatQuad");
                if (GUILayout.Button(new GUIContent(" Stop Animations", stopIcon.image), GUILayout.Height(30)))
                {
                    if (previewTarget != null)
                    {
                        DG.DOTweenEditor.DOTweenEditorPreview.Stop(true, true);
                        animationAsset.StopPreview(previewTarget);
                        animationAsset.StopAnimations();
                    }
                }

                GUIContent debugIcon = EditorGUIUtility.IconContent("console.warnicon.sml");
                if (GUILayout.Button(new GUIContent(" Debug Full End Time", debugIcon.image), GUILayout.Height(22)))
                    Debug.Log($"Full End Time: {animationAsset.FullEndTime()}");

                if (GUILayout.Button(new GUIContent(" Debug Has A Loop", debugIcon.image), GUILayout.Height(22)))
                    Debug.Log($"Has a Loop: {animationAsset.HasALoop()}");

                if (GUILayout.Button(new GUIContent(" Debug Has Infinite Loop", debugIcon.image), GUILayout.Height(22)))
                    Debug.Log($"Has an Infinite Loop: {animationAsset.HasAnInfiniteLoop()}");

                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }
#endif

            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Unscaled time: Realtime animations will not be affected by time scale.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unscaledTime"), new GUIContent("Unscaled Time"));
            EditorGUILayout.HelpBox("Disable Frags: The Frags system tries to retain elements' original positions so they don't spaz out on spamming.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("disableFrags"), new GUIContent("Disable Frags"));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Add", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fade 0-1", GUILayout.Height(30))) animationAsset.Add_Fade01();
            if (GUILayout.Button("Fade 1-0", GUILayout.Height(30))) animationAsset.Add_Fade10();
            if (GUILayout.Button("Scale Up", GUILayout.Height(30))) animationAsset.Add_ScaleUp();
            if (GUILayout.Button("Scale Down", GUILayout.Height(30))) animationAsset.Add_ScaleDown();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Punch Position", GUILayout.Height(30))) animationAsset.Add_PunchPosition();
            if (GUILayout.Button("Punch Rotation", GUILayout.Height(30))) animationAsset.Add_PunchRotation();
            if (GUILayout.Button("Punch Scale", GUILayout.Height(30))) animationAsset.Add_PunchScale();
            if (GUILayout.Button("Loop Z Rotation", GUILayout.Height(30))) animationAsset.Add_LoopZRotation();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.HelpBox("All animations listed below will play together on the target GameObject.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animations"), new GUIContent("Animations"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(UIFill)), CanEditMultipleObjects]
    public class UIFillEditor : Editor
    {
        private SerializedProperty animationAsset, startFill, animateFill, maxFill, minFill;
        private SerializedProperty adjustmentListener, adjustedAudioPos, adjustedAudioNeg, brimAnimation, brimStrength, brimAudio, directionFill, fillType, setFillListener;
        private SerializedProperty adjustedHapticsPos, adjustedHapticsNeg, brimHaptics;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Main", "Animation", "Events", "Audio", "Haptics" };

        private void OnEnable()
        {
            animationAsset = serializedObject.FindProperty("animationAsset");
            startFill = serializedObject.FindProperty("startFill");
            animateFill = serializedObject.FindProperty("animateFill");
            maxFill = serializedObject.FindProperty("maxFill");
            minFill = serializedObject.FindProperty("minFill");
            adjustmentListener = serializedObject.FindProperty("adjustmentListener");
            adjustedAudioPos = serializedObject.FindProperty("adjustedAudioPositive");
            adjustedAudioNeg = serializedObject.FindProperty("adjustedAudioNegative");
            brimAnimation = serializedObject.FindProperty("brimAnimation");
            brimStrength = serializedObject.FindProperty("brimStrength");
            brimAudio = serializedObject.FindProperty("brimAudio");
            
            adjustedHapticsPos = serializedObject.FindProperty("adjustedHapticsPositive");
            adjustedHapticsNeg = serializedObject.FindProperty("adjustedHapticsNegative");
            brimHaptics = serializedObject.FindProperty("brimHaptics");
            
            directionFill = serializedObject.FindProperty("directionFill");
            fillType = serializedObject.FindProperty("fillType");
            setFillListener = serializedObject.FindProperty("setFillListener");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical();

            GUIStyle titleStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = SSColors.Orange },
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.HelpBox("Create dynamic fill effects for UI elements. Supports both Image fill and Scale-based fills with customizable directions and animations.", MessageType.Info);
            GUILayout.Space(5);

            // **Tab Selection**
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);

            // **Tab Grouping Logic**
            switch (selectedTab)
            {
                case 0: DrawMainSettings(); break;
                case 1: DrawAnimationSettings(); break;
                case 2: DrawEventSettings(); break;
                case 3: DrawAudioSettings(); break;
                case 4: DrawHapticsSettings(); break;
            }

            GUILayout.Space(10);

            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        // **Main Settings**
        private void DrawMainSettings()
        {
            EditorGUILayout.LabelField("🎯 Fill Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(fillType, new GUIContent("Fill Type"));
            
            if (fillType.enumValueIndex == 0) // ImageFill
            {
                EditorGUILayout.HelpBox("Image Fill: Uses Unity's Image component fillAmount property. Automatically configures the Image's fill origin and method.", MessageType.Info);
            }
            else // Scale
            {
                EditorGUILayout.HelpBox("Scale Fill: Uses RectTransform's localScale to create fill effects. Automatically adjusts pivot for proper fill direction.", MessageType.Info);
            }

            GUILayout.Space(5);
            EditorGUILayout.PropertyField(directionFill, new GUIContent("Fill Direction"));
            
            // Show direction preview
            string directionInfo = GetDirectionInfo();
            EditorGUILayout.HelpBox(directionInfo, MessageType.None);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("📊 Fill Values", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(startFill, new GUIContent("Start Fill"));
            EditorGUILayout.PropertyField(minFill, new GUIContent("Min Fill"));
            EditorGUILayout.PropertyField(maxFill, new GUIContent("Max Fill"));

            // Validate fill range
            if (minFill.floatValue >= maxFill.floatValue)
            {
                EditorGUILayout.HelpBox("Min Fill should be less than Max Fill for proper functionality.", MessageType.Warning);
            }

            if (startFill.floatValue < minFill.floatValue || startFill.floatValue > maxFill.floatValue)
            {
                EditorGUILayout.HelpBox("Start Fill should be within the Min-Max Fill range.", MessageType.Warning);
            }

            // Draw testing
            DrawTestingSettings();
        }

        // **Animation Settings**
        private void DrawAnimationSettings()
        {
            EditorGUILayout.LabelField("🎭 Animation Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(animateFill, new GUIContent("Animate Fill"));
            
            if (animateFill.boolValue)
            {
                EditorGUILayout.HelpBox("When enabled, fill changes will be animated using the Animation Asset. Keep in mind, this won't use the whole animation asset, but just the duration and easing type, and only from the first animation in the asset.", MessageType.Info);
                EditorGUILayout.PropertyField(animationAsset, new GUIContent("Animation Asset"));
                
                if (animationAsset.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Animation Asset is required when Animate Fill is enabled.", MessageType.Warning);
                }
                
                GUILayout.Space(5);
                EditorGUILayout.PropertyField(brimAnimation, new GUIContent("Brim Animation"));
                
                if (brimAnimation.boolValue)
                {
                    EditorGUILayout.HelpBox("Brim Animation allows the fill to animate even when at min/max limits. Perfect for bounce easing types - creates a 'brim' effect when trying to fill beyond the limits.", MessageType.Info);
                    EditorGUILayout.PropertyField(brimStrength, new GUIContent("Brim Animation Strength"));
                }
                else
                {
                    EditorGUILayout.HelpBox("Brim Animation is disabled. Fill will not animate beyond min/max limits.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("When disabled, fill changes will be instant.", MessageType.Info);
            }
        }

        // **Event Settings**
        private void DrawEventSettings()
        {
            EditorGUILayout.LabelField("📡 Event Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox("The Adjustment Listener allows external systems to control the fill value dynamically.", MessageType.Info);
            EditorGUILayout.PropertyField(adjustmentListener, new GUIContent("Adjustment Listener"));
            
            if (!string.IsNullOrEmpty(adjustmentListener.stringValue))
            {
                EditorGUILayout.HelpBox("This listener will receive float values to adjust the fill. Use the AdjustFill(float) method to modify the fill value.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(setFillListener, new GUIContent("Set Fill Listener"));

            if (!string.IsNullOrEmpty(setFillListener.stringValue))
            {
                EditorGUILayout.HelpBox("This listener will receive float values to set the fill directly. Use the SetFill(float) method to set the fill value.", MessageType.Info);
            }
        }

        // **Audio Settings**
        private void DrawAudioSettings()
        {
            EditorGUILayout.LabelField("🔊 Audio Settings", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Audio that plays when the fill value is adjusted.", MessageType.Info);
            EditorHelpers.DrawAudioDropdown("Adjusted Positive Audio", adjustedAudioPos, serializedObject);
            EditorHelpers.DrawAudioDropdown("Adjusted Negative Audio", adjustedAudioNeg, serializedObject);

            var brimAnimation = serializedObject.FindProperty("brimAnimation");

            if (brimAnimation.boolValue)
            {
                EditorGUILayout.HelpBox("Brim Animation Audio: Plays when the brim effect is triggered during fill adjustments.", MessageType.Info);
                EditorHelpers.DrawAudioDropdown("Brim Animation Audio", brimAudio, serializedObject);
            }
        }

        // **Haptics Settings**
        private void DrawHapticsSettings()
        {
            EditorGUILayout.LabelField("⚡ Haptic Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Haptic feedback that plays alongside audio events.", MessageType.Info);
            
            EditorGUILayout.PropertyField(adjustedHapticsPos, new GUIContent("Adjusted Positive Haptics"), true);
            EditorGUILayout.PropertyField(adjustedHapticsNeg, new GUIContent("Adjusted Negative Haptics"), true);

            var brimAnimation = serializedObject.FindProperty("brimAnimation");

            if (brimAnimation.boolValue)
            {
                EditorGUILayout.HelpBox("Brim Animation Haptics: Plays when the brim effect is triggered during fill adjustments.", MessageType.Info);
                EditorGUILayout.PropertyField(brimHaptics, new GUIContent("Brim Animation Haptics"), true);
            }
        }

        // **Testing Settings**
        private void DrawTestingSettings()
        {
            EditorGUILayout.LabelField("🧪 Testing Controls", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("🎮 Runtime Testing", EditorStyles.boldLabel);
                
                UIFill fill = (UIFill)target;
                
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Fill to 100%", GUILayout.Height(25)))
                {
                    fill.SetFill(1f);
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Fill to 0%", GUILayout.Height(25)))
                {
                    fill.SetFill(0f);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.blue;
                if (GUILayout.Button("Fill to 50%", GUILayout.Height(25)))
                {
                    fill.SetFill(0.5f);
                }
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Fill to 75%", GUILayout.Height(25)))
                {
                    fill.SetFill(0.75f);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("Adjust +0.1", GUILayout.Height(25)))
                {
                    fill.AdjustFill(0.1f);
                }
                GUI.backgroundColor = Color.magenta;
                if (GUILayout.Button("Adjust -0.1", GUILayout.Height(25)))
                {
                    fill.AdjustFill(-0.1f);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("🛠️ Editor Testing", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Testing controls are available in Play Mode. Enter Play Mode to test fill animations and adjustments.", MessageType.Info);
            }
        }

        private string GetDirectionInfo()
        {
            switch (directionFill.enumValueIndex)
            {
                case 0: return "→ Left to Right: Fill grows from left edge toward right";
                case 1: return "← Right to Left: Fill grows from right edge toward left";
                case 2: return "↓ Top to Bottom: Fill grows from top edge toward bottom";
                case 3: return "↑ Bottom to Top: Fill grows from bottom edge toward top";
                default: return "Select a fill direction";
            }
        }
    }
}