using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem
{
    /// <summary>
    /// An object containing details of dialogue.
    /// Imported from an external app and transcoded from the json to dialogue.
    /// Can be modified in the inspector as well, though not recommended.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Signalia/Game Systems/Dialogue/Dialogue Book")]
    [Icon("Assets/AHAKuo Creations/Signalia/Framework/Graphics/Icons/SIGS_EDITOR_ICON_DLGBOOK.png")]
    public class DialogueBook : ScriptableObject
    {
        public string DialogueName => dialogueName; // used to reference
        
        [SerializeField] private string dialogueName;
        [SerializeField] private string dialogueContext;
        [SerializeField] private string startEvent, endEvent;

        /// <summary>
        /// Lines are read from top to bottom using an accompanying index.
        /// </summary>
        [SerializeField] private Line[] lines;

        [SerializeField] private DialogueBook exitDialogue; // optional exit dialogue
        
        [SerializeField]
        [Tooltip("Reference to the external .dlgbook file (JSON format) - used to import dialogue data")]
        private UnityEngine.Object dlgbookFile;
        
        /// <summary>
        /// Gets or sets the reference to the external .dlgbook file.
        /// </summary>
        public UnityEngine.Object DlgbookFile
        {
            get => dlgbookFile;
            set => dlgbookFile = value;
        }
        
        /// <summary>
        /// Gets the number of lines in this dialogue.
        /// </summary>
        public int LineCount => lines != null ? lines.Length : 0;

        /// <summary>
        /// Converts this dialogue object to a plug struct. Keeps things simple.
        /// </summary>
        /// <returns></returns>
        public DialogueObjPlug ToPlug() => new(this, lines, startEvent, endEvent, exitDialogue);
        
        /// <summary>
        /// Begins this dialogue object. If style unreferenced. Will find the first style in the scene.
        /// </summary>
        /// <returns></returns>
        public void StartDialogue(string style = "")
        {
            DialogueManager.StartDialogue(this, style);
        }
        
        /// <summary>
        /// Logs the number of lines in the dialogue associated with this DialogueObject.
        /// Useful for debugging or monitoring dialogue content.
        /// </summary>
        public void SizeLog()
        {
            Debug.Log($"Dialogue {dialogueName} has {lines.Length} lines.");
        }
        
        /// <summary>
        /// Updates this asset by reading and deserializing the referenced .dlgbook file.
        /// This is the primary way to import dialogue data from an external editor.
        /// Serialization is fallible — unknown or missing fields are gracefully skipped.
        /// </summary>
        public void UpdateAssetFromFile()
        {
#if UNITY_EDITOR
            if (dlgbookFile == null)
            {
                Debug.LogError("[Signalia DialogueBook] No .dlgbook file reference set. Please assign a .dlgbook file first.");
                return;
            }
            
            string path = UnityEditor.AssetDatabase.GetAssetPath(dlgbookFile);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Signalia DialogueBook] Could not get path for referenced .dlgbook file.");
                return;
            }
            
            try
            {
                string json = System.IO.File.ReadAllText(path);
                LoadFromJson(json);
                Debug.Log($"[Signalia DialogueBook] Updated asset from file: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Signalia DialogueBook] Error reading .dlgbook file: {e.Message}");
            }
#endif
        }
        
        /// <summary>
        /// Loads dialogue data from a JSON string and populates this DialogueBook.
        /// Serialization is fallible — unrecognized fields are silently ignored and
        /// missing fields fall back to safe defaults. This allows the dialogue format
        /// to grow over time without breaking existing assets.
        /// </summary>
        /// <param name="json">The JSON string to load from</param>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[Signalia DialogueBook] Cannot load from null or empty JSON.");
                return;
            }
            
            try
            {
                var jsonData = JsonUtility.FromJson<DlgBookJsonData>(json);
                
                if (jsonData == null)
                {
                    Debug.LogError("[Signalia DialogueBook] Failed to parse JSON data.");
                    return;
                }
                
                // Populate top-level fields (fallback to existing values if JSON field is absent/empty)
                if (jsonData.dialogueName != null) dialogueName = jsonData.dialogueName;
                if (jsonData.dialogueContext != null) dialogueContext = jsonData.dialogueContext;
                if (jsonData.startEvent != null) startEvent = jsonData.startEvent;
                if (jsonData.endEvent != null) endEvent = jsonData.endEvent;
                
                // Deserialize lines
                if (jsonData.lines != null)
                {
                    var lineList = new List<Line>();
                    
                    for (int i = 0; i < jsonData.lines.Length; i++)
                    {
                        var lineJson = jsonData.lines[i];
                        if (lineJson == null) continue;
                        
                        var line = new Line
                        {
                            BranchName = lineJson.branchName ?? "default",
                            SpeakerName = lineJson.speakerName ?? "",
                            LineEvent = lineJson.lineEvent ?? "",
                            ExitBranch = lineJson.exitBranch ?? ""
                        };
                        
                        // Parse line type (fallible — defaults to Speech on unknown values)
                        if (!string.IsNullOrEmpty(lineJson.lineType) && 
                            System.Enum.TryParse<LineType>(lineJson.lineType, true, out var parsedType))
                        {
                            line.LineType = parsedType;
                        }
                        else
                        {
                            line.LineType = LineType.Speech;
                        }
                        
                        // Deserialize speech data
                        if (lineJson.speech != null)
                        {
                            line.Speech = new Speech
                            {
                                speechText = lineJson.speech.speechText ?? "",
                                animationOverride = lineJson.speech.animationOverride ?? "",
                                audioEvent = lineJson.speech.audioEvent ?? "",
                                newSaveKey = lineJson.speech.newSaveKey ?? ""
                            };
                        }
                        
                        // Deserialize choices
                        if (lineJson.choices != null && lineJson.choices.Length > 0)
                        {
                            line.Choices = new List<Choice>();
                            foreach (var choiceJson in lineJson.choices)
                            {
                                if (choiceJson == null) continue;
                                line.Choices.Add(new Choice
                                {
                                    choiceText = choiceJson.choiceText ?? "",
                                    choiceEvent = choiceJson.choiceEvent ?? "",
                                    newSaveKey = choiceJson.newSaveKey ?? "",
                                    branchToSwitchTo = choiceJson.branchToSwitchTo ?? ""
                                });
                            }
                        }
                        
                        lineList.Add(line);
                    }
                    
                    lines = lineList.ToArray();
                }
                
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
                
                Debug.Log($"[Signalia DialogueBook] Loaded dialogue '{dialogueName}' with {(lines != null ? lines.Length : 0)} lines.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Signalia DialogueBook] Error loading from JSON: {e.Message}\n{e.StackTrace}");
            }
        }
        
        #region JSON Data Transfer Objects (Fallible)
        
        /// <summary>
        /// Root JSON structure for .dlgbook files.
        /// Fields that don't exist in the JSON are silently ignored by JsonUtility.
        /// New fields can be added here as the dialogue system grows.
        /// </summary>
        [System.Serializable]
        private class DlgBookJsonData
        {
            public string dialogueName;
            public string dialogueContext;
            public string startEvent;
            public string endEvent;
            public DlgLineJson[] lines;
        }
        
        [System.Serializable]
        private class DlgLineJson
        {
            public string branchName;
            public string speakerName;
            public string lineEvent;
            public string lineType;
            public string exitBranch;
            public DlgSpeechJson speech;
            public DlgChoiceJson[] choices;
            // editorMetadata and other unknown fields are silently ignored by JsonUtility
        }
        
        [System.Serializable]
        private class DlgSpeechJson
        {
            public string speechText;
            public string animationOverride;
            public string audioEvent;
            public string newSaveKey;
        }
        
        [System.Serializable]
        private class DlgChoiceJson
        {
            public string choiceText;
            public string choiceEvent;
            public string newSaveKey;
            public string branchToSwitchTo;
            // audioEvent and other future fields are silently ignored by JsonUtility
        }
        
        #endregion
        
        #region Testing and Sampling

        [ContextMenu("Fill With Mock Data")]
        public void FillWithMock(string sampleKey = "", bool choice = false)
        {
            if (choice)
                lines = Line.MockData_Choice(sampleKey);
            else
                lines = Line.MockData_Speech(sampleKey);
        }

        /// <summary>
        /// Used for testing.
        /// </summary>
        /// <returns></returns>
        public static DialogueBook MakeSample(bool withExitSampleRandom = false)
        {
            var dg = CreateInstance<DialogueBook>();
            dg.FillWithMock("neutral_encounter", true);

            if (withExitSampleRandom)
            {
                var dg_ex = CreateInstance<DialogueBook>();
                dg_ex.FillWithMock("suspicious_check");
                dg.exitDialogue = dg_ex;
            }
            return dg;
        }

        #endregion

        public string DefaultBranch()
        {
            // the first branch
            return lines.First().BranchName;
        }
    }
}
