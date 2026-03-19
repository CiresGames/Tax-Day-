using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.DialogueSystem;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem.Editors
{
    /// <summary>
    /// Handles JSON serialization and deserialization of DialogueObject instances.
    /// Used for importing/exporting dialogue data to/from external applications.
    /// </summary>
    public static class DialogueObjectParsing
    {
        #region JSON Data Transfer Objects

        [Serializable]
        private class DialogueObjectJson
        {
            public string dialogueName;
            public string dialogueContext;
            public string startEvent;
            public string endEvent;
            public LineJson[] lines;
            public string exitDialogueName;
        }

        [Serializable]
        private class LineJson
        {
            public string branchName;
            public string speakerName;
            public string lineEvent;
            public string lineType; // "Speech" or "Choice"
            public string exitBranch;
            public SpeechJson speech;
            public ChoiceJson[] choices;
        }

        [Serializable]
        private class SpeechJson
        {
            public string speechText;
            public string animationOverride;
            public string audioEvent;
            public string newSaveKey;
        }

        [Serializable]
        private class ChoiceJson
        {
            public string choiceText;
            public string choiceEvent;
            public string newSaveKey;
            public string branchToSwitchTo;
        }

        #endregion

        #region Reflection Helpers

        private static readonly FieldInfo DialogueNameField = typeof(DialogueBook).GetField("dialogueName", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo DialogueContextField = typeof(DialogueBook).GetField("dialogueContext", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo StartEventField = typeof(DialogueBook).GetField("startEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo EndEventField = typeof(DialogueBook).GetField("endEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo LinesField = typeof(DialogueBook).GetField("lines", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ExitDialogueField = typeof(DialogueBook).GetField("exitDialogue", BindingFlags.NonPublic | BindingFlags.Instance);

        #endregion

        #region Serialization (DialogueObject -> JSON)

        /// <summary>
        /// Serializes a DialogueObject to JSON string.
        /// </summary>
        /// <param name="dialogueBook">The DialogueObject to serialize</param>
        /// <param name="prettyPrint">Whether to format the JSON with indentation</param>
        /// <returns>JSON string representation of the DialogueObject</returns>
        public static string ToJson(DialogueBook dialogueBook, bool prettyPrint = true)
        {
            if (dialogueBook == null)
            {
                Debug.LogError("Cannot serialize null DialogueObject");
                return "{}";
            }

            var jsonData = new DialogueObjectJson
            {
                dialogueName = GetDialogueName(dialogueBook),
                dialogueContext = (string)DialogueContextField.GetValue(dialogueBook) ?? "",
                startEvent = (string)StartEventField.GetValue(dialogueBook) ?? "",
                endEvent = (string)EndEventField.GetValue(dialogueBook) ?? "",
                lines = SerializeLines((Line[])LinesField.GetValue(dialogueBook)),
                exitDialogueName = GetExitDialogueName(dialogueBook)
            };

            return JsonUtility.ToJson(jsonData, prettyPrint);
        }

        /// <summary>
        /// Serializes an array of DialogueObjects to JSON string.
        /// </summary>
        /// <param name="dialogueObjects">Array of DialogueObjects to serialize</param>
        /// <param name="prettyPrint">Whether to format the JSON with indentation</param>
        /// <returns>JSON string representation of the DialogueObjects array</returns>
        public static string ToJson(DialogueBook[] dialogueObjects, bool prettyPrint = true)
        {
            if (dialogueObjects == null || dialogueObjects.Length == 0)
            {
                return "[]";
            }

            var jsonDataArray = dialogueObjects
                .Where(d => d != null)
                .Select(d => new DialogueObjectJson
                {
                    dialogueName = GetDialogueName(d),
                    dialogueContext = (string)DialogueContextField.GetValue(d) ?? "",
                    startEvent = (string)StartEventField.GetValue(d) ?? "",
                    endEvent = (string)EndEventField.GetValue(d) ?? "",
                    lines = SerializeLines((Line[])LinesField.GetValue(d)),
                    exitDialogueName = GetExitDialogueName(d)
                })
                .ToArray();

            // Unity's JsonUtility doesn't handle arrays directly, so we wrap it
            var wrapper = new { dialogues = jsonDataArray };
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        private static LineJson[] SerializeLines(Line[] lines)
        {
            if (lines == null || lines.Length == 0)
                return Array.Empty<LineJson>();

            return lines.Select(line => new LineJson
            {
                branchName = line.BranchName ?? "default",
                speakerName = line.SpeakerName ?? "",
                lineEvent = line.LineEvent ?? "",
                lineType = line.LineType.ToString(),
                exitBranch = line.ExitBranch ?? "",
                speech = line.LineType == LineType.Speech ? SerializeSpeech(line.Speech) : null,
                choices = line.LineType == LineType.Choice ? SerializeChoices(line.Choices) : null
            }).ToArray();
        }

        private static SpeechJson SerializeSpeech(Speech speech)
        {
            if (speech == null)
                return null;

            return new SpeechJson
            {
                speechText = speech.speechText ?? "",
                animationOverride = speech.animationOverride ?? "",
                audioEvent = speech.audioEvent ?? "",
                newSaveKey = speech.newSaveKey ?? ""
            };
        }

        private static ChoiceJson[] SerializeChoices(List<Choice> choices)
        {
            if (choices == null || choices.Count == 0)
                return null;

            return choices.Select(choice => new ChoiceJson
            {
                choiceText = choice.choiceText ?? "",
                choiceEvent = choice.choiceEvent ?? "",
                newSaveKey = choice.newSaveKey ?? "",
                branchToSwitchTo = choice.branchToSwitchTo ?? ""
            }).ToArray();
        }

        private static string GetDialogueName(DialogueBook dialogueBook)
        {
            var name = (string)DialogueNameField.GetValue(dialogueBook);
            return !string.IsNullOrEmpty(name) ? name : (dialogueBook.name ?? "");
        }

        private static string GetExitDialogueName(DialogueBook dialogueBook)
        {
            var exitDialogue = (DialogueBook)ExitDialogueField.GetValue(dialogueBook);
            if (exitDialogue == null)
                return "";

            return GetDialogueName(exitDialogue);
        }

        #endregion

        #region Deserialization (JSON -> DialogueObject)

        /// <summary>
        /// Deserializes a JSON string to a DialogueObject.
        /// </summary>
        /// <param name="json">JSON string to deserialize</param>
        /// <param name="resolveExitDialogue">Optional function to resolve exitDialogueName to a DialogueObject. If null, exitDialogue will be left as null.</param>
        /// <returns>Deserialized DialogueObject, or null if deserialization fails</returns>
        public static DialogueBook FromJson(string json, Func<string, DialogueBook> resolveExitDialogue = null)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("Cannot deserialize empty JSON string");
                return null;
            }

            try
            {
                var jsonData = JsonUtility.FromJson<DialogueObjectJson>(json);
                return CreateDialogueObjectFromJson(jsonData, resolveExitDialogue);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize DialogueObject from JSON: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Deserializes a JSON string containing an array of DialogueObjects.
        /// </summary>
        /// <param name="json">JSON string containing array of DialogueObjects</param>
        /// <param name="resolveExitDialogue">Optional function to resolve exitDialogueName to a DialogueObject</param>
        /// <returns>Array of deserialized DialogueObjects</returns>
        public static DialogueBook[] FromJsonArray(string json, Func<string, DialogueBook> resolveExitDialogue = null)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("Cannot deserialize empty JSON string");
                return Array.Empty<DialogueBook>();
            }

            try
            {
                // Unity's JsonUtility doesn't handle arrays directly, so we need to parse the wrapper
                var wrapper = JsonUtility.FromJson<DialogueObjectArrayWrapper>(json);
                if (wrapper?.dialogues == null)
                {
                    Debug.LogError("JSON does not contain a valid dialogues array");
                    return Array.Empty<DialogueBook>();
                }

                return wrapper.dialogues
                    .Select(d => CreateDialogueObjectFromJson(d, resolveExitDialogue))
                    .Where(d => d != null)
                    .ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize DialogueObject array from JSON: {e.Message}\n{e.StackTrace}");
                return Array.Empty<DialogueBook>();
            }
        }

        [Serializable]
        private class DialogueObjectArrayWrapper
        {
            public DialogueObjectJson[] dialogues;
        }

        private static DialogueBook CreateDialogueObjectFromJson(DialogueObjectJson jsonData, Func<string, DialogueBook> resolveExitDialogue)
        {
            if (jsonData == null)
            {
                Debug.LogError("JSON data is null");
                return null;
            }

            var dialogueObject = ScriptableObject.CreateInstance<DialogueBook>();

            // Set fields using reflection
            DialogueNameField.SetValue(dialogueObject, jsonData.dialogueName ?? "");
            DialogueContextField.SetValue(dialogueObject, jsonData.dialogueContext ?? "");
            StartEventField.SetValue(dialogueObject, jsonData.startEvent ?? "");
            EndEventField.SetValue(dialogueObject, jsonData.endEvent ?? "");
            LinesField.SetValue(dialogueObject, DeserializeLines(jsonData.lines));

            // Resolve exit dialogue reference
            DialogueBook exitDialogue = null;
            if (!string.IsNullOrEmpty(jsonData.exitDialogueName) && resolveExitDialogue != null)
            {
                exitDialogue = resolveExitDialogue(jsonData.exitDialogueName);
                if (exitDialogue == null)
                {
                    Debug.LogWarning($"Could not resolve exit dialogue reference: {jsonData.exitDialogueName}");
                }
            }
            ExitDialogueField.SetValue(dialogueObject, exitDialogue);

            return dialogueObject;
        }

        private static Line[] DeserializeLines(LineJson[] linesJson)
        {
            if (linesJson == null || linesJson.Length == 0)
                return Array.Empty<Line>();

            return linesJson.Select(lineJson =>
            {
                var line = new Line
                {
                    BranchName = lineJson.branchName ?? "default",
                    SpeakerName = lineJson.speakerName ?? "",
                    LineEvent = lineJson.lineEvent ?? "",
                    ExitBranch = lineJson.exitBranch ?? ""
                };

                // Parse line type
                if (Enum.TryParse<LineType>(lineJson.lineType, out var lineType))
                {
                    line.LineType = lineType;
                }
                else
                {
                    Debug.LogWarning($"Invalid lineType '{lineJson.lineType}', defaulting to Speech");
                    line.LineType = LineType.Speech;
                }

                // Set speech or choices based on line type
                if (line.LineType == LineType.Speech)
                {
                    line.Speech = DeserializeSpeech(lineJson.speech);
                }
                else if (line.LineType == LineType.Choice)
                {
                    line.Choices = DeserializeChoices(lineJson.choices);
                }

                return line;
            }).ToArray();
        }

        private static Speech DeserializeSpeech(SpeechJson speechJson)
        {
            if (speechJson == null)
                return new Speech();

            return new Speech
            {
                speechText = speechJson.speechText ?? "",
                animationOverride = speechJson.animationOverride ?? "",
                audioEvent = speechJson.audioEvent ?? "",
                newSaveKey = speechJson.newSaveKey ?? ""
            };
        }

        private static List<Choice> DeserializeChoices(ChoiceJson[] choicesJson)
        {
            if (choicesJson == null || choicesJson.Length == 0)
                return new List<Choice>();

            return choicesJson.Select(choiceJson => new Choice
            {
                choiceText = choiceJson.choiceText ?? "",
                choiceEvent = choiceJson.choiceEvent ?? "",
                newSaveKey = choiceJson.newSaveKey ?? "",
                branchToSwitchTo = choiceJson.branchToSwitchTo ?? ""
            }).ToList();
        }

        #endregion

        #region Editor Helper Methods

#if UNITY_EDITOR
        /// <summary>
        /// Helper method to find a DialogueObject by name using AssetDatabase.
        /// Use this as the resolveExitDialogue parameter when deserializing in the editor.
        /// </summary>
        /// <param name="dialogueName">Name of the dialogue to find</param>
        /// <returns>DialogueObject with matching name, or null if not found</returns>
        public static DialogueBook FindDialogueByName(string dialogueName)
        {
            if (string.IsNullOrEmpty(dialogueName))
                return null;

            var guids = AssetDatabase.FindAssets($"t:{typeof(DialogueBook).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var dialogue = AssetDatabase.LoadAssetAtPath<DialogueBook>(path);
                if (dialogue != null && dialogue.DialogueName == dialogueName)
                {
                    return dialogue;
                }
            }

            return null;
        }

        /// <summary>
        /// Saves a DialogueObject to a JSON file in the project.
        /// </summary>
        /// <param name="dialogueBook">DialogueObject to save</param>
        /// <param name="filePath">Path relative to Assets folder (e.g., "MyFolder/dialogue.json")</param>
        /// <param name="prettyPrint">Whether to format the JSON with indentation</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SaveToFile(DialogueBook dialogueBook, string filePath, bool prettyPrint = true)
        {
            if (dialogueBook == null)
            {
                Debug.LogError("Cannot save null DialogueObject");
                return false;
            }

            if (!filePath.StartsWith("Assets/"))
            {
                filePath = "Assets/" + filePath;
            }

            try
            {
                var json = ToJson(dialogueBook, prettyPrint);
                System.IO.File.WriteAllText(filePath, json);
                AssetDatabase.Refresh();
                Debug.Log($"DialogueObject saved to {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save DialogueObject to file: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a DialogueObject from a JSON file in the project.
        /// </summary>
        /// <param name="filePath">Path relative to Assets folder (e.g., "MyFolder/dialogue.json")</param>
        /// <param name="resolveExitDialogue">Optional function to resolve exitDialogueName to a DialogueObject</param>
        /// <returns>Deserialized DialogueObject, or null if loading fails</returns>
        public static DialogueBook LoadFromFile(string filePath, Func<string, DialogueBook> resolveExitDialogue = null)
        {
            if (!filePath.StartsWith("Assets/"))
            {
                filePath = "Assets/" + filePath;
            }

            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogError($"File not found: {filePath}");
                return null;
            }

            try
            {
                var json = System.IO.File.ReadAllText(filePath);
                return FromJson(json, resolveExitDialogue ?? FindDialogueByName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load DialogueObject from file: {e.Message}");
                return null;
            }
        }
#endif

        #endregion
    }
}
