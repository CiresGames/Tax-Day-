using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocumentValidator : MonoBehaviour
{
    [SerializeField] string _documentName;
    [SerializeField] InputFieldController[] inputFields;
    bool isValid;
    [SerializeField] TextMeshProUGUI correctAnswersAmountLabel;
    [SerializeField] TextMeshProUGUI documentStateLabel;

    [SerializeField] Button validateButton; 
    [SerializeField] Button resetButton; 


    private void Start()
    {
        bool hasBeenValidated = ES3.KeyExists(_documentName);
        InitializeDocument(hasBeenValidated);
        validateButton.onClick.AddListener(ValidateDocument);
        resetButton.onClick.AddListener(ResetDocument); 

    }

    public void InitializeDocument(bool hasBeenValidated)
    {
        if (!hasBeenValidated)
        {
            foreach (InputFieldController input in inputFields)
            {
                input.EmptyField();
            }
        }
        else
        {
            foreach (InputFieldController input in inputFields)
            {
                input.LoadAnswer();
                ValidateDocument(); 
            }
        }
    }

    public void ResetDocument()
    {
        foreach (InputFieldController input in inputFields)
        {
            input.EmptyField();
            ValidateDocument(); 
        }
    }

    public int CorrectAnswers()
    {
        int target = 0;
        foreach (InputFieldController input in inputFields)
        {
            if (input.CompareInput())
                target++;
        }
        return target;
    }

    public bool IsValid()
    {
        return CorrectAnswers() == inputFields.Length;
    }

    public void ValidateDocument()
    {
        int correct = CorrectAnswers();
        isValid = correct == inputFields.Length;

        correctAnswersAmountLabel.text = $"{correct}/{inputFields.Length}";
        documentStateLabel.text = isValid ? "VALID" : "INVALID";

        Color stateColor = isValid ? Color.green : Color.red;
        correctAnswersAmountLabel.color = stateColor;
        documentStateLabel.color = stateColor;

        ES3.Save(_documentName, isValid);
    }
}