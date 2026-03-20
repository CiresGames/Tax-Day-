using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldController : MonoBehaviour
{
    [SerializeField] string _inputName;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] string key;
    [SerializeField] Image validationFeedback; 

    private void Start()
    {
        inputField.onSubmit.AddListener(SaveAnswer);
        inputField.onEndEdit.AddListener(SaveAnswer);
    }

    private void OnDestroy()
    {
        inputField.onSubmit.RemoveListener(SaveAnswer);
        inputField.onEndEdit.RemoveListener(SaveAnswer);
    }

    public void EmptyField()
    {
        inputField.text = string.Empty;
        ES3.DeleteKey(_inputName);
    }

    public bool CompareInput()
    {
        if (inputField == null) return false;
        if (string.IsNullOrEmpty(key)) return false;
        return key == inputField.text.Trim();
    }


    public void SaveAnswer(string value)
    {
        if (!string.IsNullOrEmpty(value))
            ES3.Save(_inputName, value);
    }

    public void LoadAnswer()
    {
        if (ES3.KeyExists(_inputName))
            inputField.text = ES3.Load<string>(_inputName);
    }
}