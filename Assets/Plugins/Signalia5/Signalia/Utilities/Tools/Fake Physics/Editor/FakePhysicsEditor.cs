using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.Utilities.Editors
{
	/// <summary>
	/// Custom Editor for FakePhysics component. Provides a clean inspector interface
	/// for configuring the fake physics settings on kinematic scene objects.
	/// </summary>
	[CustomEditor(typeof(FakePhysics))]
	[CanEditMultipleObjects]
	public class FakePhysicsEditor : Editor
	{
		// Physics Settings
		private SerializedProperty isEnabledProp;
		private SerializedProperty useGlobalGravityProp;
		private SerializedProperty customGravityProp;
		private SerializedProperty gravityScaleProp;

		// Collision Settings
		private SerializedProperty collisionMaskProp;
		private SerializedProperty triggerInteractionProp;
		private SerializedProperty skinWidthProp;

		// Response Settings
		private SerializedProperty bouncinessProp;
		private SerializedProperty dragProp;
		private SerializedProperty frictionProp;
		private SerializedProperty sleepThresholdProp;
		private SerializedProperty maxVelocityProp;

		// Events
		private SerializedProperty onCollisionProp;
		private SerializedProperty onSleepProp;
		private SerializedProperty onWakeProp;

		// Debug
		private SerializedProperty showDebugProp;

		// Tab state
		private readonly string[] tabs = { "Gravity", "Collision", "Response", "Events", "Debug", "Runtime" };
		private int selectedTab;

		private void OnEnable()
		{
			// Physics Settings
			isEnabledProp = serializedObject.FindProperty("isEnabled");
			useGlobalGravityProp = serializedObject.FindProperty("useGlobalGravity");
			customGravityProp = serializedObject.FindProperty("customGravity");
			gravityScaleProp = serializedObject.FindProperty("gravityScale");

			// Collision Settings
			collisionMaskProp = serializedObject.FindProperty("collisionMask");
			triggerInteractionProp = serializedObject.FindProperty("triggerInteraction");
			skinWidthProp = serializedObject.FindProperty("skinWidth");

			// Response Settings
			bouncinessProp = serializedObject.FindProperty("bounciness");
			dragProp = serializedObject.FindProperty("drag");
			frictionProp = serializedObject.FindProperty("friction");
			sleepThresholdProp = serializedObject.FindProperty("sleepThreshold");
			maxVelocityProp = serializedObject.FindProperty("maxVelocity");

			// Events
			onCollisionProp = serializedObject.FindProperty("onCollision");
			onSleepProp = serializedObject.FindProperty("onSleep");
			onWakeProp = serializedObject.FindProperty("onWake");

			// Debug
			showDebugProp = serializedObject.FindProperty("showDebug");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			// Header image (commented out for now)
			// Texture2D headerImage = GraphicLoader.FakePhysicsHeader;
			// if (headerImage != null)
			// {
			//     GUILayout.Label(headerImage, GUILayout.Height(120));
			// }
			// else
			// {
			//     EditorGUILayout.HelpBox("Fake Physics - Simplified physics for kinematic scene objects", MessageType.Info);
			// }
			// EditorGUILayout.Space(10);

			// Enable toggle at the top
			EditorGUILayout.PropertyField(isEnabledProp, new GUIContent("Enabled", "Enable or disable the physics simulation"));

			EditorGUILayout.Space(5);

			// Tab selection
			GUI.backgroundColor = Color.gray;
			selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(24));
			GUI.backgroundColor = Color.white;
			EditorGUILayout.Space(5);

			// Draw content based on selected tab
			switch (selectedTab)
			{
				case 0:
					DrawGravityTab();
					break;
				case 1:
					DrawCollisionTab();
					break;
				case 2:
					DrawResponseTab();
					break;
				case 3:
					DrawEventsTab();
					break;
				case 4:
					DrawDebugTab();
					break;
				case 5:
					DrawRuntimeTab();
					break;
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawGravityTab()
		{
			EditorGUILayout.PropertyField(useGlobalGravityProp, new GUIContent("Use Global Gravity", "Use Unity's Physics.gravity"));
			
			if (!useGlobalGravityProp.boolValue)
			{
				EditorGUILayout.PropertyField(customGravityProp, new GUIContent("Custom Gravity", "Custom gravity vector to apply"));
			}
			else
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.Vector3Field("Global Gravity", Physics.gravity);
				EditorGUI.EndDisabledGroup();
			}
			
			EditorGUILayout.PropertyField(gravityScaleProp, new GUIContent("Gravity Scale", "Multiplier applied to gravity"));
		}

		private void DrawCollisionTab()
		{
			EditorGUILayout.PropertyField(collisionMaskProp, new GUIContent("Collision Mask", "Layers this object will collide with"));
			EditorGUILayout.PropertyField(triggerInteractionProp, new GUIContent("Trigger Interaction", "How to handle trigger colliders"));
			EditorGUILayout.PropertyField(skinWidthProp, new GUIContent("Skin Width", "Small offset to prevent surface penetration"));
		}

		private void DrawResponseTab()
		{
			EditorGUILayout.PropertyField(bouncinessProp, new GUIContent("Bounciness", "How much the object bounces (0 = none, 1 = full)"));
			EditorGUILayout.PropertyField(dragProp, new GUIContent("Drag", "Air resistance - slows the object over time"));
			EditorGUILayout.PropertyField(frictionProp, new GUIContent("Friction", "Surface friction applied on collision"));
			EditorGUILayout.PropertyField(sleepThresholdProp, new GUIContent("Sleep Threshold", "Velocity below which object is considered at rest"));
			EditorGUILayout.PropertyField(maxVelocityProp, new GUIContent("Max Velocity", "Maximum velocity magnitude (0 = unlimited)"));
		}

		private void DrawEventsTab()
		{
			EditorGUILayout.PropertyField(onCollisionProp, new GUIContent("On Collision", "Called when the object collides"));
			EditorGUILayout.PropertyField(onSleepProp, new GUIContent("On Sleep", "Called when object comes to rest"));
			EditorGUILayout.PropertyField(onWakeProp, new GUIContent("On Wake", "Called when object starts moving"));
		}

		private void DrawDebugTab()
		{
			EditorGUILayout.PropertyField(showDebugProp, new GUIContent("Show Debug Gizmos", "Draw velocity and gravity gizmos"));
		}

		private void DrawRuntimeTab()
		{
			if (!Application.isPlaying)
			{
				EditorGUILayout.HelpBox("Runtime controls are only available during Play Mode.", MessageType.Info);
				return;
			}

			FakePhysics fakePhysics = (FakePhysics)target;

			// Status display
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Status:", GUILayout.Width(60));
			
			GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
			if (fakePhysics.IsAsleep)
			{
				statusStyle.normal.textColor = Color.gray;
				EditorGUILayout.LabelField("Asleep", statusStyle);
			}
			else
			{
				statusStyle.normal.textColor = Color.green;
				EditorGUILayout.LabelField("Active", statusStyle);
			}
			EditorGUILayout.EndHorizontal();

			// Velocity display
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.Vector3Field("Current Velocity", fakePhysics.Velocity);
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space(5);

			// Control buttons
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Wake"))
			{
				fakePhysics.Wake();
			}
			if (GUILayout.Button("Sleep"))
			{
				fakePhysics.Sleep();
			}
			if (GUILayout.Button("Reset"))
			{
				fakePhysics.Reset();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(5);

			// Quick impulse buttons
			EditorGUILayout.LabelField("Apply Impulse", EditorStyles.miniLabel);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("↑ Up"))
			{
				fakePhysics.AddForce(Vector3.up * 5f);
			}
			if (GUILayout.Button("↓ Down"))
			{
				fakePhysics.AddForce(Vector3.down * 5f);
			}
			if (GUILayout.Button("→ Forward"))
			{
				fakePhysics.AddForce(fakePhysics.transform.forward * 5f);
			}
			if (GUILayout.Button("Random"))
			{
				fakePhysics.AddForce(Random.insideUnitSphere * 5f);
			}
			EditorGUILayout.EndHorizontal();

			// Force repaint for live updates
			Repaint();
		}
	}
}
