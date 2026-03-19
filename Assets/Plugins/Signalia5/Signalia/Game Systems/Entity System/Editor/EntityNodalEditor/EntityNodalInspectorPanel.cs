using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace AHAKuo.Signalia.GameSystems.Entities.Editors
{
    /// <summary>
    /// Inspector panel for displaying StateNode information in the EntityNodalWindow.
    /// </summary>
    public class EntityNodalInspectorPanel
    {
        #region Private Fields
        
        private TwoPaneSplitView splitView;
        private VisualElement inspectorPanel;
        private Label inspectorHeader;
        private VisualElement inspectorContent;
        private StateNode selectedStateNode;
        private EntityLogic entityLogic;
        private System.Action onRefreshRequested;
        private System.Action<int> onInspectorChanged;
        private SerializedObject inspectorSerializedObject;
        private ReorderableList actionsList;
        private int inspectedStateIndex = -1;
        
        #endregion
        
        #region Properties
        
        public VisualElement Panel => inspectorPanel;
        public StateNode SelectedNode => selectedStateNode;
        public EntityLogic EntityLogic => entityLogic;
        
        #endregion
        
        #region Constructor
        
        public EntityNodalInspectorPanel(TwoPaneSplitView splitView, EntityLogic entityLogic = null)
        {
            this.splitView = splitView;
            this.entityLogic = entityLogic;
            ConstructInspectorPanel();
            SubscribeToEvents();
        }
        
        #endregion
        
        #region Construction
        
        private void ConstructInspectorPanel()
        {
            inspectorPanel = new VisualElement();
            inspectorPanel.AddToClassList("node-inspector-panel");
            
            CreateInspectorHeader();
            CreateInspectorContent();
            
            splitView.Add(inspectorPanel);
        }
        
        private void CreateInspectorHeader()
        {
            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("inspector-header");
            
            inspectorHeader = new Label("State");
            headerContainer.Add(inspectorHeader);
            inspectorPanel.Add(headerContainer);
        }
        
        private void CreateInspectorContent()
        {
            inspectorContent = new ScrollView(ScrollViewMode.Vertical);
            inspectorContent.AddToClassList("inspector-content");
            inspectorPanel.Add(inspectorContent);
        }
        
        #endregion
        
        #region Event Management
        
        private void SubscribeToEvents()
        {
            StateNode.OnNodeSelected += OnStateNodeSelected;
            StateNode.OnNodeDeselected += OnStateNodeDeselected;
        }
        
        public void UnsubscribeFromEvents()
        {
            StateNode.OnNodeSelected -= OnStateNodeSelected;
            StateNode.OnNodeDeselected -= OnStateNodeDeselected;
        }
        
        #endregion
        
        #region Node Selection Handlers
        
        private void OnStateNodeSelected(StateNode node)
        {
            selectedStateNode = node;
            ShowInspector(node);
        }
        
        private void OnStateNodeDeselected(StateNode node)
        {
            if (selectedStateNode == node)
            {
                HideInspector();
            }
        }
        
        #endregion
        
        #region Inspector Display
        
        public void ShowInspector(StateNode node)
        {
            selectedStateNode = node;
            UncollapseInspector();
            UpdateInspectorHeader();
            PopulateInspectorContent();
        }
        
        public void HideInspector()
        {
            if (splitView != null)
            {
                splitView.CollapseChild(1);
            }
            selectedStateNode = null;
        }
        
        public void RefreshInspector()
        {
            if (selectedStateNode != null)
            {
                ShowInspector(selectedStateNode);
            }
        }

        public void RefreshHeader()
        {
            UpdateInspectorHeader();
        }
        
        private void UncollapseInspector()
        {
            if (splitView != null)
            {
                splitView.UnCollapse();
            }
        }
        
        private void UpdateInspectorHeader()
        {
            if (selectedStateNode != null && selectedStateNode.State != null)
            {
                inspectorHeader.text = $"State: {selectedStateNode.State.stateName}";
            }
            else
            {
                inspectorHeader.text = "State";
            }
        }
        
        private void PopulateInspectorContent()
        {
            inspectorContent.Clear();
            
            if (selectedStateNode == null || selectedStateNode.State == null)
            {
                var placeholderLabel = new Label("No state selected.");
                placeholderLabel.AddToClassList("inspector-placeholder");
                inspectorContent.Add(placeholderLabel);
                return;
            }
            
            inspectorSerializedObject = new SerializedObject(entityLogic);
            inspectedStateIndex = selectedStateNode.StateIndex;
            actionsList = null;

            var imguiContainer = new IMGUIContainer(DrawInspectorIMGUI);
            inspectorContent.Add(imguiContainer);
        }

        private void DrawInspectorIMGUI()
        {
            if (entityLogic == null || inspectorSerializedObject == null)
            {
                return;
            }

            inspectorSerializedObject.Update();

            SerializedProperty statesProp = inspectorSerializedObject.FindProperty("states");
            if (statesProp == null || inspectedStateIndex < 0 || inspectedStateIndex >= statesProp.arraySize)
            {
                EditorGUILayout.HelpBox("Selected state is missing or out of range.", MessageType.Warning);
                return;
            }

            SerializedProperty stateProp = statesProp.GetArrayElementAtIndex(inspectedStateIndex);
            SerializedProperty stateNameProp = stateProp.FindPropertyRelative("stateName");
            SerializedProperty actionsProp = stateProp.FindPropertyRelative("actions");
            SerializedProperty conditionsProp = stateProp.FindPropertyRelative("conditions");

            DrawEntityComponentsHeader(entityLogic);

            GUILayout.Space(6);

            bool renameRequested = DrawStateNameField(stateNameProp);

            GUILayout.Space(6);

            DrawActionsList(actionsProp);

            GUILayout.Space(6);

            if (conditionsProp != null)
            {
                EditorGUILayout.PropertyField(conditionsProp, new GUIContent("Condition Stacks"), true);
            }

            bool changed = inspectorSerializedObject.ApplyModifiedProperties();

            if (renameRequested || changed)
            {
                EditorUtility.SetDirty(entityLogic);
                RequestInspectorChanged();
            }
        }

        private void DrawEntityComponentsHeader(EntityLogic logic)
        {
            EntityComponent[] components = logic != null ? logic.EntityComponents : null;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Entity Components (Owner)", EditorStyles.boldLabel);

            if (components == null || components.Length == 0)
            {
                EditorGUILayout.HelpBox("No EntityComponents found on the parent Entity.", MessageType.Info);
            }
            else
            {
                foreach (EntityComponent component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }

                    string displayName = EntityComponentDropdownUtilities.GetDisplayName(component);
                    string scriptName = ObjectNames.NicifyVariableName(component.GetType().Name);
                    EditorGUILayout.LabelField($"• {displayName} ({scriptName})", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private bool DrawStateNameField(SerializedProperty stateNameProp)
        {
            if (stateNameProp == null)
            {
                return false;
            }

            EditorGUI.BeginChangeCheck();
            string currentName = stateNameProp.stringValue;
            string newName = EditorGUILayout.DelayedTextField("Name", currentName);
            if (!EditorGUI.EndChangeCheck() || newName == currentName)
            {
                return false;
            }

            if (EditorUtility.DisplayDialog("Rename State",
                    $"Are you sure you want to rename '{currentName}' to '{newName}'?", "Yes", "No"))
            {
                stateNameProp.stringValue = newName;
                inspectorSerializedObject.ApplyModifiedProperties();
                entityLogic.RenameExitState(currentName, newName);
                return true;
            }

            return false;
        }

        private void DrawActionsList(SerializedProperty actionsProp)
        {
            if (actionsProp == null)
            {
                EditorGUILayout.HelpBox("Actions property not found.", MessageType.Warning);
                return;
            }

            if (actionsList == null || actionsList.serializedProperty.propertyPath != actionsProp.propertyPath)
            {
                actionsList = CreateActionsList(actionsProp);
            }

            actionsList.DoLayoutList();
        }

        private ReorderableList CreateActionsList(SerializedProperty actionsProp)
        {
            var list = new ReorderableList(actionsProp.serializedObject, actionsProp, true, true, true, true);

            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Actions");
            };

            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = actionsProp.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                EntityAction currentAction = element.objectReferenceValue as EntityAction;
                IReadOnlyList<EntityAction> availableActions = EntityComponentDropdownUtilities.GetComponents<EntityAction>(entityLogic);
                EntityAction selected = EntityComponentDropdownUtilities.DrawComponentPopup(rect, null, availableActions, currentAction);
                if (selected != currentAction)
                {
                    element.objectReferenceValue = selected;
                }
            };

            list.elementHeight = EditorGUIUtility.singleLineHeight + 4;
            list.onAddCallback = reorderableList =>
            {
                int newIndex = reorderableList.serializedProperty.arraySize;
                reorderableList.serializedProperty.arraySize++;
                SerializedProperty newElement = reorderableList.serializedProperty.GetArrayElementAtIndex(newIndex);
                newElement.objectReferenceValue = null;
            };

            return list;
        }
        
        #endregion
        
        #region Logic Management
        
        public void SetEntityLogic(EntityLogic logic)
        {
            entityLogic = logic;
        }
        
        public void SetRefreshCallback(System.Action callback)
        {
            onRefreshRequested = callback;
        }

        public void SetInspectorChangedCallback(System.Action<int> callback)
        {
            onInspectorChanged = callback;
        }
        
        private void RequestRefresh()
        {
            onRefreshRequested?.Invoke();
        }

        private void RequestInspectorChanged()
        {
            onInspectorChanged?.Invoke(inspectedStateIndex);
        }
        
        #endregion
        
        #region Cleanup
        
        public void Cleanup()
        {
            UnsubscribeFromEvents();
            inspectorPanel = null;
            inspectorHeader = null;
            inspectorContent = null;
            selectedStateNode = null;
            splitView = null;
            entityLogic = null;
            inspectorSerializedObject = null;
            actionsList = null;
            inspectedStateIndex = -1;
        }
        
        #endregion
    }
}
