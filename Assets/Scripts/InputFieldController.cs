using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldController : Answer
{
    [SerializeField] TMP_InputField inputField;

    private void Start()
    {
        LoadAnswer(); 
        inputField.onSubmit.AddListener(SaveAnswer);
        inputField.onEndEdit.AddListener(SaveAnswer);
    }

    private void OnDestroy()
    {
        inputField.onSubmit.RemoveListener(SaveAnswer);
        inputField.onEndEdit.RemoveListener(SaveAnswer);
    }

    protected override string GetValue() => inputField.text;
    protected override void SetValue(string value) => inputField.text = value;

    public override void EmptyField()
    {
        SetValue(string.Empty);
        DeleteAnswer();
    }

   
    
}