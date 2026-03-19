using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.Utilities;
using DG.DOTweenEditor;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Signalia.Utilities.Animation;

namespace Signalia.Utilities.Animation.Editors
{
    /// <summary>
    /// Custom Editor for the Animatable component.
    /// </summary>
    [CustomEditor(typeof(Animatable)), CanEditMultipleObjects]
    public class AnimatableEditor : Editor
    {
        private SerializedProperty animationArray;
        private int animationIndex = 0;
        private string animationLabel = "";

        private GUIContent playIcon;
        private GUIContent cancelIcon;

        private void OnEnable()
        {
            animationArray = serializedObject.FindProperty("animationArray");

            playIcon = EditorGUIUtility.IconContent("d_PlayButton");
            cancelIcon = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header image - commented out for now, graphics defined in GraphicLoader.AnimatableHeader
            // Texture2D headerImage = GraphicLoader.AnimatableHeader;
            // if (headerImage != null)
            // {
            //     GUILayout.Label(headerImage, GUILayout.Height(120));
            // }
            
            if (Application.isPlaying)
                DrawActionsSectionPlayMode();

            if (!Application.isPlaying)
                DrawActionsSectionEditorMode();

            DrawMainSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawActionsSectionPlayMode()
        {
            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.9f, 1f, 0.9f);
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
            GUI.backgroundColor = new Color(0.9f, 0.95f, 1f);
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
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Animation List", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(animationArray, new GUIContent("Animations"), true);
        }

        // Play Mode Methods
        private void PlayFirstAnimation()
        {
            Animatable animatable = (Animatable)target;
            animatable.PlayFirstAnimation();
            Debug.Log($"🎬 Played First Animation!");
        }

        private void PlayAnimationFromIndex(int index)
        {
            Animatable animatable = (Animatable)target;

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
            Animatable animatable = (Animatable)target;

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
            Animatable animatable = (Animatable)target;

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
            Animatable animatable = (Animatable)target;

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

        // Editor Preview Methods
        private void PreviewFirstAnimation()
        {
            Animatable animatable = (Animatable)target;
            if (animatable.AnimationArray.Length > 0)
            {
                GameObjectAnimationEditorPreview.Start(animatable.AnimationArray[0].AnimationAsset, animatable.gameObject);
            }
        }

        private void PreviewAnimationFromIndex(int index)
        {
            Animatable animatable = (Animatable)target;
            if (index >= 0 && index < animatable.AnimationArray.Length)
            {
                GameObjectAnimationEditorPreview.Start(animatable.AnimationArray[index].AnimationAsset, animatable.gameObject);
            }
            else
            {
                Debug.LogWarning($"⚠ Invalid Animation Index {index}!");
            }
        }

        private void PreviewAnimation(string label)
        {
            Animatable animatable = (Animatable)target;
            foreach (var animation in animatable.AnimationArray)
            {
                if (animation.Label == label)
                {
                    GameObjectAnimationEditorPreview.Start(animation.AnimationAsset, animatable.gameObject);
                    return;
                }
            }
            Debug.LogWarning($"⚠ Animation Label '{label}' not found!");
        }

        private void PreviewCancelAnimation(int index)
        {
            Animatable animatable = (Animatable)target;
            if (index >= 0 && index < animatable.AnimationArray.Length)
            {
                var targetGo = animatable.GetAnimatableTarget(index);
                DOTweenEditorPreview.Stop(true, true);
                animatable.AnimationArray[index].AnimationAsset?.StopPreview(targetGo);
                animatable.AnimationArray[index].AnimationAsset?.StopAnimations();
            }
            else
            {
                Debug.LogWarning($"⚠ Invalid Animation Index {index}!");
            }
        }

        private void PreviewCancelAnimation(string label)
        {
            Animatable animatable = (Animatable)target;
            var targetGo = animatable.GetAnimatableTarget(label);
            var animation = animatable.AnimationArray.FirstOrDefault(x => x.Label == label);
            if (animation != null)
            {
                DOTweenEditorPreview.Stop(true, true);
                animation.AnimationAsset?.StopPreview(targetGo);
                animation.AnimationAsset?.StopAnimations();
            }
            else
            {
                Debug.LogWarning($"⚠ Animation Label '{label}' not found!");
            }
        }
    }

