using UnityEngine;
using UnityEngine.UI;

public class MultipleChoiceController : Answer
{
    [SerializeField] Toggle[] choices;
    [SerializeField] ToggleGroup toggleGroup;
    string selectedValue = string.Empty;

    public void InitializeChoices()
    {
        if (_answerData?.choices == null) return;

        for (int i = 0; i < choices.Length; i++)
        {
            var label = choices[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null && i < _answerData.choices.Count)
                label.text = _answerData.choices[i];

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