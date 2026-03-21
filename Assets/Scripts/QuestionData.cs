
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AnswerSO", menuName = "Scriptable Objects/AnswerData")]
public class QuestionData : ScriptableObject
{
    public string uid;
    public TYPE type; 


    public string question; 
    public string answer;

    public List<string> choices; 


    public enum TYPE
    {
        OPEN, 
        MULTIPLE_CHOICE, 
    }

}
