using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem  
{
    #region Speaker Avatar
    /// <summary>
    /// Color palette for the speaker. Can be used for whatever. I just thought it was useful.
    /// </summary>
    [Serializable]
    public struct SpeakerColorPalette
    {
        public Color primaryColor;
        public Color secondaryColor;
        public Dictionary<string, Color> otherColors;
    }

    /// <summary>
    /// Can help distinguish speakers in the dialogue system via their expressions or plates.
    /// </summary>
    [Serializable]
    public struct SpeakerExpressions
    {
        [SerializeField] private string expressionName;
        [SerializeField] private Sprite expressionImage;
        
        // unnecessary boiler? not sure. sorry, csharp :(
        public string ExpressionName => expressionName;
        public Sprite ExpressionImage => expressionImage;
    }
    #endregion

    #region Dialogue Object
    [Serializable]
    /// <summary>
    /// A step in the dialogue object. Contains: Branch Name (default empty), Speaker Name, Line Type (Dialogue, Choice), and Exit Branch.
    /// This is the main object that holds all the data for a dialogue step. Anything you need to do per dialogue step is here.
    /// </summary>
    public class Line
    {
        /// <summary>
        /// Is this a default branch?
        /// </summary>
        public bool DefaultBranch => BranchName == "default" || BranchName.IsNullOrEmpty();
        
        public string BranchName = "default"; // or empty, both can be used.
        public string SpeakerName;
        public string LineEvent;
        public LineType LineType = LineType.Speech;
        public string ExitBranch;
        
        public Speech Speech = new(""); // visible only if lineType is Speech
        public List<Choice> Choices = new(); // visible only if lineType is Choice

        private Line(string sample, string loremTheGreat, LineType speech1)
        {
            Speech = new(sample);
            SpeakerName = loremTheGreat;
            LineType = speech1; // wtf?
        }

        public Line() {}

        #region Testing and Sampling [SPEECH]

        private static Dictionary<string, string[]> sampleSpeech = new()
        {
            // Default / empty
            [""] = Array.Empty<string>(),

            ["neutral_encounter"] = new[]
            {
                "I didn’t expect to see anyone down here.",
                "This place has been abandoned for years.",
                "You should turn back while you still can."
            },

            ["suspicious_check"] = new[]
            {
                "You shouldn’t be here.",
                "This area was sealed.",
                "How did you get past the gates?"
            },

            ["mocking_intro"] = new[]
            {
                "So this is what they sent?",
                "I was expecting more.",
                "Try not to disappoint me too quickly."
            },

            ["cold_warning"] = new[]
            {
                "It doesn’t matter why you came.",
                "You crossed a line.",
                "There’s no way out for you now."
            },

            ["escalation"] = new[]
            {
                "You keep pushing your luck.",
                "I warned you once.",
                "I won’t repeat myself."
            },

            ["pre_fight"] = new[]
            {
                "Enough talking.",
                "Draw your weapon.",
                "Let’s end this."
            }
        };

        /// <summary>
        /// Returns a random speech from a sequence key. Testing only.
        /// </summary>
        public static Speech Sampling(string key = "")
        {
            if (!sampleSpeech.TryGetValue(key, out var seq) || seq.Length == 0)
                return null;

            return new Speech(seq[UnityEngine.Random.Range(0, seq.Length)]);
        }

        /// <summary>
        /// Generates mock dialogue lines for inspector testing.
        /// </summary>
        public static Line[] MockData_Speech(string mockData = "")
        {
            if (!sampleSpeech.TryGetValue(mockData, out var seq) || seq.Length == 0)
                return Array.Empty<Line>();

            return seq
                .Select(t => new Line(t, "Lorem the Great", LineType.Speech))
                .ToArray();
        }
        
        /// <summary>
        /// Returns one line element as choice with some options.
        /// </summary>
        /// <returns></returns>
        public static Line MockChoiceElement()
        {
            var l = new Line()
            {
                LineType = LineType.Choice,
                Choices = new List<Choice>
                {
                    new("Yes"),
                    new("No")
                },
                SpeakerName = "Player" // probably cannot use this correctly.
            };

            return l;
        }

        public static Line[] MockData_Choice(string mockData = "")
        {
            if (!sampleSpeech.TryGetValue(mockData, out var seq) || seq.Length == 0)
                return Array.Empty<Line>();

            var array = seq
                .Select(t => new Line(t, "Lorem the Great", LineType.Speech))
                .ToArray();
            
            // to list, and then add choice line (this doesn't have much sophistication, literally just for the purpose of testing. It prolly returns Yes No.
            var list = array.ToList();
            list.Add(Line.MockChoiceElement());

            return list.ToArray();
        }

        #endregion
    }
    
    // todo: I might make the choice and speech part of the line class as they exist there for the most part.
    // todo: line, choice and speech as structs? idk.
    
    [Serializable]
    /// <summary>
    /// A choice in the line. Contains text, and whether it can be shown.
    /// </summary>
    public class Choice : IConditional
    {
        public string choiceText;
        public string choiceEvent;
        public string newSaveKey; // uses gamesaving to make a save key with boolean True value, saves into defined save file in config.
        public string branchToSwitchTo;
        
        public bool ConditionMet()
        {
            return true;
        } // todo: change it.
        
        /// <summary>
        /// Really don't recommend using this constructor.
        /// It's just for fallbacking and optional parameters.
        /// </summary>
        /// <param name="choiceText"></param>
        public Choice(string choiceText) => this.choiceText = choiceText;
        
        public Choice() {}
    }
    
    [Serializable]
    /// <summary>
    /// A speech in the line. Contains text, and whether it can be shown, and a reading animation override.
    /// </summary>
    public class Speech : IConditional
    {
        [TextArea] public string speechText;
        public string animationOverride; // name of reader animation to use instead of the default one. Does not change the course of other speeches.
        public string audioEvent; // not to be confused with the ticker audio, this is an audio event to play when the speech is shown exactly.
        public string newSaveKey; // key to save if this speech is shown.
        
        public bool ConditionMet()
        {
            return true;
        } // todo: change it.
        
        /// <summary>
        /// Really don't recommend using this constructor.
        /// </summary>
        /// <param name="speechText"></param>
        public Speech(string speechText) => this.speechText = speechText;
        
        public Speech() {}
    }
    #endregion

}
