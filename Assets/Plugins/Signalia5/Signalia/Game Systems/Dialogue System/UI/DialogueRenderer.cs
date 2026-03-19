using System;
using System.Collections.Generic;
using System.Linq;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.PackageHandlers;
using AHAKuo.Signalia.GameSystems.Localization.Internal;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities;
using TMPro;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem
{
    /// <summary>
    /// Handles moving through dialogue and displaying it. Allows customization and omission of elements per style.
    /// This is the main class that drives dialogue flow and presentation.
    /// </summary>
    public class DialogueRenderer : MonoBehaviour
    {
        #region Public API

        public string DialogueStyleName => dialogueStyleName;

        /// <summary>
        /// Continue the current dialogue.
        /// </summary>
        public void Continue()
        {
            if (!Active) return;
            if (WillEndNext)
            {
                StopDialogueFlow();
                return;
            }
            CurrentDialogueIndex++;
            ReadFlow(CurrentDialogueIndex);
        }

        /// <summary>
        /// Continue the current dialogue with a choice selection.
        /// </summary>
        public void Continue(Choice choice)
        {
            if (!Active) return;

            if (choice.branchToSwitchTo.HasValue())
            {
                CurrentDialogueBranch = choice.branchToSwitchTo;
                CurrentDialogueIndex = 0;
                ReadFlow(CurrentDialogueIndex);
                return;
            }
            Continue();
        }

        #endregion

        #region Serialized Fields

        [Header("Style")]
        [Tooltip("The dialogue style to use when displaying dialogue.")]
        [SerializeField] private string dialogueStyleName = "default";

        [Header("Text Elements")]
        [SerializeField] private TMP_Text speechArea;
        [SerializeField] private TMP_Text speakerName;

        [Header("Container Views")]
        [SerializeField] private UIView speechAreaContainer;
        [SerializeField] private UIView speakerNameContainer;
        [SerializeField] private UIView choicesContainer;

        [Header("Buttons")]
        [Tooltip("Listens to global continue event; can also be manually clicked.")]
        [SerializeField] private UIButton continueButton;
        [SerializeField] private Transform choiceButtonsContent;

        [Header("Choice Resources")]
        [Tooltip("Pooled. When choiceNotAPrefab is true, this is disabled and the in-scene instance is used instead.")]
        [SerializeField] private GameObject choiceButtonPrefab;
        [Tooltip("When true, the choice button is in-scene rather than a prefab.")]
        [SerializeField] private bool choiceNotAPrefab;
        [Tooltip("Warmup count to prevent stuttering when adding choices.")]
        [SerializeField] private int buttonWarmup = 3;

        [Header("Configuration")]
        [Tooltip("If true, will automatically configure the view for dialogue.")]
        [SerializeField] private bool configViewForDialogue = true;
        [SerializeField] private DialogueReadingAnimation readingAnimation;

        #endregion

        #region Runtime State

        private DialogueObjPlug CurrentDisplayedDialogue { get; set; }
        private int CurrentDialogueIndex { get; set; } = -1; // tracks line index within current branch
        private string CurrentDialogueBranch { get; set; } = "default"; // tracks which branch we are moving in
        private Line CurrentLine => CurrentDisplayedDialogue.ReadLine(CurrentDialogueIndex, CurrentDialogueBranch); // TODO: index semantics may need revision when branches differ in length
        private bool Active { get; set; }
        private bool WillEndNext { get; set; } // if true, dialogue will end on the next continue action

        private readonly List<GameObject> activeChoiceButtons = new();
        private ChoiceOmission choiceOmissionMode = ChoiceOmission.OmitFully; // config-driven
        private string choiceOmissionString = "???"; // shown for hidden choices when ShowDisabled mode

        #endregion

        #region Lifecycle

        private void Awake()
        {
            SIGS.Listener(DialogueEventConsts.DialogueInitEvent, () => DialogueManager.RegisterView(this));
            if (DialogueManager.DialogueInited)
                DialogueManager.RegisterView(this);

            if (choiceNotAPrefab && choiceButtonPrefab != null)
                choiceButtonPrefab.SetActive(false);

            if (choiceButtonPrefab != null && buttonWarmup > 0)
                choiceButtonPrefab.WarmupPool(buttonWarmup);

            AutoConfigView();
            ReInit();
        }

        #endregion

        #region Initialization

        private void AutoConfigView()
        {
            if (!configViewForDialogue) return;

            speakerNameContainer?.ApplyDialogueSystemSettings();
            speechAreaContainer?.ApplyDialogueSystemSettings();
            choicesContainer?.ApplyDialogueSystemSettings();
        }

        private void ReInit(DialogueBook dg = null)
        {
            Active = false;
            CurrentDialogueBranch = dg != null ? dg.DefaultBranch() : "default";
            CurrentDialogueIndex = 0;
            CurrentDisplayedDialogue = default;
            WillEndNext = false;

            var config = ConfigReader.GetConfig().DialogueSystem;
            choiceOmissionMode = config.ChoiceOmissionMode;
            choiceOmissionString = config.ChoiceOmissionString;

            ResetActiveChoiceButtons();
        }

        private void ResetActiveChoiceButtons()
        {
            foreach (var btn in activeChoiceButtons)
                btn.SetActive(false);
            activeChoiceButtons.Clear();
        }

        #endregion

        #region Dialogue Flow

        public void BeginDialogueFlow(DialogueBook dg)
        {
            ReInit(dg);
            CurrentDisplayedDialogue = dg.ToPlug();

            SendDialogueEvent(CurrentDisplayedDialogue.startEvent);
            ReadFlow(0);
            Active = true;
        }

        private void ReadFlow(int i)
        {
            var line = CurrentLine;
            if (line == null)
            {
                StopDialogueFlow();
                Debug.Log($"Dialogue ended prematurely. Could not read line at index {i} in branch {CurrentDialogueBranch}.");
                return;
            }

            // Skip lines that cannot be shown
            var skipSpeech = line.LineType == LineType.Speech && !line.Speech.ConditionMet();
            var skipEmptyChoice = line.LineType == LineType.Choice && line.Choices.Count == 0;
            var skipAllChoicesHidden = line.LineType == LineType.Choice && line.Choices.All(x => !x.ConditionMet());

            if (skipSpeech || skipEmptyChoice || skipAllChoicesHidden)
            {
                CurrentDialogueIndex++;
                ReadFlow(CurrentDialogueIndex);
                return;
            }

            UpdateSpeakerDisplay(line);
            switch (line.LineType)
            {
                case LineType.Speech:
                    UpdateSpeechDisplay(line);
                    break;
                case LineType.Choice:
                    UpdateChoiceDisplay(line);
                    break;
            }

            // Branch switch: -1 tricks next iteration to count from 0 for the new branch
            if (CurrentLine.ExitBranch.HasValue())
            {
                CurrentDialogueBranch = CurrentLine.ExitBranch;
                CurrentDialogueIndex = -1;
            }

            var noMoreLines = CurrentDisplayedDialogue.OutsideIndex(CurrentDialogueIndex + 1, CurrentDialogueBranch);
            WillEndNext = noMoreLines;
        }

        private void StopDialogueFlow()
        {
            Active = false;
            SendDialogueEvent(CurrentDisplayedDialogue.endEvent);

            // TODO: make everything optional via null check
            if (CurrentDisplayedDialogue.exit == null)
            {
                DialogueManager.EndDialogue();
                ToggleVisibility(false);
            }
            else
            {
                var config = ConfigReader.GetConfig().DialogueSystem;
                if (config.reshowOnExitObject)
                    ToggleVisibility(false);

                SIGS.DoIn(config.reshowDelay, () =>
                    DialogueManager.ContinueTo(CurrentDisplayedDialogue.exit));
            }
        }

        #endregion

        #region Display Updates

        private void UpdateSpeakerDisplay(Line line)
        {
            // Choice lines don't have a single speaker; design may evolve for multi-speaker choices
            if (line.LineType == LineType.Choice) return;

            if (speakerNameContainer != null && (!speakerNameContainer.IsShown || speakerNameContainer.IsHiding))
                speakerNameContainer.Show();

            speakerName.SetLocalizedText(line.SpeakerName);
        }

        private void UpdateSpeechDisplay(Line line)
        {
            if (speechAreaContainer != null && (!speechAreaContainer.IsShown || speechAreaContainer.IsHiding))
                speechAreaContainer.Show();

            // TODO: integrate with readingAnimation when implemented
            speechArea.SetLocalizedText(line.Speech.speechText);
            // TODO: during reading animations, keep continue button inactive until ready
            continueButton.gameObject.SetActive(true);

            if (choicesContainer != null && !choicesContainer.IsHidden)
                choicesContainer.Hide();
        }

        private void UpdateChoiceDisplay(Line line)
        {
            ResetActiveChoiceButtons();

            if (choicesContainer != null && (!choicesContainer.IsShown || choicesContainer.IsHiding))
                choicesContainer.Show();

            // Optional omissions while choices are shown (config-driven)
            var dgConfig = ConfigReader.GetConfig().DialogueSystem;
            if (dgConfig.hideSpeakerNameOnChoice && speakerNameContainer != null && !speakerNameContainer.IsHidden)
                speakerNameContainer.Hide();
            if (dgConfig.hideSpeechAreaOnChoice && speechAreaContainer != null && !speechAreaContainer.IsHidden)
                speechAreaContainer.Hide();

            foreach (var choice in CurrentLine.Choices)
            {
                var conditionMet = choice.ConditionMet();
                if (!conditionMet && choiceOmissionMode == ChoiceOmission.OmitFully)
                    continue;

                var btn = choiceButtonPrefab.FromPool(-1f, false, (typeof(TMP_Text), true), (typeof(UIButton), false));
                var uibtn = btn.compCache.GetCached<UIButton>();
                var txt = btn.compCache.GetCached<TMP_Text>();

                uibtn.ClearActions();
                btn.gameObject.SetActive(true); // enable so it can cache its components

                if (!conditionMet && choiceOmissionMode == ChoiceOmission.ShowDisabled && choiceOmissionString.HasValue())
                    txt.SetLocalizedText(choiceOmissionString);
                else
                    txt.SetLocalizedText(choice.choiceText);

                btn.gameObject.transform.SetParent(choiceButtonsContent, false);

                if (conditionMet)
                {
                    uibtn.SetInteractive(true);
                    uibtn.AddNewAction(() => DialogueEventConsts.ChoiceChosenEvent.SendEvent(choice));
                }
                else
                {
                    uibtn.SetInteractive(false);
                }

                activeChoiceButtons.Add(btn.gameObject);
            }

            // During choices, optionally hide continue button (config-driven)
            if (continueButton != null)
                continueButton.gameObject.SetActive(!dgConfig.disableContinueButtonOnChoice);
        }

        #endregion

        #region Visibility

        private void ToggleVisibility(bool visible)
        {
            if (visible)
            {
                if (speechAreaContainer != null && (!speechAreaContainer.IsShown || speechAreaContainer.IsHiding))
                    speechAreaContainer.Show();
                if (speakerNameContainer != null && (!speakerNameContainer.IsShown || speakerNameContainer.IsHiding))
                    speakerNameContainer.Show();
                if (choicesContainer != null && (!choicesContainer.IsShown || choicesContainer.IsHiding))
                    choicesContainer.Show();
            }
            else
            {
                if (speechAreaContainer != null && !speechAreaContainer.IsHidden)
                    speechAreaContainer.Hide();
                if (speakerNameContainer != null && !speakerNameContainer.IsHidden)
                    speakerNameContainer.Hide();
                if (choicesContainer != null && !choicesContainer.IsHidden)
                    choicesContainer.Hide();
            }
        }

        #endregion

        #region Helpers

        private void SendDialogueEvent(string eventName)
        {
            if (eventName.HasValue())
                eventName.SendEvent();
        }

        #endregion
    }
}