    /// <summary>
    /// Custom Editor for the AnimationAsset ScriptableObject.
    /// </summary>
    [CustomEditor(typeof(AnimationAsset)), CanEditMultipleObjects]
    public class AnimationAssetEditor : Editor
    {
        private GameObject previewTarget;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AnimationAsset animationAsset = (AnimationAsset)target;

            GUILayout.Space(5);

            // Header image - commented out for now, graphics defined in GraphicLoader.AnimationAssetHeader
            // Texture2D headerImage = GraphicLoader.AnimationAssetHeader;
            // if (headerImage != null)
            // {
            //     GUILayout.Label(headerImage, GUILayout.Height(120));
            // }

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
                        GameObjectAnimationEditorPreview.Start(animationAsset, previewTarget);
                    else
                        Debug.LogWarning("Please assign a target GameObject to preview.");
                }

                GUIContent stopIcon = EditorGUIUtility.IconContent("d_PreMatQuad");
                if (GUILayout.Button(new GUIContent(" Stop Animations", stopIcon.image), GUILayout.Height(30)))
                {
                    if (previewTarget != null)
                    {
                        DOTweenEditorPreview.Stop(true, true);
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
            EditorGUILayout.HelpBox("Unscaled time: Animations will not be affected by time scale.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unscaledTime"), new GUIContent("Unscaled Time"));
            EditorGUILayout.HelpBox("Disable Frags: The Frags system tries to retain elements' original positions so they don't spaz out on spamming.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("disableFrags"), new GUIContent("Disable Frags"));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Add", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scale Up", GUILayout.Height(30))) animationAsset.Add_ScaleUp();
            if (GUILayout.Button("Scale Down", GUILayout.Height(30))) animationAsset.Add_ScaleDown();
            if (GUILayout.Button("Move Up", GUILayout.Height(30))) animationAsset.Add_MoveUp();
            if (GUILayout.Button("Bounce", GUILayout.Height(30))) animationAsset.Add_Bounce();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Punch Position", GUILayout.Height(30))) animationAsset.Add_PunchPosition();
            if (GUILayout.Button("Punch Rotation", GUILayout.Height(30))) animationAsset.Add_PunchRotation();
            if (GUILayout.Button("Punch Scale", GUILayout.Height(30))) animationAsset.Add_PunchScale();
            if (GUILayout.Button("Loop Rotation", GUILayout.Height(30))) animationAsset.Add_LoopRotation();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.HelpBox("All animations listed below will play together on the target GameObject.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animations"), new GUIContent("Animations"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Editor preview helper for GameObject animations.
    /// Similar to EditorPreviewInjector but for AnimationAsset.
    /// </summary>
    public static class GameObjectAnimationEditorPreview
    {
        public static AnimationAsset currentPreview;
        public static GameObject currentTarget;
        public static Tween currentTween;

        public static void Start(AnimationAsset animationAsset, GameObject target)
        {
            if (currentPreview != null && currentTarget != null)
            {
                DOTweenEditorPreview.Stop(true, true);
                currentPreview.StopPreview(currentTarget);
                currentPreview.StopAnimations();
            }

            currentTween?.Kill();

            if (animationAsset == null)
            {
                Debug.LogWarning("AnimationAsset is null.");
                return;
            }

            if (target == null)
            {
                Debug.LogWarning("Target is null.");
                return;
            }

            currentPreview = animationAsset;
            currentTarget = target;

            var preview = Create(target, animationAsset);
            animationAsset.PreviewAnimation(target, preview);
        }

        public static AnimationAsset.EditorPreview Create(GameObject previewTarget, AnimationAsset animationAsset)
        {
            return (tween) =>
            {
                if (tween == null)
                {
                    Debug.LogWarning("Tween is null.");
                    return;
                }

                currentTween = tween;

                if (tween.Loops() >= 0)
                {
                    tween.OnComplete(() =>
                    {
                        DOTweenEditorPreview.Stop(true, true);
                        animationAsset.StopPreview(previewTarget);
                        animationAsset.StopAnimations();

                        if (currentPreview == animationAsset && currentTarget == previewTarget)
                        {
                            currentPreview = null;
                            currentTarget = null;
                            currentTween = null;
                        }
                    });
                }

                DOTweenEditorPreview.PrepareTweenForPreview(tween, false, true, true);
                DOTweenEditorPreview.Start();
            };
        }
    }
}
