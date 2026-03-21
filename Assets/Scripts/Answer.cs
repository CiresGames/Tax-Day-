using ES3Types;
using System.Globalization;
using UnityEngine;

public abstract class Answer : MonoBehaviour
{
    public AnswerData _answerData;

    public abstract void EmptyField();
    
    public virtual bool IsValid()
    {
        if (string.IsNullOrEmpty(_answerData.answer)) return false;
        
        return _answerData.answer == GetValue(); 

    }


    public virtual void SaveAnswer(string value)
    {
        if (!string.IsNullOrEmpty(value)) ES3.Save(_answerData.uid, value);  
    }

    public virtual void LoadAnswer()
    {
        if (ES3.KeyExists(_answerData.uid)) SetValue(ES3.Load<string>(_answerData.uid)); 
      
    }

    public virtual void DeleteAnswer()
    {
        if (ES3.KeyExists(_answerData.uid))
            ES3.DeleteKey(_answerData.uid);
    }


    protected abstract string GetValue(); 
    protected abstract void SetValue(string value); 


}
