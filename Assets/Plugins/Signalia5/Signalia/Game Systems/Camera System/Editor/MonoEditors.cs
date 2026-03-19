#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.CameraSystem;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.CameraSystem.Editors
{
    [CustomEditor(typeof(TargetCamera3D)), CanEditMultipleObjects]
    public class TargetCamera3DEditor : Editor
    {
        private SerializedProperty driveModeProp;
        private SerializedProperty driverTransformProp;
        private SerializedProperty driverTransformDeadKeyProp;
        private SerializedProperty cameraModeProp;
        private SerializedProperty targetProp;
        private SerializedProperty targetDeadKeyProp;
        private SerializedProperty targetOffsetProp;
        private SerializedProperty firstPersonMouseSensitivityProp;
        private SerializedProperty firstPersonMouseDampingProp;
        private SerializedProperty firstPersonVerticalClampMinProp;
        private SerializedProperty firstPersonVerticalClampMaxProp;
        private SerializedProperty thirdPersonDistanceProp;
        private SerializedProperty thirdPersonHeightProp;
        private SerializedProperty thirdPersonOffsetProp;
        private SerializedProperty thirdPersonMouseSensitivityProp;
        private SerializedProperty thirdPersonMouseDampingProp;
        private SerializedProperty thirdPersonVerticalClampMinProp;
        private SerializedProperty thirdPersonVerticalClampMaxProp;
        private SerializedProperty thirdPersonSmoothingProp;
        private SerializedProperty thirdPersonRotationSmoothingProp;
        private SerializedProperty rotateCharacterTowardMovementProp;
        private SerializedProperty characterRotationSpeedProp;
        private SerializedProperty cameraCollisionLayerMaskProp;
        private SerializedProperty cameraCollisionRadiusProp;
        private SerializedProperty minCameraDistanceProp;
        private SerializedProperty lookActionNameProp;
        private SerializedProperty moveActionNameProp;
        private SerializedProperty scrollActionNameProp;
        private SerializedProperty scrollSensitivityProp;
        private SerializedProperty minScrollDistanceProp;
        private SerializedProperty maxScrollDistanceProp;
        private SerializedProperty showDebugGizmosProp;

        private int selectedTab;
        private readonly string[] tabs = { "Mode & Target", "First Person", "Third Person", "Collision", "Input", "Debug" };

        private void OnEnable()
        {
            driveModeProp = serializedObject.FindProperty("driveMode");
            driverTransformProp = serializedObject.FindProperty("driverTransform");
            driverTransformDeadKeyProp = serializedObject.FindProperty("driverTransformDeadKey");
            cameraModeProp = serializedObject.FindProperty("cameraMode");
            targetProp = serializedObject.FindProperty("target");
            targetDeadKeyProp = serializedObject.FindProperty("targetDeadKey");
            targetOffsetProp = serializedObject.FindProperty("targetOffset");
            firstPersonMouseSensitivityProp = serializedObject.FindProperty("firstPersonMouseSensitivity");
            firstPersonMouseDampingProp = serializedObject.FindProperty("firstPersonMouseDamping");
            firstPersonVerticalClampMinProp = serializedObject.FindProperty("firstPersonVerticalClampMin");
            firstPersonVerticalClampMaxProp = serializedObject.FindProperty("firstPersonVerticalClampMax");
            thirdPersonDistanceProp = serializedObject.FindProperty("thirdPersonDistance");
            thirdPersonHeightProp = serializedObject.FindProperty("thirdPersonHeight");
            thirdPersonOffsetProp = serializedObject.FindProperty("thirdPersonOffset");
            thirdPersonMouseSensitivityProp = serializedObject.FindProperty("thirdPersonMouseSensitivity");
            thirdPersonMouseDampingProp = serializedObject.FindProperty("thirdPersonMouseDamping");
            thirdPersonVerticalClampMinProp = serializedObject.FindProperty("thirdPersonVerticalClampMin");
            thirdPersonVerticalClampMaxProp = serializedObject.FindProperty("thirdPersonVerticalClampMax");
            thirdPersonSmoothingProp = serializedObject.FindProperty("thirdPersonSmoothing");
            thirdPersonRotationSmoothingProp = serializedObject.FindProperty("thirdPersonRotationSmoothing");
            rotateCharacterTowardMovementProp = serializedObject.FindProperty("rotateCharacterTowardMovement");
            characterRotationSpeedProp = serializedObject.FindProperty("characterRotationSpeed");
            cameraCollisionLayerMaskProp = serializedObject.FindProperty("cameraCollisionLayerMask");
            cameraCollisionRadiusProp = serializedObject.FindProperty("cameraCollisionRadius");
            minCameraDistanceProp = serializedObject.FindProperty("minCameraDistance");
            lookActionNameProp = serializedObject.FindProperty("lookActionName");
            moveActionNameProp = serializedObject.FindProperty("moveActionName");
            scrollActionNameProp = serializedObject.FindProperty("scrollActionName");
            scrollSensitivityProp = serializedObject.FindProperty("scrollSensitivity");
            minScrollDistanceProp = serializedObject.FindProperty("minScrollDistance");
            maxScrollDistanceProp = serializedObject.FindProperty("maxScrollDistance");
            showDebugGizmosProp = serializedObject.FindProperty("showDebugGizmos");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox("Professional camera controller for 3D characters with first-person and third-person modes. Features adaptive collision detection, smooth camera movement, and configurable input handling. Supports both direct camera control and transform driving for Cinemachine integration. Target can be assigned directly or resolved from a DeadKey.", MessageType.Info);

            GUILayout.Space(6);
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawModeAndTargetTab();
                    break;
                case 1:
                    DrawFirstPersonTab();
                    break;
                case 2:
                    DrawThirdPersonTab();
                    break;
                case 3:
                    DrawCollisionTab();
                    break;
                case 4:
                    DrawInputTab();
                    break;
                case 5:
                    DrawDebugTab();
                    break;
            }

            GUILayout.Space(8);
            DrawDiagnostics();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawModeAndTargetTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Drive Mode", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(driveModeProp, new GUIContent("Drive Mode", "How the camera system drives the camera. Direct Camera manipulates the main camera directly. Transform Driver updates a transform (useful for Cinemachine virtual cameras)."));
            
            if (driveModeProp.enumValueIndex == 0)
            {
                EditorGUILayout.HelpBox("Direct Camera: The system will directly manipulate the main camera in the scene.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Transform Driver: The system will drive a transform instead of the camera directly. Attach a Cinemachine virtual camera to the driver transform to use Cinemachine features.", MessageType.Info);
                
                EditorGUILayout.PropertyField(driverTransformProp, new GUIContent("Driver Transform", "Transform to drive when in Transform Driver mode. If null, will try DeadKey or use this GameObject's transform."));
                
                EditorGUILayout.PropertyField(driverTransformDeadKeyProp, new GUIContent("Driver Transform DeadKey", "DeadKey name to find the driver transform. If driverTransform is null and this is set, will try to retrieve the transform from DeadKey system."));
                
                if (driverTransformProp.objectReferenceValue == null && string.IsNullOrEmpty(driverTransformDeadKeyProp.stringValue))
                {
                    EditorGUILayout.HelpBox("Driver Transform is not assigned and no DeadKey is set. The system will use this GameObject's transform by default.", MessageType.Info);
                }
                else if (driverTransformProp.objectReferenceValue == null && !string.IsNullOrEmpty(driverTransformDeadKeyProp.stringValue))
                {
                    EditorGUILayout.HelpBox($"Driver Transform will be resolved from DeadKey '{driverTransformDeadKeyProp.stringValue}' at runtime. Make sure the DeadKey is registered before this component initializes.", MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Camera Mode", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cameraModeProp, new GUIContent("Mode", "Camera mode: First Person or Third Person."));
            
            if (cameraModeProp.enumValueIndex == 0)
            {
                EditorGUILayout.HelpBox("First Person: Camera rotates with Look input. Character Y rotation follows camera.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Third Person: Camera orbits around target. Character movement uses Camera.main for direction.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Target Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetProp, new GUIContent("Target", "The transform the camera should follow. If null, will try to resolve from Target DeadKey at runtime."));
            EditorGUILayout.PropertyField(targetDeadKeyProp, new GUIContent("Target DeadKey", "DeadKey name to find the target transform. If target is null and this is set, will try to retrieve the transform from DeadKey system at runtime."));
            
            if (targetProp.objectReferenceValue == null && string.IsNullOrEmpty(targetDeadKeyProp.stringValue))
            {
                EditorGUILayout.HelpBox("Target is not assigned and no Target DeadKey is set. Assign a target or set a DeadKey that provides the character's transform at runtime.", MessageType.Info);
            }
            else if (targetProp.objectReferenceValue == null && !string.IsNullOrEmpty(targetDeadKeyProp.stringValue))
            {
                EditorGUILayout.HelpBox($"Target will be resolved from DeadKey '{targetDeadKeyProp.stringValue}' at runtime. Ensure the DeadKey is registered before this component initializes.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(targetOffsetProp, new GUIContent("Target Offset", "Offset from target position for camera calculations."));
            EditorGUILayout.EndVertical();
        }

        private void DrawFirstPersonTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("First Person Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(firstPersonMouseSensitivityProp, new GUIContent("Mouse Sensitivity", "How fast the camera rotates with mouse input. Higher values = faster rotation."));
            
            if (firstPersonMouseSensitivityProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Mouse sensitivity must be greater than 0.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(firstPersonMouseDampingProp, new GUIContent("Mouse Damping", "How smoothly the camera responds to mouse input. Higher values = more responsive, lower values = smoother. Separate from sensitivity."));
            
            if (firstPersonMouseDampingProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Mouse damping must be greater than 0.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(firstPersonVerticalClampMinProp, new GUIContent("Vertical Clamp Min", "Minimum vertical rotation angle in degrees (looking down). Negative values = looking down. E.g. -80 = 80 degrees down."));
            EditorGUILayout.PropertyField(firstPersonVerticalClampMaxProp, new GUIContent("Vertical Clamp Max", "Maximum vertical rotation angle in degrees (looking up). Positive values = looking up. E.g. 80 = 80 degrees up."));
            
            if (firstPersonVerticalClampMinProp.floatValue >= firstPersonVerticalClampMaxProp.floatValue)
            {
                EditorGUILayout.HelpBox("Vertical clamp min must be less than max.", MessageType.Warning);
            }
            else if (firstPersonVerticalClampMinProp.floatValue < -90f || firstPersonVerticalClampMaxProp.floatValue > 90f)
            {
                EditorGUILayout.HelpBox("Vertical clamp values outside -90 to 90 degrees may cause camera flipping.", MessageType.Warning);
            }

            EditorGUILayout.HelpBox("In first person mode, the character's Y rotation will follow the camera's horizontal rotation. The camera's vertical rotation is independent.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawThirdPersonTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Third Person Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(thirdPersonDistanceProp, new GUIContent("Distance", "Distance from target to camera in third person mode."));
            
            if (thirdPersonDistanceProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Distance must be greater than 0.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(thirdPersonHeightProp, new GUIContent("Height", "Additional height offset for camera position."));
            EditorGUILayout.PropertyField(thirdPersonOffsetProp, new GUIContent("Offset", "Additional offset from target position for camera calculations."));
            
            EditorGUILayout.PropertyField(thirdPersonMouseSensitivityProp, new GUIContent("Mouse Sensitivity", "How fast the camera orbits around the target with mouse input."));
            
            if (thirdPersonMouseSensitivityProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Mouse sensitivity must be greater than 0.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(thirdPersonMouseDampingProp, new GUIContent("Mouse Damping", "How smoothly the camera responds to mouse input. Higher values = more responsive, lower values = smoother. Separate from sensitivity."));
            
            if (thirdPersonMouseDampingProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Mouse damping must be greater than 0.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(thirdPersonVerticalClampMinProp, new GUIContent("Vertical Clamp Min", "Minimum vertical rotation angle in degrees (looking down). Negative values = looking down. E.g. -80 = 80 degrees down."));
            EditorGUILayout.PropertyField(thirdPersonVerticalClampMaxProp, new GUIContent("Vertical Clamp Max", "Maximum vertical rotation angle in degrees (looking up). Positive values = looking up. E.g. 80 = 80 degrees up."));
            
            if (thirdPersonVerticalClampMinProp.floatValue >= thirdPersonVerticalClampMaxProp.floatValue)
            {
                EditorGUILayout.HelpBox("Vertical clamp min must be less than max.", MessageType.Warning);
            }
            else if (thirdPersonVerticalClampMinProp.floatValue < -90f || thirdPersonVerticalClampMaxProp.floatValue > 90f)
            {
                EditorGUILayout.HelpBox("Vertical clamp values outside -90 to 90 degrees may cause unexpected behavior.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(thirdPersonSmoothingProp, new GUIContent("Position Smoothing", "How smoothly the camera moves to its target position. Higher values = faster movement."));
            
            if (thirdPersonSmoothingProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Smoothing must be greater than 0.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(thirdPersonRotationSmoothingProp, new GUIContent("Rotation Smoothing", "How smoothly the camera rotates. Higher values = faster rotation."));
            
            if (thirdPersonRotationSmoothingProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Rotation smoothing must be greater than 0.", MessageType.Warning);
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Character Rotation", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(rotateCharacterTowardMovementProp, new GUIContent("Rotate Toward Movement", "When enabled, the target transform will rotate to face the direction of movement input (relative to camera)."));
            
            if (rotateCharacterTowardMovementProp.boolValue)
            {
                EditorGUILayout.PropertyField(characterRotationSpeedProp, new GUIContent("Rotation Speed", "How fast the character rotates toward movement direction. Higher values = faster rotation."));
                
                if (characterRotationSpeedProp.floatValue <= 0f)
                {
                    EditorGUILayout.HelpBox("Character rotation speed must be greater than 0.", MessageType.Warning);
                }
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Scroll Control", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(scrollSensitivityProp, new GUIContent("Scroll Sensitivity", "How fast the camera distance changes when scrolling. Higher values = faster zoom."));
            
            if (scrollSensitivityProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Scroll sensitivity must be greater than 0.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(minScrollDistanceProp, new GUIContent("Min Scroll Distance", "Minimum camera distance when scrolling. Camera cannot zoom in closer than this."));
            
            if (minScrollDistanceProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Min scroll distance must be greater than 0.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(maxScrollDistanceProp, new GUIContent("Max Scroll Distance", "Maximum camera distance when scrolling. Camera cannot zoom out further than this."));
            
            if (maxScrollDistanceProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Max scroll distance must be greater than 0.", MessageType.Warning);
            }

            if (minScrollDistanceProp.floatValue >= maxScrollDistanceProp.floatValue)
            {
                EditorGUILayout.HelpBox("Min scroll distance is greater than or equal to max scroll distance. Scroll range is invalid.", MessageType.Warning);
            }

            if (thirdPersonDistanceProp.floatValue < minScrollDistanceProp.floatValue || thirdPersonDistanceProp.floatValue > maxScrollDistanceProp.floatValue)
            {
                EditorGUILayout.HelpBox("Current third person distance is outside the scroll range. It will be clamped at runtime.", MessageType.Info);
            }

            EditorGUILayout.HelpBox("In third person mode, the camera will automatically adjust its distance when colliding with walls. Use scroll input to zoom in/out.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawCollisionTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Camera Collision Detection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Collision detection is only used in third person mode. The camera will automatically move closer to the target when it detects obstacles.", MessageType.Info);
            
            EditorGUILayout.PropertyField(cameraCollisionLayerMaskProp, new GUIContent("Collision Layer Mask", "Physics layers that the camera should collide with. Use the layer mask selector to choose appropriate layers."));
            
            if (cameraCollisionLayerMaskProp.intValue == 0)
            {
                EditorGUILayout.HelpBox("Collision layer mask is set to 'Nothing'. Camera collision detection will not work.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(cameraCollisionRadiusProp, new GUIContent("Collision Radius", "Radius of the sphere used for camera collision detection. Larger values detect collisions earlier."));
            
            if (cameraCollisionRadiusProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Collision radius must be greater than 0.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(minCameraDistanceProp, new GUIContent("Min Distance", "Minimum distance the camera can get to the target. Prevents camera from getting too close."));
            
            if (minCameraDistanceProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Min distance must be greater than 0.", MessageType.Warning);
            }

            if (minCameraDistanceProp.floatValue >= thirdPersonDistanceProp.floatValue)
            {
                EditorGUILayout.HelpBox("Min distance is greater than or equal to third person distance. Camera may not be able to reach its desired position.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInputTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Input Action Names", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These action names must match the input actions configured in your Signalia Input Action Maps.", MessageType.Info);
            
            EditorGUILayout.PropertyField(lookActionNameProp, new GUIContent("Look Action", "Input action name for camera look/rotation. Should return a Vector2 value (e.g., \"Look\")."));
            
            if (string.IsNullOrEmpty(lookActionNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Look action name is required. This should match an action in your Input Action Maps that returns Vector2.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(moveActionNameProp, new GUIContent("Move Action", "Input action name for character movement. Should return a Vector2 value (e.g., \"Move\"). Used for character rotation in third-person mode."));
            
            if (string.IsNullOrEmpty(moveActionNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Move action name is required for character rotation in third-person mode. This should match an action in your Input Action Maps that returns Vector2.", MessageType.Info);
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Scroll Action", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(scrollActionNameProp, new GUIContent("Scroll Action", "Input action name for scroll wheel. Should return a float value (e.g., \"Scroll\"). Positive values zoom out, negative values zoom in."));
            
            if (string.IsNullOrEmpty(scrollActionNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Scroll action name is recommended for scroll control in third-person mode. This should match an action in your Input Action Maps that returns float.", MessageType.Info);
            }

            EditorGUILayout.HelpBox("Scroll action is only used in third-person mode. Configure your input wrapper to pass scroll wheel input as a float (positive for scroll up, negative for scroll down).", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawDebugTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Debug Visualization", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(showDebugGizmosProp, new GUIContent("Show Debug Gizmos", "Display camera collision and positioning gizmos in the Scene view for debugging."));
            
            if (showDebugGizmosProp.boolValue)
            {
                EditorGUILayout.HelpBox("Debug gizmos will be visible in the Scene view:\n• Cyan sphere = Current camera collision sphere\n• Yellow line = Line from target to camera\n• Green sphere = Desired camera position (third person only)", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

                foreach (Object targetObject in targets)
                {
                    if (targetObject is not TargetCamera3D camera)
                    {
                        continue;
                    }

                    if (camera.CurrentDriveMode == TargetCamera3D.DriveMode.DirectCamera)
                    {
                        if (Camera.main == null)
                        {
                            EditorGUILayout.HelpBox($"No main camera found in scene. The camera controller will try to find any camera at runtime, but it's recommended to have a camera tagged as 'MainCamera'.", MessageType.Warning);
                            break;
                        }
                    }
                    else
                    {
                        Transform driverTransform = camera.DriverTransform != null ? camera.DriverTransform : camera.transform;
                        EditorGUILayout.HelpBox($"Transform Driver mode: Camera will drive '{driverTransform.name}'. Attach a Cinemachine virtual camera to this transform.", MessageType.Info);
                    }

                    if (camera.Target == null)
                    {
                        EditorGUILayout.HelpBox($"{camera.name} has no target assigned. Assign a target or set Target DeadKey to resolve at runtime.", MessageType.Info);
                        break;
                    }

                    if (camera.Mode == TargetCamera3D.CameraMode.ThirdPerson)
                    {
                        if (cameraCollisionLayerMaskProp.intValue == 0)
                        {
                            EditorGUILayout.HelpBox($"{camera.name} is in third person mode but has no collision layers configured. Camera may clip through walls.", MessageType.Warning);
                            break;
                        }
                    }
                }
            }
        }
    }

}
#endif

