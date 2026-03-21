
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AnswerSO", menuName = "Scriptable Objects/AnswerData")]
public class AnswerData : ScriptableObject
{
    public string uid;
    public string answer;
    public List<string> choices; 


}
