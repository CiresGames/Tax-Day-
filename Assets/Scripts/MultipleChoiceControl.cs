using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoiceController : Question
{
    
    [SerializeField] Toggle[] choices;
    [SerializeField] ToggleGroup toggleGroup;
    string selectedValue = string.Empty;

    public void InitializeChoices()
    {
        if (_questionData?.choices == null) return;

        for (int i = 0; i < choices.Length; i++)
        {
            var label = choices[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null && i < _questionData.choices.Count)
                label.text = _questionData.choices[i];

            choices[i].group = toggleGroup;

            Toggle current = choices[i];
            choices[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn) OnChoiceSelected(current);
            });
        }
    }

    private void OnDestroy()
    {
        foreach (Toggle choice in choices)
            choice.onValueChanged.RemoveAllListeners();
    }

    private void OnChoiceSelected(Toggle selected)
    {
        selectedValue = selected.GetComponentInChildren<TMPro.TextMeshProUGUI>().text;
        SaveAnswer(selectedValue);
    }

    protected override string GetValue() => selectedValue;

    protected override void SetValue(string value)
    {
        selectedValue = value;
        foreach (Toggle choice in choices)
        {
            var label = choice.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null && label.text == value)
            {
                choice.isOn = true;
                break;
            }
        }
    }

    public override void EmptyField()
    {
        selectedValue = string.Empty;
        toggleGroup.SetAllTogglesOff();
        DeleteAnswer();
    }

   
}