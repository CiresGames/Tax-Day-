using ES3Types;
using System.Globalization;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public abstract class Question : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _questionLabel;
    public QuestionData _questionData;
    public virtual void SetQuestion()
    {
        _questionLabel.text = _questionData.question; 
    }

    public virtual void SaveAnswer(string value)
    {
        if (!string.IsNullOrEmpty(value)) ES3.Save(_questionData.uid, value);  
    }

    public virtual void LoadAnswer()
    {
        if (ES3.KeyExists(_questionData.uid)) SetValue(ES3.Load<string>(_questionData.uid));
        else return; 
      
    }

    public virtual void DeleteAnswer()
    {
        if (ES3.KeyExists(_questionData.uid))
            ES3.DeleteKey(_questionData.uid);
    }

    public virtual bool IsValid()
    {
        if (string.IsNullOrEmpty(_questionData.answer)) return false;
        
        return _questionData.answer == GetValue(); 

    }

    public abstract void EmptyField();
    protected abstract string GetValue(); 
    protected abstract void SetValue(string value); 


}
